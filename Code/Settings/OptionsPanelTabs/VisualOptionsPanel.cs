﻿// <copyright file="VisualOptionsPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using ICities;

    /// <summary>
    /// Options panel for setting options regarding tree and prop ruining.
    /// </summary>
    internal class VisualOptionsPanel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VisualOptionsPanel"/> class.
        /// </summary>
        /// <param name="tabStrip">Tab strip to add to.</param>
        /// <param name="tabIndex">Index number of tab.</param>
        internal VisualOptionsPanel(UITabstrip tabStrip, int tabIndex)
        {
            // Add tab and helper.
            UIPanel panel = UITabstrips.AddTextTab(tabStrip, Translations.Translate("BOB_OPT_VIS"), tabIndex, out _);
            UIHelper helper = new UIHelper(panel);
            panel.autoLayout = true;

            // Ruining.
            UIHelperBase ruinGroup = helper.AddGroup(Translations.Translate("BOB_OPT_RUI") + " - " + Translations.Translate("BOB_OPT_RGL"));
            ruinGroup.AddCheckbox(Translations.Translate("BOB_OPT_RRT"), ModSettings.StopTreeRuining, (value) => ModSettings.StopTreeRuining = value);
            ruinGroup.AddCheckbox(Translations.Translate("BOB_OPT_RRP"), ModSettings.StopPropRuining, (value) => ModSettings.StopPropRuining = value);

            // Electrical wire thickness.
            UIHelperBase wiresGroup = helper.AddGroup(Translations.Translate("BOB_OPT_WIR"));
            wiresGroup.AddCheckbox(Translations.Translate("BOB_OPT_WTH"), ModSettings.ThinnerWires, (value) => ModSettings.ThinnerWires = value);
        }
    }
}