namespace BOB
{
    using AlgernonCommons.UI;
    using ColossalFramework.UI;

    /// <summary>
    /// BOB options panel.
    /// </summary>
    internal class OptionsPanel : UIPanel
    {
        /// <summary>
        /// Creates the panel object in-game and displays it.
        /// </summary>
        internal OptionsPanel()
        {
            // Add tabstrip.
            UITabstrip tabstrip = UITabstrips.AddTabStrip(this, 0f, 0f, OptionsPanelManager<OptionsPanel>.PanelWidth, OptionsPanelManager<OptionsPanel>.PanelHeight, out _);

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
    }
}