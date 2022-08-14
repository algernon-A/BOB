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
        private BOBConfig.BuildingReplacement _individualReplacement;
        private BOBConfig.BuildingReplacement _groupedReplacement;
        private BOBConfig.BuildingReplacement _allReplacement;
        private BOBConfig.BuildingReplacement _packReplacement;

        /// <summary>
        /// Gets or sets the active individual replacement.
        /// </summary>
        public BOBConfig.BuildingReplacement IndividualReplacement
        {
            get => _individualReplacement;

            set
            {
                _individualReplacement = value;
                individualPrefab = value?.ReplacementInfo;
            }
        }

        /// <summary>
        /// Gets or sets the active grouped replacement.
        /// </summary>
        public BOBConfig.BuildingReplacement GroupedReplacement
        {
            get => _groupedReplacement;

            set
            {
                _groupedReplacement = value;
                replacementPrefab = value?.ReplacementInfo;
            }
        }

        /// <summary>
        /// Gets or sets the active all-  replacement.
        /// </summary>
        public BOBConfig.BuildingReplacement AllReplacement
        {
            get => _allReplacement;

            set
            {
                _allReplacement = value;
                allPrefab = value?.ReplacementInfo;
            }
        }

        /// <summary>
        /// Gets or sets the active pack replacement.
        /// </summary>
        public BOBConfig.BuildingReplacement PackReplacement
        {
            get => _packReplacement;

            set
            {
                _packReplacement = value;
                allPrefab = value?.ReplacementInfo;
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
        private BOBConfig.NetReplacement _individualReplacement;
        private BOBConfig.NetReplacement _groupedReplacement;
        private BOBConfig.NetReplacement _allReplacement;
        private BOBConfig.NetReplacement _packReplacement;

        /// <summary>
        /// Gets or sets the active individual replacement.
        /// </summary>
        public BOBConfig.NetReplacement IndividualReplacement
        {
            get => _individualReplacement;

            set
            {
                _individualReplacement = value;
                individualPrefab = value?.ReplacementInfo;
            }
        }

        /// <summary>
        /// Gets or sets the active grouped replacement.
        /// </summary>
        public BOBConfig.NetReplacement GroupedReplacement
        {
            get => _groupedReplacement;

            set
            {
                _groupedReplacement = value;
                replacementPrefab = value?.ReplacementInfo;
            }
        }

        /// <summary>
        /// Gets or sets the active all-  replacement.
        /// </summary>
        public BOBConfig.NetReplacement AllReplacement
        {
            get => _allReplacement;

            set
            {
                _allReplacement = value;
                allPrefab = value?.ReplacementInfo;
            }
        }

        /// <summary>
        /// Gets or sets the active pack replacement.
        /// </summary>
        public BOBConfig.NetReplacement PackReplacement
        {
            get => _packReplacement;

            set
            {
                _packReplacement = value;
                packagePrefab = value?.ReplacementInfo;
            }
        }
    }
}