using System.Collections.Generic;


namespace BOB
{
	/// <summary>
	/// Static class to manage all-builing prop and tree replacements.
	/// </summary>
	internal class AllBuildingReplacement
	{
		// Instance reference.
		internal static AllBuildingReplacement instance;

		// Master dictionary of replaced prop references.
		internal static Dictionary<PrefabInfo, BOBBuildingReplacement> replacements;


		/// <summary>
		/// Constructor - initializes instance reference and calls initial setup.
		/// </summary>
		internal AllBuildingReplacement()
		{
			instance = this;
			Setup();
		}


		/// <summary>
		/// Performs setup and initialises the master dictionary.  Must be called prior to use.
		/// </summary>
		internal void Setup()
		{
			replacements = new Dictionary<PrefabInfo, BOBBuildingReplacement>();
		}


		/// <summary>
		/// Reverts all active all-building replacements and re-initialises the master dictionary.
		/// </summary>
		internal void RevertAll()
		{
			// Iterate through each entry in the master prop dictionary.
			foreach (PrefabInfo prop in replacements.Keys)
			{
				// Revert this replacement (but don't remove the entry, as the dictionary is currently immutable while we're iterating through it).
				Revert(prop, removeEntries: false);
			}

			// Re-initialise the dictionaries.
			Setup();
		}


		/// <summary>
		/// Reverts an all-building replacement.
		/// </summary>
		/// <param name="target">Targeted (original) tree/prop prefab</param>
		/// <param name="removeEntries">True (default) to remove the reverted entries from the master dictionary, false to leave the dictionary unchanged</param>
		/// <returns>True if the entire building record was removed from the dictionary (due to no remaining replacements for that prefab), false if the prefab remains in the dictionary (has other active replacements)</returns>
		internal void Revert(PrefabInfo target, bool removeEntries = true)
		{
			// Iterate through each entry in our dictionary.
			foreach (BuildingPropReference propReference in replacements[target].references)
			{
				// Revert entry.
				if (target is PropInfo propTarget)
				{
					propReference.building.m_props[propReference.propIndex].m_finalProp = propTarget;
				}
				else
				{
					propReference.building.m_props[propReference.propIndex].m_finalTree = (TreeInfo)target;
				}
				propReference.building.m_props[propReference.propIndex].m_radAngle = propReference.radAngle;
				propReference.building.m_props[propReference.propIndex].m_position = propReference.postion;
				propReference.building.m_props[propReference.propIndex].m_probability = propReference.probability;

				// Add building to dirty list.
				BuildingData.DirtyList.Add(propReference.building);
			}

			// Remove entry from dictionary, if we're doing so.
			if (removeEntries)
			{
				replacements.Remove(target);
			}
		}


		/// <summary>
		/// Removes an entry from the master dictionary of all-building replacements currently applied to building.
		/// </summary>
		/// <param name="buildingPrefab">Building prefab</param>
		/// <param name="target">Target prop info</param>
		/// <param name="laneIndex">Lane index</param>
		/// <param name="propIndex">Prop index</param>
		internal void RemoveEntry(BuildingInfo buildingPrefab, PrefabInfo target, int propIndex)
		{
			// Check to see if we have an entry for this prefab.
			if (replacements.ContainsKey(target))
			{
				// Yes - iterate through each recorded prop reference.
				for (int i = 0; i < replacements[target].references.Count; ++i)
				{
					// Look for a building and index match.
					BuildingPropReference propReference = replacements[target].references[i];
					if (propReference.building == buildingPrefab && propReference.propIndex == propIndex)
					{
						// Got a match!  Revert instance.
						if (target is PropInfo propTarget)
						{
							propReference.building.m_props[propReference.propIndex].m_finalProp = propTarget;
						}
						else
						{
							propReference.building.m_props[propReference.propIndex].m_finalTree = (TreeInfo)target;
						}
						buildingPrefab.m_props[propIndex].m_radAngle = propReference.radAngle;
						buildingPrefab.m_props[propIndex].m_position = propReference.postion;
						buildingPrefab.m_props[propIndex].m_probability = propReference.probability;

						// Remove this reference and return.
						replacements[target].references.Remove(replacements[target].references[i]);
						return;
					}
				}
			}
		}


