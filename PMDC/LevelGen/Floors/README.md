# Floors

Floor-level generation for individual dungeon floors. This module orchestrates room layout, special features, and floor-wide mechanics.

## Overview

Floor generation transforms a blank map into a complete dungeon floor with interconnected rooms, hallways, and special features. The process uses a room plan that defines spatial relationships between areas before rendering them to tiles.

## Key Concepts

### Floor Generation Order

1. **Grid/Room Planning** - Establish room positions and connections
2. **Room Drawing** - Render room shapes and terrain
3. **Hall Connection** - Connect rooms with hallways
4. **Feature Placement** - Add shops, chests, monster houses
5. **Sealing** - Lock special rooms behind keys/switches
6. **Spawning** - Place items and enemies

### ListMapGenContext

The primary context type for floor generation, providing:
- `RoomPlan` - Access to room/hall layout
- Room iteration and filtering
- Tile placement APIs
- Entity spawn tracking

## Related

- [GenSteps/](./GenSteps/) - All floor generation steps
- [../Zones/](../Zones/) - Zone-level orchestration
