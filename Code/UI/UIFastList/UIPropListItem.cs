using System.Collections.Generic;


namespace BOB
{
	/// <summary>
	/// Data record for UI prop list line items.
	/// </summary>
	public class PropListItem
	{
		// Original prefab.
		public PrefabInfo originalPrefab;
		public int originalProb;
		public float originalAngle;

		// Current individual replacement (if any).
		public PrefabInfo individualPrefab;
		public int individualProb;

		// Current replacement (if any).
		public PrefabInfo replacementPrefab;
		public int replacementProb;

		// Current all- replacement (if any).
		public PrefabInfo allPrefab;
		public int allProb;

		// Current package replacement (if any).
		public PrefabInfo packagePrefab;

		// Single index.
		public int index;

		// List of indexes.
		public List<int> indexes = new List<int>();

		// Whether or not to show probabilities.
		public bool showProbs = false;

		// Current prefab.
		public PrefabInfo CurrentPrefab => individualPrefab ?? replacementPrefab ?? allPrefab ?? packagePrefab ?? originalPrefab;

		// Display name.
		public string DisplayName => PrefabLists.GetDisplayName(CurrentPrefab);
	}


	/// <summary>
	/// Data record for UI prop list item for network props.
	/// </summary>
	public class NetPropListItem : PropListItem
    {
		// Lane reference.
		public int lane;

		// Lane list.
		public List<int> lanes = new List<int>();
	}
}