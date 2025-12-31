# LevelGen

Procedural dungeon generation system for PMDC. This module contains all the steps, spawners, and utilities needed to generate randomized dungeon floors with rooms, items, enemies, and special features.

## Overview

The level generation system uses a step-based pipeline architecture where each `GenStep` modifies a generation context in sequence. The pipeline is configured per-floor and orchestrated at the zone level, allowing for consistent theming across dungeon segments while maintaining floor-by-floor variation.

## Key Concepts

### Generation Pipeline

1. **Zone Steps** - Configure which features appear across the dungeon segment
2. **Floor Plan** - Create the grid layout of rooms and halls
3. **Room Generation** - Draw individual rooms with their shapes and terrain
4. **Feature Placement** - Add chests, shops, monster houses, vaults
5. **Entity Spawning** - Place items, enemies, and interactive tiles
6. **Post-Processing** - Seal rooms, set compass directions, finalize tiles

### GenStep Pattern

All generation steps inherit from `GenStep<T>` where `T` is a context type:
- `BaseMapGenContext` - Basic map with tiles
- `ListMapGenContext` - Map with room/hall planning
- `StairsMapGenContext` - Map with entrance/exit stairs
- `IRoomGridGenContext` - Grid-based room layout

### Room Components

Rooms can be tagged with `RoomComponent` classes for filtering:
- `BossRoom` - Contains a boss encounter
- `CornerRoom` - Located at cardinal corners
- `NoEventRoom` - Excluded from random events
- `ConnectivityRoom` - Important for map connectivity

## Related

- [RogueElements](https://github.com/audinowho/RogueElements) - Core procedural generation library
- [RogueEssence.LevelGen](../../../RogueEssence/) - Base generation framework
