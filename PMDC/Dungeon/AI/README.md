# AI

AI behavior plans that control NPC and ally decision-making in dungeons. Each plan type implements a specific behavior pattern that determines how characters move, attack, and interact with the environment.

## Overview

The AI system uses a plan-based architecture where each `AIPlan` subclass implements specific behavior logic. Plans are evaluated through the `Think()` method, which analyzes the game state and returns an appropriate `GameAction`. Multiple plans can be chained together via tactics to create complex AI behaviors.

## Files

| File | Description |
|------|-------------|
| `AIPlan.cs` | **Base class** for all AI plans. Defines `AIFlags`, `AttackChoice`, `PositionChoice` enums and core helper methods |
| `AttackFoesPlan.cs` | Primary combat plan - approaches and attacks visible enemies |
| `AvoidPlan.cs` | Movement plan that maintains distance from targets while staying in attack range |
| `AvoidAllPlan.cs` | Flees from all characters regardless of alignment |
| `AvoidAlliesPlan.cs` | Specifically avoids allied characters |
| `AvoidFoesPlan.cs` | Flees from hostile characters |
| `AvoidFoesAbortPlan.cs` | Flee behavior that aborts under certain conditions |
| `BossPlan.cs` | Special boss AI with enhanced behavior patterns |
| `CultDancePlan.cs` | Specialized ritual/dance movement pattern |
| `ExplorePlan.cs` | Dungeon exploration - moves toward unexplored areas and items |
| `FindItemPlan.cs` | Searches for and navigates to items on the map |
| `FleeStairsPlan.cs` | Panic behavior - heads directly toward dungeon exit |
| `FollowLeaderPlan.cs` | Team AI - follows the party leader at a set distance |
| `OrbitLeaderPlan.cs` | Circles around the leader at a fixed distance |
| `PreparePlan.cs` | Setup behavior - uses buff moves before engaging |
| `RetreaterPlan.cs` | Hit-and-run tactics - attacks then retreats |
| `SpamAttackPlan.cs` | Aggressive stance - repeatedly uses attacks without moving |
| `StalkerPlan.cs` | Pursuit behavior - tracks targets across the dungeon |
| `StayInRangePlan.cs` | Maintains optimal attack distance from targets |
| `ThiefPlan.cs` | Item-focused AI - steals items and flees |
| `WaitPlan.cs` | Idle behavior - waits in place |
| `WaitPeriodPlan.cs` | Waits for a specified duration before activating |
| `WaitUntilAttackedPlan.cs` | Dormant until receiving damage |

## Key Concepts

### AIFlags

Bitflags that modify AI behavior. Defined in `AIPlan.cs`:

```csharp
[Flags]
public enum AIFlags
{
    None = 0,
    TeamPartner = 1,      // Won't attack enemy-of-friend
    Cannibal = 2,         // Will attack allies
    ItemGrabber = 4,      // Will pick up items
    ItemMaster = 8,       // Knows how to use items
    KnowsMatchups = 16,   // Aware of move-neutralizing abilities
    AttackToEscape = 32,  // Uses moves to escape
    WontDisturb = 64,     // Won't attack sleepers/frozen
    TrapAvoider = 128,    // Avoids traps
    PlayerSense = 256,    // Has player-team sensibilities
}
```

### AttackChoice

Strategy for selecting moves during combat:

```csharp
public enum AttackChoice
{
    StandardAttack,   // Only standard attack
    DumbAttack,       // Random weighted, may walk into range
    RandomAttack,     // Random weighted, always attacks when in range
    StatusAttack,     // Prioritizes status moves
    SmartAttack,      // Always chooses optimal move
}
```

### PositionChoice

Movement strategy relative to targets:

```csharp
public enum PositionChoice
{
    Approach,  // Move in regardless of attack range
    Close,     // Get as close as possible within attack range
    Avoid,     // Stay as far as possible within attack range
}
```

### Plan Configuration Properties

Every `AIPlan` has these configurable properties:

| Property | Description |
|----------|-------------|
| `IQ` | AIFlags bitfield controlling behavior |
| `AttackRange` | Minimum range to consider attack moves |
| `StatusRange` | Minimum range for enemy-targeting status moves |
| `SelfStatusRange` | Minimum range for self-targeting status moves |
| `RestrictedMobilityTypes` | Terrain types the AI won't enter |
| `RestrictMobilityPassable` | Whether to restrict passable terrain too |
| `AbandonRangeOnHit` | Attack from any range when damaged |

## Usage

### Creating a Basic Aggressive AI

```csharp
// Create an attack plan with smart move selection
var attackPlan = new AttackFoesPlan(
    AIFlags.KnowsMatchups | AIFlags.TrapAvoider,
    attackRange: 4,
    statusRange: 4,
    selfStatusRange: 4,
    restrictedMobility: TerrainData.Mobility.Water,
    restrictPassable: false
);
attackPlan.AttackPattern = AttackChoice.SmartAttack;
attackPlan.PositionPattern = PositionChoice.Close;
```

### Creating a Thief AI

```csharp
// Thief that steals items and runs
var thiefPlan = new ThiefPlan(
    AIFlags.ItemGrabber | AIFlags.TrapAvoider
);
```

### Creating a Boss AI

```csharp
// Boss with enhanced tactical awareness
var bossPlan = new BossPlan(
    AIFlags.KnowsMatchups | AIFlags.ItemMaster
);
bossPlan.AttackPattern = AttackChoice.SmartAttack;
```

### Creating a Passive Wait-Until-Attacked AI

```csharp
// Dormant until provoked
var waitPlan = new WaitUntilAttackedPlan(AIFlags.None);
```

## AI Plan Pattern

All plans follow this pattern:

1. **Inherit from `AIPlan`** (or `BasePlan` in RogueEssence)
2. **Override `Think(Character controlledChar)`** to return a `GameAction`
3. **Use helper methods** from `AIPlan`:
   - `canPassChar()` - Check if character can push past another
   - `playerSensibleToAttack()` - Check if player AI should attack target
   - `BlockedByObstacleChar()` - Check for blocking characters

Example structure:

```csharp
public class MyCustomPlan : AIPlan
{
    public MyCustomPlan(AIFlags iq) : base(iq) { }

    public override GameAction Think(Character controlledChar)
    {
        // 1. Scan for targets
        // 2. Evaluate options (attack, move, use item)
        // 3. Return appropriate GameAction
        return new GameAction(GameAction.ActionType.Wait, Dir8.None);
    }
}
```

## Related

- [../GameEffects/](../GameEffects/README.md) - Status effects that can influence AI behavior
- `RogueEssence.Dungeon.BasePlan` - Base class from the engine
- `RogueEssence.Dungeon.AITactic` - Combines multiple plans into a behavior
