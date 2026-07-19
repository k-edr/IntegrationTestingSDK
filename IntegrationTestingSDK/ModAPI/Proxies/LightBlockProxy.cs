using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;
using IntegrationTestingSDK.ModAPI.Types;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class LightBlockProxy : FunctionalBlockProxy, IMyLightingBlock
    {
        public Color Color
        {
            get
            {
                var raw = GetProperty("Color");
                return Color.TryParse(raw, out var c) ? c : Color.White;
            }
            set => SetProperty("Color", value.ToPackedString());
        }

        public float Radius
        {
            get => float.TryParse(GetProperty("Radius"), out var v) ? v : 0f;
            set => SetProperty("Radius", value.ToString());
        }

        public float Intensity
        {
            get => float.TryParse(GetProperty("Intensity"), out var v) ? v : 5f;
            set => SetProperty("Intensity", value.ToString());
        }

        public float Falloff
        {
            get => float.TryParse(GetProperty("Falloff"), out var v) ? v : 1f;
            set => SetProperty("Falloff", value.ToString());
        }

        public float BlinkIntervalSeconds
        {
            get => float.TryParse(GetProperty("Blink Interval"), out var v) ? v : 0f;
            set => SetProperty("Blink Interval", value.ToString());
        }

        public float BlinkLength
        {
            get => float.TryParse(GetProperty("Blink Lenght"), out var v) ? v : 50f;
            set => SetProperty("Blink Lenght", value.ToString());
        }

        public float BlinkOffset
        {
            get => float.TryParse(GetProperty("Blink Offset"), out var v) ? v : 0f;
            set => SetProperty("Blink Offset", value.ToString());
        }

        internal LightBlockProxy(IPbTestHarness harness, long gridId, BlockDto block) : base(harness, gridId, block) { }
    }
}
