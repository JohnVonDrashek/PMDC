# GenSteps

Generation steps for floor features and mechanics. Each step modifies the map context in a specific way, and steps are executed in priority order to build complete dungeon floors.

## Overview

GenSteps follow the strategy pattern - each step encapsulates a specific generation algorithm. Steps are queued by priority and executed sequentially, allowing complex floors to be built from composable pieces.

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
