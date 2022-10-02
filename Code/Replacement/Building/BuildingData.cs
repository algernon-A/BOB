// <copyright file="BuildingData.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System.Collections.Generic;
    using ColossalFramework;
    using UnityEngine;

    /// <summary>
    /// Static class to handle centralised building data.
    /// </summary>
    internal static class BuildingData
    {
        // List of dirty building prefabs.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1311:Static readonly fields should begin with upper-case letter", Justification = "Private static readonly field")]
        private static readonly HashSet<BuildingInfo> s_dirtyList = new HashSet<BuildingInfo>();

        /// <summary>
        /// Gets the dirty prefabs list.
        /// </summary>
        internal static HashSet<BuildingInfo> DirtyList => s_dirtyList;

        /// <summary>
        /// Refreshes building prefabs and renders for all 'dirty' buildings.
        /// </summary>
        internal static void Update()
        {
            // Don't do anything if nothing to update.
            if (s_dirtyList == null || s_dirtyList.Count == 0)
            {
                return;
            }

            // Pause simulation.
            SimulationManager simulationManager = Singleton<SimulationManager>.instance;
            bool simulationWasPaused = simulationManager.SimulationPaused;
            simulationManager.SimulationPaused = true;

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
                    // Update prop references.
                    BuildingInfo thisInfo = buildings[i].Info;
                    simulationManager.AddAction(() =>
                    {
                        thisInfo.m_enterDoors = null;
                        thisInfo.m_exitDoors = null;
                        thisInfo.m_specialPlaces = null;
                        thisInfo.m_animalPlaces = null;
                        thisInfo.CheckReferences();
                    });

                    // Update render instance.
                    renderManager.UpdateInstance(i);

                    // Update parking spaces in simulation thread.
                    if (buildings[i].Info.m_hasParkingSpaces != 0)
                    {
                        ushort buildingID = i;
                        simulationManager.AddAction(() => buildingManager.UpdateParkingSpaces(buildingID, ref buildings[buildingID]));
                    }

                    // Calculate building render group.
                    Vector3 position = buildings[i].m_position;
                    int num = Mathf.Clamp((int)((position.x / 64f) + 135f), 0, 269);
                    int num2 = Mathf.Clamp((int)((position.z / 64f) + 135f), 0, 269);
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
            s_dirtyList.Clear();

            // Restore simulation speed.
            simulationManager.SimulationPaused = simulationWasPaused;
        }
    }
}
