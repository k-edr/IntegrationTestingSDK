namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for gas tank blocks (oxygen/hydrogen tanks).
    /// </summary>
    public interface IMyGasTank : IMyFunctionalBlock
    {
        double FilledRatio { get; }
        float Capacity { get; }
        bool Stockpile { get; set; }
        bool AutoRefillBottles { get; set; }
    }
}
