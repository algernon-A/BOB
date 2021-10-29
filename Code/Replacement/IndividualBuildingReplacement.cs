using System.Collections.Generic;


namespace BOB
{
	/// <summary>
	/// Class to manage individual building prop and tree replacements.
	/// </summary>
	internal class IndividualBuildingReplacement : BuildingReplacementBase
	{
		/// <summary>
		/// Constructor - initializes instance reference.
		/// </summary>
		internal IndividualBuildingReplacement()
		{
			Instance = this;
		}


		/// <summary>
		/// Instance reference.
		/// </summary>
		internal static IndividualBuildingReplacement Instance { get; private set; }


		/// <summary>
		/// Returns the config file list of building elements relevant to the current replacement type.
		/// </summary>
		protected override List<BOBBuildingElement> BuildingElementList => ConfigurationUtils.CurrentConfig.indBuildings;


		/// <summary>
		/// Retrieves any currently-applied replacement entry for the given building prefab, target prefab and prop index.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <param name="targetInfo">Target prop/tree prefab</param>
		/// <param name="propIndex">Target prop/tree index</param>
		/// <returns>Currently-applied replacement (null if none)</returns>
		internal override BOBBuildingReplacement EligibileReplacement(BuildingInfo buildingInfo, PrefabInfo targetInfo, int propIndex) => ReplacementList(buildingInfo)?.Find(x => x.propIndex == propIndex);


		/// <summary>
		/// Applies a replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to apply</param>
		protected override void ApplyReplacement(BOBBuildingReplacement replacement)
		{
			// Don't do anything if prefabs can't be found.
			if (replacement?.targetInfo == null || replacement.replacementInfo == null || replacement.buildingInfo == null)
			{
				return;
			}

			// Check prop index.
			BuildingInfo.Prop thisProp = replacement.buildingInfo.m_props[replacement.propIndex];
			if (thisProp == null)
            {
				return;
			}

			// Reset any building or all-building replacements first.
			BuildingReplacement.Instance.RemoveEntry(replacement.buildingInfo, replacement.targetInfo, replacement.propIndex); ;
			AllBuildingReplacement.Instance.RemoveEntry(replacement.buildingInfo, replacement.targetInfo, replacement.propIndex);

			// Create replacment entry.
			BuildingPropReference newPropReference = CreateReference(replacement.buildingInfo, replacement.propIndex, replacement.isTree);

			// Reset replacement list to be only our new replacement entry.
			replacement.references = new List<BuildingPropReference> { newPropReference };

			// Apply the replacement.
			ReplaceProp(replacement, newPropReference);
		}


		/// <summary>
		/// Restores any replacements from lower-priority replacements after a reversion.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <param name="targetInfo">Target prop info</param>
		/// <param name="propIndex">Prop index</param>
		protected override void RestoreLower(BuildingInfo buildingInfo, PrefabInfo targetInfo, int propIndex)
		{
			// Restore any building replacement.
			if (!BuildingReplacement.Instance.Restore(buildingInfo, targetInfo, propIndex))
			{
				// No building restoration occured - restore any all-building replacement.
				AllBuildingReplacement.Instance.Restore(buildingInfo, targetInfo, propIndex);
			}
		}
	}
}
