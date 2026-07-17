using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class WarheadProxy : TerminalBlockProxy, IMyWarhead
    {
        public bool IsCountingDown
            => bool.TryParse(GetProperty("IsCountingDown"), out var v) && v;

        public float DetonationTime
        {
            get => float.TryParse(GetProperty("DetonationTime"), out var v) ? v : 0f;
            set => SetProperty("DetonationTime", value.ToString("G"));
        }

        public bool IsArmed
        {
            get => bool.TryParse(GetProperty("IsArmed"), out var v) && v;
            set => SetProperty("IsArmed", value.ToString().ToLowerInvariant());
        }

        public void StartCountdown() => ExecuteAction("StartCountdown");
        public void StopCountdown() => ExecuteAction("StopCountdown");
        public void Detonate() => ExecuteAction("Detonate");

        internal WarheadProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }
    }
}
