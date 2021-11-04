using System.Collections.Generic;


namespace BOB
{
	/// <summary>
	/// Class to manage all-network prop and tree replacements.
	/// </summary>
	internal class AllNetworkReplacement : NetworkReplacementBase
	{
		// Dictionary of active replacment entries.
		private readonly Dictionary<NetInfo, Dictionary<PrefabInfo, List<NetPropReference>>> propReferences;


		/// <summary>
		/// Constructor - initializes instance reference and replacement dictionary.
		/// </summary>
		internal AllNetworkReplacement()
		{
			Instance = this;
			propReferences = new Dictionary<NetInfo, Dictionary<PrefabInfo, List<NetPropReference>>>();
		}


		/// <summary>
		/// Instance reference.
		/// </summary>
		internal static AllNetworkReplacement Instance { get; private set; }


		/// <summary>
		/// Returns the config file list of elements relevant to the current replacement type.
		/// </summary>
		protected override List<BOBNetworkElement> NetworkElementList => null;


		/// <summary>
		/// Retrieves a currently-applied replacement entry for the given network, lane and prop index.
		/// </summary>
		/// <param name="networkInfo">Network prefab</param>
		/// <param name="targetInfo">Target prop/tree prefab</param>
		/// <param name="laneIndex">Lane number</param>
		/// <param name="propIndex">Prop index number</param>
		/// <returns>Currently-applied individual network replacement (null if none)</returns>
		internal override BOBNetReplacement EligibileReplacement(NetInfo netInfo, PrefabInfo targetInfo, int laneIndex, int propIndex) => ReplacementList(netInfo)?.Find(x => x.targetInfo == targetInfo);


