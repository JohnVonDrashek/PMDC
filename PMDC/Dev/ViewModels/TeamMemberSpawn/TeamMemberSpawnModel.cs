using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using PMDC.Data;
using PMDC.LevelGen;
using ReactiveUI;
using RogueEssence;
using RogueEssence.Data;
using RogueEssence.Dev;
using RogueEssence.Dev.ViewModels;
using RogueEssence.Dev.Views;
using RogueEssence.Dungeon;
using RogueEssence.LevelGen;

namespace PMDC.Dev.ViewModels
{
    /// <summary>
    /// View model for editing team member spawn configurations in the Avalonia editor.
    /// Manages monster selection, skill/ability assignment, level ranges, and spawn modifiers
    /// with support for filtering and reactive UI updates.
    /// </summary>
    public class TeamMemberSpawnModel : ViewModelBase
    {
        /// <summary>
        /// The underlying team member spawn being edited.
        /// </summary>
        public TeamMemberSpawn TeamSpawn;

        /// <summary>
        /// Gets or sets the collection box view model for spawn conditions.
        /// </summary>
        public CollectionBoxViewModel SpawnConditions { get; set; }

        /// <summary>
        /// Gets or sets the collection box view model for spawn features/modifiers.
        /// </summary>
        public CollectionBoxViewModel SpawnFeatures { get; set; }

        /// <summary>
        /// Flag to control whether search operations should trigger filtering updates.
        /// </summary>
        private bool _applySearch = true;

        /// <summary>
        /// Flag to control whether spawn feature changes should trigger UI updates.
        /// Used to prevent infinite loops during batch operations.
        /// </summary>
        private bool _checkSpawnFeatureDiff = true;

        private int _selectedMonsterIndex;

        /// <summary>
        /// Gets or sets the index of the selected monster form.
        /// When changed, updates the monster and refreshes available skills/abilities.
        /// </summary>
        public int SelectedMonsterIndex
        {
            get { return _selectedMonsterIndex; }
            set
            {

                if (this.SetIfChanged(ref _selectedMonsterIndex, value))
                {
                    UpdateMonster(_selectedMonsterIndex);
                }
            }
        }
        


        /// <summary>
        /// Backing field for the DisableUnusedSlots property.
        /// </summary>
        private bool _disableUnusedSlots;

        /// <summary>
        /// Gets or sets whether unused move slots are disabled on this spawn.
        /// </summary>
        public bool DisableUnusedSlots
        {
            get { return _disableUnusedSlots; }
            set
            {
                if (this.SetIfChanged(ref _disableUnusedSlots, value)  && _checkSpawnFeatureDiff)
                {
                    ToggleUnusedSlots(_disableUnusedSlots);
                }
            }
        }


        /// <summary>
        /// Backing field for the IsWeakMob property.
        /// </summary>
        private bool _isWeakMonster;

        /// <summary>
        /// Gets or sets whether this spawn is a weak monster (reduced PP and belly).
        /// </summary>
        public bool IsWeakMob
        {
            get { return _isWeakMonster; }
            set
            {
                if (this.SetIfChanged(ref _isWeakMonster, value)  && _checkSpawnFeatureDiff)
                {
                    ToggleWeakMonster(_isWeakMonster);
                }
            }
        }


        /// <summary>
        /// Backing field for the Unrecruitable property.
        /// </summary>
        private bool _unrecruitable;

        /// <summary>
        /// Gets or sets whether this spawn cannot be recruited.
        /// </summary>
        public bool Unrecruitable
        {
            get { return _unrecruitable; }
            set
            {
                if (this.SetIfChanged(ref _unrecruitable, value)  && _checkSpawnFeatureDiff)
                {
                    ToggleUnrecruitable(_unrecruitable);
                }
            }
        }


        /// <summary>
        /// Toggles the MobSpawnMovesOff feature to disable or enable unused move slots.
        /// </summary>
        /// <param name="disable">True to disable unused slots, false to enable them.</param>
        private void ToggleUnusedSlots(bool disable)
        {
            List<MobSpawnExtra> features = SpawnFeatures.GetList<List<MobSpawnExtra>>();
            if (disable)
            {
                int skillCount = getSkillList().Count;
                SpawnFeatures.InsertItem(SpawnFeatures.Collection.Count, new MobSpawnMovesOff(skillCount));
            }
            else
            {
                for (int ii = features.Count - 1; ii >= 0; ii--)
                {
                    if (features[ii] is MobSpawnMovesOff)
                    {
                        SpawnFeatures.DeleteItem(ii);
                    }
                }
            }
        }


        /// <summary>
        /// Toggles the MobSpawnWeak feature to make the spawn weak or normal.
        /// </summary>
        /// <param name="weak">True to make the spawn weak, false to make it normal.</param>
        private void ToggleWeakMonster(bool weak)
        {
            List<MobSpawnExtra> features = SpawnFeatures.GetList<List<MobSpawnExtra>>();
            if (weak)
            {
                SpawnFeatures.InsertItem(SpawnFeatures.Collection.Count, new MobSpawnWeak());
            }
            else
            {
                for (int ii = features.Count - 1; ii >= 0; ii--)
                {
                    if (features[ii] is MobSpawnWeak)
                    {
                        SpawnFeatures.DeleteItem(ii);
                    }
                }
            }
        }


        /// <summary>
        /// Toggles the MobSpawnUnrecruitable feature to make the spawn recruitable or not.
        /// </summary>
        /// <param name="unrecruitable">True to make the spawn unrecruitable, false to make it recruitable.</param>
        private void ToggleUnrecruitable(bool unrecruitable)
        {

            List<MobSpawnExtra> features = SpawnFeatures.GetList<List<MobSpawnExtra>>();
            for (int ii = features.Count - 1; ii >= 0; ii--)
            {
                if (features[ii] is MobSpawnUnrecruitable)
                {
                    SpawnFeatures.DeleteItem(ii);
                }
            }

            if (unrecruitable)
            {
                SpawnFeatures.InsertItem(SpawnFeatures.Collection.Count, new MobSpawnUnrecruitable());
            }
        }


