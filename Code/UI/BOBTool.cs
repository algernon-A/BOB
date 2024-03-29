﻿// <copyright file="BOBTool.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System.Collections.Generic;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework;
    using ColossalFramework.Math;
    using ColossalFramework.UI;
    using EManagersLib.API;
    using UnifiedUI.Helpers;
    using UnityEngine;

    /// <summary>
    /// The BOB selection tool.
    /// </summary>
    public class BOBTool : DefaultTool
    {
        // List of lane overlays to render.
        private readonly List<Bezier3> _laneOverlays = new List<Bezier3>();

        // Cursor textures.
        private CursorInfo _lightCursor;
        private CursorInfo _darkCursor;

        /// <summary>
        /// Gets the active instance reference.
        /// </summary>
        internal static BOBTool Instance => ToolsModifierControl.toolController?.gameObject?.GetComponent<BOBTool>();

        /// <summary>
        /// Gets a value indicating whether the RON tool is currently active (true) or inactive (false).
        /// </summary>
        internal static bool IsActiveTool => Instance != null && ToolsModifierControl.toolController.CurrentTool == Instance;

        /// <summary>
        /// Gets the list of lane overlays to render.
        /// </summary>
        internal List<Bezier3> LaneOverlays => _laneOverlays;

        /// <summary>
        /// Sets which network segments are ignored by the tool (always returns none, i.e. all segments are selectable by the tool).
        /// </summary>
        /// <param name="nameOnly">Always set to false.</param>
        /// <returns>NetSegment.Flags.None.</returns>
        public override NetSegment.Flags GetSegmentIgnoreFlags(out bool nameOnly)
        {
            nameOnly = false;
            return NetSegment.Flags.None;
        }

        /// <summary>
        /// Sets vehicle ingore flags to ignore all vehicles.
        /// </summary>
        /// <returns>Vehicle flags ignoring all vehicles.</returns>
        public override Vehicle.Flags GetVehicleIgnoreFlags() =>
            Vehicle.Flags.LeftHandDrive
            | Vehicle.Flags.Created
            | Vehicle.Flags.Deleted
            | Vehicle.Flags.Spawned
            | Vehicle.Flags.Inverted
            | Vehicle.Flags.TransferToTarget
            | Vehicle.Flags.TransferToSource
            | Vehicle.Flags.Emergency1
            | Vehicle.Flags.Emergency2
            | Vehicle.Flags.WaitingPath
            | Vehicle.Flags.Stopped
            | Vehicle.Flags.Leaving
            | Vehicle.Flags.Arriving
            | Vehicle.Flags.Reversed
            | Vehicle.Flags.TakingOff
            | Vehicle.Flags.Flying
            | Vehicle.Flags.Landing
            | Vehicle.Flags.WaitingSpace
            | Vehicle.Flags.WaitingCargo
            | Vehicle.Flags.GoingBack
            | Vehicle.Flags.WaitingTarget
            | Vehicle.Flags.Importing
            | Vehicle.Flags.Exporting
            | Vehicle.Flags.Parking
            | Vehicle.Flags.CustomName
            | Vehicle.Flags.OnGravel
            | Vehicle.Flags.WaitingLoading
            | Vehicle.Flags.Congestion
            | Vehicle.Flags.DummyTraffic
            | Vehicle.Flags.Underground
            | Vehicle.Flags.Transition
            | Vehicle.Flags.InsideBuilding;

        /// <summary>
        /// Called by the game every simulation step.
        /// Performs raycasting to select hovered instance.
        /// </summary>
        public override void SimulationStep()
        {
            // Get base mouse ray.
            Ray mouseRay = m_mouseRay;

            // Get raycast input.
            RaycastInput input = new RaycastInput(mouseRay, m_mouseRayLength)
            {
                m_rayRight = m_rayRight,
                m_netService = GetService(),
                m_buildingService = GetService(),
                m_propService = GetService(),
                m_treeService = GetService(),
                m_districtNameOnly = true,
                m_ignoreTerrain = true,
                m_ignoreNodeFlags = NetNode.Flags.All,
                m_ignoreSegmentFlags = GetSegmentIgnoreFlags(out input.m_segmentNameOnly),
                m_ignoreBuildingFlags = Building.Flags.None,
                m_ignoreTreeFlags = global::TreeInstance.Flags.None,
                m_ignorePropFlags = PropInstance.Flags.None,
                m_ignoreVehicleFlags = GetVehicleIgnoreFlags(),
                m_ignoreParkedVehicleFlags = VehicleParked.Flags.All,
                m_ignoreCitizenFlags = CitizenInstance.Flags.All,
                m_ignoreTransportFlags = TransportLine.Flags.All,
                m_ignoreDistrictFlags = District.Flags.All,
                m_ignoreParkFlags = DistrictPark.Flags.All,
                m_ignoreDisasterFlags = DisasterData.Flags.All,
                m_transportTypes = 0,
            };

            // Enable ferry line selection.
            input.m_netService.m_itemLayers |= ItemClass.Layer.FerryPaths;

            ToolErrors errors = ToolErrors.None;
            EToolBase.RaycastOutput output;

            bool validHover = false;

            // Is the base mouse ray valid?
            if (m_mouseRayValid)
            {
                // Yes - raycast.
                if (PropAPI.RayCast(input, out output))
                {
                    // Create new hover instance.
                    EInstanceID hoverInstance = InstanceID.Empty;

                    // Set base tool accurate position.
                    m_accuratePosition = output.m_hitPos;

                    // Select parent building of any 'untouchable' (sub-)building.
                    if (output.m_building != 0 && (Singleton<BuildingManager>.instance.m_buildings.m_buffer[output.m_building].m_flags & Building.Flags.Untouchable) != 0)
                    {
                        output.m_building = Building.FindParentBuilding((ushort)output.m_building);
                    }

                    // Check for valid hits by type - network, building, prop, tree, in that order (so e.g. embedded networks can be selected).
                    if (output.m_netSegment != 0)
                    {
                        // Network - record hit position, set hover, and set cursor to light.
                        output.m_hitPos = Singleton<NetManager>.instance.m_segments.m_buffer[output.m_netSegment].GetClosestPosition(output.m_hitPos);
                        hoverInstance.NetSegment = (ushort)output.m_netSegment;
                        validHover = true;

                        // Set hover.
                    }
                    else if (output.m_building != 0)
                    {
                        // Building - record hit position, set hover, and set cursor to light.
                        output.m_hitPos = Singleton<BuildingManager>.instance.m_buildings.m_buffer[output.m_building].m_position;
                        hoverInstance.Building = (ushort)output.m_building;
                        validHover = true;
                    }
                    else if (output.m_propInstance != 0)
                    {
                        // Prop - record hit position, set hover, and set cursor to light.
                        output.m_hitPos = PropAPI.Wrapper.GetPosition(output.m_propInstance);
                        hoverInstance.Prop = output.m_propInstance;
                        validHover = true;
                    }
                    else if (output.m_treeInstance != 0)
                    {
                        // Map tree - record hit position, set hover, and set cursor to light.
                        output.m_hitPos = Singleton<TreeManager>.instance.m_trees.m_buffer[output.m_treeInstance].Position;
                        hoverInstance.Tree = output.m_treeInstance;
                        validHover = true;
                    }

                    // Has the hovered instance changed since last time?
                    if (hoverInstance != m_hoverInstance)
                    {
                        // Hover instance has changed.
                        // Unhide any previously-hidden props, trees or buildings.
                        if (m_hoverInstance.Prop != 0)
                        {
                            // Unhide previously hovered prop.
                            PropAPI.Wrapper.SetFlags(m_hoverInstance.Prop, (ushort)(PropAPI.Wrapper.GetFlags(m_hoverInstance.Prop) & ~(ushort)PropInstance.Flags.Hidden));
                        }
                        else if (m_hoverInstance.Tree != 0)
                        {
                            // Local references.
                            TreeManager treeManager = Singleton<TreeManager>.instance;
                            global::TreeInstance[] treeBuffer = treeManager.m_trees.m_buffer;

                            // Unhide previously hovered tree.
                            if (treeBuffer[m_hoverInstance.Tree].Hidden)
                            {
                                treeBuffer[m_hoverInstance.Tree].Hidden = false;
                                treeManager.UpdateTreeRenderer(m_hoverInstance.Tree, updateGroup: true);
                            }
                        }
                        else if (m_hoverInstance.Building != 0)
                        {
                            // Local references.
                            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
                            Building[] buildingBuffer = buildingManager.m_buildings.m_buffer;

                            // Unhide previously hovered building.
                            if ((buildingBuffer[m_hoverInstance.Building].m_flags & Building.Flags.Hidden) != 0)
                            {
                                buildingBuffer[m_hoverInstance.Building].m_flags &= ~Building.Flags.Hidden;
                                buildingManager.UpdateBuildingRenderer(m_hoverInstance.Building, updateGroup: true);
                            }
                        }

                        // Update tool hover instance.
                        m_hoverInstance = hoverInstance;
                    }
                }
                else
                {
                    // Raycast failed.
                    errors = ToolErrors.RaycastFailed;
                }
            }
            else
            {
                // No valid mouse ray.
                output = default;
                errors = ToolErrors.RaycastFailed;
            }

            // Set cursor.
            m_cursor = validHover ? _lightCursor : _darkCursor;

            // Set mouse position and record errors.
            m_mousePosition = output.m_hitPos;
            m_selectErrors = errors;
        }

        /// <summary>
        /// Called by game when overlay is to be rendered.
        /// </summary>
        /// <param name="cameraInfo">Current camera instance.</param>
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            // 5m vertical offset in each direction to allow for terrain changes.
            const float verticalOffset = 5f;

            // Local references.
            ToolManager toolManager = Singleton<ToolManager>.instance;
            OverlayEffect overlay = Singleton<RenderManager>.instance.OverlayEffect;

            base.RenderOverlay(cameraInfo);

            // If any lane overlays are ready to be rendered, render them.
            if (_laneOverlays.Count != 0)
            {
                // Set color.
                Color renderColor = Color.magenta * RenderOverlays.Intensity;

                // Iterate through list.
                foreach (Bezier3 bezier in _laneOverlays)
                {
                    // Calculate minimum and maximum y-values.
                    float minY = Mathf.Min(bezier.a.y, bezier.d.y) - verticalOffset;
                    float maxY = Mathf.Max(bezier.a.y, bezier.d.y) + verticalOffset;

                    // Draw bezier overlay in magenta.
                    overlay.DrawBezier(cameraInfo, renderColor, bezier, 1.1f, 0f, 0f, minY, maxY, false, alphaBlend: false);
                    ++toolManager.m_drawCallData.m_overlayCalls;
                }
            }
        }

        /// <summary>
        /// Toggles the current tool to/from the BOB tool.
        /// </summary>
        internal static void ToggleTool()
        {
            // Activate BOB tool if it isn't already; if already active, deactivate it by selecting the default tool instead.
            if (!IsActiveTool)
            {
                // Activate BOB tool.
                ToolsModifierControl.toolController.CurrentTool = Instance;
            }
            else
            {
                // Activate default tool.
                ToolsModifierControl.SetTool<DefaultTool>();
            }
        }

        /// <summary>
        /// Initialise the tool.
        /// Called by unity when the tool is created.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            // Initializae PropAPI.
            PropAPI.Initialize();

            // Load cursors.
            _lightCursor = UITextures.LoadCursor("BOB-CursorLight.png");
            _darkCursor = UITextures.LoadCursor("BOB-CursorDark.png");
            m_cursor = _darkCursor;

            // Create new UUI button.
            UIComponent uuiButton = UUIHelpers.RegisterToolButton(
                name: nameof(BOBTool),
                groupName: null, // default group
                tooltip: Translations.Translate("BOB_NAM"),
                tool: this,
                icon: UUIHelpers.LoadTexture(UUIHelpers.GetFullPath<Mod>("Resources", "BOB-UUI.png")),
                hotkeys: new UUIHotKeys { ActivationKey = ModSettings.ToolKey });
        }

        /// <summary>
        /// Called by game when tool is enabled.
        /// </summary>
        protected override void OnEnable()
        {
            // Call base even before loaded checks to properly initialize tool.
            base.OnEnable();

            // Make sure that game is loaded before activating tool.
            if (!Loading.IsLoaded)
            {
                // Loading not complete - deactivate tool by seting default tool.
                ToolsModifierControl.SetTool<DefaultTool>();
            }
        }

        /// <summary>
        /// Called by game when tool is disabled.
        /// Used to close the BOB info panel.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();

            // Is a BOB info panel already open?
            if (BOBPanelManager.Panel != null)
            {
                // Yes - close it.
                BOBPanelManager.Panel?.Close();
            }

            // Clear tool lane overlay list.
            _laneOverlays.Clear();
        }

        /// <summary>
        /// Unity late update handling.
        /// Called by game every late update.
        /// </summary>
        protected override void OnToolLateUpdate()
        {
            base.OnToolLateUpdate();

            // Force the info mode to none.
            ForceInfoMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.None);
        }

        /// <summary>
        /// Tool GUI event processing.
        /// Called by game every GUI update.
        /// </summary>
        /// <param name="e">Event.</param>
        protected override void OnToolGUI(Event e)
        {
            // Check for escape key.
            if (e.type == EventType.keyDown && e.keyCode == KeyCode.Escape)
            {
                // Escape key pressed - disable tool.
                e.Use();
                ToolsModifierControl.SetTool<DefaultTool>();

                // Close window, if open.
                BOBPanelManager.Panel?.Close();
            }

            // Don't do anything if mouse is inside UI or if there are any errors other than failed raycast.
            if (m_toolController.IsInsideUI || (m_selectErrors != ToolErrors.None && m_selectErrors != ToolErrors.RaycastFailed))
            {
                return;
            }

            // Try to get a hovered building instance.
            ushort building = m_hoverInstance.Building;
            if (building != 0)
            {
                // Check for mousedown events with button zero.
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    // Got one; use the event.
                    UIInput.MouseUsed();

                    // Create the info panel with the hovered building prefab.
                    BOBPanelManager.SetTargetParent(Singleton<BuildingManager>.instance.m_buildings.m_buffer[building].Info);
                }
            }
            else
            {
                ushort segment = m_hoverInstance.NetSegment;

                // Try to get a hovered network instance.
                if (segment != 0)
                {
                    // Check for mousedown events with button zero.
                    if (e.type == EventType.MouseDown && e.button == 0)
                    {
                        // Got one; use the event.
                        UIInput.MouseUsed();

                        // Create the info panel with the hovered network prefab.
                        BOBPanelManager.SetTargetParent(Singleton<NetManager>.instance.m_segments.m_buffer[segment].Info);
                    }
                }
                else
                {
                    uint tree = m_hoverInstance.Tree;

                    // Try to get a hovered tree instance.
                    if (tree != 0)
                    {
                        // Check for mousedown events with button zero.
                        if (e.type == EventType.MouseDown && e.button == 0)
                        {
                            // Got one; use the event.
                            UIInput.MouseUsed();

                            // Create the info panel with the hovered network prefab.
                            BOBPanelManager.SetTargetParent(Singleton<TreeManager>.instance.m_trees.m_buffer[tree].Info);
                        }
                    }
                    else
                    {
                        uint prop = PropAPI.GetPropID(m_hoverInstance);

                        // Try to get a hovered prop instance.
                        if (prop != 0)
                        {
                            // Check for mousedown events with button zero.
                            if (e.type == EventType.MouseDown && e.button == 0)
                            {
                                // Got one; use the event.
                                UIInput.MouseUsed();

                                // Create the info panel with the hovered network prefab.
                                BOBPanelManager.SetTargetParent(PropAPI.Wrapper.GetInfo(prop));
                            }
                        }
                    }
                }
            }

            // Check for PageDown/PageUp.
            if (e.keyCode == KeyCode.PageDown)
            {
                // Set mode to underground.
                Singleton<InfoManager>.instance.SetCurrentMode(InfoManager.InfoMode.Underground, InfoManager.SubInfoMode.Default);
            }
            else if (e.keyCode == KeyCode.PageUp)
            {
                // Set mode to surface.
                Singleton<InfoManager>.instance.SetCurrentMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.Default);
            }
        }
    }
}
