using System;
using System.Collections.Generic;

namespace IntegrationTestingSDK.Contracts
{
    /// <summary>
    ///     Contract for running PB scripts and interacting with blocks
    ///     in the real game. Block-level methods use direct terminal actions,
    ///     not script commands.
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
        ///     Returns typed wrappers with block positions pre-resolved.
        /// </summary>
        IReadOnlyList<SpawnedGrid> SpawnTestGrid(string blueprintName, double x, double y, double z);

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
        string RunScript(long gridId, string argument = null);

        /// <summary>
        ///     Read the current content of the LCD panel on the grid.
        /// </summary>
        string GetLcdContent(long gridId);

        /// <summary>
        ///     Read the Enabled state of every block on the grid.
        /// </summary>
        IReadOnlyList<BlockState> GetBlockStates(long gridId);

        // ── Block-level API (direct terminal actions) ────────────

        /// <summary>
        ///     Execute a terminal action on a block (e.g. "OnOff_Off").
        /// </summary>
        bool ExecuteBlockAction(long gridId, int x, int y, int z, string actionId);

        /// <summary>
        ///     Get a terminal property value (e.g. "Color").
        /// </summary>
        string GetBlockProperty(long gridId, int x, int y, int z, string propertyId);

        /// <summary>
        ///     Set a terminal property value.
        /// </summary>
        bool SetBlockProperty(long gridId, int x, int y, int z, string propertyId, string value);
    }
}
