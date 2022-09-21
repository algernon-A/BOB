// <copyright file="IndividualNetworkReplacement.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System.Collections.Generic;
    using AlgernonCommons;

    /// <summary>
    /// Class to manage individual network prop and tree replacements.
    /// </summary>
    internal class IndividualNetworkReplacement : NetworkReplacementBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IndividualNetworkReplacement"/> class.
        /// </summary>
        internal IndividualNetworkReplacement()
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the active instance.
        /// </summary>
        internal static IndividualNetworkReplacement Instance { get; private set; }

        /// <summary>
        /// Gets the config file list of network elements relevant to the current replacement type.
        /// </summary>
        protected override List<BOBConfig.NetworkElement> NetworkElementList => ConfigurationUtils.CurrentConfig.IndNetworks;

        /// <summary>
        /// Gets the priority level of this replacmeent type.
        /// </summary>
        protected override ReplacementPriority ThisPriority => ReplacementPriority.IndividualReplacement;

        /// <summary>
        /// Finds any existing replacement relevant to the provided arguments.
        /// </summary>
        /// <param name="netInfo">Network info.</param>
        /// <param name="laneIndex">Lane index.</param>
        /// <param name="propIndex">Prop index.</param>
        /// <param name="targetInfo">Target prop/tree prefab.</param>
        /// <returns>Existing replacement entry, if one was found, otherwise null.</returns>
        protected override BOBConfig.NetReplacement FindReplacement(NetInfo netInfo, int laneIndex, int propIndex, PrefabInfo targetInfo) =>
            ReplacementList(netInfo)?.Find(x => x.LaneIndex == laneIndex && x.PropIndex == propIndex);

        /// <summary>
        /// Applies a replacement.
        /// </summary>
        /// <param name="replacement">Replacement record to apply.</param>
        protected override void ApplyReplacement(BOBConfig.NetReplacement replacement)
        {
            // Don't do anything if prefabs can't be found.
            if (replacement?.TargetInfo == null || replacement.ReplacementInfo == null || replacement.NetInfo?.m_lanes == null)
            {
                return;
            }

            // Check lane index.
            if (replacement.LaneIndex < 0 || replacement.LaneIndex >= replacement.NetInfo.m_lanes.Length)
            {
                Logging.Message("ignoring invalid individual network replacement lane index ", replacement.LaneIndex, " for network ", replacement.NetInfo.name);
                return;
            }

            // Check lane record.
            NetInfo.Lane thisLane = replacement.NetInfo.m_lanes[replacement.LaneIndex];
            if (thisLane == null)
            {
                Logging.Message("ignoring invalid individual network replacement lane index ", replacement.LaneIndex, " for network ", replacement.NetInfo.name);
                return;
            }

            // Check prop index.
            if (thisLane.m_laneProps?.m_props == null || replacement.PropIndex < 0 || replacement.PropIndex >= thisLane.m_laneProps.m_props.Length)
            {
                Logging.Message("ignoring invalid individual network replacement prop index ", replacement.PropIndex, " for network ", replacement.NetInfo.name);
                return;
            }

            // Don't apply replacement if this is an added prop.
            if (AddedNetworkProps.Instance.IsAdded(replacement.NetInfo, replacement.LaneIndex, replacement.PropIndex))
            {
                return;
            }

            // Check prop.
            NetLaneProps.Prop thisLaneProp = thisLane.m_laneProps.m_props[replacement.PropIndex];
            if (thisLaneProp == null)
            {
                return;
            }

            // Set the new replacement.
            NetHandlers.GetOrAddHandler(replacement.NetInfo, thisLane, replacement.PropIndex).SetReplacement(replacement, ThisPriority);
        }
    }
}