        /// <summary>
        /// Backing field for the SelectedIntrinsicIndex property.
        /// </summary>
        private int _selectedIntrinsicIndex;

        /// <summary>
        /// Gets or sets the index of the selected intrinsic ability.
        /// When changed, updates the spawn's intrinsic.
        /// </summary>
        public int SelectedIntrinsicIndex
        {
            get { return _selectedIntrinsicIndex; }
            set
            {
                if (this.SetIfChanged(ref _selectedIntrinsicIndex, value))
                {
                    UpdateIntrinsic(_selectedIntrinsicIndex);
                }
            }
        }

        /// <summary>
        /// Updates the spawn's intrinsic ability based on the selected index.
        /// </summary>
        /// <param name="index">The index of the intrinsic ability to set.</param>
        private void UpdateIntrinsic(int index)
        {
            string intrinsic = "";
            if (_selectedIntrinsicIndex != -1)
            {
                intrinsic = intrinsicKeys[index];
                SelectedIntrinsic = intrinsicData[index];
            }

            TeamSpawn.Spawn.Intrinsic = intrinsic;
        }


        /// <summary>
        /// Backing field for the IncludeUnreleasedIntrinsics property.
        /// </summary>
        private bool _includeUnreleasedIntrinsics;

        /// <summary>
        /// Gets or sets whether unreleased intrinsics are included in the filter results.
        /// </summary>
        public bool IncludeUnreleasedIntrinsics
        {
            get { return _includeUnreleasedIntrinsics; }
            set
            {
                
                if (this.SetIfChanged(ref _includeUnreleasedIntrinsics, value))
                {
                    if (_applySearch) 
                        updateIntrinsicData(SearchIntrinsicFilter);
                    
                }
            }
        }

        /// <summary>
        /// Backing field for the IncludeUnreleasedSkills property.
        /// </summary>
        private bool _includeUnreleasedSkills;

        /// <summary>
        /// Gets or sets whether unreleased skills are included in the filter results.
        /// </summary>
        public bool IncludeUnreleasedSkills
        {
            get { return _includeUnreleasedSkills; }
            set
            {
                
                if (this.SetIfChanged(ref _includeUnreleasedSkills, value))
                {
                    if (_applySearch) 
                        UpdateSkillData(CurrentSkillSearchFilter);
                }
            }
        }


        /// <summary>
        /// Backing field for the IncludeTemporaryForms property.
        /// </summary>
        private bool _includeUnreleasedForms;

        /// <summary>
        /// Gets or sets whether temporary forms (e.g., Mega evolutions) are included in the filter results.
        /// </summary>
        public bool IncludeTemporaryForms
        {
            get => _includeTemporaryForms;
            set
            {
                if (this.SetIfChanged(ref _includeTemporaryForms, value))
                { 
                    if (_applySearch) 
                        updateMonsterForms(SearchMonsterFilter);
                }
            }
        }


        /// <summary>
        /// Backing field for the IncludeUnreleasedForms property.
        /// </summary>
        private bool _includeTemporaryForms;

        /// <summary>
        /// Gets or sets whether unreleased monster forms are included in the filter results.
        /// </summary>
        public bool IncludeUnreleasedForms
        {
            get => _includeUnreleasedForms;
            set
            {
                if (this.SetIfChanged(ref _includeUnreleasedForms, value))
                {
                    if (_applySearch) 
                        updateMonsterForms(SearchMonsterFilter);
                }
            }
        }


        /// <summary>
        /// Backing field for the SearchMonsterFilter property.
        /// </summary>
        private string _searchMonsterFilter;

        /// <summary>
        /// Gets or sets the search filter text for monster selection.
        /// </summary>
        public string SearchMonsterFilter
        {
            get => _searchMonsterFilter;
            set
            {
                if (this.SetIfChanged(ref _searchMonsterFilter, value))
                {
                    if (_applySearch) 
                        updateMonsterForms(_searchMonsterFilter);
                }
            }
        }

        /// <summary>
        /// Backing field for the SearchIntrinsicFilter property.
        /// </summary>
        private string _searchIntrinsicFilter = string.Empty;

        /// <summary>
        /// Gets or sets the search filter text for intrinsic ability selection.
        /// </summary>
        public string SearchIntrinsicFilter
        {
            get => _searchIntrinsicFilter;
            set
            {
                if (this.SetIfChanged(ref _searchIntrinsicFilter, value))
                {
                    if (_applySearch) 
                        updateIntrinsicData(_searchIntrinsicFilter);
                }
            }
        }

        /// <summary>
        /// Gets or sets the minimum level for this spawn.
        /// </summary>
        public int Min
        {
            get { return TeamSpawn.Spawn.Level.Min; }
            set { this.RaiseAndSetIfChanged(ref TeamSpawn.Spawn.Level.Min, value); }
        }


        /// <summary>
        /// Gets or sets the maximum level for this spawn.
        /// Note: Maximum level ranges are internally stored as exclusive, so the displayed value is the internal value minus 1,
        /// but is kept above the minimum level for display clarity. When edited, the internal value is set to displayed value plus 1.
        /// </summary>
        public int Max
        {
            get { return Math.Max(TeamSpawn.Spawn.Level.Max - 1, TeamSpawn.Spawn.Level.Min); }
            set { this.RaiseAndSetIfChanged(ref TeamSpawn.Spawn.Level.Max, value + 1); }
        }

        /// <summary>
        /// Backing field for the CurrentDataGridView property.
        /// </summary>
        private DataGridType _gridViewType = DataGridType.Monster;

        /// <summary>
        /// Gets or sets which data grid (Monster, Skills, Intrinsic, Other) is currently displayed.
        /// </summary>
        public DataGridType CurrentDataGridView
        {
            get { return _gridViewType; }
            set { this.RaiseAndSetIfChanged(ref _gridViewType, value); }
        }


