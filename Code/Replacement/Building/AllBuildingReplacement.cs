using System.Collections.Generic;


namespace BOB
{
	/// <summary>
	/// Class to manage all-builing prop and tree replacements.
	/// </summary>
	internal class AllBuildingReplacement : BuildingReplacementBase
	{
		// Dictionary of active replacment entries.
		private readonly Dictionary<BuildingInfo, Dictionary<PrefabInfo, List<BuildingPropReference>>> propReferences;


		/// <summary>
		/// Constructor - initializes instance reference and replacement dictionary.
		/// </summary>
		internal AllBuildingReplacement()
		{
			Instance = this;
			propReferences = new Dictionary<BuildingInfo, Dictionary<PrefabInfo, List<BuildingPropReference>>>();
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
		/// <param name="buildingInfo">Targeted building prefab</param>
		/// <param name="targetInfo">Target prop/tree prefab</param>
		/// <param name="propIndex">Target prop/tree index (unused)</param>
		/// <returns>Currently-applied replacement (null if none)</returns>
		internal override BOBBuildingReplacement EligibileReplacement(BuildingInfo buildingInfo, PrefabInfo targetInfo, int propIndex) => ReplacementList(buildingInfo)?.Find(x => x.target.Equals(targetInfo.name));


		/// <summary>
		/// Retuns the list of active prop references for the given replacement value(s).
		/// </summary>
		/// <param name="buildingInfo">Targeted building prefab</param>
		/// <param name="targetInfo">Targeted (original) prop prefab</param>
		/// <param name="propIndex">Targeted prop index (in lanme)</param>
		/// <returns>List of active prop references for the given replacment values (null if none)</returns>
		internal override List<BuildingPropReference> ReferenceList(BuildingInfo buildingInfo, PrefabInfo targetInfo, int propIndex)
		{
			// See if we've got any active references for this net prefab.
			if (propReferences.TryGetValue(buildingInfo, out Dictionary<PrefabInfo, List<BuildingPropReference>> referenceDict))
			{
				// See if we've got any active references for this target prefab.
				if (referenceDict.TryGetValue(targetInfo, out List<BuildingPropReference> referenceList))
				{
					// Got it - return the list.
					return referenceList;
				}
			}

			// If we got here, we didn't get anything; return null.
			return null;
		}


		/// <summary>
		/// Reverts all active replacements.
		/// </summary>
		internal override void RevertAll()
		{
			// Revert all active references in dictionary.
			foreach (BuildingInfo buildingPrefab in propReferences.Keys)
			{
				foreach (PrefabInfo targetPrefab in propReferences[buildingPrefab].Keys)
				{
					RevertReferences(targetPrefab, propReferences[buildingPrefab][targetPrefab]);
				}
			}

			// Clear configuration file.
			ReplacementList(null).Clear();

			// Clear the active replacements dictionary.
			propReferences.Clear();
		}


		/// <summary>
		/// Checks if there's a currently active replacement applied to the given building and prop index, and if so, returns the replacement record.
		/// </summary>
		/// <param name="buildingInfo">Building prefab to check</param>
		/// <param name="propIndex">Prop index to check</param>
		/// <returns>Replacement record if a replacement is currently applied, null if no replacement is currently applied</returns>
		internal override BOBBuildingReplacement ActiveReplacement(BuildingInfo buildingInfo, int propIndex)
		{
			// See if we've got any active references for this net prefab.
			if (!propReferences.TryGetValue(buildingInfo, out Dictionary<PrefabInfo, List<BuildingPropReference>> referenceDict))
			{
				return null;
			}

			// Iterate through entry for each prefab under this network.
			foreach (KeyValuePair<PrefabInfo, List<BuildingPropReference>> key in referenceDict)
			{
				// Iterate through each entry in list.
				foreach (BuildingPropReference propRef in key.Value)
				{
					// Check for a a network(due to all- replacement), lane and prop index match.
					if (propRef.buildingInfo == buildingInfo && propRef.propIndex == propIndex)
					{
						// Match!  Find and return the replacement record.
						return EligibileReplacement(buildingInfo, key.Key, propRef.propIndex);
					}
				}
			}

			// If we got here, no entry was found - return null to indicate no active replacement.
			return null;
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

			// Create new reference list.
			List<BuildingPropReference> referenceList = new List<BuildingPropReference>();

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
					PrefabInfo thisProp = replacement.isTree ? (PrefabInfo)buildingInfo.m_props[propIndex].m_finalTree : (PrefabInfo)buildingInfo.m_props[propIndex].m_finalProp;

					// See if this prop matches our replacement.
					if (thisProp != null && thisProp == replacement.targetInfo)
					{
						// Match!  Add reference data to the list.
						referenceList.Add(CreateReference(buildingInfo, propIndex, replacement.isTree));
					}
				}
			}

