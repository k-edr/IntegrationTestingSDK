using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class UpgradeModuleProxy : FunctionalBlockProxy, IMyUpgradeModule
    {
        public uint Connections
            => uint.TryParse(GetProperty("Connections"), out var v) ? v : 0u;

        internal UpgradeModuleProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }
    }
}
