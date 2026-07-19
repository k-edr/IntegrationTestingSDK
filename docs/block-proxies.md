# Block Proxies

The SDK provides typed proxies for 47 Space Engineers block types.
All proxies implement the corresponding interfaces from `IntegrationTestingSDK.ModAPI.Interfaces`.

## Inheritance hierarchy

```
IMyTerminalBlock          (base: CustomName, CustomData, ShowOnHUD, ...)
  └─ IMyFunctionalBlock   (+ Enabled)
       └─ IMyDoor, IMyGyro, IMyLightingBlock, ...  (concrete types)
```

Most proxy classes inherit from `FunctionalBlockProxy` → `TerminalBlockProxy` → `BlockProxyBase`.
A few blocks extend `TerminalBlockProxy` directly and do **not** have `Enabled`.

> **Note:** All `IMyFunctionalBlock` derivatives support `Enabled { get; set; }` (On/Off toggle).
> This is available on every block below except where noted. It is listed explicitly on each entry for clarity.

## How to use

```csharp
// Method 1: via GridTerminalSystem (recommended)
var list = new List<IMyTerminalBlock>();
_grid.GridTerminalSystem.GetBlocksOfType<IMyGyro>(list);
var gyro = list[0] as IMyGyro;
gyro.GyroOverride = true;

// Method 2: via named access
var lamp = _grid.Light("Interior Light");
lamp.Color = Color.Red;
```

---

## Base interfaces

### `IMyTerminalBlock`

```csharp
string CustomName { get; set; }          // Block name
string CustomData { get; set; }          // Custom Data (string)
bool ShowOnHUD { get; set; }            // Show on HUD
bool ShowInTerminal { get; set; }       // Show in terminal
bool ShowInToolbarConfig { get; set; }  // Show in toolbar
bool ShowInInventory { get; set; }      // Show in inventory
string CustomNameWithFaction { get; }   // Name with faction (read-only)
string DetailedInfo { get; }            // Detailed info (read-only)
bool HasLocalPlayerAccess();            // Local player access
bool HasPlayerAccess(long playerId);    // Player access by ID
```

### `IMyFunctionalBlock`

```csharp
bool Enabled { get; set; }  // On/Off
```

> **⚠️ Note:** `Enabled` setter uses `SetProperty("OnOff")`, but this doesn't always work in SE.
> For reliable toggling, use `ExecuteBlockAction` with action `"OnOff_On"` / `"OnOff_Off"`.

---

## Block catalog

### 🚪 Door — `IMyDoor`

```csharp
bool Enabled { get; set; }               // On/Off (⚠️ setter no-op via HTTP — see limitations)
bool Open { get; }                       // Is open?
DoorStatus Status { get; }               // Status (Open/Closed)
float OpenRatio { get; }                 // Open percentage
void OpenDoor();                         // Open (action: Open_On)
void CloseDoor();                        // Close (action: Open_Off)
void ToggleDoor();                       // Toggle (action: Open)
```

### 💡 Lighting — `IMyLightingBlock`

```csharp
bool Enabled { get; set; }               // On/Off
Color Color { get; set; }                // Color
float Radius { get; set; }               // Radius
float Intensity { get; set; }            // Intensity
float Falloff { get; set; }             // Falloff
float BlinkIntervalSeconds { get; set; } // Blink interval
float BlinkLength { get; set; }         // Blink length
float BlinkOffset { get; set; }         // Blink offset
```

### 🔄 Gyro — `IMyGyro`

```csharp
bool Enabled { get; set; }               // On/Off
bool GyroOverride { get; set; }          // Override
float GyroPower { get; set; }            // Power (0..1)
float Yaw { get; set; }                  // Yaw (-1..1)
float Pitch { get; set; }                // Pitch (-1..1)
float Roll { get; set; }                 // Roll (-1..1)
```

### 🚀 Thrust — `IMyThrust`

```csharp
bool Enabled { get; set; }               // On/Off
float ThrustOverride { get; set; }       // Thrust override (N)
float ThrustOverridePercentage { get; set; } // Thrust override (%)
float MaxThrust { get; }                 // Max thrust (N)
float MaxEffectiveThrust { get; }        // Max effective thrust (N)
float CurrentThrust { get; }             // Current thrust output (N)
float CurrentThrustPercentage { get; }   // Current thrust output (%)
Vector3I GridThrustDirection { get; }    // Thrust direction in grid coords
```

