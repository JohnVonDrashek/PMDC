# Example

Interactive zone browsing and stress testing functionality for map generation testing.

## Features

### Interactive Zone Browser
Hierarchical menu system for exploring generated maps:
1. **Zone Selection** - Browse all available zones (dungeons)
2. **Structure Selection** - Choose segment within a zone
3. **Floor Selection** - Pick specific floor number
4. **Map Viewer** - Navigate generated map with cursor, inspect tiles/items/monsters

### Stress Testing
Bulk generation tests at multiple granularities:
- **All Zones** (`F2` from zone list) - Generate every floor of every released zone N times
- **Single Zone** (`F2` from structure list) - Generate all floors/structures of one zone N times
- **Single Structure** (`F2` from floor input) - Generate all floors of one structure N times
- **Single Floor** (`F2` from map view) - Generate one floor N times

### Output Statistics
Stress tests report:
- **Terrain distribution** - Tile type percentages
- **Item spawns** - Item counts and money totals
- **Monster spawns** - Species distribution
- **Generation times** - Min/median/max per floor
- **Failing seeds** - Seeds that caused generation errors

## Registry Persistence

The tool uses Windows Registry to persist state across runs (for debugging specific scenarios):
- `ZoneChoice` - Last selected zone
- `StructChoice` - Last selected structure index
- `FloorChoice` - Last selected floor number
- `SeedChoice` - Last used zone seed
- `MapCountChoice` - Map count for seed calculation

## Related

- [../](../) - MapGenTest main directory
- [../ExampleDebug.cs](../ExampleDebug.cs) - Console visualization and step debugging
