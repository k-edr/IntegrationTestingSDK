using IntegrationTestingSDK.Contracts;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    /// <summary>
    ///     All terminal blocks are upgradable. Core implementation is in
    ///     <see cref="TerminalBlockProxy"/>.
    /// </summary>
    internal class UpgradableBlockProxy : TerminalBlockProxy
    {
        internal UpgradableBlockProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }
    }
}
