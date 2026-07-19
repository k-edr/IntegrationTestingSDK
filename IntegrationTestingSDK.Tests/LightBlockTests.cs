using System.Collections.Generic;
using IntegrationTestingSDK.Client;
using IntegrationTestingSDK.Contracts;
using NUnit.Framework;

namespace IntegrationTestingSDK.Tests
{
    // ─────────────────────────────────────────────────────────────────────────
    // P1 — LightBlock typed wrapper tests
    // ─────────────────────────────────────────────────────────────────────────

    [TestFixture]
    public class LightBlockTests
    {
        private sealed class FakeHarness : IPbTestHarness
        {
            public (long gridId, int x, int y, int z, string actionId)? LastBlockAction;
            public (long gridId, int x, int y, int z, string propertyId)? LastGetProperty;
            public (long gridId, int x, int y, int z, string propertyId, string value)? LastSetProperty;

            public string GetPropertyReturn = "";
            public bool BlockActionReturn = true;

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
                return true;
            }

            public IReadOnlyList<BlockState> GetBlockStates(long gridId) => new List<BlockState>();
            public void Dispose() { }
            public void StartWorld(string t, string w) { }
            public void StopWorld() { }
            public IReadOnlyList<SpawnedGrid> SpawnTestGrid(string n, double x, double y, double z) => null;
            public void RemoveTestGrid(long id) { }
            public void UploadScript(long id) { }
            public string RunScript(long id, string arg = null) => null;
            public string GetLcdContent(long id) => null;
        }

        private static BlockDto MakeBlock(string name, int x = 1, int y = 2, int z = 3)
            => new BlockDto { Name = name, Type = "LightingBlock", GridPosition = new Vector3IDto { X = x, Y = y, Z = z } };

        // ── LightBlock.Color setter → SetBlockProperty("Color", ...) ─────────

        [Test]
        public void LightBlock_SetColor_CallsSetBlockPropertyWithColorKey()
        {
            var harness = new FakeHarness();
            var block = MakeBlock("Lamp", 1, 2, 3);
            var grid = new SpawnedGrid(10L, new[] { block }, harness);
            var light = grid.Light("Lamp");

            light.Color = LightColor.Red;

            Assert.That(harness.LastSetProperty, Is.Not.Null);
            Assert.That(harness.LastSetProperty.Value.propertyId, Is.EqualTo("Color"));
        }

        [Test]
        public void LightBlock_SetColor_PackedValueMatchesRgb()
        {
            var harness = new FakeHarness();
            var block = MakeBlock("Lamp");
            var grid = new SpawnedGrid(10L, new[] { block }, harness);
            var light = grid.Light("Lamp");

            var color = new LightColor(255, 0, 0); // Red
            light.Color = color;

            var expectedPacked = color.ToPackedString();
            Assert.That(harness.LastSetProperty.Value.value, Is.EqualTo(expectedPacked));
        }

        // ── LightBlock.Color getter → GetBlockProperty("Color") ──────────────

        [Test]
        public void LightBlock_GetColor_CallsGetBlockPropertyWithColorKey()
        {
            var harness = new FakeHarness();
            // Return a valid packed red: R=255 G=0 B=0 A=255 → 0xFF0000FF
            harness.GetPropertyReturn = new LightColor(255, 0, 0).ToPackedString();
            var block = MakeBlock("Lamp");
            var grid = new SpawnedGrid(10L, new[] { block }, harness);
            var light = grid.Light("Lamp");

            var color = light.Color;

            Assert.That(harness.LastGetProperty, Is.Not.Null);
            Assert.That(harness.LastGetProperty.Value.propertyId, Is.EqualTo("Color"));
            Assert.That(color.R, Is.EqualTo(255));
            Assert.That(color.G, Is.EqualTo(0));
            Assert.That(color.B, Is.EqualTo(0));
        }

        [Test]
        public void LightBlock_GetColor_InvalidString_ReturnsWhite()
        {
            var harness = new FakeHarness();
            harness.GetPropertyReturn = "NOT_A_NUMBER";
            var block = MakeBlock("Lamp");
            var grid = new SpawnedGrid(10L, new[] { block }, harness);
            var light = grid.Light("Lamp");

            var color = light.Color;
            // Should fall back to White.
            Assert.That(color.R, Is.EqualTo(255));
            Assert.That(color.G, Is.EqualTo(255));
            Assert.That(color.B, Is.EqualTo(255));
        }

        // ── LightBlock.TurnOn / TurnOff ───────────────────────────────────────

        [Test]
        public void LightBlock_TurnOn_ExecutesOnOffOnAction()
        {
            var harness = new FakeHarness();
            var block = MakeBlock("Lamp");
            var grid = new SpawnedGrid(10L, new[] { block }, harness);
            grid.Light("Lamp").TurnOn();
            Assert.That(harness.LastBlockAction.Value.actionId, Is.EqualTo("OnOff_On"));
        }

        [Test]
        public void LightBlock_TurnOff_ExecutesOnOffOffAction()
        {
            var harness = new FakeHarness();
            var block = MakeBlock("Lamp");
            var grid = new SpawnedGrid(10L, new[] { block }, harness);
            grid.Light("Lamp").TurnOff();
            Assert.That(harness.LastBlockAction.Value.actionId, Is.EqualTo("OnOff_Off"));
        }
    }
}
