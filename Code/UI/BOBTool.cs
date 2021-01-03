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
			lightCursor = FileUtils.LoadCursor("bob_cursor_light.png");
			darkCursor = FileUtils.LoadCursor("bob_cursor_dark.png");
			m_cursor = darkCursor;
		}

		// Ignore nodes, citizens, disasters, districts, transport lines, and vehicles.
		public override NetNode.Flags GetNodeIgnoreFlags() => NetNode.Flags.All;
		public override CitizenInstance.Flags GetCitizenIgnoreFlags() => CitizenInstance.Flags.All;
		public override DisasterData.Flags GetDisasterIgnoreFlags() => DisasterData.Flags.All;
		public override District.Flags GetDistrictIgnoreFlags() => District.Flags.All;
		public override TransportLine.Flags GetTransportIgnoreFlags() => TransportLine.Flags.All;
		public override VehicleParked.Flags GetParkedVehicleIgnoreFlags() => VehicleParked.Flags.All;
		public override Vehicle.Flags GetVehicleIgnoreFlags() => Vehicle.Flags.LeftHandDrive | Vehicle.Flags.Created | Vehicle.Flags.Deleted | Vehicle.Flags.Spawned | Vehicle.Flags.Inverted | Vehicle.Flags.TransferToTarget | Vehicle.Flags.TransferToSource | Vehicle.Flags.Emergency1 | Vehicle.Flags.Emergency2 | Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped | Vehicle.Flags.Leaving | Vehicle.Flags.Arriving | Vehicle.Flags.Reversed | Vehicle.Flags.TakingOff | Vehicle.Flags.Flying | Vehicle.Flags.Landing | Vehicle.Flags.WaitingSpace | Vehicle.Flags.WaitingCargo | Vehicle.Flags.GoingBack | Vehicle.Flags.WaitingTarget | Vehicle.Flags.Importing | Vehicle.Flags.Exporting | Vehicle.Flags.Parking | Vehicle.Flags.CustomName | Vehicle.Flags.OnGravel | Vehicle.Flags.WaitingLoading | Vehicle.Flags.Congestion | Vehicle.Flags.DummyTraffic | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding;


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
				// We have a hovered building; set the cursor to the light cursor.
				m_cursor = lightCursor;

				// Check for mousedown events with button zero.
				if (e.type == EventType.MouseDown && e.button == 0)
				{
					// Got one; use the event.
					UIInput.MouseUsed();

					// Restore the default tool and create the info panel with the hovered building prefab.
					ToolsModifierControl.SetTool<DefaultTool>();
					InfoPanelManager.Create(Singleton<BuildingManager>.instance.m_buildings.m_buffer[building].Info);
				}
			}
			else
			{
				ushort segment = m_hoverInstance.NetSegment;
				// Try to get a hovered network instance.
				if (segment != 0)
				{
					// We have a hovered network; set the cursor to the light cursor.
					m_cursor = lightCursor;

					// Check for mousedown events with button zero.
					if (e.type == EventType.MouseDown && e.button == 0)
					{
						// Got one; use the event.
						UIInput.MouseUsed();

						// Restore the default tool and create the info panel with the hovered network prefab.
						ToolsModifierControl.SetTool<DefaultTool>();
						InfoPanelManager.Create(Singleton<NetManager>.instance.m_segments.m_buffer[segment].Info);
					}
				}
				else
				{
					// No building or network hovered; set the cursor to the dark cursor.
					m_cursor = darkCursor;
				}
			}
		}
	}
}
