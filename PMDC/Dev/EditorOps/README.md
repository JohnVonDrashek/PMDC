# EditorOps

Character sprite sheet transformation operations for batch-processing animation data in the sprite editor.

## Overview

These operations implement `CharSheetOp` from RogueEssence.Dev to provide automated transformations on character sprite sheets. Each operation can be applied to specific animation indices or all animations.

## Key Concepts

- **CharSheetOp**: Base class requiring `Name`, `Anims` (target animation indices), and `Apply(CharSheet, anim)` method
- **Offset Tables**: `CharSheetGenAnimOp` uses hardcoded 8-direction offset matrices for procedural animation
- **Collapse**: Optimizes sprite sheets after transformations by removing duplicate frame data

## Related

- [../](../) - Parent Dev directory with DevHelper and other utilities
- [../Editors/](../Editors/) - Custom property editors that may use sprite sheet data
