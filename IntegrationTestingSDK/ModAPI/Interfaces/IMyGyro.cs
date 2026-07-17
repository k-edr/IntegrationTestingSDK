namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for gyroscope blocks.
    /// </summary>
    public interface IMyGyro : IMyFunctionalBlock
    {
        bool GyroOverride { get; set; }
        float GyroPower { get; set; }
        float Yaw { get; set; }
        float Pitch { get; set; }
        float Roll { get; set; }
    }
}
