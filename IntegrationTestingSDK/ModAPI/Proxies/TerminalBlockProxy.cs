using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;
using IntegrationTestingSDK.ModAPI.Types;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    /// <summary>
    ///     Base class for terminal block proxies. Implements <see cref="IMyTerminalBlock"/>
    ///     by reading/writing terminal properties and providing block identity.
    /// </summary>
    internal class TerminalBlockProxy : BlockProxyBase, IMyTerminalBlock, IMyCubeBlock, IMyEntity
    {
        internal TerminalBlockProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }

        // ── IMyEntity ──────────────────────────────────────────
        public long EntityId => _block?.GridPosition != null
            ? ((long)_block.GridPosition.X << 32) | ((long)_block.GridPosition.Y << 16) | (long)_block.GridPosition.Z
            : 0;

        public string DisplayName => _block?.Name ?? string.Empty;

        // ── IMyCubeBlock ───────────────────────────────────────
        public IMyCubeGrid CubeGrid => null;
        public Vector3I Min => Vector3I.Zero;
        public Vector3I Max => Vector3I.Zero;
        public string DefinitionDisplayNameText => string.Empty;
        public bool IsWorking => true;
        public bool IsFunctional => true;
        public float Mass => 0f;
        public int NumberInGrid => 0;
        public string BlockDefinition => string.Empty;

        // ── IMyTerminalBlock ───────────────────────────────────
        public string CustomName
        {
            get => _block?.Name ?? string.Empty;
            set => SetProperty("CustomName", value);
        }

        public string CustomData
        {
            get => GetProperty("CustomData") ?? string.Empty;
            set => SetProperty("CustomData", value);
        }

        public bool ShowOnHUD
        {
            get => bool.TryParse(GetProperty("ShowOnHUD"), out var v) && v;
            set => SetProperty("ShowOnHUD", value.ToString());
        }

        public bool ShowInTerminal
        {
            get => bool.TryParse(GetProperty("ShowInTerminal"), out var v) && v;
            set => SetProperty("ShowInTerminal", value.ToString());
        }

        public bool ShowInToolbarConfig
        {
            get => bool.TryParse(GetProperty("ShowInToolbarConfig"), out var v) && v;
            set => SetProperty("ShowInToolbarConfig", value.ToString());
        }

        public bool ShowInInventory
        {
            get => bool.TryParse(GetProperty("ShowInInventory"), out var v) && v;
            set => SetProperty("ShowInInventory", value.ToString());
        }

        public string CustomNameWithFaction => GetProperty("CustomNameWithFaction") ?? CustomName;

        public string DetailedInfo => GetProperty("DetailedInfo") ?? string.Empty;

        public bool HasLocalPlayerAccess()
        {
            var raw = GetProperty("HasPlayerAccess");
            return bool.TryParse(raw, out var v) && v;
        }

        public bool HasPlayerAccess(long playerId)
        {
            var raw = GetProperty("HasPlayerAccess");
            return bool.TryParse(raw, out var v) && v;
        }
    }
}
