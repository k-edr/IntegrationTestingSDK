using System;
using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;
using IntegrationTestingSDK.ModAPI.Enums;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class ShipConnectorProxy : FunctionalBlockProxy, IMyShipConnector
    {
        public IMyShipConnector OtherConnector => null;

        public bool IsLocked
        {
            get => bool.TryParse(GetProperty("IsLocked"), out var v) && v;
        }

        public bool IsConnected
        {
            get => bool.TryParse(GetProperty("IsConnected"), out var v) && v;
        }

        public MyShipConnectorStatus Status
        {
            get
            {
                var raw = GetProperty("Status");
                if (Enum.TryParse<MyShipConnectorStatus>(raw, out var s)) return s;
                return MyShipConnectorStatus.Unconnected;
            }
        }

        public bool ThrowOut
        {
            get => bool.TryParse(GetProperty("ThrowOut"), out var v) && v;
            set => SetProperty("ThrowOut", value.ToString());
        }

        public bool CollectAll
        {
            get => bool.TryParse(GetProperty("CollectAll"), out var v) && v;
            set => SetProperty("CollectAll", value.ToString());
        }

        public float PullStrength
        {
            get => float.TryParse(GetProperty("Strength"), out var v) ? v : 0f;
            set => SetProperty("Strength", value.ToString());
        }

        public bool IsParkingEnabled
        {
            get => bool.TryParse(GetProperty("IsParkingEnabled"), out var v) && v;
            set => SetProperty("IsParkingEnabled", value.ToString());
        }

        public void Connect() => ExecuteAction("Connect");
        public void Disconnect() => ExecuteAction("Disconnect");
        public void ToggleConnect() => ExecuteAction("ToggleConnect");

        internal ShipConnectorProxy(IPbTestHarness harness, long gridId, BlockDto block) : base(harness, gridId, block) { }
    }
}
