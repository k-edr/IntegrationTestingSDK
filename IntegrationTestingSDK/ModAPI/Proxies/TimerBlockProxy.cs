using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class TimerBlockProxy : FunctionalBlockProxy, IMyTimerBlock
    {
        public bool Silent
        {
            get => bool.TryParse(GetProperty("Silent"), out var v) && v;
            set => SetProperty("Silent", value.ToString().ToLowerInvariant());
        }

        public void Trigger() => ExecuteAction("Trigger");
        public void StartCountdown() => ExecuteAction("StartCountdown");
        public void StopCountdown() => ExecuteAction("StopCountdown");

        internal TimerBlockProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }
    }
}
