using ICities;

using System.Collections.Generic;


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

            // Load configuration file.
            ConfigurationUtils.LoadConfig();

            // Initialise select tool.
            ToolsModifierControl.toolController.gameObject.AddComponent<BOBTool>();
            Debugging.Message("loading complete");

            CheckNets();
        }


        private void CheckNets()
        {
            List<NetInfo.Lane> laneList = new List<NetInfo.Lane>();
            List<NetLaneProps> lanePropList = new List<NetLaneProps>();


            // Iterate through all loaded nets.
            for (uint i = 0u; i < PrefabCollection<NetInfo>.LoadedCount(); ++i)
            {

                NetInfo network = PrefabCollection<NetInfo>.GetLoaded(i);
                Debugging.Message("checking network " + network.name);



                // Iterate through all lanes in prefab.
                for (int j = 0; j < network.m_lanes.Length; ++j)
                {
                    if (laneList.Contains(network.m_lanes[j]))
                    {
                        Debugging.Message("duplicate lane found in " + network.name);
                    }
                    else
                    {
                        laneList.Add(network.m_lanes[j]);
                    }

                    if (lanePropList.Contains(network.m_lanes[j].m_laneProps))
                    {
                        Debugging.Message("duplicate laneprop found in " + network.name);
                    }
                    else
                    {
                        lanePropList.Add(network.m_lanes[j].m_laneProps);
                    }
                }
            }
        }
    }
}