using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Enums;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class LaserAntennaProxy : FunctionalBlockProxy, IMyLaserAntenna
    {
        public bool IsPermanent
        {
            // NOTE: isPerm terminal property cannot be written via SetProperty or ExecuteAction;
            // the game requires the ModAPI method SetTargetCoords + permanent flag, which HTTP can't reach.
            get => bool.TryParse(GetProperty("isPerm"), out var v) && v;
            set { /* Not supported via terminal system */ }
        }

        public float Range
        {
            get => float.TryParse(GetProperty("Range"), out var v) ? v : 0f;
            set => SetProperty("Range", value.ToString("G"));
        }

        public bool IsOutsideLimits
            => bool.TryParse(GetProperty("IsOutsideLimits"), out var v) && v;

        public MyLaserAntennaStatus Status
            => System.Enum.TryParse<MyLaserAntennaStatus>(GetProperty("Status"), out var e) ? e : default;

        public void Connect() => ExecuteAction("Connect");

        public void SetTargetCoords(string coords)
            => SetProperty("TargetCoords", coords);

        internal LaserAntennaProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }
    }
}
