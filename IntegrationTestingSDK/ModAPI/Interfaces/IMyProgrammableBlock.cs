namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for programmable blocks.
    /// </summary>
    public interface IMyProgrammableBlock : IMyFunctionalBlock
    {
        bool IsRunning { get; }
        string TerminalRunArgument { get; }

        bool TryRun(string argument);
    }
}
