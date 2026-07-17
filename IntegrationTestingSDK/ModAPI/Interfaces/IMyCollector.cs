namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for collector blocks.
    /// </summary>
    public interface IMyCollector : IMyFunctionalBlock
    {
        bool UseConveyorSystem { get; set; }
    }
}
