# PMDC - Mystery Dungeon Clone

A roguelike dungeon crawler built on the **RogueEssence** engine, featuring procedural dungeon generation, turn-based combat, and tactical mechanics.

## Claude Code Rules

- **Do not commit without explicit user consent** - Always ask before running `git commit`
- **Do not push without explicit user consent** - Always ask before running `git push`

## Quick Reference

| Aspect | Details |
|--------|---------|
| Language | C# / .NET 8.0 |
| Graphics | FNA (XNA reimplementation) |
| Editor | Avalonia |
| Scripting | Lua via NLua |
| Version | 0.8.11 |

## Build Commands

```bash
# Initialize submodules (required first time)
git submodule update --init --remote -- RogueEssence
git submodule update --init --recursive

# Build for platforms
dotnet publish -c Release -r osx-x64 PMDC/PMDC.csproj    # Mac
dotnet publish -c Release -r linux-x64 PMDC/PMDC.csproj  # Linux
dotnet publish -c Release -r win-x64 PMDC/PMDC.csproj    # Windows

# Run MapGenTest (map generation testing)
dotnet run --project MapGenTest/MapGenTest.csproj
```

## Key Entry Points

- `PMDC/Program.cs` - Main game entry, CLI args (-dev, -quest, -mod, -guide)
- `MapGenTest/Program.cs` - Map generation test harness
- `MapGenTest/Example/Example.cs` - Interactive zone/floor testing

## Directory Guide

| Directory | Purpose |
|-----------|---------|
| `PMDC/Data/` | Monster stats, forms, promotions, rarity data |
| `PMDC/Dungeon/AI/` | AI behavior plans (AttackFoesPlan, AvoidPlan, etc.) |
| `PMDC/Dungeon/GameEffects/` | Battle events, status effects, item states |
| `PMDC/LevelGen/Floors/` | Floor generation steps, rooms, seals |
| `PMDC/LevelGen/Spawning/` | Monster/item spawn logic |
| `PMDC/LevelGen/Zones/` | Zone-level generation steps |
| `PMDC/Dev/` | Editor components, serialization, strategy guide |
| `MapGenTest/` | Standalone map generation tester |
| `RogueEssence/` | Game engine (submodule - avoid editing) |

## Patterns & Conventions

### Serialization Pattern
All game data classes use `[Serializable]` attribute with copy constructor and `Clone()`:
```csharp
[Serializable]
public class MyEvent : BattleEvent
{
    public MyEvent() { }
    protected MyEvent(MyEvent other) { /* copy fields */ }
    public override GameEvent Clone() { return new MyEvent(this); }
}
```

### Coroutine-Based Effects
Battle events use `IEnumerator<YieldInstruction>` for async execution:
```csharp
public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
{
    yield return CoroutineManager.Instance.StartCoroutine(someAction);
    yield break;
}
```

### AI Plans
Inherit from `AIPlan`, implement `Think()` to return `GameAction`:
```csharp
public override GameAction Think(Character controlledChar, bool preThink, IRandom rand)
{
    // Return GameAction or null to fall through
}
```

### Level Generation Pipeline
- `ZoneStep` - Zone-wide generation (spread vaults, bosses)
- `GenStep<T>` - Per-floor generation steps
- Room types inherit from `RoomGen<T>`
- Uses grid-based floor planning

### Naming Conventions
- `*Event` - Battle/status events
- `*Plan` - AI behavior plans
- `*Step` - Level generation steps
- `*Data` - Data models
- `*State` - State objects (CharState, ItemState, etc.)

## Domain Concepts

- **Zone** - A dungeon with multiple floors/structures
- **Structure/Segment** - A section of floors within a zone
- **Floor** - A single procedurally generated level
- **Character** - Player or NPC entity
- **BattleContext** - Context for skill/attack resolution
- **MapStatus** - Global effects on the current floor

## Testing Map Generation

Use MapGenTest for debugging procedural generation:
```bash
dotnet run --project MapGenTest/MapGenTest.csproj
# Interactive menu: select zone -> structure -> floor
# F2 = Stress test, F3 = Custom seed, F4 = Step-in debug
```

## CLI Arguments (PMDC)

| Flag | Description |
|------|-------------|
| `-dev` | Run in dev mode with Lua debugging |
| `-quest [folder]` | Load quest from MODS/ |
| `-mod [mod] [...]` | Load additional mods |
| `-guide` | Generate strategy guide HTML |
| `-csv` | Generate strategy guide CSV |
| `-index [type]` | Reindex data assets |
| `-asset [path]` | Custom asset path |

## Key Classes in RogueEssence

- `DungeonScene` - Main dungeon game state
- `ZoneManager` - Current zone/map management
- `DataManager` - Game data loading/caching
- `Character` - Entity with stats, moves, AI
- `BattleData` - Skill/move data and effects