        /// <summary>
        /// The current skill search filter text for the focused skill slot.
        /// </summary>
        public string CurrentSkillSearchFilter;

        /// <summary>
        /// Backing field for the SearchSkill0Filter property.
        /// </summary>
        private string _searchSkill0Filter = string.Empty;

        /// <summary>
        /// Gets or sets the search filter text for skill slot 0.
        /// </summary>
        public string SearchSkill0Filter
        {
            get { return _searchSkill0Filter; }
            set
            { 
                if (this.SetIfChanged(ref _searchSkill0Filter, value))
                {
                    UpdateSkillData(_searchSkill0Filter);
                }
            }
        }

        /// <summary>
        /// Backing field for the SearchSkill1Filter property.
        /// </summary>
        private string _searchSkill1Filter = string.Empty;

        /// <summary>
        /// Gets or sets the search filter text for skill slot 1.
        /// </summary>
        public string SearchSkill1Filter
        {
            get { return _searchSkill1Filter; }
            set
            { 
                if (this.SetIfChanged(ref _searchSkill1Filter, value))
                {
                    UpdateSkillData(_searchSkill1Filter);
                }
            }
        }

        /// <summary>
        /// Backing field for the SearchSkill2Filter property.
        /// </summary>
        private string _searchSkill2Filter = string.Empty;

        /// <summary>
        /// Gets or sets the search filter text for skill slot 2.
        /// </summary>
        public string SearchSkill2Filter
        {
            get { return _searchSkill2Filter; }
            set
            { 
                if (this.SetIfChanged(ref _searchSkill2Filter, value))
                {
                    UpdateSkillData(_searchSkill2Filter);
                }
            }
        }

        /// <summary>
        /// Backing field for the SearchSkill3Filter property.
        /// </summary>
        private string _searchSkill3Filter = string.Empty;

        /// <summary>
        /// Gets or sets the search filter text for skill slot 3.
        /// </summary>
        public string SearchSkill3Filter
        {
            get { return _searchSkill3Filter; }
            set
            { 
                if (this.SetIfChanged(ref _searchSkill3Filter, value))
                {
                    UpdateSkillData(_searchSkill3Filter);
                }
            }
        }

        /// <summary>
        /// Filters the available monster forms based on the search filter text and release status.
        /// </summary>
        /// <param name="filter">The search string to filter monster forms by.</param>
        private void updateMonsterForms(string filter)
        {
            FilteredMonsterForms.Clear();
            IEnumerable<BaseMonsterFormViewModel> result = monsterForms.Select(item => new
                {
                    item,
                    startsWith = startsWith(item.Name, filter),
                    prefixStartsWith = prefixStartsWith(item.Name, filter),
                    includeUnreleased = IncludeUnreleasedForms || item.Released,
                    temporary = !item.Temporary || IncludeTemporaryForms,
                })
                .Where(item =>
                    {
                        return (item.prefixStartsWith || item.startsWith) && item.temporary & item.includeUnreleased;
                    }
                )
                .OrderByDescending(item => item.startsWith)
                .ThenByDescending(item => item.prefixStartsWith)
                .Select(x => x.item);
            addFilteredItems<BaseMonsterFormViewModel>(FilteredMonsterForms, result);
        }

        /// <summary>
        /// Filters the available intrinsic abilities based on the search filter text, release status, and what the current monster can have.
        /// </summary>
        /// <param name="filter">The search string to filter intrinsics by.</param>
        private void updateIntrinsicData(string filter)
        {
            FilteredIntrinsicData.Clear();
            IEnumerable<IntrinsicViewModel> result;
            
            if (SearchIntrinsicFilter == "")
            {
                ResetIntrinsic();
                result = intrinsicData.Select(item => new
                    {
                        item,
                        learns = item.MonsterLearns,
                        includeUnreleased = IncludeUnreleasedIntrinsics || item.Released,
                        selected = item.Index == SelectedIntrinsicIndex,
                    })
                    .Where(item =>
                        {
                            return (item.learns || item.selected) && item.includeUnreleased;
                        }
                    )
                    .Select(x => x.item);
            }
            else
            {

                result = intrinsicData.Select(item => new
                    {
                        item,
                        startsWith = startsWith(item.Name, filter),
                        prefixStartsWith = prefixStartsWith(item.Name, filter),
                        includeUnreleased = IncludeUnreleasedIntrinsics || item.Released,
                        learns = item.MonsterLearns,
                    })
                    .Where(item =>
                        {
                            return (item.prefixStartsWith || item.startsWith) && item.includeUnreleased;
                        }
                    )
                    .OrderByDescending(item => item.learns)
                    .ThenByDescending(item => item.startsWith)
                    .ThenByDescending(item => item.prefixStartsWith)
                    .Select(x => x.item);
            }


            addFilteredItems<IntrinsicViewModel>(FilteredIntrinsicData, result);
        }

        /// <summary>
        /// The index of the currently focused skill slot (0-3).
        /// </summary>
        public int FocusedSkillIndex;

