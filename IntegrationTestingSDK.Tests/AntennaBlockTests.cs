using System.Collections.Generic;
using IntegrationTestingSDK.Client;
using IntegrationTestingSDK.Contracts;
using NUnit.Framework;

namespace IntegrationTestingSDK.Tests
{
    [TestFixture]
    public class AntennaBlockTests
    {
        private sealed class FakeHarness : IPbTestHarness
        {
            public (long gridId, int x, int y, int z, string actionId)? LastBlockAction;

            public bool ExecuteBlockAction(long gridId, int x, int y, int z, string actionId)
            {
                LastBlockAction = (gridId, x, y, z, actionId);
                return true;
            }

            public string GetBlockProperty(long gridId, int x, int y, int z, string propertyId) => null;
            public bool SetBlockProperty(long gridId, int x, int y, int z, string propertyId, string value) => true;
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

        private static BlockDto MakeBlock(string name)
            => new BlockDto { Name = name, Type = "RadioAntenna", GridPosition = new Vector3IDto { X = 0, Y = 0, Z = 0 } };

        [Test]
        public void AntennaBlock_TurnOn_ExecutesOnOffOnAction()
        {
            var harness = new FakeHarness();
            var grid = new SpawnedGrid(1L, new[] { MakeBlock("Antenna") }, harness);
            grid.Antenna("Antenna").TurnOn();
            Assert.That(harness.LastBlockAction.Value.actionId, Is.EqualTo("OnOff_On"));
        }

        [Test]
        public void AntennaBlock_TurnOff_ExecutesOnOffOffAction()
        {
            var harness = new FakeHarness();
            var grid = new SpawnedGrid(1L, new[] { MakeBlock("Antenna") }, harness);
            grid.Antenna("Antenna").TurnOff();
            Assert.That(harness.LastBlockAction.Value.actionId, Is.EqualTo("OnOff_Off"));
        }

        [Test]
        public void AntennaBlock_TurnOn_PassesCorrectGridIdAndPosition()
        {
            var harness = new FakeHarness();
            var block = new BlockDto
            {
                Name = "Antenna",
                Type = "RadioAntenna",
                GridPosition = new Vector3IDto { X = 7, Y = 8, Z = 9 }
            };
            var grid = new SpawnedGrid(42L, new[] { block }, harness);
            grid.Antenna("Antenna").TurnOn();

            var call = harness.LastBlockAction.Value;
            Assert.That(call.gridId, Is.EqualTo(42L));
            Assert.That(call.x, Is.EqualTo(7));
            Assert.That(call.y, Is.EqualTo(8));
            Assert.That(call.z, Is.EqualTo(9));
        }
    }
}
