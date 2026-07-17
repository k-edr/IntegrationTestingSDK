using IntegrationTestingSDK.ModAPI.Enums;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Ship merge block interface. Mirrors Sandbox.ModAPI.Ingame.IMyShipMergeBlock.
    /// </summary>
    public interface IMyShipMergeBlock : IMyFunctionalBlock
    {
        IMyShipMergeBlock Other { get; }
        bool IsConnected { get; }
        MergeState State { get; }
    }
}
