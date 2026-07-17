using System.Collections.Generic;
using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class UpgradableBlockProxy : TerminalBlockProxy, IMyUpgradableBlock
    {
        public uint UpgradeCount
            => uint.TryParse(GetProperty("UpgradeCount"), out var v) ? v : 0u;

        public void GetUpgrades(Dictionary<string, float> upgrades)
        {
            // Placeholder — no-op
        }

        internal UpgradableBlockProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }
    }
}
