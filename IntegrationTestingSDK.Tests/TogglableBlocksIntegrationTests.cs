using System.IO;
using System.Linq;
using System.Threading;
using IntegrationTestingSDK.Contracts;
using NUnit.Framework;

namespace IntegrationTestingSDK.Tests.Integration
{
    [TestFixture]
    [Category("Integration")]
    public class TogglableBlocksIntegrationTests
    {
        private const string TemplateWorld = "EmptyWorld_NoLimits_WithPB_NoMods";
        private const string TestBlueprint = "TestGrid_AntenaLighsPBLCD";
        private const double SpawnX = 0.0;
        private const double SpawnY = 0.0;
        private const double SpawnZ = 0.0;

        private static string _scriptCode;
        private static IPbTestHarness _harness;
        private SpawnedGrid _grid;

        // Typed block wrappers — like script variables
        private LightBlock _lamp1;
        private LightBlock _lamp2;
        private AntennaBlock _antenna;

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            _scriptCode = LoadScriptCode();
            _harness = IntegrationTestingSDK.PbTestHarnessFactory.Create(_scriptCode);
            _harness.StartWorld(TemplateWorld, nameof(TogglableBlocksIntegrationTests));
        }

        [OneTimeTearDown]
        public void FixtureTearDown()
        {
            if (_harness != null)
            {
                if (_grid != null)
                    try { _harness.RemoveTestGrid(_grid.Id); } catch { }
                try { _harness.StopWorld(); } catch { }
                try { _harness.Dispose(); } catch { }
            }
        }

        [SetUp]
        public void SetUp()
        {
            if (_grid != null)
            {
                try { _harness.RemoveTestGrid(_grid.Id); } catch { }
                _grid = null;
            }

            Thread.Sleep(500);

            var spawned = _harness.SpawnTestGrid(TestBlueprint, SpawnX, SpawnY, SpawnZ);
            Assert.That(spawned, Is.Not.Null.And.Not.Empty, "Spawn should return at least one grid");
            _grid = spawned[0];

            // Resolve typed block wrappers once per test
            _lamp1   = _grid.Light("Interior Light");
            _lamp2   = _grid.Light("Interior Light 2");
            _antenna = _grid.Antenna("Antenna");

            _harness.UploadScript(_grid.Id);
        }

        [TearDown]
        public void TearDown()
        {
            Thread.Sleep(5000);
        }

        // ── Helpers ─────────────────────────────────────────────────

        private static string LoadScriptCode()
        {
            var appData = System.Environment.GetFolderPath(
                System.Environment.SpecialFolder.ApplicationData);
            var scriptPath = Path.Combine(appData,
                "SpaceEngineers", "IngameScripts", "local",
                "Mdk.PbTreeScript", "script.cs");

            if (File.Exists(scriptPath))
                return File.ReadAllText(scriptPath);

            return "// Script not found — build Mdk.PbTreeScript first";
        }

        // ── Block presence ──────────────────────────────────────────

        [Test]
        public void Grid_HasAllExpectedBlocks()
        {
            var blocks = _grid.GetBlockStates();
            var subtypes = blocks.Select(b => b.Subtype).ToList();

            Assert.That(subtypes, Does.Contain("SmallProgrammableBlockReskin"));
            Assert.That(subtypes, Does.Contain("SmallBlockSmallGenerator"));
            Assert.That(subtypes, Does.Contain("SmallBlockRadioAntenna"));
            Assert.That(subtypes, Does.Contain("SmallBlockSmallBatteryBlock"));
            Assert.That(subtypes, Does.Contain("SmallBlockGyro"));
            Assert.That(subtypes, Does.Contain("SmallFullBlockLCDPanel"));
            Assert.That(subtypes.Count(s => s == "SmallBlockSmallLight"), Is.EqualTo(2));
        }

        // ── Toggle: antenna ─────────────────────────────────────────

