using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;
using IntegrationTestingSDK.ModAPI.Types;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class HeatVentProxy : FunctionalBlockProxy, IMyHeatVent
    {
        public float PowerDependency
        {
            get => float.TryParse(GetProperty("PowerDependency"), out var v) ? v : 0f;
            set => SetProperty("PowerDependency", value.ToString("G"));
        }

        public Color ColorMinimal
        {
            get => Color.TryParse(GetProperty("ColorMin"), out var c) ? c : Color.Black;
            set => SetProperty("ColorMin", value.ToPackedString());
        }

        public Color ColorMaximal
        {
            get => Color.TryParse(GetProperty("ColorMaximal"), out var c) ? c : Color.White;
            set => SetProperty("ColorMaximal", value.ToPackedString());
        }

        public Color ColorCurrent
            => Color.TryParse(GetProperty("ColorCurrent"), out var c) ? c : Color.Black;

        internal HeatVentProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }
    }
}
