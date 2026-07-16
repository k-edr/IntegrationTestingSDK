# DLL Analysis — что уже есть в игре для интеграционных тестов

Анализ проведён на DLL из `D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64\`
(версия игры `1209024`).

**Вывод: мы не делаем лишнюю работу. В игре уже есть богатейшее API — мы строим
над ним HTTP-обёртку. Для тестового клиента нужно: либо дёргать HTTP (внешний
процесс), либо загружаться как плагин и использовать ModAPI напрямую.**

---

## 1. Архитектура API (слои)

```
┌─────────────────────────────────────────────────────┐
│ Sandbox.ModAPI (Sandbox.Common.dll)                 │
│   Базовые интерфейсы блоков: IMyTerminalBlock,      │
│   IMyFunctionalBlock, IMyCubeGrid, MyAPIGateway...  │
├─────────────────────────────────────────────────────┤
│ SpaceEngineers.Game.ModAPI (SpaceEngineers.Game.dll)│
│   SE-специфичные блоки: IMyGravityGenerator,        │
│   IMyTimerBlock, IMyButtonPanel, IMySafeZoneBlock...│
├─────────────────────────────────────────────────────┤
│ Sandbox.Game.dll — реализация                       │
│   MySession, MyCubeGrid, MyPrefabManager,           │
│   MyGridTerminalSystem, MyProgrammableBlock...      │
├─────────────────────────────────────────────────────┤
│ VRage.Game.dll — определения + ObjectBuilders       │
│   MyObjectBuilder_*, MyDefinitionBase, IMyEntities  │
├─────────────────────────────────────────────────────┤
│ VRage.dll — ядро: Vector3, MatrixD, IPlugin, Sync   │
└─────────────────────────────────────────────────────┘
```

---

## 2. Точка входа — `MyAPIGateway`

```csharp
// Sandbox.ModAPI.MyAPIGateway (Sandbox.Common.dll)
public static class MyAPIGateway
{
    public static IMySession Session { get; }       // Сессия
    public static IMyEntities Entities { get; }      // Спавн + поиск сущностей
    // Остальное — через рефлексию/касты:
    // PrefabManager, Utilities, Multiplayer, Physics, Gui, TerminalActionsHelper...
}
```

---

## 3. Спавн гридов (два пути)

### 3a. Из префаба по имени (проще всего)

```csharp
// VRage.Game.ModAPI.IMyPrefabManager (VRage.Game.dll)
// Доступ: MyAPIGateway.Session → каст к Sandbox.Game.World.MyPrefabManager

void SpawnPrefab(
    List<IMyCubeGrid> resultList,  // сюда сложат заспавненные гриды
    string prefabName,             // SubtypeId префаба
    Vector3D position,
    Vector3 forward,
    Vector3 up,
    Vector3 initialLinearVelocity,
    Vector3 initialAngularVelocity,
    string beaconName,
    SpawningOptions spawningOptions,
    long ownerId,
    bool updateSync,
    Action callback
);
```

**`SpawningOptions`** (флаги):
| Флаг | Описание |
|------|----------|
| `None` | — |
| `RotateFirstCockpitTowardsDirection` | Повернуть кокпит по направлению |
| `SpawnRandomCargo` | Случайный груз |
| `DisableDampeners` | Выключить демпферы |
| `SetNeutralOwner` | Нейтральный владелец |
| `TurnOffReactors` | Выключить реакторы |
| `DisableSave` | Не сохранять |
| `UseGridOrigin` | Использовать origin грида |
| `SetAuthorship` | Установить авторство |
| `ReplaceColor` | Заменить цвет |
| `UseOnlyWorldMatrix` | Только WorldMatrix |
| `RandomizeColor` | Случайный цвет |
| `SetNpcSpawnedGrid` | Пометить как NPC |
| `SetOwnerNobody` | Владелец — никто |

### 3b. Из ObjectBuilder (полный контроль)

```csharp
// VRage.ModAPI.IMyEntities (VRage.Game.dll)

