# API Reference

## `PbTestHarnessFactory`

Static factory class for creating `IPbTestHarness`.

```csharp
public static IPbTestHarness Create(string scriptCode)
```

| Parameter | Description |
|---|---|
| `scriptCode` | Script source code to upload to the Programmable Block. Pass `"// no script needed"` if scripting is not used |

The factory automatically:
1. Reads `build-config.json`
2. Deploys the plugin and blueprints to the game (`PluginDeployer.Deploy`)
3. Creates `PbTestHarnessHttp` with `GameProcessManager` and `WorldManager`

## `IPbTestHarness`

The main SDK interface.

### World management

```csharp
void StartWorld(string templateWorldName, string workingCopyName);
void StopWorld();
```

| Method | Description |
|---|---|
| `StartWorld` | Copy template → rename → launch SE → wait for HTTP health check |
| `StopWorld` | Delete world copy and kill the game process |

### Grid management

```csharp
IReadOnlyList<SpawnedGrid> SpawnTestGrid(string blueprintName, double x, double y, double z);
void RemoveTestGrid(long gridId);
```

> `SpawnTestGrid` returns a list of grids (usually 1) spawned from the blueprint.
> In SE, blueprints may contain multiple separate grids.

### Block-level access (low-level)

```csharp
bool ExecuteBlockAction(long gridId, int x, int y, int z, string actionId);
string GetBlockProperty(long gridId, int x, int y, int z, string propertyId);
bool SetBlockProperty(long gridId, int x, int y, int z, string propertyId, string value);
BlockDetailDto GetBlockDetail(long gridId, int x, int y, int z);
```

Block coordinates come from `GridPosition` in `BlockDto`, obtained during grid spawn.
**Prefer using proxies via `SpawnedGrid` over calling these methods directly.**

### Scripting (Programmable Block)

```csharp
void UploadScript(long gridId);
string RunScript(long gridId, string argument = null);
string GetLcdContent(long gridId);
IReadOnlyList<BlockState> GetBlockStates(long gridId);
```

## `SpawnedGrid`

High-level wrapper around a spawned grid.

### Properties

| Property | Type | Description |
|---|---|---|
| `Id` | `long` | In-game entity ID of the grid |
| `Blocks` | `IReadOnlyDictionary<string, BlockDto>` | All blocks keyed by name (case-insensitive) |
| `GridTerminalSystem` | `IMyGridTerminalSystem` | Emulates ModAPI GridTerminalSystem |

### Block lookup

```csharp
BlockDto Block(string name);                      // By name, throws if not found
bool HasBlock(string name);                        // Check existence
List<BlockDto> FilterBlocksBySubtype(string subtype); // Filter by subtype (e.g. "Light")
```

### Typed factories

```csharp
LightBlock Light(string name);      // → IMyLightingBlock
AntennaBlock Antenna(string name);  // → IMyRadioAntenna
```

### Grid queries

```csharp
IReadOnlyList<BlockState> GetBlockStates();
BlockDetailDto GetBlockDetail(BlockDto block);
```

## `IMyGridTerminalSystem` (via `GridTerminalSystemProxy`)

Emulates `Sandbox.ModAPI.Ingame.IMyGridTerminalSystem`.

```csharp
void GetBlocks(List<IMyTerminalBlock> blocks);
void GetBlocksOfType<T>(List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect = null);
void SearchBlocksOfName(string name, List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect = null);
IMyTerminalBlock GetBlockWithName(string name);
```

### Examples

```csharp
// Get all doors
var doors = new List<IMyTerminalBlock>();
_grid.GridTerminalSystem.GetBlocksOfType<IMyDoor>(doors);
var door = doors[0] as IMyDoor;

// Get all controllers (cockpit + remote control)
var controllers = new List<IMyTerminalBlock>();
_grid.GridTerminalSystem.GetBlocksOfType<IMyShipController>(controllers);

// Find block by name
var gyro = _grid.GridTerminalSystem.GetBlockWithName("Gyroscope") as IMyGyro;
```

### Type mapping

| Interface T | Matches block types |
|---|---|
| `IMyLightingBlock` | `Light` |
| `IMyDoor` | `Door` |
| `IMyGyro` | `Gyro` |
| `IMyThrust` | `Thrust` |
| `IMyShipController` | `Cockpit`, `ShipController`, `RemoteControl` |
| `IMyCockpit` | `Cockpit` |
| `IMyRemoteControl` | `RemoteControl` |
| `IMyBatteryBlock` | `Battery` |
| `IMyReactor` | `Reactor` |
| `IMyTerminalBlock` | All blocks |
| `IMyFunctionalBlock` | All except CargoContainer, ConveyorTube, Passage, Conveyor |

## `BlockDetailDto`

Returned by `GetBlockDetail()`. Contains full block information:

```csharp
public class BlockDetailDto
{
    public string Type { get; set; }
    public string Subtype { get; set; }
    public List<TerminalPropertyDto> Properties { get; set; }
    public List<TerminalActionDto> Actions { get; set; }
}
```

Useful for debugging — shows all available terminal properties and actions of a block.

## `BlockDto`

```csharp
public class BlockDto
{
    public string Name { get; set; }
    public string Type { get; set; }           // e.g. "MyObjectBuilder_Door"
    public Vector3IDto GridPosition { get; set; } // Grid coordinates
}
```

## ModAPI Types

### `Color`

```csharp
// in IntegrationTestingSDK.ModAPI.Types
public struct Color
{
    public byte R, G, B, A;
    public static Color Red, Green, Blue, White, Black;
    public static bool TryParse(string hex, out Color c);
}
```

### `Vector3`, `Vector3D`, `Vector3I`, `Vector2`

Structs mirroring `VRageMath`.

## ModAPI Enums

| Enum | Values |
|---|---|
| `DoorStatus` | `Opening, Open, Closing, Closed` |
| `ChargeMode` | `Auto, Recharge, Discharge` |
| `FlightMode` | `Patrol, Circle, OneWay, Track, WorkArea` |
| `PistonStatus` | `Stopped, Extending, Extended, Retracting, Retracted` |
| `MyShipConnectorStatus` | `Unconnected, Connectable, Connected` |
| `MyJumpDriveStatus` | `Charging, Ready, Jumping` |
| `MyLaserAntennaStatus` | `Idle, RotatingToTarget, ..., OutOfRange` |
| `VentStatus` | `Depressurized, Depressurizing, Pressurized, Pressurizing` |
| `MergeState` | `Unset, None, Working, Constrained, Locked` |
| `LandingGearMode` | `ReadyToLock, Locked, Unlocking` |
| `MyDetectedEntityType` | `None, Unknown, ..., Forageable` |
| `MyConveyorSorterMode` | `Whitelist, Blacklist` |
| `MyRotationDirection` | `AUTO, CW, CCW` |
| `BroadcastTarget` | `Owner, Faction, Everyone` |
| `MyPlanetElevation` | `Sealevel, Surface` |
| `Base6Directions.Direction` | `Up, Down, Forward, Backward, Left, Right` |
