using ICities;
using ColossalFramework.UI;
using CitiesHarmony.API;


namespace BOB
{
    /// <summary>
    /// The base mod class for instantiation by the game.
    /// </summary>
    public class BOBMod : IUserMod
    {
        public static string ModName => "BOB - the tree and prop replacer";
        public static string Version => "0.3.1";

        public string Name => ModName + " " + Version;
        public string Description => Translations.Translate("BOB_DESC");


        /// <summary>
        /// Called by the game when the mod is enabled.
        /// </summary>
        public void OnEnabled()
        {
            // Apply Harmony patches via Cities Harmony.
            // Called here instead of OnCreated to allow the auto-downloader to do its work prior to launch.
            HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());

            // Load the settings file.
            SettingsUtils.LoadSettings();
        }


        /// <summary>
        /// Called by the game when the mod is disabled.
        /// </summary>
        public void OnDisabled()
        {
            // Unapply Harmony patches via Cities Harmony.
            if (HarmonyHelper.IsHarmonyInstalled)
            {
                Patcher.UnpatchAll();
            }
        }


        /// <summary>
        /// Called by the game when the mod options panel is setup.
        /// </summary>
        public void OnSettingsUI(UIHelperBase helper)
        {
            // Language drop down.
            UIDropDown languageDropDown = (UIDropDown)helper.AddDropdown(Translations.Translate("TRN_CHOICE"), Translations.LanguageList, Translations.Index, (index) => { Translations.Index = index; SettingsUtils.SaveSettings(); });
            languageDropDown.autoSize = false;
            languageDropDown.width = 270f;

            // Hotkey control.
            languageDropDown.parent.parent.gameObject.AddComponent<OptionsKeymapping>();

            // Default grouping behaviour.
            string[] groupItems = new string[]
            {
                Translations.Translate("BOB_PER_LST"),
                Translations.Translate("BOB_PER_SIN"),
                Translations.Translate("BOB_PER_GRP")
            };
            UIDropDown groupDropDown = (UIDropDown)helper.AddDropdown(Translations.Translate("BOB_PER_IND"), groupItems, ModSettings.indDefault, (index) => { ModSettings.indDefault = index; SettingsUtils.SaveSettings(); });
            groupDropDown.width = 350f;

            // Rember last position.
            UICheckBox rememberPosCheck = (UICheckBox)helper.AddCheckbox(Translations.Translate("BOB_OPT_POS"), ModSettings.rememberPosition, (isChecked) => { ModSettings.rememberPosition = isChecked; SettingsUtils.SaveSettings(); });

            // Nuke all settings button.
            UIButton nukeButton = (UIButton)helper.AddButton(Translations.Translate("BOB_NUKE"), delegate
            {
                // Revert all-building and building settings.
                AllBuildingReplacement.RevertAll();
                BuildingReplacement.RevertAll();

                // Save configuration.
                ConfigurationUtils.SaveConfig();
            });
        }
    }
}
