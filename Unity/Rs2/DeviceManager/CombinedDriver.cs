using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Intel.RealSense;
using System.Threading;

namespace Rsvfx
{
    //
    // CombinedDriver
    //
    // A Unity component that manages a combination of a depth camera (D4xx)
    // and a tracker (T265). This class directly uses the C# wrapper of the
    // RealSense SDK, so there is no dependency to the Unity components
    // included in the RealSense SDK.
    //
    // There are two main functionalities in this component:
    // - Updates attribute maps (position/color) based on depth input.
    // - Updates a transform based on pose input from a tracker.
    //
    // Pose input is buffered in an internal queue to synchronize with depth
    // input. Contrastly, depth input is processed immediately after reception.
    //
    public sealed class CombinedDriver : MonoBehaviour
    {
        [SerializeField] uint2 _resolution = math.uint2(848, 480);
        [SerializeField] uint _framerate = 60;

        [SerializeField] float _depthThreshold = 10;
        [SerializeField, Range(0, 1)] float _brightness = 0;
        [SerializeField, Range(0, 1)] float _saturation = 1;

        [SerializeField] ComputeShader _compute = null;

        public List<Eidetic.URack.PointCloud> PointClouds = new List<Eidetic.URack.PointCloud>();

        public float depthThreshold {
            get { return _depthThreshold; }
            set { _depthThreshold = value; }
        }

        public float brightness {
            get { return _brightness; }
            set { _brightness = value; }
        }

        public float saturation {
            get { return _saturation; }
            set { _saturation = value; }
        }

        Pipeline Pipe;
        Thread DepthThread;
        bool _terminate;

        (VideoFrame color, Points point) _depthFrame;
        (Intrinsics color, Intrinsics depth) _intrinsics;
        readonly object _depthFrameLock = new object();

        DepthConverter _converter;
        double _depthTime;

        void ProcessDepth()
        {
            using (var pcBlock = new PointCloud())
                while (!_terminate)
                    using (var fs = Pipe.WaitForFrames())
                    {
                        // Retrieve and store the color frame.
                        lock (_depthFrameLock)
                        {
                            _depthFrame.color?.Dispose();
                            _depthFrame.color = fs.ColorFrame;

                            using (var prof = _depthFrame.color.
                                   GetProfile<VideoStreamProfile>())
                                _intrinsics.color = prof.GetIntrinsics();

                            pcBlock.MapTexture(_depthFrame.color);
                        }

                        // Construct and store a point cloud.
                        using (var df = fs.DepthFrame)
                        {
                            var pc = pcBlock.Process(df).Cast<Points>();

                            lock (_depthFrameLock)
                            {
                                _depthFrame.point?.Dispose();
                                _depthFrame.point = pc;

                                using (var prof = df.
                                       GetProfile<VideoStreamProfile>())
                                    _intrinsics.depth = prof.GetIntrinsics();
                            }
                        }
                    }
        }

        void Start()
        {
            Pipe = new Pipeline();

            // Depth camera pipeline activation
            using (var config = new Config())
            {
                var r = (int2)_resolution;
                var fps = (int)_framerate;
                config.EnableStream(Stream.Color, r.x, r.y, Format.Rgba8, fps);
                config.EnableStream(Stream.Depth, r.x, r.y, Format.Z16, fps);
                Pipe.Start(config);
            }

            // Worker thread activation
            DepthThread = new Thread(ProcessDepth);
            DepthThread.Start();

            // Local objects initialization
            _converter = new DepthConverter(_compute);
        }

        void OnDestroy()
        {
            // Thread termination
            _terminate = true;
            DepthThread?.Join();
            DepthThread = null;

            // Depth frame finalization
            _depthFrame.color?.Dispose();
            _depthFrame.point?.Dispose();
            _depthFrame = (null, null);

            // Pipeline termination
            Pipe?.Dispose();
            Pipe = null;

            // Local objects finalization
            _converter?.Dispose();
            _converter = null;
        }

        void Update()
        {
            var time = 0.0;

            // Retrieve the depth frame data.
            lock (_depthFrameLock)
            {
                if (_depthFrame.color == null) return;
                if (_depthFrame.point == null) return;
                _converter.LoadColorData(_depthFrame.color, _intrinsics.color);
                _converter.LoadPointData(_depthFrame.point, _intrinsics.depth);
                time = _depthFrame.color.Timestamp;
            }

            // Update the converter options.
            _converter.Brightness = _brightness;
            _converter.Saturation = _saturation;
            _converter.DepthThreshold = _depthThreshold;

            // Update the external attribute maps.
            foreach(var pointCloud in PointClouds)
                _converter.UpdateAttributeMaps(pointCloud);

            // Record the timestamp of the depth frame.
            _depthTime = time;
        }
    }
}