// 1. Создать без добавления в мир (можно докрутить позицию)
IMyEntity CreateFromObjectBuilder(MyObjectBuilder_EntityBase objectBuilder);

// 2. Создать и сразу добавить
IMyEntity CreateFromObjectBuilderAndAdd(MyObjectBuilder_EntityBase objectBuilder);

// 3. Создать без инициализации
IMyEntity CreateFromObjectBuilderNoinit(MyObjectBuilder_EntityBase objectBuilder);

// 4. Ремап EntityId (чтобы не было конфликтов)
void RemapObjectBuilderCollection(IEnumerable<MyObjectBuilder_EntityBase> builders);
void RemapObjectBuilder(MyObjectBuilder_EntityBase builder);

// 5. Удалить
void RemoveEntity(IMyEntity entity);
void MarkForClose(IMyEntity entity);

// 6. Найти свободное место
Vector3D? FindFreePlace(Vector3D basePos, float radius,
    int maxTestCount, int testsPerDistance, float stepSize);
```

### Blueprint → ObjectBuilder (загрузка bp.sbc)

```csharp
// VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen
// (может стать internal в будущих версиях!)

using (var stream = File.OpenRead(bpFilePath))
{
    MyObjectBuilderSerializerKeen.DeserializeXML(
        stream,
        out MyObjectBuilder_Base obj,
        typeof(MyObjectBuilder_Definitions)
    );
    var definitions = obj as MyObjectBuilder_Definitions;
    var shipBp = definitions.ShipBlueprints[0];
    var grids = shipBp.CubeGrids; // MyObjectBuilder_CubeGrid[]
}
```

### `MyObjectBuilder_CubeGrid` — ключевые поля

| Поле | Тип | Описание |
|------|-----|----------|
| `EntityId` | `long` | ID сущности |
| `GridSizeEnum` | `MyCubeSize` | Large / Small |
| `IsStatic` | `bool` | Статичный |
| `CubeBlocks` | `List<MyObjectBuilder_CubeBlock>` | **Блоки грида!** |
| `PositionAndOrientation` | `MyPositionAndOrientation?` | Позиция + ориентация |
| `LinearVelocity` | `SerializableVector3` | Начальная скорость |
| `AngularVelocity` | `SerializableVector3` | Угловая скорость |
| `DisplayName` | `string` | Имя грида |
| `DampenersEnabled` | `bool` | Демпферы |
| `IsPowered` | `bool` | Запитан? |
| `ConveyorLines` | `List<...>` | Конвейерные линии |
| `BlockGroups` | `List<...>` | Группы блоков |

---

## 4. Поиск и доступ к сущностям

```csharp
// IMyEntities

bool TryGetEntityById(long id, out IMyEntity entity);
bool TryGetEntityByName(string name, out IMyEntity entity);
IMyEntity GetEntityById(long entityId);
IMyEntity GetEntityByName(string name);
bool EntityExists(string name);
bool EntityExists(long entityId);
bool Exist(IMyEntity entity);

// Продвинутый поиск
IMyEntity GetEntity(Func<IMyEntity, bool> match);
void GetEntities(HashSet<IMyEntity> entities, Func<IMyEntity, bool> collect);
List<IMyEntity> GetEntitiesInAABB(BoundingBoxD box);
List<IMyEntity> GetEntitiesInSphere(BoundingSphereD sphere);
List<IMyEntity> GetIntersectionWithSphere(BoundingSphereD sphere);

// Проверки
bool IsRaycastBlocked(Vector3D pos, Vector3D target);
bool IsSpherePenetrating(BoundingSphereD bs);
bool IsInsideWorld(Vector3D pos);
bool IsInsideVoxel(Vector3D pos);
```

---

## 5. Работа с блоками (Terminal System)

### 5a. Получение блоков грида

```csharp
// Sandbox.ModAPI.IMyTerminalActionsHelper
public interface IMyTerminalActionsHelper
{
    // Получить TerminalSystem для грида
    IMyGridTerminalSystem GetTerminalSystemForGrid(IMyCubeGrid grid);

