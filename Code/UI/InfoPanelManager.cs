using System;
using ColossalFramework.UI;
using UnityEngine;
using BOB.MessageBox;


namespace BOB
{
	/// <summary>
	/// Static class to manage the BOB info panel.
	/// </summary>
	internal static class InfoPanelManager
	{
		// Instance references.
		private static GameObject uiGameObject;
		private static BOBInfoPanelBase panel;
		internal static BOBInfoPanelBase Panel => panel;

		// Recent state.
		internal static float lastX, lastY;

		// Exception flag.
		internal static bool wasException = false, displayingException = false;
		internal static string exceptionMessage;


		/// <summary>
		/// Sets the BOB target to the selected prefab, creating the relevant info window if necessary.
		/// </summary>
		/// <param name="selectedPrefab">Target prefab</param>
		internal static void SetTarget(PrefabInfo selectedPrefab)
        {
			// If no existing panel, create it.
			if (Panel == null)
			{
				Create(selectedPrefab);
			}
			else
			{
				// Otherwise, check for panel and prefab type match; if they match, update existing panel, otherwise close the existing panel (retaining BOB tool) and create new one with the new selection. 
				if (selectedPrefab is BuildingInfo)
                {
					// Building.
					if (Panel is BOBBuildingInfoPanel)
                    {
						Panel.SetTarget(selectedPrefab);
                    }
					else
                    {
						Close(false);
						Create(selectedPrefab);
                    }
                }
				else if (selectedPrefab is NetInfo)
				{
					// Network.
					if (Panel is BOBNetInfoPanel)
					{
						Panel.SetTarget(selectedPrefab);
					}
					else
					{
						Close(false);
						Create(selectedPrefab);
					}
				}
				else if (selectedPrefab is TreeInfo || selectedPrefab is PropInfo)
				{
					// Standalone tree/prop.
					if (Panel is BOBMapInfoPanel)
					{
						Panel.SetTarget(selectedPrefab);
					}
					else
					{
						Close(false);
						Create(selectedPrefab);
					}
				}
			}
        }


		/// <summary>
		/// Closes the panel by destroying the object (removing any ongoing UI overhead).
		/// </summary>
		/// <param name="resetTool">True to reset to default tool; false to leave current tool untouched (default true)</param>
		internal static void Close(bool resetTool = true)
		{
			// Check for null, just in case - this is also called by pressing Esc when BOB tool is active.
			if (panel != null)
			{
				// Perform any panel actions on close.
				panel.Close();

				// Stop highlighting.
				panel.CurrentTargetItem = null;
				RenderOverlays.CurrentBuilding = null;

				// Revert overlay patches.
				Patcher.PatchBuildingOverlays(false);
				Patcher.PatchNetworkOverlays(false);
				Patcher.PatchMapOverlays(false);

				// Clear tool lane overlay list.
				BOBTool.Instance.renderLanes.Clear();

				// Store previous position.
				lastX = Panel.relativePosition.x;
				lastY = Panel.relativePosition.y;

				// Destroy game objects.
				GameObject.Destroy(Panel);
				GameObject.Destroy(uiGameObject);

				// Let the garbage collector do its work (and also let us know that we've closed the object).
				panel = null;
				uiGameObject = null;

				// Restore default tool if needed.
				if (resetTool)
				{
					ToolsModifierControl.SetTool<DefaultTool>();
				}
			}
		}


		/// <summary>
		/// Checks to see if an exception has occured and, and if so displays it (if we aren't already).
		/// </summary>
		internal static void CheckException()
        {
			// Display exception message if an exception occured and we're not already displaying one.
			if (wasException && !displayingException)
			{
				// Set displaying flag and show message.
				displayingException = true;
				MessageBoxBase.ShowModal<ExceptionMessageBox>();
			}
		}


		/// <summary>
		/// Refreshes random prop/tree lists on close of random panel.
		/// </summary>
		internal static void RefreshRandom()
        {
			if (Panel is BOBInfoPanel infoPanel)
            {
				infoPanel.RefreshRandom();
            }
        }


		/// <summary>
		/// Creates the panel object in-game and displays it.
		/// </summary>
		private static void Create(PrefabInfo selectedPrefab)
		{
			try
			{
				// If no instance already set, create one.
				if (uiGameObject == null)
				{
					if (selectedPrefab is BuildingInfo)
					{
						// A building prefab is selected; create a BuildingInfo panel.
						// Give it a unique name for easy finding with ModTools.
						uiGameObject = new GameObject("BOBBuildingPanel");
						uiGameObject.transform.parent = UIView.GetAView().transform;

						panel = uiGameObject.AddComponent<BOBBuildingInfoPanel>();
					}
					else if (selectedPrefab is NetInfo)
					{
						// A network prefab is selected; create a NetInfo panel.
						// Give it a unique name for easy finding with ModTools.
						uiGameObject = new GameObject("BOBNetPanel");
						uiGameObject.transform.parent = UIView.GetAView().transform;

						panel = uiGameObject.AddComponent<BOBNetInfoPanel>();
					}
					else if (selectedPrefab is TreeInfo || selectedPrefab is PropInfo)
					{
						// A tree prefab is selected; create a TreeInfo panel.
						// Give it a unique name for easy finding with ModTools.
						uiGameObject = new GameObject("BOBMapPanel");
						uiGameObject.transform.parent = UIView.GetAView().transform;
						panel = uiGameObject.AddComponent<BOBMapInfoPanel>();
					}
					else
					{
						Logging.Message("unsupported prefab type ", selectedPrefab);
						return;
					}

					// Set up panel with selected prefab.
					Panel.transform.parent = uiGameObject.transform.parent;
					Panel.SetTarget(selectedPrefab);
				}
			}
			catch (Exception e)
			{
				Logging.LogException(e, "exception creating InfoPanel");

				// Destroy the GameObjects rather than have a half-functional (at best) panel that confuses players.
				GameObject.Destroy(Panel);
				GameObject.Destroy(uiGameObject);
			}
		}
	}
}
