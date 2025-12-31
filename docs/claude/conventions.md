# PMDC Conventions

## Naming Conventions

| Suffix | Purpose | Example |
|--------|---------|---------|
| `*Event` | Battle/status events | `StatusBattleEvent`, `HealBattleEvent` |
| `*Plan` | AI behavior plans | `AttackFoesPlan`, `AvoidPlan` |
| `*Step` | Level generation steps | `MapDataStep`, `MonsterHouseStep` |
| `*State` | State objects | `CharState`, `StatusState`, `ContextState` |
| `*Data` | Data models | `BattleData`, `MonsterFormData` |
| `*Spawner` | Entity spawning | `MobSpawner`, `SpeciesItemSpawner` |
| `*Gen` | Room generators | `RoomGenOasis`, `RoomGenEvo` |
| `*Editor` | Avalonia editors | `BattleDataEditor`, `ZoneDataEditor` |

## Required Class Patterns

### Serializable Game Objects

All game data classes must follow this pattern:

```csharp
[Serializable]
public class MyEvent : BattleEvent
{
    // Public fields for serialization
    public int Damage;
    public string StatusID;

    // Non-serialized runtime state
    [NonSerialized]
    private List<Loc> cachedPath;

    // 1. Parameterless constructor (required for deserialization)
    public MyEvent() { }

    // 2. Convenience constructor
    public MyEvent(int damage, string statusID)
    {
        Damage = damage;
        StatusID = statusID;
    }

    // 3. Copy constructor (required for cloning)
    protected MyEvent(MyEvent other)
    {
        Damage = other.Damage;
        StatusID = other.StatusID;
        // Note: Don't copy [NonSerialized] fields
    }

    // 4. Clone method (required - virtual in base)
    public override GameEvent Clone() { return new MyEvent(this); }

    // 5. Core logic
    public override IEnumerator<YieldInstruction> Apply(
        GameEventOwner owner, Character ownerChar, BattleContext context)
    {
        // Implementation
        yield break;
    }
}
```

### AI Plans

```csharp
[Serializable]
public class MyPlan : AIPlan
{
    // Serialized configuration
    public AttackChoice AttackPattern;

    // Non-serialized pathfinding state
    [NonSerialized]
    private List<Loc> goalPath;

    public MyPlan() { }

    public MyPlan(AIFlags iq, int attackRange, int statusRange,
        AttackChoice attackPattern) : base(iq, attackRange, statusRange)
    {
        AttackPattern = attackPattern;
    }

    protected MyPlan(MyPlan other) : base(other)
    {
        AttackPattern = other.AttackPattern;
    }

    public override BasePlan CreateNew() { return new MyPlan(this); }

    public override GameAction Think(Character controlledChar,
        bool preThink, IRandom rand)
    {
        // Decision logic - return null to fall through to next plan
        return null;
    }
}
```

### Generation Steps

```csharp
[Serializable]
public class MyGenStep<T> : GenStep<T> where T : BaseMapGenContext
{
    public int SomeParameter;

    public MyGenStep() { }

    public MyGenStep(int param)
    {
        SomeParameter = param;
    }

    public override void Apply(T map)
    {
        // Modify map directly
        map.Map.Music = "my_music";
    }
}
```

### State Objects

```csharp
[Serializable]
public class MyState : StatusState
{
    public int Value;

    public MyState() { }
    public MyState(int value) { Value = value; }
    protected MyState(MyState other) { Value = other.Value; }

    public override GameplayState Clone() { return new MyState(this); }
}
```

## Coroutine Pattern

All async game logic uses C# iterators:

```csharp
public override IEnumerator<YieldInstruction> Apply(...)
{
    // Execute sub-coroutine and wait
    yield return CoroutineManager.Instance.StartCoroutine(
        context.Target.PlayAnim(animation)
    );

    // Execute another action
    yield return CoroutineManager.Instance.StartCoroutine(
        DungeonScene.Instance.LogMsg(message)
    );

    // Early exit
    if (context.Target.Dead)
        yield break;

    // Must end with yield break
    yield break;
}
```

## Context State Usage

### Setting States
```csharp
// Integer states
context.ContextStates.Set(new UserAtkStat(value));
context.ContextStates.Set(new DmgMult(multiplier));

// Custom states
context.ContextStates.Set(new MyCustomState(data));
```

### Reading States
```csharp
// With default value
int atk = context.GetContextStateInt<UserAtkStat>(defaultValue: 0);

// Check if present
if (context.ContextStates.Contains<MyState>())
{
    MyState state = context.ContextStates.Get<MyState>();
}
```

### Local vs Global States
- `ContextStates` - Reset between targets in multi-hit moves
- `GlobalContextStates` - Persist across entire action

## Data References

Use `[DataType]` for cross-references:

```csharp
[DataType(0, DataManager.DataType.Item, false)]
public string ItemNum;  // Item ID

[DataType(1, DataManager.DataType.Status, false)]
public string StatusID; // Status effect ID

[DataType(2, DataManager.DataType.Skill, true)]
public string SkillNum; // Skill ID (nullable)
```

## Priority Levels

For GenStep queueing:
- `Priority.VeryHigh` - Initialization, map setup
- `Priority.High` - Layout, room placement
- `Priority.Normal` - Features, terrain
- `Priority.Low` - Spawning, finishing touches
- `Priority.VeryLow` - Post-processing

## Error Handling

```csharp
// Log errors through DiagManager
DiagManager.Instance.LogError(exception);

// Log info
DiagManager.Instance.LogInfo("Message");

// In-game messages
DungeonScene.Instance.LogMsg(Text.FormatGrammar(
    new StringKey("MSG_KEY").ToLocal(),
    context.Target.GetDisplayName(false)
));
```

## Important Don'ts

1. **Don't edit RogueEssence submodule** - Extend, don't modify
2. **Don't skip Clone pattern** - All serializable objects need it
3. **Don't use yield return null** - Use proper YieldInstruction
4. **Don't forget [NonSerialized]** - Runtime state must not serialize
5. **Don't commit/push without consent** - Per CLAUDE.md rules

## Common Gotchas

### Pathfinding
```csharp
// Returns path with LAST element as next step
Loc[] path = GetPathsBlocked(char, goals, freeGoal, respectPeers, limit);
if (path.Length > 0)
    Loc nextStep = path[path.Length - 1];
```

### Character Matching
```csharp
// Use DungeonScene for alignment checking
Alignment match = DungeonScene.Instance.GetMatchup(char1, char2);
if (match == Alignment.Foe)
    // Enemy
```

### Map Access
```csharp
// Current map through ZoneManager
Map map = ZoneManager.Instance.CurrentMap;

// Tile operations through context in GenSteps
map.TrySetTile(loc, terrain);
```

### AI Flags
```csharp
// Check AI capabilities
if ((IQ & AIFlags.ItemGrabber) != AIFlags.None)
    // Can pick up items

if ((IQ & AIFlags.TrapAvoider) != AIFlags.None)
    // Avoids traps
```
