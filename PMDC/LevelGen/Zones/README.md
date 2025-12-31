# Zones

Zone-level generation that coordinates features across multiple dungeon floors. Zones represent dungeon segments where related floors share theming and feature distribution.

## Overview

Zone steps operate at a higher level than floor steps, determining how features like boss encounters, vaults, and monster houses are distributed across a range of floors. This ensures consistent pacing and variety throughout a dungeon segment.

## Directory Structure

| Folder | Description |
|--------|-------------|
| [ZoneSteps/](./ZoneSteps/) | Zone step implementations for feature distribution |

## Key Concepts

### Zone Generation

Zones process floors in sequence:
1. Zone context tracks current floor ID
2. Zone steps decide which floors receive features
3. Floor generation steps are enqueued per-floor
4. Each floor generates independently with zone-configured steps

### SpreadPlan System

Zone steps use spread plans to distribute features:
- `SpreadPlanQuota` - Fixed number of occurrences
- `SpreadPlanSpaced` - Minimum spacing between occurrences
- `SpreadPlanRange` - Occurrences within floor ranges

### Zone Context

`ZoneGenContext` provides:
- `CurrentID` - Current floor being generated
- Access to zone-wide configuration
- Random state for consistent generation

### Feature Spreading

Common spread patterns:
- **Boss Battles** - Every N floors with minimum spacing
- **Monster Houses** - Random chance with quota
- **Vaults** - Distributed to avoid clustering

## Related

- [ZoneSteps/](./ZoneSteps/) - Zone step implementations
- [../Floors/](../Floors/) - Floor-level generation
- [../Spawning/](../Spawning/) - Entity spawning systems
