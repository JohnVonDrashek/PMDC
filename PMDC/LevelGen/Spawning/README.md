# Spawning

Entity spawning systems for placing monsters, items, and teams on dungeon floors. This module handles both initial floor population and ongoing respawn mechanics.

## Overview

Spawning operates at multiple levels: individual mob configuration, team composition, and floor-wide placement strategies. The system supports conditional spawning based on game state, map conditions, and random factors.

## Key Concepts

### MobSpawn System

Mobs are defined by `MobSpawn` objects containing:
- Base form (species, form, gender)
- Level and stats
- Skills and abilities
- Spawn features (modifiers applied at spawn time)
- Spawn checks (conditions for spawning)

### Team Spawning

Teams can be spawned via:
- `SpecificTeamSpawner` - Predefined team composition
- `ContextSpawner` - Pulls from floor's spawn table
- `LoopedTeamSpawner` - Repeats spawner for multiple instances

### Spawn Steps

Common spawn step patterns:
- `RandomRoomSpawnStep` - Places in random valid rooms
- `PlaceRandomMobsStep` - Places mobs with room filters
- `SpacedRoomSpawnStep` - Ensures minimum spacing

### Species-Based Spawning

Several spawners generate items based on monster species:
- `SpeciesItemContextSpawner` - Uses dungeon context
- `SpeciesItemElementSpawner` - Matches elemental types
- `SpeciesItemListSpawner` - Uses predefined lists

## Related

- [MobSpawn/](./MobSpawn/) - Spawn checks and modifiers
- [MultiTeamSpawner/](./MultiTeamSpawner/) - Team composition
- [../Floors/GenSteps/](../Floors/GenSteps/) - Floor generation steps
