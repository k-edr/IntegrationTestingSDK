using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class GravityGeneratorSphereProxy : FunctionalBlockProxy, IMyGravityGeneratorSphere
    {
        public float Radius
        {
            get => float.TryParse(GetProperty("Radius"), out var v) ? v : 0f;
            set => SetProperty("Radius", value.ToString("G"));
        }

        public float GravityAcceleration
        {
            get => float.TryParse(GetProperty("GravityAcceleration"), out var v) ? v : 0f;
            set => SetProperty("GravityAcceleration", value.ToString("G"));
        }

        internal GravityGeneratorSphereProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }
    }
}
