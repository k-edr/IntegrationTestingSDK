# Architecture

## Overview

IntegrationTestingSDK is a bridge between NUnit tests and the Space Engineers game.
Tests communicate with the game through an HTTP API provided by the in-game `GridSpawner.Plugin`.

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

## Layers

### 1. `IPbTestHarness` (contract)

```csharp
public interface IPbTestHarness : IDisposable
{
    void StartWorld(string template, string workingCopy);
    void StopWorld();
    IReadOnlyList<SpawnedGrid> SpawnTestGrid(string blueprint, double x, double y, double z);
    void RemoveTestGrid(long gridId);

    // Low-level block access
    bool ExecuteBlockAction(long gridId, int x, int y, int z, string actionId);
    string GetBlockProperty(long gridId, int x, int y, int z, string propertyId);
    bool SetBlockProperty(long gridId, int x, int y, int z, string propertyId, string value);
    BlockDetailDto GetBlockDetail(long gridId, int x, int y, int z);

    // Scripting (Programmable Block)
    void UploadScript(long gridId);
    string RunScript(long gridId, string argument = null);
    string GetLcdContent(long gridId);
    IReadOnlyList<BlockState> GetBlockStates(long gridId);
}
```

### 2. `PbTestHarnessHttp` (implementation)

HTTP client that sends requests to `GridSpawner.Plugin`:

| Method | HTTP | URL |
|---|---|---|
| `SpawnTestGrid` | POST | `/api/v1/grid/spawn` |
| `RemoveTestGrid` | POST | `/api/v1/grid/remove` |
| `ExecuteBlockAction` | POST | `/api/v1/grid/{id}/block/{x},{y},{z}/action` |
| `GetBlockProperty` | GET | `/api/v1/grid/{id}/block/{x},{y},{z}/property/{name}` |
| `SetBlockProperty` | POST | `/api/v1/grid/{id}/block/{x},{y},{z}/property/{name}` |
| `GetBlockDetail` | GET | `/api/v1/grid/{id}/block/{x},{y},{z}/detail` |
| `RunScript` | POST | `/api/v1/grid/{id}/script/run` |
| `GetLcdContent` | GET | `/api/v1/grid/{id}/lcd` |
| `GetBlockStates` | GET | `/api/v1/grid/{id}/blocks` |
| Health check | GET | `/api/v1/health` |

### 3. `SpawnedGrid` (high-level wrapper)

`SpawnedGrid` is the central object in tests. It provides:

- **Named block access:** `grid.Block("Battery")`
- **Typed factories:** `grid.Light("Lamp 1")`, `grid.Antenna("Antenna")`
- **ModAPI-like terminal system:** `grid.GridTerminalSystem.GetBlocksOfType<IMyDoor>(list)`
- **Low-level access:** `grid.GetBlockDetail(block)`, `grid.GetBlockStates()`

### 4. `GridTerminalSystemProxy`

Emulates `Sandbox.ModAPI.Ingame.IMyGridTerminalSystem`. Automatically:
- Matches interfaces (`IMyDoor`, `IMyGyro`, ...) by block type
- Creates correct proxies via `CreateProxy()`
- Supports `GetBlocksOfType<T>()`, `GetBlockWithName()`, `SearchBlocksOfName()`

### 5. Block Proxies

Each block type has a proxy class inheriting from `FunctionalBlockProxy` → `BlockProxyBase`.
Proxies translate property calls into HTTP:

```csharp
// Test code:
gyro.Yaw = 0.5f;
// Becomes:
// HTTP POST /api/v1/grid/{id}/block/{x},{y},{z}/property/Yaw
// body: {"value": "0.5"}

float yaw = gyro.Yaw;
// Becomes:
// HTTP GET /api/v1/grid/{id}/block/{x},{y},{z}/property/Yaw
```

### 6. Game Process & World Management

- **`GameProcessManager`** — launches `SpaceEngineersLauncher.exe`, kills processes
- **`WorldManager`** — copies template world from `AutoWorldLoader/Templates/` to `Saves/{steamId}/`, renames `SessionName` in `.sbc` files, deletes working copies after tests
- **`PluginDeployer`** — copies plugin DLLs to `Bin64/Plugins/`, registers in PluginLoader `config.xml`, copies blueprints to `Blueprints/local/`

## Test lifecycle

```
OneTimeSetUp
  ├─ PbTestHarnessFactory.Create()  →  deploy plugin + create harness
  └─ harness.StartWorld()           →  copy template → launch game → wait for HTTP health
                                     (fresh copy of the template world each run)

SetUp (before each test — optional, grids can also be spawned in tests)
  └─ harness.SpawnTestGrid()        →  HTTP → spawn blueprint → return SpawnedGrid[]
                                     (can return multiple grids; spawn whenever needed)

Test
  ├─ grid.Block("Name")             →  get block by name
  ├─ grid.GridTerminalSystem.GetBlocksOfType<T>() →  get typed blocks
  ├─ block.Property = value         →  HTTP set property (no delay needed; local execution)
  └─ Assert.That(...)               →  verify (read is synchronous)

Test (with reactive behavior)
  ├─ PB.CustomData = scriptCode     →  upload script to Programmable Block
  ├─ PB.TryRun("arg")               →  trigger script
  └─ grid.GetLcdContent() / block read →  observe results

Cleanup (any time)
  └─ harness.RemoveTestGrid(id)     →  removes grid + attached sub-grids
                                     (piston heads, rotor heads, wheels)

OneTimeTearDown
  └─ harness.Dispose()              →  remove grid → stop world → kill process
```

### Key principles

- **No delays needed** for 99% of operations — the HTTP API, game server, and client all run on the same machine, so property reads are synchronous with writes.
- **Grids can be spawned anywhere** — in `SetUp`, inside individual tests, or multiple times per test. `SpawnTestGrid` returns an array — a blueprint can contain multiple independent grids.
- **Fresh world every time** — `StartWorld` copies the template and launches a new session. Nothing carries over between runs.
- **Sub-grid cleanup is automatic** — removing a grid via `RemoveTestGrid` also removes attached mechanical sub-grids (piston heads, rotor heads, wheels).
