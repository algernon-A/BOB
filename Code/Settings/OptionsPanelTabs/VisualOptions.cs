using ICities;
using ColossalFramework.UI;


namespace BOB
{
    /// <summary>
    /// Options panel for setting options regarding tree and prop ruining.
    /// </summary>
    internal class VisualOptionsPanel
    {
        /// <summary>
        /// Adds mod options tab to tabstrip.
        /// </summary>
        /// <param name="tabStrip">Tab strip to add to</param>
        /// <param name="tabIndex">Index number of tab</param>
        internal VisualOptionsPanel(UITabstrip tabStrip, int tabIndex)
        {
            // Add tab and helper.
            UIPanel panel = PanelUtils.AddTab(tabStrip, Translations.Translate("BOB_OPT_VIS"), tabIndex);
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