    // Действия блоков
    void GetActions(Type blockType, List<ITerminalAction> resultList, Func<...> collect);
    ITerminalAction GetActionWithName(string actionId, Type blockType);
    void SearchActionsOfName(string name, Type blockType, List<...> resultList, ...);

    // Свойства блоков
    ITerminalProperty GetProperty(string id, Type blockType);
    void GetProperties(Type blockType, List<ITerminalProperty> resultList, ...);
}
```

### 5b. `IMyGridTerminalSystem`

```csharp
public interface IMyGridTerminalSystem
{
    void GetBlocks(List<IMyTerminalBlock> blocks);
    void GetBlockGroups(List<IMyBlockGroup> blockGroups);
    void GetBlocksOfType<T>(List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect);
    void SearchBlocksOfName(string name, List<IMyTerminalBlock> blocks, Func<...> collect);
    IMyTerminalBlock GetBlockWithName(string name);
    IMyBlockGroup GetBlockGroupWithName(string name);
}
```

### 5c. `IMyTerminalBlock` (базовый интерфейс всех блоков)

```csharp
// Sandbox.ModAPI.IMyTerminalBlock
bool IsDetailedInfoDirty { get; }
void RefreshCustomInfo();
void SetDetailedInfoDirty();
string GetDetailedInfo();     // StringBuilder на самом деле
bool IsInSameLogicalGroupAs(IMyTerminalBlock other);
bool IsSameConstructAs(IMyTerminalBlock other);

// Sandbox.ModAPI.Ingame.IMyTerminalBlock (для скриптов)
string CustomName { get; set; }
string CustomData { get; set; }    // ← Custom Data блока!
string DetailedInfo { get; }
bool ShowOnHUD { get; set; }
bool ShowInTerminal { get; set; }
bool HasLocalPlayerAccess();
bool HasPlayerAccess(long playerId, MyRelationsBetweenPlayerAndBlock relation);
void GetActions(List<ITerminalAction> resultList, Func<ITerminalAction, bool> collect);
ITerminalAction GetActionWithName(string name);
ITerminalProperty GetProperty(string id);
void GetProperties(List<ITerminalProperty> resultList, Func<ITerminalProperty, bool> collect);
```

### 5d. `IMyFunctionalBlock`

```csharp
// Ingame:
bool Enabled { get; set; }
void RequestEnable(bool enable);
```

### 5e. Выполнение действий и установка свойств

```csharp
// ITerminalAction
string Id { get; }
string Name { get; }
void Apply(IMyCubeBlock block);          // выполнить
void Apply(IMyCubeBlock block, ListReader<TerminalActionParameter> params);
void WriteValue(IMyCubeBlock block, object value);

// ITerminalProperty<T>
string Id { get; }
T GetValue(IMyCubeBlock block);
void SetValue(IMyCubeBlock block, T value);
```

---

## 6. Ключевые интерфейсы блоков (полная карта)

### Контроллеры

```csharp
// IMyShipController (Sandbox.Common) — базовый для Cockpit и RemoteControl
bool CanControlShip { get; }
bool IsUnderControl { get; }
bool ControlWheels { get; set; }
bool ControlThrusters { get; set; }
bool HandBrake { get; set; }
bool DampenersOverride { get; set; }
Vector3 MoveIndicator { get; }           // WASD
Vector2 RotationIndicator { get; }       // Мышь
float RollIndicator { get; }            // Q/E
Vector3D CenterOfMass { get; }
bool IsMainCockpit { get; set; }

