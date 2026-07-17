using System;
using System.Collections.Generic;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Grid terminal system interface mirroring Sandbox.ModAPI.Ingame.IMyGridTerminalSystem.
    /// </summary>
    public interface IMyGridTerminalSystem
    {
        void GetBlocks(List<IMyTerminalBlock> blocks);
        void GetBlockGroups(List<IMyBlockGroup> groups);
        void GetBlocksOfType<T>(List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect = null);
        void SearchBlocksOfName(string name, List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect = null);
        IMyTerminalBlock GetBlockWithName(string name);
        IMyBlockGroup GetBlockGroupWithName(string name);
    }
}
