# MultiTeamSpawner

Team spawners that create groups of monsters with coordinated composition. Used for boss encounters, rescue teams, and special enemy formations.

## Overview

Multi-team spawners generate complete teams rather than individual monsters. They can pull from floor spawn tables, apply leader-specific modifications, and create both monster teams and explorer teams.

## Key Concepts

### BossBandContextSpawner

Creates mini-boss teams using the floor's existing spawn pool:

```csharp
public class BossBandContextSpawner<T>
{
    RandRange TeamSize           // Total team members
    bool Explorer                // Explorer team vs monster team
    List<MobSpawnExtra> LeaderFeatures  // Extra features for leader
}
```

**Workflow:**
1. Flatten floor's team spawn table to individual mob spawns
2. Pick random mob as leader, apply `LeaderFeatures`
3. Pick remaining random mobs as subordinates
4. Create team (ExplorerTeam or MonsterTeam)
5. Spawn all members into team

**Example usage:**
```csharp
var spawner = new BossBandContextSpawner<ListMapGenContext>(new RandRange(2, 4));
spawner.LeaderFeatures.Add(new MobSpawnBoost { MaxHPBonus = 50, AtkBonus = 10 });
spawner.LeaderFeatures.Add(new MobSpawnItem(false, "item_sitrus_berry"));
```

### Team Types

**MonsterTeam:**
- Standard enemy team
- Hostile to player
- Can have FoeConflict for inter-enemy aggression

**ExplorerTeam:**
- Structured like player team
- Can hold items in inventory
- Used for rescue targets, rival teams

### Integration Points

Multi-team spawners integrate with:
- `PlaceRandomMobsStep` - Places spawned teams in rooms
- `SpreadVaultZoneStep` - Vault guardians
- `SpreadBossZoneStep` - Boss encounters

## Related

- [../MobSpawn/](../MobSpawn/) - Individual mob configuration
- [../](../) - Spawning overview
- [../../Zones/ZoneSteps/](../../Zones/ZoneSteps/) - Zone-level boss placement
