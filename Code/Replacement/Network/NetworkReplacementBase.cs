// <copyright file="NetworkReplacementBase.cs" company="algernon (K. Algernon A. Sheppard)">
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
    /// Base class for network replacement.
    /// </summary>
    internal abstract class NetworkReplacementBase
    {
        /// <summary>
        /// Gets the config file list of network elements relevant to the current replacement type.
        /// </summary>
        protected abstract List<BOBConfig.NetworkElement> NetworkElementList { get; }

        /// <summary>
        /// Gets the priority level of this replacmeent type.
        /// </summary>
        protected abstract ReplacementPriority ThisPriority { get; }

        /// <summary>
        /// Applies a new (or updated) replacement.
        /// </summary>
        /// <param name="netInfo">Targeted network prefab.</param>
        /// <param name="targetInfo">Targeted (original) prop prefab.</param>
        /// <param name="replacementInfo">Replacment prop prefab.</param>
        /// <param name="laneIndex">Targeted lane index (in parent network).</param>
        /// <param name="propIndex">Prop index to apply replacement to.</param>
        /// <param name="position">Target prop position.</param>
        /// <param name="angle">Replacment prop angle adjustment.</param>
        /// <param name="offsetX">Replacment X position offset.</param>
        /// <param name="offsetY">Replacment Y position offset.</param>
        /// <param name="offsetZ">Replacment Z position offset.</param>
        /// <param name="probability">Replacement probability.</param>
        /// <param name="repeatDistance">Replacement repeat distance.</param>
        /// <param name="existingReplacement">Existing replacement record (null if none).</param>
        internal void Replace(NetInfo netInfo, PrefabInfo targetInfo, PrefabInfo replacementInfo, int laneIndex, int propIndex, Vector3 position, float angle, float offsetX, float offsetY, float offsetZ, int probability, float repeatDistance, BOBConfig.NetReplacement existingReplacement)
        {
            // Null checks.
            if (targetInfo?.name == null || replacementInfo?.name == null)
            {
                return;
            }

            // Was an existing replacement provided?
            BOBConfig.NetReplacement thisReplacement = existingReplacement;
            if (thisReplacement == null)
            {
                // No existing replacement was provided - try to find an existing match.
                thisReplacement = FindReplacement(netInfo, laneIndex, propIndex, targetInfo);

                // If a match wasn't found, create a new replacement entry.
                if (thisReplacement == null)
                {
                    thisReplacement = new BOBConfig.NetReplacement
                    {
                        ParentInfo = netInfo,
                        Target = targetInfo.name,
                        TargetInfo = targetInfo,
                        PropIndex = propIndex,
                        LaneIndex = laneIndex,
                        Xpos = position.x,
                        Ypos = position.y,
                        Zpos = position.z,
                    };
                    ReplacementEntry(netInfo).Add(thisReplacement);
                }
            }

            // Add/replace replacement data.
            thisReplacement.IsTree = targetInfo is TreeInfo;
            thisReplacement.Angle = angle;
            thisReplacement.OffsetX = offsetX;
            thisReplacement.OffsetY = offsetY;
            thisReplacement.OffsetZ = offsetZ;
            thisReplacement.Probability = probability;
            thisReplacement.RepeatDistance = repeatDistance;

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
        /// <param name="removeEntries">True to remove the reverted entries from the list of replacements, false to leave the list unchanged.</param>
        /// <returns>True if the entire network record was removed from the list (due to no remaining replacements for that prefab), false if the prefab remains in the list (has other active replacements).</returns>
        internal virtual bool RemoveReplacement(BOBConfig.NetReplacement replacement, bool removeEntries = true)
        {
            // Safety check.
            if (replacement == null)
            {
                Logging.Error("null replacement passed to NetworkReplacementBase.RemoveReplacement");
                return false;
            }

            // Remove all active replacement references.
            NetHandlers.RemoveReplacement(replacement);

            // Remove replacement entry from list of replacements, if we're doing so.
            if (removeEntries)
            {
                // Remove from replacement list.
                ReplacementList(replacement.NetInfo).Remove(replacement);

                // See if we've got a parent network element record, and if so, if it has any remaining replacement entries.
                BOBConfig.NetworkElement thisElement = NetworkElement(replacement.NetInfo);
                if (thisElement != null && (thisElement.Replacements == null || thisElement.Replacements.Count == 0))
                {
                    // No replacement entries left - delete entire network entry and return true to indicate that we've done so.
                    NetworkElementList.Remove(thisElement);
                    return true;
                }
            }

            // If we got here, we didn't remove any network entries from the list; return false.
            return false;
        }

        /// <summary>
        /// Reverts all active replacements.
        /// </summary>
        internal virtual void RevertAll()
        {
            // Iterate through each entry in the replacement list.
            foreach (BOBConfig.NetworkElement netElement in NetworkElementList)
            {
                foreach (BOBConfig.NetReplacement replacement in netElement.Replacements)
                {
                    // Remove any references to this replacement from all network handlers.
                    NetHandlers.RemoveReplacement(replacement);
                }
            }

            // Clear configuration file entry.
            NetworkElementList.Clear();
        }

        /// <summary>
        /// Deserialises a network element list.
        /// </summary>
        /// <param name="elementList">Element list to deserialise.</param>
        internal void Deserialize(List<BOBConfig.NetworkElement> elementList)
        {
            // Iterate through each element in the provided list.
            foreach (BOBConfig.NetworkElement element in elementList)
            {
                // Try to find network prefab.
                element.Prefab = PrefabCollection<NetInfo>.FindLoaded(element.Network);

                // Don't bother deserializing further if the network info wasn't found.
                if (element.NetInfo != null)
                {
                    Deserialize(element.NetInfo, element.Replacements);
                }
            }
        }

        /// <summary>
        /// Applies a replacement.
        /// </summary>
        /// <param name="replacement">Replacement record to apply.</param>
        protected abstract void ApplyReplacement(BOBConfig.NetReplacement replacement);

        /// <summary>
        /// Finds any existing replacement relevant to the provided arguments.
        /// </summary>
        /// <param name="netInfo">Network info.</param>
        /// <param name="laneIndex">Lane index.</param>
        /// <param name="propIndex">Prop index.</param>
        /// <param name="targetInfo">Target prop/tree prefab.</param>
        /// <returns>Existing replacement entry, if one was found, otherwise null.</returns>
        protected abstract BOBConfig.NetReplacement FindReplacement(NetInfo netInfo, int laneIndex, int propIndex, PrefabInfo targetInfo);

        /// <summary>
        /// Gets the relevant replacement list entry from the active configuration file, if any.
        /// </summary>
        /// <param name="netInfo">Network prefab.</param>
        /// <returns>Replacement list for the specified network prefab (null if none).</returns>
        protected virtual List<BOBConfig.NetReplacement> ReplacementList(NetInfo netInfo) => NetworkElement(netInfo)?.Replacements;

        /// <summary>
        /// Gets the relevant network replacement list entry from the active configuration file, creating a new network entry if none already exists.
        /// </summary>
        /// <param name="netInfo">Network prefab.</param>
        /// <returns>Replacement list for the specified network prefab.</returns>
        protected virtual List<BOBConfig.NetReplacement> ReplacementEntry(NetInfo netInfo)
        {
            // Get existing entry for this network.
            BOBConfig.NetworkElement thisNetwork = NetworkElement(netInfo);

            // If no existing entry, create a new one.
            if (thisNetwork == null)
            {
                thisNetwork = new BOBConfig.NetworkElement(netInfo);
                NetworkElementList.Add(thisNetwork);
            }

            // Return replacement list from this entry.
            return thisNetwork.Replacements;
        }

        /// <summary>
        /// Returns the configuration file record for the specified network prefab.
        /// </summary>
        /// <param name="netInfo">Network prefab.</param>
        /// <returns>Replacement record for the specified network prefab (null if none).</returns>
        protected BOBConfig.NetworkElement NetworkElement(NetInfo netInfo) => netInfo == null ? null : NetworkElementList?.Find(x => x.NetInfo == netInfo);

        /// <summary>
        /// Deserialises a replacement list.
        /// </summary>
        /// <param name="netInfo">Network prefab.</param>
        /// <param name="replacementList">Replacement list to deserialise.</param>
        protected void Deserialize(NetInfo netInfo, List<BOBConfig.NetReplacement> replacementList)
        {
            // Iterate through each element in the provided list.
            foreach (BOBConfig.NetReplacement replacement in replacementList)
            {
                try
                {
                    // Assign network info.
                    replacement.ParentInfo = netInfo;

                    // Try to find target prefab.
                    replacement.TargetInfo = replacement.IsTree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacement.Target) : PrefabCollection<PropInfo>.FindLoaded(replacement.Target);

                    // Try to find replacement prefab.
                    replacement.ReplacementInfo = ConfigurationUtils.FindReplacementPrefab(replacement.ReplacementName, replacement.IsTree);

                    // Try to apply the replacement.
                    ApplyReplacement(replacement);
                }
                catch (Exception e)
                {
                    // Don't let a single failure stop us.
                    Logging.LogException(e, "exception deserializing network replacement");
                }
            }
        }
    }
}