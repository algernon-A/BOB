using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;


namespace BOB
{
	/// <summary>
	/// The BOB selection tool.
	/// </summary>
	public class SelectTool : DefaultTool
	{
		// Cursor textures.
		private CursorInfo lightCursor;
		private CursorInfo darkCursor;


		/// <summary>
		/// Instance reference.
		/// </summary>
		public static SelectTool Instance => ToolsModifierControl.toolController?.gameObject?.GetComponent<SelectTool>();

		
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

					// Restore the default tool and create the info panel with the hovered building.
					ToolsModifierControl.SetTool<DefaultTool>();
					InfoPanelManager.Create(building);
				}
			}
			else
			{
				// No building hovered; set the cursor to the dark cursor.
				m_cursor = darkCursor;
			}
		}
	}
}
