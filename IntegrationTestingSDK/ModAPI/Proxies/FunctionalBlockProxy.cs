using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    /// <summary>
    ///     Base class for functional block proxies. Implements <see cref="IMyFunctionalBlock"/>
    ///     which adds Enabled state on top of terminal block properties.
    /// </summary>
    internal class FunctionalBlockProxy : TerminalBlockProxy, IMyFunctionalBlock
    {
        internal FunctionalBlockProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }

        public bool Enabled
        {
            get => bool.TryParse(GetProperty("OnOff"), out var v) && v;
            set => ExecuteAction(value ? "OnOff_On" : "OnOff_Off");
        }
    }
}
