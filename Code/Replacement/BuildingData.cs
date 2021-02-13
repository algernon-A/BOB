using System.Collections.Generic;

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
        /// Refreshes network prefab renders for all 'dirty' buildings.
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
                    // Match - update building render.
                    BuildingManager.instance.UpdateBuildingRenderer(i, true);
                }
            }

            // Clear dirty prefabs list.
            DirtyList.Clear();
        }
    }
}
