using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class RadioAntennaProxy : FunctionalBlockProxy, IMyRadioAntenna
    {
        public float Radius
        {
            get => float.TryParse(GetProperty("Radius"), out var v) ? v : 0f;
            set => SetProperty("Radius", value.ToString());
        }

        public string HudText
        {
            get => GetProperty("HudText") ?? string.Empty;
            set => SetProperty("HudText", value);
        }

        public bool ShowShipName
        {
            get => bool.TryParse(GetProperty("ShowShipName"), out var v) && v;
            set => SetProperty("ShowShipName", value.ToString());
        }

        public bool IsBroadcasting
        {
            get => bool.TryParse(GetProperty("IsBroadcasting"), out var v) && v;
        }

        public bool EnableBroadcasting
        {
            get => bool.TryParse(GetProperty("EnableBroadcasting"), out var v) && v;
            set => SetProperty("EnableBroadcasting", value.ToString());
        }

        internal RadioAntennaProxy(IPbTestHarness harness, long gridId, BlockDto block) : base(harness, gridId, block) { }
    }
}
