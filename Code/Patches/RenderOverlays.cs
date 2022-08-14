// <copyright file="RenderOverlays.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System.Collections.Generic;
    using ColossalFramework;
    using UnityEngine;

    /// <summary>
    /// Harmony patches and associated methods for rendering selection overlays.
    /// </summary>
    public static class RenderOverlays
    {
        // Maximum building effect rendering distance.
        private const float MaxBuildingPropDistance = 500f;

        // List of positions to highlight.
        private static readonly List<OverlayData> Overlays = new List<OverlayData>();

        // Effect intensity.
        private static float s_intensity = 1f;

        // Rendering targets.
        private static int s_propIndex;
        private static PropInfo s_prop;
        private static TreeInfo s_tree;
        private static BuildingInfo s_building;
        private static NetInfo.Lane s_lane;
        private static NetInfo s_network;

        /// <summary>
        /// Sets the current prop index to highlight.
        /// </summary>
        internal static int PropIndex { set => s_propIndex = value; }

        /// <summary>
        /// Sets the current prop prefab to highlight.
        /// </summary>
        internal static PropInfo Prop { set => s_prop = value; }

        /// <summary>
        /// Sets the current tree prefab to highlight.
        /// </summary>
        internal static TreeInfo Tree { set => s_tree = value; }

        /// <summary>
        /// Sets the current parent building prefab to highlight.
        /// </summary>
        internal static BuildingInfo Building { set => s_building = value; }

        /// <summary>
        /// Sets the current lane to highlight.
        /// </summary>
        internal static NetInfo.Lane Lane { set => s_lane = value; }

        /// <summary>
        /// Sets the current parent net prefab to highlight.
        /// </summary>
        internal static NetInfo Network { set => s_network = value; }

        /// <summary>
        /// Gets or sets the rendering effect intensity.
        /// </summary>
        internal static float Intensity { get => s_intensity;  set => s_intensity = value; }

        /// <summary>
        /// Render highlight for each selected prop or tree.
        /// Should be called as a Harmony Postfix to BuildingManager.EndOverlay.
        /// </summary>
        /// <param name="cameraInfo">Current camera instance.</param>
        public static void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            // 'Contracting circle' effect constants.
            const float Lifetime = 1f;
            const float EffectDistance = 4f;
            Color circleColor = new Color(1f, 0f, 1f, 1f) * s_intensity;

            // Instance reference.
            RenderManager renderManager = Singleton<RenderManager>.instance;

            // 'Contracting circle' outside actual highlighted circle.  Alpha increases as it moves inwards.
            float effectRadius = Mathf.Lerp(EffectDistance, 0f, (Singleton<SimulationManager>.instance.m_realTimer % Lifetime) / Lifetime);
            Color effectColor = new Color(circleColor.r, circleColor.g, circleColor.b, (EffectDistance - effectRadius) * (1f / EffectDistance) * s_intensity);

            // Draw circle and effect at each position in list.
            foreach (OverlayData data in Overlays)
            {
                float yPos = data.Position.y;
                float minY = yPos - 1f;
                float maxY = yPos + data.Height + 1f;

                renderManager.OverlayEffect.DrawCircle(cameraInfo, circleColor, data.Position, data.Radius, minY, maxY, false, true); ;
                renderManager.OverlayEffect.DrawCircle(cameraInfo, effectColor, data.Position, data.Radius + effectRadius, minY, maxY, false, true);
            }

            // All done - clear the list.
            Overlays.Clear();
        }

        /// <summary>
        /// Add prop to the list to be highlighted.
        /// </summary>
        /// <param name="prop">Prop info.</param>
        /// <param name="position">Prop position.</param>
        public static void HighlightProp(PropInfo prop, Vector3 position)
        {
            if (BOBPanelManager.Panel != null & s_prop != null & s_prop == prop)
            {
                // Calculate radius of effect - largest of x and z size of props (minimum of 1 in any case).
                Vector3 size = prop.m_mesh.bounds.size;
                Overlays.Add(new OverlayData { Position = position, Radius = Mathf.Max(1f, size.x, size.z), Height = size.y });
            }
        }

        /// <summary>
        /// Add tree to the list to be highlighted.
        /// </summary>
        /// <param name="tree">Tree info.</param>
        /// <param name="position">Tree position.</param>
        public static void HighlightTree(TreeInfo tree, Vector3 position)
        {
            if (BOBPanelManager.Panel != null & s_tree != null & s_tree == tree)
            {
                // Calculate radius of effect - largest of x and z size of props (minimum of 1 in any case).
                Vector3 size = tree.m_mesh.bounds.size;
                Overlays.Add(new OverlayData { Position = position, Radius = Mathf.Max(1f, size.x, size.z), Height = size.y });
            }
        }

        /// <summary>
        /// Add building prop to the list to be highlighted.
        /// </summary>
        /// <param name="camera">Current camera.</param>
        /// <param name="index">Prop index.</param>
        /// <param name="prop">Prop info.</param>
        /// <param name="building">Building data.</param>
        public static void HighlightBuildingProp(RenderManager.CameraInfo camera, int index, BuildingInfo.Prop prop, ref Building building)
        {
            // Check for match - prop, index (if applicable) and building (if applicable).
            if (prop.m_finalProp == s_prop & (s_building == null | s_building == building.Info) & (s_propIndex < 0 | s_propIndex == index))
            {
                // Get transform matrix for building and use to convert prop location to worldspace.
                Matrix4x4 m = Matrix4x4.TRS(building.m_position, Quaternion.Euler(0, -Mathf.Rad2Deg * building.m_angle, 0), Vector3.one);

                // Offset building position to account for extensible yards.
                Vector3 propPosition = prop.m_position;

                // Allow for yard expansions.
                if (building.Info.m_expandFrontYard)
                {
                    propPosition.z -= (building.m_length - building.Info.m_cellLength) * 4f;
                }
                else
                {
                    propPosition.z += (building.m_length - building.Info.m_cellLength) * 4f;
                }

                Vector3 propLocation = m.MultiplyPoint(propPosition);

                // Don't render overlay is prop is beyond rendering distance.
                if (camera.CheckRenderDistance(propLocation, MaxBuildingPropDistance))
                {
                    // Within rendering distance size (for effect radius).
                    Vector3 size = prop.m_finalProp.m_mesh.bounds.size;

                    // Add to list of overlays to be rendered.
                    Overlays.Add(new OverlayData { Position = propLocation, Radius = Mathf.Max(1f, size.x, size.z), Height = size.y });
                }
            }
        }

        /// <summary>
        /// Add building tree to the list to be highlighted.
        /// </summary>
        /// <param name="index">Tree index.</param>
        /// <param name="tree">Tree info.</param>
        /// <param name="building">Building data.</param>
        /// <param name="position">Tree position.</param>
        public static void HighlightBuildingTree(int index, TreeInfo tree, ref Building building, Vector3 position)
        {
            if ((s_propIndex < 0 | s_propIndex == index) & (s_building == null | s_building == building.Info))
            {
                HighlightTree(tree, position);
            }
        }

        /// <summary>
        /// Add prop to the list to be highlighted.
        /// </summary>
        /// <param name="prop">Prop info.</param>
        /// <param name="position">Prop position.</param>
        /// <param name="segmentID">Network segment ID.</param>
        /// <param name="lane">Network lane info.</param>
        /// <param name="index">Prop index.</param>
        public static void HighlightNetworkProp(PropInfo prop, Vector3 position, ushort segmentID, NetInfo.Lane lane, int index)
        {
            if (BOBPanelManager.Panel != null & s_prop != null & s_prop == prop && (s_network == null || s_network == Singleton<NetManager>.instance.m_segments.m_buffer[segmentID].Info) & (s_propIndex < 0 | (s_lane == lane & s_propIndex == index)))
            {
                // Calculate radius of effect - largest of x and z size of props (minimum of 1 in any case).
                Vector3 size = prop.m_mesh.bounds.size;
                Overlays.Add(new OverlayData
                {
                    Position = position,
                    Radius = Mathf.Max(1f, size.x, size.z),
                    Height = size.y,
                });
            }
        }

        /// <summary>
        /// Add tree to the list to be highlighted.
        /// </summary>
        /// <param name="tree">Tree info.</param>
        /// <param name="position">Tree position.</param>
        /// <param name="segmentID">Network segment ID.</param>
        /// <param name="lane">Network lane info.</param>
        /// <param name="index">Prop index.</param>
        public static void HighlightNetworkTree(TreeInfo tree, Vector3 position, ushort segmentID, NetInfo.Lane lane, int index)
        {
            if (BOBPanelManager.Panel != null & s_tree != null & s_tree == tree && (s_network != null || s_network == Singleton<NetManager>.instance.m_segments.m_buffer[segmentID].Info) & (s_propIndex < 0 | (s_lane == lane && s_propIndex == index)))
            {
                // Calculate radius of effect - largest of x and z size of props (minimum of 1 in any case).
                Vector3 size = tree.m_mesh.bounds.size;
                Overlays.Add(new OverlayData
                {
                    Position = position,
                    Radius = Mathf.Max(1f, size.x, size.z),
                    Height = size.y,
                });
            }
        }

        /// <summary>
        /// Overlay data struct.
        /// </summary>
        public struct OverlayData
        {
            /// <summary>
            /// Overlay position.
            /// </summary>
            public Vector3 Position;

            /// <summary>
            /// Overlay radius.
            /// </summary>
            public float Radius;

            /// <summary>
            /// Overlay height.
            /// </summary>
            public float Height;
        }
    }
}