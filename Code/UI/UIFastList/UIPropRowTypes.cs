namespace BOB
{
	/// <summary>
	/// Prop row fastlist item for loaded props/trees.
	/// </summary>
	public class UILoadedPropRow : UIPropRow
	{
		/// <summary>
		/// Called when this item is selected.
		/// </summary>
		public override void UpdateSelection()
		{
			// Update currently selected replacement prefab.
			InfoPanelManager.Panel.ReplacementPrefab = thisPrefab;
		}
	}


	/// <summary>
	/// Prop row fastlist item for building props/trees.
	/// </summary>
	public class UIPrefabPropRow : UIPropRow
	{
		/// <summary>
		/// Called when this item is selected.
		/// </summary>
		public override void UpdateSelection()
		{
			// Update currently selected target prefab.
			InfoPanelManager.Panel.CurrentTargetItem = thisItem;

			// Update probability textfield.
			if (InfoPanelManager.Panel is BOBBuildingInfoPanel buildingPanel)
			{
				buildingPanel.probabilityField.text = probability.ToString();
			}

			// Update angle textfield.
			if (InfoPanelManager.Panel is BOBNetInfoPanel netPanel)
			{
				netPanel.angleField.text = angle.ToString();
			}
		}
	}
}
