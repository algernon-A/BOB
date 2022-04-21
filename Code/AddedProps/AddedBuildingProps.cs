using System.Collections.Generic;
using UnityEngine;


namespace BOB
{
    /// <summary>
    /// Class to manage added building props.
    /// </summary>
    internal class AddedBuildingProps
    {
        // Dictionary of active additional props.
        private readonly Dictionary<BuildingInfo, Dictionary<int, BOBBuildingReplacement>> newProps;

        /// <summary>
        /// Instance reference.
        /// </summary>
        public static AddedBuildingProps Instance { get; private set; }


        /// <summary>
        /// Constructor.
        /// </summary>
        internal AddedBuildingProps()
        {
            // Init dictionary.
            newProps = new Dictionary<BuildingInfo, Dictionary<int, BOBBuildingReplacement>>();

            // Set instance reference.
            Instance = this;
        }


        /// <summary>
        /// Checks whether or not the prop with a given building and prop index is an added prop.
        /// </summary>
        /// <param name="building">Building prefab to check</param>
        /// <param name="index">Prop index to check</param>
        /// <returns>True if the prop is added, false if not.</returns>
        internal bool IsAdded(BuildingInfo building, int index)
        {
            // Return value depends on the dictionary containing an entry.
            if (newProps.TryGetValue(building, out Dictionary<int, BOBBuildingReplacement> buildingEntry))
            {
                return buildingEntry.ContainsKey(index);
            }

            // If we got here, there's no added prop entry.
            return false;
        }


        /// <summary>
        /// Returns the replacement record for the given added prop.
        /// </summary>
        /// <param name="building">Building prefab to check</param>
        /// <param name="index">Prop index to check</param>
        /// <returns>Replacment record if available, null if none</returns>
        internal BOBBuildingReplacement GetRecord(BuildingInfo building, int index)
        {
            // Check for building entry.
            if (newProps.TryGetValue(building,out Dictionary<int, BOBBuildingReplacement> buildingEntry))
            {
                // Success - return index entry.
                buildingEntry.TryGetValue(index, out BOBBuildingReplacement record);
                return record;
            }

            // If we got here, there's no added prop entry.
            return null;
        }


        /// <summary>
        /// Records a new building prop in the reference dictionary.
        /// </summary>
        /// <param name="building">Building prefab</param>
        /// <param name="index">Prop index</param>
        /// <param name="data">Building replacement data record</param>
        internal void RecordNew(BuildingInfo building, int index, BOBBuildingReplacement data)
        {
            // If the dictionary doesn't already contain an entry for this building prefab, create one.
            if (!newProps.TryGetValue(building, out Dictionary<int, BOBBuildingReplacement> buildingEntry))
            {
                newProps.Add(building, buildingEntry = new Dictionary<int, BOBBuildingReplacement>());
            }

            // Check to see if we've already got an entry for this prop.
            if (!buildingEntry.ContainsKey(index))
            {
                // No existing entry - add one.
                buildingEntry.Add(index, data);
            }
            else
            {
                // Existing entry - update it.
                buildingEntry[index] = data;
            }
        }


        /// <summary>
        /// Removes the specified building prop from the reference dictionary.
        /// </summary>
        /// <param name="building">Building prefab</param>
        /// <param name="index">Prop index</param>
        internal void RemoveNew(BuildingInfo building, int index)
        {
            // Don't do anything if no valid building entry.
            if (!newProps.TryGetValue(building, out Dictionary<int, BOBBuildingReplacement> buildingEntry))
            {
                // Remove index from building.
                if (buildingEntry.Remove(index))
                {
                    // Removal was successful - gather any higher-numbered index references for repacking.
                    List<int> repackIndexes = new List<int>();
                    foreach (ushort entry in buildingEntry.Keys)
                    {
                        // Add any higher index numbers to list.
                        if (entry > index)
                        {
                            repackIndexes.Add(entry);
                        }
                    }

                    // Repack higher index numbers with updated indexes (reflecting the removed index).
                    foreach (ushort repackIndex in repackIndexes)
                    {
                        BOBBuildingReplacement replacement = buildingEntry[repackIndex];
                        buildingEntry.Add(repackIndex - 1, replacement);
                        buildingEntry.Remove(repackIndex);
                    }
                }

                // Remove building entry if no further index entries.
                if (buildingEntry.Count == 0)
                {
                    newProps.Remove(building);
                }
            }
        }


        /// <summary>
        /// Updates an existing replacement entry.
        /// </summary>
        /// <param name="buildingInfo">Targeted building prefab</param>
        /// <param name="targetInfo">Targeted (original) prop prefab</param>
        /// <param name="replacementInfo">Replacment prop prefab</param>
        /// <param name="propIndex">Prop index to apply replacement to (ignored)</param>
        /// <param name="angle">Replacment prop angle adjustment</param>
        /// <param name="offsetX">Replacment X position offset</param>
        /// <param name="offsetY">Replacment Y position offset</param>
        /// <param name="offsetZ">Replacment Z position offset</param>
        /// <param name="probability">Replacement probability</param>
        /// <param name="customHeight">Replacement custom height flag</param>
        internal void Update(BuildingInfo buildingInfo, PrefabInfo targetInfo, PrefabInfo replacementInfo, int propIndex, float angle, float offsetX, float offsetY, float offsetZ, int probability, bool customHeight)
        {
            // Null and valid target checks.
            if (targetInfo?.name == null
                || replacementInfo?.name == null
                || !newProps.TryGetValue(buildingInfo, out Dictionary<int, BOBBuildingReplacement> buildingEntry)
                || !buildingEntry.TryGetValue((ushort)propIndex, out BOBBuildingReplacement thisReplacement))
            {
                return;
            }

            // Update replacment entry.
            thisReplacement.propIndex = propIndex;
            thisReplacement.isTree = targetInfo is TreeInfo;
            thisReplacement.angle = angle;
            thisReplacement.offsetX = offsetX;
            thisReplacement.offsetY = offsetY;
            thisReplacement.offsetZ = offsetZ;
            thisReplacement.probability = probability;
            thisReplacement.customHeight = customHeight;

            // Record replacement prop.
            thisReplacement.replacementInfo = replacementInfo;
            thisReplacement.Replacement = replacementInfo.name;

            // Apply update to targeted prop.
            BuildingInfo.Prop thisProp = buildingInfo.m_props[propIndex];
            thisProp.m_prop = replacementInfo as PropInfo;
            thisProp.m_tree = replacementInfo as TreeInfo;
            thisProp.m_finalProp = replacementInfo as PropInfo;
            thisProp.m_finalTree = replacementInfo as TreeInfo;
            thisProp.m_radAngle = angle * Mathf.Deg2Rad;
            thisProp.m_position = new Vector3(offsetX, offsetY, offsetZ);
            thisProp.m_probability = probability;
            thisProp.m_fixedHeight = customHeight;
        }
    }
}