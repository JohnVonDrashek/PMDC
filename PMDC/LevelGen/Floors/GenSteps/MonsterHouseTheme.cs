using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.Dungeon;
using RogueEssence.Dev;
using RogueEssence;
using RogueEssence.LevelGen;
using RogueEssence.Data;
using PMDC.Data;
using Newtonsoft.Json;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Abstract base class for item themes used in monster houses and special rooms.
    /// Item themes define how items are selected and generated for placement in themed areas.
    /// </summary>
    [Serializable]
    public abstract class ItemTheme
    {
        /// <summary>
        /// The number of items to generate.
        /// </summary>
        public RandRange Amount;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public ItemTheme() { }

        /// <summary>
        /// Initializes a new instance with the specified item count range.
        /// </summary>
        /// <param name="amount">The range of items to generate.</param>
        public ItemTheme(RandRange amount) { Amount = amount; }

        /// <summary>
        /// Copy constructor for cloning.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected ItemTheme(ItemTheme other) { Amount = other.Amount; }

        /// <summary>
        /// Creates a copy of this item theme.
        /// </summary>
        /// <returns>A new instance that is a copy of this theme.</returns>
        public abstract ItemTheme Copy();

        /// <summary>
        /// Generates a list of items based on this theme's criteria.
        /// </summary>
        /// <param name="map">The map generation context.</param>
        /// <param name="specialItems">Additional special items available for spawning.</param>
        /// <returns>A list of items to place in the themed area.</returns>
        public abstract List<MapItem> GenerateItems(BaseMapGenContext map, SpawnList<MapItem> specialItems);
    }

    /// <summary>
    /// An item theme that combines multiple item themes, generating items from each.
    /// </summary>
    [Serializable]
    public class ItemThemeMultiple : ItemTheme
    {
        /// <summary>
        /// The list of item themes to combine.
        /// </summary>
        public List<ItemTheme> Themes;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public ItemThemeMultiple() { Themes = new List<ItemTheme>(); }

        /// <summary>
        /// Initializes a new instance with the specified themes.
        /// </summary>
        /// <param name="themes">The themes to combine.</param>
        public ItemThemeMultiple(params ItemTheme[] themes)
        {
            Themes = new List<ItemTheme>();
            Themes.AddRange(themes);
        }

        /// <summary>
        /// Copy constructor for cloning.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected ItemThemeMultiple(ItemThemeMultiple other) : base(other)
        {
            Themes = other.Themes;
        }

        /// <inheritdoc/>
        public override ItemTheme Copy() { return new ItemThemeMultiple(this); }

        /// <inheritdoc/>
        public override List<MapItem> GenerateItems(BaseMapGenContext map, SpawnList<MapItem> specialItems)
        {
            List<MapItem> spawners = new List<MapItem>();

            foreach (ItemTheme theme in Themes)
            {
                List<MapItem> items = theme.GenerateItems(map, specialItems);
                spawners.AddRange(items);
            }

            return spawners;
        }
    }


    /// <summary>
    /// An item theme with no specific filtering, generating random items from map spawns or special items.
    /// </summary>
    [Serializable]
    public class ItemThemeNone : ItemTheme
    {
        /// <summary>
        /// The percentage chance (0-100) to pick from special items instead of map spawns.
        /// </summary>
        public int SpecialRatio;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public ItemThemeNone() { }

        /// <summary>
        /// Initializes a new instance with the specified special ratio and amount.
        /// </summary>
        /// <param name="specialRatio">The percentage chance to use special items.</param>
        /// <param name="amount">The range of items to generate.</param>
        public ItemThemeNone(int specialRatio, RandRange amount) : base(amount)
        {
            SpecialRatio = specialRatio;
        }

        /// <summary>
        /// Copy constructor for cloning.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected ItemThemeNone(ItemThemeNone other) : base(other)
        {
            SpecialRatio = other.SpecialRatio;
        }

        /// <inheritdoc/>
        public override ItemTheme Copy() { return new ItemThemeNone(this); }

        /// <inheritdoc/>
        public override List<MapItem> GenerateItems(BaseMapGenContext map, SpawnList<MapItem> specialItems)
        {
            int itemCount = Amount.Pick(map.Rand);
            List<MapItem> spawners = new List<MapItem>();

            for (int ii = 0; ii < itemCount; ii++)
            {
                if (specialItems.Count > 0 && map.Rand.Next(100) < SpecialRatio)
                    spawners.Add(specialItems.Pick(map.Rand));
                else if (map.ItemSpawns.CanPick)
                    spawners.Add(new MapItem(map.ItemSpawns.Pick(map.Rand)));
            }

            return spawners;
        }
    }

    /// <summary>
    /// An item theme that filters items by their usage type (equipment, consumable, etc.).
    /// </summary>
    [Serializable]
    public class ItemThemeType : ItemTheme
    {
        /// <summary>
        /// The item usage type to filter by.
        /// </summary>
        public ItemData.UseType UseType;

        /// <summary>
        /// Whether to include items from the map's default item spawns.
        /// </summary>
        public bool UseMapItems;

        /// <summary>
        /// Whether to include items from the special items list.
        /// </summary>
        public bool UseSpecialItems;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public ItemThemeType() { }

        /// <summary>
        /// Initializes a new instance with the specified usage type and sources.
        /// </summary>
        /// <param name="useType">The item usage type to filter by.</param>
        /// <param name="mapItems">Whether to include map spawn items.</param>
        /// <param name="specialItems">Whether to include special items.</param>
        /// <param name="amount">The range of items to generate.</param>
        public ItemThemeType(ItemData.UseType useType, bool mapItems, bool specialItems, RandRange amount) : base(amount)
        {
            UseType = useType;
            UseMapItems = mapItems;
            UseSpecialItems = specialItems;
        }

        /// <summary>
        /// Copy constructor for cloning.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected ItemThemeType(ItemThemeType other) : base(other)
        {
            UseType = other.UseType;
            UseMapItems = other.UseMapItems;
            UseSpecialItems = other.UseSpecialItems;
        }

        /// <inheritdoc/>
        public override ItemTheme Copy() { return new ItemThemeType(this); }

        /// <inheritdoc/>
        public override List<MapItem> GenerateItems(BaseMapGenContext map, SpawnList<MapItem> specialItems)
        {
            int itemCount = Amount.Pick(map.Rand);
            List<MapItem> spawners = new List<MapItem>();

            SpawnList<MapItem> subList = new SpawnList<MapItem>();
            if (UseSpecialItems)
            {
                for (int ii = 0; ii < specialItems.Count; ii++)
                {
                    MapItem spawn = specialItems.GetSpawn(ii);
                    if (!spawn.IsMoney)
                    {
                        ItemEntrySummary itemEntry = DataManager.Instance.DataIndices[DataManager.DataType.Item].Get(spawn.Value) as ItemEntrySummary;

                        if (itemEntry.UsageType == UseType)
                            subList.Add(spawn, specialItems.GetSpawnRate(ii));
                    }
                }
            }

            if (UseMapItems)
            {
                foreach (string key in map.ItemSpawns.Spawns.GetKeys())
                {
                    SpawnList<InvItem> spawns = map.ItemSpawns.Spawns.GetSpawn(key);
                    for (int ii = 0; ii < spawns.Count; ii++)
                    {
                        //TODO: spawn rate is somewhat distorted here
                        InvItem spawn = spawns.GetSpawn(ii);
                        ItemEntrySummary itemEntry = DataManager.Instance.DataIndices[DataManager.DataType.Item].Get(spawn.ID) as ItemEntrySummary;
                        if (itemEntry.UsageType == UseType)
                            subList.Add(new MapItem(spawn), spawns.GetSpawnRate(ii));
                    }
                }
            }

            if (subList.Count == 0)
                return spawners;

            for (int ii = 0; ii < itemCount; ii++)
                spawners.Add(subList.Pick(map.Rand));

            return spawners;
        }
    }

    /// <summary>
    /// An item theme that filters items by the types of item states they contain.
    /// </summary>
    [Serializable]
    public class ItemStateType : ItemTheme
    {
        /// <summary>
        /// The item state type to filter by.
        /// </summary>
        [StringTypeConstraint(0, typeof(ItemState))]
        public FlagType UseType;

        /// <summary>
        /// Whether to include items from the map's default item spawns.
        /// </summary>
        public bool UseMapItems;

        /// <summary>
        /// Whether to include items from the special items list.
        /// </summary>
        public bool UseSpecialItems;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public ItemStateType() { }

        /// <summary>
        /// Initializes a new instance with the specified state type and sources.
        /// </summary>
        /// <param name="useType">The item state type to filter by.</param>
        /// <param name="mapItems">Whether to include map spawn items.</param>
        /// <param name="specialItems">Whether to include special items.</param>
        /// <param name="amount">The range of items to generate.</param>
        public ItemStateType(FlagType useType, bool mapItems, bool specialItems, RandRange amount) : base(amount)
        {
            UseType = useType;
            UseMapItems = mapItems;
            UseSpecialItems = specialItems;
        }

        /// <summary>
        /// Copy constructor for cloning.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected ItemStateType(ItemStateType other) : base(other)
        {
            UseType = other.UseType;
            UseMapItems = other.UseMapItems;
            UseSpecialItems = other.UseSpecialItems;
        }

        /// <inheritdoc/>
        public override ItemTheme Copy() { return new ItemStateType(this); }

        /// <inheritdoc/>
        public override List<MapItem> GenerateItems(BaseMapGenContext map, SpawnList<MapItem> specialItems)
        {
            int itemCount = Amount.Pick(map.Rand);
            List<MapItem> spawners = new List<MapItem>();

            SpawnList<MapItem> subList = new SpawnList<MapItem>();
            if (UseSpecialItems)
            {
                for (int ii = 0; ii < specialItems.Count; ii++)
                {
                    MapItem spawn = specialItems.GetSpawn(ii);
                    if (!spawn.IsMoney)
                    {
                        ItemEntrySummary itemEntry = DataManager.Instance.DataIndices[DataManager.DataType.Item].Get(spawn.Value) as ItemEntrySummary;
                        if (itemEntry.ContainsState(UseType.FullType))
                            subList.Add(spawn, specialItems.GetSpawnRate(ii));
                    }
                }
            }

            if (UseMapItems)
            {
                foreach (string key in map.ItemSpawns.Spawns.GetKeys())
                {
                    SpawnList<InvItem> spawns = map.ItemSpawns.Spawns.GetSpawn(key);
                    for (int ii = 0; ii < spawns.Count; ii++)
                    {
                        //TODO: spawn rate is somewhat distorted here
                        InvItem spawn = spawns.GetSpawn(ii);
                        ItemEntrySummary itemEntry = DataManager.Instance.DataIndices[DataManager.DataType.Item].Get(spawn.ID) as ItemEntrySummary;
                        if (itemEntry.ContainsState(UseType.FullType))
                            subList.Add(new MapItem(spawn), spawns.GetSpawnRate(ii));
                    }
                }
            }

            if (subList.Count == 0)
                return spawners;

            for (int ii = 0; ii < itemCount; ii++)
                spawners.Add(subList.Pick(map.Rand));

            return spawners;
        }
    }


    /// <summary>
    /// Abstract base class for mob themes used in monster houses and special rooms.
    /// Mob themes define how monsters are selected and generated for themed areas.
    /// </summary>
    [Serializable]
    public abstract class MobTheme
    {
        /// <summary>
        /// The number of mobs to generate.
        /// </summary>
        public RandRange Amount;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MobTheme() { }

        /// <summary>
        /// Initializes a new instance with the specified mob count range.
        /// </summary>
        /// <param name="amount">The range of mobs to generate.</param>
        public MobTheme(RandRange amount) { Amount = amount; }

        /// <summary>
        /// Copy constructor for cloning.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected MobTheme(MobTheme other) { Amount = other.Amount; }

        /// <summary>
        /// Creates a copy of this mob theme.
        /// </summary>
        /// <returns>A new instance that is a copy of this theme.</returns>
        public abstract MobTheme Copy();

        /// <summary>
        /// Generates a list of mobs based on this theme's criteria.
        /// </summary>
        /// <param name="map">The map generation context.</param>
        /// <param name="specialMobs">Additional special mobs available for spawning.</param>
        /// <returns>A list of mobs to place in the themed area.</returns>
        public abstract List<MobSpawn> GenerateMobs(BaseMapGenContext map, SpawnList<MobSpawn> specialMobs);

        /// <summary>
        /// Gets a seed character from the map's team spawns or special mobs to base theme selection on.
        /// </summary>
        /// <param name="map">The map generation context.</param>
        /// <param name="specialMobs">Additional special mobs available for spawning.</param>
        /// <returns>A seed mob spawn to derive theme characteristics from, or null if none available.</returns>
        protected MobSpawn GetSeedChar(BaseMapGenContext map, SpawnList<MobSpawn> specialMobs)
        {
            //the contents of that theme can be selected randomly,
            MobSpawn seedSpawn = null;
            //or, to add some sensibility, make it seeded from a random spawn that can already be found in the map
            if (map.TeamSpawns.CanPick)
            {
                TeamSpawner spawn = map.TeamSpawns.Pick(map.Rand);
                if (spawn != null)
                {
                    List<MobSpawn> exampleList = spawn.ChooseSpawns(map.Rand);
                    if (exampleList.Count > 0)
                        seedSpawn = exampleList[map.Rand.Next(exampleList.Count)];
                }
            }
            //choose the spawn, then seed the theme with it
            //the theme will take the aspects of the seedspawn and then be ready to spit out a list
            if (seedSpawn == null && specialMobs.CanPick)
            {
                seedSpawn = specialMobs.Pick(map.Rand);
            }
            return seedSpawn;
        }
    }



    /// <summary>
    /// A mob theme with no specific filtering, generating random mobs from map spawns or special mobs.
    /// </summary>
    [Serializable]
    public class MobThemeNone : MobTheme
    {
        /// <summary>
        /// The percentage chance (0-100) to pick from special mobs instead of map spawns.
        /// </summary>
        public int SpecialRatio;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MobThemeNone() { }

        /// <summary>
        /// Initializes a new instance with the specified special ratio and amount.
        /// </summary>
        /// <param name="specialRatio">The percentage chance to use special mobs.</param>
        /// <param name="amount">The range of mobs to generate.</param>
        public MobThemeNone(int specialRatio, RandRange amount) : base(amount)
        {
            SpecialRatio = specialRatio;
        }

        /// <summary>
        /// Copy constructor for cloning.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected MobThemeNone(MobThemeNone other) : base(other)
        {
            SpecialRatio = other.SpecialRatio;
        }

        /// <inheritdoc/>
        public override MobTheme Copy() { return new MobThemeNone(this); }

        /// <inheritdoc/>
        public override List<MobSpawn> GenerateMobs(BaseMapGenContext map, SpawnList<MobSpawn> specialMobs)
        {
            int mobCount = Amount.Pick(map.Rand);
            List<MobSpawn> spawners = new List<MobSpawn>();

            for (int ii = 0; ii < mobCount; ii++)
            {
                if (specialMobs.Count > 0 && map.Rand.Next(100) < SpecialRatio)
                    spawners.Add(specialMobs.Pick(map.Rand));
                else if (map.TeamSpawns.CanPick)
                {
                    List<MobSpawn> exampleList = map.TeamSpawns.Pick(map.Rand).ChooseSpawns(map.Rand);
                    if (exampleList.Count > 0)
                        spawners.Add(exampleList[map.Rand.Next(exampleList.Count)]);
                }
            }

            return spawners;
        }
    }




    /// <summary>
    /// An item theme that directly spawns items from a specified list without filtering.
    /// </summary>
    [Serializable]
    public class ItemThemeDirect : ItemTheme
    {
        /// <summary>
        /// The list of items to choose from directly.
        /// </summary>
        public SpawnList<MapItem> DirectList;

        /// <summary>
        /// Initializes a new instance with an empty direct list.
        /// </summary>
        public ItemThemeDirect() { DirectList = new SpawnList<MapItem>(); }

        /// <summary>
        /// Initializes a new instance with the specified direct list.
        /// </summary>
        /// <param name="amount">The range of items to generate.</param>
        /// <param name="list">The list of items to choose from.</param>
        public ItemThemeDirect(RandRange amount, SpawnList<MapItem> list) : base(amount)
        {
            DirectList = list;
        }

        /// <summary>
        /// Copy constructor for cloning.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected ItemThemeDirect(ItemThemeDirect other) : base(other)
        {
            DirectList = other.DirectList;
        }

        /// <inheritdoc/>
        public override ItemTheme Copy() { return new ItemThemeDirect(this); }

        /// <inheritdoc/>
        public override List<MapItem> GenerateItems(BaseMapGenContext map, SpawnList<MapItem> specialItems)
        {
            int itemCount = Amount.Pick(map.Rand);
            List<MapItem> spawners = new List<MapItem>();

            SpawnList<MapItem> subList = new SpawnList<MapItem>();

            for (int ii = 0; ii < DirectList.Count; ii++)
            {
                MapItem spawn = DirectList.GetSpawn(ii);
                subList.Add(spawn, specialItems.GetSpawnRate(ii));
            }

            if (subList.Count == 0)
                return spawners;

            for (int ii = 0; ii < itemCount; ii++)
                spawners.Add(subList.Pick(map.Rand));

            return spawners;
        }
    }


    /// <summary>
    /// An item theme that filters items by a specified range of item IDs.
    /// </summary>
    [Serializable]
    public class ItemThemeRange : ItemTheme
    {
        /// <summary>
        /// The list of item IDs to include in this theme.
        /// </summary>
        [JsonConverter(typeof(ItemRangeToListConverter))]
        [DataType(0, DataManager.DataType.Item, false)]
        public List<string> Range;

        /// <summary>
        /// Whether to include items from the map's default item spawns.
        /// </summary>
        public bool UseMapItems;

        /// <summary>
        /// Whether to include items from the special items list.
        /// </summary>
        public bool UseSpecialItems;

        /// <summary>
        /// Initializes a new instance with an empty range.
        /// </summary>
        public ItemThemeRange() { Range = new List<string>(); }

        /// <summary>
        /// Initializes a new instance with the specified item range and sources.
        /// </summary>
        /// <param name="mapItems">Whether to include map spawn items.</param>
        /// <param name="specialItems">Whether to include special items.</param>
        /// <param name="amount">The range of items to generate.</param>
        /// <param name="range">The item IDs to include.</param>
        public ItemThemeRange(bool mapItems, bool specialItems, RandRange amount, params string[] range) : base(amount)
        {
            Range = new List<string>();
            Range.AddRange(range);
            UseMapItems = mapItems;
            UseSpecialItems = specialItems;
        }

        /// <summary>
        /// Copy constructor for cloning.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected ItemThemeRange(ItemThemeRange other) : base(other)
        {
            Range = other.Range;
            UseMapItems = other.UseMapItems;
            UseSpecialItems = other.UseSpecialItems;
        }

        /// <inheritdoc/>
        public override ItemTheme Copy() { return new ItemThemeRange(this); }

        /// <inheritdoc/>
        public override List<MapItem> GenerateItems(BaseMapGenContext map, SpawnList<MapItem> specialItems)
        {
            int itemCount = Amount.Pick(map.Rand);
            List<MapItem> spawners = new List<MapItem>();

            SpawnList<MapItem> subList = new SpawnList<MapItem>();
            if (UseSpecialItems)
            {
                for (int ii = 0; ii < specialItems.Count; ii++)
                {
                    MapItem spawn = specialItems.GetSpawn(ii);
                    if (!spawn.IsMoney)
                    {
                        if (Range.Contains(spawn.Value))
                            subList.Add(spawn, specialItems.GetSpawnRate(ii));
                    }
                }
            }

            if (UseMapItems)
            {
                foreach (string key in map.ItemSpawns.Spawns.GetKeys())
                {
                    SpawnList<InvItem> spawns = map.ItemSpawns.Spawns.GetSpawn(key);
                    for (int ii = 0; ii < spawns.Count; ii++)
                    {
                        //TODO: spawn rate is somewhat distorted here
                        InvItem spawn = spawns.GetSpawn(ii);
                        //ItemData data = DataManager.Instance.GetItem(spawn.ID);
                        if (Range.Contains(spawn.ID))
                            subList.Add(new MapItem(spawn), spawns.GetSpawnRate(ii));
                    }
                }
            }

            if (subList.Count == 0)
                return spawners;

            for (int ii = 0; ii < itemCount; ii++)
                spawners.Add(subList.Pick(map.Rand));

            return spawners;
        }
    }

    /// <summary>
    /// An item theme that generates money drops with a custom multiplier on the base amount.
    /// </summary>
    [Serializable]
    public class ItemThemeMoney : ItemTheme
    {
        /// <summary>
        /// The percentage multiplier (0-100) applied to the base money amount.
        /// </summary>
        public int Multiplier;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public ItemThemeMoney() { }

        /// <summary>
        /// Initializes a new instance with the specified multiplier.
        /// </summary>
        /// <param name="mult">The percentage multiplier for money amounts.</param>
        /// <param name="amount">The range of money items to generate.</param>
        public ItemThemeMoney(int mult, RandRange amount) : base(amount)
        {
            Multiplier = mult;
        }

        /// <summary>
        /// Copy constructor for cloning.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected ItemThemeMoney(ItemThemeMoney other) : base(other)
        {
            Multiplier = other.Multiplier;
        }

        /// <inheritdoc/>
        public override ItemTheme Copy() { return new ItemThemeMoney(this); }

        /// <inheritdoc/>
        public override List<MapItem> GenerateItems(BaseMapGenContext map, SpawnList<MapItem> specialItems)
        {
            int itemCount = Amount.Pick(map.Rand);
            List<MapItem> spawners = new List<MapItem>();

            for (int ii = 0; ii < itemCount; ii++)
                spawners.Add(MapItem.CreateMoney(Math.Max(1, map.MoneyAmount.Pick(map.Rand).Amount * Multiplier / 100)));

            return spawners;
        }
    }

    /// <summary>
    /// An item theme that generates items using a custom box spawner.
    /// </summary>
    [Serializable]
    public class ItemThemeBox : ItemTheme
    {
        /// <summary>
        /// The box spawner that generates the items.
        /// </summary>
        public BoxSpawner<BaseMapGenContext> Spawner;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public ItemThemeBox() { }

        /// <summary>
        /// Initializes a new instance with the specified spawner.
        /// </summary>
        /// <param name="spawner">The box spawner to use for item generation.</param>
        /// <param name="amount">The range of items to generate.</param>
        public ItemThemeBox(BoxSpawner<BaseMapGenContext> spawner, RandRange amount) : base(amount)
        {
            Spawner = spawner;
        }

        /// <summary>
        /// Copy constructor for cloning.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected ItemThemeBox(ItemThemeBox other) : base(other)
        {
            Spawner = other.Spawner;
        }

        /// <inheritdoc/>
        public override ItemTheme Copy() { return new ItemThemeBox(this); }

        /// <inheritdoc/>
        public override List<MapItem> GenerateItems(BaseMapGenContext map, SpawnList<MapItem> specialItems)
        {
            
            int itemCount = Amount.Pick(map.Rand);
            List<MapItem> spawners = new List<MapItem>();

            for (int ii = 0; ii < itemCount; ii++)
            {
                List<MapItem> spawned = Spawner.GetSpawns(map);
                spawners.AddRange(spawned);
                while (spawners.Count > itemCount)
                    spawners.RemoveAt(spawners.Count - 1);
                if (spawners.Count >= itemCount)
                    break;
            }

            return spawners;
        }
    }


    /// <summary>
    /// A mob theme that generates mobs from a family seeded by mobs already present in the floor.
    /// </summary>
    [Serializable]
    public class MobThemeFamilySeeded : MobThemeFamily
    {
        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MobThemeFamilySeeded() : base() { }

        /// <summary>
        /// Initializes a new instance with the specified mob count range.
        /// </summary>
        /// <param name="amount">The range of mobs to generate.</param>
        public MobThemeFamilySeeded(RandRange amount) : base(amount) { }

        /// <summary>
        /// Copy constructor for cloning.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected MobThemeFamilySeeded(MobThemeFamilySeeded other)  : base(other)
        { }

        /// <inheritdoc/>
        public override MobTheme Copy() { return new MobThemeFamilySeeded(this); }

        /// <inheritdoc/>
        protected override IEnumerable<string> GetSpecies(BaseMapGenContext map, SpawnList<MobSpawn> specialMobs)
        {
            MobSpawn baseMob = GetSeedChar(map, specialMobs);
            if (baseMob != null)
            {
                string earliestBaseStage = baseMob.BaseForm.Species;

                MonsterFeatureData featureIndex = DataManager.Instance.UniversalData.Get<MonsterFeatureData>();
                FormFeatureSummary baseData = featureIndex.FeatureData[earliestBaseStage][0];
                yield return baseData.Family;
            }
        }
    }

    /// <summary>
    /// A mob theme that generates mobs from a specific family chosen at configuration time.
    /// </summary>
    [Serializable]
    public class MobThemeFamilyChosen : MobThemeFamily
    {
        /// <summary>
        /// The array of family indices to generate mobs from.
        /// </summary>
        public int[] Species;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MobThemeFamilyChosen() : base() { }

        /// <summary>
        /// Initializes a new instance with the specified families.
        /// </summary>
        /// <param name="amount">The range of mobs to generate.</param>
        /// <param name="species">The family indices to include.</param>
        public MobThemeFamilyChosen(RandRange amount, params int[] species) : base(amount)
        {
            Species = species;
        }

        /// <summary>
        /// Copy constructor for cloning.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected MobThemeFamilyChosen(MobThemeFamilyChosen other)  : base(other)
        {
            Species = new int[other.Species.Length];
            other.Species.CopyTo(Species, 0);
        }

        /// <inheritdoc/>
        public override MobTheme Copy() { return new MobThemeFamilyChosen(this); }

        /// <inheritdoc/>
        protected override IEnumerable<string> GetSpecies(BaseMapGenContext map, SpawnList<MobSpawn> specialMobs)
        {
            foreach (int specie in Species)
                yield return specie.ToString();
        }
    }

    /// <summary>
    /// Abstract base class for mob themes that generate mobs from specific families.
    /// </summary>
    [Serializable]
    public abstract class MobThemeFamily : MobTheme
    {
        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        protected MobThemeFamily() { }

        /// <summary>
        /// Initializes a new instance with the specified mob count range.
        /// </summary>
        /// <param name="amount">The range of mobs to generate.</param>
        public MobThemeFamily(RandRange amount) : base(amount) { }

        /// <summary>
        /// Copy constructor for cloning.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected MobThemeFamily(MobThemeFamily other) : base(other) { }

        /// <inheritdoc/>
        public override List<MobSpawn> GenerateMobs(BaseMapGenContext map, SpawnList<MobSpawn> specialMobs)
        {
            int mobCount = Amount.Pick(map.Rand);
            List<MobSpawn> spawners = new List<MobSpawn>();
            IEnumerable<string> species = GetSpecies(map, specialMobs);
            SpawnList<MobSpawn> subList = new SpawnList<MobSpawn>();
            for (int ii = 0; ii < specialMobs.Count; ii++)
            {
                MobSpawn spawn = specialMobs.GetSpawn(ii);
                if (CheckIfAllowed(map, spawn, species))
                    subList.Add(spawn, specialMobs.GetSpawnRate(ii));
            }
            for (int ii = 0; ii < map.TeamSpawns.Count; ii++)
            {
                SpawnList<MobSpawn> memberSpawns = map.TeamSpawns.GetSpawn(ii).GetPossibleSpawns();
                for (int jj = 0; jj < memberSpawns.Count; jj++)
                {
                    MobSpawn spawn = memberSpawns.GetSpawn(jj);
                    if (CheckIfAllowed(map, spawn, species))
                        subList.Add(spawn, memberSpawns.GetSpawnRate(jj));
                }
            }

            if (subList.Count > 0)
            {
                for (int ii = 0; ii < mobCount; ii++)
                    spawners.Add(subList.Pick(map.Rand));
            }

            return spawners;
        }

        /// <summary>
        /// Gets the collection of family names/IDs to generate mobs from.
        /// </summary>
        /// <param name="map">The map generation context.</param>
        /// <param name="specialMobs">Additional special mobs available for spawning.</param>
        /// <returns>An enumerable of family IDs to generate mobs from.</returns>
        protected abstract IEnumerable<string> GetSpecies(BaseMapGenContext map, SpawnList<MobSpawn> specialMobs);

        /// <summary>
        /// Checks if a mob spawn matches the specified families.
        /// </summary>
        /// <param name="map">The map generation context.</param>
        /// <param name="spawn">The mob spawn to check.</param>
        /// <param name="species">The allowed family IDs.</param>
        /// <returns>True if the mob belongs to one of the specified families; otherwise, false.</returns>
        protected bool CheckIfAllowed(BaseMapGenContext map, MobSpawn spawn, IEnumerable<string> species)
        {
            MonsterFeatureData featureIndex = DataManager.Instance.UniversalData.Get<MonsterFeatureData>();
            FormFeatureSummary baseData = featureIndex.FeatureData[spawn.BaseForm.Species][0];

            foreach (string baseStage in species)
            {
                if (baseStage == baseData.Family)
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// A mob theme that generates mobs with specific chosen element types.
    /// </summary>
    [Serializable]
    public class MobThemeTypingChosen : MobThemeTyping
    {
        /// <summary>
        /// The array of element type IDs to include.
        /// </summary>
        [JsonConverter(typeof(ElementArrayConverter))]
        [DataType(1, DataManager.DataType.Element, false)]
        public string[] Types;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MobThemeTypingChosen() : base() { }

        /// <summary>
        /// Initializes a new instance with the specified element types and evolution allowance.
        /// </summary>
        /// <param name="allowance">The evolution stages to allow in generated mobs.</param>
        /// <param name="amount">The range of mobs to generate.</param>
        /// <param name="types">The element type IDs to include.</param>
        public MobThemeTypingChosen(EvoFlag allowance, RandRange amount, params string[] types) : base(allowance, amount)
        {
            Types = types;
        }

        /// <summary>
        /// Copy constructor for cloning.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected MobThemeTypingChosen(MobThemeTypingChosen other)  : base(other)
        {
            Types = new string[other.Types.Length];
            other.Types.CopyTo(Types, 0);
        }

        /// <inheritdoc/>
        public override MobTheme Copy() { return new MobThemeTypingChosen(this); }

        /// <inheritdoc/>
        protected override List<string> GetTypes(BaseMapGenContext map, SpawnList<MobSpawn> specialMobs)
        {
            List<string> result = new List<string>();
            foreach (string type in Types)
                result.Add(type);
            return result;
        }
    }

    /// <summary>
    /// A mob theme that generates mobs with element types seeded by mobs already present in the floor.
    /// </summary>
    [Serializable]
    public class MobThemeTypingSeeded : MobThemeTyping
    {
        /// <summary>
        /// The maximum number of element types to consider when selecting from frequency analysis.
        /// </summary>
        const int THRESHOLD = 3;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MobThemeTypingSeeded() : base() { }

        /// <summary>
        /// Initializes a new instance with the specified evolution allowance and mob count range.
        /// </summary>
        /// <param name="allowance">The evolution stages to allow in generated mobs.</param>
        /// <param name="amount">The range of mobs to generate.</param>
        public MobThemeTypingSeeded(EvoFlag allowance, RandRange amount) : base(allowance, amount) { }

        /// <summary>
        /// Copy constructor for cloning.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected MobThemeTypingSeeded(MobThemeTypingSeeded other)  : base(other) { }

        /// <inheritdoc/>
        public override MobTheme Copy() { return new MobThemeTypingSeeded(this); }

        /// <inheritdoc/>
        protected override List<string> GetTypes(BaseMapGenContext map, SpawnList<MobSpawn> specialMobs)
        {
            Dictionary<string, int> elementFrequency = new Dictionary<string, int>();

            for (int ii = 0; ii < map.TeamSpawns.Count; ii++)
            {
                SpawnList<MobSpawn> mobSpawns = map.TeamSpawns.GetSpawn(ii).GetPossibleSpawns();
                foreach (MobSpawn spawn in mobSpawns.EnumerateOutcomes())
                {
                    MonsterFeatureData featureIndex = DataManager.Instance.UniversalData.Get<MonsterFeatureData>();
                    FormFeatureSummary baseData = featureIndex.FeatureData[spawn.BaseForm.Species][Math.Max(0, spawn.BaseForm.Form)];
                    if (baseData.Element1 != DataManager.Instance.DefaultElement)
                        MathUtils.AddToDictionary(elementFrequency, baseData.Element1, 1);
                    if (baseData.Element2 != DataManager.Instance.DefaultElement)
                        MathUtils.AddToDictionary(elementFrequency, baseData.Element2, 1);
                }
            }

            if (elementFrequency.Count == 0)
            {
                for (int ii = 0; ii < specialMobs.Count; ii++)
                {
                    MobSpawn spawn = specialMobs.GetSpawn(ii);
                    MonsterFeatureData featureIndex = DataManager.Instance.UniversalData.Get<MonsterFeatureData>();
                    FormFeatureSummary baseData = featureIndex.FeatureData[spawn.BaseForm.Species][Math.Max(0, spawn.BaseForm.Form)];
                    if (baseData.Element1 != DataManager.Instance.DefaultElement)
                        MathUtils.AddToDictionary(elementFrequency, baseData.Element1, 1);
                    if (baseData.Element2 != DataManager.Instance.DefaultElement)
                        MathUtils.AddToDictionary(elementFrequency, baseData.Element2, 1);
                }
            }

            List<string> result = new List<string>();

            if (elementFrequency.Count > 0)
            {
                //choose randomly from the top 3 types
                List<(string, int)> elements = new List<(string, int)>();
                foreach (string key in elementFrequency.Keys)
                    elements.Add((key, elementFrequency[key]));
                elements.Sort((a, b) => b.Item2.CompareTo(a.Item2));
                int max = elements[0].Item2;
                int limit = elements[Math.Min(THRESHOLD - 1, elements.Count - 1)].Item2 - 1;
                if (limit == 0 && max > 1)
                    limit = 1;
                for (int ii = 0; ii < elements.Count; ii++)
                {
                    if (elements[ii].Item2 > limit)
                        result.Add(elements[ii].Item1);
                }
            }
            return result;
        }
    }

    /// <summary>
    /// Abstract base class for mob themes that generate mobs based on element types.
    /// </summary>
    [Serializable]
    public abstract class MobThemeTyping : MobThemeEvoRestricted
    {
        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MobThemeTyping() { }

        /// <summary>
        /// Initializes a new instance with the specified evolution allowance and mob count range.
        /// </summary>
        /// <param name="allowance">The evolution stages to allow in generated mobs.</param>
        /// <param name="amount">The range of mobs to generate.</param>
        public MobThemeTyping(EvoFlag allowance, RandRange amount) : base(allowance, amount) { }

        /// <summary>
        /// Copy constructor for cloning.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected MobThemeTyping(MobThemeTyping other) : base(other) { }

        /// <inheritdoc/>
        public override List<MobSpawn> GenerateMobs(BaseMapGenContext map, SpawnList<MobSpawn> specialMobs)
        {
            int mobCount = Amount.Pick(map.Rand);
            List<MobSpawn> spawners = new List<MobSpawn>();
            List<string> types = GetTypes(map, specialMobs);
            SpawnList<MobSpawn> subList = new SpawnList<MobSpawn>();
            for (int ii = 0; ii < specialMobs.Count; ii++)
            {
                MobSpawn spawn = specialMobs.GetSpawn(ii);
                if (CheckIfAllowed(map, spawn, types))
                    subList.Add(spawn, specialMobs.GetSpawnRate(ii));
            }
            for (int ii = 0; ii < map.TeamSpawns.Count; ii++)
            {
                SpawnList<MobSpawn> memberSpawns = map.TeamSpawns.GetSpawn(ii).GetPossibleSpawns();
                for (int jj = 0; jj < memberSpawns.Count; jj++)
                {
                    MobSpawn spawn = memberSpawns.GetSpawn(jj);
                    if (CheckIfAllowed(map, spawn, types))
                        subList.Add(spawn, memberSpawns.GetSpawnRate(jj));
                }
            }

            if (subList.Count > 0)
            {
                for (int ii = 0; ii < mobCount; ii++)
                    spawners.Add(subList.Pick(map.Rand));
            }

            return spawners;
        }

        /// <summary>
        /// Gets the collection of element type IDs to filter mobs by.
        /// </summary>
        /// <param name="map">The map generation context.</param>
        /// <param name="specialMobs">Additional special mobs available for spawning.</param>
        /// <returns>A list of element type IDs to include.</returns>
        protected abstract List<string> GetTypes(BaseMapGenContext map, SpawnList<MobSpawn> specialMobs);

        /// <summary>
        /// Checks if a mob spawn matches the specified element types and evolution allowance.
        /// </summary>
        /// <param name="map">The map generation context.</param>
        /// <param name="spawn">The mob spawn to check.</param>
        /// <param name="types">The allowed element type IDs.</param>
        /// <returns>True if the mob matches the type and evolution criteria; otherwise, false.</returns>
        protected bool CheckIfAllowed(BaseMapGenContext map, MobSpawn spawn, List<string> types)
        {
            MonsterFeatureData featureIndex = DataManager.Instance.UniversalData.Get<MonsterFeatureData>();
            FormFeatureSummary baseData = featureIndex.FeatureData[spawn.BaseForm.Species][Math.Max(0, spawn.BaseForm.Form)];
            bool matchesType = false;
            foreach (string type in types)
            {
                if (baseData.Element1 == type || baseData.Element2 == type)
                {
                    matchesType = true;
                    break;
                }
            }

            if (matchesType)
            {
                if (CheckIfAllowed(baseData))
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// A mob theme that generates mobs based on their best or worst stat.
    /// </summary>
    //TODO: seedable stat themes?
    [Serializable]
    public class MobThemeStat : MobThemeEvoRestricted
    {
        /// <summary>
        /// The stat to filter mobs by.
        /// </summary>
        public Stat ChosenStat;

        /// <summary>
        /// If true, selects mobs with this stat as their weakness; if false, as their strength.
        /// </summary>
        public bool Weakness;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MobThemeStat() { }

        /// <summary>
        /// Initializes a new instance with the specified stat and evaluation mode.
        /// </summary>
        /// <param name="stat">The stat to filter by.</param>
        /// <param name="weakness">If true, selects mobs where this is their worst stat; if false, their best stat.</param>
        /// <param name="allowance">The evolution stages to allow in generated mobs.</param>
        /// <param name="amount">The range of mobs to generate.</param>
        public MobThemeStat(Stat stat, bool weakness, EvoFlag allowance, RandRange amount) : base(allowance, amount)
        {
            ChosenStat = stat;
            Weakness = weakness;
        }

        /// <summary>
        /// Copy constructor for cloning.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected MobThemeStat(MobThemeStat other) : base(other)
        {
            ChosenStat = other.ChosenStat;
            Weakness = other.Weakness;
        }

        /// <inheritdoc/>
        public override MobTheme Copy() { return new MobThemeStat(this); }

        /// <inheritdoc/>
        public override List<MobSpawn> GenerateMobs(BaseMapGenContext map, SpawnList<MobSpawn> specialMobs)
        {
            int mobCount = Amount.Pick(map.Rand);
            List<MobSpawn> spawners = new List<MobSpawn>();

            SpawnList<MobSpawn> subList = new SpawnList<MobSpawn>();
            for (int ii = 0; ii < specialMobs.Count; ii++)
            {
                MobSpawn spawn = specialMobs.GetSpawn(ii);
                if (CheckIfAllowed(spawn))
                    subList.Add(spawn, specialMobs.GetSpawnRate(ii));
            }
            for (int ii = 0; ii < map.TeamSpawns.Count; ii++)
            {
                SpawnList<MobSpawn> memberSpawns = map.TeamSpawns.GetSpawn(ii).GetPossibleSpawns();
                for (int jj = 0; jj < memberSpawns.Count; jj++)
                {
                    if (CheckIfAllowed(memberSpawns.GetSpawn(jj)))
                        subList.Add(memberSpawns.GetSpawn(jj), memberSpawns.GetSpawnRate(jj));
                }
            }

            if (subList.Count > 0)
            {
                for (int ii = 0; ii < mobCount; ii++)
                    spawners.Add(subList.Pick(map.Rand));
            }

            return spawners;
        }

        /// <summary>
        /// Checks if a mob spawn matches the chosen stat criteria and evolution allowance.
        /// </summary>
        /// <param name="spawn">The mob spawn to check.</param>
        /// <returns>True if the mob's chosen stat matches the criteria; otherwise, false.</returns>
        protected bool CheckIfAllowed(MobSpawn spawn)
        {
            MonsterFeatureData featureIndex = DataManager.Instance.UniversalData.Get<MonsterFeatureData>();
            FormFeatureSummary baseData = featureIndex.FeatureData[spawn.BaseForm.Species][Math.Max(0, spawn.BaseForm.Form)];

            Stat spawnStat = Weakness ? baseData.WorstStat : baseData.BestStat;

            if (spawnStat == ChosenStat)
            {
                if (CheckIfAllowed(baseData))
                    return true;
            }
            return false;
        }
    }


    /// <summary>
    /// Abstract base class for mob themes that restrict mobs by evolution stage.
    /// </summary>
    [Serializable]
    public abstract class MobThemeEvoRestricted : MobTheme
    {
        /// <summary>
        /// The evolution stages allowed in generated mobs.
        /// </summary>
        public EvoFlag EvoAllowance;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MobThemeEvoRestricted() { }

        /// <summary>
        /// Initializes a new instance with the specified evolution allowance.
        /// </summary>
        /// <param name="allowance">The evolution stages to allow.</param>
        /// <param name="amount">The range of mobs to generate.</param>
        public MobThemeEvoRestricted(EvoFlag allowance, RandRange amount) : base(amount) { EvoAllowance = allowance; }

        /// <summary>
        /// Copy constructor for cloning.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected MobThemeEvoRestricted(MobThemeEvoRestricted other) : base(other)
        {
            EvoAllowance = other.EvoAllowance;
        }

        /// <summary>
        /// Checks if a mon's evolution stage is allowed by this theme's restrictions.
        /// </summary>
        /// <param name="baseData">The monster form feature data to check.</param>
        /// <returns>True if the mob's evolution stage matches the allowed flags; otherwise, false.</returns>
        protected virtual bool CheckIfAllowed(FormFeatureSummary baseData)
        {
            return ((baseData.Stage & EvoAllowance) != EvoFlag.None);
        }
    }

}
