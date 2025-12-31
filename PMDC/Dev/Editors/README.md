# Editors

Custom Avalonia property editors for PMDC game data types, providing specialized UI and display strings in the data editor.

## Overview

These editors extend `Editor<T>` to customize how specific types appear and are edited in the RogueEssence data editor. Most provide `GetString` for list display and `GetTypeString` for type selection dropdowns; some override `LoadWindowControls`/`SaveWindowControls` for custom UI.

## Key Concepts

- **SimpleEditor**: Set `SimpleEditor => true` to use custom Avalonia controls instead of reflection-based editing
- **GetString/GetTypeString**: Override for custom display in property grids and type pickers
- **LoadWindowControls/SaveWindowControls**: Override for complete control over the edit UI

## Related

- [../ViewModels/](../ViewModels/) - ViewModels used by TeamMemberSpawnSimpleEditor
- [../Views/](../Views/) - Avalonia views for custom editors
- [../../LevelGen/](../../LevelGen/) - Types like SpreadHouseZoneStep, SpreadVaultZoneStep
