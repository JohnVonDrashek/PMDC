# TeamMemberSpawn ViewModels

ViewModels for the streamlined TeamMemberSpawn editor, enabling efficient monster spawn configuration with searchable dropdowns and data grids.

## Overview

These ViewModels power a custom editor UI that replaces the default reflection-based property editor with an optimized interface for configuring monster spawns. Features include type-ahead search, filtered data grids, and quick toggles for common spawn features.

## Key Concepts

- **Filtered Collections**: `FilteredMonsterForms`, `FilteredSkillData`, `FilteredIntrinsicData` update reactively based on search text
- **Search Priority**: Results sorted by exact match > prefix match > substring match, with learnable skills/intrinsics prioritized
- **Spawn Feature Toggles**: Quick checkboxes for common features (Unrecruitable, Weak, DisableUnusedSlots)
- **DataGridType Enum**: Tracks which data grid (Monster/Skills/Intrinsic) should be visible based on focused input

## Related

- [../../Views/UserControls/](../../Views/UserControls/) - TeamMemberSpawnView.axaml consuming this ViewModel
- [../../Editors/](../../Editors/) - TeamMemberSpawnSimpleEditor instantiating TeamMemberSpawnModel
- [../../../LevelGen/](../../../LevelGen/) - TeamMemberSpawn and MobSpawn types
