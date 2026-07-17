namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for upgrade module blocks.
    /// </summary>
    public interface IMyUpgradeModule : IMyFunctionalBlock
    {
        uint UpgradeCount { get; }
        uint Connections { get; }
    }
}
