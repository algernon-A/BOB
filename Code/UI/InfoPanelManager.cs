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
		private static BOBInfoPanelBase _panel;
		internal static BOBInfoPanelBase Panel => _panel;

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

						_panel = uiGameObject.AddComponent<BOBBuildingInfoPanel>();
					}
					else if (selectedPrefab is NetInfo)
					{
						// A network prefab is selected; create a NetInfo panel.
						// Give it a unique name for easy finding with ModTools.
						uiGameObject = new GameObject("BOBNetPanel");
						uiGameObject.transform.parent = UIView.GetAView().transform;

						_panel = uiGameObject.AddComponent<BOBNetInfoPanel>();
					}
					else if (selectedPrefab is TreeInfo)
					{
						// A tree prefab is selected; create a TreeInfo panel.
						// Give it a unique name for easy finding with ModTools.
						uiGameObject = new GameObject("BOBTreePanel");
						uiGameObject.transform.parent = UIView.GetAView().transform;

						_panel = uiGameObject.AddComponent<BOBTreeInfoPanel>();
					}
					else
                    {
						Debugging.Message("unsupported prefab type " + selectedPrefab.ToString());
						return;
					}

					// Set up panel with selected prefab.
					Panel.Setup(uiGameObject.transform.parent, selectedPrefab);
				}
			}
			catch (Exception exception)
			{
				Debugging.LogException(exception);
			}
		}


		/// <summary>
		/// Closes the panel by destroying the object (removing any ongoing UI overhead).
		/// </summary>
		internal static void Close()
		{
			// Store previous position.
			lastX = _panel.relativePosition.x;
			lastY = _panel.relativePosition.y;

			// Destroy game objects.
			GameObject.Destroy(_panel);
			GameObject.Destroy(uiGameObject);

			// Let the garbage collector do its work (and also let us know that we've closed the object).
			_panel = null;
			uiGameObject = null;
		}
	}
}
