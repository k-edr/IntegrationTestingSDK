# Getting Started

## Prerequisites

- **Space Engineers** (Steam) — installed and launched at least once
- **.NET Framework 4.8** SDK
- **Plugin Loader** — installed in SE (`avaness.PluginLoader`)
- Repository cloned alongside [MySpawnerController](https://github.com/k-edr/IntegrationTestingSDK)

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
└── docs/                           # Documentation (you are here)
```

## Installation

### 1. Clone the repository

```bash
git clone https://github.com/k-edr/IntegrationTestingSDK.git
```

### 2. Configure `build-config.json`

Copy the example and set the path to your Space Engineers `Bin64` folder:

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

### 3. Build MySpawnerController (the plugin)

The SDK auto-deploys the plugin on first test run, but the plugin must be **built** first:

```bash
cd ../MySpawnerController
powershell -File build.ps1
```

### 4. Prepare the world template

The SDK uses a template world with scripting enabled and no block limits. Place it at:

```
%AppData%\SpaceEngineers\AutoWorldLoader\Templates\EmptyWorld_NoLimits_WithPB_NoMods\
```

Make sure the world contains a Programmable Block and has Experimental Mode enabled (in `Sandbox_config.sbc`).

## Your first test

```csharp
using System.Threading;
using IntegrationTestingSDK;
using IntegrationTestingSDK.Contracts;
using NUnit.Framework;

[TestFixture]
[Category("Integration")]
public class MyFirstTest
{
    private static IPbTestHarness _harness;
    private SpawnedGrid _grid;

    [OneTimeSetUp]
    public void FixtureSetUp()
    {
        _harness = PbTestHarnessFactory.Create("// script code here");
        _harness.StartWorld("EmptyWorld_NoLimits_WithPB_NoMods", nameof(MyFirstTest));
    }

    [OneTimeTearDown]
    public void FixtureTearDown()
    {
        _grid = null;
        _harness?.Dispose();
    }

    [SetUp]
    public void SetUp()
    {
        Thread.Sleep(500);
        _grid = _harness.SpawnTestGrid("TestGrid_44Types", 0, 0, 0)[0];
    }

    [Test]
    public void Battery_HasPositiveStoredPower()
    {
        var bat = _grid.Block("Battery");
        var detail = _grid.GetBlockDetail(bat);
        Assert.That(detail.Properties.Any(p => p.Id == "MaxStoredPower"));
    }
}
```

### Running tests

```bash
dotnet test --filter "Category=Integration"
```

> **Important:** Tests launch the game. Make sure SE is not already running before starting tests.
