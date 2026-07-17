using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class GasTankProxy : FunctionalBlockProxy, IMyGasTank
    {
        public double FilledRatio
            => double.TryParse(GetProperty("FilledRatio"), out var v) ? v : 0.0;

        public float Capacity
            => float.TryParse(GetProperty("Capacity"), out var v) ? v : 0f;

        public bool Stockpile
        {
            get => bool.TryParse(GetProperty("Stockpile"), out var v) && v;
            set => SetProperty("Stockpile", value.ToString().ToLowerInvariant());
        }

        public bool AutoRefillBottles
        {
            get => bool.TryParse(GetProperty("AutoRefillBottles"), out var v) && v;
            set => SetProperty("AutoRefillBottles", value.ToString().ToLowerInvariant());
        }

        internal GasTankProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }
    }
}
