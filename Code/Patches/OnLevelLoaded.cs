using HarmonyLib;
using BOB.MessageBox;


namespace BOB
{
    /// <summary>
    /// Harmony Postfix patch for OnLevelLoaded.  This enables us to perform setup tasks after all loading has been completed.
    /// </summary>
    [HarmonyPatch(typeof(LoadingWrapper))]
    [HarmonyPatch("OnLevelLoaded")]
    public static class OnLevelLoadedPatch
    {
        /// <summary>
        /// Harmony postfix to perform actions require after the level has loaded.
        /// </summary>
        public static void Postfix()
        {
            // Don't do anything if mod hasn't activated for whatever reason (mod conflict, harmony error, something else).
            if (!Loading.isModEnabled)
            {
                return;
            }

            // Build lists of loaded prefabs.
            PrefabLists.BuildLists();

            // Load prop packs.
            new PackReplacement();

            // Load configuration file.
            ConfigurationUtils.LoadConfig();

            // Initialise select tool.
            ToolsModifierControl.toolController.gameObject.AddComponent<BOBTool>();
            Logging.KeyMessage("loading complete");

            // Display update notification.
            WhatsNew.ShowWhatsNew();

            // Warning message box for 0.3 update if a configuration file exists without the 0.3 notification flag.
            if (System.IO.File.Exists("TreePropReplacer-config.xml") && !System.IO.File.Exists("BOB-config.xml"))
            {
                ListMessageBox messageBox = MessageBoxBase.ShowModal<ListMessageBox>();
                messageBox.AddParas("BOB, the Tree and Prop Replacer, has been updated to version 0.3.  As part of this update the configuration file format has changed in order to support expanded functionality.  These changes are NOT backwards-compatible.", "IMPORTANT", "This means that your existing replacements will be lost and will need to be redone.", "The new configuration file format (BOB-config.xml) is now final and will be supported in all future releases.  Your old configuration file (TreePropReplacer-config.xml) has been left unaltered for use as a reference.");
            }

            // Set up Network Skins 2 reflection.
            ModUtils.NS2Reflection();

            // Force update of any dirty net or building prefabs from replacement process.
            BuildingData.Update();
            NetData.Update();
        }
    }
}