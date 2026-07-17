using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class ProgrammableBlockProxy : FunctionalBlockProxy, IMyProgrammableBlock
    {
        public bool IsRunning
        {
            get => bool.TryParse(GetProperty("IsRunning"), out var v) && v;
        }

        public string TerminalRunArgument
        {
            get => GetProperty("TerminalRunArgument") ?? string.Empty;
        }

        public bool TryRun(string argument)
        {
            ExecuteAction("Run");
            return true;
        }

        internal ProgrammableBlockProxy(IPbTestHarness harness, long gridId, BlockDto block) : base(harness, gridId, block) { }
    }
}
