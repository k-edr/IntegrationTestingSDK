namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Terminal property interface mirroring Sandbox.ModAPI.Ingame.ITerminalProperty.
    /// </summary>
    public interface ITerminalProperty
    {
        string Id { get; }
        string TypeName { get; }
    }
}
