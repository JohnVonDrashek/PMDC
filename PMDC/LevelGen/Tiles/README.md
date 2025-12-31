# Tiles

Tile-level generation for terrain painting, effect tiles, and environmental features. This module handles post-room-generation tile modifications.

## Overview

Tile generation steps operate after rooms are drawn, adding environmental features like water, special effect tiles, and terrain modifications. These steps work at the individual tile level rather than room level.

## Key Concepts

### SetCompassStep

Orients already-placed compass tiles to point toward objectives:

```csharp
public class SetCompassStep<T>
{
    string CompassTile  // Tile ID used as compass
}
```

**Workflow:**
1. Find all compass tiles on the map
2. Find all eligible destination tiles (exits, special tiles)
3. Attach destination list to each compass tile's state
4. Compass will point toward nearest destination when used

**Integration:**
- Works with `CompassEvent` tile behavior
- Targets tiles specified in compass event's `EligibleTiles`
- Also includes map exits as destinations

### Tile Effect System

Effect tiles are placed via `IPlaceableGenContext<EffectTile>`:
- Tiles have IDs referencing `TileData` definitions
- Tiles can have state attached (`TileStates`)
- State enables dynamic behavior (destination lists, unlock requirements)

## Related

- [Water/](./Water/) - Water terrain generation
- [../Floors/GenSteps/](../Floors/GenSteps/) - Floor generation steps
- [../Zones/](../Zones/) - Zone-level generation
