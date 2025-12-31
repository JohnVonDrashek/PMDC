# EditorOps

Character sprite sheet transformation operations for batch-processing animation data in the sprite editor.

## Overview

These operations implement `CharSheetOp` from RogueEssence.Dev to provide automated transformations on character sprite sheets. Each operation can be applied to specific animation indices or all animations.

## Files

| File | Description |
|------|-------------|
| CharSheetAlignOp.cs | Standardizes sprite alignment to a consistent center offset (0, 4), ensuring uniform positioning across all directions |
| CharSheetCollapseOffsetsOp.cs | Optimizes sprite sheets by collapsing redundant offset data, reducing file size |
| CharSheetGenAnimOp.cs | Generates swing, double-hit, and spin animations from idle frames using predefined offset tables (anims 40-42) |
| CharSheetMirrorOp.cs | Mirrors animation frames horizontally (left-to-right or right-to-left) for symmetric sprites |

## Key Concepts

- **CharSheetOp**: Base class requiring `Name`, `Anims` (target animation indices), and `Apply(CharSheet, anim)` method
- **Offset Tables**: `CharSheetGenAnimOp` uses hardcoded 8-direction offset matrices for procedural animation
- **Collapse**: Optimizes sprite sheets after transformations by removing duplicate frame data

## Related

- [../](../) - Parent Dev directory with DevHelper and other utilities
- [../Editors/](../Editors/) - Custom property editors that may use sprite sheet data
