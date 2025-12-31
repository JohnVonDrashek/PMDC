# ZoneSteps

Zone step implementations that distribute features across dungeon floors. These steps determine which floors receive special content and configure floor-level generation accordingly.

## Overview

Zone steps inherit from `SpreadZoneStep` which provides a spread plan for feature distribution. Each step defines what floor-level generation steps to add when a floor is selected for the feature.

## Files

| File | Description |
|------|-------------|
| `SpreadBossZoneStep.cs` | Distributes boss battles with reward vaults across floors |
| `SpreadVaultZoneStep.cs` | Distributes sealed vaults with items, tiles, and mobs |
| `SpreadHouseZoneStep.cs` | Distributes monster houses (standard, chest, or hall variants) |
| `DetourSpreadZoneStep.cs` | Distributes hidden detour rooms |
| `FloorNameDropZoneStep.cs` | Configures floor naming/theming |
| `SaveVarsZoneStep.cs` | Sets save variables on specific floors |

## Key Concepts

### SpreadBossZoneStep

Generates boss encounters with treasures:

```csharp
Priority BossRoomPriority          // When to add boss room
Priority RewardPriority            // When to add rewards
List<IGenPriority> VaultSteps      // Additional vault setup steps
SpawnRangeList<AddBossRoomStep> BossSteps  // Boss room configurations
SpawnRangeList<MapItem> Items      // Reward items
RangeDict<RandRange> ItemAmount    // How many items
```

**Workflow:**
1. Spread plan selects floors for bosses
2. Pick boss room step from `BossSteps`
3. Enqueue boss room step at `BossRoomPriority`
4. Enqueue vault steps
5. Configure reward items at `RewardPriority`

### SpreadVaultZoneStep

Generates sealed vaults with various content:

```csharp
Priority ItemPriority              // Item placement priority
Priority TilePriority              // Tile placement priority
Priority MobPriority               // Mob placement priority
List<IGenPriority> VaultSteps      // Vault room generation steps
SpawnRangeList<MapItem> Items      // Vault items
SpawnRangeList<MobSpawn> Mobs      // Vault guardians
RangeDict<IStepSpawner> ItemSpawners   // Custom item spawners
RangeDict<IStepSpawner> TileSpawners   // Custom tile spawners
```

**Features:**
- Separate priorities for items, tiles, mobs
- Configurable spawners per floor range
- Optional guardian mobs
- Flexible vault room types via `VaultSteps`

### SpreadHouseZoneStep

Generates monster houses across floors:

```csharp
Priority Priority                  // When to run house step
SpawnRangeList<MapItem> Items      // House items
SpawnRangeList<ItemTheme> ItemThemes  // Item filtering themes
SpawnRangeList<MobSpawn> Mobs      // House monsters
SpawnRangeList<MobTheme> MobThemes // Mob filtering themes
SpawnList<IMonsterHouseBaseStep> HouseStepSpawns  // House types
```

**House Types:**
- `MonsterHouseStep` - Standard room house
- `ChestStep` - Trapped chest variant
- `MonsterHallStep` - Hallway variant

**Workflow:**
1. Spread plan selects floors for houses
2. Pick house type from `HouseStepSpawns`
3. Populate house step with items/mobs from range lists
4. Apply themes to filter spawns
5. Enqueue configured house step

### Range-Based Configuration

Many properties use `SpawnRangeList` or `RangeDict`:
- Different configurations for floor ranges
- Enables progression (harder content on later floors)
- `[RangeBorder(0, true, true)]` attribute marks range properties

### Priority System

Priorities control step execution order:
- Lower priority numbers run first
- Critical for dependencies (rooms before sealing)
- Same priority steps run in queue order

## Related

- [../](../) - Zone generation overview
- [../../Floors/GenSteps/](../../Floors/GenSteps/) - Floor generation steps
- [../../Spawning/](../../Spawning/) - Entity spawning
