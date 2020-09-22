using System;
using ColossalFramework;
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
		private static BOBInfoPanel _panel;
		internal static BOBInfoPanel Panel => _panel;


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
					// Give it a unique name for easy finding with ModTools.
					uiGameObject = new GameObject("BOBBuildingPanel");
					uiGameObject.transform.parent = UIView.GetAView().transform;

					_panel = uiGameObject.AddComponent<BOBBuildingInfoPanel>();
					
                    // Set up panel with selected building.
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
			GameObject.Destroy(_panel);
			GameObject.Destroy(uiGameObject);

			// Let the garbage collector do its work (and also let us know that we've closed the object).
			_panel = null;
			uiGameObject = null;
		}
	}
}
