# Contributing to PMDC

First off, **thank you** for considering contributing! I truly believe in open source and the power of community collaboration. Unlike many repositories, I actively welcome contributions of all kinds - from bug fixes to new features.

## My Promise to Contributors

- **I will respond to every PR and issue** - I guarantee feedback on all contributions
- **Bug fixes are obvious accepts** - If it fixes a bug, it's getting merged
- **New features are welcome** - I'm genuinely open to new ideas and enhancements
- **Direct line of communication** - If I'm not responding to a PR or issue, email me directly at johnvondrashek@gmail.com

## Getting Started

1. **Fork the repository** and clone with submodules:
   ```bash
   git clone --recursive https://github.com/YOUR_USERNAME/PMDC.git
   ```

2. **Set up submodules** (if already cloned):
   ```bash
   git submodule update --init --remote -- RogueEssence
   git submodule update --init --recursive
   ```

3. **Build the project**:
   ```bash
   # Windows x64
   dotnet publish -c Release -r win-x64 PMDC/PMDC.csproj

   # Linux
   dotnet publish -c Release -r linux-x64 PMDC/PMDC.csproj

   # macOS
   dotnet publish -c Release -r osx-x64 PMDC/PMDC.csproj
   ```

## Project Structure

| Directory | Purpose |
|-----------|---------|
| `PMDC/` | Main game code - this is where most contributions will go |
| `PMDC/Dungeon/` | AI behaviors, battle events, status effects |
| `PMDC/LevelGen/` | Procedural dungeon generation |
| `MapGenTest/` | Map generation testing harness |
| `RogueEssence/` | Engine submodule - avoid editing directly |

## Code Style

- Follow C# conventions with `[Serializable]` attribute pattern for game data classes
- Include copy constructors and `Clone()` methods for serializable types
- Use coroutine-based patterns (`IEnumerator<YieldInstruction>`) for battle effects
- See `CLAUDE.md` and `docs/claude/conventions.md` for detailed patterns

## Types of Contributions

### Bug Fixes
Found a bug? Fix it and submit a PR. These are always welcome.

### New Features
- New dungeon mechanics or generation steps
- AI behavior improvements
- Quality of life enhancements
- Modding support expansions

### Documentation
- Improvements to existing docs
- Examples and tutorials
- Code comments

## Submitting Changes

1. Create a branch for your changes
2. Write clear commit messages
3. Open a Pull Request with a description of what you changed and why
4. Wait for feedback (I promise it will come!)

## Testing

Use MapGenTest to verify procedural generation changes:
```bash
dotnet run --project MapGenTest/MapGenTest.csproj
```

## Code of Conduct

This project follows the [Rule of St. Benedict](CODE_OF_CONDUCT.md) as its code of conduct.

## Questions?

- Open an issue
- Email: johnvondrashek@gmail.com
