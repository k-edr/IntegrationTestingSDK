using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class GasGeneratorProxy : FunctionalBlockProxy, IMyGasGenerator
    {
        public bool AutoRefill
        {
            get => bool.TryParse(GetProperty("Auto-Refill"), out var v) && v;
            set => SetProperty("Auto-Refill", value.ToString().ToLowerInvariant());
        }

        public bool IsProducing
            => bool.TryParse(GetProperty("IsProducing"), out var v) && v;

        public bool UseConveyorSystem
        {
            get => bool.TryParse(GetProperty("UseConveyor"), out var v) && v;
            set => SetProperty("UseConveyor", value.ToString().ToLowerInvariant());
        }

        internal GasGeneratorProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }
    }
}
