using IntegrationTestingSDK.ModAPI.Types;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for thruster blocks.
    /// </summary>
    public interface IMyThrust : IMyFunctionalBlock
    {
        float ThrustOverride { get; set; }
        float ThrustOverridePercentage { get; set; }
        float MaxThrust { get; }
        float MaxEffectiveThrust { get; }
        float CurrentThrust { get; }
        float CurrentThrustPercentage { get; }
        Vector3I GridThrustDirection { get; }
    }
}
