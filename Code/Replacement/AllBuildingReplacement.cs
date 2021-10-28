using System.Collections.Generic;


namespace BOB
{
	/// <summary>
	/// Class to manage all-builing prop and tree replacements.
	/// </summary>
	internal class AllBuildingReplacement : BuildingReplacementBase
	{
		/// <summary>
		/// Constructor - initializes instance reference and replacement dictionary.
		/// </summary>
		internal AllBuildingReplacement()
		{
			Instance = this;
		}

		/// <summary>
		/// Instance reference.
		/// </summary>
		internal static AllBuildingReplacement Instance { get; private set; }

		/// <summary>
		/// Returns the config file list of building elements relevant to the current replacement type.
		/// </summary>
		protected override List<BOBBuildingElement> BuildingElementList => null;


		/// <summary>
		/// Retrieves any currently-applied replacement entry that affects the given building and target prefab and prop index.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <param name="targetInfo">Target prop/tree prefab</param>
		/// <param name="propIndex">Target prop/tree index (unused)</param>
		/// <returns>Currently-applied replacement (null if none)</returns>
		internal override BOBBuildingReplacement EligibileReplacement(BuildingInfo buildingInfo, PrefabInfo targetInfo, int propIndex) => ReplacementList(buildingInfo)?.Find(x => x.target.Equals(targetInfo.name));


		/// <summary>
		/// Reverts all active replacements.
		/// </summary>
		internal override void RevertAll()
		{
			// Iterate through each entry in the replacement list.
			foreach (BOBBuildingReplacement replacement in ConfigurationUtils.CurrentConfig.allBuildingProps)
			{
				// Revert this replacement (but don't remove the entry, as the list is currently immutable while we're iterating through it).
				Revert(replacement, false);
			}
		}


		/// <summary>
		/// Deserialises a building element list.
		/// </summary>
		/// <param name="elementList">Element list to deserialise</param>
		internal void Deserialize(List<BOBBuildingReplacement> elementList) => Deserialize(null, elementList);


		/// <summary>
		/// Applies a replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to apply</param>
		protected override void ApplyReplacement(BOBBuildingReplacement replacement)
        {
			// Don't do anything if prefabs can't be found.
			if (replacement?.targetInfo == null || replacement.replacementInfo == null)
            {
				return;
            }

			// (Re)set replacement list.
			replacement.references = new List<BuildingPropReference>();

			// Iterate through each loaded building and record props to be replaced.
			for (int i = 0; i < PrefabCollection<BuildingInfo>.LoadedCount(); ++i)
			{
				// Get local reference.
				BuildingInfo buildingInfo = PrefabCollection<BuildingInfo>.GetLoaded((uint)i);

				// If no props in this building, skip it and go to the next one.
				if (buildingInfo?.m_props == null)
				{
					continue;
				}

				// Iterate through each prop in building.
				for (int propIndex = 0; propIndex < buildingInfo.m_props.Length; ++propIndex)
				{
					// Check for any currently active building or individual building prop replacement.
					if (BuildingReplacement.Instance.ActiveReplacement(buildingInfo, propIndex) != null || IndividualBuildingReplacement.Instance.ActiveReplacement(buildingInfo, propIndex) != null)
					{
						// Active building replacement; skip this one.
						continue;
					}

					// Get this prop from building.
					PrefabInfo thisProp = replacement.tree ? (PrefabInfo)buildingInfo.m_props[propIndex].m_finalTree : (PrefabInfo)buildingInfo.m_props[propIndex].m_finalProp;

					// See if this prop matches our replacement.
					if (thisProp != null && thisProp == replacement.targetInfo)
					{
						// Match!  Add reference data to the list.
						replacement.references.Add(CreateReference(buildingInfo, propIndex));
					}
				}
			}

			// Now, iterate through each entry found (if any) and apply the replacement to each one.
			foreach (BuildingPropReference propReference in replacement.references)
			{
				ReplaceProp(replacement, propReference);
			}
		}


		/// <summary>
		/// Restores any replacements from lower-priority replacements after a reversion.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <param name="targetInfo">Target prop info</param>
		/// <param name="propIndex">Prop index</param>
		protected override void RestoreLower(BuildingInfo buildingInfo, PrefabInfo targetInfo, int propIndex) { }


		/// <summary>
		/// Gets the relevant building replacement list entry from the active configuration file, if any.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <returns>Replacement list for the specified building prefab (null if none)</returns>
		protected override List<BOBBuildingReplacement> ReplacementList(BuildingInfo buildingInfo) => ConfigurationUtils.CurrentConfig.allBuildingProps;


		/// <summary>
		/// Gets the relevant building replacement list entry from the active configuration file, creating a new building entry if none already exists.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <returns>Replacement list for the specified building prefab</returns>
		protected override List<BOBBuildingReplacement> ReplacementEntry(BuildingInfo buildingInfo) => ReplacementList(buildingInfo);
	}
}