		/// <summary>
		/// Retuns the list of active prop references for the given replacement value(s).
		/// </summary>
		/// <param name="netInfo">Targeted network prefab</param>
		/// <param name="targetInfo">Targeted (original) prop prefab</param>
		/// <param name="laneIndex">Targeted lane index (in parent network)</param>
		/// <param name="propIndex">Targeted prop index (in lanme)</param>
		/// <returns>List of active prop references for the given replacment values (null if none)</returns>
		internal override List<NetPropReference> ReferenceList(NetInfo netInfo, PrefabInfo targetInfo, int laneIndex, int propIndex)
        {
			// See if we've got any active references for this net prefab.
			if (propReferences.TryGetValue(netInfo, out Dictionary<PrefabInfo, List<NetPropReference>> referenceDict))
			{
				// See if we've got any active references for this target prefab.
				if (referenceDict.TryGetValue(targetInfo, out List<NetPropReference> referenceList))
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
			foreach(NetInfo netPrefab in propReferences.Keys)
            {
				foreach (PrefabInfo targetPrefab in propReferences[netPrefab].Keys)
				{
					RevertReferences(targetPrefab, propReferences[netPrefab][targetPrefab]);
				}
            }

			// Clear configuration file.
			ReplacementList(null).Clear();

			// Clear the active replacements dictionary.
			propReferences.Clear();
		}


		/// <summary>
		/// Checks if there's a currently active replacement applied to the given network, lane and prop index, and if so, returns the replacement record.
		/// </summary>
		/// <param name="netInfo">Net prefab to check</param>
		/// <param name="laneIndex">Lane index to check</param>
		/// <param name="propIndex">Prop index to check</param>
		/// <returns>Replacement record if a replacement is currently applied, null if no replacement is currently applied</returns>
		internal override BOBNetReplacement ActiveReplacement(NetInfo netInfo, int laneIndex, int propIndex)
		{
			// See if we've got any active references for this net prefab.
			if (!propReferences.TryGetValue(netInfo, out Dictionary<PrefabInfo, List<NetPropReference>> referenceDict))
			{
				return null;
			}

			// Iterate through entry for each prefab under this network.
			foreach (KeyValuePair<PrefabInfo, List<NetPropReference>> key in referenceDict)
			{
				// Iterate through each entry in list.
				foreach (NetPropReference propRef in key.Value)
				{
					// Check for a a network(due to all- replacement), lane and prop index match.
					if (propRef.netInfo == netInfo && propRef.laneIndex == laneIndex && propRef.propIndex == propIndex)
					{
						// Match!  Find and return the replacement record.
						return EligibileReplacement(netInfo, key.Key, propRef.laneIndex, propRef.propIndex);
					}
				}
			}

			// If we got here, no entry was found - return null to indicate no active replacement.
			return null;
		}


		/// <summary>
		/// Deserialises a network element list.
		/// </summary>
		/// <param name="elementList">Element list to deserialise</param>
		internal void Deserialize(List<BOBNetReplacement> elementList) => Deserialize(null, elementList);


		/// <summary>
		/// Applies an all-building prop replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to apply</param>
		protected override void ApplyReplacement(BOBNetReplacement replacement)
		{
			// Don't do anything if prefabs can't be found.
			if (replacement?.targetInfo == null || replacement.replacementInfo == null)
			{
				return;
			}

			// Create new reference list.
			List<NetPropReference> referenceList = new List<NetPropReference>();

			// Iterate through each loaded network and record props to be replaced.
			for (int i = 0; i < PrefabCollection<NetInfo>.LoadedCount(); ++i)
			{
				// Get local reference.
				NetInfo netInfo = PrefabCollection<NetInfo>.GetLoaded((uint)i);

				// Skip any null networks, or netorks without lanes.
				if (netInfo?.m_lanes == null)
				{
					continue;
				}

				// Iterate through each lane.
				for (int laneIndex = 0; laneIndex < netInfo.m_lanes.Length; ++laneIndex)
				{
					// Local reference.
					NetLaneProps.Prop[] theseLaneProps = netInfo.m_lanes[laneIndex]?.m_laneProps?.m_props;

					// If no props in this lane, skip it and go to the next one.
					if (theseLaneProps == null)
					{
						continue;
					}

					// Iterate through each prop in lane.
					for (int propIndex = 0; propIndex < theseLaneProps.Length; ++propIndex)
					{
						// Local reference.
						NetLaneProps.Prop thisLaneProp = theseLaneProps[propIndex];

						// If invalid entry, skip this one.
						if (thisLaneProp == null)
						{
							continue;
						}

						// Check for any currently active network or individual replacement.
						if (NetworkReplacement.Instance.ActiveReplacement(netInfo, laneIndex, propIndex) != null || IndividualNetworkReplacement.Instance.ActiveReplacement(netInfo, laneIndex, propIndex) != null)
						{
							// Active network or individual replacement; skip this one.
							continue;
						}

						// Check for any existing pack replacement.
						PrefabInfo thisProp = NetworkPackReplacement.Instance.ActiveReplacement(netInfo, laneIndex, propIndex)?.targetInfo;
						if (thisProp == null)
						{
							// No active replacement; use current PropInfo.
							if (replacement.isTree)
							{
								thisProp = thisLaneProp.m_finalTree;
							}
							else
							{
								thisProp = thisLaneProp.m_finalProp;
							}
						}

						// See if this prop matches our replacement.
						if (thisProp != null && thisProp == replacement.targetInfo)
						{
							// Match!  Add reference data to the list.
							referenceList.Add(CreateReference(netInfo, thisProp, laneIndex, propIndex, replacement.isTree));
						}
					}
				}
			}

			// Now, iterate through each entry found and apply the replacement to each one.
			foreach (NetPropReference propReference in referenceList)
			{
				// Remove any pack replacements first.
				NetworkPackReplacement.Instance.RemoveEntry(propReference.netInfo, replacement.targetInfo, propReference.laneIndex, propReference.propIndex);

				// Add entry to dictionary.
				AddReference(replacement, propReference);

				// Apply the replacement.
				ReplaceProp(replacement, propReference);
			}
		}


		/// <summary>
		/// Restores any replacements from lower-priority replacements after a reversion.
		/// </summary>
		/// <param name="netInfo">Network prefab</param>
		/// <param name="targetInfo">Target prop info</param>
		/// <param name="laneIndex">Lane index</param>
		/// <param name="propIndex">Prop index</param>
		protected override void RestoreLower(NetInfo netInfo, PrefabInfo targetInfo, int laneIndex, int propIndex) => NetworkPackReplacement.Instance.Restore(netInfo, targetInfo, laneIndex, propIndex);


		/// <summary>
		/// Gets the relevant replacement list entry from the active configuration file, if any.
		/// </summary>
		/// <param name="netInfo">Network prefab</param>
		/// <returns>Replacement list for the specified network prefab (null if none)</returns>
		protected override List<BOBNetReplacement> ReplacementList(NetInfo netInfo) => ConfigurationUtils.CurrentConfig.allNetworkProps;


		/// <summary>
		/// Gets the relevant building replacement list entry from the active configuration file, creating a new building entry if none already exists.
		/// </summary>
		/// <param name="netInfo">Network prefab</param>
		/// <returns>Replacement list for the specified building prefab</returns>
		protected override List<BOBNetReplacement> ReplacementEntry(NetInfo netInfo) => ReplacementList(netInfo);

		/// <summary>
		/// Reverts a replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to revert</param>
		/// <param name="removeEntries">True to remove the reverted entries from the list of replacements, false to leave the list unchanged</param>
		/// <returns>Always false (all-network entries never remove parent network elements)</returns>
		protected override bool Revert(BOBNetReplacement replacement, bool removeEntries)
		{
			// Safety check for calls without any current replacement.
			if (replacement?.targetInfo == null)
			{
				return false;
			}

			// List of prefabs where references need to be removed.
			List<NetInfo> removeList = new List<NetInfo>();

			// Iterate through each entry in prop reference dictionary.
			foreach (KeyValuePair<NetInfo, Dictionary<PrefabInfo, List<NetPropReference>>> keyPair in propReferences)
			{
				// Attempt to get a replacement list for this entry.
				if (keyPair.Value.TryGetValue(replacement.targetInfo, out List<NetPropReference> referenceList))
				{
					// Got a list - revert all entries.
					RevertReferences(replacement.targetInfo, referenceList);

					// Add dictionary to list to be cleared.
					removeList.Add(keyPair.Key);
				}
			}

			// Remove references from dictionary.
			foreach (NetInfo removeEntry in removeList)
			{
				if (propReferences.ContainsKey(removeEntry))
				{
					if (propReferences[removeEntry].ContainsKey(replacement.targetInfo))
					{
						// Remove target info entries from this network entry.
						propReferences[removeEntry].Remove(replacement.targetInfo);
					}

					// If no entries left for this network, remove entire network entry.
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
				ReplacementList(replacement.netInfo).Remove(replacement);
			}

			// If we got here, we didn't remove any network entries from the list; return false.
			return false;
		}


		/// <summary>
		/// Adds the given prop reference to the record for the given replacement.
		/// </summary>
		/// <param name="replacement">Replacement reference</param>
		/// <param name="propReference">Pop reference to store</param>
		protected override void AddReference(BOBNetReplacement replacement, NetPropReference propReference)
		{
			// Check to see if we've got an entry for this target prefab in our dictionary, and if not, create one.
			if (!propReferences.ContainsKey(propReference.netInfo))
			{
				propReferences.Add(propReference.netInfo, new Dictionary<PrefabInfo, List<NetPropReference>>());
			}

			// Check to see if we've got an entry for this network prefab in our dictionary entry for this target prefab, and if not, create one.
			if (!propReferences[propReference.netInfo].ContainsKey(replacement.targetInfo))
			{
				propReferences[propReference.netInfo].Add(replacement.targetInfo, new List<NetPropReference>());
			}

			// Add this prop reference to the dictioanry.
			propReferences[propReference.netInfo][replacement.targetInfo].Add(propReference);
		}
	}
}
