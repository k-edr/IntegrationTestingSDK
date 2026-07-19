# Writing Tests

## Test class structure

```csharp
[TestFixture]
[Category("Integration")]
public class MyIntegrationTests
{
    // ── Constants ──────────────────────────────────────────
    private const string TemplateWorld = "EmptyWorld_NoLimits_WithPB_NoMods";
    private const string TestBlueprint = "TestGrid_44Types";
    private const double SpawnX = 0.0, SpawnY = 0.0, SpawnZ = 0.0;

    // ── Shared state ───────────────────────────────────────
    private static IPbTestHarness _harness;
    private SpawnedGrid _grid;

    // ── Lifecycle ──────────────────────────────────────────
    [OneTimeSetUp]
    public void FixtureSetUp()
    {
        _harness = PbTestHarnessFactory.Create(null);
        _harness.StartWorld(TemplateWorld, nameof(MyIntegrationTests));
    }

    [OneTimeTearDown]
    public void FixtureTearDown()
    {
        _harness?.Dispose();
    }

    [SetUp]
    public void SetUp()
    {
        _grid = _harness.SpawnTestGrid(TestBlueprint, SpawnX, SpawnY, SpawnZ)[0];
    }

    [TearDown]
    public void TearDown()
    {
        // Grid removal is automatic on Dispose; no manual work needed.
    }
}
```

`Thread.Sleep` is **not needed** in `SetUp` or `TearDown`. Because the server and game
run on the same machine, grid spawning, removal, and world lifecycle operations complete
synchronously — there is no network latency to wait for.

## Test lifecycle notes

- Each call to `StartWorld` creates a **clean copy** of the template world.
  Every test run starts from a fresh, known state.

- Grids can be spawned **anywhere** — in `SetUp`, inside individual tests, or multiple
  times within a single test. You are not limited to one grid.

- `SpawnTestGrid` returns a **list** of `SpawnedGrid`. A single blueprint can contain
  multiple independent grids (e.g. a ship docked to a station). Use the index to pick
  the right one, or iterate all of them.

- **Removing a grid also removes attached sub-grids** (pistons, rotor heads, wheels,
  connectors with connected grids). You do not need to track or clean up sub-grids
  individually.

- If a test needs a completely clean world mid-fixture, call
  `_harness.StopWorld()` / `_harness.StartWorld()` to restart the game instance.
  This is rarely necessary but available.

## Test patterns

### 1. Block presence check (Found)

```csharp
[Test]
public void Gyro_Found()
{
    var gyros = new List<IMyTerminalBlock>();
    _grid.GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyros);
    Assert.That(gyros, Is.Not.Empty, "No gyro found");
}
```

### 2. Property round-trip (Read/Write)

```csharp
[Test]
public void Battery_ChargeMode_RoundTrips()
{
    var list = new List<IMyTerminalBlock>();
    _grid.GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(list);
    var bat = list[0] as IMyBatteryBlock;

    bat.ChargeMode = ChargeMode.Recharge;
    Assert.That(bat.ChargeMode, Is.EqualTo(ChargeMode.Recharge));

    bat.ChargeMode = ChargeMode.Auto;
    Assert.That(bat.ChargeMode, Is.EqualTo(ChargeMode.Auto));
}
```

No `Thread.Sleep` is required. For **99% of properties**, read-after-write works
instantly because the HTTP API and game run on the same machine — the server processes
requests synchronously. There is no network hop, no message queue, and no tick delay
to account for.

### 3. Actions with animation (Door)

```csharp
[Test]
public void Door_OpenClose_Toggles()
{
    var list = new List<IMyTerminalBlock>();
    _grid.GridTerminalSystem.GetBlocksOfType<IMyDoor>(list);
    var door = list[0] as IMyDoor;

    door.OpenDoor();
    Thread.Sleep(2000);       // Doors have an open/close animation
    Assert.That(door.Open, Is.True);
    Assert.That(door.Status, Is.EqualTo(DoorStatus.Open));

    door.CloseDoor();
    Thread.Sleep(2000);
    Assert.That(door.Open, Is.False);
    Assert.That(door.Status, Is.EqualTo(DoorStatus.Closed));
}
```

