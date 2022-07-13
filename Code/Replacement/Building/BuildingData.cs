using System.Collections.Generic;
using UnityEngine;
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
        /// Refreshes building prefabs and renders for the specified building prefab.
        /// </summary>
        /// <param name="prefab">Building prefab to update</param>
        internal static void UpdateBuilding(BuildingInfo prefab)
        {
            // Hashset of render group coordinates to update.
            HashSet<KeyValuePair<int, int>> groupHash = new HashSet<KeyValuePair<int, int>>();

            // Local references.
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            RenderManager renderManager = Singleton<RenderManager>.instance;
            Building[] buildings = buildingManager.m_buildings.m_buffer;

            // Need to do this for each building instance, so iterate through all buildings.
            for (ushort i = 0; i < buildings.Length; ++i)
            {
                // Check that this is a valid building in the dirty list.
                if (buildings[i].m_flags != Building.Flags.None && buildings[i].Info == prefab)
                {
                    // Update building instance.
                    renderManager.UpdateInstance(i);

                    // Update parking spaces in simulation thread.
                    if (buildings[i].Info.m_hasParkingSpaces != 0)
                    {
                        ushort buildingID = i;
                        Singleton<SimulationManager>.instance.AddAction(() => buildingManager.UpdateParkingSpaces(buildingID, ref buildings[buildingID]));
                    }

                    // Calculate building render group.
                    Vector3 position = buildings[i].m_position;
                    int num = Mathf.Clamp((int)(position.x / 64f + 135f), 0, 269);
                    int num2 = Mathf.Clamp((int)(position.z / 64f + 135f), 0, 269);
                    int x = num * 45 / 270;
                    int z = num2 * 45 / 270;

                    // Add render group coordinates to hashlist (ignore if already there).
                    groupHash.Add(new KeyValuePair<int, int>(x, z));
                }
            }
            // Iterate through each key in group.
            foreach (KeyValuePair<int, int> keyPair in groupHash)
            {
                // Update group render (all 31 layers, since we've got all kinds of mismatches with replacements).
                for (int i = 0; i < 31; ++i)
                {
                    Singleton<RenderManager>.instance.UpdateGroup(keyPair.Key, keyPair.Value, i);
                }
            }
        }


        /// <summary>
        /// Refreshes building prefabs and renders for all 'dirty' buildings.
        /// </summary>
        internal static void Update()
        {
            // Don't do anything if nothing to update.
            if (dirtyList == null || dirtyList.Count == 0)
            {
                return;
            }

            // Hashset of render group coordinates to update.
            HashSet<KeyValuePair<int, int>> groupHash = new HashSet<KeyValuePair<int, int>>();

            // Local references.
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            RenderManager renderManager = Singleton<RenderManager>.instance;
            Building[] buildings = buildingManager.m_buildings.m_buffer;

            // Need to do this for each building instance, so iterate through all buildings.
            for (ushort i = 0; i < buildings.Length; ++i)
            {
                // Check that this is a valid building in the dirty list.
                if (buildings[i].m_flags != Building.Flags.None && DirtyList.Contains(buildings[i].Info))
                {
                    renderManager.UpdateInstance(i);

                    // Update parking spaces in simulation thread.
                    if (buildings[i].Info.m_hasParkingSpaces != 0)
                    {
                        ushort buildingID = i;
                        Singleton<SimulationManager>.instance.AddAction(() => buildingManager.UpdateParkingSpaces(buildingID, ref buildings[buildingID]));
                    }

                    // Calculate building render group.
                    Vector3 position = buildings[i].m_position;
                    int num = Mathf.Clamp((int)(position.x / 64f + 135f), 0, 269);
                    int num2 = Mathf.Clamp((int)(position.z / 64f + 135f), 0, 269);
                    int x = num * 45 / 270;
                    int z = num2 * 45 / 270;

                    // Add render group coordinates to hashlist (ignore if already there).
                    groupHash.Add(new KeyValuePair<int, int>(x, z));
                }
            }

            // Now, iterate through each key in group to refresh all the affected render groups.
            foreach (KeyValuePair<int, int> keyPair in groupHash)
            {
                // Update group render (all 31 layers, since we've got all kinds of mismatches with replacements).
                for (int i = 0; i < 31; ++i)
                {
                    renderManager.UpdateGroup(keyPair.Key, keyPair.Value, i);
                }
            }

            // Clear dirty prefabs list.
            DirtyList.Clear();
        }
    }
}
