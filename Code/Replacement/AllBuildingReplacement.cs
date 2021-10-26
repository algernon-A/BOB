using System.Collections.Generic;


namespace BOB
{
	/// <summary>
	/// Class to manage all-builing prop and tree replacements.
	/// </summary>
	internal class AllBuildingReplacement : BuildingReplacementBase
	{
		// Instance reference.
		internal static AllBuildingReplacement instance;
		

		/// <summary>
		/// Constructor - initializes instance reference.
		/// </summary>
		internal AllBuildingReplacement()
		{
			instance = this;
		}


		/// <summary>
		/// Retrieves any currently-applied replacement entry for the given building and target prefab.
		/// </summary>
		/// <param name="buildingInfo">Building prefab (unsued)</param>
		/// <param name="targetInfo">Target prop/tree prefab</param>
		/// <param name="targetIndex">Target prop/tree index (unused)</param>
		/// <returns>Currently-applied replacement (null if none)</returns>
		internal override BOBBuildingReplacement Replacement(BuildingInfo buildingInfo, PrefabInfo targetInfo, int targetIndex) => ConfigurationUtils.CurrentConfig.allBuildingProps.Find(x => x.target.Equals(targetInfo.name));


		/// <summary>
		/// Gets the relevant building replacement list entry from the active configuration file, creating a new building entry if none already exists.
		/// </summary>
		/// <param name="buildingInfo"></param>
		/// <returns></returns>
		internal override List<BOBBuildingReplacement> ReplacementsList(BuildingInfo buildingInfo) => ConfigurationUtils.CurrentConfig.allBuildingProps;


		/// <summary>
		/// Reverts all active all-building replacements and re-initialises the master dictionary.
		/// </summary>
		internal override void RevertAll()
		{
			// Iterate through each entry in the replacement list.
			foreach (BOBBuildingReplacement replacement in ConfigurationUtils.CurrentConfig.allBuildingProps)
			{
				// Revert this replacement (but don't remove the entry, as the dictionary is currently immutable while we're iterating through it).
				Revert(replacement, false);
			}

			// Re-initialise the dictionaries.
			Setup();
		}


		/// <summary>
		/// Removes an entry from the list of all-building replacements currently applied.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <param name="targetInfo">Target prop info</param>
		/// <param name="laneIndex">Lane index</param>
		/// <param name="propIndex">Prop index</param>
		internal void RemoveEntry(BuildingInfo buildingInfo, PrefabInfo targetInfo, int propIndex)
		{
			// Check to see if we have an entry for this prefab.
			BOBBuildingReplacement thisReplacement = CurrentReplacement(targetInfo.name);
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
		/// Checks if there's a currently active all-building replacement applied to the given building and prop index, and if so, returns the replacement record.
		/// </summary>
		/// <param name="buildingPrefab">Building prefab to check</param>
		/// <param name="propIndex">Prop index to check</param>
		/// <returns>Replacement record if an all-building replacement is currently applied, null if no all-building replacement is currently applied</returns>
		internal override BOBBuildingReplacement ActiveReplacement(BuildingInfo buildingInfo, int propIndex)
		{
			// Iterate through each building replacment record in the current config.
			foreach (BOBBuildingReplacement buildingReplacement in ConfigurationUtils.CurrentConfig.allBuildingProps)
            {
				if (buildingReplacement.references != null)
				{
					// Iterate through each individual prop replacement refeerence.
					foreach (BuildingPropReference propRef in buildingReplacement.references)
					{
						// Check for a building and prop index match.
						if (propRef.building == buildingInfo && propRef.propIndex == propIndex)
						{
							// Match!  Return the replacement record.
							return buildingReplacement;
						}
					}
				}
            }

			// If we got here, no entry was found - return null to indicate no active replacement.
			return null;
		}


		/// <summary>
		/// Deserialises an all-building element list.
		/// </summary>
		/// <param name="elementList">All-building element list to deserialise</param>
		internal void Deserialize(List<BOBBuildingReplacement> elementList) => Deserialize(null, elementList);


		/// <summary>
		/// Returns the config file list of building elements relevant to the current replacement type.
		/// </summary>
		protected override List<BOBBuildingElement> BuildingElementList => null;


		/// <summary>
		/// Applies an all-building prop replacement.
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
					if (BuildingReplacement.instance.ActiveReplacement(buildingInfo, propIndex) != null || IndividualBuildingReplacement.instance.ActiveReplacement(buildingInfo, propIndex) != null)
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
						replacement.references.Add(new BuildingPropReference
						{
							building = buildingInfo,
							propIndex = propIndex,
							radAngle = buildingInfo.m_props[propIndex].m_radAngle,
							postion = buildingInfo.m_props[propIndex].m_position,
							probability = buildingInfo.m_props[propIndex].m_probability
						});
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
		/// Returns the current replacement record for the given target prefab record.
		/// </summary>
		/// <param name="targetName">Target all-building prefab name</param>
		/// <returns>Current replacement record (null if none)</returns>
		private BOBBuildingReplacement CurrentReplacement(string targetName) => ConfigurationUtils.CurrentConfig.allBuildingProps.Find(x => x.target.Equals(targetName));
	}
}
