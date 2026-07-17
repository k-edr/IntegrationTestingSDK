namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Button panel interface. Mirrors Sandbox.ModAPI.Ingame.IMyButtonPanel.
    /// </summary>
    public interface IMyButtonPanel : IMyTerminalBlock
    {
        string GetButtonName(int index);
        void SetCustomButtonName(int index, string name);
    }
}
