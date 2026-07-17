namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for reactor blocks.
    /// </summary>
    public interface IMyReactor : IMyFunctionalBlock
    {
        bool UseConveyorSystem { get; set; }
    }
}
