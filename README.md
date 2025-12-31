# PMDC

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-lightgrey)](https://github.com/JohnVonDrashek/PMDC/releases)
[![License](https://img.shields.io/github/license/PMDCollab/PMDC)](LICENSE)
[![GitHub Release](https://img.shields.io/github/v/release/PMDCollab/PMDC)](https://github.com/PMDCollab/PMDC/releases)

A Mystery Dungeon fangame built on the [RogueEssence](https://github.com/audinowho/RogueEssence) engine. Features procedurally generated dungeons, turn-based tactical combat, and extensive modding support via Lua scripting.

## Features

- Roguelike dungeon exploration with procedural generation
- Turn-based tactical combat with abilities and items
- Avalonia-based editor for creating content
- Lua scripting for mods and custom content
- Cross-platform support (Windows, Linux, macOS)

---

## Repo Setup

1. Clone with submodules:
   ```bash
   git clone --recursive https://github.com/PMDCollab/PMDC.git
   ```

2. Or if already cloned:
   ```bash
   git submodule update --init --remote -- RogueEssence
   git submodule update --init --recursive
   ```

3. You may need to regenerate NuGet packages for the RogueEssence solution first, before building.

4. If you switch to or from the DotNetCore branch, remember to clear your `obj` folder.

## Building Game

```bash
# Windows x86
dotnet publish -c Release -r win-x86 PMDC/PMDC.csproj

# Windows x64
dotnet publish -c Release -r win-x64 PMDC/PMDC.csproj

# Linux
dotnet publish -c Release -r linux-x64 PMDC/PMDC.csproj

# macOS
dotnet publish -c Release -r osx-x64 PMDC/PMDC.csproj
```

Files will appear in the `publish` folder.

## Building Server

```bash
# Windows
dotnet publish -c Release -r win-x64 RogueEssence/WaypointServer/WaypointServer.csproj

# Linux
dotnet publish -c Release -r linux-x64 RogueEssence/WaypointServer/WaypointServer.csproj
```

---

![Repobeats analytics](https://repobeats.axiom.co/api/embed/your-hash-here.svg "Repobeats analytics image")
