using ColossalFramework.UI;


namespace BOB
{
    /// <summary>
    /// Options panel for setting general mod options.
    /// </summary>
    internal class GeneralOptionsPanel
    {
        /// <summary>
        /// Adds mod options tab to tabstrip.
        /// </summary>
        /// <param name="tabStrip">Tab strip to add to</param>
        /// <param name="tabIndex">Index number of tab</param>
        internal GeneralOptionsPanel(UITabstrip tabStrip, int tabIndex)
        {
            // Add tab and helper.
            UIPanel panel = PanelUtils.AddTab(tabStrip, Translations.Translate("BOB_OPT_GEN"), tabIndex);
            UIHelper helper = new UIHelper(panel);
            panel.autoLayout = true;

            // Language dropdown.
            UIDropDown languageDrop = UIControls.AddPlainDropDown(panel, Translations.Translate("TRN_CHOICE"), Translations.LanguageList, Translations.Index);
            languageDrop.eventSelectedIndexChanged += (control, index) => Translations.Index = index;

            // Hotkey control.
            panel.gameObject.AddComponent<OptionsKeymapping>();

            // Default grouping behaviour.
            string[] groupItems = new string[]
            {
                Translations.Translate("BOB_PER_LST"),
                Translations.Translate("BOB_PER_SIN"),
                Translations.Translate("BOB_PER_GRP")
            };
            UIDropDown groupDropDown = UIControls.AddPlainDropDown(panel, Translations.Translate("BOB_PER_IND"), groupItems, ModSettings.indDefault, 350f);
            groupDropDown.eventSelectedIndexChanged += (control, index) => ModSettings.indDefault = index;

            // Rember last position.
            UICheckBox rememberPosCheck = UIControls.AddPlainCheckBox(panel, Translations.Translate("BOB_OPT_POS"));
            rememberPosCheck.isChecked = ModSettings.rememberPosition;
            rememberPosCheck.eventCheckChanged += (control, isChecked) => ModSettings.rememberPosition = isChecked;

            // Disable vanilla tree tool network tree replacement.
            UICheckBox disableTreeToolCheck = UIControls.AddPlainCheckBox(panel, Translations.Translate("BOB_OPT_DTT"));
            disableTreeToolCheck.isChecked = ModSettings.DisableTreeTool;
            disableTreeToolCheck.eventCheckChanged += (control, isChecked) => ModSettings.DisableTreeTool = isChecked;

            // Tree tool control.
            panel.gameObject.AddComponent<TreeToolKeymapping>();

        }
    }
}