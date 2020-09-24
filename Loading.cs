using ICities;


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
            GlobalReplacement.Setup();
            BuildingReplacement.Setup();
            NetworkReplacement.Setup();

            // Load configuration file.
            ConfigurationUtils.LoadConfig();

            // Initialise select tool.
            ToolsModifierControl.toolController.gameObject.AddComponent<SelectTool>();
            Debugging.Message("loading complete");
        }
    }
}