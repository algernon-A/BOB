// <copyright file="IndividualNetworkReplacement.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System.Collections.Generic;
    using AlgernonCommons;
    using UnityEngine;

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

            // Don't do anything if prop array can't be found.
            NetLaneProps.Prop[] props = thisLane.m_laneProps?.m_props;
            if (props == null)
            {
                Logging.Error("attempt to apply individual network replacement with null lane  prop array for network ", replacement.NetInfo.name, " on lane ", replacement.LaneIndex);
                return;
            }

            // Find propIndex.
            if (replacement.PropIndex < 0)
            {
                Logging.Message("looking for individual index network match for target prop ", replacement.TargetInfo?.name);
                for (int i = 0; i < props.Length; ++i)
                {
                    NetLaneProps.Prop prop = props[i];

                    // Get prefab values.
                    PropInfo originalProp = prop.m_finalProp;
                    TreeInfo originalTree = prop.m_finalTree;
                    Vector3 originalPosition = prop.m_position;

                    // Check for any active replacements; if there are any, retrieve the original prop info.
                    if (NetHandlers.GetHandler(thisLane, i) is LanePropHandler handler)
                    {
                        originalProp = handler.OriginalFinalProp;
                        originalTree = handler.OriginalFinalTree;
                        originalPosition = handler.OriginalPosition;
                    }

                    if (prop != null)
                    {
                        if ((replacement.IsTree && originalTree == replacement.TargetTree) || (!replacement.IsTree && originalProp == replacement.TargetProp))
                        {
                            if (replacement.Xpos == originalPosition.x && replacement.Ypos == originalPosition.y && replacement.Zpos == originalPosition.z)
                            {
                                Logging.Message("found index match at ", i);
                                replacement.PropIndex = i;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                // Legacy index found - check bounds.
                if (replacement.PropIndex < props.Length)
                {
                    // Record the currrent (original) prop position.
                    Vector3 originalPosition = props[replacement.PropIndex].m_position;

                    // Check for any active replacements; if there are any, retrieve the original prop position.
                    if (NetHandlers.GetHandler(thisLane, replacement.PropIndex) is LanePropHandler handler)
                    {
                        originalPosition = handler.OriginalPosition;
                    }

                    // Record original values.
                    replacement.Xpos = originalPosition.x;
                    replacement.Ypos = originalPosition.y;
                    replacement.Zpos = originalPosition.z;
                }
                else
                {
                    // Invalid index - don't do anything.
                    Logging.Error("invalid individual network prop index of ", replacement.PropIndex, " for network ", replacement.NetInfo.name, " and lane ", replacement.LaneIndex, " with props length ", props.Length);
                    return;
                }
            }

            // Check index bounds.
            if (replacement.PropIndex < 0 || replacement.PropIndex >= thisLane.m_laneProps.m_props.Length)
            {
                Logging.Message("ignoring invalid individual network replacement prop index ", replacement.PropIndex, " for network ", replacement.NetInfo.name, " on lane ", replacement.LaneIndex);
                return;
            }

            // Don't apply replacement if this is an added prop.
            if (AddedNetworkProps.Instance.IsAdded(replacement.NetInfo, replacement.LaneIndex, replacement.PropIndex))
            {
                return;
            }

            // Check prop for null.
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