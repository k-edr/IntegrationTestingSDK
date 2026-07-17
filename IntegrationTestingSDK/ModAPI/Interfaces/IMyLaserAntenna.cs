using IntegrationTestingSDK.ModAPI.Enums;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for laser antenna blocks.
    /// </summary>
    public interface IMyLaserAntenna : IMyFunctionalBlock
    {
        bool IsPermanent { get; set; }
        float Range { get; set; }
        bool IsOutsideLimits { get; }
        MyLaserAntennaStatus Status { get; }

        void Connect();
        void SetTargetCoords(string coords);
    }
}
