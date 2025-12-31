# Editors

Custom Avalonia property editors for PMDC game data types, providing specialized UI and display strings in the data editor.

## Overview

These editors extend `Editor<T>` to customize how specific types appear and are edited in the RogueEssence data editor. Most provide `GetString` for list display and `GetTypeString` for type selection dropdowns; some override `LoadWindowControls`/`SaveWindowControls` for custom UI.

## Files

| File | Description |
|------|-------------|
| FloorNameDropZoneStepEditor.cs | Display editor for `FloorNameDropZoneStep` showing floor name configuration |
| MapTilesEditor.cs | Text-based tile map editor using ASCII characters (`.#X~^_`) for floor/wall/water/lava/pit tiles |
| MobSpawnExtraEditor.cs | Collection of 14 editors for mob spawn features: stat boosts, items, shiny chance, level scaling, etc. |
| SaveVarsZoneStepEditor.cs | Display editor for `SaveVarsZoneStep` (rescue handling) |
| SkillStateEditor.cs | Editors for `BasePowerState` and `AdditionalEffectState` showing power/effect chance values |
| SpreadHouseZoneStepEditor.cs | Display editor for monster/item house generation showing house type |
| SpreadVaultZoneStepEditor.cs | Display editor for vault generation showing vault type |
| TeamMemberSpawnSimpleEditor.cs | Streamlined editor for `TeamMemberSpawn` using the custom MVVM view |

## Key Concepts

- **SimpleEditor**: Set `SimpleEditor => true` to use custom Avalonia controls instead of reflection-based editing
- **GetString/GetTypeString**: Override for custom display in property grids and type pickers
- **LoadWindowControls/SaveWindowControls**: Override for complete control over the edit UI

## MobSpawnExtra Editors

The `MobSpawnExtraEditor.cs` file contains specialized editors for spawn modifiers:

| Editor | Purpose |
|--------|---------|
| MobSpawnWeakEditor | Half PP, 35% belly indicator |
| MobSpawnAltColorEditor | Shiny spawn chance display |
| MobSpawnMovesOffEditor | Disabled move slots display |
| MobSpawnBoostEditor | Stat bonus summary |
| MobSpawnScaledBoostEditor | Level-scaled stat ranges |
| MobSpawnItemEditor | Held item display |
| MobSpawnInvEditor | Inventory contents |
| MobSpawnLevelScaleEditor | Floor-scaled level display |
| MobSpawnLocEditor | Position and orientation |
| MobSpawnUnrecruitableEditor | Unrecruitable flag |
| MobSpawnFoeConflictEditor | Aggressive behavior flag |
| MobSpawnInteractableEditor | Interaction events list |
| MobSpawnLuaTableEditor | Custom Lua script indicator |
| MobSpawnDiscriminatorEditor | Spawn discriminator ID |
| Intrinsic3ChanceEditor | Hidden ability roll flag |

## Related

- [../ViewModels/](../ViewModels/) - ViewModels used by TeamMemberSpawnSimpleEditor
- [../Views/](../Views/) - Avalonia views for custom editors
- [../../LevelGen/](../../LevelGen/) - Types like SpreadHouseZoneStep, SpreadVaultZoneStep
