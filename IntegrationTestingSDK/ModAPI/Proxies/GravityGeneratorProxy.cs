using System;
using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;
using IntegrationTestingSDK.ModAPI.Types;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class GravityGeneratorProxy : FunctionalBlockProxy, IMyGravityGenerator
    {
        public Vector3 GravityAcceleration
        {
            get
            {
                // SE stores Gravity as scalar magnitude; assume Y-axis (downward) acceleration
                var raw = GetProperty("Gravity");
                if (float.TryParse(raw, out var g))
                    return new Vector3(0f, -g, 0f);
                return Vector3.Zero;
            }
            set => SetProperty("Gravity",
                FormattableString.Invariant($"{Math.Abs(value.Length())}"));
        }

        public Vector3 FieldSize
        {
            get
            {
                var raw = GetProperty("FieldSize");
                var parts = raw?.Split(' ');
                if (parts?.Length == 3
                    && float.TryParse(parts[0], out var x)
                    && float.TryParse(parts[1], out var y)
                    && float.TryParse(parts[2], out var z))
                {
                    return new Vector3(x, y, z);
                }
                return Vector3.Zero;
            }
        }

        internal GravityGeneratorProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }
    }
}
