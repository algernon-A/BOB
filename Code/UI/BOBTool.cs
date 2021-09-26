using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;


namespace BOB
{
	/// <summary>
	/// The BOB selection tool.
	/// </summary>
	public class BOBTool : DefaultTool
	{
		// Cursor textures.
		private CursorInfo lightCursor;
		private CursorInfo darkCursor;


		public enum Mode
		{
			Select,
			NodeOrSegment,
			Building,
			PropOrTree
		}
		/// <summary>
		/// Instance reference.
		/// </summary>
		public static BOBTool Instance => ToolsModifierControl.toolController?.gameObject?.GetComponent<BOBTool>();


		/// <summary>
		/// Initialise the tool.
		/// Called by unity when the tool is created.
		/// </summary>
		protected override void Awake()
		{
			base.Awake();
			lightCursor = TextureUtils.LoadCursor("bob_cursor_light.png");
			darkCursor = TextureUtils.LoadCursor("bob_cursor_dark.png");
			m_cursor = darkCursor;
		}

		// Ignore nodes, citizens, disasters, districts, transport lines, and vehicles.
		public override NetNode.Flags GetNodeIgnoreFlags() => NetNode.Flags.All;
		public override CitizenInstance.Flags GetCitizenIgnoreFlags() => CitizenInstance.Flags.All;
		public override DisasterData.Flags GetDisasterIgnoreFlags() => DisasterData.Flags.All;
		public override District.Flags GetDistrictIgnoreFlags() => District.Flags.All;
		public override TransportLine.Flags GetTransportIgnoreFlags() => TransportLine.Flags.None;
		public override VehicleParked.Flags GetParkedVehicleIgnoreFlags() => VehicleParked.Flags.All;
		public override TreeInstance.Flags GetTreeIgnoreFlags() => TreeInstance.Flags.None;
		public override PropInstance.Flags GetPropIgnoreFlags() => PropInstance.Flags.None;
		public override Vehicle.Flags GetVehicleIgnoreFlags() => Vehicle.Flags.LeftHandDrive | Vehicle.Flags.Created | Vehicle.Flags.Deleted | Vehicle.Flags.Spawned | Vehicle.Flags.Inverted | Vehicle.Flags.TransferToTarget | Vehicle.Flags.TransferToSource | Vehicle.Flags.Emergency1 | Vehicle.Flags.Emergency2 | Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped | Vehicle.Flags.Leaving | Vehicle.Flags.Arriving | Vehicle.Flags.Reversed | Vehicle.Flags.TakingOff | Vehicle.Flags.Flying | Vehicle.Flags.Landing | Vehicle.Flags.WaitingSpace | Vehicle.Flags.WaitingCargo | Vehicle.Flags.GoingBack | Vehicle.Flags.WaitingTarget | Vehicle.Flags.Importing | Vehicle.Flags.Exporting | Vehicle.Flags.Parking | Vehicle.Flags.CustomName | Vehicle.Flags.OnGravel | Vehicle.Flags.WaitingLoading | Vehicle.Flags.Congestion | Vehicle.Flags.DummyTraffic | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding;


		// Select all buildings.
		public override Building.Flags GetBuildingIgnoreFlags() => Building.Flags.None;

