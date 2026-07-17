using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;
using IntegrationTestingSDK.ModAPI.Types;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class ThrustProxy : FunctionalBlockProxy, IMyThrust
    {
        public float ThrustOverride
        {
            get => float.TryParse(GetProperty("ThrustOverride"), out var v) ? v : 0f;
            set => SetProperty("ThrustOverride", value.ToString());
        }

        public float ThrustOverridePercentage
        {
            get => float.TryParse(GetProperty("ThrustOverridePercentage"), out var v) ? v : 0f;
            set => SetProperty("ThrustOverridePercentage", value.ToString());
        }

        public float MaxThrust
        {
            get => float.TryParse(GetProperty("MaxThrust"), out var v) ? v : 0f;
        }

        public float MaxEffectiveThrust
        {
            get => float.TryParse(GetProperty("MaxEffectiveThrust"), out var v) ? v : 0f;
        }

        public float CurrentThrust
        {
            get => float.TryParse(GetProperty("CurrentThrust"), out var v) ? v : 0f;
        }

        public float CurrentThrustPercentage
        {
            get => float.TryParse(GetProperty("CurrentThrustPercentage"), out var v) ? v : 0f;
        }

        public Vector3I GridThrustDirection
        {
            get
            {
                var raw = GetProperty("GridThrustDirection");
                if (string.IsNullOrEmpty(raw)) return Vector3I.Zero;
                // Expected format: "(X, Y, Z)" or "X, Y, Z"
                return ParseVector3I(raw);
            }
        }

        private static Vector3I ParseVector3I(string raw)
        {
            var cleaned = raw.Replace("(", "").Replace(")", "").Replace(" ", "");
            var parts = cleaned.Split(',');
            if (parts.Length == 3 &&
                int.TryParse(parts[0], out var x) &&
                int.TryParse(parts[1], out var y) &&
                int.TryParse(parts[2], out var z))
                return new Vector3I(x, y, z);
            return Vector3I.Zero;
        }

        internal ThrustProxy(IPbTestHarness harness, long gridId, BlockDto block) : base(harness, gridId, block) { }
    }
}
