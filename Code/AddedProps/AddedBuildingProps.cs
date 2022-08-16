// <copyright file="AddedBuildingProps.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System;
    using System.Collections.Generic;
    using AlgernonCommons;
    using UnityEngine;

    /// <summary>
    /// Class to manage added building props.
    /// </summary>
    internal class AddedBuildingProps
    {
        // Dictionary of active additional props.
        private readonly Dictionary<BuildingInfo, BuildingInfo.Prop[]> _changedBuildings;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddedBuildingProps"/> class.
        /// </summary>
        internal AddedBuildingProps()
        {
            // Init dictionary.
            _changedBuildings = new Dictionary<BuildingInfo, BuildingInfo.Prop[]>();

            // Set instance reference.
            Instance = this;
        }

        /// <summary>
        /// Gets the active instance.
        /// </summary>
        internal static AddedBuildingProps Instance { get; private set; }

        /// <summary>
        /// Gets the config file list of building elements relevant to the current replacement type.
        /// </summary>
        private List<BOBConfig.BuildingElement> BuildingElementList => ConfigurationUtils.CurrentConfig.AddedBuildingProps;

        /// <summary>
        /// Checks whether or not the prop with a given building and prop index is an added prop.
        /// </summary>
        /// <param name="building">Building prefab to check.</param>
        /// <param name="index">Prop index to check.</param>
        /// <returns>True if the prop is added, false if not.</returns>
        internal bool IsAdded(BuildingInfo building, int index)
        {
            // Null check.
            if (building == null)
            {
                Logging.Error("null building passed to AddedBuildingProps.IsAdded");
                return false;
            }

            // Return value depends on the dictionary containing an entry.
            if (_changedBuildings.TryGetValue(building, out BuildingInfo.Prop[] originalProps))
            {
                // If there's no original props array,there were no original props; therefore anything here is added.
                if (originalProps == null)
                {
                    return true;
                }

                return index >= originalProps.Length;
            }

            // If we got here, there's no added prop entry.
            return false;
        }

        /// <summary>
        /// Retrieves a currently-applied replacement entry for the given network, lane and prop index.
        /// </summary>
        /// <param name="buildingInfo">Network prefab.</param>
        /// <param name="propIndex">Prop index number.</param>
        /// <returns>Currently-applied individual network replacement (null if none).</returns>
        internal BOBConfig.BuildingReplacement ReplacementRecord(BuildingInfo buildingInfo, int propIndex) => ReplacementEntry(buildingInfo)?.Find(x => x.PropIndex == propIndex);

        /// <summary>
        /// Adds a new prop to a building after updating the config file with the new entry.
        /// </summary>
        /// <param name="data">Building replacement data record.</param>
        internal void AddNew(BOBConfig.BuildingReplacement data)
        {
            // Add entry to configuration file.
            List<BOBConfig.BuildingReplacement> replacementList = ReplacementEntry(data.BuildingInfo);
            BOBConfig.BuildingReplacement replacementEntry = replacementList.Find(x => x.PropIndex == data.PropIndex);
            if (replacementEntry != null)
            {
                replacementList.Remove(replacementEntry);
            }

            replacementList.Add(data);

            // Add building to changed buildings list, if it's not already there.
            if (!_changedBuildings.ContainsKey(data.BuildingInfo))
            {
                _changedBuildings.Add(data.BuildingInfo, data.BuildingInfo.m_props);
            }

            // Add prop to building.
            AddProp(data);
        }

        /// <summary>
        /// Removes the specified added building prop.
        /// </summary>
        /// <param name="building">Building prefab.</param>
        /// <param name="index">Prop index.</param>
        internal void RemoveNew(BuildingInfo building, int index)
        {
            // Don't do anything if no valid building entry.
            if (_changedBuildings.TryGetValue(building, out BuildingInfo.Prop[] originalProps))
            {
                // Restore original prop list.
                building.m_props = originalProps;

                // Remove entry from list.
                List<BOBConfig.BuildingReplacement> replacementList = ReplacementEntry(building);
                if (replacementList != null && replacementList.Count > 0)
                {
                    // Found an entry for this building - try to find the matching prop index.
                    BOBConfig.BuildingReplacement targetEntry = replacementList.Find(x => x.PropIndex == index);
                    if (targetEntry != null)
                    {
                        // Found a matching index - remove the prop from the list.
                        replacementList.Remove(targetEntry);
                    }

                    // Add building to dirty list.
                    BuildingData.DirtyList.Add(building);

                    // Check if any added props remaining for this building.
                    if (replacementList.Count > 0)
                    {
                        // Yes - iterate through and re-add them (updating indexes).
                        foreach (BOBConfig.BuildingReplacement replacement in replacementList)
                        {
                            if (replacement.PropIndex >= 0)
                            {
                                replacement.PropIndex = AddProp(replacement);
                            }
                        }
                    }
                    else
                    {
                        // No remaining added props - remove building entry entirely.
                        _changedBuildings.Remove(building);
                    }
                }
            }
        }

        /// <summary>
        /// Updates an existing replacement entry.
        /// </summary>
        /// <param name="buildingInfo">Targeted building prefab.</param>
        /// <param name="targetInfo">Targeted (original) prop prefab.</param>
        /// <param name="replacementInfo">Replacment prop prefab.</param>
        /// <param name="propIndex">Prop index to apply replacement to.</param>
        /// <param name="angle">Replacment prop angle adjustment.</param>
        /// <param name="offsetX">Replacment X position offset.</param>
        /// <param name="offsetY">Replacment Y position offset.</param>
        /// <param name="offsetZ">Replacment Z position offset.</param>
        /// <param name="probability">Replacement probability.</param>
        /// <param name="customHeight">Replacement custom height flag.</param>
        internal void Update(BuildingInfo buildingInfo, PrefabInfo targetInfo, PrefabInfo replacementInfo, int propIndex, float angle, float offsetX, float offsetY, float offsetZ, int probability, bool customHeight)
        {
            // Check for valid daa.
            if (replacementInfo?.name == null || propIndex < 0 || buildingInfo?.m_props == null || propIndex >= buildingInfo.m_props.Length)
            {
                Logging.Error("invalid data passed to AddedBuildingProps.Update");
                return;
            }

            // Existing reference checks.
            if (!IsAdded(buildingInfo, propIndex) || !_changedBuildings.ContainsKey(buildingInfo))
            {
                Logging.Error("unrecorded reference passed to AddedBuildingProps.Update");
                return;
            }

            // Get building element list.
            List<BOBConfig.BuildingReplacement> replacementList = ReplacementEntry(buildingInfo);

            // Try to find matching entry.
            BOBConfig.BuildingReplacement thisReplacement = replacementList.Find(x => x.PropIndex == propIndex);

            if (thisReplacement != null)
            {
                // Update replacment entry.
                thisReplacement.PropIndex = propIndex;
                thisReplacement.IsTree = targetInfo is TreeInfo;
                thisReplacement.Angle = angle;
                thisReplacement.OffsetX = offsetX;
                thisReplacement.OffsetY = offsetY;
                thisReplacement.OffsetZ = offsetZ;
                thisReplacement.Probability = probability;
                thisReplacement.CustomHeight = customHeight;

                // Record replacement prop.
                thisReplacement.ReplacementInfo = replacementInfo;
                thisReplacement.ReplacementName = replacementInfo.name;

                // Apply update to targeted prop.
                BuildingInfo.Prop thisProp = buildingInfo.m_props[propIndex];
                thisProp.m_prop = replacementInfo as PropInfo;
                thisProp.m_tree = replacementInfo as TreeInfo;
                thisProp.m_radAngle = angle * Mathf.Deg2Rad;
                thisProp.m_position = new Vector3(offsetX, offsetY, offsetZ);
                thisProp.m_probability = probability;
                thisProp.m_fixedHeight = customHeight;
            }
        }

        /// <summary>
        /// Reverts all added building props.
        /// </summary>
        internal void RevertAll()
        {
            // Iterate through each building in dictionary of added props.
            foreach (KeyValuePair<BuildingInfo, BuildingInfo.Prop[]> entry in _changedBuildings)
            {
                // Restore original props.
                entry.Key.m_props = entry.Value;

                // Mark as dirty.
                BuildingData.DirtyList.Add(entry.Key);
            }

            // Clear element list.
            BuildingElementList.Clear();
        }

        /// <summary>
        /// Deserialises and applies added props.
        /// </summary>
        /// <param name="elementList">Element list to deserialise.</param>
        internal void Deserialize(List<BOBConfig.BuildingElement> elementList)
        {
            // Iterate through each element in the provided list.
            foreach (BOBConfig.BuildingElement element in elementList)
            {
                // Try to find building prefab.
                element.Prefab = PrefabCollection<BuildingInfo>.FindLoaded(element.Building);

                // Don't bother deserializing further if the building info wasn't found.
                if (element.BuildingInfo != null)
                {
                    Deserialize(element.BuildingInfo, element.Replacements);
                }
            }
        }

        /// <summary>
        /// Adds a new tree or prop to a building prefab.
        /// </summary>
        private int AddProp(BOBConfig.BuildingReplacement replacement)
        {
            // Make sure a valid replacement prefab is set.
            BuildingInfo buildingInfo = replacement?.BuildingInfo;
            if (buildingInfo != null)
            {
                // New prop index.
                int newIndex = 0;

                // Check to see if we've got a current prop array.
                if (replacement.BuildingInfo.m_props != null)
                {
                    // Existing m_props array - check that we've got space for another entry.
                    newIndex = buildingInfo.m_props.Length;
                    if (newIndex > 63)
                    {
                        // Props maxed out - exit.
                        return -1;
                    }

                    // Get old props reference.
                    BuildingInfo.Prop[] oldBuildingProps = buildingInfo.m_props;

                    // Create new props array with one extra entry, and copy the old props to it.
                    buildingInfo.m_props = new BuildingInfo.Prop[newIndex + 1];
                    for (int i = 0; i < newIndex; ++i)
                    {
                        buildingInfo.m_props[i] = oldBuildingProps[i];
                    }
                }
                else
                {
                    // No m_props array already; create one.
                    buildingInfo.m_props = new BuildingInfo.Prop[1];
                }

                // Update reference with new index.
                replacement.PropIndex = newIndex;

                // Add new prop.
                Logging.Message("adding new prop for building ", buildingInfo.name, " at index ", newIndex);
                buildingInfo.m_props[newIndex] = new BuildingInfo.Prop
                {
                    m_radAngle = replacement.Angle * Mathf.Deg2Rad,
                    m_prop = replacement.ReplacementInfo as PropInfo,
                    m_tree = replacement.ReplacementInfo as TreeInfo,
                    m_finalProp = replacement.ReplacementInfo as PropInfo,
                    m_finalTree = replacement.ReplacementInfo as TreeInfo,
                    m_fixedHeight = replacement.CustomHeight,
                    m_position = new Vector3(replacement.OffsetX, replacement.OffsetY, replacement.OffsetZ),
                    m_probability = replacement.Probability,
                    m_index = newIndex,
                };

                // Add building to dirty list.
                BuildingData.DirtyList.Add(buildingInfo);

                return newIndex;
            }

            return -1;
        }

        /// <summary>
        /// Returns the configuration file record for the specified building prefab.
        /// </summary>
        /// <param name="buildingInfo">Building prefab.</param>
        /// <returns>Replacement record for the specified building prefab (null if none).</returns>
        private BOBConfig.BuildingElement BuildingElement(BuildingInfo buildingInfo) => buildingInfo == null ? null : BuildingElementList?.Find(x => x.BuildingInfo == buildingInfo);

        /// <summary>
        /// Gets the relevant building replacement list entry from the active configuration file, creating a new building entry if none already exists.
        /// </summary>
        /// <param name="buildingInfo">Building prefab.</param>
        /// <returns>Replacement list for the specified building prefab.</returns>
        private List<BOBConfig.BuildingReplacement> ReplacementEntry(BuildingInfo buildingInfo)
        {
            // Get existing entry for this building.
            BOBConfig.BuildingElement thisBuilding = BuildingElement(buildingInfo);

            // If no existing entry, create a new one.
            if (thisBuilding == null)
            {
                thisBuilding = new BOBConfig.BuildingElement(buildingInfo);
                BuildingElementList.Add(thisBuilding);
            }

            // Return replacement list from this entry.
            return thisBuilding.Replacements;
        }

        /// <summary>
        /// Deserialises a replacement list.
        /// </summary>
        /// <param name="buildingInfo">Building prefab.</param>
        /// <param name="replacementList">Replacement list to deserialise.</param>
        private void Deserialize(BuildingInfo buildingInfo, List<BOBConfig.BuildingReplacement> replacementList)
        {
            // Creeate record for building info.
            if (!_changedBuildings.ContainsKey(buildingInfo))
            {
                _changedBuildings.Add(buildingInfo, buildingInfo.m_props);
            }

            // Iterate through each element in the provided list.
            foreach (BOBConfig.BuildingReplacement replacement in replacementList)
            {
                replacement.PropIndex = -1;

                try
                {
                    // Assign building info.
                    replacement.ParentInfo = buildingInfo;

                    // Try to find target prefab.
                    replacement.TargetInfo = replacement.IsTree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacement.Target) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacement.Target);

                    // Try to find replacement prefab.
                    replacement.ReplacementInfo = ConfigurationUtils.FindReplacementPrefab(replacement.ReplacementName, replacement.IsTree);

                    // Try to apply the replacement.
                    replacement.PropIndex = AddProp(replacement);
                }
                catch (Exception e)
                {
                    // Don't let a single failure stop us.
                    Logging.LogException(e, "exception deserializing building replacement");
                }
            }
        }
    }
}