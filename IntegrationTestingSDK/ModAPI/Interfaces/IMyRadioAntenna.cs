namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for radio antenna blocks.
    /// </summary>
    public interface IMyRadioAntenna : IMyFunctionalBlock
    {
        float Radius { get; set; }
        string HudText { get; set; }
        bool ShowShipName { get; set; }
        bool IsBroadcasting { get; }
        bool EnableBroadcasting { get; set; }
    }
}
