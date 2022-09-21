// <copyright file="GeneralOptionsPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using AlgernonCommons.Keybinding;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;

    /// <summary>
    /// Options panel for setting general mod options.
    /// </summary>
    internal class GeneralOptionsPanel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeneralOptionsPanel"/> class.
        /// </summary>
        /// <param name="tabStrip">Tab strip to add to.</param>
        /// <param name="tabIndex">Index number of tab.</param>
        internal GeneralOptionsPanel(UITabstrip tabStrip, int tabIndex)
        {
            // Add tab and helper.
            UIPanel panel = UITabstrips.AddTextTab(tabStrip, Translations.Translate("BOB_OPT_GEN"), tabIndex, out _);
            UIHelper helper = new UIHelper(panel);
            panel.autoLayout = true;

            // Language dropdown.
            UIDropDown languageDrop = UIDropDowns.AddPlainDropDown(panel, 0f, 0f, Translations.Translate("TRN_CHOICE"), Translations.LanguageList, Translations.Index);
            languageDrop.eventSelectedIndexChanged += (c, index) =>
            {
                Translations.Index = index;
                OptionsPanelManager<OptionsPanel>.LocaleChanged();
            };

            // Hotkey control.
            panel.gameObject.AddComponent<UUIKeymapping>();

            // Default grouping behaviour.
            string[] groupItems = new string[]
            {
                Translations.Translate("BOB_PER_LST"),
                Translations.Translate("BOB_PER_SIN"),
                Translations.Translate("BOB_PER_GRP"),
            };
            UIDropDown groupDropDown = UIDropDowns.AddPlainDropDown(panel, 0f, 0f, Translations.Translate("BOB_PER_IND"), groupItems, ModSettings.IndividualDefault, 350f);
            groupDropDown.eventSelectedIndexChanged += (c, index) => ModSettings.IndividualDefault = index;

            // Rember last position.
            UICheckBox rememberPosCheck = UICheckBoxes.AddPlainCheckBox(panel, Translations.Translate("BOB_OPT_POS"));
            rememberPosCheck.isChecked = ModSettings.RememberPosition;
            rememberPosCheck.eventCheckChanged += (c, isChecked) => ModSettings.RememberPosition = isChecked;

            // Disable vanilla tree tool network tree replacement.
            UICheckBox disableTreeToolCheck = UICheckBoxes.AddPlainCheckBox(panel, Translations.Translate("BOB_OPT_DTT"));
            disableTreeToolCheck.isChecked = ModSettings.DisableTreeTool;
            disableTreeToolCheck.eventCheckChanged += (c, isChecked) => ModSettings.DisableTreeTool = isChecked;

            // Tree tool control.
            OptionsKeymapping treeDisableKeyMapping = panel.gameObject.AddComponent<OptionsKeymapping>();
            treeDisableKeyMapping.Label = Translations.Translate("BOB_OPT_DTK");
            treeDisableKeyMapping.Binding = HotkeyThreading.TreeDisableKey;
        }
    }
}