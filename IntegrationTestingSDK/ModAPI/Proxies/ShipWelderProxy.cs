using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class ShipWelderProxy : FunctionalBlockProxy, IMyShipWelder
    {
        public bool HelpOthers
        {
            get => bool.TryParse(GetProperty("HelpOthers"), out var v) && v;
            set => SetProperty("HelpOthers", value.ToString().ToLowerInvariant());
        }

        internal ShipWelderProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }
    }
}
