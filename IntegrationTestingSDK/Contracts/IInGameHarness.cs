using System;
using System.Collections.Generic;

namespace IntegrationTestingSDK.Contracts
{
    /// <summary>
    ///     Contract for in-game operations that run on the game's main thread.
    ///     Implemented by the Plugin's <c>PbTestService</c>.
    ///     Exposed via <c>SessionComponent.Harness</c> after world load.
    /// </summary>
    public interface IInGameHarness : IDisposable
    {
        /// <summary>Whether the game session is ready (world loaded).</summary>
        bool IsReady { get; }

        /// <summary>Spawn a grid from blueprint at position. Returns entity IDs.</summary>
        IReadOnlyList<long> SpawnTestGrid(string blueprintName, double x, double y, double z);

        /// <summary>Delete a grid by entity ID.</summary>
        void RemoveTestGrid(long gridId);

        /// <summary>Set the PB script code on a grid.</summary>
        void SetScriptCode(long gridId, string code);

        /// <summary>Run the PB script and return captured output.</summary>
        string RunScript(long gridId, string argument = null);

        /// <summary>Read the LCD panel text on a grid.</summary>
        string GetLcdContent(long gridId);
    }
}
