# PMDC Code Flows

## 1. Game Startup Flow

```
Program.Main()
├── InitDllMap()                    # Native library loading
├── Serializer.InitSettings()       # JSON serialization setup
├── PathMod.InitPathMod()           # Path configuration
├── DiagManager.InitInstance()      # Diagnostics/logging
├── PathMod.InitNamespaces()        # Namespace loading
├── GraphicsManager.InitParams()    # Graphics config
├── Text.Init()                     # Localization
├── [If dev mode]
│   ├── InitDataEditor()            # Register 100+ editors
│   └── BuildAvaloniaApp().Start()  # Launch editor UI
└── [Else]
    └── GameBase.Run()              # Start game loop
```

**Key File:** `PMDC/Program.cs`

## 2. AI Decision Flow

```
Character Turn
├── BasePlan.Think(controlledChar, preThink, rand)
│   └── AIPlan.Think()  [Abstract - implemented by subclass]
│       ├── GetAcceptableTargets()           # Filter valid targets
│       ├── DungeonScene.GetMatchup()        # Ally/Foe determination
│       ├── GetPathsBlocked()                # A* pathfinding
│       ├── [AttackChoice logic]
│       │   ├── StandardAttack → Regular attack only
│       │   ├── SmartAttack → Best move selection
│       │   └── StatusAttack → Prefer status moves
│       └── Return GameAction
│           ├── ActionType: Move, Attack, Item, Wait
│           └── Direction: Dir8 for movement/attack
└── Execute Action
```

**Key Files:**
- `PMDC/Dungeon/AI/AIPlan.cs` (base + helpers)
- `PMDC/Dungeon/AI/AttackFoesPlan.cs` (aggressive)
- `PMDC/Dungeon/AI/AvoidPlan.cs` (evasive)

## 3. Battle Event Execution Flow

```
Skill/Attack Used
├── Create BattleContext
│   ├── User, Target, ActionType, UsageSlot
│   └── Data (BattleData from skill/item)
│
├── BeforeAction Phase
│   └── Events from User's abilities/status
│
├── BeforeHits Phase
│   └── Range/hitbox modification events
│
├── For Each Target:
│   ├── BeforeTryHit Phase
│   │   └── Accuracy/evasion events
│   │
│   ├── Hit Determination
│   │   └── context.Hit = true/false
│   │
│   ├── OnHit Phase (if Hit)
│   │   ├── Damage calculation events
│   │   │   ├── BasePowerBattleEvent
│   │   │   ├── DamageModBattleEvent
│   │   │   └── DirectDamageBattleEvent
│   │   ├── Status application
│   │   │   └── StatusBattleEvent
│   │   └── Secondary effects
│   │       └── AdditionalEffectBattleEvent
│   │
│   └── AfterHit Phase
│       ├── Recoil damage
│       └── HP drain effects
│
└── AfterAction Phase
    ├── PP consumption
    ├── Counter effects
    └── State cleanup
```

**Key Files:**
- `RogueEssence/Dungeon/GameEffects/BattleContext.cs` (context)
- `PMDC/Dungeon/GameEffects/BattleEvent/*.cs` (events)

## 4. Floor Generation Flow

```
Zone.GetFloor(floorId)
├── ZoneGenContext initialized
│   ├── seed, CurrentZone, CurrentSegment
│   └── ZoneSteps list
│
├── For Each ZoneStep:
│   ├── Instantiate(seed)
│   ├── Apply(zoneContext, context, queue)
│   │   └── Conditionally enqueue GenSteps
│   └── Examples:
│       ├── SpreadVaultZoneStep → Vault rooms
│       ├── SpreadBossZoneStep → Boss placement
│       └── SpreadHouseZoneStep → Monster houses
│
├── IFloorGen.Generate(seed)
│   ├── MapGenContext created
│   │   ├── Width, Height, RoomTerrain, WallTerrain
│   │   └── InitSeed(seed)
│   │
│   ├── Execute GenSteps by Priority:
│   │   ├── Priority.VeryHigh: Floor initialization
│   │   │   └── MapDataStep (music, time limit)
│   │   ├── Priority.High: Layout creation
│   │   │   └── GridPlanStep, FloorPlanStep
│   │   ├── Priority.Normal: Features
│   │   │   ├── RoomTerrainStep
│   │   │   ├── MonsterHouseStep
│   │   │   └── ChestStep
│   │   └── Priority.Low: Spawning
│   │       ├── MobSpawnStep
│   │       └── ItemSpawnStep
│   │
│   └── FinishGen()
│
└── Return generated Map
```

**Key Files:**
- `RogueEssence/LevelGen/ZoneGenContext.cs`
- `RogueEssence/LevelGen/MapGenContext.cs`
- `PMDC/LevelGen/Floors/GenSteps/*.cs`
- `PMDC/LevelGen/Zones/ZoneSteps/*.cs`

