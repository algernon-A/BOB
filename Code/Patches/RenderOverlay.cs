using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;


namespace BOB
{
    /// <summary>
    /// Overlay data struct.
    /// </summary>
    public struct OverlayData
    {
        public Vector3 position;
        public float radius;
        public float height;
    }


    /// <summary>
    /// Harmony patches and associated methods for rendering selection overlays.
    /// </summary>
    public static class RenderOverlays
    {
        private const float MaxBuildingPropDistance = 500f;

        // List of positions to highlight.
        private readonly static List<OverlayData> overlays = new List<OverlayData>();


        // Props and trees to highlight.
        internal static int CurrentIndex { get; set; }
        internal static NetInfo.Lane CurrentLane { get; set; }
        internal static PropInfo CurrentProp { get; set; }
        internal static TreeInfo CurrentTree { get; set; }
        internal static BuildingInfo CurrentBuilding { get; set; }
        internal static NetInfo CurrentNet { get; set; }


        /// <summary>
        /// Render highlight for each selected prop or tree.
        /// Should be called as a Harmony Postfix to BuildingManager.EndOverlay.
        /// </summary>
        /// <param name="cameraInfo">Current camera</param>
        public static void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            // 'Contracting circle' effect constants.
            const float Lifetime = 1f;
            const float EffectDistance = 4f;
            Color circleColor = new Color(1f, 0f, 1f, 1f);

            // Instance reference.
            RenderManager renderManager = Singleton<RenderManager>.instance;

            // 'Contracting circle' outside actual highlighted circle.  Alpha increases as it moves inwards.
            float effectRadius = Mathf.Lerp(EffectDistance, 0f, (Singleton<SimulationManager>.instance.m_realTimer % Lifetime) / Lifetime);
            Color effectColor = new Color(circleColor.r, circleColor.g, circleColor.b, (EffectDistance - effectRadius) * (1f / EffectDistance));

            // Draw circle and effect at each position in list.
            foreach (OverlayData data in overlays)
            {
                float yPos = data.position.y;
                float minY = yPos - 1f;
                float maxY = yPos + data.height + 1f;

                renderManager.OverlayEffect.DrawCircle(cameraInfo, circleColor, data.position, data.radius, minY, maxY, false, true); ;
                renderManager.OverlayEffect.DrawCircle(cameraInfo, effectColor, data.position, data.radius + effectRadius, minY, maxY + 15f, false, true);
            }

            // All done - clear the list.
            overlays.Clear();
        }


        /// <summary>
        /// Add prop to the list to be highlighted.
        /// </summary>
        /// <param name="prop">Prop info</param>
        /// <param name="position">Prop position</param>
        public static void HighlightProp(PropInfo prop, Vector3 position)
        {
            if (InfoPanelManager.Panel != null && CurrentProp != null && CurrentProp == prop)
            {
                // Calculate radius of effect - largest of x and z size of props (minimum of 1 in any case).
                Vector3 size = prop.m_mesh.bounds.size;
                overlays.Add(new OverlayData { position = position, radius = Mathf.Max(1f, size.x, size.z), height = size.y });
            }
        }


        /// <summary>
        /// Add tree to the list to be highlighted.
        /// </summary>
        /// <param name="tree">Tree info</param>
        /// <param name="position">Tree position</param>
        public static void HighlightTree(TreeInfo tree, Vector3 position)
        {
            if (InfoPanelManager.Panel != null && CurrentTree != null && CurrentTree == tree)
            {
                // Calculate radius of effect - largest of x and z size of props (minimum of 1 in any case).
                Vector3 size = tree.m_mesh.bounds.size;
                overlays.Add(new OverlayData { position = position, radius = Mathf.Max(1f, size.x, size.z), height = size.y });
            }
        }