		/// <summary>
		/// Applies a new (or updated) all-building replacement.
		/// </summary>
		/// <param name="target">Targeted (original) prop prefab</param>
		/// <param name="replacement">Replacment prop prefab</param>
		/// <param name="angle">Replacment prop angle adjustment</param>
		/// <param name="offsetX">Replacment X position offset</param>
		/// <param name="offsetY">Replacment Y position offset</param>
		/// <param name="offsetZ">Replacment Z position offset</param>
		/// <param name="probability">Replacement probability</param>
		internal void Apply(PrefabInfo target, PrefabInfo replacement, float angle, float offsetX, float offsetY, float offsetZ, int probability)
		{
			// Make sure that target and replacement are the same type before doing anything.
			if (target == null || replacement == null || (target is TreeInfo && !(replacement is TreeInfo)) || (target is PropInfo) && !(replacement is PropInfo))
			{
				return;
			}

			// Check to see if we already have a replacement entry for this prop - if so, revert the replacement first.
			if (replacements.ContainsKey(target))
			{
				Revert(target, true);
			}

			// Create new dictionary entry if none already exists.
			if (!replacements.ContainsKey(target))
			{
				replacements.Add(target, new BOBBuildingReplacement());
			}
			// Add/replace dictionary replacement data.
			replacements[target].references = new List<BuildingPropReference>();
			replacements[target].tree = target is TreeInfo;
			replacements[target].targetInfo = target;
			replacements[target].target = target.name;
			replacements[target].angle = angle;
			replacements[target].offsetX = offsetX;
			replacements[target].offsetY = offsetY;
			replacements[target].offsetZ = offsetZ;
			replacements[target].probability = probability;

			// Record replacement prop.
			replacements[target].replacementInfo = replacement;
			replacements[target].replacement = replacement.name;

			// Iterate through each loaded building and record props to be replaced.
			for (int i = 0; i < PrefabCollection<BuildingInfo>.LoadedCount(); ++i)
			{
				// Get local reference.
				BuildingInfo building = PrefabCollection<BuildingInfo>.GetLoaded((uint)i);

				// If no props in this building, skip it and go to the next one.
				if (building?.m_props == null)
				{
					continue;
				}

				// Iterate through each prop in building.
				for (int propIndex = 0; propIndex < building.m_props.Length; ++propIndex)
				{
					// Check for any currently active building or individual building prop replacement.
					if (BuildingReplacement.instance.GetOriginal(building, propIndex) != null || IndividualReplacement.instance.GetOriginal(building, propIndex) != null)
					{
						// Active building replacement; skip this one.
						continue;
					}

					// Get this prop from building.
					PrefabInfo thisProp = target is PropInfo ? (PrefabInfo)building.m_props[propIndex].m_finalProp : (PrefabInfo)building.m_props[propIndex].m_finalTree;

					// See if this prop matches our replacement.
					if (thisProp != null && thisProp == target)
					{
						// Match!  Add reference data to the list.
						replacements[target].references.Add(new BuildingPropReference
						{
							building = building,
							propIndex = propIndex,
							radAngle = building.m_props[propIndex].m_radAngle,
							postion = building.m_props[propIndex].m_position,
							probability = building.m_props[propIndex].m_probability
						});
					}
				}
			}

			// Now, iterate through each entry found and apply the replacement to each one.
			foreach (BuildingPropReference propReference in replacements[target].references)
			{
				BuildingReplacement.instance.ReplaceProp(replacements[target], propReference);
			}
		}


		/// <summary>
		/// Checks if there's a currently active all-building replacement applied to the given building prop index, and if so, returns the *original* prefab..
		/// </summary>
		/// <param name="buildingPrefab">Building prefab to check</param>
		/// <param name="propIndex">Prop index to check</param>
		/// <returns>Replacement record if an all-building replacement is currently applied, null if no all-building replacement is currently applied</returns>
		internal PrefabInfo GetOriginal(BuildingInfo buildingPrefab, int propIndex)
		{
			// Iterate through each entry in master dictionary.
			foreach (PrefabInfo target in replacements.Keys)
			{
				BOBBuildingReplacement reference = replacements[target];
				// Iterate through each reference in this entry.
				foreach (BuildingPropReference propRef in reference.references)
				{
					// Check for a building and prop index match.
					if (propRef.building == buildingPrefab  && propRef.propIndex == propIndex)
					{
						// Match!  Return the original prefab.
						return target;
					}
				}
			}

			// If we got here, no entry was found - return null to indicate no active replacement.
			return null;
		}


		/// <summary>
		/// Checks if there's a currently active all-building replacement applied to the given building prop index, and if so, returns the replacement record.
		/// </summary>
		/// <param name="buildingPrefab">Building prefab to check</param>
		/// <param name="propIndex">Prop index to check</param>
		/// <returns>Replacement record if an all-building replacement is currently applied, null if no all-building replacement is currently applied</returns>
		internal BOBBuildingReplacement ActiveReplacement(BuildingInfo buildingPrefab, int propIndex)
		{
			// Iterate through each entry in master dictionary.
			foreach (PrefabInfo target in replacements.Keys)
			{
				BOBBuildingReplacement reference = replacements[target];
				// Iterate through each reference in this entry.
				foreach (BuildingPropReference propRef in reference.references)
				{
					// Check for a building and prop index match.
					if (propRef.building == buildingPrefab && propRef.propIndex == propIndex)
					{
						// Match!  Return the replacement record.
						return replacements[target];
					}
				}
			}

			// If we got here, no entry was found - return null to indicate no active replacement.
			return null;
		}


		/// <summary>
		/// Restores a all-building replacement, if any (e.g. after a building replacement has been reverted).
		/// </summary>
		/// <param name="buildingPrefab">Building prefab</param>
		/// <param name="target">Target prop info</param>
		/// <param name="propIndex">Prop index</param>
		internal void Restore(BuildingInfo buildingPrefab, PrefabInfo target, int propIndex)
		{
			// Check to see if we have an entry for this prefab.
			if (replacements.ContainsKey(target))
			{
				// Yes - add reference data to the list.
				BuildingPropReference newReference = new BuildingPropReference
				{
					building = buildingPrefab,
					propIndex = propIndex,
					radAngle = buildingPrefab.m_props[propIndex].m_radAngle,
					postion = buildingPrefab.m_props[propIndex].m_position,
					probability = buildingPrefab.m_props[propIndex].m_probability
				};

				replacements[target].references.Add(newReference);

				// Apply replacement.
				BuildingReplacement.instance.ReplaceProp(replacements[target], newReference);
			}
		}
	}
}
