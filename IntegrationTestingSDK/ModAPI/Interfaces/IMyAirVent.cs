using IntegrationTestingSDK.ModAPI.Enums;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Air vent interface. Mirrors Sandbox.ModAPI.Ingame.IMyAirVent.
    /// </summary>
    public interface IMyAirVent : IMyFunctionalBlock
    {
        bool Depressurize { get; set; }
        VentStatus Status { get; }

        bool IsPressurized();
        float GetOxygenLevel();
    }
}
