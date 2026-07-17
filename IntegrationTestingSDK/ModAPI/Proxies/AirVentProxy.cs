using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Enums;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class AirVentProxy : FunctionalBlockProxy, IMyAirVent
    {
        public bool Depressurize
        {
            get => bool.TryParse(GetProperty("Depressurize"), out var v) && v;
            set => SetProperty("Depressurize", value.ToString().ToLowerInvariant());
        }

        public VentStatus Status
            => System.Enum.TryParse<VentStatus>(GetProperty("Status"), out var e) ? e : default;

        public bool IsPressurized()
            => bool.TryParse(GetProperty("IsPressurized"), out var v) && v;

        public float GetOxygenLevel()
            => float.TryParse(GetProperty("OxygenLevel"), out var v) ? v : 0f;

        internal AirVentProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }
    }
}
