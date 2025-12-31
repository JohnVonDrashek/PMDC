# GenSteps

Generation steps for floor features and mechanics. Each step modifies the map context in a specific way, and steps are executed in priority order to build complete dungeon floors.

## Overview

GenSteps follow the strategy pattern - each step encapsulates a specific generation algorithm. Steps are queued by priority and executed sequentially, allowing complex floors to be built from composable pieces.

## Directory Structure

| Folder | Description |
|--------|-------------|
| [Detours/](./Detours/) | Hidden rooms accessed via keys or switches |
| [FloorPlan/](./FloorPlan/) | Grid-based room layout algorithms |
| [Rooms/](./Rooms/) | Room shape generators and components |
| [Seals/](./Seals/) | Room locking mechanics (keys, switches, bosses) |

## Files

| File | Description |
|------|-------------|
| `ChestStep.cs` | Places treasure chests that may be trapped (ambush chests spawn monsters when opened) |
| `ShopStep.cs` | Creates shops with merchandise, shopkeeper, and theft detection |
| `MonsterHouseStep.cs` | Standard monster house - room fills with enemies when entered |
| `MonsterHouseBaseStep.cs` | Base class for monster house variants with item/mob themes |
| `MonsterHallStep.cs` | Monster house variant in hallway form |
| `MonsterMansionStep.cs` | Large-scale monster house spanning multiple rooms |
| `MapTileStep.cs` | Paints tiles across the map based on terrain stencils |
| `MapDataStep.cs` | Sets map-wide data and properties |
| `FloorTerrainStep.cs` | Configures floor terrain types |
| `MobSpawnSettingsStep.cs` | Configures mob spawn rates and rules |
| `TempTileStep.cs` | Places temporary/conditional tiles |
| `PatternPlacerStep.cs` | Places predefined patterns on the map |
| `PatternSpawnStep.cs` | Spawns entities in patterns |
| `PatternTerrainStep.cs` | Creates terrain patterns |
| `RoomPostProcStep.cs` | Post-processing for room tiles |
| `RoomTerrainStep.cs` | Sets terrain within rooms |

## Key Concepts

### Step Pattern

All steps inherit from `GenStep<T>` and implement:
```csharp
public override void Apply(T map)
{
    // Modify the map context
}
```

### Monster House System

Monster houses share a common base (`MonsterHouseBaseStep`) with:
- **Item Themes** - Filter items by category (berries, orbs, etc.)
- **Mob Themes** - Filter mobs by type (same species, same element, etc.)
- **Room Filters** - Determine eligible rooms for placement

Variants include:
- `MonsterHouseStep` - Traditional room-based house
- `ChestStep` - Chest that triggers ambush when opened
- `MonsterHallStep` - Hallway variant
- `MonsterMansionStep` - Multi-room variant

### Shop System

`ShopStep` creates functional shops with:
- Shopkeeper mob placement
- Item catalog from themed spawn lists
- Security status for theft detection
- Mat tiles marking shop boundaries

### Room Filtering

Steps use `BaseRoomFilter` to select appropriate rooms:
- Exclude boss rooms, start/exit rooms
- Filter by room components
- Check connectivity requirements

## Related

- [Detours/](./Detours/) - Secret room generation
- [FloorPlan/](./FloorPlan/) - Room layout algorithms
- [Rooms/](./Rooms/) - Room generators
- [Seals/](./Seals/) - Room locking
