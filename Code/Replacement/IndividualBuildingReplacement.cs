using System.Collections.Generic;
using System.Linq;


namespace BOB
{
	/// <summary>
	/// Class to manage individual building prop and tree replacements.
	/// </summary>
	internal class IndividualBuildingReplacement : BuildingReplacementBase
	{
		// Instance reference.
		internal static IndividualBuildingReplacement instance;


		/// <summary>
		/// Constructor - initializes instance reference.
		/// </summary>
		internal IndividualBuildingReplacement()
		{
			instance = this;
		}


		/// <summary>
		/// Retrieves any currently-applied replacement entry for the given building prefab, target prefab and prop index.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <param name="targetInfo">Target prop/tree prefab</param>
		/// <param name="targetIndex">Target prop/tree index</param>
		/// <returns>Currently-applied replacement (null if none)</returns>
		internal override BOBBuildingReplacement Replacement(BuildingInfo buildingInfo, PrefabInfo targetInfo, int targetIndex) => BuildingElementList.Find(x => x.buildingInfo == buildingInfo)?.replacements?.Find(x => x.index == targetIndex);


		/// <summary>
		/// Returns the config file list of building elements relevant to the current replacement type.
		/// </summary>
		protected override List<BOBBuildingElement> BuildingElementList => ConfigurationUtils.CurrentConfig.indBuildings;


		/// <summary>
		/// Applies an individual building prop replacement.
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
			BuildingInfo.Prop thisProp = replacement.buildingInfo.m_props[replacement.index];
			if (thisProp == null)
            {
				return;
			}

			// Reset any building or all-building replacements first.
			BuildingReplacement.instance.RemoveEntry(replacement.buildingInfo, replacement.targetInfo, replacement.index);
			AllBuildingReplacement.instance.RemoveEntry(replacement.buildingInfo, replacement.targetInfo, replacement.index);

			// Create replacment entry.
			BuildingPropReference newPropReference = new BuildingPropReference
			{
				building = replacement.buildingInfo,
				propIndex = replacement.index,
				radAngle = thisProp.m_radAngle,
				postion = thisProp.m_position,
				probability = thisProp.m_probability
			};

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
		/// <returns>True if a restoration was made, false otherwise</returns>
		protected override void ReplaceLower(BuildingInfo buildingInfo, PrefabInfo targetInfo, int propIndex)
		{
			// Restore any building replacement.
			if (!BuildingReplacement.instance.Restore(buildingInfo, targetInfo, propIndex))
			{
				// No building restoration occured - restore any all-building replacement.
				AllBuildingReplacement.instance.Restore(buildingInfo, targetInfo, propIndex);
			}
		}
	}
}
