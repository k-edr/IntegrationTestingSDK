using System;
using System.Collections.Generic;
using System.Linq;
using IntegrationTestingSDK.Client;
using IntegrationTestingSDK.Contracts;
using NUnit.Framework;

namespace IntegrationTestingSDK.Tests
{
    // ─────────────────────────────────────────────────────────────────────────
    // P1 — SpawnedGrid / IPbTestHarness delegation contract
    // ─────────────────────────────────────────────────────────────────────────

    [TestFixture]
    public class SpawnedGridTests
    {
        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        ///     Minimal in-process stub that records calls so tests can assert
        ///     delegation without requiring NSubstitute or the real game.
        /// </summary>
        private sealed class FakeHarness : IPbTestHarness
        {
            // Capture the most-recent call arguments for each method.
            public (long gridId, int x, int y, int z, string actionId)? LastBlockAction;
            public (long gridId, int x, int y, int z, string propertyId)? LastGetProperty;
            public (long gridId, int x, int y, int z, string propertyId, string value)? LastSetProperty;
            public long? LastGetBlockStatesGridId;

            // Configurable return values.
            public bool BlockActionReturn = true;
            public string GetPropertyReturn = "42";
            public bool SetPropertyReturn = true;
            public IReadOnlyList<BlockState> BlockStatesReturn = new List<BlockState>();

            public bool ExecuteBlockAction(long gridId, int x, int y, int z, string actionId)
            {
                LastBlockAction = (gridId, x, y, z, actionId);
                return BlockActionReturn;
            }

            public string GetBlockProperty(long gridId, int x, int y, int z, string propertyId)
            {
                LastGetProperty = (gridId, x, y, z, propertyId);
                return GetPropertyReturn;
            }

            public bool SetBlockProperty(long gridId, int x, int y, int z, string propertyId, string value)
            {
                LastSetProperty = (gridId, x, y, z, propertyId, value);
                return SetPropertyReturn;
            }

            public IReadOnlyList<BlockState> GetBlockStates(long gridId)
            {
                LastGetBlockStatesGridId = gridId;
                return BlockStatesReturn;
            }

            // Unused members — satisfy the interface.
            public void Dispose() { }
            public void StartWorld(string t, string w) { }
            public void StopWorld() { }
            public IReadOnlyList<SpawnedGrid> SpawnTestGrid(string n, double x, double y, double z) => null;
            public void RemoveTestGrid(long id) { }
            public void UploadScript(long id) { }
            public string RunScript(long id, string arg = null) => null;
            public string GetLcdContent(long id) => null;
        }

        private static BlockDto MakeBlock(string name, string type, int x = 0, int y = 0, int z = 0)
            => new BlockDto
            {
                Name = name,
                Type = type,
                GridPosition = new Vector3IDto { X = x, Y = y, Z = z }
            };

        private static SpawnedGrid MakeGrid(FakeHarness harness, long id, params BlockDto[] blocks)
            => new SpawnedGrid(id, blocks, harness);

        // ── Block(name) — case-insensitive lookup ─────────────────────────────

        [Test]
        public void Block_ExactName_ReturnsCorrectBlock()
        {
            var harness = new FakeHarness();
            var block = MakeBlock("Lamp 1", "LightingBlock", 1, 2, 3);
            var grid = MakeGrid(harness, 99L, block);

            var found = grid.Block("Lamp 1");
            Assert.That(found.Name, Is.EqualTo("Lamp 1"));
        }

        [Test]
        public void Block_DifferentCase_ReturnsCorrectBlock()
        {
            var harness = new FakeHarness();
            var block = MakeBlock("Lamp 1", "LightingBlock");
            var grid = MakeGrid(harness, 99L, block);

            var found = grid.Block("lamp 1");
            Assert.That(found.Name, Is.EqualTo("Lamp 1"));
        }

        [Test]
        public void Block_UnknownName_ThrowsInvalidOperationException()
        {
            var harness = new FakeHarness();
            var block = MakeBlock("Lamp 1", "LightingBlock");
            var grid = MakeGrid(harness, 99L, block);

            var ex = Assert.Throws<InvalidOperationException>(() => grid.Block("No Such Block"));
            // Message should mention the grid ID and the missing name.
            Assert.That(ex.Message, Does.Contain("No Such Block"));
            Assert.That(ex.Message, Does.Contain("99"));
        }

        // ── HasBlock() ────────────────────────────────────────────────────────

        [Test]
        public void HasBlock_ExistingBlock_ReturnsTrue()
        {
            var harness = new FakeHarness();
            var grid = MakeGrid(harness, 1L, MakeBlock("Antenna", "RadioAntenna"));
            Assert.That(grid.HasBlock("Antenna"), Is.True);
        }

        [Test]
        public void HasBlock_DifferentCase_ReturnsTrue()
        {
            var harness = new FakeHarness();
            var grid = MakeGrid(harness, 1L, MakeBlock("Antenna", "RadioAntenna"));
            Assert.That(grid.HasBlock("ANTENNA"), Is.True);
        }

        [Test]
        public void HasBlock_UnknownName_ReturnsFalse()
        {
            var harness = new FakeHarness();
            var grid = MakeGrid(harness, 1L, MakeBlock("Antenna", "RadioAntenna"));
            Assert.That(grid.HasBlock("NotHere"), Is.False);
        }

        // ── Constructor ignores null/empty names ──────────────────────────────

