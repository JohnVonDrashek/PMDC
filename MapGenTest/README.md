# MapGenTest

Console application for testing and debugging procedural dungeon map generation. Provides interactive browsing of zones/floors and stress testing capabilities for the PMDC/RogueEssence map generation system.

## Overview

MapGenTest allows developers to:
- Browse and visualize generated dungeon floors in the console
- Stress test map generation with configurable iteration counts
- Debug generation failures by inspecting specific seeds
- Step through the generation process to understand room/hall placement

## Files

| File | Description |
|------|-------------|
| `Program.cs` | Entry point; parses CLI args, initializes game data, runs tests |
| `ExampleDebug.cs` | Console visualization and step-through debugging for map generation |
| `ExpTester.cs` | Experience/leveling system tester |
| `DebugState.cs` | State tracking for debug visualization |
| `Example/Example.cs` | Interactive zone/floor browser and stress testing logic |

## Usage

```bash
# Basic run - opens interactive zone browser
dotnet run --project MapGenTest/MapGenTest.csproj

# With custom asset path
dotnet run --project MapGenTest/MapGenTest.csproj -- -asset ../path/to/assets

# With mod support
dotnet run --project MapGenTest/MapGenTest.csproj -- -quest QuestName -mod Mod1 Mod2

# Run experience tester instead
dotnet run --project MapGenTest/MapGenTest.csproj -- -exp
```

## CLI Arguments

| Argument | Description |
|----------|-------------|
| `-asset <path>` | Set custom asset path (relative to exe) |
| `-raw <path>` | Set custom dev/raw data path |
| `-quest <name>` | Load a quest mod from the mods folder |
| `-mod <names...>` | Load one or more mods (space-separated) |
| `-lua` | Enable Lua debugging |
| `-exp` | Run experience tester instead of map generator |
| `-expdir <path>` | Set experience test output directory |

## Interactive Controls

### Zone/Structure Selection
- `A-Z` - Select item by letter
- `Left/Right Arrow` - Browse pages (26 items per page)
- `F2` - Stress test (bulk generation)
- `ESC` - Exit/Go back

### Floor Viewer
- `Arrow Keys` - Navigate the map
- `Enter` - Generate new map with random seed
- `F2` - Stress test current floor
- `F3` - Enter custom seed
- `F4` - Step into generation (debug mode)
- `ESC` - Go back

### Map Legend

| Symbol | Meaning |
|--------|---------|
| `.` | Room floor |
| `,` | Hall floor |
| `#` | Wall |
| `X` | Unbreakable wall |
| `~` | Water |
| `^` | Lava |
| `_` | Abyss |
| `=` | Trap |
| `<` | Entrance |
| `>` | Exit |
| `*` | Money |
| `$` | Item |
| `!` | Throwable item |
| `/` | Orb/Wand |
| `;` | Food |
| `%` | TM/Utility |

## Related

- [Example/](./Example/) - Zone stress testing and interactive floor browsing
- [../PMDC/](../PMDC/) - Main game project
- [../RogueEssence/](../RogueEssence/) - Core engine (submodule)
