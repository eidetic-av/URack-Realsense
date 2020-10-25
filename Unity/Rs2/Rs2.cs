using System;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Eidetic.URack.Realsense
{
    public unsafe class Rs2 : UModule
    {
        public PointCloud pointCloudOutput;
        public PointCloud PointCloudOutput => pointCloudOutput ??
            (pointCloudOutput = ScriptableObject.CreateInstance<PointCloud>());

        public RenderTexture PackedInputStream;

        Texture2D Positions;
        Texture2D Colors;

        public void Start()
        {
            Positions = new Texture2D(PackedInputStream.width, PackedInputStream.height / 2, TextureFormat.RGBAFloat, false);
            Colors = new Texture2D(PackedInputStream.width, PackedInputStream.height / 2, TextureFormat.RGBAFloat, false);
        }

        public void Update()
        {
            // This is slow and dumb
            RenderTexture.active = PackedInputStream;
            Positions.ReadPixels(new Rect(0, 0, PackedInputStream.width, PackedInputStream.height / 2), 0, 0, false);
            Positions.Apply();
            Colors.ReadPixels(new Rect(0, PackedInputStream.height / 2, PackedInputStream.width, PackedInputStream.height), 0, 0, false);
            Colors.Apply();
            PointCloudOutput.SetPositionMap(Positions, false);
            PointCloudOutput.SetColorMap(Colors, false);
        }

    }
}
