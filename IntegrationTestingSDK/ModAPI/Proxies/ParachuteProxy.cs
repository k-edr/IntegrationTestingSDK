using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class ParachuteProxy : FunctionalBlockProxy, IMyParachute
    {
        public bool AutoDeploy
        {
            get => bool.TryParse(GetProperty("AutoDeploy"), out var v) && v;
            set => SetProperty("AutoDeploy", value.ToString().ToLowerInvariant());
        }

        public float AutoDeployHeight
        {
            get => float.TryParse(GetProperty("AutoDeployHeight"), out var v) ? v : 0f;
            set => SetProperty("AutoDeployHeight", value.ToString("G"));
        }

        public void OpenDoor() => ExecuteAction("OpenDoor");
        public void CloseDoor() => ExecuteAction("CloseDoor");
        public void ToggleDoor() => ExecuteAction("ToggleDoor");

        internal ParachuteProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }
    }
}
