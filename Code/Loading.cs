// <copyright file="Loading.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System.Collections.Generic;
    using AlgernonCommons;
    using AlgernonCommons.Patching;
    using ICities;

    /// <summary>
    /// Main loading class: the mod runs from here.
    /// </summary>
    public sealed class Loading : PatcherLoadingBase<OptionsPanel, Patcher>
    {
        /// <summary>
        /// Gets a list of permitted loading modes.
        /// </summary>
        protected override List<AppMode> PermittedModes => new List<AppMode> { AppMode.Game, AppMode.MapEditor, AppMode.AssetEditor, AppMode.ScenarioEditor, AppMode.AssetEditor };

        /// <summary>
        /// Performs any actions upon successful creation of the mod.
        /// E.g. Can be used to patch any other mods.
        /// </summary>
        /// <param name="loading">Loading mode (e.g. game or editor).</param>
        protected override void CreatedActions(ILoading loading)
        {
            base.CreatedActions(loading);

            // Initialise data sets prior to savegame load.
            new AllBuildingReplacement();
            new AllNetworkReplacement();
            new GroupedBuildingReplacement();
            new GroupedNetworkReplacement();
            new IndividualBuildingReplacement();
            new IndividualNetworkReplacement();
            new MapTreeReplacement();
            new MapPropReplacement();
            new Scaling();
            new AddedBuildingProps();
            new AddedNetworkProps();

            // Reflect overlay methods.
            PatcherManager<Patcher>.Instance.ReflectOverlays();
        }

        /// <summary>
        /// Performs any actions upon successful level loading completion.
        /// </summary>
        /// <param name="mode">Loading mode (e.g. game, editor, scenario, etc.).</param>
        protected override void LoadedActions(LoadMode mode)
        {
            base.LoadedActions(mode);

            // Build lists of loaded prefabs.
            PrefabLists.BuildLists();

            // Load prop packs.
            new NetworkPackReplacement();

            // Load configuration file.ApoHi Conrad
            ConfigurationUtils.LoadConfig();

            // Set up BOB tool.
            ToolsModifierControl.toolController.gameObject.AddComponent<BOBTool>();

            // Set up Network Skins 2 reflection.
            ModUtils.NS2Reflection();

            // Enable thin wires, if applicable.
            if (ModSettings.ThinnerWires)
            {
                ElectricalWires.Instance.ApplyThinnerWires();
            }

            // Ensure tree tool state.
            PatcherManager<Patcher>.Instance.DisableTreeTool(ModSettings.DisableTreeTool);

            // Force update of any dirty net or building prefabs from replacement process.
            Logging.Message("updating dirty prefabs");
            BuildingData.Update();
            NetData.Update();

            // Display any exception message that occured during load.
            BOBPanelManager.CheckException();

            // Check random templates.
            PrefabLists.CheckRandomTemplates();

            // Activate tool hotkey.
            HotkeyThreading.Operating = true;
        }
    }
}