The `Thread.Sleep(2000)` here is **specifically for the door's smooth open/close animation**.
Doors animate between states — the `Open` property and `Status` enum are
not set until the animation completes. This is one of the very few cases where a
delay is genuinely needed. See the [Timing section](#timing-and-threadsleep) for the
full list.

### 4. Read-only properties

```csharp
[Test]
public void Battery_CurrentStoredPower_IsPositive()
{
    var list = new List<IMyTerminalBlock>();
    _grid.GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(list);
    var bat = list[0] as IMyBatteryBlock;

    Assert.That(bat.CurrentStoredPower, Is.GreaterThan(0));
}
```

### 5. Enum round-trip

```csharp
[Test]
public void Battery_ChargeMode_RoundTrips()
{
    var list = new List<IMyTerminalBlock>();
    _grid.GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(list);
    var bat = list[0] as IMyBatteryBlock;

    bat.ChargeMode = ChargeMode.Recharge;
    Assert.That(bat.ChargeMode, Is.EqualTo(ChargeMode.Recharge));

    bat.ChargeMode = ChargeMode.Auto;
    Assert.That(bat.ChargeMode, Is.EqualTo(ChargeMode.Auto));
}
```

Enums behave the same as any other property — instant read-after-write.

### 6. Using `Assume.That` for optional blocks

```csharp
[Test]
public void GravityGeneratorSphere_Radius_RoundTrips()
{
    var list = new List<IMyTerminalBlock>();
    _grid.GridTerminalSystem.GetBlocksOfType<IMyGravityGeneratorSphere>(list);
    Assume.That(list, Is.Not.Empty, "No gravity sphere in blueprint — skipping");
    var gs = list[0] as IMyGravityGeneratorSphere;

    gs.Radius = 50f;
    Assert.That(gs.Radius, Is.EqualTo(50f));
}
```

`Assume.That` skips the test (Inconclusive) instead of failing when a block is not in the blueprint.

## Timing and `Thread.Sleep`

**`Thread.Sleep` is NOT needed for 99% of operations.**

The HTTP API server and the game run on the same machine. Requests are processed
synchronously — when you set a property, the game applies it before the API responds.
There is no network latency, no message queue, and no tick delay.

### The ONLY cases where a delay is required

| Operation | Delay | Reason |
|---|---|---|
| `Door.OpenDoor()` / `Door.CloseDoor()` | 1500–2000ms | Doors have a **smooth open/close animation**. The `Open` property and `Status` enum update only when the animation finishes. |
| `Door.ToggleDoor()` | 1500–2000ms | Same reason — the door runs through its animation before reaching the target state. |

For everything else — properties, enums, actions on non-animated blocks, grid spawning,
grid removal — no delay is needed.

## Test filtering

```bash
# All integration tests
dotnet test --filter "Category=Integration"

# A specific test
dotnet test --filter "FullyQualifiedName~AllProxiesIntegrationTests"

# Everything except integration
dotnet test --filter "Category!=Integration"
```

## Reactive vs non-reactive testing

Tests fall into two categories:

### Non-reactive (most tests)

Set properties, read properties, invoke simple actions — no scripts, no delays.
Everything is done directly through the block proxy API.

```csharp
// Set a value, assert it stuck — instant
gyro.GyroOverride = true;
Assert.That(gyro.GyroOverride, Is.True);
```

### Reactive

Anything involving **movement, sensors, waypoints, timers, or physical simulation**
requires a Programmable Block script. The pattern is:

1. Spawn the grid with the blocks you need
2. Upload a script to the Programmable Block (via `CustomData` or the legacy `harness.UploadScript`)
3. The test triggers the script (via `TryRun` or `harness.RunScript`)
4. The script does the reactive work (follow waypoints, detect sensors, move blocks, etc.)
5. The test reads back the result — block positions, LCD output, terminal properties

The test itself does not need `Thread.Sleep` for property reads; the script running inside the game
handles the timing. If the test needs to wait for the script to finish, poll for
a completion signal (e.g. LCD text, a property value) rather than using a fixed delay.

## Script testing (Programmable Block)

### ModAPI approach (recommended)

```csharp
[Test]
public void Script_WritesToLcd()
{
    // Get the PB from the spawned grid
    var blocks = new List<IMyTerminalBlock>();
    _grid.GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(blocks);
    var pb = blocks[0] as IMyProgrammableBlock;

    // Upload script via CustomData (or use legacy API below)
    var scriptCode = File.ReadAllText("path/to/script.cs");
    _grid.GetBlockDetail(_grid.Block(pb.CustomName)); // verify PB exists

    // NOTE: CustomData write for script code depends on the proxy implementation.
    // The legacy PbTestHarnessFactory.Create + UploadScript approach is currently
    // the reliable way to deploy scripts. See below.

    // Trigger the script
    _harness.RunScript(_grid.Id, "argument");

    // Read the result from an LCD
    var list = new List<IMyTerminalBlock>();
    _grid.GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(list);
    var lcd = list[0] as IMyTextPanel;
    Assert.That(lcd.GetText(), Does.Contain("Expected output"));
}
```

### Reactive test example (waypoint navigation)

```csharp
[Test]
public void Drone_FollowsWaypoints()
{
    // Upload the navigation script to the grid's PB
    _harness.UploadScript(_grid.Id);

    // Trigger script — it handles waypoints, movement, sensors internally
    var result = _harness.RunScript(_grid.Id, "start");

    // Read LCD for script's log output
    var lcd = _harness.GetLcdContent(_grid.Id);
    Assert.That(lcd, Does.Contain("Arrived at waypoint 3"));
}
```

### Legacy script API

> **TODO:** The current `PbTestHarnessFactory.Create(scriptCode)` + `harness.UploadScript()` +
> `harness.RunScript()` API is functional but inflexible (one global script per harness).
> A more flexible system for script deployment is planned — supporting multiple scripts,
> per-PB scripting, and hot-reload during a test session.

## Using the Low-Level API

If there's no typed proxy, you can work directly:

```csharp
[Test]
public void DirectPropertyAccess()
{
    var block = _grid.Block("Battery");

    // Read property
    var raw = _grid.GetBlockProperty(block, "MaxStoredPower");
    var mw = float.Parse(raw);
    Assert.That(mw, Is.GreaterThan(0));

    // Write property
    _grid.SetBlockProperty(block, "ChargeMode", "0"); // Auto

    // Execute action
    _grid.ExecuteBlockAction(block, "OnOff_Off");
}
```

No delays needed — the low-level API operates over the same synchronous HTTP channel.

## Debugging

### Dump all blocks on the grid

```csharp
[Test]
public void Debug_DumpAllBlocks()
{
    foreach (var kv in _grid.Blocks)
    {
        TestContext.WriteLine($"  [{kv.Value.Type}] {kv.Key}");

        var detail = _grid.GetBlockDetail(kv.Value);
        if (detail?.Properties != null)
            foreach (var p in detail.Properties)
                TestContext.WriteLine($"    [{p.PropertyType}] {p.Id} = {p.Value}");
        if (detail?.Actions != null)
            foreach (var a in detail.Actions)
                TestContext.WriteLine($"    {a.Id} ({a.Name})");
    }
}
```

This will show the **actual** terminal properties and actions of each block — invaluable for debugging and writing new proxies.

## Commonly used patterns

### `GetBlocksOfType<T>` helper

```csharp
// DRY helper in the test class
private List<IMyTerminalBlock> GetBlocksOfType<T>()
{
    var list = new List<IMyTerminalBlock>();
    _grid.GridTerminalSystem.GetBlocksOfType<T>(list);
    return list;
}

// Usage
var batteries = GetBlocksOfType<IMyBatteryBlock>();
var bat = batteries[0] as IMyBatteryBlock;
```