        /// <summary>
        /// Updates the filtered skill data based on the search filter.
        /// </summary>
        /// <param name="filter">The search string to filter skills by.</param>
        public void UpdateSkillData(string filter)
        {
            CurrentSkillSearchFilter = filter;
            FilteredSkillData.Clear();
            IEnumerable<SkillDataViewModel> result;
            if (CurrentSkillSearchFilter == "")
            {
                ResetSkill();
                result = skillData.Select(item => new
                    {
                        item,
                        learns = item.MonsterLearns,
                        includeUnreleased = IncludeUnreleasedSkills || item.Released,
                        inSkillData = SkillIndex.Contains(item.Index),
                    })
                    .Where(item =>
                        {
                            return (item.learns || item.inSkillData) && item.includeUnreleased;
                        }
                    )
                    .Select(x => x.item);
            }
            else
            {
                
                result = skillData.Select(item => new
                    {
                        item,
                        startsWith = startsWith(item.Name, filter),
                        prefixStartsWith = prefixStartsWith(item.Name, filter),
                        includeUnreleased = IncludeUnreleasedSkills || item.Released,
                        learns = item.MonsterLearns,
                    })
                    .Where(item =>
                        {
                            return (item.prefixStartsWith || item.startsWith) && item.includeUnreleased;
                        }
                    )
                    .OrderByDescending(item => item.learns)
                    .ThenByDescending(item => item.startsWith)
                    .ThenByDescending(item => item.prefixStartsWith)
                    .Select(x => x.item);
            }

            addFilteredItems<SkillDataViewModel>(FilteredSkillData, result);
        }

        /// <summary>
        /// Gets or sets the chosen gender index for this spawn.
        /// </summary>
        public int ChosenGender
        {
            get { return genderKeys.IndexOf((int) TeamSpawn.Spawn.BaseForm.Gender); }
            set
            {
                this.RaiseAndSetIfChanged(ref TeamSpawn.Spawn.BaseForm.Gender, (Gender) genderKeys[value]);
            }
        }

        /// <summary>
        /// Sets the selected monster by index and updates the search filter to match.
        /// </summary>
        /// <param name="index">The index of the monster form to select.</param>
        public void SetMonster(int index)
        {
            SelectedMonsterIndex = index;
            BaseMonsterFormViewModel vm = monsterForms[index];
            SearchMonsterFilter = vm.Name;
        }

        /// <summary>
        /// Updates the spawn's monster form and resets associated skills and intrinsics.
        /// </summary>
        /// <param name="index">The index of the monster form to set.</param>
        private void UpdateMonster(int index)
        {
            BaseMonsterFormViewModel vm = monsterForms[index];

            SetFormPossibleIntrinsics(vm.Key, vm.FormId);
            updateIntrinsicData(_searchIntrinsicFilter);
            _applySearch = false;
            SelectedMonsterForm = vm;
            TeamSpawn.Spawn.BaseForm.Form = vm.FormId;
            TeamSpawn.Spawn.BaseForm.Species = vm.Key;
            SetFormPossibleSkills(vm.Key, vm.FormId);
            SkillIndex = new List<int> { -1, -1, -1, -1 };
            TeamSpawn.Spawn.SpecifiedSkills = new List<string>();
            SearchSkill0Filter = "";
            SearchSkill1Filter = "";
            SearchSkill2Filter = "";
            SearchSkill3Filter = "";
            SearchIntrinsicFilter = "";
            SelectedIntrinsic = null;
            SelectedIntrinsicIndex = -1;
            _applySearch = true;

        }

        /// <summary>
        /// Backing field for the SelectedMonsterForm property.
        /// </summary>
        private BaseMonsterFormViewModel _selectedMonsterForm;