			// Now, iterate through each entry found (if any) and apply the replacement to each one.
			foreach (BuildingPropReference propReference in referenceList)
			{
				// Add entry to dictionary.
				AddReference(replacement, propReference);

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


		/// <summary>
		/// Reverts a replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to revert</param>
		/// <param name="removeEntries">True to remove the reverted entries from the list of replacements, false to leave the list unchanged</param>
		/// <returns>Always false (all-building entries never remove parent network elements)</returns>
		protected override bool Revert(BOBBuildingReplacement replacement, bool removeEntries)
		{
			// Safety check for calls without any current replacement.
			if (replacement?.targetInfo == null)
			{
				return false;
			}

			// List of prefabs where references need to be removed.
			List<BuildingInfo> removeList = new List<BuildingInfo>();

			// Iterate through each entry in prop reference dictionary.
			foreach (KeyValuePair<BuildingInfo, Dictionary<PrefabInfo, List<BuildingPropReference>>> keyPair in propReferences)
			{
				// Attempt to get a replacement list for this entry.
				if (keyPair.Value.TryGetValue(replacement.targetInfo, out List<BuildingPropReference> referenceList))
				{
					// Got a list - revert all entries.
					RevertReferences(replacement.targetInfo, referenceList);

					// Add dictionary to list to be cleared.
					removeList.Add(keyPair.Key);
				}
			}

			// Remove references from dictionary.
			foreach (BuildingInfo removeEntry in removeList)
			{
				if (propReferences.ContainsKey(removeEntry))
				{
					if (propReferences[removeEntry].ContainsKey(replacement.targetInfo))
					{
						// Remove target info entries from this building entry.
						propReferences[removeEntry].Remove(replacement.targetInfo);
					}

					// If no entries left for this building, remove entire building entry.
					if (propReferences[removeEntry].Count == 0)
					{
						propReferences.Remove(removeEntry);
					}
				}
			}

			// Remove replacement entry from list of replacements, if we're doing so.
			if (removeEntries)
			{
				// Remove from replacement list.
				ReplacementList(replacement.buildingInfo).Remove(replacement);
			}

			// If we got here, we didn't remove any network entries from the list; return false.
			return false;
		}


		/// <summary>
		/// Adds the given prop reference to the record for the given replacement.
		/// </summary>
		/// <param name="replacement">Replacement reference</param>
		/// <param name="propReference">Pop reference to store</param>
		protected override void AddReference(BOBBuildingReplacement replacement, BuildingPropReference propReference)
		{
			// Check to see if we've got an entry for this target prefab in our dictionary, and if not, create one.
			if (!propReferences.ContainsKey(propReference.buildingInfo))
			{
				propReferences.Add(propReference.buildingInfo, new Dictionary<PrefabInfo, List<BuildingPropReference>>());
			}

			// Check to see if we've got an entry for this network prefab in our dictionary entry for this target prefab, and if not, create one.
			if (!propReferences[propReference.buildingInfo].ContainsKey(replacement.targetInfo))
			{
				propReferences[propReference.buildingInfo].Add(replacement.targetInfo, new List<BuildingPropReference>());
			}

			// Add this prop reference to the dictioanry.
			propReferences[propReference.buildingInfo][replacement.targetInfo].Add(propReference);
		}
	}
}
