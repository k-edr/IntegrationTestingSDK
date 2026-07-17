using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class BeaconProxy : FunctionalBlockProxy, IMyBeacon
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

        internal BeaconProxy(IPbTestHarness harness, long gridId, BlockDto block) : base(harness, gridId, block) { }
    }
}
