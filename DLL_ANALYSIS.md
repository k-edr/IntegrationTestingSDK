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


========================================================================================================================
SPACE ENGINEERS - SANDBOX.COMMON.DLL - PUBLIC INTERFACES DUMP
Generated: 17-Jul-26 11:53:02
========================================================================================================================

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyAccessAnyoneCanUse
FullName:  Sandbox.ModAPI.IMyAccessAnyoneCanUse

    Properties:
        Boolean AnyoneCanUse { get;  }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyAdvancedDoor
FullName:  Sandbox.ModAPI.IMyAdvancedDoor
Inherits:  IMyDoor, IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyDoor, IMyAdvancedDoor


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyAiBlockComponent
FullName:  Sandbox.ModAPI.IMyAiBlockComponent

    Properties:
        MyAiBlockType AiBlockType { get; set; }
        IMyEntity Entity { get;  }
        Func<String> HudErrorStringGetter { get; set; }
        Boolean IsActivated { get;  }
    Methods:
        Void Deactivate()
    Events:
        event Action OnActivatedChanged

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyAirtightDoorBase
FullName:  Sandbox.ModAPI.IMyAirtightDoorBase
Inherits:  IMyDoor, IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyDoor, IMyAirtightDoorBase


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyAirtightHangarDoor
FullName:  Sandbox.ModAPI.IMyAirtightHangarDoor
Inherits:  IMyAirtightDoorBase, IMyDoor, IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyDoor, IMyAirtightDoorBase, IMyAirtightHangarDoor


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyAirtightSlideDoor
FullName:  Sandbox.ModAPI.IMyAirtightSlideDoor
Inherits:  IMyAirtightDoorBase, IMyDoor, IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyDoor, IMyAirtightDoorBase, IMyAirtightSlideDoor


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyAiRvoSessionComponent
FullName:  Sandbox.ModAPI.IMyAiRvoSessionComponent

    Methods:
        Boolean IsCubeGridSimulationComputed(IMyCubeGrid grid, out Vector3D newVelocity)
        Void RequestUpdate(IMyCubeGrid grid)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyAssembler
FullName:  Sandbox.ModAPI.IMyAssembler
Inherits:  IMyProductionBlock, IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyProductionBlock, IMyAssembler

    Events:
        event Action<IMyAssembler> CurrentModeChanged
        event Action<IMyAssembler> CurrentProgressChanged
        event Action<IMyAssembler> CurrentStateChanged

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyAttachableTopBlock
FullName:  Sandbox.ModAPI.IMyAttachableTopBlock
Inherits:  IMyCubeBlock, IMyCubeBlock, IMyEntity, IMyEntity, IMyAttachableTopBlock

    Properties:
        IMyMechanicalConnectionBlock Base { get;  }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyAttackPatternComponent
FullName:  Sandbox.ModAPI.IMyAttackPatternComponent
Inherits:  IMyAttackPatternComponent

    Properties:
        Boolean IsSelected { get; set; }
    Methods:
        Void AppendCustomInfo(StringBuilder stringBuilder)
        Void CreateTerminalInterfaceControls()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyBasicMissionAutopilot
FullName:  Sandbox.ModAPI.IMyBasicMissionAutopilot
Inherits:  IMyBasicMissionComponent, IMyBasicMissionComponent

    Properties:
        FlightMode Mode { get;  }
    Methods:
        Void AddWaypoint(IMyGps gps)
        Void AddWaypoint(IMyAutopilotWaypoint waypoint)
        Void ClearWaypoints()
        IMyAutopilotWaypoint GetLastWaypoint()
        Void GetWaypoints(List<IMyAutopilotWaypoint> points)
        Void RemoveWaypoint(IMyAutopilotWaypoint waypoint)
        Void SetMode(FlightMode mode)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyBasicMissionBlock
FullName:  Sandbox.ModAPI.IMyBasicMissionBlock
Inherits:  IMyBasicMissionBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Methods:
        Boolean TryGetMission(Int64 id, out IMyBasicMissionComponent mission)
        Boolean TryGetSelectedMission(out IMyBasicMissionComponent mission)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyBasicMissionComponent
FullName:  Sandbox.ModAPI.IMyBasicMissionComponent
Inherits:  IMyBasicMissionComponent

    Methods:
        Void CreateTerminalInterfaceControls()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyBasicMissionFollowHome
FullName:  Sandbox.ModAPI.IMyBasicMissionFollowHome
Inherits:  IMyBasicMissionComponent, IMyBasicMissionComponent, IMyBasicMissionFollowHome

    Methods:
        Void GoHome(IMyGps gps)
        Void GoHome(Int64 entityId)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyBasicMissionFollowPlayer
FullName:  Sandbox.ModAPI.IMyBasicMissionFollowPlayer
Inherits:  IMyBasicMissionComponent, IMyBasicMissionComponent, IMyBasicMissionFollowPlayer

    Methods:
        Void FollowPlayer(Int64 identityId)
        Void StopFollowing()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyBatteryBlock
FullName:  Sandbox.ModAPI.IMyBatteryBlock
Inherits:  IMyPowerProducer, IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyPowerProducer, IMyBatteryBlock


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyBeacon
FullName:  Sandbox.ModAPI.IMyBeacon
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyBeacon


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyBlockGroup
FullName:  Sandbox.ModAPI.IMyBlockGroup
Inherits:  IMyBlockGroup

    Methods:
        Void GetBlocks(List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, Boolean> collect)
        Void GetBlocksOfType(List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, Boolean> collect)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyCameraBlock
FullName:  Sandbox.ModAPI.IMyCameraBlock
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyCameraBlock, IMyCameraController

    Properties:
        Boolean IsActiveLocal { get;  }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyCargoContainer
FullName:  Sandbox.ModAPI.IMyCargoContainer
Inherits:  IMyTerminalBlock, IMyCubeBlock, IMyCubeBlock, IMyEntity, IMyEntity, IMyTerminalBlock, IMyCargoContainer


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyCharacterAimAssistComponent
FullName:  Sandbox.ModAPI.IMyCharacterAimAssistComponent
Inherits:  IMyCharacterAimAssistComponent


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyCockpit
FullName:  Sandbox.ModAPI.IMyCockpit
Inherits:  IMyShipController, IMyTerminalBlock, IMyCubeBlock, IMyCubeBlock, IMyEntity, IMyEntity, IMyTerminalBlock, IMyShipController, IMyControllableEntity, IMyTargetingCapableBlock, IMyCockpit, IMyTextSurfaceProvider, IMyCameraController, IMyTextSurfaceProvider

    Properties:
        Boolean IsOccupied { get;  }
        Single OxygenFilledRatio { get; set; }
    Methods:
        Void AttachPilot(IMyCharacter pilot, Int32 animation)
        Void AttachPilot(IMyCharacter pilot)
        Void RemovePilot()
    Events:
        event Action<IMyCockpit> IsOccupiedChanged

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyCollector
FullName:  Sandbox.ModAPI.IMyCollector
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyCollector


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyConveyor
FullName:  Sandbox.ModAPI.IMyConveyor
Inherits:  IMyCubeBlock, IMyCubeBlock, IMyEntity, IMyEntity, IMyConveyor


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyConveyorSorter
FullName:  Sandbox.ModAPI.IMyConveyorSorter
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyConveyorSorter


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyConveyorTube
FullName:  Sandbox.ModAPI.IMyConveyorTube
Inherits:  IMyCubeBlock, IMyCubeBlock, IMyEntity, IMyEntity, IMyConveyorTube


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyCryoChamber
FullName:  Sandbox.ModAPI.IMyCryoChamber
Inherits:  IMyCockpit, IMyShipController, IMyTerminalBlock, IMyCubeBlock, IMyCubeBlock, IMyEntity, IMyEntity, IMyTerminalBlock, IMyShipController, IMyControllableEntity, IMyTargetingCapableBlock, IMyCockpit, IMyTextSurfaceProvider, IMyCameraController, IMyTextSurfaceProvider, IMyCryoChamber


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyDecoy
FullName:  Sandbox.ModAPI.IMyDecoy
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyDecoy


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyDefensiveCombatBlock
FullName:  Sandbox.ModAPI.IMyDefensiveCombatBlock
Inherits:  IMyDefensiveCombatBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Nullable<Int64> FleeBeaconEntityId { get; set; }
        Nullable<GpsInfo> FleeGps { get; set; }
        IMySearchEnemyComponent SearchEnemyComponent { get;  }
    Methods:
        Void DoSearch()
        Void ForceFlee()
    Events:
        event Action<IMyDefensiveCombatBlock, Boolean> IsFleeingChanged

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyDistanceToLockedTarget
FullName:  Sandbox.ModAPI.IMyDistanceToLockedTarget

    Properties:
        Single DistanceToLockedTarget { get;  }
        IMyEntity Entity { get;  }
    Events:
        event Action<IMyDistanceToLockedTarget, Single, Single> DistanceToLockedTargetChanged

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyDoor
FullName:  Sandbox.ModAPI.IMyDoor
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyDoor

    Properties:
        Boolean IsFullyClosed { get;  }
    Events:
        event Action<Boolean> DoorStateChanged
        event Action<IMyDoor> OnDoorClosed
        event Action<IMyDoor> OnDoorOpened
        event Action<IMyDoor, Boolean> OnDoorStateChanged

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyEmotionControllerBlock
FullName:  Sandbox.ModAPI.IMyEmotionControllerBlock
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyEmotionControllerBlock


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyEventComponentWithGui
FullName:  Sandbox.ModAPI.IMyEventComponentWithGui
Inherits:  IMyEventControllerEntityComponent

    Properties:
        Boolean IsBlocksListUsed { get;  }
        Boolean IsConditionSelectionUsed { get;  }
        Boolean IsThresholdUsed { get;  }
    Methods:
        Void AddBlocks(List<IMyTerminalBlock> blocks)
        Boolean IsBlockValidForList(IMyTerminalBlock block)
        Void NotifyValuesChanged()
        Void RemoveBlocks(IEnumerable<IMyTerminalBlock> blocks)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyEventControllerBlock
FullName:  Sandbox.ModAPI.IMyEventControllerBlock
Inherits:  IMyEventControllerBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        IMyEventControllerEntityComponent SelectedEvent { get;  }
    Methods:
        Void TriggerAction(Int32 startIndex)
    Events:
        event Action<Int32> ActionTriggered

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyEventControllerEntityComponent
FullName:  Sandbox.ModAPI.IMyEventControllerEntityComponent

    Properties:
        MyStringId EventDisplayName { get;  }
        Boolean IsSelected { get; set; }
        Int64 UniqueSelectionId { get;  }
        String YesNoToolbarNoDescription { get;  }
        String YesNoToolbarYesDescription { get;  }
    Methods:
        Void CreateTerminalInterfaceControls()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyExhaustBlock
FullName:  Sandbox.ModAPI.IMyExhaustBlock
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Methods:
        Void SelectEffect(String name)
        Void StartEffects()
        Void StopEffects()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyExtendedPistonBase
FullName:  Sandbox.ModAPI.IMyExtendedPistonBase
Inherits:  IMyPistonBase, IMyMechanicalConnectionBlock, IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyMechanicalConnectionBlock, IMyPistonBase, IMyExtendedPistonBase


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyFarmPlotLogic
FullName:  Sandbox.ModAPI.IMyFarmPlotLogic
Inherits:  IMyFarmPlotLogic

    Methods:
        Boolean Harvest(IMyInventory inventory, Boolean removeDeadPlant)
        Boolean PlantSeed(MyDefinitionId seedItemDefinitionId)
        Boolean RemovePlant(Boolean playEffects)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyFlightMovementBlock
