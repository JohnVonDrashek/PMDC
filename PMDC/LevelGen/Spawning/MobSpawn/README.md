# MobSpawn

Mob spawn checks and extra features that modify how monsters are created. Checks determine if a spawn can occur, while extras modify the spawned monster's properties.

## Overview

The MobSpawn system uses a decorator pattern where spawn checks gate spawning and spawn extras modify the resulting character. This allows flexible composition of spawn rules and monster customization.

## Files

| File | Description |
|------|-------------|
| `MobSpawnCheck.cs` | Conditional checks determining if a mob can spawn |
| `MobSpawnExtra.cs` | Modifications applied to mobs after spawning |

## Key Concepts

### MobSpawnCheck Classes

Checks that must pass for spawning:

| Check | Description |
|-------|-------------|
| `MobCheckVersionDiff` | Spawns based on player seed modulo (version-specific content) |
| `MobCheckSaveVar` | Spawns based on Lua save variable state |
| `MobCheckMapStart` | Spawns only at map initialization (not implemented) |
| `MobCheckTimeOfDay` | Spawns based on in-game time (dawn, day, dusk, night) |

Example usage:
```csharp
// Spawn only if save var "boss_defeated" is true
new MobCheckSaveVar("boss_defeated", true)

// Spawn only for 1/3 of players (seed-based)
new MobCheckVersionDiff(0, 3)
```

### MobSpawnExtra Classes

Modifications applied at spawn time:

| Extra | Description |
|-------|-------------|
| `MobSpawnWeak` | Reduces fullness to 35% and PP to 50% |
| `MobSpawnAltColor` | Chance for alternate color (shiny) |
| `MobSpawnMovesOff` | Disables or removes moves from index |
| `MobSpawnBoost` | Adds vitamin stat bonuses |
| `MobSpawnScaledBoost` | Level-scaled stat bonuses |
| `MobSpawnItem` | Equips held item |
| `MobSpawnExclFamily` | Equips exclusive item from evolution family |
| `MobSpawnExclElement` | Equips exclusive item matching element |
| `MobSpawnExclAny` | Equips exclusive item from any species |
| `MobSpawnInv` | Fills inventory with items (not dropped) |
| `MobSpawnLevelScale` | Scales level based on floor ID |
| `MobSpawnLoc` | Sets specific spawn location and direction |
| `MobSpawnUnrecruitable` | Prevents recruitment |
| `MobSpawnFoeConflict` | Makes neutral mobs aggressive to enemies |
| `MobSpawnInteractable` | Adds interaction events for allies/neutrals |
| `MobSpawnLuaTable` | Attaches Lua data table |
| `MobSpawnDiscriminator` | Sets personality discriminator |
| `Intrinsic3Chance` | Chance to roll hidden ability |

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
