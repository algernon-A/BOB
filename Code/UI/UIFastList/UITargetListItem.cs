using System.Collections.Generic;


namespace BOB
{
	/// <summary>
	/// Data record for UI prop list line items.
	/// </summary>
	public class TargetListItem
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

		/// <summary>
		/// Currently effective prefab (active replacement prefab, if any, or original prefab if no replacement).
		/// </summary>
		public PrefabInfo CurrentPrefab => individualPrefab ?? replacementPrefab ?? allPrefab ?? packagePrefab ?? originalPrefab;


		/// <summary>
		/// Prop/tree display name.
		/// </summary>
		public string DisplayName => PrefabLists.GetDisplayName(CurrentPrefab);

		
		/// <summary>
		/// Returns true if there's a currently active replacement, false if no active replacement.
		/// </summary>
		public bool ActiveReplacement => individualPrefab != null || replacementPrefab != null || allPrefab != null;

	}


	/// <summary>
	/// Data record for UI prop list item for network props.
	/// </summary>
	public class NetTargetListItem : TargetListItem
	{
		// Lane reference.
		public int lane;

		// Lane list.
		public List<int> lanes = new List<int>();

		// Repeat distance.
		public float originalRepeat, individualRepeat;
	}
}