# TeamMemberSpawn ViewModels

ViewModels for the streamlined TeamMemberSpawn editor, enabling efficient monster spawn configuration with searchable dropdowns and data grids.

## Overview

These ViewModels power a custom editor UI that replaces the default reflection-based property editor with an optimized interface for configuring monster spawns. Features include type-ahead search, filtered data grids, and quick toggles for common spawn features.

## Files

| File | Description |
|------|-------------|
| BaseMonsterFormViewModel.cs | Exposes monster form data (name, elements, intrinsics, base stats, join rate) for display in selection grids |
| IntrinsicViewModel.cs | Wraps intrinsic/ability data with release status and whether the selected monster can learn it |
| SkillDataViewModel.cs | Wraps skill data (element, category, power, accuracy, charges) with monster-learnable filtering |
| TeamMemberSpawnModel.cs | Main ViewModel orchestrating monster/skill/intrinsic selection, spawn features, and data grid filtering |

## Key Concepts

- **Filtered Collections**: `FilteredMonsterForms`, `FilteredSkillData`, `FilteredIntrinsicData` update reactively based on search text
- **Search Priority**: Results sorted by exact match > prefix match > substring match, with learnable skills/intrinsics prioritized
- **Spawn Feature Toggles**: Quick checkboxes for common features (Unrecruitable, Weak, DisableUnusedSlots)
- **DataGridType Enum**: Tracks which data grid (Monster/Skills/Intrinsic) should be visible based on focused input

## TeamMemberSpawnModel Properties

| Property | Description |
|----------|-------------|
| TeamSpawn | The underlying `TeamMemberSpawn` being edited |
| SelectedMonsterForm | Currently selected monster form |
| SelectedIntrinsic | Currently selected ability override |
| SkillIndex | List of 4 selected skill indices (-1 for unset) |
| Min/Max | Level range for the spawn |
| SpawnConditions | CollectionBoxViewModel for `MobSpawnCheck` list |
| SpawnFeatures | CollectionBoxViewModel for `MobSpawnExtra` list |

## Related

- [../../Views/UserControls/](../../Views/UserControls/) - TeamMemberSpawnView.axaml consuming this ViewModel
- [../../Editors/](../../Editors/) - TeamMemberSpawnSimpleEditor instantiating TeamMemberSpawnModel
- [../../../LevelGen/](../../../LevelGen/) - TeamMemberSpawn and MobSpawn types
