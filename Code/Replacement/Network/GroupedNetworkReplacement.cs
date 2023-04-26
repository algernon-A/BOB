// <copyright file="GroupedNetworkReplacement.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System.Collections.Generic;
    using AlgernonCommons;

    /// <summary>
    /// Class to manage network prop and tree replacements.
    /// </summary>
    internal class GroupedNetworkReplacement : NetworkReplacementBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GroupedNetworkReplacement"/> class.
        /// Constructor - initializes instance reference.
        /// </summary>
        internal GroupedNetworkReplacement()
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the active instance..
        /// </summary>
        internal static GroupedNetworkReplacement Instance { get; private set; }

        /// <summary>
        /// Gets the config file list of network elements relevant to the current replacement type.
        /// </summary>
        protected override List<BOBConfig.NetworkElement> NetworkElementList => ConfigurationUtils.CurrentConfig.Networks;

        /// <summary>
        /// Gets the priority level of this replacmeent type.
        /// </summary>
        protected override ReplacementPriority ThisPriority => ReplacementPriority.GroupedReplacement;

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
        /// Applies a replacement.
        /// </summary>
        /// <param name="replacement">Replacement record to apply.</param>
        protected override void ApplyReplacement(BOBConfig.NetReplacement replacement)
        {
            // Don't do anything if prefabs can't be found.
            if (replacement?.TargetInfo == null || replacement.ReplacementInfo == null || replacement.NetInfo?.m_lanes == null)
            {
                Logging.Error("null value passed to NetworkReplacement.ApplyReplacement");
                return;
            }

            // Iterate through each lane.
            NetInfo.Lane[] lanes = replacement.Lanes;
            for (int laneIndex = 0; laneIndex < lanes.Length; ++laneIndex)
            {
                // Local references.
                NetInfo.Lane thisLane = lanes[laneIndex];
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
                            handler = NetHandlers.GetOrAddHandler(replacement.NetInfo, replacement.SegmentID, thisLane, propIndex);
                        }

                        // Set the new replacement.
                        handler.SetReplacement(replacement, ThisPriority);
                    }
                }
            }
        }
    }
}