// Навигация
Vector3 GetNaturalGravity();
Vector3 GetArtificialGravity();
Vector3 GetTotalGravity();
double GetShipSpeed();
MyShipVelocities GetShipVelocities();
float CalculateShipMass();
bool TryGetPlanetPosition(out Vector3D position);
bool TryGetPlanetElevation(MyPlanetElevation detail, out double elevation);

// IMyCockpit — доп. свойства
float OxygenCapacity { get; }
float OxygenFilledRatio { get; }
bool IsOccupied { get; }    // ModAPI
void AttachPilot(IMyCharacter pilot);  // ModAPI
void RemovePilot();                     // ModAPI
```

### Remote Control

```csharp
// IMyRemoteControl
bool IsAutoPilotEnabled { get; set; }
float SpeedLimit { get; set; }
FlightMode FlightMode { get; set; }
Base6Directions.Direction Direction { get; set; }
MyWaypointInfo CurrentWaypoint { get; }
bool WaitForFreeWay { get; set; }

void ClearWaypoints();
void GetWaypointInfo(List<MyWaypointInfo> waypoints);
void AddWaypoint(Vector3D coords, string name);
void SetAutoPilotEnabled(bool enabled);
void SetCollisionAvoidance(bool enabled);
void SetDockingMode(bool enabled);

// ModAPI:
bool GetNearestPlayer(out Vector3D playerPosition);
bool GetFreeDestination(Vector3D originalDestination, float checkRadius, out Vector3D freeDestination);
```

### Programmable Block

```csharp
// IMyProgrammableBlock
string ProgramData { get; set; }        // Исходный код
string StorageData { get; set; }        // Storage (строка)
bool HasCompileErrors { get; }

void Recompile();                        // Скомпилировать
void Run();                              // Запустить без аргументов
void Run(string argument);               // Запустить с аргументом
void Run(string argument, UpdateType updateSource);
bool TryRun(string argument);            // Безопасный запуск
```

### Камеры и сенсоры

```csharp
// IMyCameraBlock
bool IsActive { get; }
double AvailableScanRange { get; }
bool EnableRaycast { get; set; }
double RaycastConeLimit { get; set; }
double RaycastDistanceLimit { get; set; }
float RaycastTimeMultiplier { get; set; }

MyDetectedEntityInfo Raycast(double distance, float pitch, float yaw);
MyDetectedEntityInfo Raycast(Vector3D targetPos);
MyDetectedEntityInfo Raycast(double distance, Vector3D targetDirection);
bool CanScan(double distance);
int TimeUntilScan(double distance);

// IMySensorBlock
float MaxRange { get; set; }
float LeftExtend, RightExtend, BottomExtend, TopExtend, BackExtend { get; set; }
bool DetectPlayers, DetectFloatingObjects, DetectSmallShips,
     DetectLargeShips, DetectStations, DetectSubgrids, DetectAsteroids { get; set; }
bool DetectOwner, DetectFriendly, DetectNeutral, DetectEnemy { get; set; }
bool IsActive { get; }
MyDetectedEntityInfo LastDetectedEntity { get; }
void DetectedEntities(List<MyDetectedEntityInfo> entities);
```

### Движки

```csharp
// IMyThrust
float ThrustOverride { get; set; }
float ThrustOverridePercentage { get; set; }
float MaxThrust { get; }
float MaxEffectiveThrust { get; }
float CurrentThrust { get; }
float CurrentThrustPercentage { get; }
Base6Directions.Direction GridThrustDirection { get; }

// IMyGyro
float GyroPower { get; set; }
bool GyroOverride { get; set; }
float Yaw { get; set; }
float Pitch { get; set; }
float Roll { get; set; }
```

### Энергия

```csharp
// IMyBatteryBlock
bool HasCapacityRemaining { get; }
float CurrentStoredPower { get; set; }
float MaxStoredPower { get; }
float CurrentInput { get; }
float MaxInput { get; }
bool IsCharging { get; }
ChargeMode ChargeMode { get; set; }
bool OnlyRecharge, OnlyDischarge, SemiautoEnabled { get; set; }

