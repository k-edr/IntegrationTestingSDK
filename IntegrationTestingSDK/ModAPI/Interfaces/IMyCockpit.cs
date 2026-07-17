namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for cockpit blocks.
    ///     In the proxy layer, proxies that implement this should also implement IMyShipController.
    /// </summary>
    public interface IMyCockpit
    {
        float OxygenCapacity { get; }
        float OxygenFilledRatio { get; }
    }
}
