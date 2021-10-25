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
		/// <param name="buildingInfo">Building prefab</param>
		/// <param name="targetInfo">Target prop/tree prefab</param>
		/// <returns>Currently-applied replacement (null if none)</returns>
		internal override BOBBuildingReplacement Replacement(BuildingInfo _, PrefabInfo targetInfo) => ConfigurationUtils.CurrentConfig.allBuildingProps.Find(x => x.target.Equals(targetInfo.name));


		/// <summary>
		/// Applies a new (or updated) all-building replacement.
		/// </summary>
		/// <param name="buildingInfo">Targeted building prefab (ignored)</param>
		/// <param name="targetInfo">Targeted (original) prop prefab</param>
		/// <param name="replacementInfo">Replacment prop prefab</param>
		/// <param name="targetIndex">Prop index to apply replacement to (ignored)</param>
		/// <param name="angle">Replacment prop angle adjustment</param>
		/// <param name="offsetX">Replacment X position offset</param>
		/// <param name="offsetY">Replacment Y position offset</param>
		/// <param name="offsetZ">Replacment Z position offset</param>
		/// <param name="probability">Replacement probability</param>
		internal override void Replace(BuildingInfo buildingInfo, PrefabInfo targetInfo, PrefabInfo replacementInfo, int targetIndex, float angle, float offsetX, float offsetY, float offsetZ, int probability)
		{
			// Make sure that target and replacement are valid and the same type before doing anything.
			if (targetInfo?.name == null || replacementInfo?.name == null || (targetInfo is TreeInfo && !(replacementInfo is TreeInfo)) || (targetInfo is PropInfo) && !(replacementInfo is PropInfo))
			{
				return;
			}

			// Revert any current replacement entry for this prop.
			Revert(targetInfo.name, true);

			// Get current replacement after reversion above.
			BOBBuildingReplacement thisReplacement = CurrentReplacement(targetInfo.name);

			// Create new replacement list entry if none already exists.
			if (thisReplacement == null)
			{
				thisReplacement = new BOBBuildingReplacement
				{
					target = targetInfo.name,
					targetInfo = targetInfo
				};
				ConfigurationUtils.CurrentConfig.allBuildingProps.Add(thisReplacement);
			}

			// Add/replace list replacement data.
			thisReplacement.tree = targetInfo is TreeInfo;
			thisReplacement.angle = angle;
			thisReplacement.offsetX = offsetX;
			thisReplacement.offsetY = offsetY;
			thisReplacement.offsetZ = offsetZ;
			thisReplacement.probability = probability;

			// Record replacement prop.
			thisReplacement.replacementInfo = replacementInfo;
			thisReplacement.Replacement = replacementInfo.name;

			// Apply replacement.
			ApplyReplacement(thisReplacement);
		}


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
		/// Reverts an all-building replacement.
		/// </summary>
		/// <param name="targetName">Targeted (original) tree/prop prefab name</param>
		/// <param name="removeEntries">True (default) to remove the reverted entries from the list of replacements, false to leave the list unchanged</param>
		internal void Revert(string targetName, bool removeEntries) => Revert(CurrentReplacement(targetName), removeEntries);


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
		internal void Deserialize(List<BOBBuildingReplacement> elementList)
		{
			// Iterate through each element in the provided list.
			foreach (BOBBuildingReplacement replacement in elementList)
			{
				// Try to find target prefab.
				replacement.targetInfo = replacement.tree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacement.target) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacement.target);

				// Try to find replacement prefab.
				replacement.replacementInfo = ConfigurationUtils.FindReplacementPrefab(replacement.Replacement, replacement.tree);

				// Try to apply the replacement.
				ApplyReplacement(replacement);
			}
		}


		/// <summary>
		/// Applies an all-building prop replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to apply</param>
		protected void ApplyReplacement(BOBBuildingReplacement replacement)
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


		/// <summary>
		/// Reverts an all-building replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to revert</param>
		/// <param name="removeEntries">True (default) to remove the reverted entries from the list of replacements, false to leave the list unchanged</param>
		private void Revert(BOBBuildingReplacement replacement, bool removeEntries = true)
		{
			// Safety check for calls without any current replacement.
			if (replacement == null)
			{
				return;
			}

			if (replacement.references != null)
			{
				// Iterate through each entry in our list.
				foreach (BuildingPropReference propReference in replacement.references)
				{
					// Revert entry.
					if (replacement.tree)
					{
						propReference.building.m_props[propReference.propIndex].m_finalTree = replacement.TargetTree;
					}
					else
					{
						propReference.building.m_props[propReference.propIndex].m_finalProp = replacement.TargetProp;
					}
					propReference.building.m_props[propReference.propIndex].m_radAngle = propReference.radAngle;
					propReference.building.m_props[propReference.propIndex].m_position = propReference.postion;
					propReference.building.m_props[propReference.propIndex].m_probability = propReference.probability;

					// Add building to dirty list.
					BuildingData.DirtyList.Add(propReference.building);
				}
			}

			// Remove entry from dictionary, if we're doing so.
			if (removeEntries)
			{
				ConfigurationUtils.CurrentConfig.allBuildingProps.Remove(replacement);
			}
		}
	}
}
