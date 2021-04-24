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
		}
	}


	/// <summary>
	/// Prop row fastlist item for loaded props/trees for random setup.
	/// </summary>
	public class UILoadedRandomPropRow : UIPropRow
	{
		/// <summary>
		/// Called when this item is selected.
		/// </summary>
		public override void UpdateSelection()
		{
			// Update currently selected loaded prefab.
			BOBRandomPanel.Panel.SelectedLoadedPrefab = thisPrefab;
		}
	}


	/// <summary>
	/// Prop row fastlist item for random prop/tree variations.
	/// </summary>
	public class UIRandomComponentRow : UIPropRow
	{
		/// <summary>
		/// Called when this item is selected.
		/// </summary>
		public override void UpdateSelection()
		{
			// Update currently selected variation prefab.
			BOBRandomPanel.Panel.SelectedVariation = thisPrefab;
		}
	}
}
