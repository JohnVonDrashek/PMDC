# Dev

Development tools and Avalonia editor components for PMDC game data authoring, serialization, and strategy guide generation.

## Overview

This directory contains editor infrastructure built on RogueEssence.Dev, including custom property editors for game data types, MVVM components for the Avalonia UI, and utilities for data serialization and documentation generation.

## Files

| File | Description |
|------|-------------|
| DevHelper.cs | Monster/move analysis utilities: finding species by abilities, tracking dungeon encounters, Lua script migration for mod upgrades |
| JsonConverters.cs | Custom JSON converters for `ItemFake` and `MonsterID` dictionary serialization |
| SerializerContractResolver.cs | Newtonsoft.Json contract resolver that respects `NonSerializedAttribute` on fields |
| StrategyGuide.cs | Generates HTML/CSV strategy guides for items, moves, abilities, and monster encounters |
| UpgradeBinder.cs | Serialization binder for handling type renames across PMDC versions (migration support) |

## Key Concepts

- **Editor Pattern**: Custom editors extend `Editor<T>` to provide display strings and Avalonia controls for specific types
- **CharSheetOp**: Operations that transform character sprite sheets (alignment, mirroring, animation generation)
- **Strategy Guides**: Automated documentation generation from game data with progress bars and HTML styling

## Directories

| Directory | Description |
|-----------|-------------|
| [EditorOps/](EditorOps/) | Character sprite sheet transformation operations |
| [Editors/](Editors/) | Custom Avalonia property editors for game data types |
| [ViewModels/](ViewModels/) | MVVM ViewModels for editor UI components |
| [Views/](Views/) | Avalonia XAML views and user controls |

## Related

- [../LevelGen/](../LevelGen/) - Level generation types edited by these editors
- [../Dungeon/](../Dungeon/) - Dungeon data types (MobSpawn, skills, etc.)
- [../Data/](../Data/) - Core data types (MonsterFormData, etc.)
