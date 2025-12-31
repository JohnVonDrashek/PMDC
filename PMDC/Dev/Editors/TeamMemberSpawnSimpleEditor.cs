using System;
using System.Reflection;
using Avalonia.Controls;
using PMDC.Dev.ViewModels;
using PMDC.Dev.Views;
using RogueEssence.Dev;
using RogueEssence.LevelGen;

namespace PMDC.Dev
{
    /// <summary>
    /// Simple editor for <see cref="TeamMemberSpawn"/> objects that provides a streamlined UI for configuring
    /// team member spawns using the <see cref="TeamMemberSpawnView"/> custom control.
    /// </summary>
    /// <remarks>
    /// This editor extends the base <see cref="Editor{T}"/> class to provide specialized editing capabilities
    /// for team member spawn configurations. It uses a custom Avalonia view to present a user-friendly interface.
    /// </remarks>
    public class TeamMemberSpawnSimpleEditor : Editor<TeamMemberSpawn>
    {
        /// <summary>
        /// Gets a value indicating whether this editor uses a simple editing mode.
        /// </summary>
        /// <value>Always returns <c>true</c>, indicating this is a simplified editor interface.</value>
        public override bool SimpleEditor => true;

        /// <summary>
        /// Gets a display string for the team member spawn based on its spawn configuration.
        /// </summary>
        /// <remarks>
        /// This method uses reflection to retrieve the Spawn property of the TeamMemberSpawn object
        /// and generates a string representation by delegating to the DataEditor's GetString method.
        /// </remarks>
        /// <param name="obj">The team member spawn object to describe.</param>
        /// <param name="type">The type of the object being edited.</param>
        /// <param name="attributes">Custom attributes applied to the member.</param>
        /// <returns>A string describing the spawn configuration for display in the UI.</returns>
        public override string GetString(TeamMemberSpawn obj, Type type, object[] attributes)
        {
            MemberInfo[] spawnInfo = type.GetMember(nameof(obj.Spawn));
            return DataEditor.GetString(obj.Spawn, spawnInfo[0].GetMemberInfoType(), spawnInfo[0].GetCustomAttributes(false));
        }

        /// <summary>
        /// Loads the <see cref="TeamMemberSpawnView"/> control into the UI for editing the spawn configuration.
        /// </summary>
        /// <remarks>
        /// This method creates a new TeamMemberSpawnView control and initializes its data context with a
        /// TeamMemberSpawnModel. If the provided spawn object has a valid Spawn property, a copy is made and
        /// wrapped in the model; otherwise, an empty model is created.
        /// </remarks>
        /// <param name="control">The parent <see cref="StackPanel"/> control where the view will be added.</param>
        /// <param name="parent">The name of the parent object being edited.</param>
        /// <param name="parentType">The type of the parent object.</param>
        /// <param name="name">The name of the member being edited.</param>
        /// <param name="type">The type of the member being edited.</param>
        /// <param name="attributes">Custom attributes applied to the member.</param>
        /// <param name="obj">The <see cref="TeamMemberSpawn"/> object to edit.</param>
        /// <param name="subGroupStack">A stack of types representing the hierarchy of subgroups.</param>
        public override void LoadWindowControls(StackPanel control, string parent, Type parentType, string name, Type type, object[] attributes,
            TeamMemberSpawn obj, Type[] subGroupStack)
        {
            TeamMemberSpawnView view = new TeamMemberSpawnView();
            if (obj.Spawn != null)
            {
                view.DataContext = new TeamMemberSpawnModel(new TeamMemberSpawn(obj));
            }
            else
            {
                view.DataContext = new TeamMemberSpawnModel();
            }

            control.Children.Add(view);
        }


        /// <summary>
        /// Saves the edited team member spawn from the <see cref="TeamMemberSpawnView"/> control.
        /// </summary>
        /// <remarks>
        /// This method retrieves the TeamMemberSpawnView from the control's children collection,
        /// extracts the TeamMemberSpawnModel from its DataContext, and returns the updated TeamMemberSpawn object.
        /// </remarks>
        /// <param name="control">The parent <see cref="StackPanel"/> control containing the view.</param>
        /// <param name="name">The name of the member being edited.</param>
        /// <param name="type">The type of the member being edited.</param>
        /// <param name="attributes">Custom attributes applied to the member.</param>
        /// <param name="subGroupStack">A stack of types representing the hierarchy of subgroups.</param>
        /// <returns>The edited <see cref="TeamMemberSpawn"/> object with updated configuration.</returns>
        public override TeamMemberSpawn SaveWindowControls(StackPanel control, string name, Type type, object[] attributes, Type[] subGroupStack)
        {
            int controlIndex = 0;
            TeamMemberSpawnView view = (TeamMemberSpawnView)control.Children[controlIndex];
            TeamMemberSpawnModel mv = (TeamMemberSpawnModel)view.DataContext;
            return mv.TeamSpawn;
        }

    }
}