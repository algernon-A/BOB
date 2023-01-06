// <copyright file="BuildingReplacementBase.cs" company="algernon (K. Algernon A. Sheppard)">
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
    /// Base class for building replacement.
    /// </summary>
    internal abstract class BuildingReplacementBase
    {
        /// <summary>
        /// Gets the config file list of building elements relevant to the current replacement type.
        /// </summary>
        protected abstract List<BOBConfig.BuildingElement> BuildingElementList { get; }

        /// <summary>
        /// Gets the priority level of this replacmeent type.
        /// </summary>
        protected abstract ReplacementPriority ThisPriority { get; }

        /// <summary>
        /// Applies a new (or updated) replacement.
        /// </summary>
        /// <param name="buildingInfo">Targeted building prefab.</param>
        /// <param name="targetInfo">Targeted (original) prop prefab.</param>
        /// <param name="replacementInfo">Replacment prop prefab.</param>
        /// <param name="propIndex">Prop index to apply replacement to (ignored).</param>
        /// <param name="position">Target prop position.</param>
        /// <param name="angle">Replacment prop angle adjustment.</param>
        /// <param name="offsetX">Replacment X position offset.</param>
        /// <param name="offsetY">Replacment Y position offset.</param>
        /// <param name="offsetZ">Replacment Z position offset.</param>
        /// <param name="probability">Replacement probability.</param>
        /// <param name="customHeight">Replacement custom height flag.</param>
        /// <param name="existingReplacement">Existing replacement record (null if none).</param>
        internal void Replace(BuildingInfo buildingInfo, PrefabInfo targetInfo, PrefabInfo replacementInfo, int propIndex, Vector3 position, float angle, float offsetX, float offsetY, float offsetZ, int probability, bool customHeight, BOBConfig.BuildingReplacement existingReplacement)
        {
            // Null checks.
            if (targetInfo?.name == null || replacementInfo?.name == null)
            {
                return;
            }

            // Was an existing replacement provided?
            BOBConfig.BuildingReplacement thisReplacement = existingReplacement;
            if (thisReplacement == null)
            {
                // No existing replacement was provided - try to find an existing match.
                thisReplacement = FindReplacement(buildingInfo, propIndex, targetInfo);

                // If a match wasn't found, create a new replacement entry.
                if (thisReplacement == null)
                {
                    thisReplacement = new BOBConfig.BuildingReplacement
                    {
                        ParentInfo = buildingInfo,
                        Target = targetInfo.name,
                        TargetInfo = targetInfo,
                        PropIndex = propIndex,
                        Xpos = position.x,
                        Ypos = position.y,
                        Zpos = position.z,
                    };
                    ReplacementEntry(buildingInfo).Add(thisReplacement);
                }
            }

            // Add/replace replacement data.
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

            // Apply replacement.
            ApplyReplacement(thisReplacement);
        }

        /// <summary>
        /// Removes a replacement.
        /// </summary>
        /// <param name="replacement">Replacement record to remove.</param>
        /// <param name="removeEntries">True to remove the removed entries from the list of replacements, false to leave the list unchanged.</param>
        /// <returns>True if the entire building record was removed from the list (due to no remaining replacements for that prefab), false if the prefab remains in the list (has other active replacements).</returns>
        internal virtual bool RemoveReplacement(BOBConfig.BuildingReplacement replacement, bool removeEntries = true)
        {
            // Safety check.
            if (replacement == null)
            {
                Logging.Error("null replacement passed to BuildingReplacementBase.RemoveReplacement");
                return false;
            }

            // Remove all active replacement references.
            BuildingHandlers.RemoveReplacement(replacement);

            // Remove replacement entry from list of replacements, if we're doing so.
            if (removeEntries)
            {
                // Remove from replacement list.
                ReplacementList(replacement.BuildingInfo).Remove(replacement);

                // See if we've got a parent building element record, and if so, if it has any remaining replacement entries.
                BOBConfig.BuildingElement thisElement = BuildingElement(replacement.BuildingInfo);
                if (thisElement != null && (thisElement.Replacements == null || thisElement.Replacements.Count == 0))
                {
                    // No replacement entries left - delete entire building entry and return true to indicate that we've done so.
                    BuildingElementList.Remove(thisElement);
                    return true;
                }
            }

            // If we got here, we didn't remove any building entries from the list; return false.
            return false;
        }

        /// <summary>
        /// Reverts all active replacements.
        /// </summary>
        internal virtual void RevertAll()
        {
            // Iterate through each entry in the replacement list.
            foreach (BOBConfig.BuildingElement buildingElement in BuildingElementList)
            {
                foreach (BOBConfig.BuildingReplacement replacement in buildingElement.Replacements)
                {
                    // Remove any references to this replacement from all building handlers.
                    BuildingHandlers.RemoveReplacement(replacement);
                }
            }

            // Clear configuration file entry.
            BuildingElementList.Clear();
        }

        /// <summary>
        /// Deserialises a building element list.
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
        /// Applies a replacement.
        /// </summary>
        /// <param name="replacement">Replacement record to apply.</param>
        protected abstract void ApplyReplacement(BOBConfig.BuildingReplacement replacement);

        /// <summary>
        /// Finds any existing replacement relevant to the provided arguments.
        /// </summary>
        /// <param name="buildingInfo">Building prefab.</param>
        /// <param name="propIndex">Prop index.</param>
        /// <param name="targetInfo">Target prop/tree prefab.</param>
        /// <returns>Existing replacement entry, if one was found, otherwise null.</returns>
        protected abstract BOBConfig.BuildingReplacement FindReplacement(BuildingInfo buildingInfo, int propIndex, PrefabInfo targetInfo);

        /// <summary>
        /// Gets the relevant replacement list entry from the active configuration file, if any.
        /// </summary>
        /// <param name="buildingInfo">Building prefab.</param>
        /// <returns>Replacement list for the specified building prefab (null if none).</returns>
        protected virtual List<BOBConfig.BuildingReplacement> ReplacementList(BuildingInfo buildingInfo) => BuildingElement(buildingInfo)?.Replacements;

        /// <summary>
        /// Gets the relevant building replacement list entry from the active configuration file, creating a new building entry if none already exists.
        /// </summary>
        /// <param name="buildingInfo">Building prefab.</param>
        /// <returns>Replacement list for the specified building prefab.</returns>
        protected virtual List<BOBConfig.BuildingReplacement> ReplacementEntry(BuildingInfo buildingInfo)
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
        /// Returns the configuration file record for the specified building prefab.
        /// </summary>
        /// <param name="buildingInfo">Building prefab.</param>
        /// <returns>Replacement record for the specified building prefab (null if none).</returns>
        protected BOBConfig.BuildingElement BuildingElement(BuildingInfo buildingInfo) => buildingInfo == null ? null : BuildingElementList?.Find(x => x.BuildingInfo == buildingInfo);

        /// <summary>
        /// Deserialises a replacement list.
        /// </summary>
        /// <param name="buildingInfo">Building prefab.</param>
        /// <param name="replacementList">Replacement list to deserialise.</param>
        protected void Deserialize(BuildingInfo buildingInfo, List<BOBConfig.BuildingReplacement> replacementList)
        {
            // Iterate through each element in the provided list.
            foreach (BOBConfig.BuildingReplacement replacement in replacementList)
            {
                try
                {
                    // Assign building info.
                    replacement.ParentInfo = buildingInfo;

                    // Try to find target prefab.
                    replacement.TargetInfo = replacement.IsTree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacement.Target) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacement.Target);

                    // Try to find replacement prefab.
                    replacement.ReplacementInfo = ConfigurationUtils.FindReplacementPrefab(replacement.ReplacementName, replacement.IsTree);

                    // Try to apply the replacement.
                    ApplyReplacement(replacement);
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