### 📻 Radio Antenna — `IMyRadioAntenna`

```csharp
bool Enabled { get; set; }               // On/Off
float Radius { get; set; }               // Broadcast radius
string HudText { get; set; }             // HUD text
bool ShowShipName { get; set; }          // Show ship name
bool IsBroadcasting { get; }             // Is broadcasting? (read-only)
bool EnableBroadcasting { get; set; }    // Enable broadcasting
```

### 📡 Beacon — `IMyBeacon`

```csharp
bool Enabled { get; set; }               // On/Off
float Radius { get; set; }
string HudText { get; set; }
```

### 📶 Laser Antenna — `IMyLaserAntenna`

```csharp
bool Enabled { get; set; }               // On/Off
float Range { get; set; }                // Range
bool IsPermanent { get; set; }           // ⚠️ setter — no-op (not writable via terminal)
bool IsOutsideLimits { get; }            // Outside connection limits?
MyLaserAntennaStatus Status { get; }    // Status
void Connect();                          // Connect
void SetTargetCoords(string coords);     // Set target GPS coordinates as string
```

> **Note:** There is no `Disconnect()` method — laser antenna disconnect is not available via terminal actions.

### 🕹️ Cockpit — `IMyCockpit`

Inherits `IMyShipController` (see below) plus cockpit-specific members:

```csharp
float OxygenCapacity { get; }            // Oxygen tank capacity
float OxygenFilledRatio { get; }         // Oxygen fill level (0..1)
```

### 🎛️ Ship Controller — `IMyShipController`

```csharp
bool Enabled { get; set; }               // On/Off
bool CanControlShip { get; }             // Can control?
bool IsUnderControl { get; }             // Under control?
bool ControlThrusters { get; set; }      // Control thrusters
bool ControlWheels { get; set; }         // Control wheels
bool HandBrake { get; set; }             // Handbrake
bool DampenersOverride { get; set; }     // Dampeners
bool ShowHorizonIndicator { get; set; }  // Horizon indicator
bool IsMainCockpit { get; }              // Is main cockpit?
Vector3 MoveIndicator { get; }           // Move indicator
Vector2 RotationIndicator { get; }       // Rotation indicator
float RollIndicator { get; }             // Roll indicator
Vector3D CenterOfMass { get; }           // Center of mass

Vector3 GetNaturalGravity();
Vector3 GetArtificialGravity();
Vector3 GetTotalGravity();
double GetShipSpeed();
MyShipVelocities GetShipVelocities();
float CalculateShipMass();
bool TryGetPlanetPosition(out Vector3D position);
bool TryGetPlanetElevation(MyPlanetElevation detail, out double elevation);
```

> **Note:** `GetShipSpeed()`, `GetShipVelocities()`, and `CalculateShipMass()`
> are implemented by parsing the block's `DetailedInfo` rather than separate terminal properties.

### 🎮 Remote Control — `IMyRemoteControl`

```csharp
bool Enabled { get; set; }               // On/Off
bool IsAutoPilotEnabled { get; set; }    // ⚠️ setter — no-op (requires GPS waypoints)
float SpeedLimit { get; set; }           // Speed limit
FlightMode FlightMode { get; set; }      // Flight mode
Base6Directions.Direction Direction { get; set; }
MyWaypointInfo CurrentWaypoint { get; }  // Current waypoint
bool WaitForFreeWay { get; set; }        // Wait for free way

void ClearWaypoints();                           // ⚠️ no-op via HTTP
void GetWaypointInfo(List<MyWaypointInfo> waypoints); // ⚠️ no-op via HTTP
void AddWaypoint(Vector3D coords, string name);  // ⚠️ no-op via HTTP
void SetAutoPilotEnabled(bool enabled);          // ⚠️ no-op via HTTP
void SetCollisionAvoidance(bool enabled);
void SetDockingMode(bool enabled);
```

> **Workaround for autopilot:** Add GPS waypoints and toggle autopilot via a Programmable Block script.
> Pass coordinates as string arguments. The test spawns a grid, uploads the script, triggers it,
> then reads the result from LCD.

### 🔋 Battery — `IMyBatteryBlock`

```csharp
bool Enabled { get; set; }               // On/Off
bool HasCapacityRemaining { get; }
float CurrentStoredPower { get; }
float MaxStoredPower { get; }
float CurrentInput { get; }
float MaxInput { get; }
bool IsCharging { get; }
ChargeMode ChargeMode { get; set; }
bool OnlyRecharge { get; set; }
bool OnlyDischarge { get; set; }
bool SemiautoEnabled { get; set; }
```

