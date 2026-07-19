using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class GyroProxy : FunctionalBlockProxy, IMyGyro
    {
        public bool GyroOverride
        {
            get => bool.TryParse(GetProperty("Override"), out var v) && v;
            set => SetProperty("Override", value.ToString());
        }

        public float GyroPower
        {
            get => float.TryParse(GetProperty("Power"), out var v) ? v : 1f;
            set => SetProperty("Power", value.ToString());
        }

        public float Yaw
        {
            get => float.TryParse(GetProperty("Yaw"), out var v) ? v : 0f;
            set => SetProperty("Yaw", value.ToString());
        }

        public float Pitch
        {
            get => float.TryParse(GetProperty("Pitch"), out var v) ? v : 0f;
            set => SetProperty("Pitch", value.ToString());
        }

        public float Roll
        {
            get => float.TryParse(GetProperty("Roll"), out var v) ? v : 0f;
            set => SetProperty("Roll", value.ToString());
        }

        internal GyroProxy(IPbTestHarness harness, long gridId, BlockDto block) : base(harness, gridId, block) { }
    }
}
