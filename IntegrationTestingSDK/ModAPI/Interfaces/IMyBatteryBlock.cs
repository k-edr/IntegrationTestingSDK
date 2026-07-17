using IntegrationTestingSDK.ModAPI.Enums;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for battery blocks.
    /// </summary>
    public interface IMyBatteryBlock : IMyFunctionalBlock
    {
        bool HasCapacityRemaining { get; }
        float CurrentStoredPower { get; }
        float MaxStoredPower { get; }
        float CurrentInput { get; }
        float MaxInput { get; }
        bool IsCharging { get; }
        ChargeMode ChargeMode { get; set; }
        bool OnlyRecharge { get; set; }
        bool OnlyDischarge { get; set; }
        bool SemiautoEnabled { get; set; }
    }
}
