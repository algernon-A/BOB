using ColossalFramework;
using ColossalFramework.UI;
using ColossalFramework.Math; // *EML*
using UnityEngine;
using EManagersLib.API; // *EML*


namespace BOB
{
	/// <summary>
	/// The BOB selection tool.
	/// </summary>
	public class EBOBTool : BOBTool
	{
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
			EToolBase.RaycastOutput output; // *EML* RaycastOutput output;

			// Cursor is dark by default.
			m_cursor = darkCursor;

			// Is the base mouse ray valid?
			if (m_mouseRayValid)
			{
				// Yes - raycast.
				if (EToolBase.RayCast(input, out output)) // *EML* if (RayCast(input, out output))
				{
					// Set base tool accurate position.
					m_accuratePosition = output.m_hitPos;

					// Get ushort IDs for any manager not (yet) extended.
					// Temporarily commented out and assign 0 default for temporary reflection (see below).
					ushort buildingID = 0;// (ushort)output.m_building;
					ushort netSegmentID = 0;//(ushort)output.m_netSegment;

					/* ---------------
					 * TEMPORARY REFLECTION UNTIL OTHER WORKSHOP MODS UPDATED TO NEW EML API
					 * (otherwise lots of 'FieldNotFound' exceptions due to API changes)
					 */
					var buildingField = output.GetType().GetField("m_building");
					var segmentField = output.GetType().GetField("m_netSegment");
					object building = buildingField.GetValue(output);
					object segment = segmentField.GetValue(output);

					if (building is uint uBuilding)
					{
						buildingID = (ushort)uBuilding;
					}

					if (building is ushort uShortBuilding)
                    {
						buildingID = uShortBuilding;
                    }

					if (segment is uint uSegment)
					{
						netSegmentID = (ushort)uSegment;
					}

					if (segment is ushort uShortSegment)
					{
						netSegmentID = uShortSegment;
					}

					/*----------------
					 * END TEMPORARY REFLECTION
					 */


					// Select parent building of any 'untouchable' (sub-)building.
					if (buildingID != 0 && (Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].m_flags & Building.Flags.Untouchable) != 0)
					{
						buildingID = Building.FindParentBuilding(buildingID);
					}

					// Check for valid hits by type - network, building, prop, tree, in that order (so e.g. embedded networks can be selected).
					if (netSegmentID != 0)
					{
						// Networks.
						if (CheckSegment(netSegmentID, ref errors))
						{
							// CheckSegment passed - record hit position and set cursor to light.
							output.m_hitPos = Singleton<NetManager>.instance.m_segments.m_buffer[netSegmentID].GetClosestPosition(output.m_hitPos);
							m_cursor = lightCursor;

						}
						else
						{
							// CheckSegment failed - deselect segment.
							netSegmentID = 0;
						}
					}
					else if (buildingID != 0)
					{
						// Buildings.
						if (CheckBuilding(buildingID, ref errors))
						{
							// CheckBuilding passed - record hit position and set cursor to light.
							output.m_hitPos = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].m_position;
							m_cursor = lightCursor;
						}
						else
						{
							// CheckBuilding failed - deselect building.
							buildingID = 0;
						}
					}
					else if (output.m_propInstance != 0)
					{
						// Map props.
						if (CheckProp(output.m_propInstance))
						{
							// CheckProp passed - record hit position and set cursor to light.
							output.m_hitPos = EPropManager.m_props.m_buffer[output.m_propInstance].Position; // *EML* output.m_hitPos = Singleton<PropManager>.instance.m_props.m_buffer[output.m_propInstance].Position;
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
					if (netSegmentID != 0)
					{
						hoverInstance.NetSegment = netSegmentID;
					}
					else if (buildingID != 0)
					{
						hoverInstance.Building = buildingID;
					}
					else if (output.m_propInstance != 0)
					{
						hoverInstance.SetProp32(output.m_propInstance); // *EML* hoverInstance.Prop = output.m_propInstance;
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

						/* *EML* cut
						if (m_hoverInstance.Prop != 0)
						{
							// Unhide previously hovered prop.
							if (Singleton<PropManager>.instance.m_props.m_buffer[m_hoverInstance.Prop].Hidden)
							{
								Singleton<PropManager>.instance.m_props.m_buffer[m_hoverInstance.Prop].Hidden = false;
							}
						}*/

						// *EML* start, replacing above cut
						uint propID = m_hoverInstance.GetProp32();
						if (propID != 0)
						{
							// Unhide previously hovered prop.
							if (EPropManager.m_props.m_buffer[propID].Hidden) // *EML* if (Singleton<PropManager>.instance.m_props.m_buffer[propID].Hidden)
							{
								EPropManager.m_props.m_buffer[propID].Hidden = false;// *EML* Singleton<PropManager>.instance.m_props.m_buffer[propID].Hidden = false;
							}
						}
						// *EML* end

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
						uint prop = m_hoverInstance.GetProp32(); // *EML* ushort prop = m_hoverInstance.Prop;
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
								InfoPanelManager.SetTarget(EPropManager.m_props.m_buffer[prop].Info); // *EML* InfoPanelManager.SetTarget(Singleton<PropManager>.instance.m_props.m_buffer[prop].Info);
							}
						}
					}
				}
			}
		}


		/// <summary>
		/// For *EML* - prop raytrace check.
		/// </summary>
		/// <param name="prop">Prop ID</param>
		/// <returns>True if raytrace confirmed, false otherwise</returns>
		private  bool CheckProp(uint prop)
		{
			if ((m_toolController.m_mode & ItemClass.Availability.Editors) != ItemClass.Availability.None)
			{
				return true;
			}
			Vector2 a = VectorUtils.XZ(EPropManager.m_props.m_buffer[prop].Position);
			Quad2 quad = default;
			quad.a = a + new Vector2(-0.5f, -0.5f);
			quad.b = a + new Vector2(-0.5f, 0.5f);
			quad.c = a + new Vector2(0.5f, 0.5f);
			quad.d = a + new Vector2(0.5f, -0.5f);
			return !Singleton<GameAreaManager>.instance.QuadOutOfArea(quad);
		}
	}
}
