# FloorPlan

Grid-based floor layout algorithms that determine room placement and connectivity before rooms are rendered to tiles.

## Overview

Floor planning operates on a grid where each cell can contain a room or hall. These algorithms create the structural blueprint of a dungeon floor, establishing which rooms exist, where they're located, and how they connect.

## Files

| File | Description |
|------|-------------|
| `AddBossRoomStep.cs` | Attaches a boss room and connected vault to an existing layout |
| `GridPathBeetle.cs` | Creates a large central room with smaller rooms branching off like legs |
| `GridPathPyramid.cs` | Places a giant room with branching paths emanating from below it |
| `FloorStairsDistanceStep.cs` | Ensures stairs are placed at appropriate distances |
| `SetGridInnerComponentStep.cs` | Tags inner rooms with components for filtering |
| `IGridPathEdge.cs` | Interface for edge-based path generation |
| `IGridPathTreads.cs` | Interface for tread-pattern paths |

## Key Concepts

### Grid-Based Layout

The floor is divided into a grid where:
- Each cell can hold one room or be empty
- Adjacent cells can be connected by halls
- Rooms can span multiple cells (giant rooms)

### AddBossRoomStep

Creates boss encounters with reward rooms:

```csharp
BossRooms          // Room types for boss arena
TreasureRooms      // Room types for reward vault
GenericHalls       // Hall types connecting rooms
BossComponents     // Tags for boss room
VaultComponents    // Tags for treasure room
Filters            // Which existing rooms can have boss attached
```

The step:
1. Finds eligible room to branch from
2. Attaches boss room via hall
3. Attaches treasure room behind boss room
4. Labels rooms with components for later steps

### GridPathBeetle

Creates a distinctive "beetle" layout:
- One large central room (the body)
- Smaller rooms attached like legs
- Configurable orientation (vertical/horizontal)

Properties:
- `Vertical` - Orientation of the body
- `LegPercent` - Chance to add leg rooms
- `ConnectPercent` - Chance to connect adjacent legs
- `GiantHallGen` - Room generator for the body
- `LargeRoomComponents` - Tags for the central room

### GridPathPyramid

Creates a pyramid-like layout:
- Giant room placed in center region
- Connector room directly below
- Branching paths spread outward
- Corner rooms marked with special components

Properties:
- `GiantHallSize` - Size of central room in grid cells
- `RoomRatio` - Target percentage of grid to fill
- `BranchRatio` - How much the path branches
- `CornerRoomComponents` - Tags for outermost rooms

### SetGridInnerComponentStep

Tags rooms based on position:
- Identifies rooms in the interior of the grid
- Applies specified components
- Useful for excluding edge rooms from certain features

## Related

- [../Rooms/](../Rooms/) - Room shape generators
- [../](../) - Other generation steps
- [../../](../../) - Floor generation overview
