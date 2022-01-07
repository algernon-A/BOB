using System.Collections.Generic;
using ColossalFramework;


namespace BOB
{
    /// <summary>
    /// Static class to handle centralised building data.
    /// </summary>
    internal static class BuildingData
    {
        // List of dirty net prefabs.
        private static HashSet<BuildingInfo> dirtyList;


        /// <summary>
        /// Dirty prefabs list.
        /// </summary>
        internal static HashSet<BuildingInfo> DirtyList
        {
            get
            {
                // Initialise list if it isn't already.
                if (dirtyList == null)
                {
                    dirtyList = new HashSet<BuildingInfo>();
                }

                return dirtyList;
            }
        }


        /// <summary>
        /// Refreshes building prefab renders for all 'dirty' buildings.
        /// </summary>
        internal static void Update()
        {
            // Need to do this for each building instance, so iterate through all buildings.
            Building[] buildings = BuildingManager.instance.m_buildings.m_buffer;
            for (ushort i = 0; i < buildings.Length; ++i)
            {
                // Local reference.
                Building building = buildings[i];

                // Check that this is a valid building in the dirty list.
                if (building.m_flags != Building.Flags.None && DirtyList.Contains(building.Info))
                {
                    // Match - update building instance renders directly in main thread, before simulation thread starts trying to render the instances.
                    RenderManager renderManager = Singleton<RenderManager>.instance;
                    if (renderManager.RequireInstance(i, 1u, out var instanceIndex))
                    {
                        renderManager.m_instances[instanceIndex].m_dirty = true;
                        //BuildingAI.RefreshInstance(buildingInfo, renderManager.CurrentCameraInfo, i, ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[i], buildingInfo.m_prefabDataLayer, ref renderManager.m_instances[instanceIndex], true);
                    }

                    // Then, update building prefab render (for LOD model) via simulation thread, creating local reference to avoid race condition.
                    ushort buildingID = i;
                    Singleton<SimulationManager>.instance.AddAction(delegate
                    {
                        Singleton<BuildingManager>.instance.UpdateBuildingRenderer(buildingID, true);
                    });
                }
            }

            // Clear dirty prefabs list.
            DirtyList.Clear();
        }
    }
}
