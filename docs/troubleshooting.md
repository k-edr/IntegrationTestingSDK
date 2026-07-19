# Troubleshooting

## 🚫 Game won't launch

### `SpaceEngineersLauncher.exe not found`

**Cause:** Incorrect `seBin64` path in `build-config.json`.

**Fix:**
```json
{
  "seBin64": "D:\\SteamLibrary\\steamapps\\common\\SpaceEngineers\\Bin64"
}
```
Make sure the path exists and contains `SpaceEngineersLauncher.exe`.

### `Saves directory not found` or `No numeric Steam ID folder found`

**Cause:** SE has never been launched under the current user, or the Saves folder is corrupted.

**Fix:** Launch Space Engineers at least once through Steam to create the `%AppData%\SpaceEngineers\Saves\{steamId}\` folder.

### `PluginLoader config.xml not found`

**Cause:** Plugin Loader is not installed.

**Fix:** Install [Plugin Loader](https://github.com/sepluginloader/PluginLoader) for Space Engineers.

---

## 🧩 Grid spawn errors

### `HTTP NotFound: Block not found at ... on grid ...`

**Cause:** Armor blocks (and other non-terminal blocks) are not registered by the GridSpawner plugin.

**Fix:** This is normal for debug dumps. It doesn't affect tests — such blocks are simply skipped.

### `Template world not found`

**Cause:** The world template is missing from `AutoWorldLoader/Templates/`.

**Fix:** Create a template world with a Programmable Block and place it in the correct folder. See [Configuration → World Template](configuration.md#world-template).

### `GridSpawner.Plugin.dll not found`

**Cause:** MySpawnerController hasn't been built.

**Fix:**
```bash
cd MySpawnerController
powershell -File build.ps1
```

---

## 🧪 Tests failing

### Property doesn't persist after write

**Symptom:**
```
Expected: True
But was:  False
```
on a round-trip test.

**Cause:** Insufficient delay between write and read. The game hasn't applied the change yet.

**Fix:** Most properties work instantly (the HTTP API and game run on the same machine).
The only case where delays are needed is for blocks with animation — e.g. doors:
```csharp
door.OpenDoor();
Thread.Sleep(2000);  // Wait for open animation to finish
Assert.That(door.Open, Is.True);
```

### `Enabled` doesn't change

**Symptom:** `fb.Enabled = false;` doesn't turn off the block.

**Cause:** `IMyFunctionalBlock.Enabled` uses `SetProperty("OnOff")`,
but in SE this property is not always handled correctly via terminal.

**Fix:** Use direct terminal actions:
```csharp
_grid.ExecuteBlockAction(block, "OnOff_Off");  // Turn off
_grid.ExecuteBlockAction(block, "OnOff_On");   // Turn on
```

### `IsAutoPilotEnabled` doesn't work

**Symptom:** Setting `IsAutoPilotEnabled = true` doesn't enable autopilot.

**Cause:** Autopilot in SE requires GPS waypoints. They are added via the ModAPI method `AddWaypoint()`,
which is not available through the HTTP terminal API.

**Fix:** This test is marked `[Ignore]`. To test autopilot:
- **Manually** in-game: add GPS waypoints and enable autopilot through the terminal
- **Via script:** write a PB script that adds waypoints and calls autopilot, upload it to the grid's PB, trigger it from your test, then read the result from LCD

### `IsPermanent` (LaserAntenna) doesn't work

**Cause:** The `isPerm` property is not writable via the SE terminal.

**Fix:** Test is marked `[Ignore]`. Functionality not available through HTTP API.

### `MaxStoredPower` / `MaxThrust` returns 0 or assertion fail

**Cause:** These are not terminal properties. `MaxThrust` is a computed value,
`MaxStoredPower` on JumpDrive is similar.

**Fix:** Tests are marked `[Ignore]`. The only way to read these values is via a Programmable Block script (scripts have full ModAPI access to computed properties).

---

## 🔌 HTTP / Network

### `Connection refused` on `localhost:9997`

**Cause:** The game is running but the GridSpawner plugin didn't load or didn't start HTTP.

**Fix:**
1. Check that `GridSpawner.Plugin.dll` is copied to `Bin64/Plugins/`
2. Check that the plugin is registered in `Bin64/Plugins/config.xml`
3. Check game logs — the plugin writes messages during initialization
4. Try opening `http://localhost:9997/api/v1/health` in a browser

### `Timeout waiting for session`

**Cause:** The game didn't load the session within the configured time (`sessionTimeoutMinutes`).

**Fix:**
1. Increase `sessionTimeoutMinutes` in `build-config.json` (e.g. to 5)
2. Make sure the world template is valid (not corrupted)
3. Verify the world contains `Sandbox.sbc` and `Sandbox_config.sbc`

---

## 🧹 Cleanup

### Game left hanging after test crash

```powershell
# Kill all Space Engineers processes
taskkill /f /im SpaceEngineers.exe
taskkill /f /im SpaceEngineersLauncher.exe
```

### World copies left in Saves

Folders named after test classes (e.g. `AllProxiesIntegrationTests`) in
`%AppData%\SpaceEngineers\Saves\{steamId}\`. Can be deleted manually.

---

## 📋 Diagnostics

### Enabling SDK logs

```csharp
// During test initialization
SdkLog.Init();  // Called automatically in PbTestHarnessFactory.Create()
```

Logs are written to `SdkLog.txt` in the test execution folder. Check this file when troubleshooting.

### Inspecting available block properties

```csharp
[Test]
public void Debug_CheckBlockProperties()
{
    var block = _grid.Block("Battery");
    var detail = _grid.GetBlockDetail(block);

    TestContext.WriteLine($"Type: {detail.Type}");
    TestContext.WriteLine($"Subtype: {detail.Subtype}");

    TestContext.WriteLine("Properties:");
    foreach (var p in detail.Properties)
        TestContext.WriteLine($"  [{p.PropertyType}] {p.Id} = {p.Value}");

    TestContext.WriteLine("Actions:");
    foreach (var a in detail.Actions)
        TestContext.WriteLine($"  {a.Id} ({a.Name})");
}
```

This method shows the **actual** terminal properties of a block — don't rely on SE documentation,
always verify the real data from the game.
