using System;
using System.Collections.Generic;
using System.Linq;

namespace IntegrationTestingSDK.Contracts
{
    /// <summary>
    ///     Typed wrapper around a spawned grid.
    ///     Provides name-based block lookup and typed block factories
    ///     (<see cref="Light"/>, <see cref="Antenna"/>) for
    ///     script-like direct interaction with the game.
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

        // ── Typed block factories ──────────────────────────────────

        /// <summary>Get a typed light block wrapper (e.g. <c>_grid.Light("Lamp 1")</c>).</summary>
        public LightBlock Light(string name) => new(this, Block(name));

        /// <summary>Get a typed antenna block wrapper.</summary>
        public AntennaBlock Antenna(string name) => new(this, Block(name));

        // ── Block-level API (internal — used by BlockWrapper) ──────

        internal bool ExecuteBlockAction(BlockDto block, string actionId)
        {
            return _harness.ExecuteBlockAction(
                Id, block.GridPosition.X, block.GridPosition.Y, block.GridPosition.Z, actionId);
        }

        internal string GetBlockProperty(BlockDto block, string propertyId)
        {
            return _harness.GetBlockProperty(
                Id, block.GridPosition.X, block.GridPosition.Y, block.GridPosition.Z, propertyId);
        }

        internal bool SetBlockProperty(BlockDto block, string propertyId, string value)
        {
            return _harness.SetBlockProperty(
                Id, block.GridPosition.X, block.GridPosition.Y, block.GridPosition.Z, propertyId, value);
        }

        // ── Grid-level queries ─────────────────────────────────────

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
    }
}
