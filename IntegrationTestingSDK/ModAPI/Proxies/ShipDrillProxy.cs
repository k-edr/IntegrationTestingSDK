using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class ShipDrillProxy : FunctionalBlockProxy, IMyShipDrill
    {
        public bool TerrainClearingMode
        {
            get => bool.TryParse(GetProperty("TerrainClearingMode"), out var v) && v;
            set => SetProperty("TerrainClearingMode", value.ToString().ToLowerInvariant());
        }

        internal ShipDrillProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }
    }
}
