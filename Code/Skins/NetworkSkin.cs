// <copyright file="NetworkSkin.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB.Skins
{
    using System;

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
        public readonly NetInfo NetInfo;

        /// <summary>
        /// Custom lanes.
        /// </summary>
        public NetInfo.Lane[] Lanes;

        /// <summary>
        /// Custom segments.
        /// </summary>
        public NetInfo.Segment[] Segments;

        ///
        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkSkin"/> class.
        /// </summary>
        /// <param name="prefab"><see cref="NetInfo"/> prefab to create skin from.</param>
        public NetworkSkin(NetInfo prefab)
        {
            NetInfo = prefab;

            // Copy segments.
            if (prefab.m_segments != null)
            {
                int segmentSize = prefab.m_segments.Length;
                Segments = new NetInfo.Segment[segmentSize];
                Array.Copy(prefab.m_segments, Segments, segmentSize);
            }

            // Copy lanes.
            if (prefab.m_lanes != null)
            {
                int laneSize = prefab.m_lanes.Length;
                Lanes = new NetInfo.Lane[laneSize];

                for (int i = 0; i < laneSize; ++i)
                {
                    Lanes[i] = new BOBCustomLane(prefab.m_lanes[i]);
                }
            }
        }

        /// <summary>
        /// Clones the given <see cref="NetLaneProps.Prop"/>.
        /// </summary>
        /// <param name="laneProp">see cref="NetLaneProps.Prop"/> to clone.</param>
        /// <returns>New clone.</returns>
        public static NetLaneProps.Prop CloneLaneProp(NetLaneProps.Prop laneProp)
        {
            if (laneProp is ICloneable cloneableProp)
            {
                // Adaptive networks lane props are cloneable - easy.
                return cloneableProp.Clone() as NetLaneProps.Prop;
            }

            // Otherwise, clone it manually.
            NetLaneProps.Prop clone = new NetLaneProps.Prop();
            NetData.CopyFields(laneProp, clone);
            return clone;
        }

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
                    for (int i = 0; i < original.m_laneProps.m_props.Length; ++i)
                    {
                        m_laneProps.m_props[i] = CloneLaneProp(original.m_laneProps.m_props[i]);
                    }
                }
            }
        }
    }
}