		/// <summary>
		/// Called by the game.  Sets which network segments are ignored by the tool (always returns none, i.e. all segments are selectable by the tool).
		/// </summary>
		/// <param name="nameOnly">Always set to false</param>
		/// <returns>NetSegment.Flags.None</returns>
		public override NetSegment.Flags GetSegmentIgnoreFlags(out bool nameOnly)
		{
			nameOnly = false;
			return NetSegment.Flags.None;
		}


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
				m_districtNameOnly = Singleton<InfoManager>.instance.CurrentMode != InfoManager.InfoMode.Districts,
				m_ignoreTerrain = GetTerrainIgnore(),
				m_ignoreNodeFlags = GetNodeIgnoreFlags(),
				m_ignoreSegmentFlags = GetSegmentIgnoreFlags(out input.m_segmentNameOnly),
				m_ignoreBuildingFlags = GetBuildingIgnoreFlags(),
				m_ignoreTreeFlags = GetTreeIgnoreFlags(),
				m_ignorePropFlags = GetPropIgnoreFlags(),
				m_ignoreVehicleFlags = GetVehicleIgnoreFlags(),
				m_ignoreParkedVehicleFlags = GetParkedVehicleIgnoreFlags(),
				m_ignoreCitizenFlags = GetCitizenIgnoreFlags(),
				m_ignoreTransportFlags = GetTransportIgnoreFlags(),
				m_ignoreDistrictFlags = GetDistrictIgnoreFlags(),
				m_ignoreParkFlags = GetParkIgnoreFlags(),
				m_ignoreDisasterFlags = GetDisasterIgnoreFlags(),
				m_transportTypes = GetTransportTypes()
			};

			// Enable ferry line selection.
			input.m_netService.m_itemLayers |= ItemClass.Layer.FerryPaths;

			ToolErrors errors = ToolErrors.None;
			RaycastOutput output;

			// Cursor is dark by default.
			m_cursor = darkCursor;

