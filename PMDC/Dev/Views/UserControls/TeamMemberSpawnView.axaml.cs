using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using PMDC.Dev.ViewModels;
using RogueEssence.Dev;

namespace PMDC.Dev.Views
{
    /// <summary>
    /// Avalonia UserControl for editing team member spawn configurations.
    /// Provides an interactive form with search-enabled dropdowns for selecting
    /// monster species, skills, abilities, and configuring spawn parameters.
    /// </summary>
    public class TeamMemberSpawnView : UserControl
    {
        /// <summary>
        /// Defines the tab order for focus navigation between input fields.
        /// Order: Species, Skill 0-3, Intrinsic, Min level, Max level.
        /// </summary>
        private List<string> _focusOrder = new List<string> {
            "SpeciesTextBox", "SkillTextBox0", "SkillTextBox1",
            "SkillTextBox2", "SkillTextBox3", "IntrinsicTextBox",
            "MinTextBox", "MaxTextBox"
        };


        /// <summary>
        /// Initializes a new instance of the TeamMemberSpawnView control.
        /// </summary>
        public TeamMemberSpawnView()
        {
            this.InitializeComponent();
        }

        /// <inheritdoc/>
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

        }
        
        /// <inheritdoc/>
        /// <remarks>
        /// Subscribes to collection changed events for filtered monster forms, skills, and intrinsics
        /// to update UI highlights when data collections are updated.
        /// </remarks>
        protected override void OnInitialized()
        {
            base.OnInitialized();
            if (DataContext is TeamMemberSpawnModel vm)
            {
                vm.FilteredSkillData.CollectionChanged += FilteredSkillDataOnCollectionChanged;
                vm.FilteredMonsterForms.CollectionChanged += MonsterDataOnCollectionChanged;
                vm.FilteredIntrinsicData.CollectionChanged += IntrinsicDataOnCollectionChanged;
            }
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Unsubscribes from collection changed events to prevent memory leaks and ensure proper cleanup.
        /// </remarks>
        /// <param name="e">The event arguments.</param>
        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);
            if (DataContext is TeamMemberSpawnModel vm)
            {
                vm.FilteredSkillData.CollectionChanged -= FilteredSkillDataOnCollectionChanged;
                vm.FilteredMonsterForms.CollectionChanged -= MonsterDataOnCollectionChanged;
                vm.FilteredIntrinsicData.CollectionChanged -= IntrinsicDataOnCollectionChanged;
            }
        }

        /// <summary>
        /// Handles collection changes in the filtered monster forms collection.
        /// Updates the monster highlight if data loading is complete.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The collection change event arguments.</param>
        private void MonsterDataOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            TeamMemberSpawnModel vm = DataContext as TeamMemberSpawnModel;
            if (vm.FinishedAdding)
            {
                UpdateMonsterHighlight();
            }
        }

        /// <summary>
        /// Handles collection changes in the filtered skill data collection.
        /// Updates skill highlights if data loading is complete.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The collection change event arguments.</param>
        private void FilteredSkillDataOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            TeamMemberSpawnModel vm = DataContext as TeamMemberSpawnModel;
            if (vm.FinishedAdding)
            {
                UpdateSkillHighlights();
            }
        }

        /// <summary>
        /// Handles collection changes in the filtered intrinsic data collection.
        /// Updates intrinsic highlights if data loading is complete.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The collection change event arguments.</param>
        private void IntrinsicDataOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            TeamMemberSpawnModel vm = DataContext as TeamMemberSpawnModel;
            if (vm.FinishedAdding)
            {
                UpdateIntrinsicHighlights();
            }
        }
        
        /// <summary>
        /// Advances focus to the next input field in the tab order.
        /// </summary>
        /// <returns>The next focused control (either TextBox or NumericUpDown).</returns>
        private TemplatedControl FocusNextTextBox()
        {
            TeamMemberSpawnModel vm = (TeamMemberSpawnModel) DataContext;
            vm.SetFocusIndex(Math.Min(vm.FocusIndex + 1, _focusOrder.Count - 1));
            string nextTextbox = _focusOrder[vm.FocusIndex];

            TemplatedControl nextFocus;

            if (vm.FocusIndex < 6) {
                nextFocus = this.FindControl<TextBox>(nextTextbox);
            }
            else
            {
                nextFocus = this.FindControl<NumericUpDown>(nextTextbox);
            }

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                nextFocus.Focus();
            });
            return nextFocus;
        }

        /// <summary>
        /// Handles the Enter key press by moving focus to the next field and selecting all text if it's a TextBox.
        /// </summary>
        private void OnEnter()
        {
            TemplatedControl nextFocus = FocusNextTextBox();

            if (nextFocus is TextBox nf)
            {

                nf.CaretIndex = nf.Text.Length;
                nf.SelectionStart = 0;
                nf.SelectionEnd = nf.Text.Length;
            }
        }
        
        /// <summary>
        /// Handles Enter key in the species TextBox. Selects the first filtered monster form if a filter is applied.
        /// TODO: Allow the user to choose the row with arrow keys.
        /// </summary>
        private void SpeciesTextBox_OnEnterCommand()
        {
            TeamMemberSpawnModel vm = (TeamMemberSpawnModel) DataContext;

            if (vm.FilteredMonsterForms.Count > 0)
            {
                if (vm.SearchMonsterFilter != "")
                {
                    vm.SetMonster(vm.FilteredMonsterForms.First().Index);
                }
            }

            OnEnter();
        }

        /// <summary>
        /// Handles Enter key in a skill TextBox. Selects the first filtered skill if a filter is applied,
        /// otherwise resets the skill selection.
        /// </summary>
        private void SkillTextBox_OnEnterCommand()
        {
            TeamMemberSpawnModel vm = DataContext as TeamMemberSpawnModel;
            if (vm.FilteredSkillData.Count > 0)
            {
                if (vm.CurrentSkillSearchFilter != "")
                {
                    vm.SetSkill(vm.FilteredSkillData.First());
                }
                else
                {
                    vm.ResetSkill();
                }
            }
            OnEnter();
        }

        /// <summary>
        /// Handles Enter key in the intrinsic TextBox. Selects the first filtered intrinsic if a filter is applied,
        /// otherwise resets the skill selection.
        /// </summary>
        private void IntrinsicTextBox_OnEnterCommand()
        {
            TeamMemberSpawnModel vm = DataContext as TeamMemberSpawnModel;
            if (vm.FilteredIntrinsicData.Count > 0)
            {
                if (vm.SearchIntrinsicFilter != "")
                {
                    vm.SetIntrinsic(vm.FilteredIntrinsicData.First());
                }
                else
                {
                    vm.ResetSkill();
                }
            }
            OnEnter();
        }
        
        /// <summary>
        /// Handles focus gain on the species TextBox. Updates focus index to 0.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The focus event arguments.</param>
        private void SpeciesTextBox_OnGotFocus(object sender, GotFocusEventArgs e)
        {
            TeamMemberSpawnModel vm = DataContext as TeamMemberSpawnModel;
            vm.SetFocusIndex(0);
        }

        /// <summary>
        /// Handles focus gain on the first skill TextBox. Updates focus index, sets the active skill slot,
        /// and refreshes the filtered skill data display.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The focus event arguments.</param>
        private void SkillTextBox0_OnGotFocus(object sender, GotFocusEventArgs e)
        {
            TeamMemberSpawnModel vm = DataContext as TeamMemberSpawnModel;
            vm.SetFocusIndex(1);
            vm.FocusedSkillIndex = 0;
            vm.UpdateSkillData(vm.SearchSkill0Filter);
            UpdateSkillHighlights();
        }

        /// <summary>
        /// Handles focus gain on the second skill TextBox. Updates focus index, sets the active skill slot,
        /// and refreshes the filtered skill data display.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The focus event arguments.</param>
        private void SkillTextBox1_OnGotFocus(object sender, GotFocusEventArgs e)
        {
            TeamMemberSpawnModel vm = DataContext as TeamMemberSpawnModel;
            vm.SetFocusIndex(2);
            vm.FocusedSkillIndex = 1;
            vm.UpdateSkillData(vm.SearchSkill1Filter);
            UpdateSkillHighlights();
        }

        /// <summary>
        /// Handles focus gain on the third skill TextBox. Updates focus index, sets the active skill slot,
        /// and refreshes the filtered skill data display.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The focus event arguments.</param>
        private void SkillTextBox2_OnGotFocus(object sender, GotFocusEventArgs e)
        {
            TeamMemberSpawnModel vm = DataContext as TeamMemberSpawnModel;
            vm.SetFocusIndex(3);
            vm.FocusedSkillIndex = 2;
            vm.UpdateSkillData(vm.SearchSkill2Filter);
            UpdateSkillHighlights();;
        }

        /// <summary>
        /// Handles focus gain on the fourth skill TextBox. Updates focus index, sets the active skill slot,
        /// and refreshes the filtered skill data display.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The focus event arguments.</param>
        private void SkillTextBox3_OnGotFocus(object sender, GotFocusEventArgs e)
        {
            TeamMemberSpawnModel vm = DataContext as TeamMemberSpawnModel;
            vm.SetFocusIndex(4);
            vm.FocusedSkillIndex = 3;
            vm.UpdateSkillData(vm.SearchSkill3Filter);
            UpdateSkillHighlights();
        }

        /// <summary>
        /// Handles focus gain on the intrinsic TextBox. Updates focus index to 5.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The focus event arguments.</param>
        private void IntrinsicTextBox_OnGotFocus(object sender, GotFocusEventArgs e)
        {
            TeamMemberSpawnModel vm = DataContext as TeamMemberSpawnModel;
            vm.SetFocusIndex(5);
        }

        /// <summary>
        /// Handles focus gain on the minimum level NumericUpDown.
        /// TODO: Currently NumericUpDown doesn't get focused correctly, so focus index update is disabled.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The focus event arguments.</param>
        private void MinTextBox_OnGotFocus(object sender, GotFocusEventArgs e)
        {
            // TeamMemberSpawnModel vm = DataContext as TeamMemberSpawnModel;
            // vm.SetFocusIndex(6);
        }

        /// <summary>
        /// Handles focus gain on the maximum level NumericUpDown.
        /// TODO: Currently NumericUpDown doesn't get focused correctly, so focus index update is disabled.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The focus event arguments.</param>
        private void MaxTextBox_OnGotFocus(object sender, GotFocusEventArgs e)
        {
            // TeamMemberSpawnModel vm = DataContext as TeamMemberSpawnModel;
            // vm.SetFocusIndex(7);
        }
        /// <summary>
        /// Handles focus loss on the species TextBox. If the input matches a filtered monster form,
        /// it selects that monster.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void SpeciesTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            TeamMemberSpawnModel vm = DataContext as TeamMemberSpawnModel;
            int currFocusIndex = vm.FocusIndex;
            Dispatcher.UIThread.Post(() =>
            {
                if (vm.FilteredMonsterForms.Count > 0 && vm.FocusIndex != currFocusIndex)
                {
                    // if (vm.SearchMonsterFilter == "")
                    // {
                    //     vm.ResetMonster();
                    // }
                    if (vm.SanitizeStringEquals(vm.SearchMonsterFilter, vm.FilteredMonsterForms.First().Name))
                    {
                        vm.SetMonster(vm.FilteredMonsterForms.First().Index);
                    }
                }
            });
        }

        /// <summary>
        /// Handles focus loss on the first skill TextBox. Delegates to the generic handler.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void SkillTextBox0_OnLostFocus(object sender, RoutedEventArgs e)
        {
            SkillTextBox_OnLostFocus(0);
        }

        /// <summary>
        /// Handles focus loss on the second skill TextBox. Delegates to the generic handler.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void SkillTextBox1_OnLostFocus(object sender, RoutedEventArgs e)
        {
            SkillTextBox_OnLostFocus(1);
        }

        /// <summary>
        /// Handles focus loss on the third skill TextBox. Delegates to the generic handler.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void SkillTextBox2_OnLostFocus(object sender, RoutedEventArgs e)
        {
            SkillTextBox_OnLostFocus(2);
        }

        /// <summary>
        /// Handles focus loss on the fourth skill TextBox. Delegates to the generic handler.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void SkillTextBox3_OnLostFocus(object sender, RoutedEventArgs e)
        {
            SkillTextBox_OnLostFocus(3);
        }

        /// <summary>
        /// Generic handler for focus loss on skill TextBoxes. If the input matches a filtered skill,
        /// it selects that skill; otherwise, resets the skill if the filter is empty.
        /// </summary>
        /// <param name="index">The skill slot index (0-3).</param>
        private void SkillTextBox_OnLostFocus(int index)
        {
            TeamMemberSpawnModel vm = DataContext as TeamMemberSpawnModel;
            SkillDataViewModel skillData = null;

            int currFocusIndex = vm.FocusIndex;
            if (vm.FilteredSkillData.Count > 0)
            {
                skillData = vm.FilteredSkillData.First();
            }

            List<string> skillFilters = new List<string>{ vm.SearchSkill0Filter, vm.SearchSkill1Filter, vm.SearchSkill2Filter, vm.SearchSkill3Filter };
            string filter = skillFilters[index];
            Dispatcher.UIThread.Post(() =>
            {
                if (skillData != null && vm.FocusIndex != currFocusIndex)
                {
                    if (filter == "")
                    {
                        vm.ResetSkill(index);
                    }

                    else if (vm.SanitizeStringEquals(filter, skillData.Name))
                    {
                        vm.SetSkill(skillData, index);
                    }
                }
            });
        }

        /// <summary>
        /// Handles focus loss on the intrinsic TextBox. If the input matches a filtered intrinsic,
        /// it selects that intrinsic; otherwise, resets the intrinsic if the filter is empty.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void IntrinsicTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            TeamMemberSpawnModel vm = DataContext as TeamMemberSpawnModel;

            int currFocusIndex = vm.FocusIndex;

            Dispatcher.UIThread.Post(() =>
            {
                if (vm.FilteredIntrinsicData.Count > 0 && vm.FocusIndex != currFocusIndex)
                {
                    if (vm.SearchIntrinsicFilter == "")
                    {
                        vm.ResetIntrinsic();
                    }
                    else if (vm.SanitizeStringEquals(vm.SearchIntrinsicFilter, vm.FilteredIntrinsicData.First().Name))
                    {
                        vm.SetIntrinsic(vm.FilteredIntrinsicData.First());
                    }
                }
            });
        }
        
        /// <summary>
        /// Handles cell click on the monster data grid. Selects the clicked monster and moves focus to the next field.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The data grid cell pointer press event arguments.</param>
        private void MonsterDataGrid_OnCellPointerPressed(object sender, DataGridCellPointerPressedEventArgs e)
        {
            TeamMemberSpawnModel vm = DataContext as TeamMemberSpawnModel;
            BaseMonsterFormViewModel dataContext = (BaseMonsterFormViewModel)e.Cell.DataContext;
            vm.SetMonster(dataContext.Index);
            FocusNextTextBox();
        }

        /// <summary>
        /// Handles cell click on the skills data grid. Selects the clicked skill and moves focus to the next field.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The data grid cell pointer press event arguments.</param>
        private void SkillsDataGrid_OnCellPointerPressed(object sender, DataGridCellPointerPressedEventArgs e)
        {
            TeamMemberSpawnModel vm = DataContext as TeamMemberSpawnModel;
            SkillDataViewModel skillData = (SkillDataViewModel)e.Cell.DataContext;
            vm.SetSkill(skillData);
            FocusNextTextBox();
        }

        /// <summary>
        /// Handles cell click on the intrinsic data grid. Selects the clicked intrinsic and moves focus to the next field.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The data grid cell pointer press event arguments.</param>
        private void IntrinsicDataGrid_OnCellPointerPressed(object sender, DataGridCellPointerPressedEventArgs e)
        {
            TeamMemberSpawnModel vm = DataContext as TeamMemberSpawnModel;
            IntrinsicViewModel intrinsicData = (IntrinsicViewModel)e.Cell.DataContext;
            vm.SetIntrinsic(intrinsicData);
            FocusNextTextBox();
        }
        
        /// <summary>
        /// Updates the monster data grid highlight to show the currently selected monster form.
        /// Only highlights if the selected monster passes filter and release status checks.
        /// </summary>
        private void UpdateMonsterHighlight()
        {
            TeamMemberSpawnModel vm = DataContext as TeamMemberSpawnModel;
            int index = vm.SelectedMonsterIndex;
            if (vm.FilteredMonsterForms.Count > 0 && vm.SearchMonsterFilter != null)
            {
                BaseMonsterFormViewModel monsterData = vm.MonsterAtIndex(index);
                if (vm.ShouldAddToFilter(monsterData.Name, vm.SearchMonsterFilter) &&
                    (vm.IncludeUnreleasedForms || monsterData.Released))
                {
                    vm.SelectedMonsterForm = monsterData;
                }
            }
        }

        /// <summary>
        /// Updates the skills data grid to highlight all currently selected skill slots.
        /// Clears existing selection and re-adds all valid selected skills that pass filter and release checks.
        /// </summary>
        private void UpdateSkillHighlights()
        {
            TeamMemberSpawnModel vm = DataContext as TeamMemberSpawnModel;
            DataGrid datagrid = this.FindControl<DataGrid>("SkillsDataGrid");
            datagrid.SelectedItems.Clear();
            foreach (int index in vm.SkillIndex)
            {
                if (index != -1 && vm.FilteredSkillData.Count > 0)
                {
                    SkillDataViewModel skillData = vm.SkillDataAtIndex(index);
                    if (vm.ShouldAddToFilter(skillData.Name, vm.CurrentSkillSearchFilter) && (vm.IncludeUnreleasedSkills || skillData.Released))
                    {
                        datagrid.SelectedItems.Add(skillData);
                    }
                }
            }

        }

        /// <summary>
        /// Updates the intrinsic data grid highlight to show the currently selected intrinsic.
        /// Only highlights if the selected intrinsic passes filter and release status checks.
        /// </summary>
        private void UpdateIntrinsicHighlights()
        {
            TeamMemberSpawnModel vm = DataContext as TeamMemberSpawnModel;
            int index = vm.SelectedIntrinsicIndex;
            if (vm.SelectedIntrinsicIndex != -1 && vm.FilteredIntrinsicData.Count > 0 && vm.SearchIntrinsicFilter != null)
            {
                IntrinsicViewModel intrinsicData = vm.IntrinsicAtIndex(index);
                if (vm.ShouldAddToFilter(intrinsicData.Name, vm.SearchIntrinsicFilter) && (vm.IncludeUnreleasedIntrinsics || intrinsicData.Released))
                {
                    vm.SelectedIntrinsic = intrinsicData;
                }
            }
        }

        /// <summary>
        /// Handles value changes on the minimum level NumericUpDown. Ensures max level is not lower than min level.
        /// </summary>
        /// <param name="sender">The minimum level NumericUpDown control.</param>
        /// <param name="e">The value changed event arguments.</param>
        private void MinTextBox_OnValueChanged(object sender, NumericUpDownValueChangedEventArgs e)
        {
            NumericUpDown nudMin = (NumericUpDown)sender;
            NumericUpDown nudMax = this.FindControl<NumericUpDown>("MaxTextBox");

            if (nudMin.Value > nudMax.Value)
                nudMax.Value = nudMin.Value;
        }

        /// <summary>
        /// Handles value changes on the maximum level NumericUpDown. Ensures min level is not higher than max level.
        /// </summary>
        /// <param name="sender">The maximum level NumericUpDown control.</param>
        /// <param name="e">The value changed event arguments.</param>
        private void MaxTextBox_OnValueChanged(object sender, NumericUpDownValueChangedEventArgs e)
        {
            NumericUpDown nudMin = this.FindControl<NumericUpDown>("MinTextBox");
            NumericUpDown nudMax = (NumericUpDown)sender;


            if (nudMin.Value > nudMax.Value)
                nudMin.Value = nudMax.Value;
        }
    }
}