namespace BOB
{
    using AlgernonCommons;
    using AlgernonCommons.Notifications;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ICities;

    /// <summary>
    /// Main loading class: the mod runs from here.
    /// </summary>
    public class Loading : LoadingExtensionBase
    {
        // Internal flags.
        internal static bool isModEnabled = false;
        internal static bool isLoaded = false;

        /// <summary>
        /// Called by the game when the mod is initialised at the start of the loading process.
        /// </summary>
        /// <param name="loading">Loading mode (e.g. game, editor, scenario, etc.)</param>
        public override void OnCreated(ILoading loading)
        {
            Logging.KeyMessage("version ", AssemblyUtils.TrimmedCurrentVersion, " loading");

            // Don't do anything if not in game (e.g. if we're going into an editor).
            if (loading.currentMode != AppMode.Game && loading.currentMode != AppMode.MapEditor)
            {
                isModEnabled = false;
                Logging.KeyMessage("not loading into game, skipping activation");

                // Unload Harmony patches and exit before doing anything further.
                Patcher.Instance.UnpatchAll();
                return;
            }

            // All good to go at this point.
            isModEnabled = true;

            // Initialise data sets prior to savegame load.
            new AllBuildingReplacement();
            new AllNetworkReplacement();
            new BuildingReplacement();
            new NetworkReplacement();
            new IndividualBuildingReplacement();
            new IndividualNetworkReplacement();
            new MapTreeReplacement();
            new MapPropReplacement();
            new Scaling();
            new AddedBuildingProps();
            new AddedNetworkProps();

            // Reflect overlay methods.
            Patcher.Instance.ReflectOverlays();

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
            if (!Patcher.Instance.Patched)
            {
                // Harmony 2 wasn't loaded; abort.
                Logging.Error("Harmony patches not applied; aborting");
                isModEnabled = false;

                // Display warning message.
                ListNotification harmonyNotification = NotificationBase.ShowNotification<ListNotification>();

                // Key text items.
                harmonyNotification.AddParas(Translations.Translate("ERR_HAR0"), Translations.Translate("BOB_ERR_HAR"), Translations.Translate("BOB_ERR_FAT"), Translations.Translate("ERR_HAR1"));

                // List of dot points.
                harmonyNotification.AddList(Translations.Translate("ERR_HAR2"), Translations.Translate("ERR_HAR3"));

                // Closing para.
                harmonyNotification.AddParas(Translations.Translate("MES_PAGE"));

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

            // Set up BOB tool.
            ToolsModifierControl.toolController.gameObject.AddComponent<BOBTool>();

            // Display update notification.
            WhatsNew.ShowWhatsNew();

            // Set up Network Skins 2 reflection.
            ModUtils.NS2Reflection();

            // Enable thin wires, if applicable.
            if (ModSettings.ThinnerWires)
            {
                ElectricalWires.Instance.ApplyThinnerWires();
            }

            // Force update of any dirty net or building prefabs from replacement process.
            Logging.Message("updating dirty prefabs");
            BuildingData.Update();
            NetData.Update();

            // Set up options panel event handler.
            OptionsPanelManager<OptionsPanel>.OptionsEventHook();

            // Display any exception message that occured during load.
            InfoPanelManager.CheckException();

            // Activate tool hotkey.
            HotkeyThreading.Operating = true;

            isLoaded = true;
            Logging.Message("loading complete");
        }


        /// <summary>
        /// Called by the game when exiting loaded leve.
        /// </summary>
        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();
            isLoaded = false;
        }
    }
}