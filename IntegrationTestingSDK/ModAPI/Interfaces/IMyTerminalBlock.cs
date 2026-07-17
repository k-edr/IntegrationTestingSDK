namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Base interface for all terminal blocks.
    /// </summary>
    public interface IMyTerminalBlock
    {
        string CustomName { get; set; }
        string CustomData { get; set; }
        bool ShowOnHUD { get; set; }
        bool ShowInTerminal { get; set; }
        bool ShowInToolbarConfig { get; set; }
        bool ShowInInventory { get; set; }
        string CustomNameWithFaction { get; }
        string DetailedInfo { get; }
        bool HasLocalPlayerAccess();
        bool HasPlayerAccess(long playerId);
    }
}
