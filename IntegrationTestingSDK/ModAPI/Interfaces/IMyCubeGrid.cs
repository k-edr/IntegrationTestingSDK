using IntegrationTestingSDK.ModAPI.Types;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Cube grid interface mirroring Sandbox.ModAPI.Ingame.IMyCubeGrid.
    /// </summary>
    public interface IMyCubeGrid : IMyEntity
    {
        IMyGridTerminalSystem GridTerminalSystem { get; }
        bool IsStatic { get; }
        MyCubeSize GridSizeEnum { get; }
        string CustomName { get; }
        int BlocksCount { get; }
    }
}
