using IntegrationTestingSDK.ModAPI.Types;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Cube block interface mirroring Sandbox.ModAPI.Ingame.IMyCubeBlock.
    /// </summary>
    public interface IMyCubeBlock : IMyEntity
    {
        IMyCubeGrid CubeGrid { get; }
        Vector3I Min { get; }
        Vector3I Max { get; }
        string DefinitionDisplayNameText { get; }
        bool IsWorking { get; }
        bool IsFunctional { get; }
        float Mass { get; }
        int NumberInGrid { get; }
        string BlockDefinition { get; }
    }
}
