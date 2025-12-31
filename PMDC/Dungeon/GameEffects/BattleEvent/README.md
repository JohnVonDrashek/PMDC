# BattleEvent

Battle event system implementing combat effects, status application, damage modification, and special abilities. This is the core event system that powers move effects, ability triggers, and item interactions during battle.

## Overview

BattleEvents are coroutine-based handlers that execute during specific phases of combat. Each event implements the `Apply()` method which uses C#'s `IEnumerator<YieldInstruction>` pattern to support asynchronous game logic like animations, damage calculations, and sequential effects.

## Key Concepts

### The BattleEvent Pattern

All battle events follow this coroutine pattern:

```csharp
[Serializable]
public class MyBattleEvent : BattleEvent
{
    // Configuration fields
    public int SomeValue;
    public bool SomeFlag;

    // Default constructor (required for serialization)
    public MyBattleEvent() { }

    // Parameterized constructor
    public MyBattleEvent(int value, bool flag)
    {
        SomeValue = value;
        SomeFlag = flag;
    }

    // Copy constructor (for cloning)
    protected MyBattleEvent(MyBattleEvent other)
    {
        SomeValue = other.SomeValue;
        SomeFlag = other.SomeFlag;
    }

    // Clone method (required)
    public override GameEvent Clone() { return new MyBattleEvent(this); }

    // Main logic
    public override IEnumerator<YieldInstruction> Apply(
        GameEventOwner owner,
        Character ownerChar,
        BattleContext context)
    {
        // Access battle state
        Character user = context.User;
        Character target = context.Target;

        // Check conditions
        if (target.Dead)
            yield break;

        // Perform async operations
        yield return CoroutineManager.Instance.StartCoroutine(
            target.InflictDamage(50)
        );

        // Set context state for later events
        context.ContextStates.Set(new DamageDealt(50));
    }
}
```

### BattleContext

The context object provides access to battle state:

```csharp
context.User           // Character using the action
context.Target         // Current target
context.Data           // BattleData (move/action data)
context.ActionType     // Skill, Item, Throw, etc.
context.Item           // Item being used (if applicable)
context.UsageSlot      // Move slot being used

// State management
context.ContextStates     // Per-hit states
context.GlobalContextStates // Per-action states

// Common state access
context.GetContextStateInt<DamageDealt>(0)
context.AddContextStateMult<DmgMult>(false, numerator, denominator)
```

### Common Event Patterns

#### Status Application

```csharp
public override IEnumerator<YieldInstruction> Apply(
    GameEventOwner owner, Character ownerChar, BattleContext context)
{
    StatusEffect status = new StatusEffect("poison");
    status.LoadFromData();

    // Add status states
    status.StatusStates.Set(new StackState(1));
    status.StatusStates.Set(new CountDownState(5));

    yield return CoroutineManager.Instance.StartCoroutine(
        context.Target.AddStatusEffect(context.User, status)
    );
}
```

#### Damage Modification

```csharp
public override IEnumerator<YieldInstruction> Apply(
    GameEventOwner owner, Character ownerChar, BattleContext context)
{
    // Double damage for fire-type moves
    if (context.Data.Element == "fire")
    {
        context.AddContextStateMult<DmgMult>(false, 2, 1);
    }
    yield break;
}
```

#### HP Drain

```csharp
public override IEnumerator<YieldInstruction> Apply(
    GameEventOwner owner, Character ownerChar, BattleContext context)
{
    int damageDone = context.GetContextStateInt<TotalDamageDealt>(true, 0);
    if (damageDone > 0)
    {
        int heal = Math.Max(1, damageDone / 2);
        yield return CoroutineManager.Instance.StartCoroutine(
            context.User.RestoreHP(heal)
        );
    }
}
```

#### Counter/Reflect

```csharp
public override IEnumerator<YieldInstruction> Apply(
    GameEventOwner owner, Character ownerChar, BattleContext context)
{
    int damage = context.GetContextStateInt<DamageDealt>(0);
    if (damage > 0 && context.Data.Category == BattleData.SkillCategory.Physical)
    {
        int reflect = damage / 2;
        yield return CoroutineManager.Instance.StartCoroutine(
            context.User.InflictDamage(reflect)
        );
    }
}
```

## Usage Examples

### Creating a Type-Based Damage Boost

```csharp
// Boost water moves by 50% (1.5x)
var waterBoost = new MultiplyElementEvent(
    element: "water",
    numerator: 3,
    denominator: 2,
    msg: true
);
```

### Creating a Status-On-Hit Effect

```csharp
// 30% chance to poison on hit
var poisonChance = new StatusBattleEvent(
    statusID: "poison",
    affectTarget: true,
    silentCheck: false
);
// Wrap in AdditionalEvent for chance
var additionalPoison = new AdditionalEvent(
    effect: poisonChance,
    chance: 30
);
```

### Creating a HP Drain Move Effect

```csharp
// Drain 50% of damage dealt
var drainEffect = new HPDrainEvent(drainFraction: 2);
```

### Creating a Weather-Dependent Heal

```csharp
// Heal more in sun, less in rain
var weatherHeal = new WeatherHPEvent(
    hpDiv: 4,
    weather: new Dictionary<string, bool>
    {
        { "sunny", true },   // 2x heal
        { "rain", false }    // 0.5x heal
    }
);
```

### Creating a Stat Boost Effect

```csharp
// Raise Attack by 1 stage
var atkBoost = new StatusStackBattleEvent(
    statusID: "mod_attack",
    affectTarget: false,  // Affects user
    silentCheck: false,
    stack: 1
);
```

### Creating a Counter Effect

```csharp
// Reflect 100% of physical damage
var counter = new CounterCategoryEvent(
    category: BattleData.SkillCategory.Physical,
    numerator: 1,
    denominator: 1
);
```

## Event Execution Order

Events are attached to various trigger points in the battle flow:

1. **BeforeAction** - Before the action executes
2. **BeforeHits** - Before hit calculation
3. **BeforeTryHit** - Before accuracy check
4. **OnHit** - When hit lands
5. **AfterHit** - After hit processing
6. **AfterAction** - After action completes

Events are also organized by priority to ensure correct execution order.

## Related

- [../](../README.md) - Parent GameEffects module
- `RogueEssence.Dungeon.BattleEvent` - Base class from engine
- `RogueEssence.Dungeon.BattleContext` - Context passed to events
- [../../AI/](../../AI/README.md) - AI system that triggers these events