        [Test]
        public void Toggle_Antenna_OffThenOn()
        {
            Assert.That(_antenna.TurnOff(), Is.True);

            Thread.Sleep(2000);

            var states = _grid.GetBlockStates();
            var ant = states.FirstOrDefault(b =>
                b.Subtype != null &&
                b.Subtype.IndexOf("RadioAntenna", System.StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.That(ant, Is.Not.Null);
            Assert.That(ant.Enabled, Is.False, "Antenna should be off");

            Assert.That(_antenna.TurnOn(), Is.True);

            Thread.Sleep(2000);

            states = _grid.GetBlockStates();
            ant = states.FirstOrDefault(b =>
                b.Subtype != null &&
                b.Subtype.IndexOf("RadioAntenna", System.StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.That(ant, Is.Not.Null);
            Assert.That(ant.Enabled, Is.True, "Antenna should be on");
        }

        // ── Toggle: single lamp ─────────────────────────────────────

        [Test]
        public void Toggle_Lamp1_OffThenOn()
        {
            Assert.That(_lamp1.TurnOff(), Is.True);
            var lamps = _grid.GetBlockStates()
                .Where(b => b.Subtype != null &&
                            b.Subtype.IndexOf("Light", System.StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            Assert.That(lamps[0].Enabled, Is.False, "Lamp1 off");

            Thread.Sleep(2000);

            Assert.That(_lamp1.TurnOn(), Is.True);
            lamps = _grid.GetBlockStates()
                .Where(b => b.Subtype != null &&
                            b.Subtype.IndexOf("Light", System.StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            Assert.That(lamps[0].Enabled, Is.True, "Lamp1 on");
        }

        // ── Lamp colors ─────────────────────────────────────────────

        [Test]
        public void Lamp_ColorSequence_RedYellowGreen_ThenOff()
        {
            // Red
            _lamp1.Color = LightColor.Red;
            _lamp2.Color = LightColor.Red;
            TestContext.WriteLine($"→ Red  lamp1={_lamp1.Color}");
            Thread.Sleep(5000);

            // Yellow
            _lamp1.Color = LightColor.Yellow;
            _lamp2.Color = LightColor.Yellow;
            TestContext.WriteLine($"→ Yellow  lamp1={_lamp1.Color}");
            Thread.Sleep(5000);

            // Green
            _lamp1.Color = LightColor.Green;
            _lamp2.Color = LightColor.Green;
            TestContext.WriteLine($"→ Green  lamp1={_lamp1.Color}");
            Thread.Sleep(5000);

            // Turn off both
            Assert.That(_lamp1.TurnOff(), Is.True);
            Assert.That(_lamp2.TurnOff(), Is.True);
            TestContext.WriteLine("→ Off");
            Thread.Sleep(5000);

            var lamps = _grid.GetBlockStates()
                .Where(b => b.Subtype != null &&
                            b.Subtype.IndexOf("Light", System.StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            Assert.That(lamps[0].Enabled, Is.False, "Lamp1 off");
            Assert.That(lamps[1].Enabled, Is.False, "Lamp2 off");
        }

        [Test]
        public void Toggle_Lamp1On_DoesNotAffectLamp2()
        {
            Assert.That(_lamp1.TurnOff(), Is.True);
            Assert.That(_lamp2.TurnOff(), Is.True);
            Assert.That(_lamp1.TurnOn(), Is.True);

            var lamps = _grid.GetBlockStates()
                .Where(b => b.Subtype != null &&
                            b.Subtype.IndexOf("Light", System.StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            Assert.That(lamps[0].Enabled, Is.True, "Lamp1 on");
            Assert.That(lamps[1].Enabled, Is.False, "Lamp2 still off");
        }

        // ── Toggle: all lamps ───────────────────────────────────────

        [Test]
        public void Toggle_Lamps_BothOn()
        {
            Assert.That(_lamp1.TurnOff(), Is.True);
            Assert.That(_lamp2.TurnOff(), Is.True);
            Assert.That(_lamp1.TurnOn(), Is.True);
            Assert.That(_lamp2.TurnOn(), Is.True);

            var lamps = _grid.GetBlockStates()
                .Where(b => b.Subtype != null &&
                            b.Subtype.IndexOf("Light", System.StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            Assert.That(lamps[0].Enabled, Is.True, "Lamp1 on");
            Assert.That(lamps[1].Enabled, Is.True, "Lamp2 on");
        }

        // ── Toggle: all ─────────────────────────────────────────────

        [Test]
        public void Toggle_All_OffThenOn()
        {
            Assert.That(_antenna.TurnOff(), Is.True);
            Assert.That(_lamp1.TurnOff(), Is.True);
            Assert.That(_lamp2.TurnOff(), Is.True);

            var states = _grid.GetBlockStates();
            var ant = states.FirstOrDefault(b =>
                b.Subtype != null &&
                b.Subtype.IndexOf("RadioAntenna", System.StringComparison.OrdinalIgnoreCase) >= 0);
            var lamps = states
                .Where(b => b.Subtype != null &&
                            b.Subtype.IndexOf("Light", System.StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            Assert.That(ant.Enabled, Is.False, "Antenna off");
            Assert.That(lamps[0].Enabled, Is.False, "Lamp1 off");
            Assert.That(lamps[1].Enabled, Is.False, "Lamp2 off");

            Assert.That(_antenna.TurnOn(), Is.True);
            Assert.That(_lamp1.TurnOn(), Is.True);
            Assert.That(_lamp2.TurnOn(), Is.True);

            states = _grid.GetBlockStates();
            ant = states.FirstOrDefault(b =>
                b.Subtype != null &&
                b.Subtype.IndexOf("RadioAntenna", System.StringComparison.OrdinalIgnoreCase) >= 0);
            lamps = states
                .Where(b => b.Subtype != null &&
                            b.Subtype.IndexOf("Light", System.StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            Assert.That(ant.Enabled, Is.True, "Antenna on");
            Assert.That(lamps[0].Enabled, Is.True, "Lamp1 on");
            Assert.That(lamps[1].Enabled, Is.True, "Lamp2 on");
        }

        // ── Debug ───────────────────────────────────────────────────

        [Test]
        public void Debug_DumpAllBlocks()
        {
            var blocks = _grid.GetBlockStates();
            Assert.That(blocks, Is.Not.Empty);
            foreach (var b in blocks)
                TestContext.WriteLine($"  [{b.Type}] {b.Subtype} enabled={b.Enabled}");
        }
    }
}
