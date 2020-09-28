using System.Collections.Generic;


namespace BOB
{
	/// <summary>
	/// Static class to manage building prop and tree replacements.
	/// </summary>
	internal static class BuildingReplacement
	{
		// Master dictionary of building replacements.
		internal static Dictionary<BuildingInfo, SortedList<int, Replacement>> buildingDict;


		/// <summary>
		/// Performs setup and initialises the master dictionary.  Must be called prior to use.
		/// </summary>
		internal static void Setup()
		{
			buildingDict = new Dictionary<BuildingInfo, SortedList<int, Replacement>>();
		}


		/// <summary>
		/// Reverts all active building replacements and re-initialises the master dictionary.
		/// </summary>
		internal static void RevertAll()
		{
			// Iterate through each entry in the master dictionary.
			foreach (BuildingInfo building in buildingDict.Keys)
			{
				// Iterate through each active replacement for this building.
				foreach (int index in buildingDict[building].Keys)
				{
					// Revert this replacement (but don't remove the entry, as the dictionary is currently immutable while we're iterating through it).
					Revert(building, index, removeEntries: false);
				}
			}

			// Re-initialise the dictionary.
			Setup();
		}


		/// <summary>
		/// Reverts an individual building replacement.
		/// </summary>
		/// <param name="buildingPrefab">Targeted building prefab</param>
		/// <param name="index">Replacement index to revert</param>
		/// <param name="removeEntries">True (default) to remove the reverted entries from the master dictionary, false to leave the dictionary unchanged</param>
		/// <returns>True if the entire building record was removed from the dictionary (due to no remaining replacements for that prefab), false if the prefab remains in the dictionary (has other active replacements)</returns>
		internal static bool Revert(BuildingInfo buildingPrefab, int index, bool removeEntries = true)
		{
			// Get original prop.
			PrefabInfo prefabInfo = GetOriginal(buildingPrefab, index);

			// Only revert if there is an active replacement (GetOriginal returns null if there's no active replacement).
			if (prefabInfo != null)
			{
				// Tree or prop?
				if (prefabInfo is TreeInfo)
				{
					// Tree - restore original.
					buildingPrefab.m_props[index].m_finalTree = prefabInfo as TreeInfo;
				}
				else
				{
					// Prop - restore original.
					buildingPrefab.m_props[index].m_finalProp = (prefabInfo as PropInfo);
				}

				// Restore original probability.
				buildingPrefab.m_props[index].m_probability = buildingDict[buildingPrefab][index].originalProb;

				// Apply any all-building replacement.
				AllBuildingReplacement.Restore(buildingPrefab, index);

				// Remove dictionary entries if that setting is enabled.
				if (removeEntries)
				{
					// Remove individual replacement record.
					buildingDict[buildingPrefab].Remove(index);

					// Check to see if there are any remaining replacements for this building prefab.
					if (buildingDict[buildingPrefab].Count == 0)
					{
						// No remaining replacements - remove the entire building prefab entry and return true to indicate that we've done so.
						buildingDict.Remove(buildingPrefab);
						return true;
					}
				}
			}

			// If we got here, we haven't removed the building prefab entry from the master dictionary - return false to indicate that.
			return false;
		}


		/// <summary>
		/// Applies a new building replacement (replacing individual trees or props).
		/// </summary>
		/// <param name="buildingPrefab">Building prefab to apply to</param>
		/// <param name="replacement">Replacement to apply</param>
		internal static void ApplyReplacement(BuildingInfo buildingPrefab, Replacement replacement)
		{
			// Just in case.
			if (replacement.targetInfo == null || replacement.targetName == null || replacement.replaceName == null || replacement.replacementInfo == null)
			{
				Debugging.Message("invalid replacement");
				return;
			}

			// Tree or prop?
			if (replacement.isTree)
			{
				// Tree - replace the target tree with the replacement tree.
				buildingPrefab.m_props[replacement.targetIndex].m_finalTree = (TreeInfo)replacement.replacementInfo;
			}
			else
			{
				// Tree - replace the target prop with the replacement prop.
				buildingPrefab.m_props[replacement.targetIndex].m_finalProp = (PropInfo)replacement.replacementInfo;
			}

			// Apply replacement angle and probability.
			buildingPrefab.m_props[replacement.targetIndex].m_angle = replacement.angle;
			buildingPrefab.m_props[replacement.targetIndex].m_probability = replacement.probability;

			// Remove any currently applied all-building building replacement entry for this tree or prop.
			AllBuildingReplacement.RemoveEntry(buildingPrefab, replacement.targetIndex);
		}


