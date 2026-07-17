using System.Collections.Generic;
using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Enums;
using IntegrationTestingSDK.ModAPI.Interfaces;
using IntegrationTestingSDK.ModAPI.Types;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class ConveyorSorterProxy : FunctionalBlockProxy, IMyConveyorSorter
    {
        public bool DrainAll
        {
            get => bool.TryParse(GetProperty("DrainAll"), out var v) && v;
            set => SetProperty("DrainAll", value.ToString().ToLowerInvariant());
        }

        public MyConveyorSorterMode Mode
            => System.Enum.TryParse<MyConveyorSorterMode>(GetProperty("Mode"), out var e) ? e : default;

        public void AddItem(MyDefinitionId id) { }
        public void RemoveItem(MyDefinitionId id) { }
        public bool IsAllowed(MyDefinitionId id) => false;

        public List<MyDefinitionId> GetFilterList()
            => new List<MyDefinitionId>();

        internal ConveyorSorterProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }
    }
}
