using IntegrationTestingSDK.ModAPI.Types;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Heat vent interface. Mirrors Space Engineers in-game heat vent block.
    /// </summary>
    public interface IMyHeatVent : IMyFunctionalBlock
    {
        float PowerDependency { get; set; }
        Color ColorMinimal { get; set; }
        Color ColorMaximal { get; set; }
        Color ColorCurrent { get; }
    }
}
