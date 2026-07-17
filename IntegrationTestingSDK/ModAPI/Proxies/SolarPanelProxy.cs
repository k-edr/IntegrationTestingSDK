using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class SolarPanelProxy : FunctionalBlockProxy, IMySolarPanel
    {
        public float MaxOutput
            => float.TryParse(GetProperty("MaxOutput"), out var v) ? v : 0f;

        public float CurrentOutput
            => float.TryParse(GetProperty("CurrentOutput"), out var v) ? v : 0f;

        internal SolarPanelProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }
    }
}
