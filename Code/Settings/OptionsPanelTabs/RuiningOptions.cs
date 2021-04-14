using ICities;
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

            UIHelperBase ruinGroup = helper.AddGroup(Translations.Translate("BOB_OPT_RGL"));

            // Checkboxes.
            ruinGroup.AddCheckbox(Translations.Translate("BOB_OPT_RRT"), ModSettings.StopTreeRuining, (value) => ModSettings.StopTreeRuining = value);
            ruinGroup.AddCheckbox(Translations.Translate("BOB_OPT_RRP"), ModSettings.StopPropRuining, (value) => ModSettings.StopPropRuining = value);
        }
    }
}