# Detours

Hidden room generation that creates secret areas accessible only through specific unlock mechanisms. Detour rooms branch off from the main path and contain treasures guarded by enemies.

## Overview

Detours add optional challenge rooms to dungeon floors. These rooms are carved into walls adjacent to the main path and sealed with locked doors. Players must find keys or activate switches to access the treasures within.

## Key Concepts

### Detour Architecture

Each detour consists of:
1. **Sealed Door** - Placed at the entrance, requires unlock mechanism
2. **Tunnel** - Unbreakable walls connecting door to room
3. **Secret Room** - Contains treasures, tiles, and guards
4. **Unlock Mechanism** - Key item or switch tile

### BaseDetourStep Properties

```csharp
BulkSpawner<T, MapItem> Treasures      // Items in the secret room
BulkSpawner<T, EffectTile> TileTreasures  // Special tiles (exits, etc.)
BulkSpawner<T, MobSpawn> GuardTypes    // Enemies guarding the room
RandRange HallLength                    // Length of connecting tunnel
SpawnList<RoomGen<T>> GenericRooms     // Possible room shapes
```

### KeyDetourStep

Unlocked by using a specific key item:
- `LockedTile` - The door tile blocking entry
- `KeyItem` - Item consumed to unlock

### SwitchDetourStep

Unlocked by pressing switch tiles:
- `SealedTile` - The door tile blocking entry
- `SwitchTile` - The switch that opens the door
- `TimeLimit` - Whether a timer starts when switch is pressed
- `EntranceCount` - Number of sealed rooms per switch

### Room Placement Algorithm

1. Detect valid wall positions adjacent to walkable tiles
2. Select random wall ray from entrance location
3. Verify hall can be carved without hitting unbreakable terrain
4. Generate room shape and verify placement bounds
5. Carve tunnel with unbreakable walls on sides
6. Draw room and surround with unbreakable walls
7. Place sealing tile at entrance
8. Spawn treasures and guards in room

## Related

- [../Seals/](../Seals/) - Room sealing for existing rooms
- [../](../) - Other generation steps
- [../../](../../) - Floor generation overview
