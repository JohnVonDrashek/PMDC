# UserControls

Avalonia user controls providing custom UI components for the data editor.

## Overview

These user controls are XAML-based Avalonia components paired with code-behind files. They provide specialized editing interfaces that integrate with the RogueEssence data editor framework.

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

## Related

- [../../ViewModels/TeamMemberSpawn/](../../ViewModels/TeamMemberSpawn/) - TeamMemberSpawnModel ViewModel
- [../../Editors/](../../Editors/) - TeamMemberSpawnSimpleEditor that loads this view
