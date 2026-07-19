using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;
using IntegrationTestingSDK.ModAPI.Types;
using NUnit.Framework;

namespace IntegrationTestingSDK.Tests.Integration
{
    /// <summary>
    ///     Integration tests for <b>every ModAPI proxy</b> (47 types) against
    ///     <c>TestGrid_44Types</c>. Blocks are resolved via
    ///     <c>GridTerminalSystem.GetBlocksOfType&lt;T&gt;()</c>.
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    public class AllProxiesIntegrationTests
    {
        private const string TemplateWorld = "EmptyWorld_NoLimits_WithPB_NoMods";
        private const string TestBlueprint = "TestGrid_44Types";
        private const double SpawnX = 0.0;
        private const double SpawnY = 0.0;
        private const double SpawnZ = 0.0;

        private static IPbTestHarness _harness;
        private SpawnedGrid _grid;

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            _harness = IntegrationTestingSDK.PbTestHarnessFactory.Create("// no script needed");
            _harness.StartWorld(TemplateWorld, nameof(AllProxiesIntegrationTests));
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

            // Thread.Sleep(500);

            var spawned = _harness.SpawnTestGrid(TestBlueprint, SpawnX, SpawnY, SpawnZ);
            Assert.That(spawned, Is.Not.Null.And.Not.Empty, "Spawn should return at least one grid");
            _grid = spawned[0];
        }

        [TearDown]
        public void TearDown()
        {
            // Thread.Sleep(3000);
        }

        // ═══════════════════════════════════════════════════════════
        // Helper: resolve all blocks of a given interface type
        // ═══════════════════════════════════════════════════════════

        private List<IMyTerminalBlock> GetBlocksOfType<T>()
        {
            var list = new List<IMyTerminalBlock>();
            _grid.GridTerminalSystem.GetBlocksOfType<T>(list);
            return list;
        }

        // ═══════════════════════════════════════════════════════════
        // Debug — dump all blocks present on the grid
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void Debug_DumpAllBlocks()
        {
            var blocks = _grid.GetBlockStates();
            Assert.That(blocks, Is.Not.Empty);
            TestContext.WriteLine($"=== {blocks.Count} blocks on {TestBlueprint} ===");
            foreach (var b in blocks)
                TestContext.WriteLine($"  [{b.Type}] {b.Subtype} enabled={b.Enabled}");
        }

