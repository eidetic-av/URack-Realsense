using UnityEngine;

namespace Eidetic.URack.Realsense
{
    public class Rs2 : UModule
    {
        static Rsvfx.CombinedDriver deviceDriver;
        Rsvfx.CombinedDriver DeviceDriver => deviceDriver ??
            (GetAsset<GameObject>("Rs2DeviceManager.prefab").Instantiate()
             .GetComponent<Rsvfx.CombinedDriver>());

        public PointCloud pointCloudOutput;
        public PointCloud PointCloudOutput
        {
            get
            {
                if (pointCloudOutput != null) return pointCloudOutput;
                return (pointCloudOutput = ScriptableObject.CreateInstance<PointCloud>());
            }
        }

        public void Start()
        {
            DeviceDriver.PointClouds.Add(PointCloudOutput);
        }

        void OnDestroy()
        {
            DeviceDriver?.PointClouds.Remove(PointCloudOutput);
            Destroy(PointCloudOutput);
        }

    }
}
