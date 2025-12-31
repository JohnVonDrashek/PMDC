# Water

Water terrain generation using pattern-based placement. Creates lakes, rivers, and water features by loading pre-designed patterns and placing them on valid map locations.

## Overview

Water generation uses a pattern-based approach where pre-designed map files define water shapes. These patterns are then placed at random valid locations on the dungeon floor, allowing for consistent water features while maintaining randomization.

## Files

| File | Description |
|------|-------------|
| `PatternWaterStep.cs` | Places water patterns loaded from map files |
| `IPatternWaterStep.cs` | Interface defining pattern water step contract |

## Key Concepts

### PatternWaterStep

Creates water features by loading and placing map patterns:

```csharp
public class PatternWaterStep<T> : WaterStep<T>
{
    RandRange Amount              // Number of patterns to place
    SpawnList<string> Maps        // Map file paths to load
    IBlobStencil<T> BlobStencil   // Validates placement positions
}
```

**Workflow:**
1. Pick random number of patterns to place (from `Amount`)
2. For each pattern:
   - Select random map file from `Maps`
   - Load map (cached for reuse)
   - Optionally transpose for variation
   - Attempt placement at random positions
   - Use `BlobStencil` to validate position
   - Draw pattern if valid
3. Retry failed placements up to 30 times

### IPatternWaterStep Interface

Defines the contract for pattern-based water:
```csharp
public interface IPatternWaterStep : IWaterStep
{
    RandRange Amount { get; set; }
}
```

### Pattern Loading

Maps are loaded from the `Map/` data folder:
- Non-room terrain in pattern becomes water
- Room terrain is treated as empty space
- Patterns are cached during step execution

### Blob Stencils

`IBlobStencil<T>` validates entire pattern placement:
- `DefaultBlobStencil` - Basic bounds checking
- Custom stencils can enforce:
  - Distance from walls
  - Room boundaries
  - Other terrain requirements

### Terrain Stencils

Inherited from `WaterStep<T>`:
- `ITerrainStencil<T>` validates individual tiles
- Determines which tiles can become water
- Common stencil: only paint on room terrain

### Transposition

Patterns can be randomly transposed:
- 50% chance to flip orientation
- Adds variety without additional map files
- Size is adjusted accordingly

## Related

- [../](../) - Tile generation overview
- [../../Floors/GenSteps/Rooms/](../../Floors/GenSteps/Rooms/) - Room generators with water (RoomGenOasis, RoomGenWaterRing)
- [../../Zones/](../../Zones/) - Zone-level generation
