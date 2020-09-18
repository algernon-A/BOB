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
	public class UIBuildingPropRow : UIPropRow
	{
		/// <summary>
		/// Called when this item is selected.
		/// </summary>
		public override void UpdateSelection()
		{
			// Update currently selected target prefab.
			InfoPanelManager.Panel.CurrentBuildingItem = thisItem;

			// Update probability textfield.
			InfoPanelManager.Panel.probabilityField.text = probability.ToString();
		}
	}
}
