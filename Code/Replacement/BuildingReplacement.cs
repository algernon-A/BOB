using System.Collections.Generic;
using System.Linq;


namespace BOB
{
	/// <summary>
	/// Class to manage building prop and tree replacements.
	/// </summary>
	internal class BuildingReplacement : BuildingReplacementBase
	{
		// Instance reference.
		internal static BuildingReplacement instance;


		/// <summary>
		/// Constructor - initializes instance reference.
		/// </summary>
		internal BuildingReplacement()
		{
			instance = this;
		}


		/// <summary>
		/// Retrieves any currently-applied replacement entry for the given building and target prefab.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <param name="targetInfo">Target prop/tree prefab</param>
		/// <param name="targetIndex">Target prop/tree index (unused)</param>
		/// <returns>Currently-applied replacement (null if none)</returns>
		internal override BOBBuildingReplacement Replacement(BuildingInfo buildingInfo, PrefabInfo targetInfo, int targetIndex) => BuildingElementList.Find(x => x.buildingInfo == buildingInfo)?.replacements.Find(x => x.targetInfo == targetInfo);


		/// <summary>
		/// Removes an entry from the list of building replacements currently applied to building.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <param name="targetInfo">Target prop info</param>
		/// <param name="laneIndex">Lane index</param>
		/// <param name="propIndex">Prop index</param>
		internal void RemoveEntry(BuildingInfo buildingInfo, PrefabInfo targetInfo, int propIndex)
		{
			// Check to see if we have an entry for this prefab and target.
			BOBBuildingReplacement thisReplacement = Replacement(buildingInfo, targetInfo, -1);
			if (thisReplacement != null)
			{
				BuildingPropReference thisPropReference = null;

				// Iterate through each recorded prop reference.
				foreach (BuildingPropReference propReference in thisReplacement.references)
				{
					// Look for a building and index match.
					if (propReference.building == buildingInfo && propReference.propIndex == propIndex)
					{
						// Got a match!  Revert instance.
						if (targetInfo is PropInfo propTarget)
						{
							propReference.building.m_props[propReference.propIndex].m_finalProp = propTarget;
						}
						else
						{
							propReference.building.m_props[propReference.propIndex].m_finalTree = (TreeInfo)targetInfo;
						}
						buildingInfo.m_props[propIndex].m_radAngle = propReference.radAngle;
						buildingInfo.m_props[propIndex].m_position = propReference.postion;
						buildingInfo.m_props[propIndex].m_probability = propReference.probability;

						// Record the matching reference and stop iterating - we're done here.
						thisPropReference = propReference;
						break;
					}
				}

				// Remove replacement if one was found.
				if (thisPropReference != null)
				{
					thisReplacement.references.Remove(thisPropReference);
				}
				return;
			}
		}


		/// <summary>
		/// Returns the config file list of building elements relevant to the current replacement type.
		/// </summary>
		protected override List<BOBBuildingElement> BuildingElementList => ConfigurationUtils.CurrentConfig.buildings;


		/// <summary>
		/// Applies a building prop replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to apply</param>
		protected override void ApplyReplacement(BOBBuildingReplacement replacement)
		{
			// Don't do anything if prefabs can't be found.
			if (replacement?.targetInfo == null || replacement.replacementInfo == null || replacement.buildingInfo == null)
			{
				return;
			}

			// (Re)set replacement list.
			replacement.references = new List<BuildingPropReference>();

			// Iterate through each prop in building.
			for (int propIndex = 0; propIndex < replacement.buildingInfo.m_props.Length; ++propIndex)
			{
				// Check for any currently active individual building prop replacement.
				if (IndividualBuildingReplacement.instance.ActiveReplacement(replacement.buildingInfo, propIndex) != null)
				{
					// Active individual building prop replacement; skip this one.
					continue;
				}

				// Check for any existing all-building replacement.
				PrefabInfo thisProp = AllBuildingReplacement.instance.ActiveReplacement(replacement.buildingInfo, propIndex)?.targetInfo;
				if (thisProp == null)
				{
					// No active replacement; use current PropInfo.
					if (replacement.targetInfo is PropInfo)
					{
						thisProp = replacement.buildingInfo.m_props[propIndex].m_finalProp;
					}
					else
					{
						thisProp = replacement.buildingInfo.m_props[propIndex].m_finalTree;
					}
				}

				// See if this prop matches our replacement.
				if (thisProp != null && thisProp == replacement.targetInfo)
				{
					// Match!  Add reference data to the list.
					replacement.references.Add(new BuildingPropReference
					{
						building = replacement.buildingInfo,
						propIndex = propIndex,
						radAngle = replacement.buildingInfo.m_props[propIndex].m_radAngle,
						postion = replacement.buildingInfo.m_props[propIndex].m_position,
						probability = replacement.buildingInfo.m_props[propIndex].m_probability
					});
				}
			}

			// Now, iterate through each entry found.
			foreach (BuildingPropReference propReference in replacement.references)
			{
				// Reset any all-building replacements first.
				AllBuildingReplacement.instance.RemoveEntry(replacement.buildingInfo, replacement.targetInfo, propReference.propIndex);

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
		/// <returns>True if a restoration was made, false otherwise</returns>
		protected override void ReplaceLower(BuildingInfo buildingInfo, PrefabInfo targetInfo, int propIndex) => AllBuildingReplacement.instance.Restore(buildingInfo, targetInfo, propIndex);
	}
}
