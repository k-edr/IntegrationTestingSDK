using System;
using System.Collections.Generic;

namespace IntegrationTestingSDK.Contracts
{
    /// <summary>
    ///     Contract for running PB scripts in the real game.
    ///     Operates on native game entity IDs (<c>long EntityId</c>) —
    ///     the primary key of every in-game entity.
    ///     Name-based lookup is secondary.
    /// </summary>
    public interface IPbTestHarness : IDisposable
    {
        /// <summary>
        ///     Copy template → rename → launch game → wait ready.
        /// </summary>
        void StartWorld(string templateWorldName, string workingCopyName);

        /// <summary>
        ///     Close game → delete world copy.
        /// </summary>
        void StopWorld();

        /// <summary>
        ///     Spawn a grid from a blueprint at the given position.
        ///     Returns the entity IDs of all spawned grids.
        /// </summary>
        IReadOnlyList<long> SpawnTestGrid(string blueprintName, double x, double y, double z);

        /// <summary>
        ///     Remove the spawned grid by its entity ID.
        /// </summary>
        void RemoveTestGrid(long gridId);

        /// <summary>
        ///     Upload the compiled script to the Programmable Block
        ///     on the grid identified by <paramref name="gridId"/>.
        /// </summary>
        void UploadScript(long gridId);

        /// <summary>
        ///     Compile and run the script on the grid's PB.
        /// </summary>
        /// <param name="gridId">Target grid entity ID.</param>
        /// <param name="argument">Argument passed to <c>Main</c>. Null for no argument.</param>
        /// <returns>The captured Echo output from the PB.</returns>
        string RunScript(long gridId, string argument = null);

        /// <summary>
        ///     Read the current content of the LCD panel on the grid.
        /// </summary>
        string GetLcdContent(long gridId);

        /// <summary>
        ///     Read the Enabled state of every block on the grid.
        /// </summary>
        IReadOnlyList<BlockState> GetBlockStates(long gridId);
    }
}
