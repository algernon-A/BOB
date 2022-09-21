// <copyright file="ElectricalWires.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System.Collections.Generic;
    using AlgernonCommons;
    using UnityEngine;

    /// <summary>
    /// Class to handle changes to electrical wire visual appearance.
    /// </summary>
    internal class ElectricalWires
    {
        // Instance reference.
        private static ElectricalWires s_instance;

        // Wire thickness dictionaries.
        private readonly Dictionary<NetInfo.Segment, WireThickness> _segmentWires = new Dictionary<NetInfo.Segment, WireThickness>();
        private readonly Dictionary<NetInfo.Node, WireThickness> _nodeWires = new Dictionary<NetInfo.Node, WireThickness>();

        /// <summary>
        /// Gets the active instance (creating one if needed).
        /// </summary>
        internal static ElectricalWires Instance
        {
            get
            {
                // Create new instance if one doesn't exist.
                if (s_instance == null)
                {
                    s_instance = new ElectricalWires();
                }

                // Return instance reference.
                return s_instance;
            }
        }

        /// <summary>
        /// Applies a thinner size to electrial wires (networks using Custom/Net/Electricity shader).
        /// </summary>
        internal void ApplyThinnerWires()
        {
            // Use Railway Replacer figure for thinner wires.
            Vector2 thinnerWireScale = new Vector2(3.5f, 1f);

            // Target shader name.
            string shaderName = "Custom/Net/Electricity";

            Logging.Message("thinning electrical wires");

            // Iterate thorugh each loaded net prefab.
            for (uint i = 0; i < PrefabCollection<NetInfo>.LoadedCount(); ++i)
            {
                NetInfo netPrefab = PrefabCollection<NetInfo>.GetLoaded(i);
                if (netPrefab?.m_segments != null)
                {
                    // Iterate through each segment in prefab.
                    foreach (NetInfo.Segment segment in netPrefab.m_segments)
                    {
                        // Check for electricity shader.
                        Shader shader = segment?.m_material?.shader;
                        if (shader != null && shader.name == shaderName)
                        {
                            // Using electricity shader - record original values (if we haven't already).
                            if (!_segmentWires.ContainsKey(segment))
                            {
                                _segmentWires.Add(segment, new WireThickness(segment.m_material.mainTextureScale, segment.m_segmentMaterial.mainTextureScale));
                            }

                            // Rescale materials.
                            segment.m_material.mainTextureScale = thinnerWireScale;
                            segment.m_segmentMaterial.mainTextureScale = thinnerWireScale;
                        }
                    }

                    // Iterate through each node in prefab.
                    foreach (NetInfo.Node node in netPrefab.m_nodes)
                    {
                        // Check for electricity shader.
                        Shader shader = node?.m_material?.shader;
                        if (shader != null && shader.name == shaderName)
                        {
                            // Using electricity shader - record original values (if we haven't already).
                            if (!_nodeWires.ContainsKey(node))
                            {
                                _nodeWires.Add(node, new WireThickness(node.m_material.mainTextureScale, node.m_nodeMaterial.mainTextureScale));
                            }

                            // Rescale materials.
                            node.m_material.mainTextureScale = thinnerWireScale;
                            node.m_nodeMaterial.mainTextureScale = thinnerWireScale;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Reverts thinner wire settings, restoring prefabs to their original state.
        /// </summary>
        internal void RevertThinnerWires()
        {
            Logging.Message("reverting electrical wires");

            // Iterate through segment dictionary, restoring original scaling values.
            foreach (KeyValuePair<NetInfo.Segment, WireThickness> segmentEntry in _segmentWires)
            {
                segmentEntry.Key.m_material.mainTextureScale = segmentEntry.Value.Main;
                segmentEntry.Key.m_segmentMaterial.mainTextureScale = segmentEntry.Value.Component;
            }

            // Iterate through node dictionary, restoring original scaling values.
            foreach (KeyValuePair<NetInfo.Node, WireThickness> nodeEntry in _nodeWires)
            {
                nodeEntry.Key.m_material.mainTextureScale = nodeEntry.Value.Main;
                nodeEntry.Key.m_nodeMaterial.mainTextureScale = nodeEntry.Value.Component;
            }

            // Clear dictionaries.
            _segmentWires.Clear();
            _nodeWires.Clear();
        }

        /// <summary>
        /// Struct to hold wire thickness details.
        /// </summary>
        public struct WireThickness
        {
            /// <summary>
            /// Main material scaling.
            /// </summary>
            public Vector2 Main;

            /// <summary>
            /// Component material scaling.
            /// </summary>
            public Vector2 Component;

            /// <summary>
            /// Initializes a new instance of the <see cref="WireThickness"/> struct.
            /// Constructor.
            /// </summary>
            /// <param name="mainScale">Main material scaling.</param>
            /// <param name="componentScale">Component material scaling.</param>
            public WireThickness(Vector2 mainScale, Vector2 componentScale)
            {
                Main = mainScale;
                Component = componentScale;
            }
        }
    }
}