using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;
using IntegrationTestingSDK.ModAPI.Types;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class ProjectorProxy : FunctionalBlockProxy, IMyProjector
    {
        public bool IsProjecting
        {
            get => bool.TryParse(GetProperty("IsProjecting"), out var v) && v;
        }

        public int TotalBlocks
        {
            get => int.TryParse(GetProperty("TotalBlocks"), out var v) ? v : 0;
        }

        public int RemainingBlocks
        {
            get => int.TryParse(GetProperty("RemainingBlocks"), out var v) ? v : 0;
        }

        public int BuildableBlocksCount
        {
            get => int.TryParse(GetProperty("BuildableBlocksCount"), out var v) ? v : 0;
        }

        public Vector3I ProjectionOffset
        {
            get
            {
                var raw = GetProperty("ProjectionOffset");
                if (string.IsNullOrEmpty(raw)) return Vector3I.Zero;
                return ParseVector3I(raw);
            }
            set => SetProperty("ProjectionOffset", $"({value.X}, {value.Y}, {value.Z})");
        }

        public Vector3I ProjectionRotation
        {
            get
            {
                var raw = GetProperty("ProjectionRotation");
                if (string.IsNullOrEmpty(raw)) return Vector3I.Zero;
                return ParseVector3I(raw);
            }
            set => SetProperty("ProjectionRotation", $"({value.X}, {value.Y}, {value.Z})");
        }

        public bool ShowOnlyBuildable
        {
            get => bool.TryParse(GetProperty("ShowOnlyBuildable"), out var v) && v;
            set => SetProperty("ShowOnlyBuildable", value.ToString());
        }

        public void UpdateOffsetAndRotation() => ExecuteAction("UpdateOffsetAndRotation");

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

        internal ProjectorProxy(IPbTestHarness harness, long gridId, BlockDto block) : base(harness, gridId, block) { }
    }
}
