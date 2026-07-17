using IntegrationTestingSDK.ModAPI.Enums;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for jump drive blocks.
    /// </summary>
    public interface IMyJumpDrive : IMyFunctionalBlock
    {
        float CurrentStoredPower { get; }
        float MaxStoredPower { get; }
        float JumpDistanceMeters { get; set; }
        float MaxJumpDistanceMeters { get; }
        float MinJumpDistanceMeters { get; }
        float JumpDistanceRatio { get; set; }
        bool Recharge { get; set; }
        MyJumpDriveStatus Status { get; }
    }
}
