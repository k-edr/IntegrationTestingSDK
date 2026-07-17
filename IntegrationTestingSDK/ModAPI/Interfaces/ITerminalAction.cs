namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Terminal action interface mirroring Sandbox.ModAPI.Ingame.ITerminalAction.
    /// </summary>
    public interface ITerminalAction
    {
        string Id { get; }
        string Name { get; }
        void Apply();
    }
}