// IMyReactor
float PowerOutputMultiplier { get; set; }  // ModAPI
bool UseConveyorSystem { get; set; }       // Ingame
```

### Коннекторы, поршни, роторы

```csharp
// IMyShipConnector
IMyShipConnector OtherConnector { get; }    // ModAPI
bool IsLocked { get; }
bool IsConnected { get; }
MyShipConnectorStatus Status { get; }
bool ThrowOut { get; set; }
bool CollectAll { get; set; }
float PullStrength { get; set; }
bool IsParkingEnabled { get; set; }
void Connect();
void Disconnect();
void ToggleConnect();

// IMyPistonBase
float Velocity { get; set; }
float MinLimit, MaxLimit { get; set; }
float CurrentPosition { get; }
PistonStatus Status { get; }
void Extend();
void Retract();
void Reverse();
void MoveToPosition(float position, float velocity);
void Attach(IMyPistonTop top, bool updateSync);  // ModAPI

// IMyMotorStator
float Angle { get; }                      // Текущий угол
float Torque { get; set; }
float BrakingTorque { get; set; }
float TargetVelocityRad { get; set; }
float TargetVelocityRPM { get; set; }
float LowerLimitRad, UpperLimitRad { get; set; }
float LowerLimitDeg, UpperLimitDeg { get; set; }
float Displacement { get; set; }
bool RotorLock { get; set; }
void RotateToAngle(MyRotationDirection dir, float angle, float velocity);
```

### Проектор

```csharp
// IMyProjector
IMyCubeGrid ProjectedGrid { get; }                    // ModAPI
Vector3I ProjectionOffset { get; set; }
Vector3I ProjectionRotation { get; set; }
bool IsProjecting { get; }
int TotalBlocks, RemainingBlocks { get; }
int BuildableBlocksCount { get; }
bool ShowOnlyBuildable { get; set; }

void SetProjectedGrid(MyObjectBuilder_CubeGrid grid); // ModAPI
bool CanBuild(IMySlimBlock block, bool checkHavokIntersections); // ModAPI
void Build(IMySlimBlock block, long owner, long builder, bool requestInstant); // ModAPI
bool LoadBlueprint(string path);                       // ModAPI
bool LoadRandomBlueprint(string searchPattern);        // ModAPI
```

### Прочие важные блоки

```csharp
// IMyDoor — ingame
bool Open { get; }
DoorStatus Status { get; }
float OpenRatio { get; }
void OpenDoor();
void CloseDoor();
void ToggleDoor();

// IMyWarhead — ingame
bool IsCountingDown { get; }
float DetonationTime { get; set; }
bool IsArmed { get; set; }
void StartCountdown();
void StopCountdown();
void Detonate();

// IMyBeacon / IMyRadioAntenna — ingame
float Radius { get; set; }
string HudText { get; set; }
bool ShowShipName { get; set; }
bool IsBroadcasting { get; }
bool EnableBroadcasting { get; set; }

// IMyLightingBlock — ingame
float Radius { get; set; }
float Intensity { get; set; }
float Falloff { get; set; }
float BlinkIntervalSeconds { get; set; }
float BlinkLength { get; set; }
float BlinkOffset { get; set; }
Color Color { get; set; }

// IMyTimerBlock — ModAPI (действия через терминал)
// IMyButtonPanel — ModAPI (действия через терминал)

// IMyLandingGear — ModAPI
// IMyGravityGenerator — ModAPI
// IMySafeZoneBlock — ModAPI
// IMySoundBlock — ModAPI
```

---

## 7. Управление сессией

```csharp
// IMySession (VRage.Game.ModAPI) — 60+ свойств, 30+ методов
public interface IMySession
{
    // Состояние
    bool Ready { get; }
    bool CreativeMode { get; set; }
    bool SurvivalMode { get; }
    string Name { get; }
    DateTime GameDateTime { get; set; }
    TimeSpan ElapsedPlayTime { get; }

