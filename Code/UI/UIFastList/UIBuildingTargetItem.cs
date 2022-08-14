namespace BOB
{
    using System.Collections.Generic;

    /// <summary>
    /// Data record for UI prop list line items.
    /// </summary>
    public class BuildingTargetItem
    {
        // TODO:  Adding here.
        public BOBConfig.BuildingReplacement m_individualReplacement;
        public BOBConfig.BuildingReplacement m_groupedReplacement;
        public BOBConfig.BuildingReplacement m_allReplacement;

        // Single index.
        public int index;

        // List of indexes.
        public List<int> indexes = new List<int>();

        // Whether or not to show probabilities.
        public bool showProbs = false;

        // Whether or not this is an added prop.
        public bool isAdded = false;

        // Currently displayed prefab.
        public PrefabInfo currentPrefab;

        // Original prefab.
        public PrefabInfo originalPrefab;
        public int originalProb;
        public float originalAngle;

        /// <summary>
        /// Prop/tree display name.
        /// </summary>
        public string DisplayName => PrefabLists.GetDisplayName(CurrentPrefab);

        /// <summary>
        /// Currently effective prefab (active replacement prefab, if any, or original prefab if no replacement).
        /// </summary>
        public virtual PrefabInfo CurrentPrefab => currentPrefab;


        /// <summary>
        /// Returns true if there's a currently active replacement, false if no active replacement.
        /// </summary>
        public virtual bool ActiveReplacement => m_individualReplacement != null || m_groupedReplacement != null || m_allReplacement != null;
    }
}