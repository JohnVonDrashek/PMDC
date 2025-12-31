# GameEffects

Game effect system containing status effects, state management, and battle events. This module defines how abilities, items, statuses, and moves interact with characters and the dungeon environment.

## Overview

GameEffects implements the state and event systems that power PMDC's combat and status mechanics. It uses a state-based architecture where effects attach data to game entities (characters, items, skills, map statuses) and events trigger logic during gameplay phases.

## Subfolder

- [BattleEvent/](./BattleEvent/README.md) - Battle event implementations (damage, healing, status, counters)

## Key Concepts

### State Architecture

States are data containers that attach to game entities. Each state type has a specific purpose:

```
GameplayState (base)
    |
    +-- CharState      -> Attached to Character
    +-- ItemState      -> Attached to Item
    +-- SkillState     -> Attached to Skill
    +-- StatusState    -> Attached to StatusEffect
    +-- MapStatusState -> Attached to MapStatus
    +-- TileState      -> Attached to Tile
    +-- ContextState   -> Attached to BattleContext
```

### Common Status States

```csharp
// Counter for timed effects (sleep, freeze, etc.)
public class CountDownState : StatusState
{
    public int Counter;
}

// Stack value for stat modifiers (+1 Attack, -2 Defense, etc.)
public class StackState : StatusState
{
    public int Stack;
}

// HP storage for damage-over-time or shields
public class HPState : StatusState
{
    public int HP;
}

// Reference to another character (for targeted statuses)
public class TargetState : StatusState
{
    // TargetChar stored in StatusEffect
}

// Category flags for status classification
public class BadStatusState : StatusState { }
public class MajorStatusState : StatusState { }
public class TransferStatusState : StatusState { }
```

### Item States

```csharp
// Item categories
public class EdibleState : ItemState { }   // Can be eaten
public class FoodState : ItemState { }     // Food items
public class BerryState : ItemState { }    // Berries
public class SeedState : ItemState { }     // Seeds
public class HerbState : ItemState { }     // Herbs
public class GummiState : ItemState { }    // Gummis
public class DrinkState : ItemState { }    // Drinks

// Item usage types
public class WandState : ItemState { }     // Wands
public class OrbState : ItemState { }      // Orbs
public class AmmoState : ItemState { }     // Thrown ammo
public class UtilityState : ItemState { }  // Utility items
public class HeldState : ItemState { }     // Holdable items
public class EquipState : ItemState { }    // Equippable
public class EvoState : ItemState { }      // Evolution items
public class MachineState : ItemState { }  // TMs
public class RecruitState : ItemState { }  // Recruitment items
public class CurerState : ItemState { }    // Status cure items

// Exclusive items
public class ExclusiveState : ItemState
{
    public ExclusiveItemType ItemType;  // Claw, Fang, Ring, etc.
}

public class FamilyState : ItemState
{
    public List<string> Members;  // Pokemon that can use this item
}
```

### Context States

Used during battle calculations:

```csharp
// Damage multiplier (stacks multiplicatively)
public class DmgMult : ContextState { }

// Damage dealt tracking
public class DamageDealt : ContextState { public int Damage; }
public class TotalDamageDealt : ContextState { }
public class HPLost : ContextState { }

// Stat overrides for damage calc
public class UserAtkStat : ContextState { }
public class TargetDefStat : ContextState { }

// Special flags
public class AttackEndure : ContextState { }  // Survive with 1 HP
public class Knockout : ContextState { }      // Target was KO'd
```

### Map Status States

```csharp
// Timer for map-wide effects
public class MapTickState : MapStatusState
{
    public int Counter;
}

// Shop mechanics
public class ShopPriceState : MapStatusState
{
    public int Amount;  // Total price
    public int Cart;    // Items in cart
}

public class ShopSecurityState : MapStatusState
{
    public SpawnList<MobSpawn> Security;
}
```

## Usage

### Creating a Custom Status State

```csharp
[Serializable]
public class MyCustomState : StatusState
{
    public int Value;

    public MyCustomState() { }
    public MyCustomState(int value) { Value = value; }
    protected MyCustomState(MyCustomState other) { Value = other.Value; }

    public override GameplayState Clone() { return new MyCustomState(this); }
}
```

### Attaching States to a Status Effect

```csharp
StatusEffect status = new StatusEffect("my_status");
status.LoadFromData();
status.StatusStates.Set(new StackState(2));
status.StatusStates.Set(new CountDownState(5));
yield return CoroutineManager.Instance.StartCoroutine(
    target.AddStatusEffect(origin, status)
);
```

### Reading States

```csharp
// Check if a status has a specific state
StatusEffect status = character.GetStatusEffect("poison");
if (status != null && status.StatusStates.Contains<BadStatusState>())
{
    // Handle bad status
}

// Get state with default
StackState stack = status.StatusStates.GetWithDefault<StackState>();
int stackValue = stack.Stack;
```

## Related

- [BattleEvent/](./BattleEvent/README.md) - Events that use these states
- `RogueEssence.Data.GameplayState` - Base state class
- `RogueEssence.Dungeon.StatusEffect` - Status effect container
- [../AI/](../AI/README.md) - AI system that reacts to status effects
