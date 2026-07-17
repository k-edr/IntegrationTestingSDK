using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Enums;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class ShipMergeBlockProxy : FunctionalBlockProxy, IMyShipMergeBlock
    {
        public IMyShipMergeBlock Other => null;

        public bool IsConnected
            => bool.TryParse(GetProperty("IsConnected"), out var v) && v;

        public MergeState State
            => System.Enum.TryParse<MergeState>(GetProperty("State"), out var e) ? e : default;

        internal ShipMergeBlockProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }
    }
}