        /// <summary>
        /// Gets or sets the currently selected monster form view model.
        /// </summary>
        public BaseMonsterFormViewModel SelectedMonsterForm
        {
            get => _selectedMonsterForm;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedMonsterForm, value);
            }
        }
        
        private IntrinsicViewModel _selectedIntrinsic;

        /// <summary>
        /// Gets or sets the currently selected intrinsic ability view model.
        /// </summary>
        public IntrinsicViewModel SelectedIntrinsic
        {
            get => _selectedIntrinsic;
            set { this.RaiseAndSetIfChanged(ref _selectedIntrinsic, value); }
        }

        /// <summary>
        /// Backing field for the SelectedSkillData property.
        /// </summary>
        private SkillDataViewModel _selectedSkillData;

        /// <summary>
        /// Gets or sets the currently selected skill data view model.
        /// </summary>
        public SkillDataViewModel SelectedSkillData
        {
            get => _selectedSkillData;
            set { this.RaiseAndSetIfChanged(ref _selectedSkillData, value); }
        }

        /// <summary>
        /// Gets or sets the chosen skin index for this spawn.
        /// </summary>
        public int ChosenSkin
        {
            get { return skinKeys.IndexOf(TeamSpawn.Spawn.BaseForm.Skin); }
            set { this.RaiseAndSetIfChanged(ref TeamSpawn.Spawn.BaseForm.Skin, skinKeys[value]); }
        }

        /// <summary>
        /// Gets or sets the chosen AI tactic index for this spawn.
        /// </summary>
        public int ChosenTactic
        {
            get { return tacticKeys.IndexOf(TeamSpawn.Spawn.Tactic); }
            set { this.RaiseAndSetIfChanged(ref TeamSpawn.Spawn.Tactic, tacticKeys[value]); }
        }

        /// <summary>
        /// Gets or sets the chosen team member role index for this spawn.
        /// </summary>
        public int ChosenRole
        {
            get { return Roles.IndexOf(TeamSpawn.Role.ToLocal()); }
            set { this.RaiseAndSetIfChanged(ref TeamSpawn.Role,  (TeamMemberSpawn.MemberRole) value); }
        }

        /// <summary>
        /// Backing field for the FocusIndex property.
        /// </summary>
        private int _focusIndex;

        /// <summary>
        /// Gets or sets the index of the currently focused UI element.
        /// Used to determine which data grid to display.
        /// </summary>
        public int FocusIndex
        {
            get => _focusIndex;
            set => _focusIndex = value;
        }
        
        /// <summary>
        /// Sets the focus index and updates the data grid display accordingly.
        /// </summary>
        /// <param name="index">The new focus index.</param>
        public void SetFocusIndex(int index)
        {
            FocusIndex = index;
            UpdateDataGrid();
        }

        /// <summary>
        /// Backing field for available AI tactic keys.
        /// </summary>
        private List<string> tacticKeys;

        /// <summary>
        /// Backing field for available monster species keys.
        /// </summary>
        private List<string> monsterKeys;

        /// <summary>
        /// Backing field for available skin keys.
        /// </summary>
        private List<string> skinKeys;

        /// <summary>
        /// Backing field for available gender values.
        /// </summary>
        private List<int> genderKeys;

        /// <summary>
        /// Backing field for available intrinsic ability keys.
        /// </summary>
        private List<string> intrinsicKeys;

        /// <summary>
        /// Backing field for available skill keys.
        /// </summary>
        private List<string> skillKeys;

        /// <summary>
        /// Collection of all available monster forms.
        /// </summary>
        private List<BaseMonsterFormViewModel> monsterForms;

        /// <summary>
        /// Collection of all available skills.
        /// </summary>
        private List<SkillDataViewModel> skillData;

        /// <summary>
        /// Collection of all available intrinsic abilities.
        /// </summary>
        private List<IntrinsicViewModel> intrinsicData;

        /// <summary>
        /// The indices of skills assigned to each of the 4 skill slots. -1 indicates no skill assigned.
        /// </summary>
        public List<int> SkillIndex;

        /// <summary>
        /// Gets or sets the collection of available AI tactics.
        /// </summary>
        public ObservableCollection<string> Tactics { get; set; }

        /// <summary>
        /// Gets or sets the collection of available skins.
        /// </summary>
        public ObservableCollection<string> Skins { get; set; }

        /// <summary>
        /// Gets or sets the collection of available genders.
        /// </summary>
        public ObservableCollection<string> Genders { get; set; }

        /// <summary>
        /// Gets or sets the collection of available team member roles.
        /// </summary>
        public ObservableCollection<string> Roles { get; set; }

        /// <summary>
        /// Gets or sets the filtered collection of monster forms matching the current search.
        /// </summary>
        public ObservableCollection<BaseMonsterFormViewModel> FilteredMonsterForms { get; set; }

        /// <summary>
        /// Gets or sets the filtered collection of skills matching the current search.
        /// </summary>
        public ObservableCollection<SkillDataViewModel> FilteredSkillData { get; set; }

        /// <summary>
        /// Gets or sets the filtered collection of intrinsic abilities matching the current search.
        /// </summary>
        public ObservableCollection<IntrinsicViewModel> FilteredIntrinsicData { get; set; }

        /// <summary>
        /// Initializes the available genders from the data manager.
        /// </summary>
        private void InitializeGenders()
        {
            Genders = new ObservableCollection<string>();
            genderKeys = new List<int>();

            for (int ii = -1; ii <= (int)Gender.Female; ii++)
            {
                Genders.Add(((Gender)ii).ToLocal());
                genderKeys.Add(ii);
            }


        }

        /// <summary>
        /// Initializes the available intrinsics from the data manager.
        /// </summary>
        private void InitializeIntrinsics()
        {
            FilteredIntrinsicData = new ObservableCollection<IntrinsicViewModel>();
            PossibleIntrinsicIndexes = new List<int>();
            intrinsicData = new List<IntrinsicViewModel>();
            intrinsicKeys = new List<string>();
            Dictionary<string, string> intrinsicNames = DataManager.Instance.DataIndices[DataManager.DataType.Intrinsic].GetLocalStringArray(true);

            int ii = 0;
            foreach (string key in intrinsicNames.Keys)
            {
                intrinsicKeys.Add(key);
                intrinsicData.Add(new IntrinsicViewModel(key, ii));
                ii++;
            }

        }

        /// <summary>
        /// Replaces a skill in the specified slot with the given skill index.
        /// </summary>
        /// <param name="index">The skill index to assign.</param>
        /// <param name="skillIndex">The slot index to replace, or -1 to use the focused slot.</param>
        public void ReplaceSkillIndex(int index, int skillIndex = -1)
        {
            if (skillIndex != -1)
            {
                SkillIndex[skillIndex] = index;
            }
            else
            {
                SkillIndex[FocusedSkillIndex] = index;

            }

            TeamSpawn.Spawn.SpecifiedSkills = getSkillList();
        }

        /// <summary>
        /// Sets a skill in the specified slot and updates the search filter to match.
        /// </summary>
        /// <param name="skillData">The skill to assign.</param>
        /// <param name="ind">The slot index to set, or -1 to use the focused slot.</param>
        public void SetSkill(SkillDataViewModel skillData, int ind = -1)
        {
            ReplaceSkillIndex(skillData.Index, ind);

            int index = ind;
            
            if (ind == -1)
            {
                index = FocusedSkillIndex;
            }
            
            if (index == 0)
            {
                SearchSkill0Filter = skillData.Name;
            } else if (index == 1)
            {
                SearchSkill1Filter = skillData.Name;
            } else if (index == 2)
            {
                SearchSkill2Filter = skillData.Name;
            } else if (index == 3)
            {
                SearchSkill3Filter = skillData.Name;
            }
        }

        /// <summary>
        /// Sets the selected ability and updates the search filter to match.
        /// </summary>
        /// <param name="vm">The ability view model to select.</param>
        public void SetIntrinsic(IntrinsicViewModel vm)
        {
            SelectedIntrinsicIndex = vm.Index;
            SearchIntrinsicFilter = vm.Name;
            TeamSpawn.Spawn.Intrinsic = intrinsicKeys[vm.Index];
        }

        /// <summary>
        /// Resets the selected intrinsic to none.
        /// </summary>
        public void ResetIntrinsic()
        {
            SelectedIntrinsicIndex = -1;
        }

        /// <summary>
        /// Resets the skill in the specified slot to none.
        /// </summary>
        /// <param name="skillSlot">The slot index to reset, or -1 to use the focused slot.</param>
        public void ResetSkill(int skillSlot = -1)
        {
            ReplaceSkillIndex(-1, skillSlot);
        }

        /// <summary>
        /// Gets the skill data view model at the specified index.
        /// </summary>
        /// <param name="index">The index of the skill.</param>
        /// <returns>The skill data view model.</returns>
        public SkillDataViewModel SkillDataAtIndex(int index)
        {
            return skillData[index];
        }

        /// <summary>
        /// Gets the monster form view model at the specified index.
        /// </summary>
        /// <param name="index">The index of the monster form.</param>
        /// <returns>The monster form view model.</returns>
        public BaseMonsterFormViewModel MonsterAtIndex(int index)
        {
            return monsterForms[index];
        }

        /// <summary>
        /// Gets the intrinsic ability view model at the specified index.
        /// </summary>
        /// <param name="index">The index of the intrinsic.</param>
        /// <returns>The intrinsic view model.</returns>
        public IntrinsicViewModel IntrinsicAtIndex(int index)
        {
            return intrinsicData[index];
        }
        private void InitializeSkills()
        {
            PossibleSkillIndexes = new List<int>();
            skillData = new List<SkillDataViewModel>();
            FilteredSkillData = new ObservableCollection<SkillDataViewModel>();
            skillKeys = new List<string>();
            Dictionary<string, string> skillNames = DataManager.Instance.DataIndices[DataManager.DataType.Skill].GetLocalStringArray(true);
            
            int ii = 0;
            foreach (string key in skillNames.Keys)
            {
                skillKeys.Add(key);
                skillData.Add(new SkillDataViewModel(key, ii));
                ii++;
            }
            SkillIndex = new List<int> { -1, -1, -1, -1 };
        }

        private void InitializeMonsters()
        {
            monsterKeys = new List<string>();
            Dictionary<string, string> monsterNames = DataManager.Instance.DataIndices[DataManager.DataType.Monster].GetLocalStringArray(true);
            List<BaseMonsterFormViewModel> forms = new List<BaseMonsterFormViewModel>();

            int index = 0;
            foreach (string key in monsterNames.Keys)
            {
                monsterKeys.Add(key);
                MonsterEntrySummary summary = (MonsterEntrySummary)DataManager.Instance.DataIndices[DataManager.DataType.Monster].Get(key);
               
                for (int jj = 0; jj < summary.Forms.Count; jj++)
                {
              
                    forms.Add(new BaseMonsterFormViewModel(key, jj, index));
                    index += 1;
                }
            }
            
            monsterForms = new List<BaseMonsterFormViewModel>(forms);
            FilteredMonsterForms = new ObservableCollection<BaseMonsterFormViewModel>();
        }

        
        private void InitializeSkins()
        {
            Skins = new ObservableCollection<string>();
            skinKeys = new List<string>();
            Skins.Add("**EMPTY**");
            skinKeys.Add("");
            
            Dictionary<string, string> skin_names = DataManager.Instance.DataIndices[DataManager.DataType.Skin].GetLocalStringArray(true);
            foreach (string key in skin_names.Keys)
            {
                Skins.Add(skin_names[key]);
                skinKeys.Add(key);
            }
        }
        
        private void InitializeTactics()
        {
            Tactics = new ObservableCollection<string>();
            Dictionary<string, string> tacticNames = DataManager.Instance.DataIndices[DataManager.DataType.AI].GetLocalStringArray();

            tacticKeys = new List<string>();

            foreach (string key in tacticNames.Keys)
            {
                tacticKeys.Add(key);
                Tactics.Add(tacticNames[key]);
            }

        }

        private void InitializeRoles()
        {   
            Roles = new ObservableCollection<string>();
            for (int ii = 0; ii <= (int)TeamMemberSpawn.MemberRole.Loner; ii++) {
                Roles.Add(((TeamMemberSpawn.MemberRole)ii).ToLocal());
            }
        }
        
     

        /// <summary>
        /// Initializes all data collections (monsters, skills, abilities, etc.) for the editor.
        /// </summary>
        public void Initialize()
        {
            InitializeMonsters();
            InitializeGenders();
            InitializeIntrinsics();
            InitializeSkills();
            InitializeSkins();
            InitializeTactics();
            InitializeRoles();
            DevForm devForm = (DevForm)DiagManager.Instance.DevEditor;
            SpawnConditions = new CollectionBoxViewModel(devForm, new StringConv(typeof(MobSpawnCheck), new object[0]));
            SpawnConditions.OnMemberChanged += SpawnConditionsChanged;
            SpawnConditions.OnEditItem += SpawnConditionsEditItem;
            SpawnConditions.LoadFromList(TeamSpawn.Spawn.SpawnConditions);
            
            SpawnFeatures = new CollectionBoxViewModel(devForm, new StringConv(typeof(MobSpawnExtra), new object[0]));
            SpawnFeatures.OnMemberChanged += SpawnFeaturesChanged;
            SpawnFeatures.OnEditItem += SpawnFeaturesEditItem;
            SpawnFeatures.LoadFromList(TeamSpawn.Spawn.SpawnFeatures);
        }


        /// <summary>
        /// The list of skill indices that the current monster form can learn.
        /// </summary>
        public List<int> PossibleSkillIndexes;
        
        private void SetFormPossibleSkills(string species, int formId)
        {

            foreach (int index in PossibleSkillIndexes)
            {
                skillData[index].SetMonsterLearns(false);
            }
            PossibleSkillIndexes.Clear();
            
            MonsterData entry = DataManager.Instance.GetMonster(species);
            MonsterFormData form = (MonsterFormData) entry.Forms[formId];
            List<string> possibleSkills = form.GetPossibleSkills();
            foreach (string skill in possibleSkills)
            {
                int index = skillKeys.BinarySearch(skill);
                skillData[index].SetMonsterLearns(true);
                PossibleSkillIndexes.Add(index);
            }
        }

        /// <summary>
        /// The list of intrinsic ability indices that the current monster form can have.
        /// </summary>
        public List<int> PossibleIntrinsicIndexes;
        private void SetFormPossibleIntrinsics(string species, int formId)
        {
            foreach (int index in PossibleIntrinsicIndexes)
            {
                intrinsicData[index].SetMonsterLearns(false);
            }
            PossibleIntrinsicIndexes.Clear();
            
            MonsterData entry = DataManager.Instance.GetMonster(species);
            BaseMonsterForm form = entry.Forms[formId];

            List<string> possibleIntrinsics = new List<string>(
                    new string[] { form.Intrinsic1, form.Intrinsic2, form.Intrinsic3 })
                .Where(intrinsic => intrinsic != "none")
                .ToList();
            foreach (string intrinsic in possibleIntrinsics)
            {
                int index = intrinsicKeys.BinarySearch(intrinsic);
                intrinsicData[index].SetMonsterLearns(true);
                PossibleIntrinsicIndexes.Add(index);

            }
        }
        
        /// <summary>
        /// Called when spawn conditions are modified. Updates the underlying data.
        /// </summary>
        public void SpawnConditionsChanged()
        {
            TeamSpawn.Spawn.SpawnConditions = SpawnConditions.GetList<List<MobSpawnCheck>>();
        }
        
        /// <summary>
        /// Called when spawn features are modified. Updates the underlying data and UI toggles.
        /// </summary>
        public void SpawnFeaturesChanged()
        {
            TeamSpawn.Spawn.SpawnFeatures = SpawnFeatures.GetList<List<MobSpawnExtra>>();
            _checkSpawnFeatureDiff = false;
            
            // prevent an infinite loop caused by the change 
            Unrecruitable = containsType<MobSpawnExtra, MobSpawnUnrecruitable>(TeamSpawn.Spawn.SpawnFeatures);
            DisableUnusedSlots = containsType<MobSpawnExtra, MobSpawnMovesOff>(TeamSpawn.Spawn.SpawnFeatures);
            IsWeakMob = containsType<MobSpawnExtra, MobSpawnWeak>(TeamSpawn.Spawn.SpawnFeatures);
            _checkSpawnFeatureDiff = true;
        }


        /// <summary>
        /// Opens an editor for a spawn condition item.
        /// </summary>
        /// <param name="index">The index of the condition to edit.</param>
        /// <param name="element">The condition element to edit.</param>
        /// <param name="advancedEdit">Whether to use advanced editing mode.</param>
        /// <param name="op">The callback to apply the edit.</param>
        public void SpawnConditionsEditItem(int index, object element, bool advancedEdit, CollectionBoxViewModel.EditElementOp op)
        {
            string elementName = "Spawn Conditions[" + index + "]";
            DataEditForm frmData = new DataEditRootForm();
            frmData.Title = DataEditor.GetWindowTitle("Spawn", elementName, element, typeof(MobSpawnCheck), new object[0]);

            DataEditor.LoadClassControls(frmData.ControlPanel, "Spawn", null, elementName, typeof(MobSpawnCheck), new object[0], element, true, new Type[0], advancedEdit);
            DataEditor.TrackTypeSize(frmData, typeof(MobSpawnCheck));
            
            frmData.SelectedOKEvent += async () =>
            {
                element = DataEditor.SaveClassControls(frmData.ControlPanel, elementName, typeof(MobSpawnCheck), new object[0], true, new Type[0], advancedEdit);
                op(index, element);
                return true;
            };
            
            frmData.Show();
        }
        
        /// <summary>
        /// Opens an editor for a spawn feature item.
        /// </summary>
        /// <param name="index">The index of the feature to edit.</param>
        /// <param name="element">The feature element to edit.</param>
        /// <param name="advancedEdit">Whether to use advanced editing mode.</param>
        /// <param name="op">The callback to apply the edit.</param>
        public void SpawnFeaturesEditItem(int index, object element, bool advancedEdit, CollectionBoxViewModel.EditElementOp op)
        {
            string elementName = "Spawn Features[" + index + "]";
            DataEditForm frmData = new DataEditRootForm();
            frmData.Title = DataEditor.GetWindowTitle("Spawn", elementName, element, typeof(MobSpawnExtra), new object[0]);

            DataEditor.LoadClassControls(frmData.ControlPanel, "Spawn", null, elementName, typeof(MobSpawnExtra), new object[0], element, true, new Type[0], advancedEdit);
            DataEditor.TrackTypeSize(frmData, typeof(MobSpawnExtra));
            
            frmData.SelectedOKEvent += async () =>
            {
                element = DataEditor.SaveClassControls(frmData.ControlPanel, elementName, typeof(MobSpawnExtra), new object[0], true, new Type[0], advancedEdit);
                op(index, element);
                return true;
            };
            
            frmData.Show();
        }
        /// <summary>
        /// Creates a new TeamMemberSpawnModel with default values.
        /// </summary>
        public TeamMemberSpawnModel()
        {
            TeamSpawn = new TeamMemberSpawn();
            TeamSpawn.Spawn = new MobSpawn();
            TeamSpawn.Spawn.Level.Min = 1;
            TeamSpawn.Spawn.Level.Max = 1;
            Initialize();
            ChosenTactic = tacticKeys.BinarySearch("wander_normal");
            MonsterData entry = DataManager.Instance.GetMonster("missingno");
            BaseMonsterForm form = entry.Forms[0];
            SearchMonsterFilter = form.FormName.ToLocal();
            SelectedMonsterIndex = findMonsterForm("missingno", 0);
        }

        /// <summary>
        /// Creates a new TeamMemberSpawnModel from an existing spawn configuration.
        /// </summary>
        /// <param name="spawn">The spawn configuration to edit.</param>
        public TeamMemberSpawnModel(TeamMemberSpawn spawn)
        {

            string species = spawn.Spawn.BaseForm.Species == "" ? "missingno" : spawn.Spawn.BaseForm.Species;
            MonsterData entry = DataManager.Instance.GetMonster(species);
            BaseMonsterForm form = entry.Forms[spawn.Spawn.BaseForm.Form];
            TeamSpawn = spawn;
            
            Initialize();
            
            _unrecruitable = containsType<MobSpawnExtra, MobSpawnUnrecruitable>(SpawnFeatures.GetList<List<MobSpawnExtra>>());
            _disableUnusedSlots = containsType<MobSpawnExtra, MobSpawnMovesOff>(SpawnFeatures.GetList<List<MobSpawnExtra>>());
            _isWeakMonster = containsType<MobSpawnExtra, MobSpawnWeak>(SpawnFeatures.GetList<List<MobSpawnExtra>>());
            
            _selectedMonsterIndex = findMonsterForm(TeamSpawn.Spawn.BaseForm.Species, TeamSpawn.Spawn.BaseForm.Form);
            SelectedMonsterForm = monsterForms[_selectedMonsterIndex];

            SearchMonsterFilter = form.FormName.ToLocal();
            
            if (TeamSpawn.Spawn.Intrinsic != "")
            {
                SelectedIntrinsicIndex = intrinsicKeys.BinarySearch(spawn.Spawn.Intrinsic);
                SearchIntrinsicFilter = DataManager.Instance.GetIntrinsic(spawn.Spawn.Intrinsic).Name.ToLocal();
            }
            
            SetFormPossibleSkills(TeamSpawn.Spawn.BaseForm.Species, TeamSpawn.Spawn.BaseForm.Form);
            SetFormPossibleIntrinsics(TeamSpawn.Spawn.BaseForm.Species, TeamSpawn.Spawn.BaseForm.Form);
            updateIntrinsicData(SearchIntrinsicFilter);
            
            for (int ii = 0; ii < CharData.MAX_SKILL_SLOTS; ii++)
            {
                string filter = "";
                if (ii < spawn.Spawn.SpecifiedSkills.Count)
                {
                    string skillKey = spawn.Spawn.SpecifiedSkills[ii];
                    filter = DataManager.Instance.GetSkill(skillKey).Name.ToLocal();
                    int index = skillKeys.BinarySearch(skillKey);
                    SkillIndex[ii] = index;
                }
                
                if (ii == 0)
                {
                    _searchSkill0Filter = filter;
                }
                else if (ii == 1)
                {
                    _searchSkill1Filter = filter;
                } 
                else if (ii == 2)
                {
                    _searchSkill2Filter = filter;
                    
                }
                else if (ii == 3)
                {
                    _searchSkill3Filter = filter;
                }
            }
        }

        /// <summary>
        /// Determines if a string should be included in filtered results based on the filter.
        /// </summary>
        /// <param name="s">The string to test.</param>
        /// <param name="filter">The filter text.</param>
        /// <returns>True if the string matches the filter criteria.</returns>
        public bool ShouldAddToFilter(string s, string filter)
        {
            return prefixStartsWith(s, filter) || startsWith(s, filter);
        }

        private bool containsType<T, V>(List<T> collection)
        {
            bool result = false;

            foreach (T item in collection)
            {
                if (item is V)
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        private bool _finishedAdding;

        /// <summary>
        /// Gets whether the collection has finished adding filtered items.
        /// Used to optimize UI updates during batch additions.
        /// </summary>
        public bool FinishedAdding
        {
            get => _finishedAdding;
        }
        private void addFilteredItems<T>(ObservableCollection<T> collection, IEnumerable<T> items)
        {
            if (items.Count() > 0)
            {
                _finishedAdding = false;
                foreach (T item in items.Take(items.Count() - 1))
                {
                    collection.Add(item);
                }

                _finishedAdding = true;
                collection.Add(items.Last());
            }
        }
        
        private List<string> getSkillList()
        {
            return SkillIndex.Where(i => i != -1).Select(i => skillKeys[i]).ToList();
        }
        
        private int findMonsterForm(string species, int form)
        {
            
            int result = -1;
            for (int ii = 0; ii < monsterForms.Count(); ii++)
            {
                BaseMonsterFormViewModel vm = monsterForms[ii];
                if (species == vm.Key && form == vm.FormId)
                {
                    result = ii;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Compares two strings for equality after sanitizing (lowercasing and removing whitespace).
        /// </summary>
        /// <param name="s1">The first string.</param>
        /// <param name="s2">The second string.</param>
        /// <returns>True if the sanitized strings are equal.</returns>
        public bool SanitizeStringEquals(string s1, string s2)
        {
            return sanitize(s1) == sanitize(s2);
        }

        private string sanitize(string s)
        {
            s = s.ToLower();
            s = Regex.Replace(s, @"\s+|_", "");
            return s;
        }

        private bool startsWith(string s, string filter)
        {
            return sanitize(s).StartsWith(sanitize(filter));
        }

        private bool prefixStartsWith(string s, string filter)
        {
            return s.Split(' ', '-').Any(prefix =>
                sanitize(prefix).StartsWith(sanitize(filter), StringComparison.OrdinalIgnoreCase)
            );
        }

        /// <summary>
        /// Updates which data grid is displayed based on the current focus index.
        /// </summary>
        public void UpdateDataGrid()
        {
            DataGridType nextView = CurrentDataGridView;
            if (FocusIndex >= 6)
            {
                // nextView = DataGridType.Other;
            }
            else if (FocusIndex == 5)
            {
                nextView = DataGridType.Intrinsic;
            }
            else if (FocusIndex >= 1)
            {
                nextView = DataGridType.Skills;
            }
            else if (FocusIndex == 0)
            {
                nextView = DataGridType.Monster;
            }

            CurrentDataGridView = nextView;
        }

    }
    
    /// <summary>
    /// Specifies which data grid is currently displayed in the spawn editor.
    /// </summary>
    public enum DataGridType
    {
        /// <summary>Monster selection grid.</summary>
        Monster = 0,
        /// <summary>Skill selection grid.</summary>
        Skills = 1,
        /// <summary>Ability selection grid.</summary>
        Intrinsic = 2,
        /// <summary>Other data grid.</summary>
        Other = 3,
    }

}