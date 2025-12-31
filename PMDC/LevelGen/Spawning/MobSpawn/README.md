# MobSpawn

Mob spawn checks and extra features that modify how monsters are created. Checks determine if a spawn can occur, while extras modify the spawned monster's properties.

## Overview

The MobSpawn system uses a decorator pattern where spawn checks gate spawning and spawn extras modify the resulting character. This allows flexible composition of spawn rules and monster customization.

## Key Concepts

### MobSpawnCheck Classes

Checks that must pass for spawning. Example usage:
```csharp
// Spawn only if save var "boss_defeated" is true
new MobCheckSaveVar("boss_defeated", true)

// Spawn only for 1/3 of players (seed-based)
new MobCheckVersionDiff(0, 3)
```

### MobSpawnExtra Classes

Modifications applied at spawn time.

### Common Patterns

**Scaling enemies by floor:**
```csharp
new MobSpawnLevelScale(10, 1, 2, true)  // Level 10 + floor/2, reroll skills
```

**Boss with held item:**
```csharp
new MobSpawnItem(false, "item_oran_berry")  // Always has Oran Berry
```

**Shiny chance:**
```csharp
new MobSpawnAltColor(32)  // 1/32 chance of shiny
```

## Related

- [../](../) - Spawning overview
- [../MultiTeamSpawner/](../MultiTeamSpawner/) - Team composition
- [../../Floors/GenSteps/](../../Floors/GenSteps/) - Floor generation
