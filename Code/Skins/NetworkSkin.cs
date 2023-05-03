// <copyright file="NetworkSkin.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB.Skins
{
    using System;
    using System.Collections.Generic;
    using AlgernonCommons;

    /// <summary>
    /// Custom network skin.
    /// Based on boformer's Network Skins mod.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Performant fields")]
    public class NetworkSkin
    {
        /// <summary>
        /// Base network prefab.
        /// </summary>
        public readonly NetInfo NetPrefab;

        /// <summary>
        /// Custom lanes.
        /// </summary>
        public NetInfo.Lane[] Lanes;

        /// <summary>
        /// Custom segments.
        /// </summary>
        public NetInfo.Segment[] Segments;

        // Array of lists of changed prop indicies, in array indexed by lane.
        private readonly HashSet<int>[] _changedLanes;

        ///
        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkSkin"/> class.
        /// </summary>
        /// <param name="prefab"><see cref="NetInfo"/> prefab to create skin from.</param>
        public NetworkSkin(NetInfo prefab)
        {
            NetPrefab = prefab;

            // Copy segments.
            if (prefab.m_segments != null)
            {
                int segmentSize = prefab.m_segments.Length;
                Segments = new NetInfo.Segment[segmentSize];
                Array.Copy(prefab.m_segments, Segments, segmentSize);
            }

            // Assign lane data (pointing to original lane data to start with).
            if (prefab.m_lanes != null)
            {
                int laneSize = prefab.m_lanes.Length;
                Lanes = new NetInfo.Lane[laneSize];

                for (int i = 0; i < laneSize; ++i)
                {
                    Lanes[i] = prefab.m_lanes[i];
                }

                // Initialise change array.
                _changedLanes = new HashSet<int>[laneSize];
            }
        }

        /// <summary>
        /// Records a change to a <see cref="NetLaneProps.Prop"/> being applied to this skin.
        /// </summary>
        /// <param name="laneIndex">Lane index of changed <see cref="NetLaneProps.Prop"/>.</param>
        /// <param name="propIndex">Prop index of changed <see cref="NetLaneProps.Prop"/>.</param>
        public void AddChange(int laneIndex, int propIndex)
        {
            Logging.KeyMessage("adding skin change with lane index ", laneIndex, " of ", Lanes.Length, " and prop index ", propIndex, " of ", Lanes[laneIndex]?.m_laneProps?.m_props?.Length);

            // Ensure a custom lane.
            EnsureCustomLane(laneIndex);

            // Check if a list has already been created for this item.
            if (_changedLanes[laneIndex] == null)
            {
                // No exisitng list - create one.
                _changedLanes[laneIndex] = new HashSet<int>();
            }
            else if (_changedLanes[laneIndex].Contains(propIndex))
            {
                // Existing list already contains this prop index - don't do anything further.
                return;
            }

            // Clone existing prop as template for change.
            Lanes[laneIndex].m_laneProps.m_props[propIndex] = NetData.CloneLaneProp(Lanes[laneIndex].m_laneProps.m_props[propIndex]);

            // Record this change.
            _changedLanes[laneIndex].Add(propIndex);
        }

        /// <summary>
        /// Removes all changes to a <see cref="NetLaneProps.Prop"/> from this skin.
        /// </summary>
        /// <param name="laneIndex">Lane index of changed <see cref="NetLaneProps.Prop"/>.</param>
        /// <param name="propIndex">Prop index of changed <see cref="NetLaneProps.Prop"/>.</param>
        public void RemoveChange(int laneIndex, int propIndex)
        {
            // Don't do anything if there's no change recorded for the given item.
            if (_changedLanes[laneIndex] == null || !_changedLanes[laneIndex].Contains(propIndex))
            {
                return;
            }

            // Restore original prop.
            Lanes[laneIndex].m_laneProps.m_props[propIndex] = NetPrefab.m_lanes[laneIndex].m_laneProps.m_props[propIndex];

            // Remove record.
            _changedLanes[laneIndex].Remove(propIndex);

            // Remove entire lane if no changes left here.
            if (_changedLanes[laneIndex].Count == 0)
            {
                // NetLaneProps are ScriptableObjects, and should be disposed of properly when no longer in use.
                UnityEngine.Object.Destroy(Lanes[laneIndex].m_laneProps);

                // Restore original lane.
                Logging.Message("restoring original lane ", laneIndex);
                Lanes[laneIndex] = NetPrefab.m_lanes[laneIndex];
            }
        }

        /// <summary>
        /// Chceks to see if a skin has a change for the given <see cref="NetLaneProps.Prop"/>.
        /// </summary>
        /// <param name="laneIndex">Lane index of <see cref="NetLaneProps.Prop"/>.</param>
        /// <param name="propIndex">Prop index of <see cref="NetLaneProps.Prop"/>.</param>
        /// <returns><c>true</c> if this skin has a change for that item, <c>false</c> otherwise.</returns>
        public bool HasChange(int laneIndex, int propIndex) => _changedLanes[laneIndex] != null && _changedLanes[laneIndex].Contains(propIndex);

        /// <summary>
        /// Destroys the object and all ScriptableObject components.
        /// </summary>
        public void Destroy()
        {
            if (Lanes != null)
            {
                foreach (NetInfo.Lane lane in Lanes)
                {
                    if (lane is BOBCustomLane && lane.m_laneProps != null)
                    {
                        // NetLaneProps are ScriptableObjects, and should be disposed of properly when no longer in use.
                        UnityEngine.Object.Destroy(lane.m_laneProps);
                        lane.m_laneProps = null;
                    }
                }
            }
        }

        /// <summary>
        /// Ensures that a custom lane exists for the given lane index.
        /// </summary>
        /// <param name="laneIndex">Lane index number.</param>
        public void EnsureCustomLane(int laneIndex)
        {
            // Check to make sure we haven't already done this.
            if (Lanes[laneIndex] is BOBCustomLane)
            {
                return;
            }

            // Create new BOB lane based on the existing lane.
            Lanes[laneIndex] = new BOBCustomLane(NetPrefab.m_lanes[laneIndex]);
        }

        /// <summary>
        /// Custom <see cref="NetInfo.Lane"/> for BOB per-segment skinning.
        /// </summary>
        private class BOBCustomLane : NetInfo.Lane
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="BOBCustomLane"/> class.
            /// </summary>
            /// <param name="original">Original <see cref="NetInfo.Lane"/>.</param>
            public BOBCustomLane(NetInfo.Lane original)
            {
                NetData.CopyFields(original, this);

                // NetLaneProps are ScriptableObjects and need proper instantiation.
                if (original.m_laneProps != null)
                {
                    m_laneProps = UnityEngine.Object.Instantiate(original.m_laneProps);

                    // Copy original prop references to new lane.
                    for (int i = 0; i < original.m_laneProps.m_props.Length; ++i)
                    {
                        m_laneProps.m_props[i] = original.m_laneProps.m_props[i];
                    }
                }
            }
        }
    }
}