    // Настройки
    float AssemblerEfficiencyMultiplier, AssemblerSpeedMultiplier { get; set; }
    float RefinerySpeedMultiplier { get; set; }
    float WelderSpeedMultiplier, GrinderSpeedMultiplier { get; set; }
    float InventoryMultiplier, BlocksInventorySizeMultiplier { get; set; }
    bool AutoHealing { get; set; }
    bool EnableCopyPaste { get; set; }
    bool WeaponsEnabled { get; set; }
    bool ThrusterDamage { get; set; }

    // Камера и игрок
    IMyCameraController CameraController { get; }
    IMyCamera Camera { get; }
    IMyPlayer LocalHumanPlayer { get; }
    IMyControllableEntity ControlledObject { get; set; }

    // Компоненты
    T GetComponentByInterfaceType<T>();
    bool TryGetComponentByInterfaceType<T>(out T component);
    void RegisterComponent(MySessionComponentBase comp, MyUpdateOrder order, int priority);
    void UnregisterComponent(MySessionComponentBase comp);

    // Сохранение
    bool Save(string customSaveName);
    MyObjectBuilder_Checkpoint GetCheckpoint(string saveName);

    // Права
    MyPromoteLevel GetUserPromoteLevel(ulong steamId);
    bool IsUserAdmin(ulong steamId);
    bool HasCreativeRights { get; }
    bool HasAdminPrivileges { get; }

    // Коллекции
    IMyFactionCollection Factions { get; }
    IMyDamageSystem DamageSystem { get; }
    IMyGpsCollection GPS { get; }
    IMyVoxelMaps VoxelMaps { get; }
    BoundingBoxD WorldBoundaries { get; }
}
```

---

## 8. Работа с файлами и уведомлениями

```csharp
// IMyUtilities
void ShowNotification(string message, int disappearTimeMs, string font = "White");
IMyHudNotification CreateNotification(string message, int disappearTimeMs, string font);
void ShowMessage(string sender, string messageText);
void SendMessage(string messageText);

// Файлы
bool FileExistsInLocalStorage(string file, Type callingType);
bool FileExistsInWorldStorage(string file, Type callingType);
TextReader ReadFileInLocalStorage(string file, Type callingType);
TextWriter WriteFileInLocalStorage(string file, Type callingType);
BinaryReader ReadBinaryFileInLocalStorage(string file, Type callingType);
BinaryWriter WriteBinaryFileInLocalStorage(string file, Type callingType);
void DeleteFileInLocalStorage(string file, Type callingType);
// ...то же для WorldStorage, GlobalStorage

// Сериализация
string SerializeToXML<T>(T obj);
T SerializeFromXML<T>(string buffer);