### ⚡ Reactor — `IMyReactor`

```csharp
bool Enabled { get; set; }               // On/Off
bool UseConveyorSystem { get; set; }
```

### ☀️ Solar Panel — `IMySolarPanel`

```csharp
bool Enabled { get; set; }               // On/Off
float MaxOutput { get; }
float CurrentOutput { get; }
```

### 📷 Camera — `IMyCameraBlock`

```csharp
bool Enabled { get; set; }               // On/Off
bool EnableRaycast { get; set; }
bool IsActive { get; }
double AvailableScanRange { get; }
float RaycastConeLimit { get; }
double RaycastDistanceLimit { get; }
float RaycastTimeMultiplier { get; }
MyDetectedEntityInfo Raycast(double distance, float pitch, float yaw);
MyDetectedEntityInfo Raycast(Vector3D targetPos);
bool CanScan(double distance);
int TimeUntilScan(double distance);
```

> **Note:** `Raycast()` returns an empty `MyDetectedEntityInfo` (⚠️ no-op via HTTP).
> `CanScan()` always returns `true`; `TimeUntilScan()` always returns `0`.

### 🎯 Sensor — `IMySensorBlock`

```csharp
bool Enabled { get; set; }               // On/Off
float MaxRange { get; set; }             // Sets all 6 extents to the same value
float LeftExtend { get; set; }
float RightExtend { get; set; }
float BottomExtend { get; set; }
float TopExtend { get; set; }
float FrontExtend { get; set; }
float BackExtend { get; set; }
bool DetectPlayers { get; set; }
bool DetectFloatingObjects { get; set; }
bool DetectSmallShips { get; set; }
bool DetectLargeShips { get; set; }
bool DetectStations { get; set; }
bool DetectSubgrids { get; set; }
bool DetectAsteroids { get; set; }
bool DetectOwner { get; set; }
bool DetectFriendly { get; set; }
bool DetectNeutral { get; set; }
bool DetectEnemy { get; set; }
bool IsActive { get; }                   // Is sensor triggered?
MyDetectedEntityInfo LastDetectedEntity { get; }
void DetectedEntities(List<MyDetectedEntityInfo> entities);
```

### 🔌 Connector — `IMyShipConnector`

```csharp
bool Enabled { get; set; }               // On/Off
bool ThrowOut { get; set; }
bool CollectAll { get; set; }
float PullStrength { get; set; }
MyShipConnectorStatus Status { get; }
IMyShipConnector OtherConnector { get; } // Connected connector (always null via HTTP)
bool IsLocked { get; }
bool IsConnected { get; }
bool IsParkingEnabled { get; set; }
void Connect();                          // Connect (action: Connect)
void Disconnect();                       // Disconnect (action: Disconnect)
void ToggleConnect();                    // Toggle connect (action: ToggleConnect)
```

> **Note:** `Enabled = false` disconnects, `Enabled = true` connects.

### 📏 Piston — `IMyPistonBase`

```csharp
bool Enabled { get; set; }               // On/Off
float Velocity { get; set; }
float MinLimit { get; set; }
float MaxLimit { get; set; }
float CurrentPosition { get; }
PistonStatus Status { get; }
void Extend();
void Retract();
void Reverse();
```

> **Note:** There are no `Attach()`/`Detach()` methods for pistons via the HTTP terminal API.
> Piston head attachment is a ModAPI-only feature.

### ⚙️ Motor Stator (Rotor) — `IMyMotorStator`

```csharp
bool Enabled { get; set; }               // On/Off
float Angle { get; }                     // Current angle (degrees)
float Torque { get; set; }
float BrakingTorque { get; set; }
float TargetVelocityRPM { get; set; }
float TargetVelocityRad { get; set; }
float LowerLimitDeg { get; set; }
float UpperLimitDeg { get; set; }
float LowerLimitRad { get; set; }
float UpperLimitRad { get; set; }
float Displacement { get; set; }
bool RotorLock { get; set; }
void RotateToAngle(MyRotationDirection dir, float angle, float velocity);
```

> **Note:** There are no `Attach()`/`Detach()` methods for rotors via the HTTP terminal API.
> Rotor head attachment is a ModAPI-only feature.

### 🛞 Motor Suspension — `IMyMotorSuspension`