        /// <summary>
        /// Add building prop to the list to be highlighted.
        /// </summary>
        /// <param name="camera">Current camera</param>
        /// <param name="index">Prop index</param>
        /// <param name="prop">Prop info</param>
        /// <param name="building">Building data</param>
        public static void HighlightBuildingProp(RenderManager.CameraInfo camera, int index, BuildingInfo.Prop prop, ref Building building)
        {
            // Check for match - prop, index (if applicable) and building (if applicable).
            if (prop.m_finalProp == CurrentProp && (CurrentBuilding == null || CurrentBuilding == building.Info) && (CurrentIndex < 0 || CurrentIndex == index))
            {
                // Get transform matrix for building and use to convert prop location to worldspace.
                Matrix4x4 m = Matrix4x4.TRS(building.m_position, Quaternion.Euler(0, -Mathf.Rad2Deg * building.m_angle, 0), Vector3.one);
                
                // Offset building position to account for extensible yards.
                Vector3 propPosition = prop.m_position;
                propPosition.z += (building.m_length - building.Info.m_cellLength) * 4f;

                Vector3 propLocation = m.MultiplyPoint(propPosition);

                // Don't render overlay is prop is beyond rendering distance.
                if (camera.CheckRenderDistance(propLocation, MaxBuildingPropDistance))
                {
                    // Within rendering distance size (for effect radius).
                    Vector3 size = prop.m_finalProp.m_mesh.bounds.size;

                    // Add to list of overlays to be rendered.
                    overlays.Add(new OverlayData { position = propLocation, radius = Mathf.Max(1f, size.x, size.z), height = size.y });
                }
            }
        }


        /// <summary>
        /// Add building tree to the list to be highlighted.
        /// </summary>
        /// <param name="index">Tree index</param>
        /// <param name="tree">Tree info</param>
        /// <param name="building">Building data</param>
        /// <param name="position">Tree position</param>
        public static void HighlightBuildingTree(int index, TreeInfo tree, ref Building building, Vector3 position)
        {
            if (CurrentIndex < 0 || (CurrentIndex == index) && (CurrentBuilding == null || CurrentBuilding == building.Info))
            {
                HighlightTree(tree, position);
            }
        }


        /// <summary>
        /// Add prop to the list to be highlighted.
        /// </summary>
        /// <param name="prop">Prop info</param>
        /// <param name="position">Prop position</param>
        public static void HighlightNetworkProp(PropInfo prop, Vector3 position, ushort segmentID, NetInfo.Lane lane, int index)
        {
            if (InfoPanelManager.Panel != null && CurrentProp != null && CurrentProp == prop && (CurrentNet == null || CurrentNet == Singleton<NetManager>.instance.m_segments.m_buffer[segmentID].Info) && (CurrentIndex < 0 || (CurrentLane == lane && CurrentIndex == index)))
            {
                // Calculate radius of effect - largest of x and z size of props (minimum of 1 in any case).
                Vector3 size = prop.m_mesh.bounds.size;
                overlays.Add(new OverlayData { position = position, radius = Mathf.Max(1f, size.x, size.z), height = size.y });
            }
        }


        /// <summary>
        /// Add tree to the list to be highlighted.
        /// </summary>
        /// <param name="tree">Tree info</param>
        /// <param name="position">Tree position</param>
        public static void HighlightNetworkTree(TreeInfo tree, Vector3 position, ushort segmentID, NetInfo.Lane lane, int index)
        {
            if (InfoPanelManager.Panel != null && CurrentTree != null && CurrentTree == tree && (CurrentIndex < 0 || (CurrentLane == lane && CurrentIndex == index && CurrentNet != null && CurrentNet == Singleton<NetManager>.instance.m_segments.m_buffer[segmentID].Info)))
            {
                // Calculate radius of effect - largest of x and z size of props (minimum of 1 in any case).
                Vector3 size = tree.m_mesh.bounds.size;
                overlays.Add(new OverlayData { position = position, radius = Mathf.Max(1f, size.x, size.z), height = size.y });
            }
        }
    }
}