		/// <summary>
		/// Adds or upates a building prop or tree replacement.
		/// </summary>
		/// <param name="buildingPrefab">Building prefab to apply to</param>
		/// <param name="replacement">Replacement to apply</param>
		/// <param name="index">(Optional) target index override</param>
		internal static void AddReplacement(BuildingInfo buildingPrefab, Replacement replacement, int index = -1)
		{
			// Just in case.
			if (replacement.targetInfo == null || replacement.targetName == null || replacement.replaceName == null || replacement.replacementInfo == null)
			{
				Debugging.Message("invalid replacement");
				return;
			}

			// Clone the provided replacement record for adding to the master dictionary (so the original can be modified by the calling method without clobbering the dictionary entry, and so we can tweak the clone here prior to adding without affecting the original).
			Replacement clone = ReplacementUtils.Clone(replacement);

			// Check to see if an index override has been provided.
			if (index >= 0)
			{
				// Override provided - simply use the provided index as the target index.
				clone.targetIndex = index;
			}

			// Check to see if we don't already have an entry for this building prefab in the master dictionary.
			if (!buildingDict.ContainsKey(buildingPrefab))
			{
				// No existing entry, so add one.
				buildingDict.Add(buildingPrefab, new SortedList<int, Replacement>());
			}

			// Check to see if we already have an entry for this replacement in the master dictionary.
			if (buildingDict[buildingPrefab].ContainsKey(clone.targetIndex))
			{
				// An entry already exists - update it (first preserving the original probability).
				clone.originalProb = buildingDict[buildingPrefab][clone.targetIndex].originalProb;
				buildingDict[buildingPrefab][clone.targetIndex] = clone;
			}
			else
			{
				// No existing entry - add a new one (first storing the original probability).
				clone.originalProb = buildingPrefab.m_props[clone.targetIndex].m_probability;
				buildingDict[buildingPrefab].Add(clone.targetIndex, clone);
			}

			// Apply the actual tree/prop prefab replacement.
			ApplyReplacement(buildingPrefab, clone);
		}


		/// <summary>
		/// Returns the original probability for the given building prop record.
		/// </summary>
		/// <param name="buildingPrefab">Building prefab</param>
		/// <param name="index">Building prop index</param>
		/// <returns>Original probability (which will be the current probability if no replacement is active)</returns>
		internal static int OriginalProbability(BuildingInfo buildingPrefab, int index)
		{
			// Try to find an entry for this index of this building in the master dictionary.
			if (buildingDict.ContainsKey(buildingPrefab) && buildingDict[buildingPrefab].ContainsKey(index))
			{
				// Entry found - return the stored original probability.
				return buildingDict[buildingPrefab][index].originalProb;
			}

			// No entry found - return the current probability (which IS the original probability).
			return buildingPrefab.m_props[index].m_probability;
		}


		/// <summary>
		/// Retrieves the original tree/prop prefab for the given index (returns null if there's no active replacement).
		/// </summary>
		/// <param name="buildingPrefab">Building prefab</param>
		/// <param name="index">Prop index</param>
		/// <returns>PrefabInfo of the original prefab, or null if there's no currently active replacement</returns>
		internal static PrefabInfo GetOriginal(BuildingInfo buildingPrefab, int index)
		{
			// Try to find an entry for this index of this building in the master dictionary.
			if (buildingDict.ContainsKey(buildingPrefab) && buildingDict[buildingPrefab].ContainsKey(index))
			{
				// Entry found - return the stored original prefab.
				return buildingDict[buildingPrefab][index].targetInfo;
			}

			// No entry found - return null to indicate no active replacement.
			return null;
		}
	}
}
