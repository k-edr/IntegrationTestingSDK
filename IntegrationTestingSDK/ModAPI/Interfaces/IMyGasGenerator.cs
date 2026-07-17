namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for gas generator blocks (O2/H2 generators).
    /// </summary>
    public interface IMyGasGenerator : IMyFunctionalBlock
    {
        bool AutoRefill { get; set; }
        bool IsProducing { get; }
        bool UseConveyorSystem { get; set; }
    }
}
