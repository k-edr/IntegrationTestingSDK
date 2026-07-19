# IntegrationTestingSDK

Integration testing for Space Engineers scripts and blocks via HTTP API.

The SDK launches the game, loads a world, spawns grids from blueprints, and allows
reading/writing terminal properties and executing terminal actions on blocks — all from C# NUnit tests.

## Contents

| Document | Topic |
|---|---|
| [Getting Started](getting-started.md) | Prerequisites, installation, first test |
| [Architecture](architecture.md) | How the SDK works: HTTP → Plugin → Terminal Actions |
| [Configuration](configuration.md) | `build-config.json`, world templates, blueprints |
| [API Reference](api-reference.md) | `IPbTestHarness`, `SpawnedGrid`, `GridTerminalSystem` |
| [Block Proxies](block-proxies.md) | Full catalog of 47 block types with properties and actions |
| [Writing Tests](writing-tests.md) | Test patterns, threading, timing |
| [Troubleshooting](troubleshooting.md) | Common issues and solutions |

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

## Key numbers

- **47** block types with typed proxies
- **137** passing integration tests (0 failures)
- **6** skipped (properties not available via HTTP terminal API)
