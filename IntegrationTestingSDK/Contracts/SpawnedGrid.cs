using System;
using System.Collections.Generic;
using System.Linq;

namespace IntegrationTestingSDK.Contracts
{
    /// <summary>
    ///     Typed wrapper around a spawned grid.
    ///     Provides name-based block lookup and high-level operations
    ///     (turn on/off, set color) that delegate to the harness block API.
    /// </summary>
    public class SpawnedGrid
    {
        private readonly IPbTestHarness _harness;
        private readonly Dictionary<string, BlockDto> _blocksByName;

        /// <summary>Game entity ID of the grid.</summary>
        public long Id { get; }

        /// <summary>All blocks keyed by blueprint name (case-insensitive).</summary>
        public IReadOnlyDictionary<string, BlockDto> Blocks => _blocksByName;

        internal SpawnedGrid(long id, IEnumerable<BlockDto> blocks, IPbTestHarness harness)
        {
            Id = id;
            _harness = harness;
            _blocksByName = new Dictionary<string, BlockDto>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var b in blocks)
            {
                if (!string.IsNullOrEmpty(b.Name))
                    _blocksByName[b.Name] = b;
            }
        }

        // ── Block lookup ───────────────────────────────────────────

        /// <summary>Find a block by name (case-insensitive).</summary>
        public BlockDto Block(string name)
        {
            if (_blocksByName.TryGetValue(name, out var block))
                return block;
            throw new InvalidOperationException(
                $"Block '{name}' not found on grid {Id}. " +
                $"Available: {string.Join(", ", _blocksByName.Keys)}");
        }

        /// <summary>Check if a block with the given name exists.</summary>
        public bool HasBlock(string name) => _blocksByName.ContainsKey(name);

        // ── High-level block operations ────────────────────────────

        /// <summary>Turn a block on.</summary>
        public bool TurnOn(string blockName) =>
            ExecuteAction(Block(blockName), "OnOff_On");

        /// <summary>Turn a block off.</summary>
        public bool TurnOff(string blockName) =>
            ExecuteAction(Block(blockName), "OnOff_Off");

        /// <summary>Toggle a block's power state.</summary>
        public bool Toggle(string blockName) =>
            ExecuteAction(Block(blockName), "OnOff");

        /// <summary>Set a terminal property on a block.</summary>
        public bool SetProperty(string blockName, string propertyId, string value)
        {
            var b = Block(blockName);
            return _harness.SetBlockProperty(Id, b.GridPosition.X, b.GridPosition.Y, b.GridPosition.Z, propertyId, value);
        }

        /// <summary>Get a terminal property value from a block.</summary>
        public string GetProperty(string blockName, string propertyId)
        {
            var b = Block(blockName);
            return _harness.GetBlockProperty(Id, b.GridPosition.X, b.GridPosition.Y, b.GridPosition.Z, propertyId);
        }

        /// <summary>Get all blocks matching a subtype filter (e.g. "Light").</summary>
        public List<BlockDto> FilterBlocksBySubtype(string subtypeContains)
        {
            return _blocksByName.Values
                .Where(b => b.Type != null &&
                            b.Type.IndexOf(subtypeContains, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        /// <summary>Get all block states for this grid.</summary>
        public IReadOnlyList<BlockState> GetBlockStates() => _harness.GetBlockStates(Id);

        // ── Private helpers ────────────────────────────────────────

        private bool ExecuteAction(BlockDto block, string actionId)
        {
            return _harness.ExecuteBlockAction(
                Id, block.GridPosition.X, block.GridPosition.Y, block.GridPosition.Z, actionId);
        }
    }
}