```csharp
bool Enabled { get; set; }               // On/Off
bool Steering { get; set; }
bool Propulsion { get; set; }
float Power { get; set; }
float Strength { get; set; }
float Height { get; set; }
float MaxSteerAngle { get; set; }
float SteerAngle { get; }
float PropulsionOverride { get; set; }
float SteeringOverride { get; set; }
bool AirShockEnabled { get; set; }
float Friction { get; set; }
float Damping { get; }
bool Brake { get; set; }
```

### 🦾 Landing Gear — `IMyLandingGear`

```csharp
bool Enabled { get; set; }               // On/Off
bool IsLocked { get; }
LandingGearMode LockMode { get; set; }   // Lock mode
void Lock();                             // Lock (action: Lock)
void Unlock();                           // Unlock (action: Unlock)
void ToggleLock();                       // Toggle lock (action: ToggleLock)
```

### Projector `IMyProjector`

```csharp
bool Enabled { get; set; }               // On/Off
bool IsProjecting { get; }               // Is currently projecting?
int TotalBlocks { get; }                 // Total blocks in the blueprint
int RemainingBlocks { get; }             // Blocks not yet welded
int BuildableBlocksCount { get; }        // Buildable (projected) block count
Vector3I ProjectionOffset { get; set; }  // Position offset (grid coords)
Vector3I ProjectionRotation { get; set; }// Rotation offset
bool ShowOnlyBuildable { get; set; }     // Show only buildable blocks
void UpdateOffsetAndRotation();          // Apply offset/rotation changes
```

> **Note:** Blueprint selection (`BlueprintPath`) and `KeepProjection` are not exposed
> as terminal properties and cannot be changed via the HTTP API.

### 💻 Programmable Block — `IMyProgrammableBlock`

```csharp
bool Enabled { get; set; }               // On/Off
bool IsRunning { get; }
string TerminalRunArgument { get; }
bool TryRun(string argument);            // ⚠️ ignores argument — always runs without args
```

> **Note:** `CustomData` stores the script code (inherited from `IMyTerminalBlock`).
> There is no `Recompile()` method — use `TryRun("")` instead.

### ⛏️ Drill — `IMyShipDrill`

```csharp
bool Enabled { get; set; }               // On/Off
bool TerrainClearingMode { get; set; }
```

### 🔧 Welder — `IMyShipWelder`

```csharp
bool Enabled { get; set; }               // On/Off
bool HelpOthers { get; set; }
```

### 🔨 Grinder — `IMyShipGrinder`

```csharp
bool Enabled { get; set; }               // On/Off
```

### 🫧 Gas Tank — `IMyGasTank`

```csharp
bool Enabled { get; set; }               // On/Off
double FilledRatio { get; }              // Fill level (0..1)
float Capacity { get; }                  // Max capacity
bool Stockpile { get; set; }
bool AutoRefillBottles { get; set; }
```

### ⛽ Gas Generator — `IMyGasGenerator`

```csharp
bool Enabled { get; set; }               // On/Off
bool AutoRefill { get; set; }
bool IsProducing { get; }
bool UseConveyorSystem { get; set; }
```

### ⚡ Jump Drive — `IMyJumpDrive`

```csharp
bool Enabled { get; set; }               // On/Off
MyJumpDriveStatus Status { get; }
bool Recharge { get; set; }
float CurrentStoredPower { get; }
float MaxStoredPower { get; }
float JumpDistanceMeters { get; set; }
float MaxJumpDistanceMeters { get; }
float MinJumpDistanceMeters { get; }
float JumpDistanceRatio { get; set; }
```

> **Jump to coordinates:** Only possible via a Programmable Block script.
> The test uploads a script that calls jump, then reads status.

### 📦 Cargo Container — `IMyCargoContainer`

```csharp
// No additional members beyond IMyTerminalBlock.
// Does NOT have Enabled — extends TerminalBlockProxy directly.
```

> **Note:** Cargo containers do not expose `Enabled` or `HasInventory` as terminal properties.

### 💣 Warhead — `IMyWarhead`

```csharp
// Does NOT have Enabled — extends TerminalBlockProxy directly.
bool IsArmed { get; set; }               // ⚠️ inverted: sets "Safety" to opposite
bool IsCountingDown { get; }             // Is countdown active?
float DetonationTime { get; set; }       // Countdown in seconds
void Detonate();                         // Detonate immediately
void StartCountdown();                   // Start countdown
void StopCountdown();                    // Stop countdown
```

