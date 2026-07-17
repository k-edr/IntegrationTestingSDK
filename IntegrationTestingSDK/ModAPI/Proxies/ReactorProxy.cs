using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class ReactorProxy : FunctionalBlockProxy, IMyReactor
    {
        public bool UseConveyorSystem
        {
            get => bool.TryParse(GetProperty("UseConveyorSystem"), out var v) && v;
            set => SetProperty("UseConveyorSystem", value.ToString());
        }

        internal ReactorProxy(IPbTestHarness harness, long gridId, BlockDto block) : base(harness, gridId, block) { }
    }
}
