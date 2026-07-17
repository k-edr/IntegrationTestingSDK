using System.Collections.Generic;
using IntegrationTestingSDK.ModAPI.Enums;
using IntegrationTestingSDK.ModAPI.Types;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for conveyor sorter blocks.
    /// </summary>
    public interface IMyConveyorSorter : IMyFunctionalBlock
    {
        bool DrainAll { get; set; }
        MyConveyorSorterMode Mode { get; }

        void AddItem(MyDefinitionId id);
        void RemoveItem(MyDefinitionId id);
        bool IsAllowed(MyDefinitionId id);
        List<MyDefinitionId> GetFilterList();
    }
}