### 📺 Text Panel / LCD — `IMyTextPanel`

```csharp
bool Enabled { get; set; }               // On/Off

// ── IMyTextPanel ──────────────────────────
void WriteText(string text, bool append = false);
string GetText();

// ── IMyTextSurface ────────────────────────
Vector2 SurfaceSize { get; }             // ⚠️ returns (0,0) — not available via terminal
Vector2 TextureSize { get; }             // ⚠️ returns (0,0) — not available via terminal
float FontSize { get; set; }
string Font { get; set; }
Color FontColor { get; set; }
Color BackgroundColor { get; set; }
Color ScriptForegroundColor { get; set; }
Color ScriptBackgroundColor { get; set; }
float TextPadding { get; set; }
string Script { get; set; }              // Script content displayed
bool PreserveAspectRatio { get; set; }
void ClearImagesFromSelection();         // ⚠️ no-op
void AddImageToSelection(string id, bool checkExistence = false); // ⚠️ no-op
```

### 📥 Collector — `IMyCollector`

```csharp
bool Enabled { get; set; }               // On/Off
bool UseConveyorSystem { get; set; }
```

> **Note:** There is no `ThrowOutAll()` method in the proxy. Use `ExecuteBlockAction(block, "ThrowOut")` for direct action access.

### 📤 Conveyor Sorter — `IMyConveyorSorter`

```csharp
bool Enabled { get; set; }               // On/Off
bool DrainAll { get; set; }
MyConveyorSorterMode Mode { get; }       // Whitelist / Blacklist
void AddItem(MyDefinitionId id);         // ⚠️ no-op via HTTP
void RemoveItem(MyDefinitionId id);      // ⚠️ no-op via HTTP
bool IsAllowed(MyDefinitionId id);       // ⚠️ always returns false via HTTP
List<MyDefinitionId> GetFilterList();    // ⚠️ always returns empty list via HTTP
```

### 🌍 Gravity Generator — `IMyGravityGenerator`

```csharp
bool Enabled { get; set; }               // On/Off
Vector3 GravityAcceleration { get; set; }
Vector3 FieldSize { get; }               // Field dimensions (x, y, z)
```

### 🔮 Gravity Generator Sphere — `IMyGravityGeneratorSphere`

```csharp
bool Enabled { get; set; }               // On/Off
float Radius { get; set; }
float GravityAcceleration { get; set; }
```

### 🔊 Sound Block — `IMySoundBlock`

```csharp
bool Enabled { get; set; }               // On/Off
float Volume { get; set; }
float Range { get; set; }
float LoopPeriod { get; set; }           // Loop interval in seconds
string SelectedSound { get; set; }       // Selected sound ID
void Play();
void Stop();
List<string> GetSounds();                // ⚠️ no-op — always returns empty list
```

### ⏱️ Timer Block — `IMyTimerBlock`

```csharp
bool Enabled { get; set; }               // On/Off
bool Silent { get; set; }
void Trigger();                          // Trigger immediately (action: Trigger)
void StartCountdown();                   // Start the timer (action: StartCountdown)
void StopCountdown();                    // Stop the timer (action: StopCountdown)
```

### 🔘 Button Panel — `IMyButtonPanel`

```csharp
// Does NOT have Enabled — extends TerminalBlockProxy directly.
string GetButtonName(int index);                  // ⚠️ ignores index — reads "ButtonName"
void SetCustomButtonName(int index, string name); // ⚠️ ignores index — sets "ButtonName"
```

> **Note:** The `index` parameter is not supported via the terminal API — both methods operate
> on the same single `"ButtonName"` terminal property regardless of index.

### 🪂 Parachute — `IMyParachute`

```csharp
bool Enabled { get; set; }               // On/Off
bool AutoDeploy { get; set; }
float AutoDeployHeight { get; set; }     // Deployment altitude
void OpenDoor();                         // Deploy/open (action: OpenDoor)
void CloseDoor();                        // Close (action: CloseDoor)
void ToggleDoor();                       // Toggle (action: ToggleDoor)
```

### 🛡️ Safe Zone — `IMySafeZoneBlock`

```csharp
bool Enabled { get; set; }               // On/Off
bool IsSafeZoneEnabled { get; }
void EnableSafeZone(bool enable);
```

### 💨 Air Vent — `IMyAirVent`

