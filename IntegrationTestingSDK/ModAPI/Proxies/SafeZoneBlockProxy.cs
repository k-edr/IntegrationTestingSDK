using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class SafeZoneBlockProxy : FunctionalBlockProxy, IMySafeZoneBlock
    {
        public bool IsSafeZoneEnabled
            => bool.TryParse(GetProperty("IsSafeZoneEnabled"), out var v) && v;

        public void EnableSafeZone(bool enable) => ExecuteAction("EnableSafeZone");

        internal SafeZoneBlockProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }
    }
}
