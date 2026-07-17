using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class ButtonPanelProxy : TerminalBlockProxy, IMyButtonPanel
    {
        public string GetButtonName(int index)
            => GetProperty($"ButtonName_{index}");

        public void SetCustomButtonName(int index, string name)
            => SetProperty($"ButtonName_{index}", name);

        internal ButtonPanelProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }
    }
}
