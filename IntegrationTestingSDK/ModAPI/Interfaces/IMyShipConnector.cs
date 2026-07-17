using IntegrationTestingSDK.ModAPI.Enums;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for ship connector blocks.
    /// </summary>
    public interface IMyShipConnector : IMyFunctionalBlock
    {
        IMyShipConnector OtherConnector { get; }
        bool IsLocked { get; }
        bool IsConnected { get; }
        MyShipConnectorStatus Status { get; }
        bool ThrowOut { get; set; }
        bool CollectAll { get; set; }
        float PullStrength { get; set; }
        bool IsParkingEnabled { get; set; }

        void Connect();
        void Disconnect();
        void ToggleConnect();
    }
}
