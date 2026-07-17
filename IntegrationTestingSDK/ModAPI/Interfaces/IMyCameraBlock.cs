using IntegrationTestingSDK.ModAPI.Types;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for camera blocks.
    /// </summary>
    public interface IMyCameraBlock : IMyFunctionalBlock
    {
        bool IsActive { get; }
        double AvailableScanRange { get; }
        bool EnableRaycast { get; set; }
        float RaycastConeLimit { get; }
        double RaycastDistanceLimit { get; }
        float RaycastTimeMultiplier { get; }

        MyDetectedEntityInfo Raycast(double distance, float pitch, float yaw);
        MyDetectedEntityInfo Raycast(Vector3D targetPos);
        bool CanScan(double distance);
        int TimeUntilScan(double distance);
    }
}
