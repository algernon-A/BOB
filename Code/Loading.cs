using ICities;
using BOB.MessageBox;


namespace BOB
{
    /// <summary>
    /// Main loading class: the mod runs from here.
    /// </summary>
    public class Loading : LoadingExtensionBase
    {
        /// <summary>
        /// Called by the game when level loading is complete.
        /// </summary>
        /// <param name="mode">Loading mode (e.g. game, editor, scenario, etc.)</param>
        public override void OnLevelLoaded(LoadMode mode)
        {
            // Don't do anything if not in game.
            if (mode != LoadMode.NewGame && mode != LoadMode.LoadGame)
            {
                Debugging.Message("not loading into game; exiting");
                Patcher.UnpatchAll();
                return;
            }

            Debugging.Message("loading");

            // Initialise data sets.
            PrefabLists.BuildLists();
            AllBuildingReplacement.Setup();
            AllNetworkReplacement.Setup();
            BuildingReplacement.Setup();
            NetworkReplacement.Setup();
            IndividualReplacement.Setup();
            MapTreeReplacement.Setup();

            // Load configuration file.
            ConfigurationUtils.LoadConfig();

            // Initialise select tool.
            ToolsModifierControl.toolController.gameObject.AddComponent<BOBTool>();
            Debugging.Message("loading complete");


            // Warning message box for 0.3 update if a configuration file exists without the 0.3 notification flag.
            if (System.IO.File.Exists("TreePropReplacer-config.xml") && !System.IO.File.Exists("BOB-config.xml"))
            {
                OkMessageBox messageBox = MessageBoxBase.ShowModal<OkMessageBox>();
                messageBox.CaprionText = BOBMod.ModName;
                messageBox.MessageText = "BOB, the Tree and Prop Replacer, has been updated to version 0.3.  As part of this update the configuration file format has changed in order to support expanded functionality.  These changes are NOT backwards-compatible.\r\n\r\nIMPORTANT\r\nThis means that your existing replacements will be lost and will need to be redone.\r\n\r\nThe new configuration file format (BOB-config.xml) is now final and will be supported in all future releases.  Your old configuration file (TreePropReplacer-config.xml) has been left unaltered for use as a reference.";
            }
        }
    }
}