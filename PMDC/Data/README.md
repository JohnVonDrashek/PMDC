# Data

Game data models for monsters, characters, and related systems. Extends RogueEssence's base data structures with Pokemon Mystery Dungeon-specific functionality.

## Overview

This folder contains data classes that define monster statistics, evolution mechanics, rarity tiers, and universal effect systems. The core class `MonsterFormData` extends RogueEssence's `BaseMonsterForm` to provide Pokemon-style stat calculations, gender weights, and skill learning categories.

## Key Concepts

### Stat System
- Six base stats: HP, Attack, Defense, Special Attack (MAtk), Special Defense (MDef), Speed
- `MAX_STAT_BOOST` constant (256) defines maximum stat bonus
- Stat calculations scale with level using `genericStatCalc()` and `hpStatCalc()` formulas
- Max stats are scaled proportionally to total base stat distribution

### Gender Weights
- Three weight properties: `GenderlessWeight`, `MaleWeight`, `FemaleWeight`
- `RollGender()` randomly selects gender based on weighted distribution
- `GetPossibleGenders()` returns list of rollable genders

### Skill Learning Categories
- `LevelSkills` - Moves learned by leveling up (inherited from `BaseMonsterForm`)
- `TeachSkills` - TM/HM moves
- `SharedSkills` - Egg moves
- `SecretSkills` - Tutor moves
- `CanLearnSkill()` checks all four categories

### Evolution Stages
```csharp
public enum EvoFlag
{
    None = 0,
    NoEvo = 1,      // Cannot evolve
    FirstEvo = 2,   // Base form, can evolve
    FinalEvo = 4,   // Fully evolved
    MidEvo = 8,     // Middle evolution
    All = 15
}
```

### Evolution Requirements (PromoteDetail subclasses)
- `EvoLevel` - Level threshold
- `EvoItem` - Held/inventory item (consumed on evolution)
- `EvoFriendship` - Number of evolved allies required
- `EvoTime` - Time of day condition
- `EvoWeather` - Active map weather status
- `EvoStats` - Attack vs Defense comparison
- `EvoMove` / `EvoMoveElement` - Known move requirements
- `EvoGender` / `EvoForm` - Hard requirements (non-displayable)
- `EvoLocation` - Map element type
- `EvoPartner` / `EvoPartnerElement` - Team composition
- `EvoShed` - Special evolution that spawns additional team member (Shedinja-style)

## Related

- [`../Dungeon/`](../Dungeon/) - Dungeon gameplay mechanics, battle events, and status effects referenced by these data models
- RogueEssence `BaseMonsterForm` - Parent class for `MonsterFormData`
- RogueEssence `DataManager` - Data loading and indexing system used by feature/rarity data
