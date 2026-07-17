namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for ship drill blocks.
    /// </summary>
    public interface IMyShipDrill : IMyFunctionalBlock
    {
        bool TerrainClearingMode { get; set; }
    }
}