## 5. Room Generation Flow

```
RoomGen.DrawOnMap(map)
├── ProposeSize(rand)
│   └── Return Loc(width, height)
│
├── PrepareSize(rand, size)
│   └── Set Draw rectangle
│
├── PrepareFulfillableBorders(rand)
│   └── Initialize which edges can have doors
│
├── SetRoomBorders(map)
│   └── Mark potential door locations
│
├── DrawOnMap(map)  [Abstract implementation]
│   └── Place tiles within Draw rect
│       Examples:
│       ├── RoomGenOasis → Cave with water ring
│       ├── RoomGenEvo → Evolution chamber
│       └── RoomGenGuardedCave → Guarded entrance
│
└── FulfillRoomBorders(map, isHall)
    └── Connect doors to adjacent rooms/halls
```

**Key Files:**
- `RogueElements/MapGen/Rooms/RoomGen.cs` (base)
- `PMDC/LevelGen/Floors/GenSteps/Rooms/*.cs`

## 6. Serialization Flow

```
Save/Load Data
├── Serializer.InitSettings(ContractResolver, Binder)
│
├── Save:
│   ├── SerializerContractResolver.CreateProperty()
│   │   └── Filter [NonSerialized] fields
│   └── JsonConverter.WriteJson()
│       └── Custom converters for complex types
│
└── Load:
    ├── UpgradeBinder.BindToType()
    │   └── Map legacy type names to current types
    │       Examples:
    │       ├── "FloorNameIDZoneStep" → FloorNameDropZoneStep
    │       └── "AllyDifferentEvent" → AlignmentDifferentEvent
    └── JsonConverter.ReadJson()
        └── Deserialize with validation
```

**Key Files:**
- `PMDC/Dev/SerializerContractResolver.cs`
- `PMDC/Dev/UpgradeBinder.cs`
- `PMDC/Dev/JsonConverters.cs`

## 7. Map Generation Testing Flow

```
MapGenTest.Main()
├── Initialize engine (headless)
│   ├── Serializer.InitSettings()
│   ├── DataManager.InitInstance()
│   └── LuaEngine.InitInstance()
│
├── Register debug hooks
│   ├── GenContextDebug.OnInit
│   ├── GenContextDebug.OnStep
│   ├── GenContextDebug.OnStepIn
│   └── GenContextDebug.OnError
│
├── Example.Run()
│   └── Interactive menu:
│       ├── Select Zone
│       ├── Select Structure/Segment
│       ├── Select Floor
│       └── Generate with options:
│           ├── F2: Stress test
│           ├── F3: Custom seed
│           └── F4: Step-in debug
│
└── Display generated map (console)
```

**Key Files:**
- `MapGenTest/Program.cs`
- `MapGenTest/Example/Example.cs`
- `MapGenTest/ExampleDebug.cs`

## 8. Editor Data Flow

```
InitDataEditor()
├── DataEditor.Init()
├── Register Custom Editors:
│   ├── Zone Steps
│   │   ├── SaveVarsZoneStepEditor
│   │   ├── SpreadHouseZoneStepEditor
│   │   └── SpreadVaultZoneStepEditor
│   ├── Gen Steps
│   │   ├── CombinedGridRoomStepEditor
│   │   └── BlobWaterStepEditor
│   ├── Battle Data
│   │   ├── BattleDataEditor
│   │   ├── SkillDataEditor
│   │   └── ExplosionDataEditor
│   ├── Spawners
│   │   ├── MobSpawnEditor
│   │   └── TeamMemberSpawnEditor
│   └── Primitives
│       ├── LocEditor, ColorEditor
│       └── IntRangeEditor, RandRangeEditor
│
└── Launch Avalonia UI
    └── DataEditor displays appropriate editor for each type
```

**Key Files:**
- `PMDC/Program.cs:InitDataEditor()`
- `PMDC/Dev/Editors/*.cs`
- `PMDC/Dev/ViewModels/*.cs`

## Key Coroutine Pattern

All async game logic uses `IEnumerator<YieldInstruction>`:

```csharp
public override IEnumerator<YieldInstruction> Apply(
    GameEventOwner owner, Character ownerChar, BattleContext context)
{
    // Wait for animation
    yield return CoroutineManager.Instance.StartCoroutine(
        context.Target.PlayAnim(anim)
    );

    // Apply damage
    yield return CoroutineManager.Instance.StartCoroutine(
        context.Target.InflictDamage(damage)
    );

    // Set state for chained events
    context.ContextStates.Set(new DamageDealt(damage));

    yield break;
}
```

This allows frame-accurate timing for animations and effects.
