using System;
using System.Collections.Generic;
using System.Linq;
using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    public class GridTerminalSystemProxy : IMyGridTerminalSystem
    {
        private readonly IPbTestHarness _harness;
        private readonly long _gridId;
        private readonly Dictionary<string, BlockDto> _blocksByName;
        private readonly List<BlockDto> _allBlocks;

        public GridTerminalSystemProxy(IPbTestHarness harness, long gridId, Dictionary<string, BlockDto> blocksByName)
        {
            _harness = harness;
            _gridId = gridId;
            _blocksByName = blocksByName;
            _allBlocks = blocksByName.Values.ToList();
        }

        public void GetBlocks(List<IMyTerminalBlock> blocks)
        {
            blocks.Clear();
            foreach (var b in _allBlocks)
                blocks.Add(CreateProxy(b));
        }

        public void GetBlockGroups(List<IMyBlockGroup> groups) { groups?.Clear(); }

        public void GetBlocksOfType<T>(List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect = null)
        {
            blocks.Clear();
            var typeName = typeof(T).Name; // e.g. "IMyLightingBlock"

            // Map interface name to block type/subtype patterns
            var filter = GetTypeFilter(typeName);
            foreach (var b in _allBlocks)
            {
                if (!filter(b)) continue;
                var proxy = CreateProxy(b);
                if (collect == null || collect(proxy))
                    blocks.Add(proxy);
            }
        }

        public void SearchBlocksOfName(string name, List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect = null)
        {
            blocks.Clear();
            foreach (var kv in _blocksByName)
            {
                if (kv.Key.IndexOf(name, StringComparison.OrdinalIgnoreCase) < 0) continue;
                var proxy = CreateProxy(kv.Value);
                if (collect == null || collect(proxy))
                    blocks.Add(proxy);
            }
        }

        public IMyTerminalBlock GetBlockWithName(string name)
        {
            if (_blocksByName.TryGetValue(name, out var block))
                return CreateProxy(block);
            return null;
        }

        public IMyBlockGroup GetBlockGroupWithName(string name) => null;

        private IMyTerminalBlock CreateProxy(BlockDto block)
        {
            // Create the most specific proxy based on block type
            var type = block.Type ?? "";

            // Map known types to proxy classes
            if (IsType(type, "Light"))              return new LightBlockProxy(_harness, _gridId, block);
            if (IsType(type, "RadioAntenna"))        return new RadioAntennaProxy(_harness, _gridId, block);
            if (IsType(type, "Beacon"))              return new BeaconProxy(_harness, _gridId, block);
            if (IsType(type, "Door"))                return new DoorProxy(_harness, _gridId, block);
            if (IsType(type, "Gyro"))                return new GyroProxy(_harness, _gridId, block);
            if (IsType(type, "Thrust"))              return new ThrustProxy(_harness, _gridId, block);
            if (IsType(type, "Cockpit") || IsType(type, "ShipController"))
                                                     return new CockpitProxy(_harness, _gridId, block);
            if (IsType(type, "RemoteControl"))       return new RemoteControlProxy(_harness, _gridId, block);
            if (IsType(type, "Battery"))             return new BatteryBlockProxy(_harness, _gridId, block);
            if (IsType(type, "Reactor"))             return new ReactorProxy(_harness, _gridId, block);
            if (IsType(type, "Camera"))              return new CameraBlockProxy(_harness, _gridId, block);
            if (IsType(type, "Sensor"))              return new SensorBlockProxy(_harness, _gridId, block);
            if (IsType(type, "Connector"))           return new ShipConnectorProxy(_harness, _gridId, block);
            if (IsType(type, "Piston"))              return new PistonBaseProxy(_harness, _gridId, block);
            if (IsType(type, "MotorStator") || IsType(type, "MotorAdvancedStator") || IsType(type, "Rotor"))
                                                     return new MotorStatorProxy(_harness, _gridId, block);
            if (IsType(type, "MotorSuspension") || IsType(type, "Wheel"))
                                                     return new MotorSuspensionProxy(_harness, _gridId, block);
            if (IsType(type, "Projector"))           return new ProjectorProxy(_harness, _gridId, block);
            if (IsType(type, "ProgrammableBlock"))   return new ProgrammableBlockProxy(_harness, _gridId, block);
            if (IsType(type, "Drill"))               return new ShipDrillProxy(_harness, _gridId, block);
            if (IsType(type, "Welder"))              return new ShipWelderProxy(_harness, _gridId, block);
            if (IsType(type, "Grinder"))             return new ShipGrinderProxy(_harness, _gridId, block);
            if (IsType(type, "GasTank") || IsType(type, "OxygenTank") || IsType(type, "HydrogenTank"))
                                                     return new GasTankProxy(_harness, _gridId, block);
            if (IsType(type, "GasGenerator") || IsType(type, "OxygenGenerator"))
                                                     return new GasGeneratorProxy(_harness, _gridId, block);
            if (IsType(type, "JumpDrive"))           return new JumpDriveProxy(_harness, _gridId, block);
            if (IsType(type, "CargoContainer"))      return new CargoContainerProxy(_harness, _gridId, block);
            if (IsType(type, "Warhead"))             return new WarheadProxy(_harness, _gridId, block);
            if (IsType(type, "TextPanel") || IsType(type, "LCD"))
                                                     return new TextPanelProxy(_harness, _gridId, block);
            if (IsType(type, "Collector"))           return new CollectorProxy(_harness, _gridId, block);
            if (IsType(type, "ConveyorSorter"))      return new ConveyorSorterProxy(_harness, _gridId, block);
            if (IsType(type, "LaserAntenna"))        return new LaserAntennaProxy(_harness, _gridId, block);
            if (IsType(type, "GravityGenerator") && !IsType(type, "Sphere"))
                                                     return new GravityGeneratorProxy(_harness, _gridId, block);
            if (IsType(type, "GravityGeneratorSphere"))
                                                     return new GravityGeneratorSphereProxy(_harness, _gridId, block);
            if (IsType(type, "LandingGear"))         return new LandingGearProxy(_harness, _gridId, block);
            if (IsType(type, "SoundBlock"))          return new SoundBlockProxy(_harness, _gridId, block);
            if (IsType(type, "TimerBlock"))          return new TimerBlockProxy(_harness, _gridId, block);
            if (IsType(type, "ButtonPanel"))         return new ButtonPanelProxy(_harness, _gridId, block);
            if (IsType(type, "Parachute"))           return new ParachuteProxy(_harness, _gridId, block);
            if (IsType(type, "SafeZone"))            return new SafeZoneBlockProxy(_harness, _gridId, block);
            if (IsType(type, "AirVent"))             return new AirVentProxy(_harness, _gridId, block);
            if (IsType(type, "MergeBlock"))          return new ShipMergeBlockProxy(_harness, _gridId, block);
            if (IsType(type, "MedicalRoom"))         return new MedicalRoomProxy(_harness, _gridId, block);
            if (IsType(type, "SolarPanel"))          return new SolarPanelProxy(_harness, _gridId, block);
            if (IsType(type, "HeatVent"))            return new HeatVentProxy(_harness, _gridId, block);
            if (IsType(type, "UpgradeModule"))       return new UpgradeModuleProxy(_harness, _gridId, block);

            // Fallback: functional or terminal
            if (IsFunctionalType(type))
                return new FunctionalBlockProxy(_harness, _gridId, block);
            return new TerminalBlockProxy(_harness, _gridId, block);
        }

        private static bool IsType(string type, string match)
        {
            return type.IndexOf(match, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsFunctionalType(string type)
        {
            // Most block types are functional
            return !IsType(type, "CargoContainer") && !IsType(type, "ConveyorTube")
                && !IsType(type, "Passage") && !IsType(type, "Conveyor");
        }

        private static Func<BlockDto, bool> GetTypeFilter(string interfaceName)
        {
            switch (interfaceName)
            {
                case "IMyLightingBlock": return b => IsType(b.Type, "Light");
                case "IMyRadioAntenna": return b => IsType(b.Type, "RadioAntenna");
                case "IMyBeacon": return b => IsType(b.Type, "Beacon");
                case "IMyDoor": return b => IsType(b.Type, "Door");
                case "IMyGyro": return b => IsType(b.Type, "Gyro");
                case "IMyThrust": return b => IsType(b.Type, "Thrust");
                case "IMyShipController": return b => IsType(b.Type, "Cockpit") || IsType(b.Type, "ShipController") || IsType(b.Type, "RemoteControl");
                case "IMyCockpit": return b => IsType(b.Type, "Cockpit");
                case "IMyRemoteControl": return b => IsType(b.Type, "RemoteControl");
                case "IMyBatteryBlock": return b => IsType(b.Type, "Battery");
                case "IMyReactor": return b => IsType(b.Type, "Reactor");
                case "IMyCameraBlock": return b => IsType(b.Type, "Camera");
                case "IMySensorBlock": return b => IsType(b.Type, "Sensor");
                case "IMyShipConnector": return b => IsType(b.Type, "Connector");
                case "IMyPistonBase": return b => IsType(b.Type, "Piston");
                case "IMyMotorStator": return b => IsType(b.Type, "MotorStator") || IsType(b.Type, "Rotor");
                case "IMyMotorSuspension": return b => IsType(b.Type, "MotorSuspension") || IsType(b.Type, "Wheel");
                case "IMyProjector": return b => IsType(b.Type, "Projector");
                case "IMyProgrammableBlock": return b => IsType(b.Type, "ProgrammableBlock");
                case "IMyShipDrill": return b => IsType(b.Type, "Drill");
                case "IMyShipWelder": return b => IsType(b.Type, "Welder");
                case "IMyShipGrinder": return b => IsType(b.Type, "Grinder");
                case "IMyGasTank": return b => IsType(b.Type, "GasTank") || IsType(b.Type, "OxygenTank") || IsType(b.Type, "HydrogenTank");
                case "IMyGasGenerator": return b => IsType(b.Type, "GasGenerator") || IsType(b.Type, "OxygenGenerator");
                case "IMyJumpDrive": return b => IsType(b.Type, "JumpDrive");
                case "IMyCargoContainer": return b => IsType(b.Type, "CargoContainer");
                case "IMyWarhead": return b => IsType(b.Type, "Warhead");
                case "IMyTextPanel": return b => IsType(b.Type, "TextPanel") || IsType(b.Type, "LCD");
                case "IMyCollector": return b => IsType(b.Type, "Collector");
                case "IMyConveyorSorter": return b => IsType(b.Type, "ConveyorSorter");
                case "IMyLaserAntenna": return b => IsType(b.Type, "LaserAntenna");
                case "IMyGravityGenerator": return b => IsType(b.Type, "GravityGenerator") && !IsType(b.Type, "Sphere");
                case "IMyGravityGeneratorSphere": return b => IsType(b.Type, "GravityGeneratorSphere");
                case "IMyLandingGear": return b => IsType(b.Type, "LandingGear");
                case "IMySoundBlock": return b => IsType(b.Type, "SoundBlock");
                case "IMyTimerBlock": return b => IsType(b.Type, "TimerBlock");
                case "IMyButtonPanel": return b => IsType(b.Type, "ButtonPanel");
                case "IMyParachute": return b => IsType(b.Type, "Parachute");
                case "IMySafeZoneBlock": return b => IsType(b.Type, "SafeZone");
                case "IMyAirVent": return b => IsType(b.Type, "AirVent");
                case "IMyShipMergeBlock": return b => IsType(b.Type, "MergeBlock");
                case "IMyMedicalRoom": return b => IsType(b.Type, "MedicalRoom");
                case "IMySolarPanel": return b => IsType(b.Type, "SolarPanel");
                case "IMyHeatVent": return b => IsType(b.Type, "HeatVent");
                case "IMyUpgradableBlock": return b => true; // Many blocks can be upgraded
                case "IMyUpgradeModule": return b => IsType(b.Type, "UpgradeModule");
                default: return b => true; // IMyTerminalBlock / IMyFunctionalBlock → all blocks
            }
        }
    }
}
