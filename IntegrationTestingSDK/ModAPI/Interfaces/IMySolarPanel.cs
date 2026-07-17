namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Solar panel interface. Mirrors Sandbox.ModAPI.Ingame.IMySolarPanel.
    /// </summary>
    public interface IMySolarPanel : IMyFunctionalBlock
    {
        float MaxOutput { get; }
        float CurrentOutput { get; }
    }
}
