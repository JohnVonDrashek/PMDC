# PMDC Architecture

## Overview

PMDC is a roguelike dungeon crawler built on the **RogueEssence** engine (git submodule). It implements Mystery Dungeon-style gameplay with procedural dungeon generation, turn-based combat, and tactical AI.

## System Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         PMDC Layer                              │
├─────────────────┬─────────────────┬─────────────────┬──────────┤
│    Dungeon/     │    LevelGen/    │      Data/      │   Dev/   │
│  (AI, Combat)   │  (Generation)   │   (Game Data)   │ (Editor) │
└────────┬────────┴────────┬────────┴────────┬────────┴────┬─────┘
         │                 │                 │             │
         ▼                 ▼                 ▼             ▼
┌─────────────────────────────────────────────────────────────────┐
│                    RogueEssence Engine                          │
├─────────────────┬─────────────────┬─────────────────┬──────────┤
│  DungeonScene   │   ZoneManager   │   DataManager   │ Avalonia │
│  (Game State)   │   (Map/Floor)   │   (Assets)      │   (UI)   │
├─────────────────┴─────────────────┴─────────────────┴──────────┤
│            RogueElements (Procedural Generation)                │
├─────────────────────────────────────────────────────────────────┤
│                   FNA/XNA (Graphics/Input)                      │
└─────────────────────────────────────────────────────────────────┘
```

## Directory Structure

| Directory | Purpose | Key Classes |
|-----------|---------|-------------|
| `PMDC/Data/` | Monster stats, forms, promotions | MonsterFormData, PromoteBranch, RarityData |
| `PMDC/Dungeon/AI/` | AI behavior plans | AIPlan, AttackFoesPlan, AvoidPlan |
| `PMDC/Dungeon/GameEffects/` | Battle events, states | BattleEvent, CharState, StatusState |
| `PMDC/LevelGen/Floors/` | Floor generation steps | GenStep, RoomGen, MonsterHouseStep |
| `PMDC/LevelGen/Zones/` | Zone-level generation | ZoneStep, SpreadVaultZoneStep |
| `PMDC/LevelGen/Spawning/` | Entity spawning | MobSpawn, SpeciesItemSpawner |
| `PMDC/Dev/` | Editor, serialization | DataEditor, UpgradeBinder |
| `MapGenTest/` | Map generation tester | Example.cs, ExampleDebug.cs |

## Core Subsystems

### 1. AI System (`Dungeon/AI/`)

**Base Class:** `AIPlan` (extends `BasePlan` from RogueEssence)

```csharp
public abstract class AIPlan : BasePlan
{
    public abstract GameAction Think(Character controlledChar, bool preThink, IRandom rand);
}
```

**Key Implementations:**
- `AttackFoesPlan` - Aggressive behavior with pathfinding
- `AvoidPlan` / `AvoidFoesPlan` - Evasion strategies
- `BossPlan` - Boss-specific behavior (rage mode at 50% HP)
- `FollowLeaderPlan` - Team following
- `ExplorePlan` - Dungeon exploration

**AI Configuration:**
```csharp
AIFlags IQ;          // ItemGrabber, KnowsMatchups, TrapAvoider, etc.
AttackChoice;        // StandardAttack, SmartAttack, StatusAttack
PositionChoice;      // Approach, Close, Avoid
```

### 2. Battle Event System (`Dungeon/GameEffects/`)

**Base Class:** `BattleEvent` (extends `GameEvent`)

```csharp
public abstract class BattleEvent : GameEvent
{
    public abstract IEnumerator<YieldInstruction> Apply(
        GameEventOwner owner, Character ownerChar, BattleContext context);
}
```

**Event Categories (52+ types):**
| Category | Examples |
|----------|----------|
| Damage | DirectDamageBattleEvent, DamageModBattleEvent |
| Status | StatusBattleEvent, MapStatusBattleEvent |
| Healing | HealBattleEvent, HPDrainEvent |
| Defense | CounterBattleEvent, EndureBattleEvent |
| Movement | DisplaceBattleEvent, ForcedMoveBattleEvent |
| Conditional | ConditionalBattleEvent, InvokeBattleEvent |

**BattleContext State:**
- `User` / `Target` - Combatants
- `ContextStates` - Per-hit states (damage mults, stat boosts)
- `GlobalContextStates` - Persistent across multi-hit moves

### 3. Level Generation (`LevelGen/`)

**Three-Level Pipeline:**

```
Zone (SpreadVaultZoneStep, SpreadBossZoneStep)
    ↓
Segment/Structure (LayeredSegment)
    ↓
Floor (GenStep<T> pipeline via StablePriorityQueue)
```

**Key Base Classes:**
- `GenStep<T>` - Per-floor generation step
- `ZoneStep` - Zone-wide generation (vault/boss placement)
- `RoomGen<T>` - Room shape generation
- `FloorPlanStep<T>` / `GridPlanStep<T>` - Floor layout

**Context Hierarchy:**
```csharp
IGenContext           // Base: seed, RNG
  └─ ITiledGenContext // Tile operations
     └─ IFloorPlanGenContext // Room planning
        └─ BaseMapGenContext // Full map features
```

### 4. State System

**State Hierarchy:**
```
GameplayState (base)
├─ CharState       // Permanent character abilities
├─ StatusState     // Status effect data (HPState, CountDownState)
├─ ContextState    // Battle calculation (UserAtkStat, DmgMult)
├─ ItemState       // Item properties
├─ SkillState      // Move properties
└─ MapStatusState  // Floor-wide effects
```

### 5. Engine Integration

**Key Managers (from RogueEssence):**
- `DungeonScene.Instance` - Active dungeon, character management
- `ZoneManager.Instance` - Current map, pathfinding
- `DataManager.Instance` - Asset loading, caching
- `GameManager.Instance` - BGM, scene transitions
- `CoroutineManager.Instance` - Async effect execution

**Serialization:**
- `SerializerContractResolver` - JSON field filtering
- `UpgradeBinder` - Backward compatibility for saves
- Custom converters for complex types

## Extension Points

| Area | Base Class | PMDC Count | Pattern |
|------|------------|------------|---------|
| AI Plans | `AIPlan` | 15+ | Implement `Think()` |
| Battle Events | `BattleEvent` | 60+ | Implement `Apply()` |
| Gen Steps | `GenStep<T>` | 20+ | Implement `Apply(T map)` |
| Room Gens | `RoomGen<T>` | 8+ | Implement `DrawOnMap()` |
| Zone Steps | `ZoneStep` | 6+ | Implement `Apply()` with queue |
| States | `*State` | 50+ | Clone pattern |

## Data Flow

### Combat Flow
```
Character Action → BattleContext created
    → BeforeAction events
    → BeforeHits events
    → Per-target: BeforeTryHit → OnHit → AfterHit
    → AfterAction events
    → State updates
```

### Generation Flow
```
Zone requested → ZoneGenContext initialized
    → ZoneSteps executed (spread features across floors)
    → For each floor:
        → GenSteps queued by priority
        → Steps execute: tiles → rooms → terrain → spawns
        → Map finalized
```

## Dependencies

| Library | Purpose |
|---------|---------|
| RogueEssence | Game engine (submodule) |
| RogueElements | Procedural generation |
| FNA/XNA | Graphics framework |
| Avalonia | Editor UI framework |
| NLua | Lua scripting |
| Newtonsoft.Json | Serialization |
