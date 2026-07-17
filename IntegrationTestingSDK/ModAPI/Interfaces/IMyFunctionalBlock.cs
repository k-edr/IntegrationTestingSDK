namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for functional blocks that can be enabled/disabled.
    ///     Inherits from IMyTerminalBlock.
    /// </summary>
    public interface IMyFunctionalBlock : IMyTerminalBlock
    {
        bool Enabled { get; set; }
    }
}
