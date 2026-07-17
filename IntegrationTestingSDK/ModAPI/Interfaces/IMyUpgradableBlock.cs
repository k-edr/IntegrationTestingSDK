using System.Collections.Generic;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for upgradable blocks.
    ///     Inherits from IMyTerminalBlock directly.
    /// </summary>
    public interface IMyUpgradableBlock : IMyTerminalBlock
    {
        uint UpgradeCount { get; }

        void GetUpgrades(Dictionary<string, float> upgrades);
    }
}
