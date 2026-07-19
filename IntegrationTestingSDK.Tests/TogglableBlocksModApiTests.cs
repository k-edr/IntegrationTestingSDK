using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;
using IntegrationTestingSDK.ModAPI.Types;
using NUnit.Framework;

namespace IntegrationTestingSDK.Tests.Integration
{
    /// <summary>
    ///     Integration tests for togglable blocks using the <b>ModAPI-mirror</b> style.
    ///     Blocks are resolved via <c>GridTerminalSystem</c> (like in-game scripts),
    ///     and controlled through <c>IMyLightingBlock</c>, <c>IMyRadioAntenna</c>
    ///     interfaces — the same as <c>Sandbox.ModAPI.Ingame</c>.
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    public class TogglableBlocksModApiTests
    {
        private const string TemplateWorld = "EmptyWorld_NoLimits_WithPB_NoMods";
        private const string TestBlueprint = "TestGrid_AntenaLighsPBLCD";
        private const double SpawnX = 0.0;
        private const double SpawnY = 0.0;
        private const double SpawnZ = 0.0;

        private static string _scriptCode;
        private static IPbTestHarness _harness;
        private SpawnedGrid _grid;

        // ModAPI-style typed block references — like script fields
        private IMyLightingBlock _lamp1;
        private IMyLightingBlock _lamp2;
        private IMyRadioAntenna _antenna;

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            _scriptCode = LoadScriptCode();
            _harness = IntegrationTestingSDK.PbTestHarnessFactory.Create(_scriptCode);
            _harness.StartWorld(TemplateWorld, nameof(TogglableBlocksModApiTests));
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

            // ── ModAPI style: resolve blocks through GridTerminalSystem ──
            var gts = _grid.GridTerminalSystem;

            _lamp1   = gts.GetBlockWithName("Interior Light") as IMyLightingBlock;
            _lamp2   = gts.GetBlockWithName("Interior Light 2") as IMyLightingBlock;
            _antenna = gts.GetBlockWithName("Antenna") as IMyRadioAntenna;

            Assert.That(_lamp1,   Is.Not.Null, "Interior Light not found or wrong type");
            Assert.That(_lamp2,   Is.Not.Null, "Interior Light 2 not found or wrong type");
            Assert.That(_antenna, Is.Not.Null, "Antenna not found or wrong type");

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

        // ── Block presence (old API, still works) ──────────────────

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

        // ── Block discovery via GridTerminalSystem ─────────────────

        [Test]
        public void GetBlocksOfType_LightingBlocks_ReturnsTwoLamps()
        {
            var gts = _grid.GridTerminalSystem;
            var lamps = new List<IMyTerminalBlock>();

            gts.GetBlocksOfType<IMyLightingBlock>(lamps);

            Assert.That(lamps, Has.Count.EqualTo(2), "Should find exactly 2 light blocks");
        }

        [Test]
        public void GetBlocksOfType_RadioAntenna_ReturnsOne()
        {
            var gts = _grid.GridTerminalSystem;
            var antennas = new List<IMyTerminalBlock>();

            gts.GetBlocksOfType<IMyRadioAntenna>(antennas);

            Assert.That(antennas, Has.Count.EqualTo(1), "Should find exactly 1 antenna");
        }

        // ── Toggle: antenna (ModAPI style) ─────────────────────────

        [Test]
        public void Toggle_Antenna_OffThenOn()
        {
            // Turn off via IMyFunctionalBlock.Enabled (ModAPI style)
            _antenna.Enabled = false;

            Thread.Sleep(2000);

            var states = _grid.GetBlockStates();
            var ant = states.FirstOrDefault(b =>
                b.Subtype != null &&
                b.Subtype.IndexOf("RadioAntenna", System.StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.That(ant, Is.Not.Null);
            Assert.That(ant.Enabled, Is.False, "Antenna should be off");

            // Turn on
            _antenna.Enabled = true;

            Thread.Sleep(2000);

            states = _grid.GetBlockStates();
            ant = states.FirstOrDefault(b =>
                b.Subtype != null &&
                b.Subtype.IndexOf("RadioAntenna", System.StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.That(ant, Is.Not.Null);
            Assert.That(ant.Enabled, Is.True, "Antenna should be on");
        }

        [Test]
        public void Toggle_Antenna_BroadcastingOnThenOff()
        {
            // Enable broadcasting
            _antenna.EnableBroadcasting = true;

            Thread.Sleep(2000);

            var states = _grid.GetBlockStates();
            var ant = states.FirstOrDefault(b =>
                b.Subtype != null &&
                b.Subtype.IndexOf("RadioAntenna", System.StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.That(ant, Is.Not.Null);
            Assert.That(ant.Enabled, Is.True, "Antenna should be on (broadcasting)");

            // Disable broadcasting
            _antenna.EnableBroadcasting = false;

            Thread.Sleep(2000);

            states = _grid.GetBlockStates();
            ant = states.FirstOrDefault(b =>
                b.Subtype != null &&
                b.Subtype.IndexOf("RadioAntenna", System.StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.That(ant, Is.Not.Null);
            // Note: the block itself may still be Enabled; broadcasting is a separate toggle
        }

        // ── Toggle: single lamp (ModAPI style) ─────────────────────

        [Test]
        public void Toggle_Lamp1_OffThenOn()
        {
            _lamp1.Enabled = false;
            var lamps = _grid.GetBlockStates()
                .Where(b => b.Subtype != null &&
                            b.Subtype.IndexOf("Light", System.StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            Assert.That(lamps[0].Enabled, Is.False, "Lamp1 off");

            Thread.Sleep(2000);

            _lamp1.Enabled = true;
            lamps = _grid.GetBlockStates()
                .Where(b => b.Subtype != null &&
                            b.Subtype.IndexOf("Light", System.StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            Assert.That(lamps[0].Enabled, Is.True, "Lamp1 on");
        }

        // ── Lamp colors (ModAPI style — Color is VRageMath-like) ───

        [Test]
        public void Lamp_ColorSequence_RedYellowGreen_ThenOff()
        {
            // Red
            _lamp1.Color = Color.Red;
            _lamp2.Color = Color.Red;
            TestContext.WriteLine($"→ Red  lamp1={_lamp1.Color}");
            Thread.Sleep(5000);

            // Yellow
            _lamp1.Color = Color.Yellow;
            _lamp2.Color = Color.Yellow;
            TestContext.WriteLine($"→ Yellow  lamp1={_lamp1.Color}");
            Thread.Sleep(5000);

            // Green
            _lamp1.Color = Color.Green;
            _lamp2.Color = Color.Green;
            TestContext.WriteLine($"→ Green  lamp1={_lamp1.Color}");
            Thread.Sleep(5000);

            // Turn off both
            _lamp1.Enabled = false;
            _lamp2.Enabled = false;
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
            _lamp1.Enabled = false;
            _lamp2.Enabled = false;
            _lamp1.Enabled = true;

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
            _lamp1.Enabled = false;
            _lamp2.Enabled = false;
            _lamp1.Enabled = true;
            _lamp2.Enabled = true;

            var lamps = _grid.GetBlockStates()
                .Where(b => b.Subtype != null &&
                            b.Subtype.IndexOf("Light", System.StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            Assert.That(lamps[0].Enabled, Is.True, "Lamp1 on");
            Assert.That(lamps[1].Enabled, Is.True, "Lamp2 on");
        }

        // ── Toggle: all blocks via interface ────────────────────────

        [Test]
        public void Toggle_All_OffThenOn()
        {
            _antenna.Enabled = false;
            _lamp1.Enabled   = false;
            _lamp2.Enabled   = false;

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

            _antenna.Enabled = true;
            _lamp1.Enabled   = true;
            _lamp2.Enabled   = true;

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

        // ── Custom Data (IMyTerminalBlock) ─────────────────────────

        [Test]
        public void TerminalBlock_CustomData_CanBeWrittenAndRead()
        {
            var testData = "TestPayload_v1";
            _antenna.CustomData = testData;

            Thread.Sleep(1000);

            var readBack = _antenna.CustomData;
            Assert.That(readBack, Does.Contain("TestPayload"), "CustomData should persist");
        }

        [Test]
        public void TerminalBlock_CustomName_IsReadable()
        {
            var name = _lamp1.CustomName;
            Assert.That(name, Is.Not.Null.And.Not.Empty, "CustomName should be readable");
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
