using ICities;
using BOB.MessageBox;


namespace BOB
{
    /// <summary>
    /// Main loading class: the mod runs from here.
    /// </summary>
    public class Loading : LoadingExtensionBase
    {
        // Internal flags.
        internal static bool isModEnabled = false;

        /// <summary>
        /// Called by the game when the mod is initialised at the start of the loading process.
        /// </summary>
        /// <param name="loading">Loading mode (e.g. game, editor, scenario, etc.)</param>
        public override void OnCreated(ILoading loading)
        {
            Logging.KeyMessage("version ", BOBMod.Version, " loading");

            // Don't do anything if not in game (e.g. if we're going into an editor).
            if (loading.currentMode != AppMode.Game && loading.currentMode != AppMode.MapEditor)
            {
                isModEnabled = false;
                Logging.KeyMessage("not loading into game, skipping activation");

                // Unload Harmony patches and exit before doing anything further.
                Patcher.UnpatchAll();
                return;
            }

            // All good to go at this point.
            isModEnabled = true;

            // Check if we're using EML's EPropManager.
            ModSettings.ePropManager = ModUtils.IsModInstalled("EManagersLib", true);

            // Initialise data sets prior to savegame load.
            new AllBuildingReplacement();
            new AllNetworkReplacement();
            new BuildingReplacement();
            new NetworkReplacement();
            new IndividualBuildingReplacement();
            new MapTreeReplacement();

            // Using EPropManager?
            if (ModSettings.ePropManager)
            {
                // Yes - use EPropManager.
                new EMapPropReplacement();
            }
            else
            {
                // No - use game prop manager.
                new MapPropReplacement();
            }
            
            new Scaling();

            base.OnCreated(loading);
        }


        /// <summary>
        /// Called by the game when level loading is complete.
        /// </summary>
        /// <param name="mode">Loading mode (e.g. game, editor, scenario, etc.)</param>
        public override void OnLevelLoaded(LoadMode mode)
        {
            Logging.Message("commencing loading checks");

            base.OnLevelLoaded(mode);

            // Don't do anything further if we're not operating.
            if (!isModEnabled)
            {
                Logging.Message("exiting");
                return;
            }

            // Check to see that Harmony 2 was properly loaded.
            if (!Patcher.Patched)
            {
                // Harmony 2 wasn't loaded; abort.
                Logging.Error("Harmony patches not applied; aborting");
                isModEnabled = false;

                // Display warning message.
                ListMessageBox harmonyBox = MessageBoxBase.ShowModal<ListMessageBox>();

                // Key text items.
                harmonyBox.AddParas(Translations.Translate("ERR_HAR0"), Translations.Translate("BOB_ERR_HAR"), Translations.Translate("BOB_ERR_FAT"), Translations.Translate("ERR_HAR1"));

                // List of dot points.
                harmonyBox.AddList(Translations.Translate("ERR_HAR2"), Translations.Translate("ERR_HAR3"));

                // Closing para.
                harmonyBox.AddParas(Translations.Translate("MES_PAGE"));

                // Don't do anything further.
                return;
            }

            Logging.Message("loading checks passed");

            // Build lists of loaded prefabs.
            PrefabLists.BuildLists();

            // Load prop packs.
            new NetworkPackReplacement();

            // Load configuration file.
            ConfigurationUtils.LoadConfig();

            // Using EPropManager?
            if (ModSettings.ePropManager)
            {
                // Yes - use BOB tool with EPropManager.
                ToolsModifierControl.toolController.gameObject.AddComponent<EBOBTool>();
            }
            else
            {
                // No - use BOB tool with base game prop manager.
                ToolsModifierControl.toolController.gameObject.AddComponent<BOBTool>();
            }

            // Display update notification.
            WhatsNew.ShowWhatsNew();

            // Set up Network Skins 2 reflection.
            ModUtils.NS2Reflection();

            // Force update of any dirty net or building prefabs from replacement process.
            Logging.Message("updating dirty prefabs");
            BuildingData.Update();
            NetData.Update();

            // Set up options panel event handler.
            OptionsPanel.OptionsEventHook();

            Logging.Message("loading complete");
        }
    }
}