```csharp
bool Enabled { get; set; }               // On/Off
bool Depressurize { get; set; }
VentStatus Status { get; }
bool IsPressurized();                    // Is the room pressurized?
float GetOxygenLevel();                  // Current oxygen level
```

### 🏥 Medical Room — `IMyMedicalRoom`

```csharp
bool Enabled { get; set; }               // On/Off
```

### 🔗 Merge Block — `IMyShipMergeBlock`

```csharp
bool Enabled { get; set; }               // On/Off — connect/disconnect
```

> **Note:** No dedicated proxy class exists, but merge blocks work through
> `FunctionalBlockProxy`. `Enabled = true`/`false` maps to connect/disconnect actions.
> `IsConnected` and `State` can be read via `GetBlockProperty(block, "IsConnected")`.

### 🔥 Heat Vent — `IMyHeatVent`

```csharp
bool Enabled { get; set; }               // On/Off
float PowerDependency { get; set; }
Color ColorMinimal { get; set; }
Color ColorMaximal { get; set; }
Color ColorCurrent { get; }              // Current color (read-only)
```

### ⬆️ Upgrade Module — `IMyUpgradeModule`

```csharp
bool Enabled { get; set; }               // On/Off
uint UpgradeCount { get; }
uint Connections { get; }                // Number of connected upgradable blocks
```

### ⬆️ Upgradable Block — `IMyUpgradableBlock`

```csharp
uint UpgradeCount { get; }
void GetUpgrades(Dictionary<string, float> upgrades); // ⚠️ no-op — dictionary is not populated
```

> **Note:** `IMyUpgradableBlock` is implemented by `TerminalBlockProxy`, so ALL blocks
> support this interface. `GetUpgrades()` is a no-op via HTTP.

---

## Direct Terminal Actions

Any block supports executing terminal actions directly via `ExecuteBlockAction`,
even if there is no typed proxy for it:

```csharp
// Turn off battery
_grid.ExecuteBlockAction(battery, "OnOff_Off");

// Enable Recharge mode
_grid.ExecuteBlockAction(battery, "Recharge_On");
```

To discover available actions/properties of a block, use the debug method:

```csharp
var detail = _grid.GetBlockDetail(block);
foreach (var p in detail.Properties)
    TestContext.WriteLine($"  [{p.PropertyType}] {p.Id} = {p.Value}");
foreach (var a in detail.Actions)
    TestContext.WriteLine($"  {a.Id} ({a.Name})");
```

## ⚠️ HTTP Terminal API limitations

Some properties/methods are unavailable via the HTTP terminal API and are **no-op**:

| Block | Property/Method | Reason |
|---|---|---|
| Door | `Enabled` (set) | Requires main-thread action dispatch |
| Remote Control | `IsAutoPilotEnabled` (set) | Requires GPS waypoints (ModAPI `AddWaypoint`) |
| Remote Control | `SetAutoPilotEnabled()` | Same as above |
| Remote Control | `AddWaypoint()` | ModAPI method, no terminal equivalent |
| Remote Control | `ClearWaypoints()` | ModAPI method, no terminal equivalent |
| Remote Control | `GetWaypointInfo()` | ModAPI method, no terminal equivalent |
| Laser Antenna | `IsPermanent` (set) | `isPerm` not writable via terminal |
| Button Panel | `SetCustomButtonName()` | Index parameter ignored — sets single `ButtonName` property |
| Button Panel | `GetButtonName()` | Index parameter ignored — reads single `ButtonName` property |
| Programmable Block | `TryRun(string)` | Argument ignored — always runs without args |
| Text Panel | `SurfaceSize` | Returns `(0, 0)` — not a terminal property |
| Text Panel | `TextureSize` | Returns `(0, 0)` — not a terminal property |
| Text Panel | `ClearImagesFromSelection()` | No terminal equivalent |
| Text Panel | `AddImageToSelection()` | No terminal equivalent |
| Camera | `Raycast()` | Returns empty `MyDetectedEntityInfo` |
| Conveyor Sorter | `AddItem()` / `RemoveItem()` / `IsAllowed()` / `GetFilterList()` | No terminal equivalent |
| Sound Block | `GetSounds()` | Returns empty list — no terminal equivalent |
| Upgradable Block | `GetUpgrades()` | Dictionary is never populated — no terminal equivalent |
| Merge Block | `IsConnected`, `State`, `Other` | No dedicated proxy — use `GetBlockProperty` |
