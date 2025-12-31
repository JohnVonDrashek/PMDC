# Dev

Development tools and Avalonia editor components for PMDC game data authoring, serialization, and strategy guide generation.

## Overview

This directory contains editor infrastructure built on RogueEssence.Dev, including custom property editors for game data types, MVVM components for the Avalonia UI, and utilities for data serialization and documentation generation.

## Key Concepts

- **Editor Pattern**: Custom editors extend `Editor<T>` to provide display strings and Avalonia controls for specific types
- **CharSheetOp**: Operations that transform character sprite sheets (alignment, mirroring, animation generation)
- **Strategy Guides**: Automated documentation generation from game data with progress bars and HTML styling

## Related

- [../LevelGen/](../LevelGen/) - Level generation types edited by these editors
- [../Dungeon/](../Dungeon/) - Dungeon data types (MobSpawn, skills, etc.)
- [../Data/](../Data/) - Core data types (MonsterFormData, etc.)
