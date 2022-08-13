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

		// Whether or not this is an added prop.
		public bool isAdded = false;


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

	public class BuildingTargetListItem : TargetListItem
	{

		// TODO: added here.
		private BOBBuildingReplacement _individualReplacement;
		private BOBBuildingReplacement _groupedReplacement;
		private BOBBuildingReplacement _allReplacement;
		private BOBBuildingReplacement _packReplacement;

		/// <summary>
		/// Gets or sets the active individual replacement.
		/// </summary>
		public BOBBuildingReplacement IndividualReplacement
		{
			get => _individualReplacement;

			set
			{
				_individualReplacement = value;
				individualPrefab = value?.replacementInfo;
			}
		}

		/// <summary>
		/// Gets or sets the active grouped replacement.
		/// </summary>
		public BOBBuildingReplacement GroupedReplacement
		{
			get => _groupedReplacement;

			set
			{
				_groupedReplacement = value;
				replacementPrefab = value?.replacementInfo;
			}
		}

		/// <summary>
		/// Gets or sets the active all-  replacement.
		/// </summary>
		public BOBBuildingReplacement AllReplacement
		{
			get => _allReplacement;

			set
			{
				_allReplacement = value;
				allPrefab = value?.replacementInfo;
			}
		}

		/// <summary>
		/// Gets or sets the active pack replacement.
		/// </summary>
		public BOBBuildingReplacement PackReplacement
		{
			get => _packReplacement;

			set
			{
				_packReplacement = value;
				allPrefab = value?.replacementInfo;
			}
		}
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



		// TODO: added here.
		private BOBNetReplacement _individualReplacement;
		private BOBNetReplacement _groupedReplacement;
		private BOBNetReplacement _allReplacement;
		private BOBNetReplacement _packReplacement;

		/// <summary>
		/// Gets or sets the active individual replacement.
		/// </summary>
		public BOBNetReplacement IndividualReplacement
		{
			get => _individualReplacement;

			set
			{
				_individualReplacement = value;
				individualPrefab = value?.replacementInfo;
			}
		}

		/// <summary>
		/// Gets or sets the active grouped replacement.
		/// </summary>
		public BOBNetReplacement GroupedReplacement
		{
			get => _groupedReplacement;

			set
			{
				_groupedReplacement = value;
				replacementPrefab = value?.replacementInfo;
			}
		}

		/// <summary>
		/// Gets or sets the active all-  replacement.
		/// </summary>
		public BOBNetReplacement AllReplacement
		{
			get => _allReplacement;

			set
			{
				_allReplacement = value;
				allPrefab = value?.replacementInfo;
			}
		}

		/// <summary>
		/// Gets or sets the active pack replacement.
		/// </summary>
		public BOBNetReplacement PackReplacement
		{
			get => _packReplacement;

			set
			{
				_packReplacement = value;
				packagePrefab = value?.replacementInfo;
			}
		}
	}
}