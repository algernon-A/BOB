// <copyright file="OptionsPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using AlgernonCommons.UI;
    using ColossalFramework.UI;

    /// <summary>
    /// BOB options panel.
    /// </summary>
    public class OptionsPanel : UIPanel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OptionsPanel"/> class.
        /// </summary>
        internal OptionsPanel()
        {
            // Add tabstrip.
            UITabstrip tabstrip = AutoTabstrip.AddTabstrip(this, 0f, 0f, OptionsPanelManager<OptionsPanel>.PanelWidth, OptionsPanelManager<OptionsPanel>.PanelHeight, out _);

            // Add tabs and panels.
            new GeneralOptionsPanel(tabstrip, 0);
            new ConfigurationsPanel(tabstrip, 1);
            new VisualOptionsPanel(tabstrip, 2);

            // Disable hotkey while options panel is open.
            HotkeyThreading.Operating = false;

            // Ensure initial selected tab (doing a 'quickstep' to ensure proper events are triggered).
            tabstrip.selectedIndex = -1;
            tabstrip.selectedIndex = 0;
        }

        /// <summary>
        /// Called by the game when the component is destroyed.
        /// </summary>
        public override void OnDestroy()
        {
            // Re-enable hotkey threading.
            HotkeyThreading.Operating = true;
        }
    }
}