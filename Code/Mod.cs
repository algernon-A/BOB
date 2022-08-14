﻿// <copyright file="Mod.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using AlgernonCommons;
    using AlgernonCommons.Patching;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using ICities;

    /// <summary>
    /// The base mod class for instantiation by the game.
    /// </summary>
    public class Mod : PatcherMod, IUserMod
    {
        /// <summary>
        /// Gets the mod's base display name (name only).
        /// </summary>
        public override string BaseName => "BOB - the tree and prop replacer";

        /// <summary>
        /// Gets the mod's unique Harmony identfier.
        /// </summary>
        public override string HarmonyID => "com.github.algernon-A.csl.bob";

        /// <summary>
        /// Gets the mod's description for display in the content manager.
        /// </summary>
        public string Description => Translations.Translate("BOB_DESC");

        /// <summary>
        /// Called by the game when the mod is enabled.
        /// </summary>
        public override void OnEnabled()
        {
            base.OnEnabled();

            // Add the options panel event handler for the start screen (to enable/disable options panel based on visibility).
            // First, check to see if UIView is ready.
            if (UIView.GetAView() != null)
            {
                // It's ready - attach the hook now.
                OptionsPanelManager<OptionsPanel>.OptionsEventHook();
            }
            else
            {
                // Otherwise, queue the hook for when the intro's finished loading.
                LoadingManager.instance.m_introLoaded += OptionsPanelManager<OptionsPanel>.OptionsEventHook;
            }

            // Set up exception notifications.
            Logging.EventExceptionOccured += BOBPanelManager.RecordException;
        }

        /// <summary>
        /// Called by the game when the mod options panel is setup.
        /// </summary>
        /// <param name="helper">UI helper instance.</param>
        public void OnSettingsUI(UIHelperBase helper)
        {
            // Create options panel.
            OptionsPanelManager<OptionsPanel>.Setup(helper);
        }

        /// <summary>
        /// Saves settings file.
        /// </summary>
        public override void SaveSettings() => ModSettings.Save();

        /// <summary>
        /// Loads settings file.
        /// </summary>
        public override void LoadSettings() => ModSettings.Load();

        /// <summary>
        /// Apply Harmony patches.
        /// </summary>
        protected override void ApplyPatches() => Patcher.Instance.PatchAll();

        /// <summary>
        /// Remove Harmony patches.
        /// </summary>
        protected override void RemovePatches() => Patcher.Instance.UnpatchAll();
    }
}
