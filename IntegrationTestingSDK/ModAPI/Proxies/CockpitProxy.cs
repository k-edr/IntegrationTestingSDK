using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;
using IntegrationTestingSDK.ModAPI.Enums;
using IntegrationTestingSDK.ModAPI.Types;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class CockpitProxy : FunctionalBlockProxy, IMyShipController, IMyCockpit
    {
        internal CockpitProxy(IPbTestHarness harness, long gridId, BlockDto block) : base(harness, gridId, block) { }

        // ── IMyShipController ──────────────────────────────────

        public bool CanControlShip
        {
            get => bool.TryParse(GetProperty("CanControlShip"), out var v) && v;
        }

        public bool IsUnderControl
        {
            get => bool.TryParse(GetProperty("IsUnderControl"), out var v) && v;
        }

        public bool ControlThrusters
        {
            get => bool.TryParse(GetProperty("ControlThrusters"), out var v) && v;
            set => SetProperty("ControlThrusters", value.ToString());
        }

        public bool ControlWheels
        {
            get => bool.TryParse(GetProperty("ControlWheels"), out var v) && v;
            set => SetProperty("ControlWheels", value.ToString());
        }

        public bool HandBrake
        {
            get => bool.TryParse(GetProperty("HandBrake"), out var v) && v;
            set => SetProperty("HandBrake", value.ToString());
        }

        public bool DampenersOverride
        {
            get => bool.TryParse(GetProperty("DampenersOverride"), out var v) && v;
            set => SetProperty("DampenersOverride", value.ToString());
        }

        public bool ShowHorizonIndicator
        {
            get => bool.TryParse(GetProperty("ShowHorizonIndicator"), out var v) && v;
            set => SetProperty("ShowHorizonIndicator", value.ToString());
        }

        public bool IsMainCockpit
        {
            get => bool.TryParse(GetProperty("MainCockpit"), out var v) && v;
        }

        public Vector3 MoveIndicator
        {
            get
            {
                var raw = GetProperty("MoveIndicator");
                return TryParseVector3(raw, out var v) ? v : Vector3.Zero;
            }
        }

        public Vector2 RotationIndicator
        {
            get
            {
                var raw = GetProperty("RotationIndicator");
                return TryParseVector2(raw, out var v) ? v : Vector2.Zero;
            }
        }

        public float RollIndicator
        {
            get => float.TryParse(GetProperty("RollIndicator"), out var v) ? v : 0f;
        }

        public Vector3D CenterOfMass
        {
            get
            {
                var raw = GetProperty("CenterOfMass");
                return TryParseVector3D(raw, out var v) ? v : Vector3D.Zero;
            }
        }

        public Vector3 GetNaturalGravity() => Vector3.Zero;
        public Vector3 GetArtificialGravity() => Vector3.Zero;
        public Vector3 GetTotalGravity() => Vector3.Zero;
        public double GetShipSpeed() => 0.0;
        public MyShipVelocities GetShipVelocities() => new MyShipVelocities();

        public float CalculateShipMass() => 0f;

        public bool TryGetPlanetPosition(out Vector3D position)
        {
            position = Vector3D.Zero;
            return false;
        }

        public bool TryGetPlanetElevation(MyPlanetElevation detail, out double elevation)
        {
            elevation = 0.0;
            return false;
        }

        // ── IMyCockpit ─────────────────────────────────────────

        public float OxygenCapacity
        {
            get => float.TryParse(GetProperty("OxygenCapacity"), out var v) ? v : 0f;
        }

        public float OxygenFilledRatio
        {
            get => float.TryParse(GetProperty("OxygenFilledRatio"), out var v) ? v : 0f;
        }

        // ── Helpers ────────────────────────────────────────────

        private static bool TryParseVector3(string raw, out Vector3 result)
        {
            result = Vector3.Zero;
            if (string.IsNullOrEmpty(raw)) return false;
            var cleaned = raw.Replace("(", "").Replace(")", "").Replace(" ", "");
            var parts = cleaned.Split(',');
            if (parts.Length != 3) return false;
            if (!float.TryParse(parts[0], out var x)) return false;
            if (!float.TryParse(parts[1], out var y)) return false;
            if (!float.TryParse(parts[2], out var z)) return false;
            result = new Vector3(x, y, z);
            return true;
        }

        private static bool TryParseVector2(string raw, out Vector2 result)
        {
            result = Vector2.Zero;
            if (string.IsNullOrEmpty(raw)) return false;
            var cleaned = raw.Replace("(", "").Replace(")", "").Replace(" ", "");
            var parts = cleaned.Split(',');
            if (parts.Length != 2) return false;
            if (!float.TryParse(parts[0], out var x)) return false;
            if (!float.TryParse(parts[1], out var y)) return false;
            result = new Vector2(x, y);
            return true;
        }

        private static bool TryParseVector3D(string raw, out Vector3D result)
        {
            result = Vector3D.Zero;
            if (string.IsNullOrEmpty(raw)) return false;
            var cleaned = raw.Replace("(", "").Replace(")", "").Replace(" ", "");
            var parts = cleaned.Split(',');
            if (parts.Length != 3) return false;
            if (!double.TryParse(parts[0], out var x)) return false;
            if (!double.TryParse(parts[1], out var y)) return false;
            if (!double.TryParse(parts[2], out var z)) return false;
            result = new Vector3D(x, y, z);
            return true;
        }
    }
}