			// Is the base mouse ray valid?
			if (m_mouseRayValid)
			{
				// Yes - raycast.
				if (RayCast(input, out output))
				{
					// Set base tool accurate position.
					m_accuratePosition = output.m_hitPos;

					// Select parent building of any 'untouchable' (sub-)building.
					if (output.m_building != 0 && (Singleton<BuildingManager>.instance.m_buildings.m_buffer[output.m_building].m_flags & Building.Flags.Untouchable) != 0)
					{
						output.m_building = Building.FindParentBuilding(output.m_building);
					}

					// Check for valid hits by type - network, building, prop, tree, in that order (so e.g. embedded networks can be selected).
					if (output.m_netSegment != 0)
					{
						// Networks.
						if (CheckSegment(output.m_netSegment, ref errors))
						{
							// CheckSegment passed - record hit position and set cursor to light.
							output.m_hitPos = Singleton<NetManager>.instance.m_segments.m_buffer[output.m_netSegment].GetClosestPosition(output.m_hitPos);
							m_cursor = lightCursor;

						}
						else
						{
							// CheckSegment failed - deselect segment.
							output.m_netSegment = 0;
						}
					}
					else if (output.m_building != 0)
					{
						// Buildings.
						if (CheckBuilding(output.m_building, ref errors))
						{
							// CheckBuilding passed - record hit position and set cursor to light.
							output.m_hitPos = Singleton<BuildingManager>.instance.m_buildings.m_buffer[output.m_building].m_position;
							m_cursor = lightCursor;
						}
						else
						{
							// CheckBuilding failed - deselect building.
							output.m_building = 0;
						}
					}
					else if (output.m_propInstance != 0)
					{
						// Map props.
						if (CheckProp(output.m_propInstance, ref errors))
						{
							// CheckProp passed - record hit position and set cursor to light.
							output.m_hitPos = Singleton<PropManager>.instance.m_props.m_buffer[output.m_propInstance].Position;
							m_cursor = lightCursor;
						}
						else
						{
							// CheckProp failed - deselect prop.
							output.m_propInstance = 0;
						}
					}
					else if (output.m_treeInstance != 0)
					{
						// Map trees.
						if (CheckTree(output.m_treeInstance, ref errors))
						{
							// CheckTree passed - record hit position and set cursor to light.
							output.m_hitPos = Singleton<TreeManager>.instance.m_trees.m_buffer[output.m_treeInstance].Position;
							m_cursor = lightCursor;
						}
						else
						{
							// CheckTree failed - deselect tree.
							output.m_treeInstance = 0u;
						}
					}


					// Create new hover instance and set hovered type (if applicable).
					InstanceID hoverInstance = InstanceID.Empty;
					if (output.m_netSegment != 0)
					{
						hoverInstance.NetSegment = output.m_netSegment;
					}
					else if (output.m_building != 0)
					{
						hoverInstance.Building = output.m_building;
					}
					else if (output.m_propInstance != 0)
					{
						hoverInstance.Prop = output.m_propInstance;
					}
					else if (output.m_treeInstance != 0)
					{
						hoverInstance.Tree = output.m_treeInstance;
					}

					// Has the hovered instance changed since last time?
					if (hoverInstance != m_hoverInstance)
					{
						// Hover instance has changed.
						// Unhide any previously-hidden props, trees or buildings.
						if (m_hoverInstance.Prop != 0)
						{
							// Unhide previously hovered prop.
							if (Singleton<PropManager>.instance.m_props.m_buffer[m_hoverInstance.Prop].Hidden)
							{
								Singleton<PropManager>.instance.m_props.m_buffer[m_hoverInstance.Prop].Hidden = false;
							}
						}
						else if (m_hoverInstance.Tree != 0)
						{
							// Unhide previously hovered tree.
							if (Singleton<TreeManager>.instance.m_trees.m_buffer[m_hoverInstance.Tree].Hidden)
							{
								Singleton<TreeManager>.instance.m_trees.m_buffer[m_hoverInstance.Tree].Hidden = false;
								Singleton<TreeManager>.instance.UpdateTreeRenderer(m_hoverInstance.Tree, updateGroup: true);
							}
						}
						else if (m_hoverInstance.Building != 0)
						{
							// Unhide previously hovered building.
							if ((Singleton<BuildingManager>.instance.m_buildings.m_buffer[m_hoverInstance.Building].m_flags & Building.Flags.Hidden) != 0)
							{
								Singleton<BuildingManager>.instance.m_buildings.m_buffer[m_hoverInstance.Building].m_flags &= ~Building.Flags.Hidden;
								Singleton<BuildingManager>.instance.UpdateBuildingRenderer(m_hoverInstance.Building, updateGroup: true);
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

			// Set mouse position and record errors.
			m_mousePosition = output.m_hitPos;
			m_selectErrors = errors;
		}


		/// <summary>
		/// Unity late update handling.
		/// Called by game every late update.
		/// </summary>
		protected override void OnToolLateUpdate()
		{
			base.OnToolLateUpdate();

			// Force the info mode to none.
			ToolBase.ForceInfoMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.None);
		}


		/// <summary>
		/// Tool GUI event processing.
		/// Called by game every GUI update.
		/// </summary>
		/// <param name="e">Event</param>
		protected override void OnToolGUI(Event e)
		{
			// Check for escape key.
			if (e.type == EventType.keyDown && e.keyCode == KeyCode.Escape)
			{
				// Escape key pressed - disable tool.
				e.Use();
				ToolsModifierControl.SetTool<DefaultTool>();

				// Close window, if open.
				InfoPanelManager.Close();
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
					//ToolsModifierControl.SetTool<DefaultTool>();
					InfoPanelManager.SetTarget(Singleton<BuildingManager>.instance.m_buildings.m_buffer[building].Info);
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
						//ToolsModifierControl.SetTool<DefaultTool>();
						InfoPanelManager.SetTarget(Singleton<NetManager>.instance.m_segments.m_buffer[segment].Info);
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
							//ToolsModifierControl.SetTool<DefaultTool>();
							InfoPanelManager.SetTarget(Singleton<TreeManager>.instance.m_trees.m_buffer[tree].Info);
						}
					}
					else
					{
						ushort prop = m_hoverInstance.Prop;
						// Try to get a hovered prop instance.
						if (prop != 0)
						{
							// Check for mousedown events with button zero.
							if (e.type == EventType.MouseDown && e.button == 0)
							{
								// Got one; use the event.
								UIInput.MouseUsed();

								// Create the info panel with the hovered network prefab.
								// ToolsModifierControl.SetTool<DefaultTool>();
								InfoPanelManager.SetTarget(Singleton<PropManager>.instance.m_props.m_buffer[prop].Info);
							}
						}
					}
				}
			}
		}
	}
}
