// <copyright file="AllNetworkReplacement.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System.Collections.Generic;
    using AlgernonCommons;

    /// <summary>
    /// Class to manage all-network prop and tree replacements.
    /// </summary>
    internal class AllNetworkReplacement : NetworkReplacementBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AllNetworkReplacement"/> class.
        /// Constructor - initializes instance reference and replacement dictionary.
        /// </summary>
        internal AllNetworkReplacement()
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the active instance.
        /// </summary>
        internal static AllNetworkReplacement Instance { get; private set; }

        /// <summary>
        /// Gets the config file list of elements relevant to the current replacement type.
        /// </summary>
        protected override List<BOBConfig.NetworkElement> NetworkElementList => null;

        /// <summary>
        /// Gets the priority level of this replacmeent type.
        /// </summary>
        protected override ReplacementPriority ThisPriority => ReplacementPriority.AllReplacement;

        /// <summary>
        /// Removes a replacement.
        /// </summary>
        /// <param name="replacement">Replacement record to remove.</param>
        /// <param name="removeEntries">True to remove the reverted entries from the list of replacements, false to leave the list unchanged.</param>
        /// <returns>True if the entire network record was removed from the list (due to no remaining replacements for that prefab), false if the prefab remains in the list (has other active replacements).</returns>
        internal override bool RemoveReplacement(BOBConfig.NetReplacement replacement, bool removeEntries = true)
        {
            // Safety check.
            if (replacement == null)
            {
                Logging.Error("null replacement passed to AllNetworkReplacement.RemoveReplacement");
                return false;
            }

            // Remove any references to this replacement from all prop handlers.
            NetHandlers.RemoveReplacement(replacement);

            // Remove replacement entry from list of replacements, if we're doing so.
            if (removeEntries)
            {
                // Remove from replacement list.
                ReplacementList(replacement.NetInfo).Remove(replacement);
            }

            // If we got here, we didn't remove any network entries from the list; return false.
            return false;
        }

        /// <summary>
        /// Reverts all active replacements.
        /// </summary>
        internal override void RevertAll()
        {
            foreach (BOBConfig.NetReplacement replacement in ReplacementList(null))
            {
                // Remove any references to this replacement from all network handlers.
                NetHandlers.RemoveReplacement(replacement);
            }

            // Clear configuration file.
            ReplacementList(null).Clear();
        }

        /// <summary>
        /// Deserialises a network element list.
        /// </summary>
        /// <param name="elementList">Element list to deserialise.</param>
        internal void Deserialize(List<BOBConfig.NetReplacement> elementList) => Deserialize(null, elementList);

        /// <summary>
        /// Finds any existing replacement relevant to the provided arguments.
        /// </summary>
        /// <param name="netInfo">Network info.</param>
        /// <param name="segmentID">Segment ID (if using a skin; set to 0 otherwise).</param>
        /// <param name="laneIndex">Lane index.</param>
        /// <param name="propIndex">Prop index.</param>
        /// <param name="targetInfo">Target prop/tree prefab.</param>
        /// <returns>Existing replacement entry, if one was found, otherwise null.</returns>
        protected override BOBConfig.NetReplacement FindReplacement(NetInfo netInfo, ushort segmentID, int laneIndex, int propIndex, PrefabInfo targetInfo) =>
            ReplacementList(netInfo)?.Find(x => x.TargetInfo == targetInfo);

        /// <summary>
        /// Applies an all-network prop replacement.
        /// </summary>
        /// <param name="replacement">Replacement record to apply.</param>
        protected override void ApplyReplacement(BOBConfig.NetReplacement replacement)
        {
            // Don't do anything if prefabs can't be found.
            if (replacement?.TargetInfo == null || replacement.ReplacementInfo == null)
            {
                return;
            }

            // Iterate through each loaded network and record props to be replaced.
            for (int i = 0; i < PrefabCollection<NetInfo>.LoadedCount(); ++i)
            {
                // Get local reference.
                NetInfo netInfo = PrefabCollection<NetInfo>.GetLoaded((uint)i);

                // Skip any null networks, or netorks without lanes.
                if (netInfo?.m_lanes == null)
                {
                    continue;
                }

                // Iterate through each lane.
                for (int laneIndex = 0; laneIndex < netInfo.m_lanes.Length; ++laneIndex)
                {
                    // Local references.
                    NetInfo.Lane thisLane = netInfo.m_lanes[laneIndex];
                    NetLaneProps.Prop[] theseLaneProps = thisLane?.m_laneProps?.m_props;

                    // If no props in this lane, skip it and go to the next one.
                    if (theseLaneProps == null)
                    {
                        continue;
                    }

                    // Iterate through each prop in lane.
                    for (int propIndex = 0; propIndex < theseLaneProps.Length; ++propIndex)
                    {
                        // Local reference.
                        NetLaneProps.Prop thisLaneProp = theseLaneProps[propIndex];

                        // If invalid entry, skip this one.
                        if (thisLaneProp == null)
                        {
                            continue;
                        }

                        // Note current props.
                        TreeInfo thisTree = thisLaneProp.m_finalTree;
                        PropInfo thisProp = thisLaneProp.m_finalProp;

                        // Get any active handler.
                        LanePropHandler handler = NetHandlers.GetHandler(thisLane, propIndex);
                        if (handler != null)
                        {
                            // Active handler found - use original values for checking eligibility (instead of currently active values).
                            thisTree = handler.OriginalTree;
                            thisProp = handler.OriginalProp;
                        }

                        // See if this prop matches our replacement.
                        bool treeMatch = replacement.IsTree && thisTree != null && thisTree == replacement.TargetInfo;
                        bool propMatch = !replacement.IsTree && thisProp != null && thisProp == replacement.TargetInfo;
                        if (treeMatch | propMatch)
                        {
                            // Match!  Create new handler if there wasn't an existing one.
                            if (handler == null)
                            {
                                handler = NetHandlers.GetOrAddHandler(netInfo, 0, thisLane, laneIndex, propIndex);
                            }

                            // Set the new replacement.
                            handler.SetReplacement(replacement, ThisPriority);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the relevant replacement list entry from the active configuration file, if any.
        /// </summary>
        /// <param name="netInfo">Network prefab.</param>
        /// <returns>Replacement list for the specified network prefab (null if none).</returns>
        protected override List<BOBConfig.NetReplacement> ReplacementList(NetInfo netInfo) => ConfigurationUtils.CurrentConfig.AllNetworkProps;

        /// <summary>
        /// Gets the relevant network replacement list entry from the active configuration file, creating a new network entry if none already exists.
        /// </summary>
        /// <param name="netInfo">Network prefab.</param>
        /// <returns>Replacement list for the specified network prefab.</returns>
        protected override List<BOBConfig.NetReplacement> ReplacementEntry(NetInfo netInfo) => ReplacementList(netInfo);
    }
}