        [Test]
        public void Constructor_BlockWithNullName_IsExcludedFromBlocks()
        {
            var harness = new FakeHarness();
            var nullName = new BlockDto { Name = null, Type = "X", GridPosition = new Vector3IDto() };
            var emptyName = new BlockDto { Name = "", Type = "X", GridPosition = new Vector3IDto() };
            var valid = MakeBlock("ValidBlock", "Something");

            var grid = MakeGrid(harness, 1L, nullName, emptyName, valid);

            Assert.That(grid.Blocks.Count, Is.EqualTo(1));
            Assert.That(grid.Blocks.ContainsKey("ValidBlock"), Is.True);
        }

        // ── FilterBlocksBySubtype() ───────────────────────────────────────────

        [Test]
        public void FilterBlocksBySubtype_MatchingType_ReturnsBlocks()
        {
            var harness = new FakeHarness();
            var light1 = MakeBlock("Lamp 1", "MyObjectBuilder_InteriorLight");
            var light2 = MakeBlock("Lamp 2", "MyObjectBuilder_InteriorLight");
            var antenna = MakeBlock("Antenna", "MyObjectBuilder_RadioAntenna");
            var grid = MakeGrid(harness, 1L, light1, light2, antenna);

            var lights = grid.FilterBlocksBySubtype("InteriorLight");
            Assert.That(lights.Count, Is.EqualTo(2));
            Assert.That(lights.Select(b => b.Name), Is.EquivalentTo(new[] { "Lamp 1", "Lamp 2" }));
        }

        [Test]
        public void FilterBlocksBySubtype_CaseInsensitive_ReturnsMatches()
        {
            var harness = new FakeHarness();
            var light = MakeBlock("Lamp 1", "MyObjectBuilder_InteriorLight");
            var grid = MakeGrid(harness, 1L, light);

            var result = grid.FilterBlocksBySubtype("interiorlight");
            Assert.That(result.Count, Is.EqualTo(1));
        }

        [Test]
        public void FilterBlocksBySubtype_NoMatch_ReturnsEmpty()
        {
            var harness = new FakeHarness();
            var grid = MakeGrid(harness, 1L, MakeBlock("Lamp", "LightBlock"));

            var result = grid.FilterBlocksBySubtype("Antenna");
            Assert.That(result, Is.Empty);
        }

        // ── GetBlockStates() delegates to harness ─────────────────────────────

        [Test]
        public void GetBlockStates_DelegatesToHarnessWithGridId()
        {
            var harness = new FakeHarness();
            var expectedStates = new List<BlockState> { new BlockState { EntityId = 42 } };
            harness.BlockStatesReturn = expectedStates;

            var grid = MakeGrid(harness, 77L);
            var result = grid.GetBlockStates();

            Assert.That(harness.LastGetBlockStatesGridId, Is.EqualTo(77L));
            Assert.That(result, Is.SameAs(expectedStates));
        }

        // ── ExecuteBlockAction delegates through SpawnedGrid ──────────────────

        [Test]
        public void ExecuteBlockAction_DelegatesToHarnessWithCorrectArgs()
        {
            var harness = new FakeHarness();
            var block = MakeBlock("B1", "SomeType", 3, 5, 7);
            var grid = MakeGrid(harness, 11L, block);

            var result = grid.ExecuteBlockAction(block, "OnOff_On");

            Assert.That(result, Is.True);
            Assert.That(harness.LastBlockAction, Is.Not.Null);
            var call = harness.LastBlockAction.Value;
            Assert.That(call.gridId, Is.EqualTo(11L));
            Assert.That(call.x, Is.EqualTo(3));
            Assert.That(call.y, Is.EqualTo(5));
            Assert.That(call.z, Is.EqualTo(7));
            Assert.That(call.actionId, Is.EqualTo("OnOff_On"));
        }

        // ── GetBlockProperty delegates through SpawnedGrid ───────────────────

        [Test]
        public void GetBlockProperty_DelegatesToHarnessWithCorrectArgs()
        {
            var harness = new FakeHarness();
            harness.GetPropertyReturn = "SomeValue";
            var block = MakeBlock("B1", "SomeType", 1, 2, 3);
            var grid = MakeGrid(harness, 55L, block);

            var result = grid.GetBlockProperty(block, "Color");

            Assert.That(result, Is.EqualTo("SomeValue"));
            var call = harness.LastGetProperty.Value;
            Assert.That(call.gridId, Is.EqualTo(55L));
            Assert.That(call.propertyId, Is.EqualTo("Color"));
        }

        // ── SetBlockProperty delegates through SpawnedGrid ───────────────────

        [Test]
        public void SetBlockProperty_DelegatesToHarnessWithCorrectArgs()
        {
            var harness = new FakeHarness();
            var block = MakeBlock("B1", "SomeType", 4, 5, 6);
            var grid = MakeGrid(harness, 33L, block);

            var result = grid.SetBlockProperty(block, "Color", "ff0000");

            Assert.That(result, Is.True);
            var call = harness.LastSetProperty.Value;
            Assert.That(call.gridId, Is.EqualTo(33L));
            Assert.That(call.propertyId, Is.EqualTo("Color"));
            Assert.That(call.value, Is.EqualTo("ff0000"));
        }

        // ── GridTerminalSystem — lazy init ────────────────────────────────────

        [Test]
        public void GridTerminalSystem_CalledTwice_ReturnsSameInstance()
        {
            var harness = new FakeHarness();
            var grid = MakeGrid(harness, 1L, MakeBlock("B", "T"));

            var first = grid.GridTerminalSystem;
            var second = grid.GridTerminalSystem;

            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void GridTerminalSystem_IsNotNull()
        {
            var harness = new FakeHarness();
            var grid = MakeGrid(harness, 1L, MakeBlock("B", "T"));

            Assert.That(grid.GridTerminalSystem, Is.Not.Null);
        }
    }
}
