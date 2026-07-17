using IntegrationTestingSDK.ModAPI.Types;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for projector blocks.
    /// </summary>
    public interface IMyProjector : IMyFunctionalBlock
    {
        bool IsProjecting { get; }
        int TotalBlocks { get; }
        int RemainingBlocks { get; }
        int BuildableBlocksCount { get; }
        Vector3I ProjectionOffset { get; set; }
        Vector3I ProjectionRotation { get; set; }
        bool ShowOnlyBuildable { get; set; }

        void UpdateOffsetAndRotation();
    }
}
