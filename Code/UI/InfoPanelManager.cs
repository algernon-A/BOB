using System;
using ColossalFramework.UI;
using UnityEngine;


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


		/// <summary>
		/// Creates the panel object in-game and displays it.
		/// </summary>
		internal static void Create(PrefabInfo selectedPrefab)
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
						Logging.Message("unsupported prefab type ", selectedPrefab.ToString());
						return;
					}

					// Set up panel with selected prefab.
					Panel.transform.parent = uiGameObject.transform.parent;
					Panel.Setup(selectedPrefab);
				}
			}
			catch (Exception e)
			{
				Logging.LogException(e, "exception creating InfoPanel");
			}
		}


		/// <summary>
		/// Closes the panel by destroying the object (removing any ongoing UI overhead).
		/// </summary>
		internal static void Close()
		{
			// Stop highlighting.
			panel.CurrentTargetItem = null;
			RenderOverlays.CurrentBuilding = null;

			// Revert overlay patches.
			Patcher.PatchBuildingOverlays(false);
			Patcher.PatchNetworkOverlays(false);
			Patcher.PatchMapOverlays(false);

			// Store previous position.
			lastX = Panel.relativePosition.x;
			lastY = Panel.relativePosition.y;

			// Destroy game objects.
			GameObject.Destroy(Panel);
			GameObject.Destroy(uiGameObject);

			// Let the garbage collector do its work (and also let us know that we've closed the object).
			panel = null;
			uiGameObject = null;
		}
	}
}
