using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Enums;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class LandingGearProxy : FunctionalBlockProxy, IMyLandingGear
    {
        public bool IsLocked
            => bool.TryParse(GetProperty("IsLocked"), out var v) && v;

        public LandingGearMode LockMode
        {
            get => System.Enum.TryParse<LandingGearMode>(GetProperty("LockMode"), out var e) ? e : default;
            set => SetProperty("LockMode", value.ToString());
        }

        public void Lock() => ExecuteAction("Lock");
        public void Unlock() => ExecuteAction("Unlock");
        public void ToggleLock() => ExecuteAction("ToggleLock");

        internal LandingGearProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }
    }
}