// Invoke на game thread
void InvokeOnGameThread(Action action, string invokerName = "GridSpawner");
```

---

## 9. События (можно подписаться!)

```csharp
// События из Sandbox.Game.dll
PrefabSpawnedEvent       // Префаб заспавнен
RespawnShipSpawnedEvent  // Корабль респавна заспавнен
GridJumpedEvent          // Прыжок совершён
ButtonPanelEvent         // Кнопка нажата
ConnectorStateChangedEvent
RemoteControlChangedEvent
GridPowerGenerationStateChangedEvent
BlockDamagedEvent / BlockEvent
RoomFullyPressurizedEvent
```

---

## 10. Выводы для дизайна тестового клиента

### Что уже есть на стороне игры (НЕ надо писать заново):

| Возможность | Где взять |
|-------------|-----------|
| Спавн грида по имени префаба | `IMyPrefabManager.SpawnPrefab()` |
| Спавн из ObjectBuilder (blueprint) | `IMyEntities.CreateFromObjectBuilderAndAdd()` |
| Загрузка bp.sbc | `MyObjectBuilderSerializerKeen.DeserializeXML()` |
| Поиск свободного места | `IMyEntities.FindFreePlace()` |
| Список всех гридов | `IMyEntities.GetEntities()` |
| Блоки на гриде | `IMyGridTerminalSystem.GetBlocks()` / `GetBlocksOfType<T>()` |
| Выполнение действия блока | `ITerminalAction.Apply()` |
| Установка свойства блока | `ITerminalProperty<T>.SetValue()` |
| Custom Data | `IMyTerminalBlock.CustomData` |
| Компиляция и запуск PB | `IMyProgrammableBlock.Recompile()`, `Run()`, `Run(string)` |
| Позиция/скорость грида | `IMyCubeGrid.PositionComp.GetPosition()`, физика |
| Навигация (координаты, гравитация) | `IMyShipController.GetShipSpeed()`, `TryGetPlanetPosition()` |
| Автопилот | `IMyRemoteControl.SetAutoPilotEnabled()`, `AddWaypoint()` |
| Raycast (камеры) | `IMyCameraBlock.Raycast()` |
| Сенсоры | `IMySensorBlock.DetectedEntities()` |
| Удаление грида | `IMyEntities.RemoveEntity()` |
| Уведомления в HUD | `IMyUtilities.ShowNotification()` |
| Логирование в файл | `IMyUtilities.WriteFileInLocalStorage()` |

### Два подхода к тестовому клиенту:

#### Подход A: Внешний процесс → HTTP (текущий путь)

```
TestProcess.exe ──HTTP──► GridSpawner Plugin (порт 9997) ──ModAPI──► Игра
```

- **+** Тесты не зависят от игры, можно запускать любые фреймворки
- **+** Можно использовать xUnit/NUnit без ограничений
- **−** Задержка HTTP на каждый вызов
- **−** Нужна HTTP-обёртка над каждым действием

#### Подход B: Плагин-тест внутри игры (ModAPI напрямую)

```
Game ──PluginLoader──► TestPlugin.dll ──ModAPI──► Игра
```

- **+** Прямой доступ ко всем ModAPI без посредников
- **+** Нулевая задержка
- **−** Тесты живут внутри процесса игры
- **−** Сложнее с отладкой, надо перезапускать игру

#### Рекомендация: гибрид

Сделать **общий интерфейс** (`IGridSpawnerClient`), две реализации:
- `GridSpawnerHttpClient` — дёргает HTTP API (Подход A)
- `GridSpawnerModApiClient` — использует ModAPI напрямую (Подход B) — для тестов, которые
  загружаются как плагин

Тогда один и тот же тест можно запустить и извне (NUnit), и внутри игры.

### Что нужно добавить в HTTP API для полноценного тестирования:

| Текущее API | Не хватает |
|-------------|------------|
| `POST /spawn` | ✅ Есть |
| `GET /grids` | ✅ Есть |
| `GET /grids/{id}` | ✅ Есть |
| `DELETE /grids/{id}` | ✅ Есть |
| `GET /grids/{id}/blocks` | ✅ Есть |
| `POST .../action` | ✅ Есть |
| `GET/PUT .../properties/{id}` | ✅ Есть |
| `GET /grids/{id}/position` | ❌ Нет — позиция грида |
| `GET /grids/{id}/velocity` | ❌ Нет — скорость |
| `POST /grids/{id}/pb/compile` | ❌ Нет — компиляция PB |
| `POST /grids/{id}/pb/run` | ❌ Нет — запуск PB |
| `POST /grids/{id}/controller/move` | ❌ Нет — управление кораблём |
| `GET /grids/{id}/waypoints` | ❌ Нет — маршрут |
| `POST /grids/{id}/waypoints` | ❌ Нет — добавить точку |
| `GET /grids/{id}/sensors` | ❌ Нет — данные сенсоров |
| `POST /grids/{id}/camera/raycast` | ❌ Нет — raycast |
| `WebSocket /events` | ❌ Нет — подписка на события |

