# Dungeon

Core dungeon gameplay systems for Pokemon Mystery Dungeon Clone (PMDC). This folder contains AI behavior logic and game effect systems that drive combat, status effects, and character behavior.

## Overview

The Dungeon module implements the runtime gameplay mechanics that occur during dungeon exploration. It builds on the RogueEssence engine to provide Pokemon-specific battle mechanics, AI behaviors, and status effect systems.

## Subfolders

- [AI/](./AI/README.md) - AI behavior plans that control NPC and ally decision-making
- [GameEffects/](./GameEffects/README.md) - Status effects, battle events, and state management systems

## Key Concepts

### AI System
- **AIPlan**: Base class for all AI behaviors. Each plan implements `Think()` which returns a `GameAction`.
- **AIFlags**: Bitflags controlling AI behavior (ItemGrabber, KnowsMatchups, TrapAvoider, etc.)
- **AttackChoice**: Enum for attack selection strategy (StandardAttack, DumbAttack, SmartAttack, etc.)

### GameEffects System
- **BattleEvent**: Coroutine-based events triggered during combat
- **State Classes**: Data containers for status effects, skills, items, and characters
- Uses `IEnumerator<YieldInstruction>` pattern for asynchronous game logic

## Related

- **RogueEssence.Dungeon**: Base dungeon framework this module extends
- **PMDC.Data**: Data definitions for moves, abilities, items
- **PMDC.LevelGen**: Procedural dungeon generation