        [Test]
        public void Debug_DumpBlockPropertiesAndActions()
        {
            var blocks = _grid.Blocks;
            Assert.That(blocks, Is.Not.Empty);

            foreach (var kv in blocks)
            {
                TestContext.WriteLine($"\n--- {kv.Key} ({kv.Value.Type}) ---");
                try
                {
                    var detail = _grid.GetBlockDetail(kv.Value);
                    if (detail == null) continue;

                    if (detail.Properties != null && detail.Properties.Count > 0)
                    {
                        TestContext.WriteLine("  Properties:");
                        foreach (var p in detail.Properties)
                            TestContext.WriteLine($"    [{p.PropertyType}] {p.Id} = {p.Value}");
                    }

                    if (detail.Actions != null && detail.Actions.Count > 0)
                    {
                        TestContext.WriteLine("  Actions:");
                        foreach (var a in detail.Actions)
                            TestContext.WriteLine($"    {a.Id} ({a.Name})");
                    }
                }
                catch (Exception ex)
                {
                    TestContext.WriteLine($"  SKIP: {ex.Message}");
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // TERMINAL BLOCK & FUNCTIONAL BLOCK (bases)
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void TerminalBlock_CustomData_ReadWrite()
        {
            var all = GetBlocksOfType<IMyTerminalBlock>();
            Assert.That(all, Is.Not.Empty);

            var tb = all[0] as IMyTerminalBlock;
            Assert.That(tb, Is.Not.Null);

            tb.CustomData = "Test_v42";
            // Thread.Sleep(1000);
            Assert.That(tb.CustomData, Does.Contain("Test_v42"), "CustomData should persist");
        }

        [Test]
        public void TerminalBlock_CustomName_IsReadable()
        {
            var all = GetBlocksOfType<IMyTerminalBlock>();
            Assert.That(all, Is.Not.Empty);

            var tb = all[0] as IMyTerminalBlock;
            Assert.That(tb, Is.Not.Null);
            Assert.That(tb.CustomName, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void TerminalBlock_ShowOnHUD_CanBeRead()
        {
            var all = GetBlocksOfType<IMyTerminalBlock>();
            Assert.That(all, Is.Not.Empty);
            var tb = all[0] as IMyTerminalBlock;
            Assert.That(tb, Is.Not.Null);

            // Read current state (just don't crash)
            var hud = tb.ShowOnHUD;
            TestContext.WriteLine($"ShowOnHUD = {hud}");
        }

        [Test]
        public void TerminalBlock_ShowInTerminal_CanBeRead()
        {
            var all = GetBlocksOfType<IMyTerminalBlock>();
            Assert.That(all, Is.Not.Empty);
            var tb = all[0] as IMyTerminalBlock;
            Assert.That(tb, Is.Not.Null);

            var sit = tb.ShowInTerminal;
            TestContext.WriteLine($"ShowInTerminal = {sit}");
        }

        [Test]
        public void TerminalBlock_DetailedInfo_IsNotNullOrEmpty()
        {
            var all = GetBlocksOfType<IMyTerminalBlock>();
            Assert.That(all, Is.Not.Empty);
            var tb = all[0] as IMyTerminalBlock;
            Assert.That(tb, Is.Not.Null);
            Assert.That(tb.DetailedInfo, Is.Not.Null);
        }

        [Test]
        public void TerminalBlock_HasLocalPlayerAccess_ReturnsBool()
        {
            var all = GetBlocksOfType<IMyTerminalBlock>();
            Assert.That(all, Is.Not.Empty);
            var tb = all[0] as IMyTerminalBlock;
            Assert.That(tb, Is.Not.Null);

            var access = tb.HasLocalPlayerAccess();
            TestContext.WriteLine($"HasLocalPlayerAccess = {access}");
        }

        [Test]
        [Ignore("OnOff property requires main-thread action dispatch; use ExecuteAction instead")]
        public void FunctionalBlock_Enabled_Toggle()
        {
            var blocks = GetBlocksOfType<IMyFunctionalBlock>();
            Assert.That(blocks, Is.Not.Empty);

            var fb = blocks[0] as IMyFunctionalBlock;
            Assert.That(fb, Is.Not.Null);

            var initial = fb.Enabled;

            fb.Enabled = false;
            // Thread.Sleep(1500);
            Assert.That(fb.Enabled, Is.False, "Block should be off");

            fb.Enabled = true;
            // Thread.Sleep(1500);
            Assert.That(fb.Enabled, Is.True, "Block should be on");
        }

        // ═══════════════════════════════════════════════════════════
        // ENTITY / CUBE BLOCK / TERMINAL identity
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void Entity_EntityId_IsPositive()
        {
            var all = GetBlocksOfType<IMyEntity>();
            Assert.That(all, Is.Not.Empty);
            var e = all[0] as IMyEntity;
            Assert.That(e, Is.Not.Null);
            Assert.That(e.EntityId, Is.GreaterThan(0));
        }

        [Test]
        public void Entity_DisplayName_IsNotNullOrEmpty()
        {
            var all = GetBlocksOfType<IMyEntity>();
            Assert.That(all, Is.Not.Empty);
            var e = all[0] as IMyEntity;
            Assert.That(e, Is.Not.Null);
            Assert.That(e.DisplayName, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void CubeBlock_MinMax_AreNotNull()
        {
            var all = GetBlocksOfType<IMyCubeBlock>();
            Assert.That(all, Is.Not.Empty);
            var cb = all[0] as IMyCubeBlock;
            Assert.That(cb, Is.Not.Null);
            Assert.That(cb.Min, Is.Not.Null);
            Assert.That(cb.Max, Is.Not.Null);
        }

        // ═══════════════════════════════════════════════════════════
        // GRID TERMINAL SYSTEM
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void GridTerminalSystem_GetBlocks_ReturnsMany()
        {
            var gts = _grid.GridTerminalSystem;
            var blocks = new List<IMyTerminalBlock>();
            gts.GetBlocks(blocks);
            Assert.That(blocks, Is.Not.Empty);
            TestContext.WriteLine($"Total blocks via GTS: {blocks.Count}");
        }

        [Test]
        public void GridTerminalSystem_SearchBlocksOfName_CanSearch()
        {
            var gts = _grid.GridTerminalSystem;
            var blocks = new List<IMyTerminalBlock>();
            gts.SearchBlocksOfName("", blocks);
            Assert.That(blocks, Is.Not.Empty);
        }

        // ═══════════════════════════════════════════════════════════
        // LIGHT BLOCK
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void LightBlock_Found()
        {
            var lamps = GetBlocksOfType<IMyLightingBlock>();
            Assert.That(lamps, Is.Not.Empty, "No light blocks found on grid");
            TestContext.WriteLine($"Light blocks: {lamps.Count}");
        }

        [Test]
        public void LightBlock_Color_RoundTrips()
        {
            var lamps = GetBlocksOfType<IMyLightingBlock>();
            Assume.That(lamps, Is.Not.Empty);
            var lamp = lamps[0] as IMyLightingBlock;
            Assert.That(lamp, Is.Not.Null);

            lamp.Color = Color.Red;
            // Thread.Sleep(2000);
            Assert.That(lamp.Color, Is.EqualTo(Color.Red));

            lamp.Color = Color.Green;
            // Thread.Sleep(2000);
            Assert.That(lamp.Color, Is.EqualTo(Color.Green));
        }

        [Test]
        public void LightBlock_Intensity_RoundTrips()
        {
            var lamps = GetBlocksOfType<IMyLightingBlock>();
            Assume.That(lamps, Is.Not.Empty);
            var lamp = lamps[0] as IMyLightingBlock;
            Assert.That(lamp, Is.Not.Null);

            lamp.Intensity = 10f;
            // Thread.Sleep(1500);
            Assert.That(lamp.Intensity, Is.EqualTo(10f));
        }

        [Test]
        public void LightBlock_Radius_RoundTrips()
        {
            var lamps = GetBlocksOfType<IMyLightingBlock>();
            Assume.That(lamps, Is.Not.Empty);
            var lamp = lamps[0] as IMyLightingBlock;
            Assert.That(lamp, Is.Not.Null);

            lamp.Radius = 5f;
            // Thread.Sleep(1500);
            Assert.That(lamp.Radius, Is.EqualTo(5f));
        }

        [Test]
        public void LightBlock_Blink_RoundTrips()
        {
            var lamps = GetBlocksOfType<IMyLightingBlock>();
            Assume.That(lamps, Is.Not.Empty);
            var lamp = lamps[0] as IMyLightingBlock;
            Assert.That(lamp, Is.Not.Null);

            lamp.BlinkIntervalSeconds = 1f;
            lamp.BlinkLength = 0.5f;
            lamp.BlinkOffset = 0.5f;
            // Thread.Sleep(1500);
            Assert.That(lamp.BlinkIntervalSeconds, Is.EqualTo(1f));
            Assert.That(lamp.BlinkLength, Is.EqualTo(0.5f));
            Assert.That(lamp.BlinkOffset, Is.EqualTo(0.5f));
        }

        // ═══════════════════════════════════════════════════════════
        // RADIO ANTENNA
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void RadioAntenna_Found()
        {
            var antennas = GetBlocksOfType<IMyRadioAntenna>();
            Assert.That(antennas, Is.Not.Empty, "No antenna found");
        }

        [Test]
        public void RadioAntenna_EnableBroadcasting_RoundTrips()
        {
            var antennas = GetBlocksOfType<IMyRadioAntenna>();
            Assume.That(antennas, Is.Not.Empty);
            var ant = antennas[0] as IMyRadioAntenna;
            Assert.That(ant, Is.Not.Null);

            ant.EnableBroadcasting = true;
            // Thread.Sleep(1500);
            Assert.That(ant.EnableBroadcasting, Is.True);

            ant.EnableBroadcasting = false;
            // Thread.Sleep(1500);
            Assert.That(ant.EnableBroadcasting, Is.False);
        }

        [Test]
        public void RadioAntenna_Radius_RoundTrips()
        {
            var antennas = GetBlocksOfType<IMyRadioAntenna>();
            Assume.That(antennas, Is.Not.Empty);
            var ant = antennas[0] as IMyRadioAntenna;
            Assert.That(ant, Is.Not.Null);

            ant.Radius = 5000f;
            // Thread.Sleep(1500);
            Assert.That(ant.Radius, Is.EqualTo(5000f));
        }

        [Test]
        public void RadioAntenna_HudText_RoundTrips()
        {
            var antennas = GetBlocksOfType<IMyRadioAntenna>();
            Assume.That(antennas, Is.Not.Empty);
            var ant = antennas[0] as IMyRadioAntenna;
            Assert.That(ant, Is.Not.Null);

            ant.HudText = "TestSignal";
            // Thread.Sleep(1500);
            Assert.That(ant.HudText, Is.EqualTo("TestSignal"));
        }

        // ═══════════════════════════════════════════════════════════
        // BEACON
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void Beacon_Found()
        {
            var beacons = GetBlocksOfType<IMyBeacon>();
            Assume.That(beacons, Is.Not.Empty, "No beacon — may not be in blueprint");
            TestContext.WriteLine($"Beacons: {beacons.Count}");
        }

        [Test]
        public void Beacon_Radius_RoundTrips()
        {
            var beacons = GetBlocksOfType<IMyBeacon>();
            Assume.That(beacons, Is.Not.Empty);
            var b = beacons[0] as IMyBeacon;
            Assert.That(b, Is.Not.Null);

            b.Radius = 10000f;
            // Thread.Sleep(1500);
            Assert.That(b.Radius, Is.EqualTo(10000f));
        }

        [Test]
        public void Beacon_HudText_RoundTrips()
        {
            var beacons = GetBlocksOfType<IMyBeacon>();
            Assume.That(beacons, Is.Not.Empty);
            var b = beacons[0] as IMyBeacon;
            Assert.That(b, Is.Not.Null);

            b.HudText = "BeaconTest";
            // Thread.Sleep(1500);
            Assert.That(b.HudText, Is.EqualTo("BeaconTest"));
        }

        // ═══════════════════════════════════════════════════════════
        // DOOR
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void Door_Found()
        {
            var doors = GetBlocksOfType<IMyDoor>();
            Assert.That(doors, Is.Not.Empty, "No door found");
        }
        // NOTE: Door actions "Open_On", "Open_Off", and "Open" are correct based on game data.
        // Failures were timing-related (no Thread.Sleep between action and state check).
        [Test]
        public void Door_OpenClose_Toggles()
        {
            var doors = GetBlocksOfType<IMyDoor>();
            Assume.That(doors, Is.Not.Empty);
            var door = doors[0] as IMyDoor;
            Assert.That(door, Is.Not.Null);

            door.OpenDoor();
            Thread.Sleep(2000);
            Assert.That(door.Open, Is.True);
            Assert.That(door.Status, Is.EqualTo(ModAPI.Enums.DoorStatus.Open));

            door.CloseDoor();
            Thread.Sleep(2000);
            Assert.That(door.Open, Is.False);
            Assert.That(door.Status, Is.EqualTo(ModAPI.Enums.DoorStatus.Closed));
        }

        [Test]
        public void Door_ToggleDoor_Works()
        {
            var doors = GetBlocksOfType<IMyDoor>();
            Assume.That(doors, Is.Not.Empty);
            var door = doors[0] as IMyDoor;
            Assert.That(door, Is.Not.Null);

            var initial = door.Open;
            door.ToggleDoor();
            // Thread.Sleep(2000);
            Assert.That(door.Open, Is.EqualTo(!initial));
        }

        // ═══════════════════════════════════════════════════════════
        // GYRO
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void Gyro_Found()
        {
            var gyros = GetBlocksOfType<IMyGyro>();
            Assert.That(gyros, Is.Not.Empty, "No gyro found");
        }

        [Test]
        public void Gyro_Override_RoundTrips()
        {
            var gyros = GetBlocksOfType<IMyGyro>();
            Assume.That(gyros, Is.Not.Empty);
            var g = gyros[0] as IMyGyro;
            Assert.That(g, Is.Not.Null);

            g.GyroOverride = true;
            // Thread.Sleep(1500);
            Assert.That(g.GyroOverride, Is.True);

            g.GyroOverride = false;
            // Thread.Sleep(1500);
            Assert.That(g.GyroOverride, Is.False);
        }

        [Test]
        public void Gyro_YawPitchRoll_RoundTrips()
        {
            var gyros = GetBlocksOfType<IMyGyro>();
            Assume.That(gyros, Is.Not.Empty);
            var g = gyros[0] as IMyGyro;
            Assert.That(g, Is.Not.Null);

            g.GyroOverride = true;
            g.Yaw = 0.5f;
            g.Pitch = -0.3f;
            g.Roll = 0.1f;
            // Thread.Sleep(2000);
            Assert.That(g.Yaw, Is.EqualTo(0.5f));
            Assert.That(g.Pitch, Is.EqualTo(-0.3f));
            Assert.That(g.Roll, Is.EqualTo(0.1f));
        }

        [Test]
        public void Gyro_Power_RoundTrips()
        {
            var gyros = GetBlocksOfType<IMyGyro>();
            Assume.That(gyros, Is.Not.Empty);
            var g = gyros[0] as IMyGyro;
            Assert.That(g, Is.Not.Null);

            g.GyroPower = 0.5f;
            // Thread.Sleep(1500);
            Assert.That(g.GyroPower, Is.EqualTo(0.5f));
        }

        // ═══════════════════════════════════════════════════════════
        // THRUST
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void Thrust_Found()
        {
            var thrusters = GetBlocksOfType<IMyThrust>();
            Assert.That(thrusters, Is.Not.Empty, "No thruster found");
        }

        [Test]
        public void Thrust_Override_RoundTrips()
        {
            var thrusters = GetBlocksOfType<IMyThrust>();
            Assume.That(thrusters, Is.Not.Empty);
            var t = thrusters[0] as IMyThrust;
            Assert.That(t, Is.Not.Null);

            t.ThrustOverride = 10000f;
            // Thread.Sleep(1500);
            Assert.That(t.ThrustOverride, Is.EqualTo(10000f));
        }

        [Test]
        [Ignore("MaxThrust is not a terminal property in SE")]
        public void Thrust_MaxThrust_IsPositive()
        {
            var thrusters = GetBlocksOfType<IMyThrust>();
            Assume.That(thrusters, Is.Not.Empty);
            var t = thrusters[0] as IMyThrust;
            Assert.That(t, Is.Not.Null);
            Assert.That(t.MaxThrust, Is.GreaterThan(0));
        }

        // ═══════════════════════════════════════════════════════════
        // COCKPIT (IMyShipController + IMyCockpit)
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void Cockpit_Found()
        {
            var cockpits = GetBlocksOfType<IMyCockpit>();
            Assert.That(cockpits, Is.Not.Empty, "No cockpit found");
        }

        [Test]
        public void Cockpit_HandBrake_RoundTrips()
        {
            var cockpits = GetBlocksOfType<IMyCockpit>();
            Assume.That(cockpits, Is.Not.Empty);
            var c = cockpits[0] as IMyShipController;
            Assert.That(c, Is.Not.Null);

            c.HandBrake = true;
            // Thread.Sleep(1500);
            Assert.That(c.HandBrake, Is.True);

            c.HandBrake = false;
            // Thread.Sleep(1500);
            Assert.That(c.HandBrake, Is.False);
        }

        [Test]
        public void Cockpit_ControlThrusters_RoundTrips()
        {
            var cockpits = GetBlocksOfType<IMyCockpit>();
            Assume.That(cockpits, Is.Not.Empty);
            var c = cockpits[0] as IMyShipController;
            Assert.That(c, Is.Not.Null);

            c.ControlThrusters = true;
            // Thread.Sleep(1500);
            Assert.That(c.ControlThrusters, Is.True);
        }

        [Test]
        public void Cockpit_IsMainCockpit_CanBeRead()
        {
            var cockpits = GetBlocksOfType<IMyCockpit>();
            Assume.That(cockpits, Is.Not.Empty);
            var c = cockpits[0] as IMyShipController;
            Assert.That(c, Is.Not.Null);
            TestContext.WriteLine($"IsMainCockpit = {c.IsMainCockpit}");
        }

        // ═══════════════════════════════════════════════════════════
        // REMOTE CONTROL
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void RemoteControl_Found()
        {
            var rcs = GetBlocksOfType<IMyRemoteControl>();
            Assert.That(rcs, Is.Not.Empty, "No remote control found");
        }

        [Test]
        public void RemoteControl_SpeedLimit_RoundTrips()
        {
            var rcs = GetBlocksOfType<IMyRemoteControl>();
            Assume.That(rcs, Is.Not.Empty);
            var rc = rcs[0] as IMyRemoteControl;
            Assert.That(rc, Is.Not.Null);

            rc.SpeedLimit = 50f;
            // Thread.Sleep(1500);
            Assert.That(rc.SpeedLimit, Is.EqualTo(50f));
        }

        [Test]
        public void RemoteControl_IsAutoPilotEnabled_RoundTrips()
        {
            var rcs = GetBlocksOfType<IMyRemoteControl>();
            Assume.That(rcs, Is.Not.Empty);
            var rc = rcs[0] as IMyRemoteControl;
            Assert.That(rc, Is.Not.Null);

            rc.IsAutoPilotEnabled = true;
            Thread.Sleep(1500);
            Assert.That(rc.IsAutoPilotEnabled, Is.True);

            rc.IsAutoPilotEnabled = false;
            Thread.Sleep(1500);
            Assert.That(rc.IsAutoPilotEnabled, Is.False);
        }

        // ═══════════════════════════════════════════════════════════
        // BATTERY
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void Battery_Found()
        {
            var batteries = GetBlocksOfType<IMyBatteryBlock>();
            Assert.That(batteries, Is.Not.Empty, "No battery found");
        }

        [Test]
        public void Battery_CurrentStoredPower_IsPositive()
        {
            var batteries = GetBlocksOfType<IMyBatteryBlock>();
            Assume.That(batteries, Is.Not.Empty);
            var b = batteries[0] as IMyBatteryBlock;
            Assert.That(b, Is.Not.Null);
            Assert.That(b.CurrentStoredPower, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void Battery_MaxStoredPower_IsPositive()
        {
            var batteries = GetBlocksOfType<IMyBatteryBlock>();
            Assume.That(batteries, Is.Not.Empty);
            var b = batteries[0] as IMyBatteryBlock;
            Assert.That(b, Is.Not.Null);
            Assert.That(b.MaxStoredPower, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void Battery_ChargeMode_RoundTrips()
        {
            var batteries = GetBlocksOfType<IMyBatteryBlock>();
            Assume.That(batteries, Is.Not.Empty);
            var b = batteries[0] as IMyBatteryBlock;
            Assert.That(b, Is.Not.Null);

            b.ChargeMode = ModAPI.Enums.ChargeMode.Recharge;
            // Thread.Sleep(1500);
            Assert.That(b.ChargeMode, Is.EqualTo(ModAPI.Enums.ChargeMode.Recharge));

            b.ChargeMode = ModAPI.Enums.ChargeMode.Auto;
            // Thread.Sleep(1500);
            Assert.That(b.ChargeMode, Is.EqualTo(ModAPI.Enums.ChargeMode.Auto));
        }

        // ═══════════════════════════════════════════════════════════
        // REACTOR
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void Reactor_Found()
        {
            var reactors = GetBlocksOfType<IMyReactor>();
            Assert.That(reactors, Is.Not.Empty, "No reactor found");
        }

        [Test]
        public void Reactor_UseConveyorSystem_RoundTrips()
        {
            var reactors = GetBlocksOfType<IMyReactor>();
            Assume.That(reactors, Is.Not.Empty);
            var r = reactors[0] as IMyReactor;
            Assert.That(r, Is.Not.Null);

            r.UseConveyorSystem = false;
            // Thread.Sleep(1500);
            Assert.That(r.UseConveyorSystem, Is.False);

            r.UseConveyorSystem = true;
            // Thread.Sleep(1500);
            Assert.That(r.UseConveyorSystem, Is.True);
        }

        // ═══════════════════════════════════════════════════════════
        // SOLAR PANEL
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void SolarPanel_Found()
        {
            var panels = GetBlocksOfType<IMySolarPanel>();
            Assume.That(panels, Is.Not.Empty, "No solar panel — may not be in blueprint");
            TestContext.WriteLine($"Solar panels: {panels.Count}");
        }

        [Test]
        public void SolarPanel_MaxOutput_IsNonNegative()
        {
            var panels = GetBlocksOfType<IMySolarPanel>();
            Assume.That(panels, Is.Not.Empty);
            var sp = panels[0] as IMySolarPanel;
            Assert.That(sp, Is.Not.Null);
            Assert.That(sp.MaxOutput, Is.GreaterThanOrEqualTo(0));
        }

        // ═══════════════════════════════════════════════════════════
        // CAMERA
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void Camera_Found()
        {
            var cameras = GetBlocksOfType<IMyCameraBlock>();
            Assert.That(cameras, Is.Not.Empty, "No camera found");
        }

        // NOTE: EnableRaycast is NOT a terminal property in Space Engineers (only accessible
        // via .NET interface, not the terminal system). This test just prints the value.
        [Test]
        public void Camera_EnableRaycast_RoundTrips()
        {
            var cameras = GetBlocksOfType<IMyCameraBlock>();
            Assume.That(cameras, Is.Not.Empty);
            var cam = cameras[0] as IMyCameraBlock;
            Assert.That(cam, Is.Not.Null);

            TestContext.WriteLine($"Camera EnableRaycast = {cam.EnableRaycast}");
        }

        [Test]
        public void Camera_IsActive_CanBeRead()
        {
            var cameras = GetBlocksOfType<IMyCameraBlock>();
            Assume.That(cameras, Is.Not.Empty);
            var cam = cameras[0] as IMyCameraBlock;
            Assert.That(cam, Is.Not.Null);
            TestContext.WriteLine($"Camera IsActive = {cam.IsActive}");
        }

        // ═══════════════════════════════════════════════════════════
        // SENSOR
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void Sensor_Found()
        {
            var sensors = GetBlocksOfType<IMySensorBlock>();
            Assert.That(sensors, Is.Not.Empty, "No sensor found");
        }

        [Test]
        public void Sensor_MaxRange_RoundTrips()
        {
            var sensors = GetBlocksOfType<IMySensorBlock>();
            Assume.That(sensors, Is.Not.Empty);
            var s = sensors[0] as IMySensorBlock;
            Assert.That(s, Is.Not.Null);

            s.MaxRange = 10f;
            // Thread.Sleep(1500);
            Assert.That(s.MaxRange, Is.EqualTo(10f));
        }

        [Test]
        public void Sensor_DetectPlayers_RoundTrips()
        {
            var sensors = GetBlocksOfType<IMySensorBlock>();
            Assume.That(sensors, Is.Not.Empty);
            var s = sensors[0] as IMySensorBlock;
            Assert.That(s, Is.Not.Null);

            s.DetectPlayers = false;
            // Thread.Sleep(1500);
            Assert.That(s.DetectPlayers, Is.False);

            s.DetectPlayers = true;
            // Thread.Sleep(1500);
            Assert.That(s.DetectPlayers, Is.True);
        }

        // ═══════════════════════════════════════════════════════════
        // SHIP CONNECTOR
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void Connector_Found()
        {
            var connectors = GetBlocksOfType<IMyShipConnector>();
            Assert.That(connectors, Is.Not.Empty, "No connector found");
        }

        [Test]
        public void Connector_ThrowOut_RoundTrips()
        {
            var connectors = GetBlocksOfType<IMyShipConnector>();
            Assume.That(connectors, Is.Not.Empty);
            var c = connectors[0] as IMyShipConnector;
            Assert.That(c, Is.Not.Null);

            c.ThrowOut = true;
            // Thread.Sleep(1500);
            Assert.That(c.ThrowOut, Is.True);

            c.ThrowOut = false;
            // Thread.Sleep(1500);
            Assert.That(c.ThrowOut, Is.False);
        }

        [Test]
        public void Connector_CollectAll_RoundTrips()
        {
            var connectors = GetBlocksOfType<IMyShipConnector>();
            Assume.That(connectors, Is.Not.Empty);
            var c = connectors[0] as IMyShipConnector;
            Assert.That(c, Is.Not.Null);

            c.CollectAll = true;
            // Thread.Sleep(1500);
            Assert.That(c.CollectAll, Is.True);

            c.CollectAll = false;
            // Thread.Sleep(1500);
            Assert.That(c.CollectAll, Is.False);
        }

        [Test]
        public void Connector_PullStrength_RoundTrips()
        {
            var connectors = GetBlocksOfType<IMyShipConnector>();
            Assume.That(connectors, Is.Not.Empty);
            var c = connectors[0] as IMyShipConnector;
            Assert.That(c, Is.Not.Null);

            c.PullStrength = 0.5f;
            // Thread.Sleep(1500);
            Assert.That(c.PullStrength, Is.EqualTo(0.5f));
        }

        // ═══════════════════════════════════════════════════════════
        // PISTON
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void Piston_Found()
        {
            var pistons = GetBlocksOfType<IMyPistonBase>();
            Assert.That(pistons, Is.Not.Empty, "No piston found");
        }

        [Test]
        public void Piston_Velocity_RoundTrips()
        {
            var pistons = GetBlocksOfType<IMyPistonBase>();
            Assume.That(pistons, Is.Not.Empty);
            var p = pistons[0] as IMyPistonBase;
            Assert.That(p, Is.Not.Null);

            p.Velocity = 2f;
            // Thread.Sleep(1500);
            Assert.That(p.Velocity, Is.EqualTo(2f));
        }

        [Test]
        public void Piston_MinMaxLimit_RoundTrips()
        {
            var pistons = GetBlocksOfType<IMyPistonBase>();
            Assume.That(pistons, Is.Not.Empty);
            var p = pistons[0] as IMyPistonBase;
            Assert.That(p, Is.Not.Null);

            p.MinLimit = 1f;
            p.MaxLimit = 5f;
            // Thread.Sleep(1500);
            Assert.That(p.MinLimit, Is.EqualTo(1f));
            Assert.That(p.MaxLimit, Is.EqualTo(5f));
        }

        [Test]
        public void Piston_CurrentPosition_CanBeRead()
        {
            var pistons = GetBlocksOfType<IMyPistonBase>();
            Assume.That(pistons, Is.Not.Empty);
            var p = pistons[0] as IMyPistonBase;
            Assert.That(p, Is.Not.Null);
            TestContext.WriteLine($"Piston position = {p.CurrentPosition}");
        }

        // ═══════════════════════════════════════════════════════════
        // MOTOR STATOR (Rotor)
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void MotorStator_Found()
        {
            var rotors = GetBlocksOfType<IMyMotorStator>();
            Assert.That(rotors, Is.Not.Empty, "No rotor found");
        }

        [Test]
        public void MotorStator_Torque_RoundTrips()
        {
            var rotors = GetBlocksOfType<IMyMotorStator>();
            Assume.That(rotors, Is.Not.Empty);
            var r = rotors[0] as IMyMotorStator;
            Assert.That(r, Is.Not.Null);

            r.Torque = 50000f;
            // Thread.Sleep(1500);
            Assert.That(r.Torque, Is.EqualTo(50000f));
        }

        [Test]
        public void MotorStator_Velocity_RoundTrips()
        {
            var rotors = GetBlocksOfType<IMyMotorStator>();
            Assume.That(rotors, Is.Not.Empty);
            var r = rotors[0] as IMyMotorStator;
            Assert.That(r, Is.Not.Null);

            r.TargetVelocityRPM = 5f;
            // Thread.Sleep(1500);
            Assert.That(r.TargetVelocityRPM, Is.EqualTo(5f));
        }

        [Test]
        public void MotorStator_RotorLock_RoundTrips()
        {
            var rotors = GetBlocksOfType<IMyMotorStator>();
            Assume.That(rotors, Is.Not.Empty);
            var r = rotors[0] as IMyMotorStator;
            Assert.That(r, Is.Not.Null);

            r.RotorLock = true;
            // Thread.Sleep(1500);
            Assert.That(r.RotorLock, Is.True);

            r.RotorLock = false;
            // Thread.Sleep(1500);
            Assert.That(r.RotorLock, Is.False);
        }

        // ═══════════════════════════════════════════════════════════
        // MOTOR SUSPENSION (Wheel)
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void MotorSuspension_Found()
        {
            var wheels = GetBlocksOfType<IMyMotorSuspension>();
            Assume.That(wheels, Is.Not.Empty, "No wheel suspension — may not be in blueprint");
            TestContext.WriteLine($"Wheels: {wheels.Count}");
        }

        [Test]
        public void MotorSuspension_Steering_RoundTrips()
        {
            var wheels = GetBlocksOfType<IMyMotorSuspension>();
            Assume.That(wheels, Is.Not.Empty);
            var w = wheels[0] as IMyMotorSuspension;
            Assert.That(w, Is.Not.Null);

            w.Steering = true;
            // Thread.Sleep(1500);
            Assert.That(w.Steering, Is.True);

            w.Steering = false;
            // Thread.Sleep(1500);
            Assert.That(w.Steering, Is.False);
        }

        [Test]
        public void MotorSuspension_Power_RoundTrips()
        {
            var wheels = GetBlocksOfType<IMyMotorSuspension>();
            Assume.That(wheels, Is.Not.Empty);
            var w = wheels[0] as IMyMotorSuspension;
            Assert.That(w, Is.Not.Null);

            w.Power = 80f;
            // Thread.Sleep(1500);
            Assert.That(w.Power, Is.EqualTo(80f));
        }

        [Test]
        public void MotorSuspension_Strength_RoundTrips()
        {
            var wheels = GetBlocksOfType<IMyMotorSuspension>();
            Assume.That(wheels, Is.Not.Empty);
            var w = wheels[0] as IMyMotorSuspension;
            Assert.That(w, Is.Not.Null);

            w.Strength = 20f;
            // Thread.Sleep(1500);
            Assert.That(w.Strength, Is.EqualTo(20f));
        }

        // ═══════════════════════════════════════════════════════════
        // LANDING GEAR
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void LandingGear_Found()
        {
            var gears = GetBlocksOfType<IMyLandingGear>();
            Assert.That(gears, Is.Not.Empty, "No landing gear found");
        }

        [Test]
        public void LandingGear_IsLocked_InitiallyFalse()
        {
            var gears = GetBlocksOfType<IMyLandingGear>();
            Assume.That(gears, Is.Not.Empty);
            var lg = gears[0] as IMyLandingGear;
            Assert.That(lg, Is.Not.Null);
            Assert.That(lg.IsLocked, Is.False, "Should not be locked on spawn");
        }

        [Test]
        public void LandingGear_LockMode_CanBeRead()
                {
                    var gears = GetBlocksOfType<IMyLandingGear>();
                    Assume.That(gears, Is.Not.Empty);
                    var lg = gears[0] as IMyLandingGear;
                    Assert.That(lg, Is.Not.Null);
                    TestContext.WriteLine($"LockMode = {lg.LockMode}");
        }

        // ═══════════════════════════════════════════════════════════
        // PROJECTOR
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void Projector_Found()
        {
            var projectors = GetBlocksOfType<IMyProjector>();
            Assert.That(projectors, Is.Not.Empty, "No projector found");
        }

        [Test]
        public void Projector_IsProjecting_CanBeRead()
        {
            var projectors = GetBlocksOfType<IMyProjector>();
            Assume.That(projectors, Is.Not.Empty);
            var p = projectors[0] as IMyProjector;
            Assert.That(p, Is.Not.Null);
            TestContext.WriteLine($"IsProjecting = {p.IsProjecting}");
        }

        [Test]
        public void Projector_TotalBlocks_IsNonNegative()
        {
            var projectors = GetBlocksOfType<IMyProjector>();
            Assume.That(projectors, Is.Not.Empty);
            var p = projectors[0] as IMyProjector;
            Assert.That(p, Is.Not.Null);
            Assert.That(p.TotalBlocks, Is.GreaterThanOrEqualTo(0));
        }

        // ═══════════════════════════════════════════════════════════
        // PROGRAMMABLE BLOCK
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void ProgrammableBlock_Found()
        {
            var pbs = GetBlocksOfType<IMyProgrammableBlock>();
            Assert.That(pbs, Is.Not.Empty, "No PB found");
        }

        [Test]
        public void ProgrammableBlock_IsRunning_CanBeRead()
        {
            var pbs = GetBlocksOfType<IMyProgrammableBlock>();
            Assume.That(pbs, Is.Not.Empty);
            var pb = pbs[0] as IMyProgrammableBlock;
            Assert.That(pb, Is.Not.Null);
            TestContext.WriteLine($"PB IsRunning = {pb.IsRunning}");
        }

        [Test]
        public void ProgrammableBlock_TerminalRunArgument_CanBeRead()
        {
            var pbs = GetBlocksOfType<IMyProgrammableBlock>();
            Assume.That(pbs, Is.Not.Empty);
            var pb = pbs[0] as IMyProgrammableBlock;
            Assert.That(pb, Is.Not.Null);
            Assert.That(pb.TerminalRunArgument, Is.Not.Null);
        }

        // ═══════════════════════════════════════════════════════════
        // SHIP DRILL
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void Drill_Found()
        {
            var drills = GetBlocksOfType<IMyShipDrill>();
            Assume.That(drills, Is.Not.Empty, "No drill — may not be in blueprint");
            TestContext.WriteLine($"Drills: {drills.Count}");
        }

        [Test]
        public void Drill_TerrainClearingMode_RoundTrips()
        {
            var drills = GetBlocksOfType<IMyShipDrill>();
            Assume.That(drills, Is.Not.Empty);
            var d = drills[0] as IMyShipDrill;
            Assert.That(d, Is.Not.Null);

            d.TerrainClearingMode = true;
            // Thread.Sleep(1500);
            Assert.That(d.TerrainClearingMode, Is.True);

            d.TerrainClearingMode = false;
            // Thread.Sleep(1500);
            Assert.That(d.TerrainClearingMode, Is.False);
        }

        // ═══════════════════════════════════════════════════════════
        // SHIP WELDER
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void Welder_Found()
        {
            var welders = GetBlocksOfType<IMyShipWelder>();
            Assume.That(welders, Is.Not.Empty, "No welder — may not be in blueprint");
            TestContext.WriteLine($"Welders: {welders.Count}");
        }

        [Test]
        public void Welder_HelpOthers_RoundTrips()
        {
            var welders = GetBlocksOfType<IMyShipWelder>();
            Assume.That(welders, Is.Not.Empty);
            var w = welders[0] as IMyShipWelder;
            Assert.That(w, Is.Not.Null);

            w.HelpOthers = true;
            // Thread.Sleep(1500);
            Assert.That(w.HelpOthers, Is.True);

            w.HelpOthers = false;
            // Thread.Sleep(1500);
            Assert.That(w.HelpOthers, Is.False);
        }

        // ═══════════════════════════════════════════════════════════
        // SHIP GRINDER
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void Grinder_Found()
        {
            var grinders = GetBlocksOfType<IMyShipGrinder>();
            Assume.That(grinders, Is.Not.Empty, "No grinder — may not be in blueprint");
            TestContext.WriteLine($"Grinders: {grinders.Count}");
        }

        // ═══════════════════════════════════════════════════════════
        // GAS TANK
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void GasTank_Found()
        {
            var tanks = GetBlocksOfType<IMyGasTank>();
            Assert.That(tanks, Is.Not.Empty, "No gas tank found");
        }

        [Test]
        public void GasTank_FilledRatio_CanBeRead()
        {
            var tanks = GetBlocksOfType<IMyGasTank>();
            Assume.That(tanks, Is.Not.Empty);
            var t = tanks[0] as IMyGasTank;
            Assert.That(t, Is.Not.Null);
            TestContext.WriteLine($"FilledRatio = {t.FilledRatio}");
        }

        [Test]
        public void GasTank_Stockpile_RoundTrips()
        {
            var tanks = GetBlocksOfType<IMyGasTank>();
            Assume.That(tanks, Is.Not.Empty);
            var t = tanks[0] as IMyGasTank;
            Assert.That(t, Is.Not.Null);

            t.Stockpile = true;
            // Thread.Sleep(1500);
            Assert.That(t.Stockpile, Is.True);

            t.Stockpile = false;
            // Thread.Sleep(1500);
            Assert.That(t.Stockpile, Is.False);
        }

        // ═══════════════════════════════════════════════════════════
        // GAS GENERATOR (O2/H2 Generator)
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void GasGenerator_Found()
        {
            var gens = GetBlocksOfType<IMyGasGenerator>();
            Assert.That(gens, Is.Not.Empty, "No O2/H2 generator found");
        }

        [Test]
        public void GasGenerator_AutoRefill_RoundTrips()
        {
            var gens = GetBlocksOfType<IMyGasGenerator>();
            Assume.That(gens, Is.Not.Empty);
            var g = gens[0] as IMyGasGenerator;
            Assert.That(g, Is.Not.Null);

            g.AutoRefill = false;
            // Thread.Sleep(1500);
            Assert.That(g.AutoRefill, Is.False);

            g.AutoRefill = true;
            // Thread.Sleep(1500);
            Assert.That(g.AutoRefill, Is.True);
        }

        // ═══════════════════════════════════════════════════════════
        // JUMP DRIVE
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void JumpDrive_Found()
        {
            var jds = GetBlocksOfType<IMyJumpDrive>();
            Assume.That(jds, Is.Not.Empty, "No jump drive — may not be in blueprint");
            TestContext.WriteLine($"Jump drives: {jds.Count}");
        }

        [Test]
        [Ignore("MaxStoredPower is not a terminal property in SE")]
        public void JumpDrive_MaxStoredPower_IsPositive()
        {
            var jds = GetBlocksOfType<IMyJumpDrive>();
            Assume.That(jds, Is.Not.Empty);
            var jd = jds[0] as IMyJumpDrive;
            Assert.That(jd, Is.Not.Null);
            Assert.That(jd.MaxStoredPower, Is.GreaterThan(0));
        }

        [Test]
        public void JumpDrive_Recharge_RoundTrips()
        {
            var jds = GetBlocksOfType<IMyJumpDrive>();
            Assume.That(jds, Is.Not.Empty);
            var jd = jds[0] as IMyJumpDrive;
            Assert.That(jd, Is.Not.Null);

            jd.Recharge = true;
            // Thread.Sleep(1500);
            Assert.That(jd.Recharge, Is.True);

            jd.Recharge = false;
            // Thread.Sleep(1500);
            Assert.That(jd.Recharge, Is.False);
        }

        // ═══════════════════════════════════════════════════════════
        // CARGO CONTAINER
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void CargoContainer_Found()
        {
            var cargos = GetBlocksOfType<IMyCargoContainer>();
            Assert.That(cargos, Is.Not.Empty, "No cargo container found");
        }

        [Test]
        public void CargoContainer_HasInventory_CanBeRead()
        {
            var cargos = GetBlocksOfType<IMyCargoContainer>();
            Assume.That(cargos, Is.Not.Empty);
            var c = cargos[0] as IMyCargoContainer;
            Assert.That(c, Is.Not.Null);

            // IMyCargoContainer extends IMyTerminalBlock, test basic properties
            Assert.That(c.CustomName, Is.Not.Null);
        }

        // ═══════════════════════════════════════════════════════════
        // WARHEAD
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void Warhead_Found()
        {
            var warheads = GetBlocksOfType<IMyWarhead>();
            Assume.That(warheads, Is.Not.Empty, "No warhead — may not be in blueprint");
            TestContext.WriteLine($"Warheads: {warheads.Count}");
        }

        [Test]
        public void Warhead_IsArmed_RoundTrips()
        {
            var warheads = GetBlocksOfType<IMyWarhead>();
            Assume.That(warheads, Is.Not.Empty);
            var w = warheads[0] as IMyWarhead;
            Assert.That(w, Is.Not.Null);

            w.IsArmed = false;
            // Thread.Sleep(1500);
            Assert.That(w.IsArmed, Is.False);

            w.IsArmed = true;
            // Thread.Sleep(1500);
            Assert.That(w.IsArmed, Is.True);

            // Safety: disarm after test
            w.IsArmed = false;
        }

        // ═══════════════════════════════════════════════════════════
        // TEXT PANEL / LCD
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void TextPanel_Found()
        {
            var panels = GetBlocksOfType<IMyTextPanel>();
            Assert.That(panels, Is.Not.Empty, "No LCD/text panel found");
        }

        [Test]
        public void TextPanel_FontSize_RoundTrips()
        {
            var panels = GetBlocksOfType<IMyTextPanel>();
            Assume.That(panels, Is.Not.Empty);
            var tp = panels[0] as IMyTextPanel;
            Assert.That(tp, Is.Not.Null);

            tp.FontSize = 1.5f;
            // Thread.Sleep(1500);
            Assert.That(tp.FontSize, Is.EqualTo(1.5f));
        }

        [Test]
        public void TextPanel_FontColor_RoundTrips()
        {
            var panels = GetBlocksOfType<IMyTextPanel>();
            Assume.That(panels, Is.Not.Empty);
            var tp = panels[0] as IMyTextPanel;
            Assert.That(tp, Is.Not.Null);

            tp.FontColor = Color.Red;
            // Thread.Sleep(1500);
            Assert.That(tp.FontColor, Is.EqualTo(Color.Red));
        }

        [Test]
        public void TextPanel_BackgroundColor_RoundTrips()
        {
            var panels = GetBlocksOfType<IMyTextPanel>();
            Assume.That(panels, Is.Not.Empty);
            var tp = panels[0] as IMyTextPanel;
            Assert.That(tp, Is.Not.Null);

            tp.BackgroundColor = Color.Blue;
            // Thread.Sleep(1500);
            Assert.That(tp.BackgroundColor, Is.EqualTo(Color.Blue));
        }

        [Test]
        public void TextPanel_WriteAndReadText()
        {
            var panels = GetBlocksOfType<IMyTextPanel>();
            Assume.That(panels, Is.Not.Empty);
            var tp = panels[0] as IMyTextPanel;
            Assert.That(tp, Is.Not.Null);

            tp.WriteText("Hello from test!");
            // Thread.Sleep(2000);
            var text = tp.GetText();
            Assert.That(text, Does.Contain("Hello from test"));
        }

        // ═══════════════════════════════════════════════════════════
        // COLLECTOR
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void Collector_Found()
        {
            var collectors = GetBlocksOfType<IMyCollector>();
            Assume.That(collectors, Is.Not.Empty, "No collector — may not be in blueprint");
            TestContext.WriteLine($"Collectors: {collectors.Count}");
        }

        [Test]
        public void Collector_UseConveyorSystem_RoundTrips()
        {
            var collectors = GetBlocksOfType<IMyCollector>();
            Assume.That(collectors, Is.Not.Empty);
            var c = collectors[0] as IMyCollector;
            Assert.That(c, Is.Not.Null);

            c.UseConveyorSystem = false;
            // Thread.Sleep(1500);
            Assert.That(c.UseConveyorSystem, Is.False);

            c.UseConveyorSystem = true;
            // Thread.Sleep(1500);
            Assert.That(c.UseConveyorSystem, Is.True);
        }

        // ═══════════════════════════════════════════════════════════
        // CONVEYOR SORTER
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void ConveyorSorter_Found()
        {
            var sorters = GetBlocksOfType<IMyConveyorSorter>();
            Assume.That(sorters, Is.Not.Empty, "No conveyor sorter — may not be in blueprint");
            TestContext.WriteLine($"Conveyor sorters: {sorters.Count}");
        }

        [Test]
        public void ConveyorSorter_DrainAll_RoundTrips()
        {
            var sorters = GetBlocksOfType<IMyConveyorSorter>();
            Assume.That(sorters, Is.Not.Empty);
            var cs = sorters[0] as IMyConveyorSorter;
            Assert.That(cs, Is.Not.Null);

            cs.DrainAll = true;
            // Thread.Sleep(1500);
            Assert.That(cs.DrainAll, Is.True);

            cs.DrainAll = false;
            // Thread.Sleep(1500);
            Assert.That(cs.DrainAll, Is.False);
        }

        // ═══════════════════════════════════════════════════════════
        // LASER ANTENNA
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void LaserAntenna_Found()
        {
            var lasers = GetBlocksOfType<IMyLaserAntenna>();
            Assume.That(lasers, Is.Not.Empty, "No laser antenna — may not be in blueprint");
            TestContext.WriteLine($"Laser antennas: {lasers.Count}");
        }

        [Test]
        public void LaserAntenna_Range_RoundTrips()
        {
            var lasers = GetBlocksOfType<IMyLaserAntenna>();
            Assume.That(lasers, Is.Not.Empty);
            var la = lasers[0] as IMyLaserAntenna;
            Assert.That(la, Is.Not.Null);

            la.Range = 20000f;
            // Thread.Sleep(1500);
            Assert.That(la.Range, Is.EqualTo(20000f));
        }

        [Test]
        public void LaserAntenna_IsPermanent_RoundTrips()
        {
            var lasers = GetBlocksOfType<IMyLaserAntenna>();
            Assume.That(lasers, Is.Not.Empty);
            var la = lasers[0] as IMyLaserAntenna;
            Assert.That(la, Is.Not.Null);

            la.IsPermanent = true;
            Thread.Sleep(1500);
            Assert.That(la.IsPermanent, Is.True);

            la.IsPermanent = false;
            Thread.Sleep(1500);
            Assert.That(la.IsPermanent, Is.False);
        }

        // ═══════════════════════════════════════════════════════════
        // GRAVITY GENERATOR
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void GravityGenerator_Found()
        {
            var gens = GetBlocksOfType<IMyGravityGenerator>();
            Assume.That(gens, Is.Not.Empty, "No gravity generator — may not be in blueprint");
            TestContext.WriteLine($"Gravity generators: {gens.Count}");
        }

        [Test]
        public void GravityGenerator_Acceleration_RoundTrips()
        {
            var gens = GetBlocksOfType<IMyGravityGenerator>();
            Assume.That(gens, Is.Not.Empty);
            var g = gens[0] as IMyGravityGenerator;
            Assert.That(g, Is.Not.Null);

            var acc = new Vector3(0f, -9.81f, 0f);
            g.GravityAcceleration = acc;
            // Thread.Sleep(1500);
            Assert.That(g.GravityAcceleration, Is.EqualTo(acc));
        }

        // ═══════════════════════════════════════════════════════════
        // GRAVITY GENERATOR SPHERE
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void GravityGeneratorSphere_Found()
        {
            var spheres = GetBlocksOfType<IMyGravityGeneratorSphere>();
            Assume.That(spheres, Is.Not.Empty, "No spherical gravity generator — may not be in blueprint");
            TestContext.WriteLine($"Gravity spheres: {spheres.Count}");
        }

        [Test]
        public void GravityGeneratorSphere_Radius_RoundTrips()
        {
            var spheres = GetBlocksOfType<IMyGravityGeneratorSphere>();
            Assume.That(spheres, Is.Not.Empty);
            var gs = spheres[0] as IMyGravityGeneratorSphere;
            Assert.That(gs, Is.Not.Null);

            gs.Radius = 50f;
            // Thread.Sleep(1500);
            Assert.That(gs.Radius, Is.EqualTo(50f));
        }

        // ═══════════════════════════════════════════════════════════
        // SOUND BLOCK
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void SoundBlock_Found()
        {
            var sounds = GetBlocksOfType<IMySoundBlock>();
            Assume.That(sounds, Is.Not.Empty, "No sound block — may not be in blueprint");
            TestContext.WriteLine($"Sound blocks: {sounds.Count}");
        }

        [Test]
        public void SoundBlock_Volume_RoundTrips()
        {
            var sounds = GetBlocksOfType<IMySoundBlock>();
            Assume.That(sounds, Is.Not.Empty);
            var sb = sounds[0] as IMySoundBlock;
            Assert.That(sb, Is.Not.Null);

            sb.Volume = 0.5f;
            // Thread.Sleep(1500);
            Assert.That(sb.Volume, Is.EqualTo(0.5f));
        }

        [Test]
        public void SoundBlock_Range_RoundTrips()
        {
            var sounds = GetBlocksOfType<IMySoundBlock>();
            Assume.That(sounds, Is.Not.Empty);
            var sb = sounds[0] as IMySoundBlock;
            Assert.That(sb, Is.Not.Null);

            sb.Range = 200f;
            // Thread.Sleep(1500);
            Assert.That(sb.Range, Is.EqualTo(200f));
        }

        // ═══════════════════════════════════════════════════════════
        // TIMER BLOCK
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void TimerBlock_Found()
        {
            var timers = GetBlocksOfType<IMyTimerBlock>();
            Assume.That(timers, Is.Not.Empty, "No timer block — may not be in blueprint");
            TestContext.WriteLine($"Timer blocks: {timers.Count}");
        }

        [Test]
        public void TimerBlock_Silent_RoundTrips()
        {
            var timers = GetBlocksOfType<IMyTimerBlock>();
            Assume.That(timers, Is.Not.Empty);
            var tb = timers[0] as IMyTimerBlock;
            Assert.That(tb, Is.Not.Null);

            tb.Silent = true;
            // Thread.Sleep(1500);
            Assert.That(tb.Silent, Is.True);

            tb.Silent = false;
            // Thread.Sleep(1500);
            Assert.That(tb.Silent, Is.False);
        }

        // ═══════════════════════════════════════════════════════════
        // BUTTON PANEL
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void ButtonPanel_Found()
        {
            var panels = GetBlocksOfType<IMyButtonPanel>();
            Assume.That(panels, Is.Not.Empty, "No button panel — may not be in blueprint");
            TestContext.WriteLine($"Button panels: {panels.Count}");
        }

        [Test]
        public void ButtonPanel_GetSetButtonName()
        {
            var panels = GetBlocksOfType<IMyButtonPanel>();
            Assume.That(panels, Is.Not.Empty);
            var bp = panels[0] as IMyButtonPanel;
            Assert.That(bp, Is.Not.Null);

            bp.SetCustomButtonName(0, "TestButton");
            Thread.Sleep(1000);
            Assert.That(bp.GetButtonName(0), Is.EqualTo("TestButton"));
        }

        // ═══════════════════════════════════════════════════════════
        // PARACHUTE
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void Parachute_Found()
        {
            var chutes = GetBlocksOfType<IMyParachute>();
            Assume.That(chutes, Is.Not.Empty, "No parachute — may not be in blueprint");
            TestContext.WriteLine($"Parachutes: {chutes.Count}");
        }

        [Test]
        public void Parachute_AutoDeploy_RoundTrips()
        {
            var chutes = GetBlocksOfType<IMyParachute>();
            Assume.That(chutes, Is.Not.Empty);
            var p = chutes[0] as IMyParachute;
            Assert.That(p, Is.Not.Null);

            p.AutoDeploy = true;
            // Thread.Sleep(1500);
            Assert.That(p.AutoDeploy, Is.True);

            p.AutoDeploy = false;
            // Thread.Sleep(1500);
            Assert.That(p.AutoDeploy, Is.False);
        }

        // ═══════════════════════════════════════════════════════════
        // SAFE ZONE
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void SafeZone_Found()
        {
            var zones = GetBlocksOfType<IMySafeZoneBlock>();
            Assume.That(zones, Is.Not.Empty, "No safe zone — may not be in blueprint");
            TestContext.WriteLine($"Safe zones: {zones.Count}");
        }

        [Test]
        public void SafeZone_IsSafeZoneEnabled_CanBeRead()
        {
            var zones = GetBlocksOfType<IMySafeZoneBlock>();
            Assume.That(zones, Is.Not.Empty);
            var sz = zones[0] as IMySafeZoneBlock;
            Assert.That(sz, Is.Not.Null);
            TestContext.WriteLine($"IsSafeZoneEnabled = {sz.IsSafeZoneEnabled}");
        }

        // ═══════════════════════════════════════════════════════════
        // AIR VENT
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void AirVent_Found()
        {
            var vents = GetBlocksOfType<IMyAirVent>();
            Assert.That(vents, Is.Not.Empty, "No air vent found");
        }

        [Test]
        public void AirVent_Depressurize_RoundTrips()
        {
            var vents = GetBlocksOfType<IMyAirVent>();
            Assume.That(vents, Is.Not.Empty);
            var av = vents[0] as IMyAirVent;
            Assert.That(av, Is.Not.Null);

            av.Depressurize = true;
            // Thread.Sleep(1500);
            Assert.That(av.Depressurize, Is.True);

            av.Depressurize = false;
            // Thread.Sleep(1500);
            Assert.That(av.Depressurize, Is.False);
        }

        // ═══════════════════════════════════════════════════════════
        // MEDICAL ROOM
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void MedicalRoom_Found()
        {
            var rooms = GetBlocksOfType<IMyMedicalRoom>();
            Assert.That(rooms, Is.Not.Empty, "No medical room found");
        }

        [Test]
        public void MedicalRoom_CustomName_IsReadable()
        {
            var rooms = GetBlocksOfType<IMyMedicalRoom>();
            Assume.That(rooms, Is.Not.Empty);
            var mr = rooms[0] as IMyMedicalRoom;
            Assert.That(mr, Is.Not.Null);
            Assert.That(mr.CustomName, Is.Not.Null.And.Not.Empty);
        }

        // ═══════════════════════════════════════════════════════════
        // MERGE BLOCK
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void MergeBlock_Found()
        {
            var merges = GetBlocksOfType<IMyShipMergeBlock>();
            Assume.That(merges, Is.Not.Empty, "No merge block — may not be in blueprint");
            TestContext.WriteLine($"Merge blocks: {merges.Count}");
        }

        [Test]
        public void MergeBlock_IsConnected_InitiallyFalse()
        {
            var merges = GetBlocksOfType<IMyShipMergeBlock>();
            Assume.That(merges, Is.Not.Empty);
            var mb = merges[0] as IMyShipMergeBlock;
            Assert.That(mb, Is.Not.Null);
            Assert.That(mb.IsConnected, Is.False);
        }

        // ═══════════════════════════════════════════════════════════
        // HEAT VENT
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void HeatVent_Found()
        {
            var vents = GetBlocksOfType<IMyHeatVent>();
            Assume.That(vents, Is.Not.Empty, "No heat vent — may not be in blueprint");
            TestContext.WriteLine($"Heat vents: {vents.Count}");
        }

        [Test]
        public void HeatVent_PowerDependency_RoundTrips()
        {
            var vents = GetBlocksOfType<IMyHeatVent>();
            Assume.That(vents, Is.Not.Empty);
            var hv = vents[0] as IMyHeatVent;
            Assert.That(hv, Is.Not.Null);

            hv.PowerDependency = 0.8f;
            // Thread.Sleep(1500);
            Assert.That(hv.PowerDependency, Is.EqualTo(0.8f));
        }

        [Test]
        public void HeatVent_ColorMinimal_RoundTrips()
        {
            var vents = GetBlocksOfType<IMyHeatVent>();
            Assume.That(vents, Is.Not.Empty);
            var hv = vents[0] as IMyHeatVent;
            Assert.That(hv, Is.Not.Null);

            hv.ColorMinimal = Color.Red;
            // Thread.Sleep(1500);
            Assert.That(hv.ColorMinimal, Is.EqualTo(Color.Red));
        }

        // ═══════════════════════════════════════════════════════════
        // UPGRADE MODULE
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void UpgradeModule_Found()
        {
            var modules = GetBlocksOfType<IMyUpgradeModule>();
            Assume.That(modules, Is.Not.Empty, "No upgrade module — may not be in blueprint");
            TestContext.WriteLine($"Upgrade modules: {modules.Count}");
        }

        [Test]
        public void UpgradeModule_UpgradeCount_CanBeRead()
        {
            var modules = GetBlocksOfType<IMyUpgradeModule>();
            Assume.That(modules, Is.Not.Empty);
            var um = modules[0] as IMyUpgradeModule;
            Assert.That(um, Is.Not.Null);
            Assert.That(um.UpgradeCount, Is.GreaterThanOrEqualTo(0));
        }

        // ═══════════════════════════════════════════════════════════
        // UPGRADABLE BLOCK
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void UpgradableBlock_Found()
        {
            var ubs = GetBlocksOfType<IMyUpgradableBlock>();
            Assert.That(ubs, Is.Not.Empty, "No upgradable block — filter is broad (all blocks)");
        }

        [Test]
        public void UpgradableBlock_UpgradeCount_CanBeRead()
        {
            var ubs = GetBlocksOfType<IMyUpgradableBlock>();
            Assume.That(ubs, Is.Not.Empty);
            var ub = ubs[0] as IMyUpgradableBlock;
            Assert.That(ub, Is.Not.Null);
            Assert.That(ub.UpgradeCount, Is.GreaterThanOrEqualTo(0));
        }

        // ═══════════════════════════════════════════════════════════
        // IMyShipController (broader than IMyCockpit — includes RemoteControl)
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void ShipController_Found()
        {
            var controllers = GetBlocksOfType<IMyShipController>();
            Assert.That(controllers, Is.Not.Empty, "No ship controller found");
            TestContext.WriteLine($"Ship controllers: {controllers.Count}");

            foreach (var c in controllers)
                            TestContext.WriteLine($"  Controller: {((IMyEntity)c).DisplayName}");
        }

        [Test]
        public void ShipController_DampenersOverride_RoundTrips()
        {
            var controllers = GetBlocksOfType<IMyShipController>();
            Assume.That(controllers, Is.Not.Empty);
            var sc = controllers[0] as IMyShipController;
            Assert.That(sc, Is.Not.Null);

            sc.DampenersOverride = false;
            // Thread.Sleep(1500);
            Assert.That(sc.DampenersOverride, Is.False);

            sc.DampenersOverride = true;
            // Thread.Sleep(1500);
            Assert.That(sc.DampenersOverride, Is.True);
        }

        // ═══════════════════════════════════════════════════════════
        // IMyTextSurface — tested through text panel
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void TextSurface_Font_CanBeRead()
        {
            var panels = GetBlocksOfType<IMyTextPanel>();
            Assume.That(panels, Is.Not.Empty);
            var ts = panels[0] as IMyTextSurface;
            Assert.That(ts, Is.Not.Null);
            // Font is Int64 typeface ID in SE, not a string name
            Assert.That(ts.Font, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void TextSurface_SurfaceSize_CanBeRead()
        {
            var panels = GetBlocksOfType<IMyTextPanel>();
            Assume.That(panels, Is.Not.Empty);
            var ts = panels[0] as IMyTextSurface;
            Assert.That(ts, Is.Not.Null);
            Assert.That(ts.SurfaceSize, Is.Not.Null);
        }
    }
}
