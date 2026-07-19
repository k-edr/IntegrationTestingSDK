# IntegrationTestingSDK

Integration testing for Space Engineers — launch the game, spawn grids, and interact with
terminal blocks from C# NUnit tests via HTTP API.

```
NUnit Test  ──HTTP──>  Space Engineers  ──ModAPI──>  Terminal Actions & Properties
```

## What it does

- **Launches** Space Engineers from your tests
- **Copies** a template world so every run starts clean
- **Spawns** grids from blueprints anywhere in the world
- **Reads/writes** terminal block properties (color, power, limits, ...)
- **Executes** terminal actions (open door, lock gear, trigger timer, ...)
- **47 block types** with typed C# proxies — doors, batteries, gyros, pistons, rotors, LCDs, ...
- **Script testing** — upload and run Programmable Block scripts, read LCD output

## Quick start

```csharp
[TestFixture]
[Category("Integration")]
public class MyIntegrationTests
{
    private static IPbTestHarness _harness;
    private SpawnedGrid _grid;

    [OneTimeSetUp]
    public void FixtureSetUp()
    {
        _harness = PbTestHarnessFactory.Create(null);
        _harness.StartWorld("EmptyWorld_NoLimits_WithPB_NoMods", nameof(MyIntegrationTests));
    }

    [OneTimeTearDown]
    public void FixtureTearDown() => _harness?.Dispose();

    [SetUp]
    public void SetUp()
    {
        var spawned = _harness.SpawnTestGrid("TestGrid_44Types", 0, 0, 0);
        _grid = spawned[0];
    }

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

    [Test]
    public void Door_OpenClose_Toggles()
    {
        var list = new List<IMyTerminalBlock>();
        _grid.GridTerminalSystem.GetBlocksOfType<IMyDoor>(list);
        var door = list[0] as IMyDoor;

        door.OpenDoor();
        Thread.Sleep(2000);  // Door animation takes time
        Assert.That(door.Open, Is.True);
    }
}
```

```bash
dotnet test --filter "Category=Integration"
```

## Prerequisites

- **Space Engineers** (Steam) — installed and launched at least once
- **.NET Framework 4.8** SDK
- **Plugin Loader** (`avaness.PluginLoader`) — installed in SE
- Repository cloned alongside [MySpawnerController](https://github.com/k-edr/IntegrationTestingSDK)

## Setup

### 1. Clone

```bash
git clone https://github.com/k-edr/IntegrationTestingSDK.git
```

### 2. Configure `build-config.json`

```bash
cd IntegrationTestingSDK.Tests
cp ../build-config.example.json build-config.json
```

Edit `build-config.json`:

```json
{
  "seBin64": "D:\\SteamLibrary\\steamapps\\common\\SpaceEngineers\\Bin64",
  "apiPort": 9997,
  "healthPollIntervalSeconds": 2,
  "sessionTimeoutMinutes": 3,
  "httpTimeoutSeconds": 30
}
```

### 3. Build the plugin (MySpawnerController)

The SDK auto-deploys the plugin on first test run, but it must be built first:

```bash
cd ../MySpawnerController
powershell -File build.ps1
```

### 4. Prepare the world template

Place a template world at:

```
%AppData%\SpaceEngineers\AutoWorldLoader\Templates\EmptyWorld_NoLimits_WithPB_NoMods\
```

The world must have a Programmable Block and Experimental Mode enabled (`Sandbox_config.sbc`).

## Documentation

| Document | Topic |
|---|---|
| [Getting Started](docs/getting-started.md) | Prerequisites, installation, first test |
| [Architecture](docs/architecture.md) | HTTP → Plugin → Terminal Actions pipeline |
| [Configuration](docs/configuration.md) | `build-config.json`, world templates, blueprints |
| [API Reference](docs/api-reference.md) | `IPbTestHarness`, `SpawnedGrid`, `GridTerminalSystem` |
| [Block Proxies](docs/block-proxies.md) | Full catalog of 47 block types with properties and actions |
| [Writing Tests](docs/writing-tests.md) | Test patterns, timing, script testing, debugging |
| [Troubleshooting](docs/troubleshooting.md) | Common issues and solutions |

## How it works

```
┌──────────────────┐     HTTP (localhost:9997)     ┌───────────────────────┐
│  NUnit Test       │ ──────────────────────────>  │  Space Engineers      │
│  (C#)             │ <──────────────────────────  │  ┌─────────────────┐  │
│                   │     JSON responses            │  │ GridSpawner     │  │
│  IntegrationTest  │                              │  │ Plugin          │  │
│  SDK              │                              │  │ (HTTP API)      │  │
│  ┌─────────────┐  │                              │  ├─────────────────┤  │
│  │ IPbTestHarness│ │                              │  │ Terminal Actions│  │
│  │ (interface) │  │                              │  │ & Properties   │  │
│  ├─────────────┤  │                              │  │ (ModAPI)        │  │
│  │ PbTestHarness│  │                              │  └─────────────────┘  │
│  │ Http (impl) │  │                              │                       │
│  ├─────────────┤  │                              │  Game World           │
│  │ GameProcess  │  │── launches game ───────────>│  ┌─────────────────┐  │
│  │ Manager      │  │                              │  │ Spawned Grid    │  │
│  ├─────────────┤  │                              │  │ (from blueprint)│  │
│  │ WorldManager │  │── copies template world ───>│  └─────────────────┘  │
│  └─────────────┘  │                              └───────────────────────┘
└──────────────────┘
```

## Repository structure

```
IntegrationTestingSDK/
├── IntegrationTestingSDK/          # Core library (SDK)
│   ├── Client/                     # HTTP client, game process management
│   │   ├── ApiRoutes.cs            # HTTP API URLs
│   │   ├── GameProcessManager.cs   # SE launch/stop
│   │   ├── PbTestHarnessHttp.cs    # HTTP implementation of IPbTestHarness
│   │   ├── PluginDeployer.cs       # Plugin deployment to the game
│   │   └── WorldManager.cs         # World copy/delete
│   ├── Contracts/                  # DTOs and contracts
│   ├── Infrastructure/             # Logging (SdkLog)
│   └── ModAPI/
│       ├── Enums/                  # Enums (DoorStatus, ChargeMode, ...)
│       ├── Interfaces/             # Block interfaces (IMyDoor, IMyGyro, ...)
│       ├── Proxies/                # HTTP proxies for each block type
│       └── Types/                  # Value types (Color, Vector3, ...)
├── IntegrationTestingSDK.Tests/    # Tests
├── docs/                           # Documentation
├── build-config.example.json       # Configuration template
├── DLL_ANALYSIS.md                 # SE ModAPI analysis reference
├── IntegrationTestingSDK.sln       # Solution file
└── README.md
```

## Key facts

- **47** block types with typed C# proxies
- **137** passing integration tests
- **6** skipped (properties unavailable via HTTP terminal API)
- Each test run starts from a **clean copy** of the template world
- Grids can be spawned and removed **at any point** in a test
- `Thread.Sleep` is almost never needed — everything is local (game + server on same machine)
  - Only exception: **door animations** require a brief pause
- Actions requiring server-side reaction (autopilot, jumping) should be triggered via **Programmable Block scripts** within the game
