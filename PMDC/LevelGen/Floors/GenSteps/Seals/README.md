# Seals

Room sealing mechanics that lock existing rooms behind various unlock mechanisms. Sealed rooms typically contain valuable rewards and require players to find keys, defeat bosses, or activate switches.

## Overview

Seal steps take rooms marked with specific components and surround them with unbreakable walls, leaving only sealed doors as entry points. Different seal types use different unlock mechanisms, from consumable keys to boss defeats.

## Files

| File | Description |
|------|-------------|
| `BaseSealStep.cs` | Abstract base class handling border detection and wall placement |
| `BossSealStep.cs` | Seals vault rooms that unlock when boss is defeated |
| `KeySealStep.cs` | Seals rooms requiring a key item to unlock |
| `SwitchSealStep.cs` | Seals rooms requiring switch activation to unlock |
| `GuardSealStep.cs` | Seals rooms with guard enemies |
| `TerrainSealStep.cs` | Seals using terrain-based barriers |

## Key Concepts

### Seal Types

The base class categorizes border tiles:
- `Blocked` - Always becomes unbreakable wall
- `Locked` - Becomes sealed door tile
- `Key` - Becomes the primary unlock point

### BaseSealStep

Handles common sealing logic:
1. Find rooms matching filter criteria
2. Iterate room borders detecting entry points
3. Categorize each border tile by seal type
4. Call subclass to place specific tile types

Properties:
- `Filters` - Room filters identifying vault rooms

### BossSealStep

Locks vaults behind boss encounters:
```csharp
SealedTile   // Door tile blocking entry
BossTile     // Tile that triggers boss fight
BossFilters  // Filters to find boss room
```

Workflow:
1. Find boss room via `BossFilters`
2. Locate boss trigger tile in room
3. Place sealed doors around vault
4. Attach unlock event to boss tile
5. Defeating boss opens all sealed doors

### KeySealStep

Requires consumable key item:
```csharp
LockedTile  // Sealed door tiles
KeyTile     // The door accepting key
KeyItem     // Item consumed to unlock
```

Workflow:
1. Surround vault with sealed doors
2. Choose one door as key tile
3. Other doors are locked tiles
4. Using key on key tile opens all doors

### SwitchSealStep

Requires switch activation:
```csharp
SealedTile     // Sealed door tiles
SwitchTile     // Switch that opens doors
Amount         // Number of switches to place
Revealed       // Whether switch is visible
TimeLimit      // Start timer on activation
SwitchFilters  // Where switches can be placed
```

Workflow:
1. Surround vault with sealed doors
2. Find valid switch locations via filters
3. Place specified number of switches
4. Activating all switches opens doors
5. Optional time limit adds urgency

### Border Detection Algorithm

The base class uses ray-casting to detect borders:
1. Cast rays outward from room edges
2. Check if adjacent tile is in connected room
3. If connected room passes filter, skip (same vault)
4. If connected room fails filter, mark as entry point
5. Handle corner cases with diagonal checks

## Related

- [../Detours/](../Detours/) - Creates new hidden rooms
- [../Rooms/](../Rooms/) - Room components for filtering
- [../](../) - Other generation steps
