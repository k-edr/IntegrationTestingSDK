namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for ship welder blocks.
    /// </summary>
    public interface IMyShipWelder : IMyFunctionalBlock
    {
        bool HelpOthers { get; set; }
    }
}
