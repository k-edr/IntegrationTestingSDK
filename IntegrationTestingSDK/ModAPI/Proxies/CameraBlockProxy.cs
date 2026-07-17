using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;
using IntegrationTestingSDK.ModAPI.Types;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class CameraBlockProxy : FunctionalBlockProxy, IMyCameraBlock
    {
        public bool IsActive
        {
            get => bool.TryParse(GetProperty("IsActive"), out var v) && v;
        }

        public double AvailableScanRange
        {
            get => double.TryParse(GetProperty("AvailableScanRange"), out var v) ? v : 0.0;
        }

        public bool EnableRaycast
        {
            get => bool.TryParse(GetProperty("EnableRaycast"), out var v) && v;
            set => SetProperty("EnableRaycast", value.ToString());
        }

        public float RaycastConeLimit
        {
            get => float.TryParse(GetProperty("RaycastConeLimit"), out var v) ? v : 0f;
        }

        public double RaycastDistanceLimit
        {
            get => double.TryParse(GetProperty("RaycastDistanceLimit"), out var v) ? v : 0.0;
        }

        public float RaycastTimeMultiplier
        {
            get => float.TryParse(GetProperty("RaycastTimeMultiplier"), out var v) ? v : 0f;
        }

        public MyDetectedEntityInfo Raycast(double distance, float pitch, float yaw)
            => new MyDetectedEntityInfo();

        public MyDetectedEntityInfo Raycast(Vector3D targetPos)
            => new MyDetectedEntityInfo();

        public bool CanScan(double distance) => true;

        public int TimeUntilScan(double distance) => 0;

        internal CameraBlockProxy(IPbTestHarness harness, long gridId, BlockDto block) : base(harness, gridId, block) { }
    }
}
