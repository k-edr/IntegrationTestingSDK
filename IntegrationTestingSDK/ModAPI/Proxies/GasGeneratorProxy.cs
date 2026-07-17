using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class GasGeneratorProxy : FunctionalBlockProxy, IMyGasGenerator
    {
        public bool AutoRefill
        {
            get => bool.TryParse(GetProperty("AutoRefill"), out var v) && v;
            set => SetProperty("AutoRefill", value.ToString().ToLowerInvariant());
        }

        public bool IsProducing
            => bool.TryParse(GetProperty("IsProducing"), out var v) && v;

        public bool UseConveyorSystem
        {
            get => bool.TryParse(GetProperty("UseConveyorSystem"), out var v) && v;
            set => SetProperty("UseConveyorSystem", value.ToString().ToLowerInvariant());
        }

        internal GasGeneratorProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }
    }
}
