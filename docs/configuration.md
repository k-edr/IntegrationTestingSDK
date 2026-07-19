# Configuration

## `build-config.json`

Configuration file for the test harness. Must be located next to the test assembly (`IntegrationTestingSDK.Tests/`).

### All fields

| Field | Type | Default | Description |
|---|---|---|---|
| `seBin64` | `string` | **required** | Path to SE `Bin64` directory (e.g. `D:\\SteamLibrary\\...\\Bin64`) |
| `apiScheme` | `string` | `http` | API scheme (`http` or `https`) |
| `apiHost` | `string` | `localhost` | API host |
| `apiPort` | `int` | `9997` | Plugin HTTP API port |
| `healthPollIntervalSeconds` | `int` | `2` | Health-check poll interval while waiting for game readiness |
| `sessionTimeoutMinutes` | `int` | `3` | Max wait time for session load |
| `httpTimeoutSeconds` | `int` | `30` | HTTP client timeout |
| `lcdPollTimeoutSeconds` | `double` | `0.5` | Max LCD poll time |
| `lcdPollIntervalMs` | `int` | `50` | LCD poll interval |

### Example

```json
{
  "_comment": "Copy this file to build-config.json and edit values to match your setup.",
  "seBin64": "D:\\SteamLibrary\\steamapps\\common\\SpaceEngineers\\Bin64",
  "apiPort": 9997,
  "healthPollIntervalSeconds": 2,
  "sessionTimeoutMinutes": 3,
  "httpTimeoutSeconds": 30
}
```

### Config resolution

`PbTestHarnessFactory` looks for `build-config.json`:
1. In the executing assembly directory (`BaseDirectory`)
2. Three levels up (for test runners that copy assemblies to temp folders)

## World Template

The SDK uses a template world as the base for each test run.
Each `StartWorld()` call creates a **copy** of the template — the original is never modified.

### Template requirements

- Location: `%AppData%\SpaceEngineers\AutoWorldLoader\Templates\{TemplateName}\`
- Must contain `Sandbox.sbc` and `Sandbox_config.sbc`
- `Sandbox_config.sbc` must have:
  - `ExperimentalMode` = `true`
  - `EnableIngameScripts` = `true`
  - `BlockLimitsEnabled` = `false` (recommended)
- World must contain at least one **Programmable Block** (if you plan to upload scripts)

### Naming

The template name is the folder name. For example, template `EmptyWorld_NoLimits_WithPB_NoMods` maps to:

```
%AppData%\SpaceEngineers\AutoWorldLoader\Templates\EmptyWorld_NoLimits_WithPB_NoMods\
```

## Test Blueprints

Blueprints for grid spawning are located in `MySpawnerController/GridSpawner.Shared/Gamedata/TestBlueprints/`.

`PluginDeployer` automatically copies them to `%AppData%\SpaceEngineers\Blueprints\local\` on first test run.

### Available blueprints

| Blueprint | Description | Blocks |
|---|---|---|
| `TestGrid_44Types` | 61 blocks, 44+ unique types | All major types for proxy testing |
| `TestGrid_AntenaLighsPBLCD` | Compact grid | Antenna, lights, PB, LCD |

### Creating your own blueprint

1. Build a grid in a creative SE world
2. Save as blueprint (Ctrl+B)
3. Copy the blueprint folder into `TestBlueprints/`
4. Use the folder name as `blueprintName` in `SpawnTestGrid()`

## Environment variables

The SDK does not require environment variables. The path to `Bin64` is configured via `seBin64` in config.

The path to `%AppData%\SpaceEngineers` is resolved automatically via `Environment.GetFolderPath(SpecialFolder.ApplicationData)`.
