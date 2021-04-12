using ColossalFramework.UI;


namespace BOB
{
    /// <summary>
    /// Options panel for setting options regarding tree and prop ruining.
    /// </summary>
    internal class RuiningOptionsPanel
    {
        /// <summary>
        /// Adds mod options tab to tabstrip.
        /// </summary>
        /// <param name="tabStrip">Tab strip to add to</param>
        /// <param name="tabIndex">Index number of tab</param>
        internal RuiningOptionsPanel(UITabstrip tabStrip, int tabIndex)
        {
            // Add tab and helper.
            UIPanel panel = PanelUtils.AddTab(tabStrip, Translations.Translate("BOB_OPT_RUI"), tabIndex);
            UIHelper helper = new UIHelper(panel);
            panel.autoLayout = true;

            // Prevent ruining by trees.
            UICheckBox stopTreeRuin = UIControls.AddPlainCheckBox(panel, Translations.Translate("BOB_OPT_RPT"));
            stopTreeRuin.isChecked = ModSettings.StopTreeRuining;
            stopTreeRuin.eventCheckChanged += (control, isChecked) => PrefabLists.DisableTreeRuining(isChecked);

            // Prevent ruining by props.
            UICheckBox stopPropRuin = UIControls.AddPlainCheckBox(panel, Translations.Translate("BOB_OPT_RPP"));
            stopPropRuin.isChecked = ModSettings.StopPropRuining;
            stopPropRuin.eventCheckChanged += (control, isChecked) => PrefabLists.DisablePropRuining(isChecked);
        }
    }
}