using System.Collections.Generic;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Block group interface mirroring Sandbox.ModAPI.Ingame.IMyBlockGroup.
    /// </summary>
    public interface IMyBlockGroup
    {
        string Name { get; }
        void GetBlocks(List<IMyTerminalBlock> blocks);
    }
}
