# UserControls

Avalonia user controls providing custom UI components for the data editor.

## Overview

These user controls are XAML-based Avalonia components paired with code-behind files. They provide specialized editing interfaces that integrate with the RogueEssence data editor framework.

## Files

| File | Description |
|------|-------------|
| TeamMemberSpawnView.axaml | XAML layout with search textboxes, data grids for monsters/skills/intrinsics, and spawn configuration controls |
| TeamMemberSpawnView.axaml.cs | Code-behind handling focus management, Enter-key navigation, data grid selection, and collection change events |

## Key Features

### TeamMemberSpawnView

- **Tabbed Input Flow**: Enter key advances through Species > Skill 1-4 > Intrinsic > Level Min/Max
- **Type-Ahead Search**: Text input filters data grids in real-time with fuzzy matching
- **Auto-Selection**: Pressing Enter or losing focus auto-selects the best match from filtered results
- **Multi-Select Skills**: Skill data grid supports multiple selection for viewing assigned skills
- **Dynamic Grid Switching**: Data grid visibility changes based on which input field has focus
- **Level Range Validation**: Min/Max NumericUpDown controls enforce Min <= Max constraint

### Focus Order

```
SpeciesTextBox -> SkillTextBox0 -> SkillTextBox1 -> SkillTextBox2 -> SkillTextBox3 -> IntrinsicTextBox -> MinTextBox -> MaxTextBox
```

## Event Handlers

| Handler | Purpose |
|---------|---------|
| `*_OnGotFocus` | Updates ViewModel focus index, triggers appropriate data grid filtering |
| `*_OnLostFocus` | Auto-selects matching item if search text matches top result |
| `*_OnEnterCommand` | Selects current match and advances to next input |
| `*DataGrid_OnCellPointerPressed` | Handles click selection in data grids |
| `*_OnValueChanged` | Enforces level range constraints |

## Related

- [../../ViewModels/TeamMemberSpawn/](../../ViewModels/TeamMemberSpawn/) - TeamMemberSpawnModel ViewModel
- [../../Editors/](../../Editors/) - TeamMemberSpawnSimpleEditor that loads this view