FullName:  Sandbox.ModAPI.IMyFlightMovementBlock
Inherits:  IMyFlightMovementBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        String AditionalDetailInfo { get; set; }
        DirectionFlags FacingDirection { get; set; }
        Boolean IsPowered { get;  }
    Methods:
        Void AddWaypoint(IMyAutopilotWaypoint waypoint)
        Void RemoveWaypoint(IMyAutopilotWaypoint waypoint)
        Void SetCollisionAvoidanceErrorState(Boolean state)
    Events:
        event Action<IMyFlightMovementBlock> AutopilotSpeedLimitValueChanged
        event Action<IMyFlightMovementBlock, IMyAutopilotWaypoint> OnWaypointReached

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyFunctionalBlock
FullName:  Sandbox.ModAPI.IMyFunctionalBlock
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Boolean IsUpdateTimerCreated { get;  }
        Boolean IsUpdateTimerEnabled { get;  }
    Methods:
        UInt32 GetFramesFromLastTrigger()
    Events:
        event Action<IMyTerminalBlock> EnabledChanged
        event Action<IMyFunctionalBlock> UpdateTimerTriggered

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyGasGenerator
FullName:  Sandbox.ModAPI.IMyGasGenerator
Inherits:  IMyGasGenerator, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Single PowerConsumptionMultiplier { get; set; }
        Single ProductionCapacityMultiplier { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyGasTank
FullName:  Sandbox.ModAPI.IMyGasTank
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyGasTank

    Methods:
        Void ChangeFilledRatio(Double newFilledRatio, Boolean updateSync)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyGlobalEncounterComponent
FullName:  Sandbox.ModAPI.IMyGlobalEncounterComponent

    Properties:
        Int64 EncounterId { get;  }
        Boolean GpsCreated { get;  }
        Boolean RegisteredAsEncounter { get;  }
        String SpawnGroupName { get;  }
    Methods:
        Void AddGlobalEncounterComponent(IMyCubeGrid grid)
        Void UnregisterFromEncounter()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyGridProgram
FullName:  Sandbox.ModAPI.IMyGridProgram

    Properties:
        Action<String> Echo { get; set; }
        IMyGridTerminalSystem GridTerminalSystem { get; set; }
        Boolean HasMainMethod { get;  }
        Boolean HasSaveMethod { get;  }
        Func<IMyIntergridCommunicationSystem> IGC_ContextGetter {  set; }
        IMyProgrammableBlock Me { get; set; }
        IMyGridProgramRuntimeInfo Runtime { get; set; }
        String Storage { get; set; }
        IMyGridProgramWorldInfo World { get; set; }
    Methods:
        Void Main(String argument, UpdateType updateSource)
        Void Main(String argument)
        Void Save()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyGridTerminalSystem
FullName:  Sandbox.ModAPI.IMyGridTerminalSystem
Inherits:  IMyGridTerminalSystem

    Methods:
        Void GetBlockGroups(List<IMyBlockGroup> blockGroups)
        IMyBlockGroup GetBlockGroupWithName(String name)
        Void GetBlocks(List<IMyTerminalBlock> blocks)
        Void GetBlocksOfType(List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, Boolean> collect)
        IMyTerminalBlock GetBlockWithName(String name)
        Void SearchBlocksOfName(String name, List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, Boolean> collect)
    Events:
        event Action<IMyBlockGroup> GroupAdded
        event Action<IMyBlockGroup> GroupRemoved

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyGyro
FullName:  Sandbox.ModAPI.IMyGyro
Inherits:  IMyGyro, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Single GyroStrengthMultiplier { get; set; }
        Single PowerConsumptionMultiplier { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyHazardExposureComponent
FullName:  Sandbox.ModAPI.IMyHazardExposureComponent

    Methods:
        Void AddSource(IMyHazardSource src)
        Void GetCurrentlyProcessedSources(List<IMyHazardSource> result)
        Void RemoveSource()
        Boolean RemoveSource(IMyHazardSource toRemove)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyHazardPointSource
FullName:  Sandbox.ModAPI.IMyHazardPointSource
Inherits:  IMyHazardSource

    Methods:
        Vector3D GetPosition()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyHazardReceiver
FullName:  Sandbox.ModAPI.IMyHazardReceiver

    Methods:
        Void Apply(Single amount, MyStringHash statId, MyStringHash damageType)
        Boolean CanBeAffected()
        IMyEntity GetEntity()
        Vector3D GetPosition()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyHazardSource
FullName:  Sandbox.ModAPI.IMyHazardSource

    Properties:
        MyStringHash DamageType { get;  }
        Boolean IgnoresSheltering { get;  }
        Boolean IsExposureScalingNeeded { get;  }
        MyStringHash StatId { get;  }
    Methods:
        Single GetCurrentExposure(IMyHazardReceiver receiver)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyIngameScripting
FullName:  Sandbox.ModAPI.IMyIngameScripting

    Properties:
        IMyScriptBlacklist ScriptBlacklist { get;  }
    Methods:
        Void Clean()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyInventoryBag
FullName:  Sandbox.ModAPI.IMyInventoryBag
Inherits:  IMyEntity, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyItemProducerComponent
FullName:  Sandbox.ModAPI.IMyItemProducerComponent
Inherits:  IMyItemProducerComponent

    Properties:
        Boolean UseConveyorSystem { get; set; }
    Methods:
        Boolean Produce(MyDefinitionId itemDefinitionId, MyFixedPoint quantity)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyJumpDrive
FullName:  Sandbox.ModAPI.IMyJumpDrive
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyJumpDrive

    Properties:
        Single CurrentStoredPower { get; set; }
    Methods:
        Void Jump(Boolean usePilot)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyLargeTurretBase
FullName:  Sandbox.ModAPI.IMyLargeTurretBase
Inherits:  IMyUserControllableGun, IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyUserControllableGun, IMyLargeTurretBase, IMyCameraController, IMyTargetingCapableBlock

    Properties:
        IMyEntity Target { get;  }
    Methods:
        Void SetTarget(IMyEntity entity)
        Void TrackTarget(IMyEntity entity)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyLaserAntenna
FullName:  Sandbox.ModAPI.IMyLaserAntenna
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyLaserAntenna

    Properties:
        IMyLaserAntenna Other { get;  }
    Methods:
        Boolean IsInRange(IMyLaserAntenna target)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyLcdSurfaceComponent
FullName:  Sandbox.ModAPI.IMyLcdSurfaceComponent

    Properties:
        Boolean InitializedWithBuilder { get;  }
        Int32 SelectedRotationIndex { get;  }
    Methods:
        Void OnRemovedFromScene(Object source)
        Void SetRenderForAllAreas()
        Void SetSelectedRotationIndex(Int32 newIndex)
        Void UpdateVisibility()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyLightingBlock
FullName:  Sandbox.ModAPI.IMyLightingBlock
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyLightingBlock


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyLightingComponent
FullName:  Sandbox.ModAPI.IMyLightingComponent
Inherits:  IMyLightingComponent


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyMechanicalConnectionBlock
FullName:  Sandbox.ModAPI.IMyMechanicalConnectionBlock
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyMechanicalConnectionBlock

    Properties:
        IMyAttachableTopBlock Top { get;  }
        IMyCubeGrid TopGrid { get;  }
    Methods:
        Void Attach(IMyAttachableTopBlock top, Boolean updateGroup)
    Events:
        event Action<IMyMechanicalConnectionBlock> OnAttachedChanged

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyMeteor
FullName:  Sandbox.ModAPI.IMyMeteor
Inherits:  IMyEntity, IMyEntity, IMyDestroyableObject, IMyDecalProxy


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyMissile
FullName:  Sandbox.ModAPI.IMyMissile
Inherits:  IMyEntity, IMyEntity, IMyDestroyableObject

    Properties:
        MyDefinitionBase AmmoDefinition { get;  }
        MyDefinitionBase AmmoMagazineDefinition { get;  }
        MyEntity CollidedEntity { get;  }
        Vector3 CollisionNormal { get;  }
        Nullable<Vector3D> CollisionPoint { get;  }
        Single ExplosionDamage { get; set; }
        MyExplosionTypeEnum ExplosionType { get; set; }
        Single HealthPool { get; set; }
        Int64 LauncherId { get; set; }
        Vector3 LinearVelocity { get; set; }
        Single MaxTrajectory { get; set; }
        Vector3D Origin { get; set; }
        Int64 Owner { get; set; }
        MyParticleEffect ParticleEffect { get; set; }
        Boolean ShouldExplode { get; set; }
        MyDefinitionBase WeaponDefinition { get;  }
    Methods:
        Void Destroy()
        Boolean IsCharacterIdFriendly(Int64 charId)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyMissiles
FullName:  Sandbox.ModAPI.IMyMissiles

    Methods:
        Void GetAllMissilesInSphere(ref BoundingSphereD sphere, List<MyEntity> result)
        Void Remove(Int64 entityId)
    Events:
        event Action<IMyMissile> OnMissileAdded
        event Action<IMyMissile> OnMissileCollided
        event MissileMoveDelegate OnMissileMoved
        event Action<IMyMissile> OnMissileRemoved

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyMotorAdvancedRotor
FullName:  Sandbox.ModAPI.IMyMotorAdvancedRotor
Inherits:  IMyMotorRotor, IMyAttachableTopBlock, IMyCubeBlock, IMyCubeBlock, IMyEntity, IMyEntity, IMyAttachableTopBlock, IMyMotorRotor, IMyMotorAdvancedRotor


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyMotorAdvancedStator
FullName:  Sandbox.ModAPI.IMyMotorAdvancedStator
Inherits:  IMyMotorStator, IMyMotorStator, IMyMotorBase, IMyMechanicalConnectionBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyMotorBase, IMyMechanicalConnectionBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyMotorAdvancedStator


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyMotorBase
FullName:  Sandbox.ModAPI.IMyMotorBase
Inherits:  IMyMechanicalConnectionBlock, IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyMechanicalConnectionBlock, IMyMotorBase

    Properties:
        Vector3 DummyPosition { get;  }
        Single MaxRotorAngularVelocity { get;  }
        IMyCubeBlock Rotor { get;  }
        Vector3 RotorAngularVelocity { get;  }
        IMyCubeGrid RotorGrid { get;  }
    Methods:
        Void Attach(IMyMotorRotor rotor, Boolean updateGroup)
    Events:
        event Action<IMyMotorBase> AttachedEntityChanged

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyMotorRotor
FullName:  Sandbox.ModAPI.IMyMotorRotor
Inherits:  IMyAttachableTopBlock, IMyCubeBlock, IMyCubeBlock, IMyEntity, IMyEntity, IMyAttachableTopBlock, IMyMotorRotor

    Properties:
        IMyMotorBase Base { get;  }
        IMyMotorBase Stator { get;  }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyMotorStator
FullName:  Sandbox.ModAPI.IMyMotorStator
Inherits:  IMyMotorStator, IMyMotorBase, IMyMechanicalConnectionBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyMotorBase, IMyMechanicalConnectionBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Events:
        event Action<IMyMotorStator, Single, Single> AngleChanged
        event Action<Boolean> LimitReached

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyMotorSuspension
FullName:  Sandbox.ModAPI.IMyMotorSuspension
Inherits:  IMyMotorSuspension, IMyMotorBase, IMyMechanicalConnectionBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyMotorBase, IMyMechanicalConnectionBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyNPCGridClaimSessionComponent
FullName:  Sandbox.ModAPI.IMyNPCGridClaimSessionComponent

    Methods:
        Nullable<Int32> GetFramesElapsed(Int64 gridEntityId)
        Nullable<TimeSpan> GetTimeRemaining(Int64 gridEntityId)
        Void RequestRegisterGrid(Int64 gridId)
        Void RequestSetFramesElapsed(Int64 gridEntityId, Nullable<Int32> elapsed)
        Void RequestUnregisterGrid(Int64 gridId)
    Events:
        event Action<Int64> ClaimTimerResumed
        event Action<Int64> ClaimTimerStarted
        event Action<IMyCubeGrid> GridUnregistered

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyOffensiveCombatBlock
FullName:  Sandbox.ModAPI.IMyOffensiveCombatBlock
Inherits:  IMyOffensiveCombatBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        IMySearchEnemyComponent SearchEnemyComponent { get;  }
    Methods:
        Void DoForceSearchEnemy()
        Void DoSearch()
        Void SetSearchLocked(Boolean locked, Object by)
        Boolean TryGetSelectedAttackPattern(Int64 patternId, out IMyAttackPatternComponent attackPatternComponent)
        Boolean TryGetSelectedAttackPattern(out IMyAttackPatternComponent attackPatternComponent)
    Events:
        event Action<IMyOffensiveCombatBlock, Int64> OnSelectedAttackPatternChanged
        event Action<IMyOffensiveCombatBlock, IMyEntity, IMyEntity, Boolean> OnTargetChanged

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyOffensiveCombatCircleOrbit
FullName:  Sandbox.ModAPI.IMyOffensiveCombatCircleOrbit
Inherits:  IMyOffensiveCombatCircleOrbit, IMyAttackPatternComponent, IMyAttackPatternComponent

    Properties:
        Func<IMyTerminalBlock, Boolean> BlockValidation { get; set; }
        Action<IMyCubeGrid, MyObjectBuilder_CubeGrid, IMyAttackPatternComponent> ClearBlocksShootingState { get; set; }
        Func<IMyAttackPatternComponent, Nullable<Vector3>> GetFirstWeaponForwardVector { get; set; }
        Func<IMyEntity, IMyAttackPatternComponent, Double, Vector3D> GetPredictedLookAt { get; set; }
        Action<IMyTerminalBlock, Boolean, Double, Nullable<Vector3D>, Direction> SetShooting { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyOffensiveCombatHitAndRun
FullName:  Sandbox.ModAPI.IMyOffensiveCombatHitAndRun
Inherits:  IMyOffensiveCombatHitAndRun, IMyAttackPatternComponent, IMyAttackPatternComponent

    Properties:
        Func<IMyTerminalBlock, Boolean> BlockValidation { get; set; }
        Action<IMyCubeGrid, MyObjectBuilder_CubeGrid, IMyAttackPatternComponent> ClearBlocksShootingState { get; set; }
        Func<IMyAttackPatternComponent, Nullable<Vector3>> GetFirstWeaponForwardVector { get; set; }
        Func<IMyEntity, IMyAttackPatternComponent, Double, Vector3D> GetPredictedLookAt { get; set; }
        Action<IMyTerminalBlock, Boolean, Double, Nullable<Vector3D>, Direction> SetShooting { get; set; }
    Events:
        event Action<IMyOffensiveCombatHitAndRun> StateChanged

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyOffensiveCombatIntercept
FullName:  Sandbox.ModAPI.IMyOffensiveCombatIntercept
Inherits:  IMyOffensiveCombatIntercept, IMyAttackPatternComponent, IMyAttackPatternComponent

    Events:
        event Action<IMyOffensiveCombatIntercept> OnGuidanceTypeValueChanged

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyOffensiveCombatStayAtRange
FullName:  Sandbox.ModAPI.IMyOffensiveCombatStayAtRange
Inherits:  IMyOffensiveCombatStayAtRange, IMyAttackPatternComponent, IMyAttackPatternComponent

    Properties:
        Func<IMyTerminalBlock, Boolean> BlockValidation { get; set; }
        Action<IMyCubeGrid, MyObjectBuilder_CubeGrid, IMyAttackPatternComponent> ClearBlocksShootingState { get; set; }
        Func<IMyAttackPatternComponent, Nullable<Vector3>> GetFirstWeaponForwardVector { get; set; }
        Func<IMyEntity, IMyAttackPatternComponent, Double, Vector3D> GetPredictedLookAt { get; set; }
        Action<IMyTerminalBlock, Boolean, Double, Nullable<Vector3D>, Direction> SetShooting { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyOreDetector
FullName:  Sandbox.ModAPI.IMyOreDetector
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyOreDetector


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyPassage
FullName:  Sandbox.ModAPI.IMyPassage
Inherits:  IMyCubeBlock, IMyCubeBlock, IMyEntity, IMyEntity, IMyPassage


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyPathfindingSessionComponent
FullName:  Sandbox.ModAPI.IMyPathfindingSessionComponent

    Methods:
        MyPath<MyPathVertex> FindAvoidPath(IMyEntity entity, Vector3D startPosition, Vector3D endPosition, Single shipRadius, out MyAutopilotPathfindingState state)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyPathRecorderBlock
FullName:  Sandbox.ModAPI.IMyPathRecorderBlock
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyPathRecorderBlock

    Methods:
        Boolean GetComponent(out IMyPathRecorderComponent component)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyPathRecorderComponent
FullName:  Sandbox.ModAPI.IMyPathRecorderComponent
Inherits:  IMyPathRecorderComponent

    Methods:
        Void AddWaypoint(IMyAutopilotWaypoint waypoint)
        Void DeleteAllWaypoints()
        Void GetWaypoints(List<IMyAutopilotWaypoint> points)
        Void RemoveWaypoint(IMyAutopilotWaypoint waypoint)
        Void ReverseOrder()
    Events:
        event Action<IMyPathRecorderComponent> IsPlayingChanged
        event Action<IMyPathRecorderComponent> IsRecordingChanged

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyPistonBase
FullName:  Sandbox.ModAPI.IMyPistonBase
Inherits:  IMyMechanicalConnectionBlock, IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyMechanicalConnectionBlock, IMyPistonBase

    Methods:
        Void Attach(IMyPistonTop top, Boolean updateGroup)
    Events:
        event Action<IMyPistonBase> AttachedEntityChanged
        event Action<Boolean> LimitReached
        event Action<IMyPistonBase, Single, Single> NormalizedPositionChanged

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyPistonTop
FullName:  Sandbox.ModAPI.IMyPistonTop
Inherits:  IMyAttachableTopBlock, IMyCubeBlock, IMyCubeBlock, IMyEntity, IMyEntity, IMyAttachableTopBlock, IMyPistonTop

    Properties:
        IMyPistonBase Base { get;  }
        IMyPistonBase Piston { get;  }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyPowerProducer
FullName:  Sandbox.ModAPI.IMyPowerProducer
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyPowerProducer

    Events:
        event Action<IMyPowerProducer, Single, Single> CurrentOutputRatioChanged

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyProductionBlock
FullName:  Sandbox.ModAPI.IMyProductionBlock
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyProductionBlock

    Properties:
        IMyInventory InputInventory { get;  }
        IMyInventory OutputInventory { get;  }
    Methods:
        Void AddQueueItem(MyDefinitionBase blueprint, MyFixedPoint amount)
        Boolean CanUseBlueprint(MyDefinitionBase blueprint)
        List<MyProductionQueueItem> GetQueue()
        Void InsertQueueItem(Int32 idx, MyDefinitionBase blueprint, MyFixedPoint amount)
    Events:
        event Action StartedProducing
        event Action StoppedProducing

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyProgrammableBlock
FullName:  Sandbox.ModAPI.IMyProgrammableBlock
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyProgrammableBlock, IMyTextSurfaceProvider

    Properties:
        Boolean HasCompileErrors { get;  }
        String ProgramData { get; set; }
        String StorageData { get; set; }
    Methods:
        Void Recompile()
        Void Run(String argument, UpdateType updateSource)
        Void Run(String argument)
        Void Run()
        Boolean TryRun(String argument)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyProjectileDetector
FullName:  Sandbox.ModAPI.IMyProjectileDetector

    Properties:
        BoundingBoxD DetectorAABB { get;  }
        IMyEntity HitEntity { get;  }
        Boolean IsDetectorEnabled { get;  }
    Methods:
        Boolean GetDetectorIntersectionWithLine(ref LineD line, out Nullable<Vector3D> hit)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyProjectiles
FullName:  Sandbox.ModAPI.IMyProjectiles

    Methods:
        Void Add(MyDefinitionBase weaponDefinition, MyDefinitionBase ammoDefinition, Vector3D origin, Vector3 initialVelocity, Vector3 directionNormalized, MyEntity owningEntity, MyEntity owningEntityAbsolute, MyEntity weapon, MyEntity[] ignoredEntities, Boolean supressHitIndicator, UInt64 owningPlayer)
        Void AddHitDetector(IMyProjectileDetector detector)
        Void AddOnHitInterceptor(Int32 priority, HitInterceptor interceptor)
        Int32 GetAllProjectileCount()
        MyProjectileInfo GetProjectile(Int32 index)
        Void GetSurfaceAndMaterial(IMyEntity entity, ref LineD line, ref Vector3D hitPosition, UInt32 shapeKey, out MySurfaceImpactEnum surfaceImpact, out MyStringHash materialType)
        Void MarkProjectileForDestroy(Int32 index)
        Void RemoveHitDetector(IMyProjectileDetector detector)
        Void RemoveOnHitInterceptor(HitInterceptor interceptor)
    Events:
        event OnProjectileAddedRemoved OnProjectileAdded
        event OnProjectileAddedRemoved OnProjectileRemoving

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyProjector
FullName:  Sandbox.ModAPI.IMyProjector
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyProjector, IMyTextSurfaceProvider, IMyTextSurfaceProvider

    Properties:
        IMyCubeGrid ProjectedGrid { get;  }
    Methods:
        Void Build(IMySlimBlock cubeBlock, Int64 owner, Int64 builder, Boolean requestInstant, Int64 builtBy)
        BuildCheckResult CanBuild(IMySlimBlock projectedBlock, Boolean checkHavokIntersections)
        Boolean LoadBlueprint(String path)
        Boolean LoadRandomBlueprint(String searchPattern)
        Void SetProjectedGrid(MyObjectBuilder_CubeGrid grid)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyRadioAntenna
FullName:  Sandbox.ModAPI.IMyRadioAntenna
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyRadioAntenna


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyRandomCargoEntityComponent
FullName:  Sandbox.ModAPI.IMyRandomCargoEntityComponent

    Properties:
        String ContainerType { get;  }
    Methods:
        Void SpawnRandomCargo()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyReactor
FullName:  Sandbox.ModAPI.IMyReactor
Inherits:  IMyReactor, IMyPowerProducer, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyPowerProducer, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Single PowerOutputMultiplier { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyRefinery
FullName:  Sandbox.ModAPI.IMyRefinery
Inherits:  IMyProductionBlock, IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyProductionBlock, IMyRefinery


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyReflectorLight
FullName:  Sandbox.ModAPI.IMyReflectorLight
Inherits:  IMyLightingBlock, IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyLightingBlock, IMyReflectorLight


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyRemoteControl
FullName:  Sandbox.ModAPI.IMyRemoteControl
Inherits:  IMyShipController, IMyTerminalBlock, IMyCubeBlock, IMyCubeBlock, IMyEntity, IMyEntity, IMyTerminalBlock, IMyShipController, IMyControllableEntity, IMyTargetingCapableBlock, IMyRemoteControl

    Methods:
        Vector3D GetFreeDestination(Vector3D originalDestination, Single checkRadius, Single shipRadius)
        Boolean GetNearestPlayer(out Vector3D playerPosition)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyRepairServiceComponent
FullName:  Sandbox.ModAPI.IMyRepairServiceComponent

    Methods:
        Void RequestGrids(Action<List<MyObjectBuilder_ServiceGridData>, Boolean> resultCallback)
        Void RequestRepair(MyObjectBuilder_ServiceQuoteData quote, Action<RepairResult> resultCallback)
        Void RequestRepairQuote(Int64 gridEntityId, Action<MyObjectBuilder_ServiceQuoteData> resultCallback)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyResourceStorageComponent
FullName:  Sandbox.ModAPI.IMyResourceStorageComponent
Inherits:  IMyResourceStorageComponent

    Properties:
        Action<IMyResourceStorageComponent, Double, Double> FilledRatioChanged { get; set; }
    Methods:
        Void ChangeFilledRatio(Double newFilledRatio, Boolean updateSync)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyRespawnComponent
FullName:  Sandbox.ModAPI.IMyRespawnComponent
Inherits:  IMyComponentBase

    Properties:
        Boolean SpawnWithoutOxygen { get;  }
    Methods:
        Boolean CanPlayerSpawn(Int64 playerId, Boolean acceptPublicRespawn)
        Single GetOxygenLevel()
        MatrixD GetSpawnPosition()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMySalvageServiceComponent
FullName:  Sandbox.ModAPI.IMySalvageServiceComponent

    Methods:
        Void ClearCallbacks()
        Void RequestAcceptOffer(Action<Boolean> resultCallback, MyObjectBuilder_SalvageServiceQuote quote)
        Void RequestGrids(Action<List<MyObjectBuilder_ServiceGridData>, Boolean> resultCallback)
        Void RequestQuote(Action<MyObjectBuilder_SalvageServiceQuote> resultCallback, Int64 gridEntityId)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyScriptBlacklist
FullName:  Sandbox.ModAPI.IMyScriptBlacklist

    Methods:
        HashSetReader<String> GetBlacklistedIngameEntries()
        DictionaryReader<String, MyWhitelistTarget> GetWhitelist()
        IMyScriptBlacklistBatch OpenIngameBlacklistBatch()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyScriptBlacklistBatch
FullName:  Sandbox.ModAPI.IMyScriptBlacklistBatch
Inherits:  IDisposable

    Methods:
        Void AddMembers(Type type, String[] memberNames)
        Void AddNamespaceOfTypes(Type[] types)
        Void AddTypes(Type[] types)
        Void RemoveMembers(Type type, String[] memberNames)
        Void RemoveNamespaceOfTypes(Type[] types)
        Void RemoveTypes(Type[] types)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMySearchEnemyComponent
FullName:  Sandbox.ModAPI.IMySearchEnemyComponent
Inherits:  IMySearchEnemyComponent

    Properties:
        IMyEntity FoundEnemy { get;  }
    Events:
        event Action ForceSearchRequired
        event Action<IMyEntity> SearchComplete
        event Action<IMyEntity, IMyEntity, Boolean> TargetChanged

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMySearchlight
FullName:  Sandbox.ModAPI.IMySearchlight
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMySearchlight

    Methods:
        Void SetAimingRadiusNoCheck(Single radius)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMySensorBlock
FullName:  Sandbox.ModAPI.IMySensorBlock
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMySensorBlock

    Properties:
        Vector3 FieldMax { get; set; }
        Vector3 FieldMin { get; set; }
    Events:
        event Action<Boolean> StateChanged

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyShipConnector
FullName:  Sandbox.ModAPI.IMyShipConnector
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyShipConnector

    Properties:
        IMyShipConnector OtherConnector { get;  }
    Events:
        event Action<IMyShipConnector> AttachFinished
        event Action<IMyShipConnector> DetachFinished
        event Action<IMyShipConnector> IsConnectedChanged

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyShipController
FullName:  Sandbox.ModAPI.IMyShipController
Inherits:  IMyTerminalBlock, IMyCubeBlock, IMyCubeBlock, IMyEntity, IMyEntity, IMyTerminalBlock, IMyShipController, IMyControllableEntity, IMyTargetingCapableBlock

    Properties:
        Boolean HasFirstPersonCamera { get;  }
        Boolean IsDefault3rdView { get;  }
        Boolean IsShooting { get;  }
        IMyCharacter LastPilot { get;  }
        IMyCharacter Pilot { get;  }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyShipDrill
FullName:  Sandbox.ModAPI.IMyShipDrill
Inherits:  IMyShipDrill, IMyShipToolBase, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyShipToolBase, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Single DrillHarvestMultiplier { get; set; }
        Single PowerConsumptionMultiplier { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyShipGrinder
FullName:  Sandbox.ModAPI.IMyShipGrinder
Inherits:  IMyShipToolBase, IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyShipToolBase, IMyShipGrinder


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyShipToolBase
FullName:  Sandbox.ModAPI.IMyShipToolBase
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyShipToolBase


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyShipWelder
FullName:  Sandbox.ModAPI.IMyShipWelder
Inherits:  IMyShipToolBase, IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyShipToolBase, IMyShipWelder

    Methods:
        Boolean IsWithinWorldLimits(IMyProjector projector, String name, Int32 pcuToBuild)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyShootOrigin
FullName:  Sandbox.ModAPI.IMyShootOrigin

    Properties:
        MyDefinitionBase GetAmmoDefinition { get;  }
        Single MaxShootRange { get;  }
        Vector3D ShootOrigin { get;  }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMySignalReceiverEntityComponent
FullName:  Sandbox.ModAPI.IMySignalReceiverEntityComponent

    Properties:
        MyTransponderRelationFilter AllowSignalsFrom { get; set; }
    Methods:
        Boolean Receive(Int64 signalSenderOwnerPlayerId, Int32 incomingChannelId)
    Events:
        event Action<Int32> SignalReceived
        event Action<Int32> TriggeredBySignal

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMySignalSenderEntityComponent
FullName:  Sandbox.ModAPI.IMySignalSenderEntityComponent

    Methods:
        Void SendSignal(Nullable<Int32> channelId)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMySmallGatlingGun
FullName:  Sandbox.ModAPI.IMySmallGatlingGun
Inherits:  IMyUserControllableGun, IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyUserControllableGun, IMySmallGatlingGun


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMySmallMissileLauncher
FullName:  Sandbox.ModAPI.IMySmallMissileLauncher
Inherits:  IMyUserControllableGun, IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyUserControllableGun, IMySmallMissileLauncher


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMySmallMissileLauncherReload
FullName:  Sandbox.ModAPI.IMySmallMissileLauncherReload
Inherits:  IMySmallMissileLauncher, IMyUserControllableGun, IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyUserControllableGun, IMySmallMissileLauncher, IMySmallMissileLauncherReload


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMySolarFoodGenerator
FullName:  Sandbox.ModAPI.IMySolarFoodGenerator
Inherits:  IMySolarFoodGenerator


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMySolarOccludable
FullName:  Sandbox.ModAPI.IMySolarOccludable
Inherits:  IMySolarOccludable


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyStoreBlock
FullName:  Sandbox.ModAPI.IMyStoreBlock
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyStoreBlock

    Methods:
        IMyStoreItem CreateStoreItem(Int32 amount, Int32 pricePerUnit, StoreItemTypes storeItemType, ItemTypes itemType)
        IMyStoreItem CreateStoreItem(MyObjectBuilder_StoreItem builder)
        IMyStoreItem CreateStoreItem(MyDefinitionId itemId, Int32 amount, Int32 pricePerUnit, StoreItemTypes storeItemType)
        IMyStoreItem CreateStoreItem(String prefabName, Int32 amount, Int32 pricePerUnit, Int32 totalPcu)
        IMyStoreItem GetStoreItemById(Int64 id)
        Void GetStoreItems(List<IMyStoreItem> items)
        MyStoreInsertResults InsertOffer(MyStoreItemData item, out Int64 id)
        MyStoreInsertResults InsertOrder(MyStoreItemData item, out Int64 id)
        Void InsertStoreItem(IMyStoreItem item)
        Void RemoveStoreItem(IMyStoreItem item)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyStoredPowerRatio
FullName:  Sandbox.ModAPI.IMyStoredPowerRatio

    Properties:
        Single StoredPowerRatio { get;  }
    Events:
        event Action<IMyStoredPowerRatio, Single, Single> StoredPowerRatioChanged

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMySurvivalBuff
FullName:  Sandbox.ModAPI.IMySurvivalBuff

    Properties:
        Int32 Level { get; set; }
        Int32 MaxLevel { get;  }
        MyStringId NameId { get;  }
        MyStringId NotificationText { get;  }
        MyStringId NotificationTitle { get;  }
        Int32 ProgressionTime { get; set; }
    Methods:
        Boolean DoProgressStep()
        Single GetCurrentBuffValue()
        String GetCurrentBuffValueString()
        Void Reset()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyTargetDummyBlock
FullName:  Sandbox.ModAPI.IMyTargetDummyBlock
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyTargetingCapableBlock
FullName:  Sandbox.ModAPI.IMyTargetingCapableBlock

    Methods:
        Boolean CanActiveToolShoot()
        Vector3D GetActiveToolPosition()
        MatrixD GetWorldMatrix()
        Boolean IsShipToolSelected()
        Boolean IsTargetLockingEnabled()
        Void SetLockedTarget(IMyCharacter target)
        Void SetLockedTarget(IMyCubeGrid target)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyTerminalActionsHelper
FullName:  Sandbox.ModAPI.IMyTerminalActionsHelper

    Methods:
        Void GetActions(Type blockType, List<ITerminalAction> resultList, Func<ITerminalAction, Boolean> collect)
        ITerminalAction GetActionWithName(String actionId, Type blockType)
        Void GetProperties(Type blockType, List<ITerminalProperty> resultList, Func<ITerminalProperty, Boolean> collect)
        ITerminalProperty GetProperty(String id, Type blockType)
        IMyGridTerminalSystem GetTerminalSystemForGrid(IMyCubeGrid grid)
        Void SearchActionsOfName(String name, Type blockType, List<ITerminalAction> resultList, Func<ITerminalAction, Boolean> collect)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyTerminalBlock
FullName:  Sandbox.ModAPI.IMyTerminalBlock
Inherits:  IMyCubeBlock, IMyCubeBlock, IMyEntity, IMyEntity, IMyTerminalBlock

    Properties:
        Boolean IsDetailedInfoDirty { get;  }
    Methods:
        Void ClearDetailedInfo()
        StringBuilder GetDetailedInfo()
        Boolean IsInSameLogicalGroupAs(IMyTerminalBlock other)
        Boolean IsSameConstructAs(IMyTerminalBlock other)
        Void RefreshCustomInfo()
        Void SetDetailedInfoDirty()
    Events:
        event Action<IMyTerminalBlock, StringBuilder> AppendingCustomInfo
        event Action<IMyTerminalBlock> CustomDataChanged
        event Action<IMyTerminalBlock> CustomNameChanged
        event Action<IMyTerminalBlock> OwnershipChanged
        event Action<IMyTerminalBlock> PropertiesChanged
        event Action<IMyTerminalBlock> ShowOnHUDChanged
        event Action<IMyTerminalBlock> VisibilityChanged

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyTerminalControls
FullName:  Sandbox.ModAPI.IMyTerminalControls

    Methods:
        Void AddAction(IMyTerminalAction action)
        Void AddControl(IMyTerminalControl item)
        IMyTerminalAction CreateAction(String id)
        TControl CreateControl(String id)
        IMyTerminalControlProperty<TValue> CreateProperty(String id)
        Void GetActions(out List<IMyTerminalAction> items)
        Void GetControls(out List<IMyTerminalControl> items)
        Void RemoveAction(IMyTerminalAction action)
        Void RemoveControl(IMyTerminalControl item)
    Events:
        event CustomActionGetDelegate CustomActionGetter
        event CustomControlGetDelegate CustomControlGetter

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyTextPanel
FullName:  Sandbox.ModAPI.IMyTextPanel
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTextSurface, IMyTextSurface, IMyTextPanel


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyTextSurface
FullName:  Sandbox.ModAPI.IMyTextSurface
Inherits:  IMyTextSurface


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyTextSurfaceProvider
FullName:  Sandbox.ModAPI.IMyTextSurfaceProvider
Inherits:  IMyTextSurfaceProvider


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyThrust
FullName:  Sandbox.ModAPI.IMyThrust
Inherits:  IMyThrust, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Single PowerConsumptionMultiplier { get; set; }
        Single ThrustMultiplier { get; set; }
    Events:
        event Action<IMyThrust, Single, Single> ThrustChanged
        event Action<IMyThrust, Single> ThrustOverrideChanged

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyUpgradableBlock
FullName:  Sandbox.ModAPI.IMyUpgradableBlock
Inherits:  IMyCubeBlock, IMyCubeBlock, IMyEntity, IMyEntity, IMyUpgradableBlock


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyUpgradeModule
FullName:  Sandbox.ModAPI.IMyUpgradeModule
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyUpgradeModule


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyUserControllableGun
FullName:  Sandbox.ModAPI.IMyUserControllableGun
Inherits:  IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyUserControllableGun


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyVendingMachine
FullName:  Sandbox.ModAPI.IMyVendingMachine
Inherits:  IMyStoreBlock, IMyFunctionalBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyStoreBlock

    Methods:
        Void Buy()
        Void SelectNextItem()
        Void SelectPreviewsItem()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyWarhead
FullName:  Sandbox.ModAPI.IMyWarhead
Inherits:  IMyTerminalBlock, IMyCubeBlock, IMyCubeBlock, IMyEntity, IMyEntity, IMyTerminalBlock, IMyWarhead, IMyDestroyableObject


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyWheel
FullName:  Sandbox.ModAPI.IMyWheel
Inherits:  IMyMotorRotor, IMyAttachableTopBlock, IMyCubeBlock, IMyCubeBlock, IMyEntity, IMyEntity, IMyAttachableTopBlock, IMyMotorRotor, IMyWheel


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyWindTurbine
FullName:  Sandbox.ModAPI.IMyWindTurbine
Inherits:  IMyWindTurbine, IMyPowerProducer, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyPowerProducer

    Properties:
        Single Effectivity { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
INTERFACE: IMyWorkAreaTool
FullName:  Sandbox.ModAPI.IMyWorkAreaTool

    Properties:
        Boolean IsBusy { get;  }
    Events:
        event Action<Boolean> BusyStatusChanged
        event Action InventoryActionTriggered

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyAdvancedDoor
FullName:  Sandbox.ModAPI.Ingame.IMyAdvancedDoor
Inherits:  IMyDoor, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyAirtightDoorBase
FullName:  Sandbox.ModAPI.Ingame.IMyAirtightDoorBase
Inherits:  IMyDoor, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyAirtightHangarDoor
FullName:  Sandbox.ModAPI.Ingame.IMyAirtightHangarDoor
Inherits:  IMyAirtightDoorBase, IMyDoor, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyAirtightSlideDoor
FullName:  Sandbox.ModAPI.Ingame.IMyAirtightSlideDoor
Inherits:  IMyAirtightDoorBase, IMyDoor, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyAssembler
FullName:  Sandbox.ModAPI.Ingame.IMyAssembler
Inherits:  IMyProductionBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Boolean CooperativeMode { get; set; }
        Single CurrentProgress { get;  }
        Boolean DisassembleEnabled { get;  }
        MyAssemblerMode Mode { get; set; }
        Boolean Repeating { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyAttachableTopBlock
FullName:  Sandbox.ModAPI.Ingame.IMyAttachableTopBlock
Inherits:  IMyCubeBlock, IMyEntity

    Properties:
        IMyMechanicalConnectionBlock Base { get;  }
        Boolean IsAttached { get;  }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyAttackPatternComponent
FullName:  Sandbox.ModAPI.Ingame.IMyAttackPatternComponent

    Properties:
        Int64 AttackPatternId { get;  }
        MyStringId AttackPatternName { get;  }
        MyStringId AttackPatternTooltip { get;  }
        Boolean HasEnemy { get;  }
        Boolean IsSelected { get;  }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyAutopilotWaypoint
FullName:  Sandbox.ModAPI.Ingame.IMyAutopilotWaypoint

    Properties:
        MatrixD Matrix { get;  }
        String Name { get;  }
        Int64 RelatedEntityId { get;  }
        MatrixD RelativeMatrix { get;  }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyBasicMissionBlock
FullName:  Sandbox.ModAPI.Ingame.IMyBasicMissionBlock
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Int64 SelectedMissionId { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyBasicMissionComponent
FullName:  Sandbox.ModAPI.Ingame.IMyBasicMissionComponent

    Properties:
        StringBuilder DetailedInfo { get;  }
        Boolean IsSelected { get; set; }
        MyStringId MissionName { get;  }
        Int64 UniqueSelectionId { get;  }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyBasicMissionFollowHome
FullName:  Sandbox.ModAPI.Ingame.IMyBasicMissionFollowHome

    Properties:
        Single MaxRange { get; set; }
        Single MinRange { get; set; }
        Boolean Wander { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyBasicMissionFollowPlayer
FullName:  Sandbox.ModAPI.Ingame.IMyBasicMissionFollowPlayer

    Properties:
        Single FollowDistance { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyBatteryBlock
FullName:  Sandbox.ModAPI.Ingame.IMyBatteryBlock
Inherits:  IMyPowerProducer, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        ChargeMode ChargeMode { get; set; }
        Single CurrentInput { get;  }
        Single CurrentStoredPower { get;  }
        Boolean HasCapacityRemaining { get;  }
        Boolean IsCharging { get;  }
        Single MaxInput { get;  }
        Single MaxStoredPower { get;  }
        Boolean OnlyDischarge { get; set; }
        Boolean OnlyRecharge { get; set; }
        Boolean SemiautoEnabled { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyBeacon
FullName:  Sandbox.ModAPI.Ingame.IMyBeacon
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        String HudText { get; set; }
        Single Radius { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyBlockGroup
FullName:  Sandbox.ModAPI.Ingame.IMyBlockGroup

    Properties:
        String Name { get;  }
    Methods:
        Void GetBlocks(List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, Boolean> collect)
        Void GetBlocksOfType(List<T> blocks, Func<T, Boolean> collect)
        Void GetBlocksOfType(List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, Boolean> collect)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyBroadcastControllerBlock
FullName:  Sandbox.ModAPI.Ingame.IMyBroadcastControllerBlock
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyBroadcastListener
FullName:  Sandbox.ModAPI.Ingame.IMyBroadcastListener
Inherits:  IMyMessageProvider

    Properties:
        Boolean IsActive { get;  }
        String Tag { get;  }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyCameraBlock
FullName:  Sandbox.ModAPI.Ingame.IMyCameraBlock
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Double AvailableScanRange { get;  }
        Boolean EnableRaycast { get; set; }
        Boolean IsActive { get;  }
        Single RaycastConeLimit { get;  }
        Double RaycastDistanceLimit { get;  }
        Single RaycastTimeMultiplier { get;  }
    Methods:
        Boolean CanScan(Double distance, Vector3D direction)
        Boolean CanScan(Vector3D target)
        Boolean CanScan(Double distance)
        MyDetectedEntityInfo Raycast(Double distance, Single pitch, Single yaw)
        MyDetectedEntityInfo Raycast(Vector3D targetPos)
        MyDetectedEntityInfo Raycast(Double distance, Vector3D targetDirection)
        Int32 TimeUntilScan(Double distance)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyCargoContainer
FullName:  Sandbox.ModAPI.Ingame.IMyCargoContainer
Inherits:  IMyTerminalBlock, IMyCubeBlock, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyCharacterAimAssistComponent
FullName:  Sandbox.ModAPI.Ingame.IMyCharacterAimAssistComponent


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyChatBroadcastControllerComponent
FullName:  Sandbox.ModAPI.Ingame.IMyChatBroadcastControllerComponent

    Properties:
        BroadcastTarget BroadcastTarget { get; set; }
        String CustomName { get; set; }
        Int32 MaxMessageCount { get;  }
        Boolean UseAntenna { get; set; }
    Methods:
        String GetMessage(Int32 messageIndex)
        Void SendGps()
        Void SendMessage(String message)
        Void SendMessage(Int32 messageIndex)
        Void SendRandomMessage()
        Void SetMessage(Int32 messageIndex, String message)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyCockpit
FullName:  Sandbox.ModAPI.Ingame.IMyCockpit
Inherits:  IMyShipController, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTextSurfaceProvider

    Properties:
        Single OxygenCapacity { get;  }
        Single OxygenFilledRatio { get;  }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyCollector
FullName:  Sandbox.ModAPI.Ingame.IMyCollector
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Boolean UseConveyorSystem { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyConveyor
FullName:  Sandbox.ModAPI.Ingame.IMyConveyor
Inherits:  IMyCubeBlock, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyConveyorSorter
FullName:  Sandbox.ModAPI.Ingame.IMyConveyorSorter
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Boolean DrainAll { get; set; }
        MyConveyorSorterMode Mode { get;  }
    Methods:
        Void AddItem(MyInventoryItemFilter item)
        Void GetFilterList(List<MyInventoryItemFilter> items)
        Boolean IsAllowed(MyDefinitionId id)
        Void RemoveItem(MyInventoryItemFilter item)
        Void SetFilter(MyConveyorSorterMode mode, List<MyInventoryItemFilter> items)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyConveyorTube
FullName:  Sandbox.ModAPI.Ingame.IMyConveyorTube
Inherits:  IMyCubeBlock, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyCryoChamber
FullName:  Sandbox.ModAPI.Ingame.IMyCryoChamber
Inherits:  IMyCockpit, IMyShipController, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTextSurfaceProvider


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyDecoy
FullName:  Sandbox.ModAPI.Ingame.IMyDecoy
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyDefensiveCombatBlock
FullName:  Sandbox.ModAPI.Ingame.IMyDefensiveCombatBlock
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Nullable<Vector3D> CustomFleeCoordinates { get; set; }
        Nullable<Int64> FleeBeaconEntityId { get;  }
        Nullable<Vector3D> FleeCoordinates { get;  }
        Nullable<GpsInfo> FleeGps { get;  }
        FleeTrigger FleeTrigger { get; set; }
        Boolean IsFleeing { get;  }
        Boolean LockTarget { get; set; }
        IMySearchEnemyComponent SearchEnemyComponent { get;  }
        Single WaypointZoneSize { get; set; }
    Methods:
        Void Flee()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyDoor
FullName:  Sandbox.ModAPI.Ingame.IMyDoor
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Boolean Open { get;  }
        Single OpenRatio { get;  }
        DoorStatus Status { get;  }
    Methods:
        Void CloseDoor()
        Void OpenDoor()
        Void ToggleDoor()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyEmotionControllerBlock
FullName:  Sandbox.ModAPI.Ingame.IMyEmotionControllerBlock
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyEventControllerBlock
FullName:  Sandbox.ModAPI.Ingame.IMyEventControllerBlock
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Boolean IsAndModeEnabled { get; set; }
        Boolean IsLowerOrEqualCondition { get; set; }
        Single Threshold { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyExtendedPistonBase
FullName:  Sandbox.ModAPI.Ingame.IMyExtendedPistonBase
Inherits:  IMyPistonBase, IMyMechanicalConnectionBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyFarmPlotLogic
FullName:  Sandbox.ModAPI.Ingame.IMyFarmPlotLogic

    Properties:
        Int32 AmountOfSeedsRequired { get;  }
        Boolean IsAlive { get;  }
        Boolean IsHarvestable { get;  }
        Boolean IsPlantFullyGrown { get;  }
        Boolean IsPlantPlanted { get;  }
        MyDefinitionId OutputItem { get;  }
        Int32 OutputItemAmount { get;  }
    Methods:
        String GetDetailedInfoWithoutRequiredInput()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyFlightMovementBlock
FullName:  Sandbox.ModAPI.Ingame.IMyFlightMovementBlock
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Boolean AlignToPGravity { get; set; }
        Boolean CollisionAvoidance { get; set; }
        IMyAutopilotWaypoint CurrentWaypoint { get;  }
        FlightMode FlightMode { get; set; }
        Boolean IsAutoPilotEnabled { get;  }
        Nullable<Vector3D> LookAtPosition { get; set; }
        Single MinimalAltitude { get; set; }
        Boolean PrecisionMode { get; set; }
        Single SpeedLimit { get; set; }
    Methods:
        Void GetWaypoints(List<IMyAutopilotWaypoint> points)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyFunctionalBlock
FullName:  Sandbox.ModAPI.Ingame.IMyFunctionalBlock
Inherits:  IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Boolean Enabled { get; set; }
    Methods:
        Void RequestEnable(Boolean enable)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyGasGenerator
FullName:  Sandbox.ModAPI.Ingame.IMyGasGenerator
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Boolean AutoRefill { get; set; }
        Boolean IsProducing { get;  }
        Boolean UseConveyorSystem { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyGasTank
FullName:  Sandbox.ModAPI.Ingame.IMyGasTank
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Boolean AutoRefillBottles { get; set; }
        Single Capacity { get;  }
        Double FilledRatio { get;  }
        Boolean Stockpile { get; set; }
    Methods:
        Void RefillBottles()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyGridProgramRuntimeInfo
FullName:  Sandbox.ModAPI.Ingame.IMyGridProgramRuntimeInfo

    Properties:
        Int32 CurrentCallChainDepth { get;  }
        Int32 CurrentInstructionCount { get;  }
        Double LastRunTimeMs { get;  }
        Int64 LifetimeTicks { get;  }
        Int32 MaxCallChainDepth { get;  }
        Int32 MaxInstructionCount { get;  }
        TimeSpan TimeSinceLastRun { get;  }
        UpdateFrequency UpdateFrequency { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyGridProgramWorldInfo
FullName:  Sandbox.ModAPI.Ingame.IMyGridProgramWorldInfo

    Properties:
        Single InventoryMultiplier { get;  }
        Single LargeShipMaxAngularSpeed { get;  }
        Single LargeShipMaxSpeed { get;  }
        Boolean PressurizationEnabled { get;  }
        Single SmallShipMaxAngularSpeed { get;  }
        Single SmallShipMaxSpeed { get;  }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyGridTerminalSystem
FullName:  Sandbox.ModAPI.Ingame.IMyGridTerminalSystem

    Methods:
        Boolean CanAccess(IMyTerminalBlock block, MyTerminalAccessScope scope)
        Boolean CanAccess(IMyCubeGrid grid, MyTerminalAccessScope scope)
        Void GetBlockGroups(List<IMyBlockGroup> blockGroups, Func<IMyBlockGroup, Boolean> collect)
        IMyBlockGroup GetBlockGroupWithName(String name)
        Void GetBlocks(List<IMyTerminalBlock> blocks)
        Void GetBlocksOfType(List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, Boolean> collect)
        Void GetBlocksOfType(List<T> blocks, Func<T, Boolean> collect)
        IMyTerminalBlock GetBlockWithId(Int64 id)
        IMyTerminalBlock GetBlockWithName(String name)
        Void SearchBlocksOfName(String name, List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, Boolean> collect)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyGyro
FullName:  Sandbox.ModAPI.Ingame.IMyGyro
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Boolean GyroOverride { get; set; }
        Single GyroPower { get; set; }
        Single Pitch { get; set; }
        Single Roll { get; set; }
        Single Yaw { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyIntergridCommunicationSystem
FullName:  Sandbox.ModAPI.Ingame.IMyIntergridCommunicationSystem

    Properties:
        Int64 Me { get;  }
        IMyUnicastListener UnicastListener { get;  }
    Methods:
        Void DisableBroadcastListener(IMyBroadcastListener broadcastListener)
        Void GetBroadcastListeners(List<IMyBroadcastListener> broadcastListeners, Func<IMyBroadcastListener, Boolean> collect)
        Boolean IsEndpointReachable(Int64 address, TransmissionDistance transmissionDistance)
        IMyBroadcastListener RegisterBroadcastListener(String tag)
        Void SendBroadcastMessage(String tag, TData data, TransmissionDistance transmissionDistance)
        Boolean SendUnicastMessage(Int64 addressee, String tag, TData data)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyItemProducerComponent
FullName:  Sandbox.ModAPI.Ingame.IMyItemProducerComponent


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyJumpDrive
FullName:  Sandbox.ModAPI.Ingame.IMyJumpDrive
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Single CurrentStoredPower { get;  }
        Single JumpDistanceMeters { get; set; }
        Single JumpDistanceRatio { get; set; }
        Single MaxJumpDistanceMeters { get;  }
        Single MaxStoredPower { get;  }
        Single MinJumpDistanceMeters { get;  }
        Boolean Recharge { get; set; }
        MyJumpDriveStatus Status { get;  }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyLargeTurretBase
FullName:  Sandbox.ModAPI.Ingame.IMyLargeTurretBase
Inherits:  IMyUserControllableGun, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Boolean AIEnabled { get;  }
        Single Azimuth { get; set; }
        Boolean CanControl { get;  }
        Single Elevation { get; set; }
        Boolean EnableIdleRotation { get; set; }
        Boolean HasTarget { get;  }
        Boolean IsAimed { get;  }
        Boolean IsUnderControl { get;  }
        Single Range { get; set; }
        Boolean TargetCharacters { get; set; }
        Boolean TargetEnemies { get; set; }
        Boolean TargetLargeGrids { get; set; }
        Boolean TargetMeteors { get; set; }
        Boolean TargetMissiles { get; set; }
        Boolean TargetNeutrals { get; set; }
        Boolean TargetSmallGrids { get; set; }
        Boolean TargetStations { get; set; }
    Methods:
        MyDetectedEntityInfo GetTargetedEntity()
        String GetTargetingGroup()
        Void GetTargetingGroups(List<String> targetingGroups)
        List<String> GetTargetingGroups()
        Void ResetTargetingToDefault()
        Void SetManualAzimuthAndElevation(Single azimuth, Single elevation)
        Void SetTarget(Vector3D pos)
        Void SetTargetingGroup(String groupSubtypeId)
        Void SyncAzimuth()
        Void SyncElevation()
        Void SyncEnableIdleRotation()
        Void TrackTarget(Vector3D pos, Vector3 velocity)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyLaserAntenna
FullName:  Sandbox.ModAPI.Ingame.IMyLaserAntenna
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Boolean IsOutsideLimits { get;  }
        Boolean IsPermanent { get; set; }
        Single Range { get; set; }
        Boolean RequireLoS { get;  }
        MyLaserAntennaStatus Status { get;  }
        Vector3D TargetCoords { get;  }
    Methods:
        Void Connect()
        Void SetTargetCoords(String coords)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyLightingBlock
FullName:  Sandbox.ModAPI.Ingame.IMyLightingBlock
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Single BlinkIntervalSeconds { get; set; }
        Single BlinkLenght { get;  }
        Single BlinkLength { get; set; }
        Single BlinkOffset { get; set; }
        Color Color { get; set; }
        Single Falloff { get; set; }
        Single Intensity { get; set; }
        Single Radius { get; set; }
        Single ReflectorRadius { get;  }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyLightingComponent
FullName:  Sandbox.ModAPI.Ingame.IMyLightingComponent

    Properties:
        Single BlinkIntervalSeconds { get; set; }
        Single BlinkLength { get; set; }
        Single BlinkOffset { get; set; }
        Color Color { get; set; }
        Single Falloff { get; set; }
        Single Intensity { get; set; }
        Single Radius { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyMechanicalConnectionBlock
FullName:  Sandbox.ModAPI.Ingame.IMyMechanicalConnectionBlock
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Boolean IsAttached { get;  }
        Boolean IsLocked { get;  }
        Boolean PendingAttachment { get;  }
        Boolean SafetyLock { get; set; }
        Single SafetyLockSpeed { get; set; }
        IMyAttachableTopBlock Top { get;  }
        IMyCubeGrid TopGrid { get;  }
    Methods:
        Void Attach()
        Void Detach()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyMessageProvider
FullName:  Sandbox.ModAPI.Ingame.IMyMessageProvider

    Properties:
        Boolean HasPendingMessage { get;  }
        Int32 MaxWaitingMessages { get;  }
    Methods:
        MyIGCMessage AcceptMessage()
        Void DisableMessageCallback()
        Void SetMessageCallback(String argument)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyMotorAdvancedRotor
FullName:  Sandbox.ModAPI.Ingame.IMyMotorAdvancedRotor
Inherits:  IMyMotorRotor, IMyAttachableTopBlock, IMyCubeBlock, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyMotorAdvancedStator
FullName:  Sandbox.ModAPI.Ingame.IMyMotorAdvancedStator
Inherits:  IMyMotorStator, IMyMotorBase, IMyMechanicalConnectionBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyMotorBase
FullName:  Sandbox.ModAPI.Ingame.IMyMotorBase
Inherits:  IMyMechanicalConnectionBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyMotorRotor
FullName:  Sandbox.ModAPI.Ingame.IMyMotorRotor
Inherits:  IMyAttachableTopBlock, IMyCubeBlock, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyMotorStator
FullName:  Sandbox.ModAPI.Ingame.IMyMotorStator
Inherits:  IMyMotorBase, IMyMechanicalConnectionBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Single Angle { get;  }
        Single BrakingTorque { get; set; }
        Single Displacement { get; set; }
        Single LowerLimitDeg { get; set; }
        Single LowerLimitRad { get; set; }
        Boolean RotorLock { get; set; }
        Single TargetVelocityRad { get; set; }
        Single TargetVelocityRPM { get; set; }
        Single Torque { get; set; }
        Single UpperLimitDeg { get; set; }
        Single UpperLimitRad { get; set; }
    Methods:
        Void RotateToAngle(MyRotationDirection dir, Single desiredAng, Single velAbsRpm)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyMotorSuspension
FullName:  Sandbox.ModAPI.Ingame.IMyMotorSuspension
Inherits:  IMyMotorBase, IMyMechanicalConnectionBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Boolean AirShockEnabled { get; set; }
        Boolean Brake { get; set; }
        Single Damping { get;  }
        Single Friction { get; set; }
        Single Height { get; set; }
        Boolean InvertPropulsion { get; set; }
        Boolean InvertSteer { get; set; }
        Boolean IsParkingEnabled { get; set; }
        Single MaxSteerAngle { get; set; }
        Single Power { get; set; }
        Boolean Propulsion { get; set; }
        Single PropulsionOverride { get; set; }
        Single SteerAngle { get;  }
        Boolean Steering { get; set; }
        Single SteeringOverride { get; set; }
        Single SteerReturnSpeed { get;  }
        Single SteerSpeed { get;  }
        Single Strength { get; set; }
        Single SuspensionTravel { get;  }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyOffensiveCombatBlock
FullName:  Sandbox.ModAPI.Ingame.IMyOffensiveCombatBlock
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Int32 LastScan { get;  }
        IMySearchEnemyComponent SearchEnemyComponent { get;  }
        Int64 SelectedAttackPattern { get; set; }
        OffensiveCombatTargetPriority TargetPriority { get; set; }
        Int32 UpdateTargetInterval { get; set; }
    Methods:
        T GetAttackPatternIds(T idsList)
        Boolean TryGetSelectedAttackPattern(out IMyAttackPatternComponent attackPatternComponent)
        Boolean TryGetSelectedAttackPattern(Int64 patternId, out IMyAttackPatternComponent attackPatternComponent)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyOffensiveCombatCircleOrbit
FullName:  Sandbox.ModAPI.Ingame.IMyOffensiveCombatCircleOrbit
Inherits:  IMyAttackPatternComponent

    Properties:
        Single CircleDistance { get; set; }
        Boolean CircleInPGravity { get; set; }
        DirectionFlags Facing { get; set; }
        Boolean IsOrbiting { get;  }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyOffensiveCombatHitAndRun
FullName:  Sandbox.ModAPI.Ingame.IMyOffensiveCombatHitAndRun
Inherits:  IMyAttackPatternComponent

    Properties:
        Single BreakOffDistance { get; set; }
        Boolean IsMovingAwayFromTarget { get;  }
        Boolean IsMovingToTarget { get;  }
        Single RetreatAngle { get; set; }
        Single RetreatDistance { get; set; }
        Single RetreatTimeout { get; set; }
    Methods:
        Void GetSelectedWeapons(List<Int64> blockEntityIds)
        Boolean IsWeaponSelected(Int64 blockEntityId)
        Void SetSelectedWeapons(List<Int64> blockEntityId)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyOffensiveCombatIntercept
FullName:  Sandbox.ModAPI.Ingame.IMyOffensiveCombatIntercept
Inherits:  IMyAttackPatternComponent

    Properties:
        GuidanceType GuidanceType { get; set; }
        Boolean ShouldOverrideCollisionAvoidance { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyOffensiveCombatStayAtRange
FullName:  Sandbox.ModAPI.Ingame.IMyOffensiveCombatStayAtRange
Inherits:  IMyAttackPatternComponent

    Properties:
        Single EvasiveManeuverDistance { get;  }
        Boolean EvasiveManeuvers { get; set; }
        DirectionFlags FacingPriority { get; set; }
        Single MaximalDistance { get; set; }
        Single MinimalDistance { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyOreDetector
FullName:  Sandbox.ModAPI.Ingame.IMyOreDetector
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Boolean BroadcastUsingAntennas { get; set; }
        Single Range { get;  }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyPassage
FullName:  Sandbox.ModAPI.Ingame.IMyPassage
Inherits:  IMyCubeBlock, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyPathRecorderBlock
FullName:  Sandbox.ModAPI.Ingame.IMyPathRecorderBlock
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyPathRecorderComponent
FullName:  Sandbox.ModAPI.Ingame.IMyPathRecorderComponent

    Properties:
        String BeaconDisplayText { get;  }
        Nullable<Int64> BeaconEntityId { get;  }
        Vector3D BeaconWorldPosition { get;  }
        Boolean IsPlaying { get;  }
        Boolean IsRecording { get;  }
        Single MinimalDistance { get; set; }
        Byte RecordInterval { get; set; }
        Boolean RepeatPath { get; set; }
        Boolean ShowPath { get; set; }
        Boolean ShowSelectedPoints { get; set; }
    Methods:
        Void Play()
        Void Record()
        Void RemoveBeacon()
        Void StopPlay()
        Void StopRecord()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyPistonBase
FullName:  Sandbox.ModAPI.Ingame.IMyPistonBase
Inherits:  IMyMechanicalConnectionBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Single CurrentPosition { get;  }
        Single HighestPosition { get;  }
        Single LowestPosition { get;  }
        Single MaxLimit { get; set; }
        Single MaxVelocity { get;  }
        Single MinLimit { get; set; }
        Single NormalizedPosition { get;  }
        PistonStatus Status { get;  }
        Single Velocity { get; set; }
    Methods:
        Void Extend()
        Void MoveToPosition(Single extent, Single speed)
        Void Retract()
        Void Reverse()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyPistonTop
FullName:  Sandbox.ModAPI.Ingame.IMyPistonTop
Inherits:  IMyAttachableTopBlock, IMyCubeBlock, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyPowerProducer
FullName:  Sandbox.ModAPI.Ingame.IMyPowerProducer
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Single CurrentOutput { get;  }
        Single CurrentOutputRatio { get;  }
        Single MaxOutput { get;  }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyProductionBlock
FullName:  Sandbox.ModAPI.Ingame.IMyProductionBlock
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        IMyInventory InputInventory { get;  }
        Boolean IsProducing { get;  }
        Boolean IsQueueEmpty { get;  }
        UInt32 NextItemId { get;  }
        IMyInventory OutputInventory { get;  }
        Boolean UseConveyorSystem { get; set; }
    Methods:
        Void AddQueueItem(MyDefinitionId blueprint, MyFixedPoint amount)
        Void AddQueueItem(MyDefinitionId blueprint, Double amount)
        Void AddQueueItem(MyDefinitionId blueprint, Decimal amount)
        Boolean CanUseBlueprint(MyDefinitionId blueprint)
        Void ClearQueue()
        Void GetQueue(List<MyProductionItem> items)
        Void InsertQueueItem(Int32 idx, MyDefinitionId blueprint, Double amount)
        Void InsertQueueItem(Int32 idx, MyDefinitionId blueprint, MyFixedPoint amount)
        Void InsertQueueItem(Int32 idx, MyDefinitionId blueprint, Decimal amount)
        Void MoveQueueItemRequest(UInt32 queueItemId, Int32 targetIdx)
        Void RemoveQueueItem(Int32 idx, Decimal amount)
        Void RemoveQueueItem(Int32 idx, MyFixedPoint amount)
        Void RemoveQueueItem(Int32 idx, Double amount)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyProgrammableBlock
FullName:  Sandbox.ModAPI.Ingame.IMyProgrammableBlock
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTextSurfaceProvider

    Properties:
        Boolean IsRunning { get;  }
        String TerminalRunArgument { get;  }
    Methods:
        Boolean TryRun(String argument)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyProjector
FullName:  Sandbox.ModAPI.Ingame.IMyProjector
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity, IMyTextSurfaceProvider

    Properties:
        Int32 BuildableBlocksCount { get;  }
        Boolean IsProjecting { get;  }
        Vector3I ProjectionOffset { get; set; }
        Int32 ProjectionOffsetX { get;  }
        Int32 ProjectionOffsetY { get;  }
        Int32 ProjectionOffsetZ { get;  }
        Vector3I ProjectionRotation { get; set; }
        Int32 ProjectionRotX { get;  }
        Int32 ProjectionRotY { get;  }
        Int32 ProjectionRotZ { get;  }
        Int32 RemainingArmorBlocks { get;  }
        Int32 RemainingBlocks { get;  }
        Dictionary<MyDefinitionBase, Int32> RemainingBlocksPerType { get;  }
        Boolean ShowOnlyBuildable { get; set; }
        Int32 TotalBlocks { get;  }
    Methods:
        Void UpdateOffsetAndRotation()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyRadioAntenna
FullName:  Sandbox.ModAPI.Ingame.IMyRadioAntenna
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Boolean EnableBroadcasting { get; set; }
        String HudText { get; set; }
        Boolean IsBroadcasting { get;  }
        Single Radius { get; set; }
        Boolean ShowShipName { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyReactor
FullName:  Sandbox.ModAPI.Ingame.IMyReactor
Inherits:  IMyPowerProducer, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Boolean UseConveyorSystem { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyRefinery
FullName:  Sandbox.ModAPI.Ingame.IMyRefinery
Inherits:  IMyProductionBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyReflectorLight
FullName:  Sandbox.ModAPI.Ingame.IMyReflectorLight
Inherits:  IMyLightingBlock, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyRemoteControl
FullName:  Sandbox.ModAPI.Ingame.IMyRemoteControl
Inherits:  IMyShipController, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        MyWaypointInfo CurrentWaypoint { get;  }
        Direction Direction { get; set; }
        FlightMode FlightMode { get; set; }
        Boolean IsAutoPilotEnabled { get;  }
        Single SpeedLimit { get; set; }
        Boolean WaitForFreeWay { get; set; }
    Methods:
        Void AddWaypoint(MyWaypointInfo coords)
        Void AddWaypoint(Vector3D coords, String name)
        Void ClearWaypoints()
        Boolean GetNearestPlayer(out Vector3D playerPosition)
        Void GetWaypointInfo(List<MyWaypointInfo> waypoints)
        Void SetAutoPilotEnabled(Boolean enabled)
        Void SetCollisionAvoidance(Boolean enabled)
        Void SetDockingMode(Boolean enabled)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyResourceStorageComponent
FullName:  Sandbox.ModAPI.Ingame.IMyResourceStorageComponent

    Properties:
        Double FilledRatio { get;  }
        Single ResourceCapacity { get;  }
    Methods:
        Boolean IsResourceStorage(MyDefinitionId resourceDefinition)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMySearchEnemyComponent
FullName:  Sandbox.ModAPI.Ingame.IMySearchEnemyComponent

    Properties:
        Nullable<Int64> FoundEnemyId { get;  }
        MyStringHash SubsystemsToDestroy { get; set; }
        MyGridTargetingRelationFiltering TargetingLockOptions { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMySearchlight
FullName:  Sandbox.ModAPI.Ingame.IMySearchlight
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Single AimingRadius { get; set; }
        Single BlinkInterval { get; set; }
        Single BlinkLength { get; set; }
        Single BlinkOffset { get; set; }
        Color Color { get; set; }
        Boolean EnableIdleMovement { get; set; }
        Single Intensity { get; set; }
        Single Offset { get; set; }
        Single Radius { get; set; }
        Boolean TargetCharacters { get; set; }
        Boolean TargetEnemy { get; set; }
        Boolean TargetFriends { get; set; }
        Boolean TargetLargeShips { get; set; }
        Boolean TargetLockEnemy { get; set; }
        Boolean TargetMeteors { get; set; }
        Boolean TargetNeutrals { get; set; }
        TargetingGroupOptions TargetOptions { get;  }
        Boolean TargetRockets { get; set; }
        Boolean TargetSmallShips { get; set; }
        Boolean TargetStations { get; set; }
    Methods:
        Void SetManualAzimuthAndElevation(Single azimuth, Single elevation)
        Void SetTargetOptions(TargetingGroupOptions Options)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMySensorBlock
FullName:  Sandbox.ModAPI.Ingame.IMySensorBlock
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Single BackExtend { get; set; }
        Single BottomExtend { get; set; }
        Boolean DetectAsteroids { get; set; }
        Boolean DetectEnemy { get; set; }
        Boolean DetectFloatingObjects { get; set; }
        Boolean DetectFriendly { get; set; }
        Boolean DetectLargeShips { get; set; }
        Boolean DetectNeutral { get; set; }
        Boolean DetectOwner { get; set; }
        Boolean DetectPlayers { get; set; }
        Boolean DetectSmallShips { get; set; }
        Boolean DetectStations { get; set; }
        Boolean DetectSubgrids { get; set; }
        Single FrontExtend { get; set; }
        Boolean IsActive { get;  }
        MyDetectedEntityInfo LastDetectedEntity { get;  }
        Single LeftExtend { get; set; }
        Single MaxRange { get;  }
        Boolean PlayProximitySound { get; set; }
        Single RightExtend { get; set; }
        Single TopExtend { get; set; }
    Methods:
        Void DetectedEntities(List<MyDetectedEntityInfo> entities)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyShipConnector
FullName:  Sandbox.ModAPI.Ingame.IMyShipConnector
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Boolean CollectAll { get; set; }
        Boolean IsConnected { get;  }
        Boolean IsLocked { get;  }
        Boolean IsParkingEnabled { get; set; }
        IMyShipConnector OtherConnector { get;  }
        Single PullStrength { get; set; }
        MyShipConnectorStatus Status { get;  }
        Boolean ThrowOut { get; set; }
    Methods:
        Void Connect()
        Void Disconnect()
        Void ToggleConnect()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyShipController
FullName:  Sandbox.ModAPI.Ingame.IMyShipController
Inherits:  IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Boolean CanControlShip { get;  }
        Vector3D CenterOfMass { get;  }
        Boolean ControlThrusters { get; set; }
        Boolean ControlWheels { get; set; }
        Boolean DampenersOverride { get; set; }
        Boolean HandBrake { get; set; }
        Boolean HasWheels { get;  }
        Boolean IsMainCockpit { get; set; }
        Boolean IsUnderControl { get;  }
        Vector3 MoveIndicator { get;  }
        Single RollIndicator { get;  }
        Vector2 RotationIndicator { get;  }
        Boolean ShowHorizonIndicator { get; set; }
    Methods:
        MyShipMass CalculateShipMass()
        Vector3D GetArtificialGravity()
        Vector3D GetNaturalGravity()
        Double GetShipSpeed()
        MyShipVelocities GetShipVelocities()
        Vector3D GetTotalGravity()
        Boolean TryGetPlanetElevation(MyPlanetElevation detail, out Double elevation)
        Boolean TryGetPlanetPosition(out Vector3D position)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyShipDrill
FullName:  Sandbox.ModAPI.Ingame.IMyShipDrill
Inherits:  IMyShipToolBase, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Boolean TerrainClearingMode { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyShipGrinder
FullName:  Sandbox.ModAPI.Ingame.IMyShipGrinder
Inherits:  IMyShipToolBase, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyShipToolBase
FullName:  Sandbox.ModAPI.Ingame.IMyShipToolBase
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Boolean IsActivated { get;  }
        Boolean UseConveyorSystem { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyShipWelder
FullName:  Sandbox.ModAPI.Ingame.IMyShipWelder
Inherits:  IMyShipToolBase, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Boolean HelpOthers { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMySmallGatlingGun
FullName:  Sandbox.ModAPI.Ingame.IMySmallGatlingGun
Inherits:  IMyUserControllableGun, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Boolean UseConveyorSystem { get;  }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMySmallMissileLauncher
FullName:  Sandbox.ModAPI.Ingame.IMySmallMissileLauncher
Inherits:  IMyUserControllableGun, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Boolean UseConveyorSystem { get;  }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMySmallMissileLauncherReload
FullName:  Sandbox.ModAPI.Ingame.IMySmallMissileLauncherReload
Inherits:  IMySmallMissileLauncher, IMyUserControllableGun, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMySolarFoodGenerator
FullName:  Sandbox.ModAPI.Ingame.IMySolarFoodGenerator

    Properties:
        Single ItemsPerMinute { get;  }
        Single TimeRemainingUntilNextBatch { get;  }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMySolarOccludable
FullName:  Sandbox.ModAPI.Ingame.IMySolarOccludable

    Properties:
        Boolean IsSolarOccluded { get;  }
    Methods:
        Int64 GetEntityId()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyStoreBlock
FullName:  Sandbox.ModAPI.Ingame.IMyStoreBlock
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Methods:
        Boolean CancelStoreItem(Int64 id)
        Void GetPlayerStoreItems(List<MyStoreQueryItem> storeItems)
        MyStoreInsertResults InsertOffer(MyStoreItemDataSimple item, out Int64 id)
        MyStoreInsertResults InsertOrder(MyStoreItemDataSimple item, out Int64 id)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyTargetDummyBlock
FullName:  Sandbox.ModAPI.Ingame.IMyTargetDummyBlock
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyTerminalBlock
FullName:  Sandbox.ModAPI.Ingame.IMyTerminalBlock
Inherits:  IMyCubeBlock, IMyEntity

    Properties:
        String CustomData { get; set; }
        String CustomInfo { get;  }
        String CustomName { get; set; }
        String CustomNameWithFaction { get;  }
        String DetailedInfo { get;  }
        Boolean ShowInInventory { get; set; }
        Boolean ShowInTerminal { get; set; }
        Boolean ShowInToolbarConfig { get; set; }
        Boolean ShowOnHUD { get; set; }
    Methods:
        Void GetActions(List<ITerminalAction> resultList, Func<ITerminalAction, Boolean> collect)
        ITerminalAction GetActionWithName(String name)
        Void GetProperties(List<ITerminalProperty> resultList, Func<ITerminalProperty, Boolean> collect)
        ITerminalProperty GetProperty(String id)
        Boolean HasLocalPlayerAccess()
        Boolean HasNobodyPlayerAccessToBlock()
        Boolean HasPlayerAccess(Int64 playerId, MyRelationsBetweenPlayerAndBlock defaultNoUser)
        Boolean HasPlayerAccessWithNobodyCheck(Int64 playerId, Boolean isForPB)
        Boolean IsSameConstructAs(IMyTerminalBlock other)
        Void SearchActionsOfName(String name, List<ITerminalAction> resultList, Func<ITerminalAction, Boolean> collect)
        Void SetCustomName(StringBuilder text)
        Void SetCustomName(String text)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyTextPanel
FullName:  Sandbox.ModAPI.Ingame.IMyTextPanel
Inherits:  IMyTextSurface, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Methods:
        String GetPublicTitle()
        Boolean WritePublicTitle(String value, Boolean append)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyTextSurface
FullName:  Sandbox.ModAPI.Ingame.IMyTextSurface

    Properties:
        TextAlignment Alignment { get; set; }
        Byte BackgroundAlpha { get; set; }
        Color BackgroundColor { get; set; }
        Single ChangeInterval { get; set; }
        ContentType ContentType { get; set; }
        String CurrentlyShownImage { get;  }
        String DisplayName { get;  }
        String Font { get; set; }
        Color FontColor { get; set; }
        Single FontSize { get; set; }
        String Name { get;  }
        Boolean PreserveAspectRatio { get; set; }
        String Script { get; set; }
        Color ScriptBackgroundColor { get; set; }
        Color ScriptForegroundColor { get; set; }
        Vector2 SurfaceSize { get;  }
        Single TextPadding { get; set; }
        Vector2 TextureSize { get;  }
    Methods:
        Void AddImagesToSelection(List<String> ids, Boolean checkExistence)
        Void AddImageToSelection(String id, Boolean checkExistence)
        Void ClearImagesFromSelection()
        MySpriteDrawFrame DrawFrame()
        Void GetFonts(List<String> fonts)
        Void GetScripts(List<String> scripts)
        Void GetSelectedImages(List<String> output)
        Void GetSprites(List<String> sprites)
        String GetText()
        Vector2 MeasureStringInPixels(StringBuilder text, String font, Single scale)
        Void ReadText(StringBuilder buffer, Boolean append)
        Void RemoveImageFromSelection(String id, Boolean removeDuplicates)
        Void RemoveImagesFromSelection(List<String> ids, Boolean removeDuplicates)
        Boolean WriteText(String value, Boolean append)
        Boolean WriteText(StringBuilder value, Boolean append)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyTextSurfaceProvider
FullName:  Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider

    Properties:
        Int32 SurfaceCount { get;  }
        Boolean UseGenericLcd { get;  }
    Methods:
        IMyTextSurface GetSurface(Int32 index)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyThrust
FullName:  Sandbox.ModAPI.Ingame.IMyThrust
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Single CurrentThrust { get;  }
        Single CurrentThrustPercentage { get;  }
        Vector3I GridThrustDirection { get;  }
        Single MaxEffectiveThrust { get;  }
        Single MaxThrust { get;  }
        Single ThrustOverride { get; set; }
        Single ThrustOverridePercentage { get; set; }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyUnicastListener
FullName:  Sandbox.ModAPI.Ingame.IMyUnicastListener
Inherits:  IMyMessageProvider


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyUpgradableBlock
FullName:  Sandbox.ModAPI.Ingame.IMyUpgradableBlock
Inherits:  IMyCubeBlock, IMyEntity

    Properties:
        UInt32 UpgradeCount { get;  }
    Methods:
        Void FillUpgradesDictionary(Dictionary<String, Single> upgrades)
        Void GetUpgrades(out Dictionary<String, Single> upgrades)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyUpgradeModule
FullName:  Sandbox.ModAPI.Ingame.IMyUpgradeModule
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        UInt32 Connections { get;  }
        UInt32 UpgradeCount { get;  }
    Methods:
        Void FillUpgradeList(List<MyUpgradeModuleInfo> upgrades)
        Void GetUpgradeList(out List<MyUpgradeModuleInfo> upgrades)

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyUserControllableGun
FullName:  Sandbox.ModAPI.Ingame.IMyUserControllableGun
Inherits:  IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Boolean IsShooting { get;  }
        Boolean Shoot { get; set; }
    Methods:
        Void ShootOnce()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyWarhead
FullName:  Sandbox.ModAPI.Ingame.IMyWarhead
Inherits:  IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Single DetonationTime { get; set; }
        Boolean IsArmed { get; set; }
        Boolean IsCountingDown { get;  }
    Methods:
        Void Detonate()
        Boolean StartCountdown()
        Boolean StopCountdown()

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyWheel
FullName:  Sandbox.ModAPI.Ingame.IMyWheel
Inherits:  IMyMotorRotor, IMyAttachableTopBlock, IMyCubeBlock, IMyEntity


----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IMyWindTurbine
FullName:  Sandbox.ModAPI.Ingame.IMyWindTurbine
Inherits:  IMyPowerProducer, IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

    Properties:
        Single Effectivity { get;  }
        Single PlacementEffectivity { get;  }
        Single WindEffectivity { get;  }

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
INTERFACE: IUserCustomizableTerminalAction
FullName:  Sandbox.ModAPI.Ingame.IUserCustomizableTerminalAction

    Properties:
        IList<TerminalActionParameter> Parameters { get;  }
    Methods:
        Void FetchAllBlocks(List<IMyTerminalBlock> result)
        IMyTerminalBlock GetBlock()
        Void SetCustomIconTitle(String title)

========================================================================================================================
INTERFACE HIERARCHY
========================================================================================================================

+-- IMyAccessAnyoneCanUse

+-- IMyAiBlockComponent

+-- IMyAiRvoSessionComponent

+-- IMyAttachableTopBlock
|   +-- IMyAttachableTopBlock
|   |   +-- IMyMotorAdvancedRotor
|   |   +-- IMyMotorRotor
|   |   |   +-- IMyMotorAdvancedRotor
|   |   |   +-- IMyWheel
|   |   +-- IMyPistonTop
|   |   +-- IMyWheel
|   +-- IMyMotorAdvancedRotor
|   |   +-- IMyMotorAdvancedRotor
|   +-- IMyMotorAdvancedRotor
|   +-- IMyMotorRotor
|   |   +-- IMyMotorAdvancedRotor
|   |   |   +-- IMyMotorAdvancedRotor
|   |   +-- IMyMotorAdvancedRotor
|   |   +-- IMyMotorRotor
|   |   |   +-- IMyMotorAdvancedRotor
|   |   |   +-- IMyWheel
|   |   +-- IMyWheel
|   |   |   +-- IMyWheel
|   |   +-- IMyWheel
|   +-- IMyMotorRotor
|   |   +-- IMyMotorAdvancedRotor
|   |   +-- IMyWheel
|   +-- IMyPistonTop
|   |   +-- IMyPistonTop
|   +-- IMyPistonTop
|   +-- IMyWheel
|   |   +-- IMyWheel
|   +-- IMyWheel

+-- IMyAttackPatternComponent
|   +-- IMyAttackPatternComponent
|   |   +-- IMyOffensiveCombatCircleOrbit
|   |   +-- IMyOffensiveCombatHitAndRun
|   |   +-- IMyOffensiveCombatIntercept
|   |   +-- IMyOffensiveCombatStayAtRange
|   +-- IMyOffensiveCombatCircleOrbit
|   |   +-- IMyOffensiveCombatCircleOrbit
|   +-- IMyOffensiveCombatCircleOrbit
|   +-- IMyOffensiveCombatHitAndRun
|   |   +-- IMyOffensiveCombatHitAndRun
|   +-- IMyOffensiveCombatHitAndRun
|   +-- IMyOffensiveCombatIntercept
|   |   +-- IMyOffensiveCombatIntercept
|   +-- IMyOffensiveCombatIntercept
|   +-- IMyOffensiveCombatStayAtRange
|   |   +-- IMyOffensiveCombatStayAtRange
|   +-- IMyOffensiveCombatStayAtRange

+-- IMyAutopilotWaypoint

+-- IMyBasicMissionComponent
|   +-- IMyBasicMissionAutopilot
|   +-- IMyBasicMissionComponent
|   |   +-- IMyBasicMissionAutopilot
|   |   +-- IMyBasicMissionFollowHome
|   |   +-- IMyBasicMissionFollowPlayer
|   +-- IMyBasicMissionFollowHome
|   +-- IMyBasicMissionFollowPlayer

+-- IMyBasicMissionFollowHome
|   +-- IMyBasicMissionFollowHome

+-- IMyBasicMissionFollowPlayer
|   +-- IMyBasicMissionFollowPlayer

+-- IMyBlockGroup
|   +-- IMyBlockGroup

+-- IMyCharacterAimAssistComponent
|   +-- IMyCharacterAimAssistComponent

+-- IMyChatBroadcastControllerComponent

+-- IMyConveyor
|   +-- IMyConveyor

+-- IMyConveyorTube
|   +-- IMyConveyorTube

+-- IMyDistanceToLockedTarget

+-- IMyEventControllerEntityComponent
|   +-- IMyEventComponentWithGui

+-- IMyFarmPlotLogic
|   +-- IMyFarmPlotLogic

+-- IMyGlobalEncounterComponent

+-- IMyGridProgram

+-- IMyGridProgramRuntimeInfo

+-- IMyGridProgramWorldInfo

+-- IMyGridTerminalSystem
|   +-- IMyGridTerminalSystem

+-- IMyHazardExposureComponent

+-- IMyHazardReceiver

+-- IMyHazardSource
|   +-- IMyHazardPointSource

+-- IMyIngameScripting

+-- IMyIntergridCommunicationSystem

+-- IMyInventoryBag

+-- IMyItemProducerComponent
|   +-- IMyItemProducerComponent

+-- IMyLcdSurfaceComponent

+-- IMyLightingComponent
|   +-- IMyLightingComponent

+-- IMyMessageProvider
|   +-- IMyBroadcastListener
|   +-- IMyUnicastListener

+-- IMyMeteor

+-- IMyMissile

+-- IMyMissiles

+-- IMyNPCGridClaimSessionComponent

+-- IMyPassage
|   +-- IMyPassage

+-- IMyPathfindingSessionComponent

+-- IMyPathRecorderComponent
|   +-- IMyPathRecorderComponent

+-- IMyProjectileDetector

+-- IMyProjectiles

+-- IMyRandomCargoEntityComponent

+-- IMyRepairServiceComponent

+-- IMyResourceStorageComponent
|   +-- IMyResourceStorageComponent

+-- IMyRespawnComponent

+-- IMySalvageServiceComponent

+-- IMyScriptBlacklist

+-- IMyScriptBlacklistBatch

+-- IMySearchEnemyComponent
|   +-- IMySearchEnemyComponent

+-- IMyShootOrigin

+-- IMySignalReceiverEntityComponent

+-- IMySignalSenderEntityComponent

+-- IMySolarFoodGenerator
|   +-- IMySolarFoodGenerator

+-- IMySolarOccludable
|   +-- IMySolarOccludable

+-- IMyStoredPowerRatio

+-- IMySurvivalBuff

+-- IMyTargetingCapableBlock
|   +-- IMyCockpit
|   |   +-- IMyCryoChamber
|   +-- IMyCryoChamber
|   +-- IMyLargeTurretBase
|   +-- IMyRemoteControl
|   +-- IMyShipController
|   |   +-- IMyCockpit
|   |   |   +-- IMyCryoChamber
|   |   +-- IMyCryoChamber
|   |   +-- IMyRemoteControl

+-- IMyTerminalActionsHelper

+-- IMyTerminalBlock
|   +-- IMyAdvancedDoor
|   |   +-- IMyAdvancedDoor
|   +-- IMyAdvancedDoor
|   +-- IMyAirtightDoorBase
|   |   +-- IMyAirtightHangarDoor
|   |   +-- IMyAirtightSlideDoor
|   +-- IMyAirtightDoorBase
|   |   +-- IMyAirtightDoorBase
|   |   |   +-- IMyAirtightHangarDoor
|   |   |   +-- IMyAirtightSlideDoor
|   |   +-- IMyAirtightHangarDoor
|   |   |   +-- IMyAirtightHangarDoor
|   |   +-- IMyAirtightHangarDoor
|   |   +-- IMyAirtightSlideDoor
|   |   |   +-- IMyAirtightSlideDoor
|   |   +-- IMyAirtightSlideDoor
|   +-- IMyAirtightHangarDoor
|   +-- IMyAirtightHangarDoor
|   |   +-- IMyAirtightHangarDoor
|   +-- IMyAirtightSlideDoor
|   +-- IMyAirtightSlideDoor
|   |   +-- IMyAirtightSlideDoor
|   +-- IMyAssembler
|   |   +-- IMyAssembler
|   +-- IMyAssembler
|   +-- IMyBasicMissionBlock
|   +-- IMyBasicMissionBlock
|   |   +-- IMyBasicMissionBlock
|   +-- IMyBatteryBlock
|   +-- IMyBatteryBlock
|   |   +-- IMyBatteryBlock
|   +-- IMyBeacon
|   |   +-- IMyBeacon
|   +-- IMyBeacon
|   +-- IMyBroadcastControllerBlock
|   +-- IMyCameraBlock
|   +-- IMyCameraBlock
|   |   +-- IMyCameraBlock
|   +-- IMyCargoContainer
|   +-- IMyCargoContainer
|   |   +-- IMyCargoContainer
|   +-- IMyCockpit
|   |   +-- IMyCockpit
|   |   |   +-- IMyCryoChamber
|   |   +-- IMyCryoChamber
|   |   |   +-- IMyCryoChamber
|   |   +-- IMyCryoChamber
|   +-- IMyCockpit
|   |   +-- IMyCryoChamber
|   +-- IMyCollector
|   +-- IMyCollector
|   |   +-- IMyCollector
|   +-- IMyConveyorSorter
|   +-- IMyConveyorSorter
|   |   +-- IMyConveyorSorter
|   +-- IMyCryoChamber
|   +-- IMyCryoChamber
|   |   +-- IMyCryoChamber
|   +-- IMyDecoy
|   |   +-- IMyDecoy
|   +-- IMyDecoy
|   +-- IMyDefensiveCombatBlock
|   |   +-- IMyDefensiveCombatBlock
|   +-- IMyDefensiveCombatBlock
|   +-- IMyDoor
|   |   +-- IMyAdvancedDoor
|   |   +-- IMyAirtightDoorBase
|   |   |   +-- IMyAirtightHangarDoor
|   |   |   +-- IMyAirtightSlideDoor
|   |   +-- IMyAirtightHangarDoor
|   |   +-- IMyAirtightSlideDoor
|   +-- IMyDoor
|   |   +-- IMyAdvancedDoor
|   |   |   +-- IMyAdvancedDoor
|   |   +-- IMyAdvancedDoor
|   |   +-- IMyAirtightDoorBase
|   |   |   +-- IMyAirtightDoorBase
|   |   |   |   +-- IMyAirtightHangarDoor
|   |   |   |   +-- IMyAirtightSlideDoor
|   |   |   +-- IMyAirtightHangarDoor
|   |   |   |   +-- IMyAirtightHangarDoor
|   |   |   +-- IMyAirtightHangarDoor
|   |   |   +-- IMyAirtightSlideDoor
|   |   |   |   +-- IMyAirtightSlideDoor
|   |   |   +-- IMyAirtightSlideDoor
|   |   +-- IMyAirtightDoorBase
|   |   |   +-- IMyAirtightHangarDoor
|   |   |   +-- IMyAirtightSlideDoor
|   |   +-- IMyAirtightHangarDoor
|   |   +-- IMyAirtightHangarDoor
|   |   |   +-- IMyAirtightHangarDoor
|   |   +-- IMyAirtightSlideDoor
|   |   +-- IMyAirtightSlideDoor
|   |   |   +-- IMyAirtightSlideDoor
|   |   +-- IMyDoor
|   |   |   +-- IMyAdvancedDoor
|   |   |   +-- IMyAirtightDoorBase
|   |   |   |   +-- IMyAirtightHangarDoor
|   |   |   |   +-- IMyAirtightSlideDoor
|   |   |   +-- IMyAirtightHangarDoor
|   |   |   +-- IMyAirtightSlideDoor
|   +-- IMyEmotionControllerBlock
|   |   +-- IMyEmotionControllerBlock
|   +-- IMyEmotionControllerBlock
|   +-- IMyEventControllerBlock
|   +-- IMyEventControllerBlock
|   |   +-- IMyEventControllerBlock
|   +-- IMyExhaustBlock
|   +-- IMyExtendedPistonBase
|   |   +-- IMyExtendedPistonBase
|   +-- IMyExtendedPistonBase
|   +-- IMyFlightMovementBlock
|   |   +-- IMyFlightMovementBlock
|   +-- IMyFlightMovementBlock
|   +-- IMyFunctionalBlock
|   |   +-- IMyAdvancedDoor
|   |   |   +-- IMyAdvancedDoor
|   |   +-- IMyAdvancedDoor
|   |   +-- IMyAirtightDoorBase
|   |   |   +-- IMyAirtightHangarDoor
|   |   |   +-- IMyAirtightSlideDoor
|   |   +-- IMyAirtightDoorBase
|   |   |   +-- IMyAirtightDoorBase
|   |   |   |   +-- IMyAirtightHangarDoor
|   |   |   |   +-- IMyAirtightSlideDoor
|   |   |   +-- IMyAirtightHangarDoor
|   |   |   |   +-- IMyAirtightHangarDoor
|   |   |   +-- IMyAirtightHangarDoor
|   |   |   +-- IMyAirtightSlideDoor
|   |   |   |   +-- IMyAirtightSlideDoor
|   |   |   +-- IMyAirtightSlideDoor
|   |   +-- IMyAirtightHangarDoor
|   |   +-- IMyAirtightHangarDoor
|   |   |   +-- IMyAirtightHangarDoor
|   |   +-- IMyAirtightSlideDoor
|   |   +-- IMyAirtightSlideDoor
|   |   |   +-- IMyAirtightSlideDoor
|   |   +-- IMyAssembler
|   |   |   +-- IMyAssembler
|   |   +-- IMyAssembler
|   |   +-- IMyBasicMissionBlock
|   |   +-- IMyBasicMissionBlock
|   |   |   +-- IMyBasicMissionBlock
|   |   +-- IMyBatteryBlock
|   |   |   +-- IMyBatteryBlock
|   |   +-- IMyBatteryBlock
|   |   +-- IMyBeacon
|   |   |   +-- IMyBeacon
|   |   +-- IMyBeacon
|   |   +-- IMyBroadcastControllerBlock
|   |   +-- IMyCameraBlock
|   |   +-- IMyCameraBlock
|   |   |   +-- IMyCameraBlock
|   |   +-- IMyCollector
|   |   |   +-- IMyCollector
|   |   +-- IMyCollector
|   |   +-- IMyConveyorSorter
|   |   |   +-- IMyConveyorSorter
|   |   +-- IMyConveyorSorter
|   |   +-- IMyDecoy
|   |   |   +-- IMyDecoy
|   |   +-- IMyDecoy
|   |   +-- IMyDefensiveCombatBlock
|   |   +-- IMyDefensiveCombatBlock
|   |   |   +-- IMyDefensiveCombatBlock
|   |   +-- IMyDoor
|   |   |   +-- IMyAdvancedDoor
|   |   |   |   +-- IMyAdvancedDoor
|   |   |   +-- IMyAdvancedDoor
|   |   |   +-- IMyAirtightDoorBase
|   |   |   |   +-- IMyAirtightDoorBase
|   |   |   |   |   +-- IMyAirtightHangarDoor
|   |   |   |   |   +-- IMyAirtightSlideDoor
|   |   |   |   +-- IMyAirtightHangarDoor
|   |   |   |   |   +-- IMyAirtightHangarDoor
|   |   |   |   +-- IMyAirtightHangarDoor
|   |   |   |   +-- IMyAirtightSlideDoor
|   |   |   |   |   +-- IMyAirtightSlideDoor
|   |   |   |   +-- IMyAirtightSlideDoor
|   |   |   +-- IMyAirtightDoorBase
|   |   |   |   +-- IMyAirtightHangarDoor
|   |   |   |   +-- IMyAirtightSlideDoor
|   |   |   +-- IMyAirtightHangarDoor
|   |   |   +-- IMyAirtightHangarDoor
|   |   |   |   +-- IMyAirtightHangarDoor
|   |   |   +-- IMyAirtightSlideDoor
|   |   |   +-- IMyAirtightSlideDoor
|   |   |   |   +-- IMyAirtightSlideDoor
|   |   |   +-- IMyDoor
|   |   |   |   +-- IMyAdvancedDoor
|   |   |   |   +-- IMyAirtightDoorBase
|   |   |   |   |   +-- IMyAirtightHangarDoor
|   |   |   |   |   +-- IMyAirtightSlideDoor
|   |   |   |   +-- IMyAirtightHangarDoor
|   |   |   |   +-- IMyAirtightSlideDoor
|   |   +-- IMyDoor
|   |   |   +-- IMyAdvancedDoor
|   |   |   +-- IMyAirtightDoorBase
|   |   |   |   +-- IMyAirtightHangarDoor
|   |   |   |   +-- IMyAirtightSlideDoor
|   |   |   +-- IMyAirtightHangarDoor
|   |   |   +-- IMyAirtightSlideDoor
|   |   +-- IMyEmotionControllerBlock
|   |   +-- IMyEmotionControllerBlock
|   |   |   +-- IMyEmotionControllerBlock
|   |   +-- IMyEventControllerBlock
|   |   |   +-- IMyEventControllerBlock
|   |   +-- IMyEventControllerBlock
|   |   +-- IMyExhaustBlock
|   |   +-- IMyExtendedPistonBase
|   |   |   +-- IMyExtendedPistonBase
|   |   +-- IMyExtendedPistonBase
|   |   +-- IMyFlightMovementBlock
|   |   |   +-- IMyFlightMovementBlock
|   |   +-- IMyFlightMovementBlock
|   |   +-- IMyFunctionalBlock
|   |   |   +-- IMyAdvancedDoor
|   |   |   +-- IMyAirtightDoorBase
|   |   |   |   +-- IMyAirtightHangarDoor
|   |   |   |   +-- IMyAirtightSlideDoor
|   |   |   +-- IMyAirtightHangarDoor
|   |   |   +-- IMyAirtightSlideDoor
|   |   |   +-- IMyAssembler
|   |   |   +-- IMyBasicMissionBlock
|   |   |   +-- IMyBatteryBlock
|   |   |   +-- IMyBeacon
|   |   |   +-- IMyCameraBlock
|   |   |   +-- IMyCollector
|   |   |   +-- IMyConveyorSorter
|   |   |   +-- IMyDecoy
|   |   |   +-- IMyDefensiveCombatBlock
|   |   |   +-- IMyDoor
|   |   |   |   +-- IMyAdvancedDoor
|   |   |   |   +-- IMyAirtightDoorBase
|   |   |   |   |   +-- IMyAirtightHangarDoor
|   |   |   |   |   +-- IMyAirtightSlideDoor
|   |   |   |   +-- IMyAirtightHangarDoor
|   |   |   |   +-- IMyAirtightSlideDoor
|   |   |   +-- IMyEmotionControllerBlock
|   |   |   +-- IMyEventControllerBlock
|   |   |   +-- IMyExhaustBlock
|   |   |   +-- IMyExtendedPistonBase
|   |   |   +-- IMyFlightMovementBlock
|   |   |   +-- IMyGasGenerator
|   |   |   +-- IMyGasTank
|   |   |   +-- IMyGyro
|   |   |   +-- IMyJumpDrive
|   |   |   +-- IMyLargeTurretBase
|   |   |   +-- IMyLaserAntenna
|   |   |   +-- IMyLightingBlock
|   |   |   |   +-- IMyReflectorLight
|   |   |   +-- IMyMechanicalConnectionBlock
|   |   |   |   +-- IMyExtendedPistonBase
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorBase
|   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   |   +-- IMyMotorStator
|   |   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   |   +-- IMyMotorSuspension
|   |   |   |   +-- IMyMotorStator
|   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorSuspension
|   |   |   |   +-- IMyPistonBase
|   |   |   |   |   +-- IMyExtendedPistonBase
|   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorBase
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorStator
|   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorSuspension
|   |   |   +-- IMyMotorStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorSuspension
|   |   |   +-- IMyOffensiveCombatBlock
|   |   |   +-- IMyOreDetector
|   |   |   +-- IMyPathRecorderBlock
|   |   |   +-- IMyPistonBase
|   |   |   |   +-- IMyExtendedPistonBase
|   |   |   +-- IMyPowerProducer
|   |   |   |   +-- IMyBatteryBlock
|   |   |   |   +-- IMyReactor
|   |   |   |   +-- IMyWindTurbine
|   |   |   +-- IMyProductionBlock
|   |   |   |   +-- IMyAssembler
|   |   |   |   +-- IMyRefinery
|   |   |   +-- IMyProgrammableBlock
|   |   |   +-- IMyProjector
|   |   |   +-- IMyRadioAntenna
|   |   |   +-- IMyReactor
|   |   |   +-- IMyRefinery
|   |   |   +-- IMyReflectorLight
|   |   |   +-- IMySearchlight
|   |   |   +-- IMySensorBlock
|   |   |   +-- IMyShipConnector
|   |   |   +-- IMyShipDrill
|   |   |   +-- IMyShipGrinder
|   |   |   +-- IMyShipToolBase
|   |   |   |   +-- IMyShipDrill
|   |   |   |   +-- IMyShipGrinder
|   |   |   |   +-- IMyShipWelder
|   |   |   +-- IMyShipWelder
|   |   |   +-- IMySmallGatlingGun
|   |   |   +-- IMySmallMissileLauncher
|   |   |   |   +-- IMySmallMissileLauncherReload
|   |   |   +-- IMySmallMissileLauncherReload
|   |   |   +-- IMyStoreBlock
|   |   |   |   +-- IMyVendingMachine
|   |   |   +-- IMyTargetDummyBlock
|   |   |   +-- IMyTextPanel
|   |   |   +-- IMyThrust
|   |   |   +-- IMyUpgradeModule
|   |   |   +-- IMyUserControllableGun
|   |   |   |   +-- IMyLargeTurretBase
|   |   |   |   +-- IMySmallGatlingGun
|   |   |   |   +-- IMySmallMissileLauncher
|   |   |   |   |   +-- IMySmallMissileLauncherReload
|   |   |   |   +-- IMySmallMissileLauncherReload
|   |   |   +-- IMyVendingMachine
|   |   |   +-- IMyWindTurbine
|   |   +-- IMyGasGenerator
|   |   |   +-- IMyGasGenerator
|   |   +-- IMyGasGenerator
|   |   +-- IMyGasTank
|   |   |   +-- IMyGasTank
|   |   +-- IMyGasTank
|   |   +-- IMyGyro
|   |   +-- IMyGyro
|   |   |   +-- IMyGyro
|   |   +-- IMyJumpDrive
|   |   |   +-- IMyJumpDrive
|   |   +-- IMyJumpDrive
|   |   +-- IMyLargeTurretBase
|   |   |   +-- IMyLargeTurretBase
|   |   +-- IMyLargeTurretBase
|   |   +-- IMyLaserAntenna
|   |   |   +-- IMyLaserAntenna
|   |   +-- IMyLaserAntenna
|   |   +-- IMyLightingBlock
|   |   |   +-- IMyReflectorLight
|   |   +-- IMyLightingBlock
|   |   |   +-- IMyLightingBlock
|   |   |   |   +-- IMyReflectorLight
|   |   |   +-- IMyReflectorLight
|   |   |   |   +-- IMyReflectorLight
|   |   |   +-- IMyReflectorLight
|   |   +-- IMyMechanicalConnectionBlock
|   |   |   +-- IMyExtendedPistonBase
|   |   |   |   +-- IMyExtendedPistonBase
|   |   |   +-- IMyExtendedPistonBase
|   |   |   +-- IMyMechanicalConnectionBlock
|   |   |   |   +-- IMyExtendedPistonBase
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorBase
|   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   |   +-- IMyMotorStator
|   |   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   |   +-- IMyMotorSuspension
|   |   |   |   +-- IMyMotorStator
|   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorSuspension
|   |   |   |   +-- IMyPistonBase
|   |   |   |   |   +-- IMyExtendedPistonBase
|   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorBase
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorStator
|   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorSuspension
|   |   |   +-- IMyMotorBase
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorBase
|   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   |   +-- IMyMotorStator
|   |   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   |   +-- IMyMotorSuspension
|   |   |   |   +-- IMyMotorStator
|   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   |   +-- IMyMotorStator
|   |   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorStator
|   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorSuspension
|   |   |   |   |   +-- IMyMotorSuspension
|   |   |   |   +-- IMyMotorSuspension
|   |   |   +-- IMyMotorStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorStator
|   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorSuspension
|   |   |   |   +-- IMyMotorSuspension
|   |   |   +-- IMyMotorSuspension
|   |   |   +-- IMyPistonBase
|   |   |   |   +-- IMyExtendedPistonBase
|   |   |   |   |   +-- IMyExtendedPistonBase
|   |   |   |   +-- IMyExtendedPistonBase
|   |   |   |   +-- IMyPistonBase
|   |   |   |   |   +-- IMyExtendedPistonBase
|   |   |   +-- IMyPistonBase
|   |   |   |   +-- IMyExtendedPistonBase
|   |   +-- IMyMechanicalConnectionBlock
|   |   |   +-- IMyExtendedPistonBase
|   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorBase
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorStator
|   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorSuspension
|   |   |   +-- IMyMotorStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorSuspension
|   |   |   +-- IMyPistonBase
|   |   |   |   +-- IMyExtendedPistonBase
|   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorAdvancedStator
|   |   +-- IMyMotorAdvancedStator
|   |   +-- IMyMotorBase
|   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorSuspension
|   |   +-- IMyMotorBase
|   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorBase
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorStator
|   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorSuspension
|   |   |   +-- IMyMotorStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorStator
|   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorSuspension
|   |   |   |   +-- IMyMotorSuspension
|   |   |   +-- IMyMotorSuspension
|   |   +-- IMyMotorStator
|   |   |   +-- IMyMotorAdvancedStator
|   |   +-- IMyMotorStator
|   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   +-- IMyMotorSuspension
|   |   +-- IMyMotorSuspension
|   |   |   +-- IMyMotorSuspension
|   |   +-- IMyOffensiveCombatBlock
|   |   +-- IMyOffensiveCombatBlock
|   |   |   +-- IMyOffensiveCombatBlock
|   |   +-- IMyOreDetector
|   |   +-- IMyOreDetector
|   |   |   +-- IMyOreDetector
|   |   +-- IMyPathRecorderBlock
|   |   +-- IMyPathRecorderBlock
|   |   |   +-- IMyPathRecorderBlock
|   |   +-- IMyPistonBase
|   |   |   +-- IMyExtendedPistonBase
|   |   |   |   +-- IMyExtendedPistonBase
|   |   |   +-- IMyExtendedPistonBase
|   |   |   +-- IMyPistonBase
|   |   |   |   +-- IMyExtendedPistonBase
|   |   +-- IMyPistonBase
|   |   |   +-- IMyExtendedPistonBase
|   |   +-- IMyPowerProducer
|   |   |   +-- IMyBatteryBlock
|   |   |   |   +-- IMyBatteryBlock
|   |   |   +-- IMyBatteryBlock
|   |   |   +-- IMyPowerProducer
|   |   |   |   +-- IMyBatteryBlock
|   |   |   |   +-- IMyReactor
|   |   |   |   +-- IMyWindTurbine
|   |   |   +-- IMyReactor
|   |   |   |   +-- IMyReactor
|   |   |   +-- IMyReactor
|   |   |   +-- IMyWindTurbine
|   |   |   |   +-- IMyWindTurbine
|   |   |   +-- IMyWindTurbine
|   |   +-- IMyPowerProducer
|   |   |   +-- IMyBatteryBlock
|   |   |   +-- IMyReactor
|   |   |   +-- IMyWindTurbine
|   |   +-- IMyProductionBlock
|   |   |   +-- IMyAssembler
|   |   |   |   +-- IMyAssembler
|   |   |   +-- IMyAssembler
|   |   |   +-- IMyProductionBlock
|   |   |   |   +-- IMyAssembler
|   |   |   |   +-- IMyRefinery
|   |   |   +-- IMyRefinery
|   |   |   |   +-- IMyRefinery
|   |   |   +-- IMyRefinery
|   |   +-- IMyProductionBlock
|   |   |   +-- IMyAssembler
|   |   |   +-- IMyRefinery
|   |   +-- IMyProgrammableBlock
|   |   +-- IMyProgrammableBlock
|   |   |   +-- IMyProgrammableBlock
|   |   +-- IMyProjector
|   |   +-- IMyProjector
|   |   |   +-- IMyProjector
|   |   +-- IMyRadioAntenna
|   |   |   +-- IMyRadioAntenna
|   |   +-- IMyRadioAntenna
|   |   +-- IMyReactor
|   |   |   +-- IMyReactor
|   |   +-- IMyReactor
|   |   +-- IMyRefinery
|   |   |   +-- IMyRefinery
|   |   +-- IMyRefinery
|   |   +-- IMyReflectorLight
|   |   |   +-- IMyReflectorLight
|   |   +-- IMyReflectorLight
|   |   +-- IMySearchlight
|   |   +-- IMySearchlight
|   |   |   +-- IMySearchlight
|   |   +-- IMySensorBlock
|   |   |   +-- IMySensorBlock
|   |   +-- IMySensorBlock
|   |   +-- IMyShipConnector
|   |   +-- IMyShipConnector
|   |   |   +-- IMyShipConnector
|   |   +-- IMyShipDrill
|   |   +-- IMyShipDrill
|   |   |   +-- IMyShipDrill
|   |   +-- IMyShipGrinder
|   |   |   +-- IMyShipGrinder
|   |   +-- IMyShipGrinder
|   |   +-- IMyShipToolBase
|   |   |   +-- IMyShipDrill
|   |   |   |   +-- IMyShipDrill
|   |   |   +-- IMyShipDrill
|   |   |   +-- IMyShipGrinder
|   |   |   |   +-- IMyShipGrinder
|   |   |   +-- IMyShipGrinder
|   |   |   +-- IMyShipToolBase
|   |   |   |   +-- IMyShipDrill
|   |   |   |   +-- IMyShipGrinder
|   |   |   |   +-- IMyShipWelder
|   |   |   +-- IMyShipWelder
|   |   |   |   +-- IMyShipWelder
|   |   |   +-- IMyShipWelder
|   |   +-- IMyShipToolBase
|   |   |   +-- IMyShipDrill
|   |   |   +-- IMyShipGrinder
|   |   |   +-- IMyShipWelder
|   |   +-- IMyShipWelder
|   |   +-- IMyShipWelder
|   |   |   +-- IMyShipWelder
|   |   +-- IMySmallGatlingGun
|   |   +-- IMySmallGatlingGun
|   |   |   +-- IMySmallGatlingGun
|   |   +-- IMySmallMissileLauncher
|   |   |   +-- IMySmallMissileLauncher
|   |   |   |   +-- IMySmallMissileLauncherReload
|   |   |   +-- IMySmallMissileLauncherReload
|   |   |   |   +-- IMySmallMissileLauncherReload
|   |   |   +-- IMySmallMissileLauncherReload
|   |   +-- IMySmallMissileLauncher
|   |   |   +-- IMySmallMissileLauncherReload
|   |   +-- IMySmallMissileLauncherReload
|   |   +-- IMySmallMissileLauncherReload
|   |   |   +-- IMySmallMissileLauncherReload
|   |   +-- IMyStoreBlock
|   |   |   +-- IMyStoreBlock
|   |   |   |   +-- IMyVendingMachine
|   |   |   +-- IMyVendingMachine
|   |   +-- IMyStoreBlock
|   |   |   +-- IMyVendingMachine
|   |   +-- IMyTargetDummyBlock
|   |   +-- IMyTargetDummyBlock
|   |   +-- IMyTextPanel
|   |   +-- IMyTextPanel
|   |   |   +-- IMyTextPanel
|   |   +-- IMyThrust
|   |   +-- IMyThrust
|   |   |   +-- IMyThrust
|   |   +-- IMyUpgradeModule
|   |   +-- IMyUpgradeModule
|   |   |   +-- IMyUpgradeModule
|   |   +-- IMyUserControllableGun
|   |   |   +-- IMyLargeTurretBase
|   |   |   +-- IMySmallGatlingGun
|   |   |   +-- IMySmallMissileLauncher
|   |   |   |   +-- IMySmallMissileLauncherReload
|   |   |   +-- IMySmallMissileLauncherReload
|   |   +-- IMyUserControllableGun
|   |   |   +-- IMyLargeTurretBase
|   |   |   |   +-- IMyLargeTurretBase
|   |   |   +-- IMyLargeTurretBase
|   |   |   +-- IMySmallGatlingGun
|   |   |   |   +-- IMySmallGatlingGun
|   |   |   +-- IMySmallGatlingGun
|   |   |   +-- IMySmallMissileLauncher
|   |   |   |   +-- IMySmallMissileLauncherReload
|   |   |   +-- IMySmallMissileLauncher
|   |   |   |   +-- IMySmallMissileLauncher
|   |   |   |   |   +-- IMySmallMissileLauncherReload
|   |   |   |   +-- IMySmallMissileLauncherReload
|   |   |   |   |   +-- IMySmallMissileLauncherReload
|   |   |   |   +-- IMySmallMissileLauncherReload
|   |   |   +-- IMySmallMissileLauncherReload
|   |   |   +-- IMySmallMissileLauncherReload
|   |   |   |   +-- IMySmallMissileLauncherReload
|   |   |   +-- IMyUserControllableGun
|   |   |   |   +-- IMyLargeTurretBase
|   |   |   |   +-- IMySmallGatlingGun
|   |   |   |   +-- IMySmallMissileLauncher
|   |   |   |   |   +-- IMySmallMissileLauncherReload
|   |   |   |   +-- IMySmallMissileLauncherReload
|   |   +-- IMyVendingMachine
|   |   +-- IMyWindTurbine
|   |   |   +-- IMyWindTurbine
|   |   +-- IMyWindTurbine
|   +-- IMyFunctionalBlock
|   |   +-- IMyAdvancedDoor
|   |   +-- IMyAirtightDoorBase
|   |   |   +-- IMyAirtightHangarDoor
|   |   |   +-- IMyAirtightSlideDoor
|   |   +-- IMyAirtightHangarDoor
|   |   +-- IMyAirtightSlideDoor
|   |   +-- IMyAssembler
|   |   +-- IMyBasicMissionBlock
|   |   +-- IMyBatteryBlock
|   |   +-- IMyBeacon
|   |   +-- IMyCameraBlock
|   |   +-- IMyCollector
|   |   +-- IMyConveyorSorter
|   |   +-- IMyDecoy
|   |   +-- IMyDefensiveCombatBlock
|   |   +-- IMyDoor
|   |   |   +-- IMyAdvancedDoor
|   |   |   +-- IMyAirtightDoorBase
|   |   |   |   +-- IMyAirtightHangarDoor
|   |   |   |   +-- IMyAirtightSlideDoor
|   |   |   +-- IMyAirtightHangarDoor
|   |   |   +-- IMyAirtightSlideDoor
|   |   +-- IMyEmotionControllerBlock
|   |   +-- IMyEventControllerBlock
|   |   +-- IMyExhaustBlock
|   |   +-- IMyExtendedPistonBase
|   |   +-- IMyFlightMovementBlock
|   |   +-- IMyGasGenerator
|   |   +-- IMyGasTank
|   |   +-- IMyGyro
|   |   +-- IMyJumpDrive
|   |   +-- IMyLargeTurretBase
|   |   +-- IMyLaserAntenna
|   |   +-- IMyLightingBlock
|   |   |   +-- IMyReflectorLight
|   |   +-- IMyMechanicalConnectionBlock
|   |   |   +-- IMyExtendedPistonBase
|   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorBase
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorStator
|   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorSuspension
|   |   |   +-- IMyMotorStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorSuspension
|   |   |   +-- IMyPistonBase
|   |   |   |   +-- IMyExtendedPistonBase
|   |   +-- IMyMotorAdvancedStator
|   |   +-- IMyMotorBase
|   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorSuspension
|   |   +-- IMyMotorStator
|   |   |   +-- IMyMotorAdvancedStator
|   |   +-- IMyMotorSuspension
|   |   +-- IMyOffensiveCombatBlock
|   |   +-- IMyOreDetector
|   |   +-- IMyPathRecorderBlock
|   |   +-- IMyPistonBase
|   |   |   +-- IMyExtendedPistonBase
|   |   +-- IMyPowerProducer
|   |   |   +-- IMyBatteryBlock
|   |   |   +-- IMyReactor
|   |   |   +-- IMyWindTurbine
|   |   +-- IMyProductionBlock
|   |   |   +-- IMyAssembler
|   |   |   +-- IMyRefinery
|   |   +-- IMyProgrammableBlock
|   |   +-- IMyProjector
|   |   +-- IMyRadioAntenna
|   |   +-- IMyReactor
|   |   +-- IMyRefinery
|   |   +-- IMyReflectorLight
|   |   +-- IMySearchlight
|   |   +-- IMySensorBlock
|   |   +-- IMyShipConnector
|   |   +-- IMyShipDrill
|   |   +-- IMyShipGrinder
|   |   +-- IMyShipToolBase
|   |   |   +-- IMyShipDrill
|   |   |   +-- IMyShipGrinder
|   |   |   +-- IMyShipWelder
|   |   +-- IMyShipWelder
|   |   +-- IMySmallGatlingGun
|   |   +-- IMySmallMissileLauncher
|   |   |   +-- IMySmallMissileLauncherReload
|   |   +-- IMySmallMissileLauncherReload
|   |   +-- IMyStoreBlock
|   |   |   +-- IMyVendingMachine
|   |   +-- IMyTargetDummyBlock
|   |   +-- IMyTextPanel
|   |   +-- IMyThrust
|   |   +-- IMyUpgradeModule
|   |   +-- IMyUserControllableGun
|   |   |   +-- IMyLargeTurretBase
|   |   |   +-- IMySmallGatlingGun
|   |   |   +-- IMySmallMissileLauncher
|   |   |   |   +-- IMySmallMissileLauncherReload
|   |   |   +-- IMySmallMissileLauncherReload
|   |   +-- IMyVendingMachine
|   |   +-- IMyWindTurbine
|   +-- IMyGasGenerator
|   |   +-- IMyGasGenerator
|   +-- IMyGasGenerator
|   +-- IMyGasTank
|   +-- IMyGasTank
|   |   +-- IMyGasTank
|   +-- IMyGyro
|   +-- IMyGyro
|   |   +-- IMyGyro
|   +-- IMyJumpDrive
|   +-- IMyJumpDrive
|   |   +-- IMyJumpDrive
|   +-- IMyLargeTurretBase
|   +-- IMyLargeTurretBase
|   |   +-- IMyLargeTurretBase
|   +-- IMyLaserAntenna
|   |   +-- IMyLaserAntenna
|   +-- IMyLaserAntenna
|   +-- IMyLightingBlock
|   |   +-- IMyReflectorLight
|   +-- IMyLightingBlock
|   |   +-- IMyLightingBlock
|   |   |   +-- IMyReflectorLight
|   |   +-- IMyReflectorLight
|   |   |   +-- IMyReflectorLight
|   |   +-- IMyReflectorLight
|   +-- IMyMechanicalConnectionBlock
|   |   +-- IMyExtendedPistonBase
|   |   +-- IMyMotorAdvancedStator
|   |   +-- IMyMotorBase
|   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorSuspension
|   |   +-- IMyMotorStator
|   |   |   +-- IMyMotorAdvancedStator
|   |   +-- IMyMotorSuspension
|   |   +-- IMyPistonBase
|   |   |   +-- IMyExtendedPistonBase
|   +-- IMyMechanicalConnectionBlock
|   |   +-- IMyExtendedPistonBase
|   |   |   +-- IMyExtendedPistonBase
|   |   +-- IMyExtendedPistonBase
|   |   +-- IMyMechanicalConnectionBlock
|   |   |   +-- IMyExtendedPistonBase
|   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorBase
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorStator
|   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorSuspension
|   |   |   +-- IMyMotorStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorSuspension
|   |   |   +-- IMyPistonBase
|   |   |   |   +-- IMyExtendedPistonBase
|   |   +-- IMyMotorAdvancedStator
|   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorAdvancedStator
|   |   +-- IMyMotorBase
|   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorSuspension
|   |   +-- IMyMotorBase
|   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorBase
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorStator
|   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorSuspension
|   |   |   +-- IMyMotorStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorStator
|   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorSuspension
|   |   |   |   +-- IMyMotorSuspension
|   |   |   +-- IMyMotorSuspension
|   |   +-- IMyMotorStator
|   |   |   +-- IMyMotorAdvancedStator
|   |   +-- IMyMotorStator
|   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   +-- IMyMotorSuspension
|   |   |   +-- IMyMotorSuspension
|   |   +-- IMyMotorSuspension
|   |   +-- IMyPistonBase
|   |   |   +-- IMyExtendedPistonBase
|   |   |   |   +-- IMyExtendedPistonBase
|   |   |   +-- IMyExtendedPistonBase
|   |   |   +-- IMyPistonBase
|   |   |   |   +-- IMyExtendedPistonBase
|   |   +-- IMyPistonBase
|   |   |   +-- IMyExtendedPistonBase
|   +-- IMyMotorAdvancedStator
|   |   +-- IMyMotorAdvancedStator
|   +-- IMyMotorAdvancedStator
|   +-- IMyMotorBase
|   |   +-- IMyMotorAdvancedStator
|   |   +-- IMyMotorStator
|   |   |   +-- IMyMotorAdvancedStator
|   |   +-- IMyMotorSuspension
|   +-- IMyMotorBase
|   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorAdvancedStator
|   |   +-- IMyMotorAdvancedStator
|   |   +-- IMyMotorBase
|   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorSuspension
|   |   +-- IMyMotorStator
|   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   +-- IMyMotorStator
|   |   |   +-- IMyMotorAdvancedStator
|   |   +-- IMyMotorSuspension
|   |   |   +-- IMyMotorSuspension
|   |   +-- IMyMotorSuspension
|   +-- IMyMotorStator
|   |   +-- IMyMotorAdvancedStator
|   +-- IMyMotorStator
|   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorAdvancedStator
|   |   +-- IMyMotorAdvancedStator
|   |   +-- IMyMotorStator
|   |   |   +-- IMyMotorAdvancedStator
|   +-- IMyMotorSuspension
|   +-- IMyMotorSuspension
|   |   +-- IMyMotorSuspension
|   +-- IMyOffensiveCombatBlock
|   |   +-- IMyOffensiveCombatBlock
|   +-- IMyOffensiveCombatBlock
|   +-- IMyOreDetector
|   |   +-- IMyOreDetector
|   +-- IMyOreDetector
|   +-- IMyPathRecorderBlock
|   +-- IMyPathRecorderBlock
|   |   +-- IMyPathRecorderBlock
|   +-- IMyPistonBase
|   |   +-- IMyExtendedPistonBase
|   +-- IMyPistonBase
|   |   +-- IMyExtendedPistonBase
|   |   |   +-- IMyExtendedPistonBase
|   |   +-- IMyExtendedPistonBase
|   |   +-- IMyPistonBase
|   |   |   +-- IMyExtendedPistonBase
|   +-- IMyPowerProducer
|   |   +-- IMyBatteryBlock
|   |   |   +-- IMyBatteryBlock
|   |   +-- IMyBatteryBlock
|   |   +-- IMyPowerProducer
|   |   |   +-- IMyBatteryBlock
|   |   |   +-- IMyReactor
|   |   |   +-- IMyWindTurbine
|   |   +-- IMyReactor
|   |   |   +-- IMyReactor
|   |   +-- IMyReactor
|   |   +-- IMyWindTurbine
|   |   |   +-- IMyWindTurbine
|   |   +-- IMyWindTurbine
|   +-- IMyPowerProducer
|   |   +-- IMyBatteryBlock
|   |   +-- IMyReactor
|   |   +-- IMyWindTurbine
|   +-- IMyProductionBlock
|   |   +-- IMyAssembler
|   |   +-- IMyRefinery
|   +-- IMyProductionBlock
|   |   +-- IMyAssembler
|   |   |   +-- IMyAssembler
|   |   +-- IMyAssembler
|   |   +-- IMyProductionBlock
|   |   |   +-- IMyAssembler
|   |   |   +-- IMyRefinery
|   |   +-- IMyRefinery
|   |   |   +-- IMyRefinery
|   |   +-- IMyRefinery
|   +-- IMyProgrammableBlock
|   |   +-- IMyProgrammableBlock
|   +-- IMyProgrammableBlock
|   +-- IMyProjector
|   |   +-- IMyProjector
|   +-- IMyProjector
|   +-- IMyRadioAntenna
|   +-- IMyRadioAntenna
|   |   +-- IMyRadioAntenna
|   +-- IMyReactor
|   +-- IMyReactor
|   |   +-- IMyReactor
|   +-- IMyRefinery
|   |   +-- IMyRefinery
|   +-- IMyRefinery
|   +-- IMyReflectorLight
|   |   +-- IMyReflectorLight
|   +-- IMyReflectorLight
|   +-- IMyRemoteControl
|   |   +-- IMyRemoteControl
|   +-- IMyRemoteControl
|   +-- IMySearchlight
|   +-- IMySearchlight
|   |   +-- IMySearchlight
|   +-- IMySensorBlock
|   +-- IMySensorBlock
|   |   +-- IMySensorBlock
|   +-- IMyShipConnector
|   +-- IMyShipConnector
|   |   +-- IMyShipConnector
|   +-- IMyShipController
|   |   +-- IMyCockpit
|   |   |   +-- IMyCryoChamber
|   |   +-- IMyCryoChamber
|   |   +-- IMyRemoteControl
|   +-- IMyShipController
|   |   +-- IMyCockpit
|   |   |   +-- IMyCockpit
|   |   |   |   +-- IMyCryoChamber
|   |   |   +-- IMyCryoChamber
|   |   |   |   +-- IMyCryoChamber
|   |   |   +-- IMyCryoChamber
|   |   +-- IMyCockpit
|   |   |   +-- IMyCryoChamber
|   |   +-- IMyCryoChamber
|   |   +-- IMyCryoChamber
|   |   |   +-- IMyCryoChamber
|   |   +-- IMyRemoteControl
|   |   +-- IMyRemoteControl
|   |   |   +-- IMyRemoteControl
|   |   +-- IMyShipController
|   |   |   +-- IMyCockpit
|   |   |   |   +-- IMyCryoChamber
|   |   |   +-- IMyCryoChamber
|   |   |   +-- IMyRemoteControl
|   +-- IMyShipDrill
|   +-- IMyShipDrill
|   |   +-- IMyShipDrill
|   +-- IMyShipGrinder
|   |   +-- IMyShipGrinder
|   +-- IMyShipGrinder
|   +-- IMyShipToolBase
|   |   +-- IMyShipDrill
|   |   |   +-- IMyShipDrill
|   |   +-- IMyShipDrill
|   |   +-- IMyShipGrinder
|   |   |   +-- IMyShipGrinder
|   |   +-- IMyShipGrinder
|   |   +-- IMyShipToolBase
|   |   |   +-- IMyShipDrill
|   |   |   +-- IMyShipGrinder
|   |   |   +-- IMyShipWelder
|   |   +-- IMyShipWelder
|   |   |   +-- IMyShipWelder
|   |   +-- IMyShipWelder
|   +-- IMyShipToolBase
|   |   +-- IMyShipDrill
|   |   +-- IMyShipGrinder
|   |   +-- IMyShipWelder
|   +-- IMyShipWelder
|   |   +-- IMyShipWelder
|   +-- IMyShipWelder
|   +-- IMySmallGatlingGun
|   +-- IMySmallGatlingGun
|   |   +-- IMySmallGatlingGun
|   +-- IMySmallMissileLauncher
|   |   +-- IMySmallMissileLauncherReload
|   +-- IMySmallMissileLauncher
|   |   +-- IMySmallMissileLauncher
|   |   |   +-- IMySmallMissileLauncherReload
|   |   +-- IMySmallMissileLauncherReload
|   |   |   +-- IMySmallMissileLauncherReload
|   |   +-- IMySmallMissileLauncherReload
|   +-- IMySmallMissileLauncherReload
|   +-- IMySmallMissileLauncherReload
|   |   +-- IMySmallMissileLauncherReload
|   +-- IMyStoreBlock
|   |   +-- IMyVendingMachine
|   +-- IMyStoreBlock
|   |   +-- IMyStoreBlock
|   |   |   +-- IMyVendingMachine
|   |   +-- IMyVendingMachine
|   +-- IMyTargetDummyBlock
|   +-- IMyTargetDummyBlock
|   +-- IMyTerminalBlock
|   |   +-- IMyAdvancedDoor
|   |   +-- IMyAirtightDoorBase
|   |   |   +-- IMyAirtightHangarDoor
|   |   |   +-- IMyAirtightSlideDoor
|   |   +-- IMyAirtightHangarDoor
|   |   +-- IMyAirtightSlideDoor
|   |   +-- IMyAssembler
|   |   +-- IMyBasicMissionBlock
|   |   +-- IMyBatteryBlock
|   |   +-- IMyBeacon
|   |   +-- IMyCameraBlock
|   |   +-- IMyCargoContainer
|   |   +-- IMyCockpit
|   |   |   +-- IMyCryoChamber
|   |   +-- IMyCollector
|   |   +-- IMyConveyorSorter
|   |   +-- IMyCryoChamber
|   |   +-- IMyDecoy
|   |   +-- IMyDefensiveCombatBlock
|   |   +-- IMyDoor
|   |   |   +-- IMyAdvancedDoor
|   |   |   +-- IMyAirtightDoorBase
|   |   |   |   +-- IMyAirtightHangarDoor
|   |   |   |   +-- IMyAirtightSlideDoor
|   |   |   +-- IMyAirtightHangarDoor
|   |   |   +-- IMyAirtightSlideDoor
|   |   +-- IMyEmotionControllerBlock
|   |   +-- IMyEventControllerBlock
|   |   +-- IMyExhaustBlock
|   |   +-- IMyExtendedPistonBase
|   |   +-- IMyFlightMovementBlock
|   |   +-- IMyFunctionalBlock
|   |   |   +-- IMyAdvancedDoor
|   |   |   +-- IMyAirtightDoorBase
|   |   |   |   +-- IMyAirtightHangarDoor
|   |   |   |   +-- IMyAirtightSlideDoor
|   |   |   +-- IMyAirtightHangarDoor
|   |   |   +-- IMyAirtightSlideDoor
|   |   |   +-- IMyAssembler
|   |   |   +-- IMyBasicMissionBlock
|   |   |   +-- IMyBatteryBlock
|   |   |   +-- IMyBeacon
|   |   |   +-- IMyCameraBlock
|   |   |   +-- IMyCollector
|   |   |   +-- IMyConveyorSorter
|   |   |   +-- IMyDecoy
|   |   |   +-- IMyDefensiveCombatBlock
|   |   |   +-- IMyDoor
|   |   |   |   +-- IMyAdvancedDoor
|   |   |   |   +-- IMyAirtightDoorBase
|   |   |   |   |   +-- IMyAirtightHangarDoor
|   |   |   |   |   +-- IMyAirtightSlideDoor
|   |   |   |   +-- IMyAirtightHangarDoor
|   |   |   |   +-- IMyAirtightSlideDoor
|   |   |   +-- IMyEmotionControllerBlock
|   |   |   +-- IMyEventControllerBlock
|   |   |   +-- IMyExhaustBlock
|   |   |   +-- IMyExtendedPistonBase
|   |   |   +-- IMyFlightMovementBlock
|   |   |   +-- IMyGasGenerator
|   |   |   +-- IMyGasTank
|   |   |   +-- IMyGyro
|   |   |   +-- IMyJumpDrive
|   |   |   +-- IMyLargeTurretBase
|   |   |   +-- IMyLaserAntenna
|   |   |   +-- IMyLightingBlock
|   |   |   |   +-- IMyReflectorLight
|   |   |   +-- IMyMechanicalConnectionBlock
|   |   |   |   +-- IMyExtendedPistonBase
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorBase
|   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   |   +-- IMyMotorStator
|   |   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   |   +-- IMyMotorSuspension
|   |   |   |   +-- IMyMotorStator
|   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorSuspension
|   |   |   |   +-- IMyPistonBase
|   |   |   |   |   +-- IMyExtendedPistonBase
|   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorBase
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorStator
|   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorSuspension
|   |   |   +-- IMyMotorStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorSuspension
|   |   |   +-- IMyOffensiveCombatBlock
|   |   |   +-- IMyOreDetector
|   |   |   +-- IMyPathRecorderBlock
|   |   |   +-- IMyPistonBase
|   |   |   |   +-- IMyExtendedPistonBase
|   |   |   +-- IMyPowerProducer
|   |   |   |   +-- IMyBatteryBlock
|   |   |   |   +-- IMyReactor
|   |   |   |   +-- IMyWindTurbine
|   |   |   +-- IMyProductionBlock
|   |   |   |   +-- IMyAssembler
|   |   |   |   +-- IMyRefinery
|   |   |   +-- IMyProgrammableBlock
|   |   |   +-- IMyProjector
|   |   |   +-- IMyRadioAntenna
|   |   |   +-- IMyReactor
|   |   |   +-- IMyRefinery
|   |   |   +-- IMyReflectorLight
|   |   |   +-- IMySearchlight
|   |   |   +-- IMySensorBlock
|   |   |   +-- IMyShipConnector
|   |   |   +-- IMyShipDrill
|   |   |   +-- IMyShipGrinder
|   |   |   +-- IMyShipToolBase
|   |   |   |   +-- IMyShipDrill
|   |   |   |   +-- IMyShipGrinder
|   |   |   |   +-- IMyShipWelder
|   |   |   +-- IMyShipWelder
|   |   |   +-- IMySmallGatlingGun
|   |   |   +-- IMySmallMissileLauncher
|   |   |   |   +-- IMySmallMissileLauncherReload
|   |   |   +-- IMySmallMissileLauncherReload
|   |   |   +-- IMyStoreBlock
|   |   |   |   +-- IMyVendingMachine
|   |   |   +-- IMyTargetDummyBlock
|   |   |   +-- IMyTextPanel
|   |   |   +-- IMyThrust
|   |   |   +-- IMyUpgradeModule
|   |   |   +-- IMyUserControllableGun
|   |   |   |   +-- IMyLargeTurretBase
|   |   |   |   +-- IMySmallGatlingGun
|   |   |   |   +-- IMySmallMissileLauncher
|   |   |   |   |   +-- IMySmallMissileLauncherReload
|   |   |   |   +-- IMySmallMissileLauncherReload
|   |   |   +-- IMyVendingMachine
|   |   |   +-- IMyWindTurbine
|   |   +-- IMyGasGenerator
|   |   +-- IMyGasTank
|   |   +-- IMyGyro
|   |   +-- IMyJumpDrive
|   |   +-- IMyLargeTurretBase
|   |   +-- IMyLaserAntenna
|   |   +-- IMyLightingBlock
|   |   |   +-- IMyReflectorLight
|   |   +-- IMyMechanicalConnectionBlock
|   |   |   +-- IMyExtendedPistonBase
|   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorBase
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorStator
|   |   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   |   +-- IMyMotorSuspension
|   |   |   +-- IMyMotorStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorSuspension
|   |   |   +-- IMyPistonBase
|   |   |   |   +-- IMyExtendedPistonBase
|   |   +-- IMyMotorAdvancedStator
|   |   +-- IMyMotorBase
|   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorStator
|   |   |   |   +-- IMyMotorAdvancedStator
|   |   |   +-- IMyMotorSuspension
|   |   +-- IMyMotorStator
|   |   |   +-- IMyMotorAdvancedStator
|   |   +-- IMyMotorSuspension
|   |   +-- IMyOffensiveCombatBlock
|   |   +-- IMyOreDetector
|   |   +-- IMyPathRecorderBlock
|   |   +-- IMyPistonBase
|   |   |   +-- IMyExtendedPistonBase
|   |   +-- IMyPowerProducer
|   |   |   +-- IMyBatteryBlock
|   |   |   +-- IMyReactor
|   |   |   +-- IMyWindTurbine
|   |   +-- IMyProductionBlock
|   |   |   +-- IMyAssembler
|   |   |   +-- IMyRefinery
|   |   +-- IMyProgrammableBlock
|   |   +-- IMyProjector
|   |   +-- IMyRadioAntenna
|   |   +-- IMyReactor
|   |   +-- IMyRefinery
|   |   +-- IMyReflectorLight
|   |   +-- IMyRemoteControl
|   |   +-- IMySearchlight
|   |   +-- IMySensorBlock
|   |   +-- IMyShipConnector
|   |   +-- IMyShipController
|   |   |   +-- IMyCockpit
|   |   |   |   +-- IMyCryoChamber
|   |   |   +-- IMyCryoChamber
|   |   |   +-- IMyRemoteControl
|   |   +-- IMyShipDrill
|   |   +-- IMyShipGrinder
|   |   +-- IMyShipToolBase
|   |   |   +-- IMyShipDrill
|   |   |   +-- IMyShipGrinder
|   |   |   +-- IMyShipWelder
|   |   +-- IMyShipWelder
|   |   +-- IMySmallGatlingGun
|   |   +-- IMySmallMissileLauncher
|   |   |   +-- IMySmallMissileLauncherReload
|   |   +-- IMySmallMissileLauncherReload
|   |   +-- IMyStoreBlock
|   |   |   +-- IMyVendingMachine
|   |   +-- IMyTargetDummyBlock
|   |   +-- IMyTextPanel
|   |   +-- IMyThrust
|   |   +-- IMyUpgradeModule
|   |   +-- IMyUserControllableGun
|   |   |   +-- IMyLargeTurretBase
|   |   |   +-- IMySmallGatlingGun
|   |   |   +-- IMySmallMissileLauncher
|   |   |   |   +-- IMySmallMissileLauncherReload
|   |   |   +-- IMySmallMissileLauncherReload
|   |   +-- IMyVendingMachine
|   |   +-- IMyWarhead
|   |   +-- IMyWindTurbine
|   +-- IMyTextPanel
|   |   +-- IMyTextPanel
|   +-- IMyTextPanel
|   +-- IMyThrust
|   +-- IMyThrust
|   |   +-- IMyThrust
|   +-- IMyUpgradeModule
|   +-- IMyUpgradeModule
|   |   +-- IMyUpgradeModule
|   +-- IMyUserControllableGun
|   |   +-- IMyLargeTurretBase
|   |   +-- IMySmallGatlingGun
|   |   +-- IMySmallMissileLauncher
|   |   |   +-- IMySmallMissileLauncherReload
|   |   +-- IMySmallMissileLauncherReload
|   +-- IMyUserControllableGun
|   |   +-- IMyLargeTurretBase
|   |   |   +-- IMyLargeTurretBase
|   |   +-- IMyLargeTurretBase
|   |   +-- IMySmallGatlingGun
|   |   |   +-- IMySmallGatlingGun
|   |   +-- IMySmallGatlingGun
|   |   +-- IMySmallMissileLauncher
|   |   |   +-- IMySmallMissileLauncherReload
|   |   +-- IMySmallMissileLauncher
|   |   |   +-- IMySmallMissileLauncher
|   |   |   |   +-- IMySmallMissileLauncherReload
|   |   |   +-- IMySmallMissileLauncherReload
|   |   |   |   +-- IMySmallMissileLauncherReload
|   |   |   +-- IMySmallMissileLauncherReload
|   |   +-- IMySmallMissileLauncherReload
|   |   +-- IMySmallMissileLauncherReload
|   |   |   +-- IMySmallMissileLauncherReload
|   |   +-- IMyUserControllableGun
|   |   |   +-- IMyLargeTurretBase
|   |   |   +-- IMySmallGatlingGun
|   |   |   +-- IMySmallMissileLauncher
|   |   |   |   +-- IMySmallMissileLauncherReload
|   |   |   +-- IMySmallMissileLauncherReload
|   +-- IMyVendingMachine
|   +-- IMyWarhead
|   |   +-- IMyWarhead
|   +-- IMyWarhead
|   +-- IMyWindTurbine
|   |   +-- IMyWindTurbine
|   +-- IMyWindTurbine

+-- IMyTerminalControls

+-- IMyTextSurface
|   +-- IMyTextPanel
|   |   +-- IMyTextPanel
|   +-- IMyTextPanel
|   +-- IMyTextSurface
|   |   +-- IMyTextPanel

+-- IMyTextSurfaceProvider
|   +-- IMyCockpit
|   |   +-- IMyCockpit
|   |   |   +-- IMyCryoChamber
|   |   +-- IMyCryoChamber
|   |   |   +-- IMyCryoChamber
|   |   +-- IMyCryoChamber
|   +-- IMyCockpit
|   |   +-- IMyCryoChamber
|   +-- IMyCryoChamber
|   |   +-- IMyCryoChamber
|   +-- IMyCryoChamber
|   +-- IMyProgrammableBlock
|   +-- IMyProgrammableBlock
|   |   +-- IMyProgrammableBlock
|   +-- IMyProjector
|   +-- IMyProjector
|   |   +-- IMyProjector
|   +-- IMyTextSurfaceProvider
|   |   +-- IMyCockpit
|   |   |   +-- IMyCryoChamber
|   |   +-- IMyCryoChamber
|   |   +-- IMyProjector

+-- IMyUpgradableBlock
|   +-- IMyUpgradableBlock

+-- IMyWorkAreaTool

+-- IUserCustomizableTerminalAction

========================================================================================================================
ENUMS IN Sandbox.ModAPI / Sandbox.ModAPI.Ingame
========================================================================================================================

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
ENUM:      BuildCheckResult
Underlying type: Int32
Values:
    OK = 0
    NotConnected = 1
    IntersectedWithGrid = 2
    IntersectedWithSomethingElse = 3
    AlreadyBuilt = 4
    NotFound = 5
    NotWeldable = 6

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
ENUM:      BroadcastTarget
Underlying type: Int32
Values:
    Owner = 0
    Faction = 1
    Everyone = 2

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
ENUM:      ChargeMode
Underlying type: Int32
Values:
    Auto = 0
    Recharge = 1
    Discharge = 2

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
ENUM:      DoorStatus
Underlying type: Int32
Values:
    Opening = 0
    Open = 1
    Closing = 2
    Closed = 3

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
ENUM:      FlightMode
Underlying type: Int32
Values:
    Patrol = 0
    Circle = 1
    OneWay = 2
    Track = 3
    WorkArea = 4

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
ENUM:      MyAssemblerMode
Underlying type: Int32
Values:
    Assembly = 0
    Disassembly = 1

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
ENUM:      MyConveyorSorterMode
Underlying type: Int32
Values:
    Whitelist = 0
    Blacklist = 1

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
ENUM:      MyDetectedEntityType
Underlying type: Int32
Values:
    None = 0
    Unknown = 1
    SmallGrid = 2
    LargeGrid = 3
    CharacterHuman = 4
    CharacterOther = 5
    FloatingObject = 6
    Asteroid = 7
    Planet = 8
    Meteor = 9
    Missile = 10
    Tree = 11
    Forageable = 12

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
ENUM:      MyJumpDriveStatus
Underlying type: Int32
Values:
    Charging = 0
    Ready = 1
    Jumping = 2

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
ENUM:      MyLaserAntennaStatus
Underlying type: Int32
Values:
    Idle = 0
    RotatingToTarget = 1
    SearchingTargetForAntenna = 2
    Connecting = 3
    Connected = 4
    OutOfRange = 5

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
ENUM:      MyPlanetElevation
Underlying type: Int32
Values:
    Sealevel = 0
    Surface = 1

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
ENUM:      MyRotationDirection
Underlying type: Int32
Values:
    AUTO = 0
    CW = 1
    CCW = 2

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
ENUM:      MyShipConnectorStatus
Underlying type: Int32
Values:
    Unconnected = 0
    Connectable = 1
    Connected = 2

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
ENUM:      MyStoreInsertResults
Underlying type: Int32
Values:
    Success = 0
    Fail_StoreLimitReached = 1
    Fail_PricePerUnitIsLessThanMinimum = 2
    Error = 3

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
ENUM:      MyTerminalAccessScope
Underlying type: Int32
Values:
    All = 0
    Construct = 1
    Grid = 2

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
ENUM:      MyTransmitTarget
Underlying type: Int32
[Flags]
Values:
    None = 0
    Owned = 1
    Ally = 2
    Default = 3
    Neutral = 4
    Enemy = 8
    Everyone = 15

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
ENUM:      PistonStatus
Underlying type: Int32
Values:
    Stopped = 0
    Extending = 1
    Extended = 2
    Retracting = 3
    Retracted = 4

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
ENUM:      TargetingGroupOptions
Underlying type: Int32
Values:
    Default = 0
    Weapons = 1
    Propulsion = 2
    Power = 3

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
ENUM:      TransmissionDistance
Underlying type: Int32
Values:
    CurrentConstruct = 0
    ConnectedConstructs = 1
    AntennaRelay = 2
    TransmissionDistanceMax = 2

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
ENUM:      UpdateFrequency
Underlying type: Byte
[Flags]
Values:
    None = 0
    Update1 = 1
    Update10 = 2
    Update100 = 4
    Once = 8

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI.Ingame
ENUM:      UpdateType
Underlying type: Int32
[Flags]
Values:
    None = 0
    Terminal = 1
    Trigger = 2
    Mod = 8
    Script = 16
    Update1 = 32
    Update10 = 64
    Update100 = 128
    Once = 256
    IGC = 512

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
ENUM:      MyAutopilotPathfindingState
Underlying type: Int32
Values:
    NotUsed = 0
    OctreeGenerating = 1
    VertexFound = 2
    VertexNotFound = 3
    PathFound = 4
    PathNotFound = 5

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
ENUM:      MyExplosionTypeEnum
Underlying type: Byte
Values:
    MISSILE_EXPLOSION = 0
    BOMB_EXPLOSION = 1
    AMMO_EXPLOSION = 2
    GRID_DEFORMATION = 3
    GRID_DESTRUCTION = 4
    WARHEAD_EXPLOSION_02 = 5
    WARHEAD_EXPLOSION_15 = 6
    WARHEAD_EXPLOSION_30 = 7
    WARHEAD_EXPLOSION_50 = 8
    CUSTOM = 9
    ProjectileExplosion = 10

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
ENUM:      MyGridStorageRequestResult
Underlying type: Int32
[Flags]
Values:
    Success = 0
    GridNotFound = 1
    GridIsStatic = 2
    OwnershipIssue = 4
    InventoryIssue = 8
    GridsPerPlayerLimitReached = 16
    BlockLimitsViolation = 32
    HasLinkedGrids = 64
    UnspecifiedStorageIssue = 128
    SpawnSpotIsOccupied = 256
    InsufficientPCU = 512
    PlayerBlockLimitsExceeded = 1024

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
ENUM:      MyStoredGridOwnershipKind
Underlying type: Int32
Values:
    Unknown = 0
    Player = 1
    FactionShare = 2

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
ENUM:      MySurfaceImpactEnum
Underlying type: Int32
Values:
    Metal = 0
    Destructible = 1
    Indestructible = 2
    Character = 3

----------------------------------------------------------------------------------------------------
NAMESPACE: Sandbox.ModAPI
ENUM:      RepairResult
Underlying type: Int32
Values:
    Success = 0
    GridStateChanged = 1
    InsufficientFunds = 2
    InsufficientPCU = 3

========================================================================================================================
TARGETED INTERFACES - DETAILED DUMP
========================================================================================================================

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyBatteryBlock ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyBatteryBlock
Inheritance chain (bottom to top):
  -> IMyPowerProducer [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]
  -> IMyPowerProducer [Sandbox.ModAPI.Ingame]
  -> IMyBatteryBlock [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyBeacon ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyBeacon
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]
  -> IMyBeacon [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyCameraBlock ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyCameraBlock
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]
  -> IMyCameraBlock [Sandbox.ModAPI.Ingame]
  -> IMyCameraController [VRage.Game.ModAPI.Interfaces]

  === ALL PROPERTIES (including inherited) ===
    Boolean IsActiveLocal { get;  }

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyCargoContainer ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyCargoContainer
Inheritance chain (bottom to top):
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.ModAPI]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCargoContainer [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyCockpit ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyCockpit
Inheritance chain (bottom to top):
  -> IMyShipController [Sandbox.ModAPI]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.ModAPI]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyShipController [Sandbox.ModAPI.Ingame]
  -> IMyControllableEntity [VRage.Game.ModAPI.Interfaces]
  -> IMyTargetingCapableBlock [Sandbox.ModAPI]
  -> IMyCockpit [Sandbox.ModAPI.Ingame]
  -> IMyTextSurfaceProvider [Sandbox.ModAPI.Ingame]
  -> IMyCameraController [VRage.Game.ModAPI.Interfaces]
  -> IMyTextSurfaceProvider [Sandbox.ModAPI]

  === ALL PROPERTIES (including inherited) ===
    Boolean IsOccupied { get;  }
    Single OxygenFilledRatio { get; set; }

  === ALL METHODS (including inherited) ===
    Void AttachPilot(IMyCharacter pilot, Int32 animation)
    Void AttachPilot(IMyCharacter pilot)
    Void RemovePilot()

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyCollector ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyCollector
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]
  -> IMyCollector [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyConveyorSorter ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyConveyorSorter
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]
  -> IMyConveyorSorter [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyConveyorTube ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyConveyorTube
Inheritance chain (bottom to top):
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.ModAPI]
  -> IMyConveyorTube [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyDoor ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyDoor
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]
  -> IMyDoor [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Boolean IsFullyClosed { get;  }

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyFunctionalBlock ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyFunctionalBlock
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]

  === ALL PROPERTIES (including inherited) ===
    Boolean IsUpdateTimerCreated { get;  }
    Boolean IsUpdateTimerEnabled { get;  }

  === ALL METHODS (including inherited) ===
    UInt32 GetFramesFromLastTrigger()

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyGasGenerator ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyGasGenerator
Inheritance chain (bottom to top):
  -> IMyGasGenerator [Sandbox.ModAPI.Ingame]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]

  === ALL PROPERTIES (including inherited) ===
    Single PowerConsumptionMultiplier { get; set; }
    Single ProductionCapacityMultiplier { get; set; }

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyGasTank ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyGasTank
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]
  -> IMyGasTank [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===
    Void ChangeFilledRatio(Double newFilledRatio, Boolean updateSync)

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyGyro ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyGyro
Inheritance chain (bottom to top):
  -> IMyGyro [Sandbox.ModAPI.Ingame]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]

  === ALL PROPERTIES (including inherited) ===
    Single GyroStrengthMultiplier { get; set; }
    Single PowerConsumptionMultiplier { get; set; }

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyJumpDrive ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyJumpDrive
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]
  -> IMyJumpDrive [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Single CurrentStoredPower { get; set; }

  === ALL METHODS (including inherited) ===
    Void Jump(Boolean usePilot)

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyLargeTurretBase ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyLargeTurretBase
Inheritance chain (bottom to top):
  -> IMyUserControllableGun [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]
  -> IMyUserControllableGun [Sandbox.ModAPI.Ingame]
  -> IMyLargeTurretBase [Sandbox.ModAPI.Ingame]
  -> IMyCameraController [VRage.Game.ModAPI.Interfaces]
  -> IMyTargetingCapableBlock [Sandbox.ModAPI]

  === ALL PROPERTIES (including inherited) ===
    IMyEntity Target { get;  }

  === ALL METHODS (including inherited) ===
    Void SetTarget(IMyEntity entity)
    Void TrackTarget(IMyEntity entity)

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyLaserAntenna ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyLaserAntenna
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]
  -> IMyLaserAntenna [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    IMyLaserAntenna Other { get;  }

  === ALL METHODS (including inherited) ===
    Boolean IsInRange(IMyLaserAntenna target)

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyLightingBlock ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyLightingBlock
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]
  -> IMyLightingBlock [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyMotorStator ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyMotorStator
Inheritance chain (bottom to top):
  -> IMyMotorStator [Sandbox.ModAPI.Ingame]
  -> IMyMotorBase [Sandbox.ModAPI.Ingame]
  -> IMyMechanicalConnectionBlock [Sandbox.ModAPI.Ingame]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyMotorBase [Sandbox.ModAPI]
  -> IMyMechanicalConnectionBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyMotorSuspension ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyMotorSuspension
Inheritance chain (bottom to top):
  -> IMyMotorSuspension [Sandbox.ModAPI.Ingame]
  -> IMyMotorBase [Sandbox.ModAPI.Ingame]
  -> IMyMechanicalConnectionBlock [Sandbox.ModAPI.Ingame]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyMotorBase [Sandbox.ModAPI]
  -> IMyMechanicalConnectionBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyPistonBase ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyPistonBase
Inheritance chain (bottom to top):
  -> IMyMechanicalConnectionBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]
  -> IMyMechanicalConnectionBlock [Sandbox.ModAPI.Ingame]
  -> IMyPistonBase [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===
    Void Attach(IMyPistonTop top, Boolean updateGroup)

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyProgrammableBlock ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyProgrammableBlock
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]
  -> IMyProgrammableBlock [Sandbox.ModAPI.Ingame]
  -> IMyTextSurfaceProvider [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Boolean HasCompileErrors { get;  }
    String ProgramData { get; set; }
    String StorageData { get; set; }

  === ALL METHODS (including inherited) ===
    Void Recompile()
    Void Run(String argument, UpdateType updateSource)
    Void Run(String argument)
    Void Run()
    Boolean TryRun(String argument)

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyProjector ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyProjector
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]
  -> IMyProjector [Sandbox.ModAPI.Ingame]
  -> IMyTextSurfaceProvider [Sandbox.ModAPI.Ingame]
  -> IMyTextSurfaceProvider [Sandbox.ModAPI]

  === ALL PROPERTIES (including inherited) ===
    IMyCubeGrid ProjectedGrid { get;  }

  === ALL METHODS (including inherited) ===
    Void Build(IMySlimBlock cubeBlock, Int64 owner, Int64 builder, Boolean requestInstant, Int64 builtBy)
    BuildCheckResult CanBuild(IMySlimBlock projectedBlock, Boolean checkHavokIntersections)
    Boolean LoadBlueprint(String path)
    Boolean LoadRandomBlueprint(String searchPattern)
    Void SetProjectedGrid(MyObjectBuilder_CubeGrid grid)

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyRadioAntenna ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyRadioAntenna
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]
  -> IMyRadioAntenna [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyReactor ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyReactor
Inheritance chain (bottom to top):
  -> IMyReactor [Sandbox.ModAPI.Ingame]
  -> IMyPowerProducer [Sandbox.ModAPI.Ingame]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyPowerProducer [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]

  === ALL PROPERTIES (including inherited) ===
    Single PowerOutputMultiplier { get; set; }

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyRemoteControl ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyRemoteControl
Inheritance chain (bottom to top):
  -> IMyShipController [Sandbox.ModAPI]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.ModAPI]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyShipController [Sandbox.ModAPI.Ingame]
  -> IMyControllableEntity [VRage.Game.ModAPI.Interfaces]
  -> IMyTargetingCapableBlock [Sandbox.ModAPI]
  -> IMyRemoteControl [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===
    Vector3D GetFreeDestination(Vector3D originalDestination, Single checkRadius, Single shipRadius)
    Boolean GetNearestPlayer(out Vector3D playerPosition)

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMySensorBlock ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMySensorBlock
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]
  -> IMySensorBlock [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Vector3 FieldMax { get; set; }
    Vector3 FieldMin { get; set; }

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyShipConnector ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyShipConnector
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]
  -> IMyShipConnector [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    IMyShipConnector OtherConnector { get;  }

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyShipController ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyShipController
Inheritance chain (bottom to top):
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.ModAPI]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyShipController [Sandbox.ModAPI.Ingame]
  -> IMyControllableEntity [VRage.Game.ModAPI.Interfaces]
  -> IMyTargetingCapableBlock [Sandbox.ModAPI]

  === ALL PROPERTIES (including inherited) ===
    Boolean HasFirstPersonCamera { get;  }
    Boolean IsDefault3rdView { get;  }
    Boolean IsShooting { get;  }
    IMyCharacter LastPilot { get;  }
    IMyCharacter Pilot { get;  }

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyShipDrill ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyShipDrill
Inheritance chain (bottom to top):
  -> IMyShipDrill [Sandbox.ModAPI.Ingame]
  -> IMyShipToolBase [Sandbox.ModAPI.Ingame]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyShipToolBase [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]

  === ALL PROPERTIES (including inherited) ===
    Single DrillHarvestMultiplier { get; set; }
    Single PowerConsumptionMultiplier { get; set; }

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyShipGrinder ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyShipGrinder
Inheritance chain (bottom to top):
  -> IMyShipToolBase [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]
  -> IMyShipToolBase [Sandbox.ModAPI.Ingame]
  -> IMyShipGrinder [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyShipWelder ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyShipWelder
Inheritance chain (bottom to top):
  -> IMyShipToolBase [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]
  -> IMyShipToolBase [Sandbox.ModAPI.Ingame]
  -> IMyShipWelder [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===
    Boolean IsWithinWorldLimits(IMyProjector projector, String name, Int32 pcuToBuild)

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMySmallGatlingGun ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMySmallGatlingGun
Inheritance chain (bottom to top):
  -> IMyUserControllableGun [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]
  -> IMyUserControllableGun [Sandbox.ModAPI.Ingame]
  -> IMySmallGatlingGun [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMySmallMissileLauncher ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMySmallMissileLauncher
Inheritance chain (bottom to top):
  -> IMyUserControllableGun [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]
  -> IMyUserControllableGun [Sandbox.ModAPI.Ingame]
  -> IMySmallMissileLauncher [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyTerminalBlock ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyTerminalBlock
Inheritance chain (bottom to top):
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.ModAPI]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Boolean IsDetailedInfoDirty { get;  }

  === ALL METHODS (including inherited) ===
    Void ClearDetailedInfo()
    StringBuilder GetDetailedInfo()
    Boolean IsInSameLogicalGroupAs(IMyTerminalBlock other)
    Boolean IsSameConstructAs(IMyTerminalBlock other)
    Void RefreshCustomInfo()
    Void SetDetailedInfoDirty()

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyTextPanel ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyTextPanel
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]
  -> IMyTextSurface [Sandbox.ModAPI]
  -> IMyTextSurface [Sandbox.ModAPI.Ingame]
  -> IMyTextPanel [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyTextSurface ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyTextSurface
Inheritance chain (bottom to top):
  -> IMyTextSurface [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyThrust ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyThrust
Inheritance chain (bottom to top):
  -> IMyThrust [Sandbox.ModAPI.Ingame]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]

  === ALL PROPERTIES (including inherited) ===
    Single PowerConsumptionMultiplier { get; set; }
    Single ThrustMultiplier { get; set; }

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyUpgradableBlock ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyUpgradableBlock
Inheritance chain (bottom to top):
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.ModAPI]
  -> IMyUpgradableBlock [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyUpgradeModule ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyUpgradeModule
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]
  -> IMyUpgradeModule [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyUserControllableGun ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyUserControllableGun
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyEntity [VRage.ModAPI]
  -> IMyUserControllableGun [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyWarhead ===
Namespace: Sandbox.ModAPI
FullName:  Sandbox.ModAPI.IMyWarhead
Inheritance chain (bottom to top):
  -> IMyTerminalBlock [Sandbox.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.ModAPI]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyWarhead [Sandbox.ModAPI.Ingame]
  -> IMyDestroyableObject [VRage.Game.ModAPI.Interfaces]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyBatteryBlock ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyBatteryBlock
Inheritance chain (bottom to top):
  -> IMyPowerProducer [Sandbox.ModAPI.Ingame]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    ChargeMode ChargeMode { get; set; }
    Single CurrentInput { get;  }
    Single CurrentStoredPower { get;  }
    Boolean HasCapacityRemaining { get;  }
    Boolean IsCharging { get;  }
    Single MaxInput { get;  }
    Single MaxStoredPower { get;  }
    Boolean OnlyDischarge { get; set; }
    Boolean OnlyRecharge { get; set; }
    Boolean SemiautoEnabled { get; set; }

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyBeacon ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyBeacon
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    String HudText { get; set; }
    Single Radius { get; set; }

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyCameraBlock ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyCameraBlock
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Double AvailableScanRange { get;  }
    Boolean EnableRaycast { get; set; }
    Boolean IsActive { get;  }
    Single RaycastConeLimit { get;  }
    Double RaycastDistanceLimit { get;  }
    Single RaycastTimeMultiplier { get;  }

  === ALL METHODS (including inherited) ===
    Boolean CanScan(Double distance, Vector3D direction)
    Boolean CanScan(Vector3D target)
    Boolean CanScan(Double distance)
    MyDetectedEntityInfo Raycast(Double distance, Single pitch, Single yaw)
    MyDetectedEntityInfo Raycast(Vector3D targetPos)
    MyDetectedEntityInfo Raycast(Double distance, Vector3D targetDirection)
    Int32 TimeUntilScan(Double distance)

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyCargoContainer ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyCargoContainer
Inheritance chain (bottom to top):
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyCockpit ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyCockpit
Inheritance chain (bottom to top):
  -> IMyShipController [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTextSurfaceProvider [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Single OxygenCapacity { get;  }
    Single OxygenFilledRatio { get;  }

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyCollector ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyCollector
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Boolean UseConveyorSystem { get; set; }

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyConveyorSorter ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyConveyorSorter
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Boolean DrainAll { get; set; }
    MyConveyorSorterMode Mode { get;  }

  === ALL METHODS (including inherited) ===
    Void AddItem(MyInventoryItemFilter item)
    Void GetFilterList(List<MyInventoryItemFilter> items)
    Boolean IsAllowed(MyDefinitionId id)
    Void RemoveItem(MyInventoryItemFilter item)
    Void SetFilter(MyConveyorSorterMode mode, List<MyInventoryItemFilter> items)

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyConveyorTube ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyConveyorTube
Inheritance chain (bottom to top):
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyDoor ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyDoor
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Boolean Open { get;  }
    Single OpenRatio { get;  }
    DoorStatus Status { get;  }

  === ALL METHODS (including inherited) ===
    Void CloseDoor()
    Void OpenDoor()
    Void ToggleDoor()

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyFunctionalBlock ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyFunctionalBlock
Inheritance chain (bottom to top):
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Boolean Enabled { get; set; }

  === ALL METHODS (including inherited) ===
    Void RequestEnable(Boolean enable)

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyGasGenerator ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyGasGenerator
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Boolean AutoRefill { get; set; }
    Boolean IsProducing { get;  }
    Boolean UseConveyorSystem { get; set; }

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyGasTank ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyGasTank
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Boolean AutoRefillBottles { get; set; }
    Single Capacity { get;  }
    Double FilledRatio { get;  }
    Boolean Stockpile { get; set; }

  === ALL METHODS (including inherited) ===
    Void RefillBottles()

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyGyro ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyGyro
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Boolean GyroOverride { get; set; }
    Single GyroPower { get; set; }
    Single Pitch { get; set; }
    Single Roll { get; set; }
    Single Yaw { get; set; }

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyJumpDrive ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyJumpDrive
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Single CurrentStoredPower { get;  }
    Single JumpDistanceMeters { get; set; }
    Single JumpDistanceRatio { get; set; }
    Single MaxJumpDistanceMeters { get;  }
    Single MaxStoredPower { get;  }
    Single MinJumpDistanceMeters { get;  }
    Boolean Recharge { get; set; }
    MyJumpDriveStatus Status { get;  }

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyLargeTurretBase ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyLargeTurretBase
Inheritance chain (bottom to top):
  -> IMyUserControllableGun [Sandbox.ModAPI.Ingame]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Boolean AIEnabled { get;  }
    Single Azimuth { get; set; }
    Boolean CanControl { get;  }
    Single Elevation { get; set; }
    Boolean EnableIdleRotation { get; set; }
    Boolean HasTarget { get;  }
    Boolean IsAimed { get;  }
    Boolean IsUnderControl { get;  }
    Single Range { get; set; }
    Boolean TargetCharacters { get; set; }
    Boolean TargetEnemies { get; set; }
    Boolean TargetLargeGrids { get; set; }
    Boolean TargetMeteors { get; set; }
    Boolean TargetMissiles { get; set; }
    Boolean TargetNeutrals { get; set; }
    Boolean TargetSmallGrids { get; set; }
    Boolean TargetStations { get; set; }

  === ALL METHODS (including inherited) ===
    MyDetectedEntityInfo GetTargetedEntity()
    String GetTargetingGroup()
    Void GetTargetingGroups(List<String> targetingGroups)
    List<String> GetTargetingGroups()
    Void ResetTargetingToDefault()
    Void SetManualAzimuthAndElevation(Single azimuth, Single elevation)
    Void SetTarget(Vector3D pos)
    Void SetTargetingGroup(String groupSubtypeId)
    Void SyncAzimuth()
    Void SyncElevation()
    Void SyncEnableIdleRotation()
    Void TrackTarget(Vector3D pos, Vector3 velocity)

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyLaserAntenna ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyLaserAntenna
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Boolean IsOutsideLimits { get;  }
    Boolean IsPermanent { get; set; }
    Single Range { get; set; }
    Boolean RequireLoS { get;  }
    MyLaserAntennaStatus Status { get;  }
    Vector3D TargetCoords { get;  }

  === ALL METHODS (including inherited) ===
    Void Connect()
    Void SetTargetCoords(String coords)

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyLightingBlock ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyLightingBlock
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Single BlinkIntervalSeconds { get; set; }
    Single BlinkLenght { get;  }
    Single BlinkLength { get; set; }
    Single BlinkOffset { get; set; }
    Color Color { get; set; }
    Single Falloff { get; set; }
    Single Intensity { get; set; }
    Single Radius { get; set; }
    Single ReflectorRadius { get;  }

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyMotorStator ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyMotorStator
Inheritance chain (bottom to top):
  -> IMyMotorBase [Sandbox.ModAPI.Ingame]
  -> IMyMechanicalConnectionBlock [Sandbox.ModAPI.Ingame]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Single Angle { get;  }
    Single BrakingTorque { get; set; }
    Single Displacement { get; set; }
    Single LowerLimitDeg { get; set; }
    Single LowerLimitRad { get; set; }
    Boolean RotorLock { get; set; }
    Single TargetVelocityRad { get; set; }
    Single TargetVelocityRPM { get; set; }
    Single Torque { get; set; }
    Single UpperLimitDeg { get; set; }
    Single UpperLimitRad { get; set; }

  === ALL METHODS (including inherited) ===
    Void RotateToAngle(MyRotationDirection dir, Single desiredAng, Single velAbsRpm)

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyMotorSuspension ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyMotorSuspension
Inheritance chain (bottom to top):
  -> IMyMotorBase [Sandbox.ModAPI.Ingame]
  -> IMyMechanicalConnectionBlock [Sandbox.ModAPI.Ingame]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Boolean AirShockEnabled { get; set; }
    Boolean Brake { get; set; }
    Single Damping { get;  }
    Single Friction { get; set; }
    Single Height { get; set; }
    Boolean InvertPropulsion { get; set; }
    Boolean InvertSteer { get; set; }
    Boolean IsParkingEnabled { get; set; }
    Single MaxSteerAngle { get; set; }
    Single Power { get; set; }
    Boolean Propulsion { get; set; }
    Single PropulsionOverride { get; set; }
    Single SteerAngle { get;  }
    Boolean Steering { get; set; }
    Single SteeringOverride { get; set; }
    Single SteerReturnSpeed { get;  }
    Single SteerSpeed { get;  }
    Single Strength { get; set; }
    Single SuspensionTravel { get;  }

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyPistonBase ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyPistonBase
Inheritance chain (bottom to top):
  -> IMyMechanicalConnectionBlock [Sandbox.ModAPI.Ingame]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Single CurrentPosition { get;  }
    Single HighestPosition { get;  }
    Single LowestPosition { get;  }
    Single MaxLimit { get; set; }
    Single MaxVelocity { get;  }
    Single MinLimit { get; set; }
    Single NormalizedPosition { get;  }
    PistonStatus Status { get;  }
    Single Velocity { get; set; }

  === ALL METHODS (including inherited) ===
    Void Extend()
    Void MoveToPosition(Single extent, Single speed)
    Void Retract()
    Void Reverse()

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyProgrammableBlock ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyProgrammableBlock
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTextSurfaceProvider [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Boolean IsRunning { get;  }
    String TerminalRunArgument { get;  }

  === ALL METHODS (including inherited) ===
    Boolean TryRun(String argument)

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyProjector ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyProjector
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]
  -> IMyTextSurfaceProvider [Sandbox.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Int32 BuildableBlocksCount { get;  }
    Boolean IsProjecting { get;  }
    Vector3I ProjectionOffset { get; set; }
    Int32 ProjectionOffsetX { get;  }
    Int32 ProjectionOffsetY { get;  }
    Int32 ProjectionOffsetZ { get;  }
    Vector3I ProjectionRotation { get; set; }
    Int32 ProjectionRotX { get;  }
    Int32 ProjectionRotY { get;  }
    Int32 ProjectionRotZ { get;  }
    Int32 RemainingArmorBlocks { get;  }
    Int32 RemainingBlocks { get;  }
    Dictionary<MyDefinitionBase, Int32> RemainingBlocksPerType { get;  }
    Boolean ShowOnlyBuildable { get; set; }
    Int32 TotalBlocks { get;  }

  === ALL METHODS (including inherited) ===
    Void UpdateOffsetAndRotation()

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyRadioAntenna ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyRadioAntenna
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Boolean EnableBroadcasting { get; set; }
    String HudText { get; set; }
    Boolean IsBroadcasting { get;  }
    Single Radius { get; set; }
    Boolean ShowShipName { get; set; }

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyReactor ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyReactor
Inheritance chain (bottom to top):
  -> IMyPowerProducer [Sandbox.ModAPI.Ingame]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Boolean UseConveyorSystem { get; set; }

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyRemoteControl ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyRemoteControl
Inheritance chain (bottom to top):
  -> IMyShipController [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    MyWaypointInfo CurrentWaypoint { get;  }
    Direction Direction { get; set; }
    FlightMode FlightMode { get; set; }
    Boolean IsAutoPilotEnabled { get;  }
    Single SpeedLimit { get; set; }
    Boolean WaitForFreeWay { get; set; }

  === ALL METHODS (including inherited) ===
    Void AddWaypoint(MyWaypointInfo coords)
    Void AddWaypoint(Vector3D coords, String name)
    Void ClearWaypoints()
    Boolean GetNearestPlayer(out Vector3D playerPosition)
    Void GetWaypointInfo(List<MyWaypointInfo> waypoints)
    Void SetAutoPilotEnabled(Boolean enabled)
    Void SetCollisionAvoidance(Boolean enabled)
    Void SetDockingMode(Boolean enabled)

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMySensorBlock ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMySensorBlock
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Single BackExtend { get; set; }
    Single BottomExtend { get; set; }
    Boolean DetectAsteroids { get; set; }
    Boolean DetectEnemy { get; set; }
    Boolean DetectFloatingObjects { get; set; }
    Boolean DetectFriendly { get; set; }
    Boolean DetectLargeShips { get; set; }
    Boolean DetectNeutral { get; set; }
    Boolean DetectOwner { get; set; }
    Boolean DetectPlayers { get; set; }
    Boolean DetectSmallShips { get; set; }
    Boolean DetectStations { get; set; }
    Boolean DetectSubgrids { get; set; }
    Single FrontExtend { get; set; }
    Boolean IsActive { get;  }
    MyDetectedEntityInfo LastDetectedEntity { get;  }
    Single LeftExtend { get; set; }
    Single MaxRange { get;  }
    Boolean PlayProximitySound { get; set; }
    Single RightExtend { get; set; }
    Single TopExtend { get; set; }

  === ALL METHODS (including inherited) ===
    Void DetectedEntities(List<MyDetectedEntityInfo> entities)

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyShipConnector ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyShipConnector
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Boolean CollectAll { get; set; }
    Boolean IsConnected { get;  }
    Boolean IsLocked { get;  }
    Boolean IsParkingEnabled { get; set; }
    IMyShipConnector OtherConnector { get;  }
    Single PullStrength { get; set; }
    MyShipConnectorStatus Status { get;  }
    Boolean ThrowOut { get; set; }

  === ALL METHODS (including inherited) ===
    Void Connect()
    Void Disconnect()
    Void ToggleConnect()

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyShipController ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyShipController
Inheritance chain (bottom to top):
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Boolean CanControlShip { get;  }
    Vector3D CenterOfMass { get;  }
    Boolean ControlThrusters { get; set; }
    Boolean ControlWheels { get; set; }
    Boolean DampenersOverride { get; set; }
    Boolean HandBrake { get; set; }
    Boolean HasWheels { get;  }
    Boolean IsMainCockpit { get; set; }
    Boolean IsUnderControl { get;  }
    Vector3 MoveIndicator { get;  }
    Single RollIndicator { get;  }
    Vector2 RotationIndicator { get;  }
    Boolean ShowHorizonIndicator { get; set; }

  === ALL METHODS (including inherited) ===
    MyShipMass CalculateShipMass()
    Vector3D GetArtificialGravity()
    Vector3D GetNaturalGravity()
    Double GetShipSpeed()
    MyShipVelocities GetShipVelocities()
    Vector3D GetTotalGravity()
    Boolean TryGetPlanetElevation(MyPlanetElevation detail, out Double elevation)
    Boolean TryGetPlanetPosition(out Vector3D position)

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyShipDrill ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyShipDrill
Inheritance chain (bottom to top):
  -> IMyShipToolBase [Sandbox.ModAPI.Ingame]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Boolean TerrainClearingMode { get; set; }

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyShipGrinder ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyShipGrinder
Inheritance chain (bottom to top):
  -> IMyShipToolBase [Sandbox.ModAPI.Ingame]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyShipWelder ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyShipWelder
Inheritance chain (bottom to top):
  -> IMyShipToolBase [Sandbox.ModAPI.Ingame]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Boolean HelpOthers { get; set; }

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMySmallGatlingGun ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMySmallGatlingGun
Inheritance chain (bottom to top):
  -> IMyUserControllableGun [Sandbox.ModAPI.Ingame]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Boolean UseConveyorSystem { get;  }

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMySmallMissileLauncher ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMySmallMissileLauncher
Inheritance chain (bottom to top):
  -> IMyUserControllableGun [Sandbox.ModAPI.Ingame]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Boolean UseConveyorSystem { get;  }

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyTerminalBlock ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyTerminalBlock
Inheritance chain (bottom to top):
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    String CustomData { get; set; }
    String CustomInfo { get;  }
    String CustomName { get; set; }
    String CustomNameWithFaction { get;  }
    String DetailedInfo { get;  }
    Boolean ShowInInventory { get; set; }
    Boolean ShowInTerminal { get; set; }
    Boolean ShowInToolbarConfig { get; set; }
    Boolean ShowOnHUD { get; set; }

  === ALL METHODS (including inherited) ===
    Void GetActions(List<ITerminalAction> resultList, Func<ITerminalAction, Boolean> collect)
    ITerminalAction GetActionWithName(String name)
    Void GetProperties(List<ITerminalProperty> resultList, Func<ITerminalProperty, Boolean> collect)
    ITerminalProperty GetProperty(String id)
    Boolean HasLocalPlayerAccess()
    Boolean HasNobodyPlayerAccessToBlock()
    Boolean HasPlayerAccess(Int64 playerId, MyRelationsBetweenPlayerAndBlock defaultNoUser)
    Boolean HasPlayerAccessWithNobodyCheck(Int64 playerId, Boolean isForPB)
    Boolean IsSameConstructAs(IMyTerminalBlock other)
    Void SearchActionsOfName(String name, List<ITerminalAction> resultList, Func<ITerminalAction, Boolean> collect)
    Void SetCustomName(StringBuilder text)
    Void SetCustomName(String text)

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyTextPanel ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyTextPanel
Inheritance chain (bottom to top):
  -> IMyTextSurface [Sandbox.ModAPI.Ingame]
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===

  === ALL METHODS (including inherited) ===
    String GetPublicTitle()
    Boolean WritePublicTitle(String value, Boolean append)

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyTextSurface ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyTextSurface

  === ALL PROPERTIES (including inherited) ===
    TextAlignment Alignment { get; set; }
    Byte BackgroundAlpha { get; set; }
    Color BackgroundColor { get; set; }
    Single ChangeInterval { get; set; }
    ContentType ContentType { get; set; }
    String CurrentlyShownImage { get;  }
    String DisplayName { get;  }
    String Font { get; set; }
    Color FontColor { get; set; }
    Single FontSize { get; set; }
    String Name { get;  }
    Boolean PreserveAspectRatio { get; set; }
    String Script { get; set; }
    Color ScriptBackgroundColor { get; set; }
    Color ScriptForegroundColor { get; set; }
    Vector2 SurfaceSize { get;  }
    Single TextPadding { get; set; }
    Vector2 TextureSize { get;  }

  === ALL METHODS (including inherited) ===
    Void AddImagesToSelection(List<String> ids, Boolean checkExistence)
    Void AddImageToSelection(String id, Boolean checkExistence)
    Void ClearImagesFromSelection()
    MySpriteDrawFrame DrawFrame()
    Void GetFonts(List<String> fonts)
    Void GetScripts(List<String> scripts)
    Void GetSelectedImages(List<String> output)
    Void GetSprites(List<String> sprites)
    String GetText()
    Vector2 MeasureStringInPixels(StringBuilder text, String font, Single scale)
    Void ReadText(StringBuilder buffer, Boolean append)
    Void RemoveImageFromSelection(String id, Boolean removeDuplicates)
    Void RemoveImagesFromSelection(List<String> ids, Boolean removeDuplicates)
    Boolean WriteText(String value, Boolean append)
    Boolean WriteText(StringBuilder value, Boolean append)

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyThrust ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyThrust
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Single CurrentThrust { get;  }
    Single CurrentThrustPercentage { get;  }
    Vector3I GridThrustDirection { get;  }
    Single MaxEffectiveThrust { get;  }
    Single MaxThrust { get;  }
    Single ThrustOverride { get; set; }
    Single ThrustOverridePercentage { get; set; }

  === ALL METHODS (including inherited) ===

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyUpgradableBlock ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyUpgradableBlock
Inheritance chain (bottom to top):
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    UInt32 UpgradeCount { get;  }

  === ALL METHODS (including inherited) ===
    Void FillUpgradesDictionary(Dictionary<String, Single> upgrades)
    Void GetUpgrades(out Dictionary<String, Single> upgrades)

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyUpgradeModule ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyUpgradeModule
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    UInt32 Connections { get;  }
    UInt32 UpgradeCount { get;  }

  === ALL METHODS (including inherited) ===
    Void FillUpgradeList(List<MyUpgradeModuleInfo> upgrades)
    Void GetUpgradeList(out List<MyUpgradeModuleInfo> upgrades)

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyUserControllableGun ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyUserControllableGun
Inheritance chain (bottom to top):
  -> IMyFunctionalBlock [Sandbox.ModAPI.Ingame]
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Boolean IsShooting { get;  }
    Boolean Shoot { get; set; }

  === ALL METHODS (including inherited) ===
    Void ShootOnce()

----------------------------------------------------------------------------------------------------
=== FULL DETAIL: IMyWarhead ===
Namespace: Sandbox.ModAPI.Ingame
FullName:  Sandbox.ModAPI.Ingame.IMyWarhead
Inheritance chain (bottom to top):
  -> IMyTerminalBlock [Sandbox.ModAPI.Ingame]
  -> IMyCubeBlock [VRage.Game.ModAPI.Ingame]
  -> IMyEntity [VRage.Game.ModAPI.Ingame]

  === ALL PROPERTIES (including inherited) ===
    Single DetonationTime { get; set; }
    Boolean IsArmed { get; set; }
    Boolean IsCountingDown { get;  }

  === ALL METHODS (including inherited) ===
    Void Detonate()
    Boolean StartCountdown()
    Boolean StopCountdown()

========================================================================================================================
SUMMARY STATISTICS
========================================================================================================================

Total public types in Sandbox.Common: 315
Interfaces in target namespaces: 244
Enums in target namespaces: 27

Interfaces by namespace:
  Sandbox.ModAPI: 137 interfaces
  Sandbox.ModAPI.Ingame: 107 interfaces

Enums by namespace:
  Sandbox.ModAPI: 7 enums
  Sandbox.ModAPI.Ingame: 20 enums
