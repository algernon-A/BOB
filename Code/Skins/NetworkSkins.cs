// <copyright file="NetworkSkins.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB.Skins
{
    using System;
    using System.Collections.Generic;
    using AlgernonCommons;
    using ColossalFramework;
    using static BOBConfig;

    /// <summary>
    /// Network skinning class.  Based on boformer's Network Skins mod.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Performant fields")]
    internal class NetworkSkins : IDisposable
    {
        /// <summary>
        /// Array of active skins (null if none), directly mapped to segment ID.
        /// </summary>
        public static NetworkSkin[] SegmentSkins;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkSkins"/> class.
        /// </summary>
        internal NetworkSkins()
        {
            // Set instance.
            Instance = this;

            // Initialize array of skins to match game's segment array.
            SegmentSkins = new NetworkSkin[Singleton<NetManager>.instance.m_segments.m_buffer.Length];
        }

        /// <summary>
        /// Gets the active instance.
        /// </summary>
        internal static NetworkSkins Instance { get; private set; }

        /// <summary>
        /// Disposes of this instance and frees arrays.
        /// </summary>
        public void Dispose()
        {
            // Clear arrays.
            SegmentSkins = null;

            // Clear instance reference.
            Instance = null;
        }

        internal void AddIndividualReplacement(NetInfo prefab, ushort segmentID, int laneIndex, int propIndex, NetReplacement replacement)
        {
            Logging.Message("adding individual replacement to segment ", segmentID);

            if (segmentID == 0)
            {
                Logging.Error("segment ID of 0 passed to NetworkSkins.AddIndividualReplacement");
                return;
            }

            if (SegmentSkins[segmentID] == null)
            {
                SegmentSkins[segmentID] = new NetworkSkin(prefab);
            }

            NetworkSkin thisSkin = SegmentSkins[segmentID];

            KeyValuePair<int, int> handlerKey = new KeyValuePair<int, int>(laneIndex, propIndex);
            thisSkin.AddChange(laneIndex, propIndex);
            if (!thisSkin.Handlers.TryGetValue(handlerKey, out LanePropHandler handler))
            {
                NetInfo.Lane thisLane = thisSkin.Lanes[laneIndex];
                handler = new LanePropHandler(prefab, segmentID, thisLane, laneIndex, propIndex, thisLane.m_laneProps.m_props[laneIndex]);
                thisSkin.Handlers.Add(handlerKey, handler);
            }

            handler.SetReplacement(replacement, ReplacementPriority.InstanceIndividualReplacement);
        }

        internal static void RemoveIndividualReplacement(ushort segmentID, int laneIndex, int propIndex)
        {
            Logging.Message("removing individual replacement ", segmentID, ":", laneIndex, ":", propIndex);

            if (segmentID != 0 && laneIndex >= 0 && propIndex >= 0 && SegmentSkins[segmentID] is NetworkSkin thisSkin)
            {

                KeyValuePair<int, int> handlerKey = new KeyValuePair<int, int>(laneIndex, propIndex);

                if (thisSkin.Handlers.TryGetValue(handlerKey, out LanePropHandler handler))
                {
                    NetReplacement replacement = handler.GetReplacement(ReplacementPriority.InstanceIndividualReplacement);
                    if (replacement != null)
                    {

                        Logging.Message("clearing replacement");
                        handler.ClearReplacement(replacement);
                    }
                }
            }
        }

        internal static void RemoveGroupedReplacement(ushort segmentID, NetReplacement replacement)
        {
            if (segmentID != 0 && SegmentSkins[segmentID] is NetworkSkin thisSkin)
            {
                foreach (LanePropHandler handler in thisSkin.Handlers.Values)
                {
                    handler.ClearReplacement(replacement);
                }
            }
        }

        internal void AddGroupedReplacement(NetInfo prefab, ushort segmentID, NetReplacement replacement)
        {
            Logging.Message("adding grouped replacement to segment ", segmentID);

            if (segmentID == 0)
            {
                Logging.Error("segment ID of 0 passed to NetworkSkins.AddGroupedReplacement");
                return;
            }


            if (SegmentSkins[segmentID] == null)
            {
                SegmentSkins[segmentID] = new NetworkSkin(prefab);
            }

            NetworkSkin thisSkin = SegmentSkins[segmentID];

            for (int laneIndex = 0; laneIndex < thisSkin.Lanes.Length; ++laneIndex)
            {
                if (thisSkin.Lanes[laneIndex]?.m_laneProps?.m_props is NetLaneProps.Prop[] laneProps)
                {
                    for (int propIndex = 0; propIndex < laneProps.Length; ++propIndex)
                    {
                        if (laneProps[propIndex] != null)
                        {

                            // Note current props.
                            TreeInfo thisTree = laneProps[propIndex].m_finalTree;
                            PropInfo thisProp = laneProps[propIndex].m_finalProp;

                            // Get any active handler.
                            LanePropHandler handler = NetHandlers.GetHandler(thisSkin.Lanes[laneIndex], replacement.SegmentID, laneIndex, propIndex);
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
                                KeyValuePair<int, int> handlerKey = new KeyValuePair<int, int>(laneIndex, propIndex);
                                thisSkin.AddChange(laneIndex, propIndex);
                                if (!thisSkin.Handlers.TryGetValue(handlerKey, out LanePropHandler individualHandler))
                                {
                                    NetInfo.Lane thisLane = thisSkin.Lanes[laneIndex];
                                    individualHandler = new LanePropHandler(prefab, segmentID, thisLane, laneIndex, propIndex, thisLane.m_laneProps.m_props[laneIndex]);
                                    thisSkin.Handlers.Add(handlerKey, individualHandler);
                                }

                                individualHandler.SetReplacement(replacement, ReplacementPriority.InstanceGroupedReplacement);
                            }
                        }
                    }
                }
            }
        }
    }
}
