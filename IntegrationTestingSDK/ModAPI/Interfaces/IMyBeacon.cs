namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for beacon blocks.
    /// </summary>
    public interface IMyBeacon : IMyFunctionalBlock
    {
        float Radius { get; set; }
        string HudText { get; set; }
    }
}
