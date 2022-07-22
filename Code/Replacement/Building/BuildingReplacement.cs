using System.Collections.Generic;


namespace BOB
{
	/// <summary>
	/// Class to manage building prop and tree replacements.
	/// </summary>
	internal class BuildingReplacement : BuildingReplacementBase
	{
		/// <summary>
		/// Constructor - initializes instance reference.
		/// </summary>
		internal BuildingReplacement()
		{
			Instance = this;
		}


		/// <summary>
		/// Instance reference.
		/// </summary>
		internal static BuildingReplacement Instance { get; private set; }


		/// <summary>
		/// Returns the config file list of building elements relevant to the current replacement type.
		/// </summary>
		protected override List<BOBBuildingElement> BuildingElementList => ConfigurationUtils.CurrentConfig.buildings;


		/// <summary>
		/// Retrieves any currently-applied replacement entry that affects the given building and target prefab and prop index.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <param name="targetInfo">Target prop/tree prefab</param>
		/// <param name="propIndex">Target prop/tree index (unused)</param>
		/// <returns>Currently-applied replacement (null if none)</returns>
		internal override BOBBuildingReplacement ActiveReplacement(BuildingInfo buildingInfo, PrefabInfo targetInfo, int propIndex) => BuildingElement(buildingInfo)?.replacements.Find(x => x.targetInfo == targetInfo);


		/// <summary>
		/// Applies a replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to apply</param>
		protected override void ApplyReplacement(BOBBuildingReplacement replacement)
		{
			// Don't do anything if prefabs can't be found, or if building prefab has no prop array.
			if (replacement?.targetInfo == null || replacement.replacementInfo == null || replacement.BuildingInfo?.m_props == null)
			{
				return;
			}

			// (Re)set replacement list.
			replacement.references = new List<BuildingPropReference>();

			// Iterate through each prop in building.
			for (int propIndex = 0; propIndex < replacement.BuildingInfo.m_props.Length; ++propIndex)
			{
				// Check for any currently active individual building prop replacement, or if this is an added prop.
				if (IndividualBuildingReplacement.Instance.ActiveReplacement(replacement.BuildingInfo, propIndex, out _) != null || AddedBuildingProps.Instance.IsAdded(replacement.BuildingInfo, propIndex))
				{
					// Active individual building prop replacement or added prop; skip this one.
					continue;
				}

				// Local reference.
				BuildingInfo.Prop thisBuildingProp = replacement.BuildingInfo.m_props[propIndex];

				// Check for any existing all-building replacement.
				PrefabInfo thisProp = AllBuildingReplacement.Instance.ActiveReplacement(replacement.BuildingInfo, propIndex, out _)?.targetInfo;
				if (thisProp == null)
				{
					// No active replacement; use current PropInfo.
					if (replacement.targetInfo is PropInfo)
					{
						thisProp = thisBuildingProp.m_finalProp;
					}
					else
					{
						thisProp = thisBuildingProp.m_finalTree;
					}
				}

				// See if this prop matches our replacement.
				if (thisProp != null && thisProp == replacement.targetInfo)
				{
					// Match!  Add reference data to the list.
					replacement.references.Add(CreateReference(replacement.BuildingInfo, thisProp, propIndex, replacement.isTree));
				}
			}

			// Now, iterate through each entry found.
			foreach (BuildingPropReference propReference in replacement.references)
			{
				// Reset any all-building replacements first.
				AllBuildingReplacement.Instance.RemoveEntry(propReference.buildingInfo, replacement.targetInfo, propReference.propIndex);

				// Apply the replacement.
				ReplaceProp(replacement, propReference);
			}
		}


		/// <summary>
		/// Restores any replacements from lower-priority replacements after a reversion.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <param name="targetInfo">Target prop info</param>
		/// <param name="propIndex">Prop index</param>
		protected override void RestoreLower(BuildingInfo buildingInfo, PrefabInfo targetInfo, int propIndex) => AllBuildingReplacement.Instance.Restore(buildingInfo, targetInfo, propIndex);
	}
}
