// <copyright file="NetData.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using AlgernonCommons;
    using BOB.Skins;
    using ColossalFramework;
    using UnityEngine;

    /// <summary>
    /// Class to handle centralised network data.
    /// </summary>
    internal static class NetData
    {
        // List of dirty network prefabs.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1311:Static readonly fields should begin with upper-case letter", Justification = "Private static readonly field")]
        private static readonly HashSet<NetInfo> s_dirtyPrefabs = new HashSet<NetInfo>();

        /// <summary>
        /// Gets the dirty prefabs list.
        /// </summary>
        internal static HashSet<NetInfo> DirtyPrefabs => s_dirtyPrefabs;

        /// <summary>
        /// Updates the render for the given prefab or segment.
        /// </summary>
        /// <param name="netInfo">Prefab to update (ignored if <c>segmentID</c> is not 0).</param>
        /// <param name="segmentID">Segment to update (set to 0 if no skin).</param>
        internal static void UpdateRender(NetInfo netInfo, ushort segmentID)
        {
            // Prefab or skin?
            if (segmentID == 0)
            {
                // Prefab - add network prefab to dirty list.
                s_dirtyPrefabs.Add(netInfo);
            }
            else
            {
                // Skin - Update renderer directly.
                Singleton<SimulationManager>.instance.AddAction(() => Singleton<NetManager>.instance.UpdateSegmentRenderer(segmentID, true));
            }
        }

        /// <summary>
        /// Refreshes network prefab renders for all 'dirty' networks and calls a recalculation of any Network Skins 2 skins.
        /// </summary>
        internal static void Update()
        {
            // Hashset of render group coordinates to update.
            HashSet<KeyValuePair<int, int>> groupHash = new HashSet<KeyValuePair<int, int>>();

            // Local references.
            NetManager netManager = Singleton<NetManager>.instance;
            RenderManager renderManager = Singleton<RenderManager>.instance;
            NetSegment[] segments = netManager.m_segments.m_buffer;
            NetNode[] nodes = netManager.m_nodes.m_buffer;

            // Need to do this for each segment instance, so iterate through all segments.
            for (ushort i = 0; i < segments.Length; ++i)
            {
                // Check that this is a valid network in the dirty list.
                if (segments[i].m_flags != NetSegment.Flags.None && s_dirtyPrefabs.Contains(segments[i].Info))
                {
                    // Update segment instance.
                    renderManager.UpdateInstance((uint)(49152 + i));

                    // Caclulate segment render group.
                    ushort startNode = segments[i].m_startNode;
                    ushort endNode = segments[i].m_endNode;
                    Vector3 position = nodes[startNode].m_position;
                    Vector3 position2 = nodes[endNode].m_position;
                    Vector3 vector = (position + position2) * 0.5f;
                    int num = Mathf.Clamp((int)((vector.x / 64f) + 135f), 0, 269);
                    int num2 = Mathf.Clamp((int)((vector.z / 64f) + 135f), 0, 269);
                    int x = num * 45 / 270;
                    int z = num2 * 45 / 270;

                    // Add render group coordinates to hashlist (ignore if already there).
                    groupHash.Add(new KeyValuePair<int, int>(x, z));
                }
            }

            // Update render groups.
            // Iterate through each key in group.
            foreach (KeyValuePair<int, int> keyPair in groupHash)
            {
                // Update group render (all 31 layers, since we've got all kinds of mismatches with replacements).
                for (int i = 0; i < 31; ++i)
                {
                    renderManager.UpdateGroup(keyPair.Key, keyPair.Value, i);
                }
            }

            // Recalculate any Network Skins 2 applied skins.
            ModUtils.NS2Recalculate();

            // Clear dirty prefabs list.
            s_dirtyPrefabs.Clear();
        }

        /// <summary>
        /// Gets the <see cref="NetInfo.Lane"/> referred to by the given prefab or segment and lane index.
        /// </summary>
        /// <param name="netInfo">Prefab (ignored if <c>segmentID</c> is not 0).</param>
        /// <param name="segmentID">Segment ID (set to 0 for no skin).</param>
        /// <param name="laneIndex">Lane index.</param>
        /// <returns>Relevant <see cref="NetInfo.Lane"/>.</returns>
        internal static NetInfo.Lane GetLane(NetInfo netInfo, ushort segmentID, int laneIndex)
        {
            NetInfo.Lane[] lanes = GetLanes(netInfo, segmentID);

            // Bounds check.
            if (laneIndex >= 0 && laneIndex < lanes.Length)
            {
                return lanes[laneIndex];
            }

            // If we got here, no matching lane was found.
            Logging.Error("invalid lane index reference of ", laneIndex, " for network ", netInfo?.name);
            return null;
        }

        /// <summary>
        /// Gets the <see cref="NetInfo.Lane"/> array referred to by the given prefab or segment.
        /// </summary>
        /// <param name="netInfo">Prefab (ignored if <c>segmentID</c> is not 0).</param>
        /// <param name="segmentID">Segment ID (set to 0 for no skin).</param>
        /// <returns>Relevant <see cref="NetInfo.Lane"/> array.</returns>
        internal static NetInfo.Lane[] GetLanes(NetInfo netInfo, ushort segmentID)
        {
            // Skin or prefab?
            if (segmentID == 0)
            {
                // Prefab.
                return netInfo.m_lanes;
            }
            else if (NetworkSkins.SegmentSkins[segmentID] != null)
            {
                // Skin.
                return NetworkSkins.SegmentSkins[segmentID].Lanes;
            }

            // If we got here, no matching lane was found.
            return null;
        }

        /// <summary>
        /// Creates a new NetInfo.Lane instance for the specified network and lane index.
        /// Used to 'separate' target networks for individual and network prop replacement when the network uses shared m_laneProps (e.g. vanilla roads).
        /// </summary>
        /// <param name="network">Network prefab.</param>
        /// <param name="lane">Lane index.</param>
        internal static void CloneLanePropInstance(NetInfo network, int lane) => CloneLanePropInstance(network, network.m_lanes[lane]);

        /// <summary>
        /// Creates a new NetInfo.Lane instance for the specified network and lane index.
        /// Used to 'separate' target networks for individual and network prop replacement when the network uses shared m_laneProps (e.g. vanilla roads).
        /// </summary>
        /// <param name="network">Network prefab.</param>
        /// <param name="laneInfo">Lane info.</param>
        internal static void CloneLanePropInstance(NetInfo network, NetInfo.Lane laneInfo)
        {
            // Don't do anything if we've previously converted this one, or if it's custom content.
            if (network.m_isCustomContent || laneInfo.m_laneProps is NewNetLaneProps)
            {
                return;
            }

            Logging.Message("creating new m_laneProps instance for network ", network.name);

            // Create new m_laneProps instance with new props list, using our custom class instead of NetLaneProps as a flag that we've already done this one.
            NewNetLaneProps newLaneProps = ScriptableObject.CreateInstance<NewNetLaneProps>();
            newLaneProps.m_props = new NetLaneProps.Prop[laneInfo.m_laneProps.m_props.Length];

            // Iterate through each LaneProp in the existing instance and clone it.
            for (int i = 0; i < newLaneProps.m_props.Length; ++i)
            {
                newLaneProps.m_props[i] = CloneLaneProp(laneInfo.m_laneProps.m_props[i]);
            }

            // Replace network laneProps with our new instance.
            laneInfo.m_laneProps = newLaneProps;
        }

        /// <summary>
        /// Clones the given <see cref="NetLaneProps.Prop"/>.
        /// </summary>
        /// <param name="laneProp">see cref="NetLaneProps.Prop"/> to clone.</param>
        /// <returns>New clone.</returns>
        internal static NetLaneProps.Prop CloneLaneProp(NetLaneProps.Prop laneProp)
        {
            if (laneProp is ICloneable cloneableProp)
            {
                // Adaptive networks lane props are cloneable - easy.
                return cloneableProp.Clone() as NetLaneProps.Prop;
            }

            // Otherwise, clone it manually.
            NetLaneProps.Prop clone = new NetLaneProps.Prop();
            CopyFields(laneProp, clone);
            return clone;
        }

        /// <summary>
        /// Copies all public instance fields from the source object to the target object.
        /// </summary>
        /// <param name="source">Source object.</param>
        /// <param name="target">Target object.</param>
        internal static void CopyFields(object source, object target)
        {
            FieldInfo[] sourceFields = source.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (FieldInfo field in sourceFields)
            {
                field.SetValue(target, field.GetValue(source));
            }
        }

        /// <summary>
        /// 'Dummy' class for new NetLaneProps.Prop instances to overcome network NetLaneProps sharing.
        /// </summary>
        public class NewNetLaneProps : NetLaneProps
        {
        }
    }
}
