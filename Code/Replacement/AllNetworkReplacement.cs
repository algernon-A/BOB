using System.Collections.Generic;


namespace BOB
{
	/// <summary>
	/// Class to manage all-network prop and tree replacements.
	/// </summary>
	internal class AllNetworkReplacement : NetworkReplacementBase
	{
		// Instance reference.
		internal static AllNetworkReplacement instance;


		/// <summary>
		/// Constructor - initializes instance reference.
		/// </summary>
		internal AllNetworkReplacement()
		{
			instance = this;
		}


		/// <summary>
		/// Retrieves a currently-applied network replacement entry for the given target prefab.
		/// </summary>
		/// <param name="target">Target prop/tree prefab</param>
		/// <returns>Currently-applied network replacement (null if none)</returns>
		internal BOBNetReplacement Replacement(PrefabInfo target) => ConfigurationUtils.CurrentConfig.allNetworkProps.Find(x => x.target.Equals(target.name));


		/// <summary>
		/// Applies a new (or updated) all-network replacement.
		/// </summary>
		/// <param name="network">Targeted network (ignored)</param>
		/// <param name="target">Targeted (original) prop prefab</param>
		/// <param name="replacement">Replacment prop prefab</param>
		/// <param name="targetLane">Targeted lane index (in parent network)</param>
		/// <param name="targetIndex">Prop index to apply replacement to</param>
		/// <param name="angle">Replacment prop angle adjustment</param>
		/// <param name="offsetX">Replacment X position offset</param>
		/// <param name="offsetY">Replacment Y position offset</param>
		/// <param name="offsetZ">Replacment Z position offset</param>
		/// <param name="probability">Replacement probability</param>
		internal override void Replace(NetInfo network, PrefabInfo target, PrefabInfo replacement, int targetLane, int targetIndex, float angle, float offsetX, float offsetY, float offsetZ, int probability)
		{
			// Make sure that target and replacement are valid and the same type before doing anything.
			if (target?.name == null || replacement?.name == null || (target is TreeInfo && !(replacement is TreeInfo)) || (target is PropInfo) && !(replacement is PropInfo))
			{
				return;
			}

			// Revert any current replacement entry for this prop.
			Revert(target.name, true);

			// Get current replacement after reversion above.
			BOBNetReplacement thisReplacement = CurrentReplacement(target.name);

			// Create new replacement list entry if none already exists.
			if (thisReplacement == null)
			{
				thisReplacement = new BOBNetReplacement
				{
					target = target.name,
					targetInfo = target
				};
				ConfigurationUtils.CurrentConfig.allNetworkProps.Add(thisReplacement);
			}

			// Add/replace list replacement data.
			thisReplacement.tree = target is TreeInfo;
			thisReplacement.angle = angle;
			thisReplacement.offsetX = offsetX;
			thisReplacement.offsetY = offsetY;
			thisReplacement.offsetZ = offsetZ;
			thisReplacement.probability = probability;

			// Record replacement prop.
			thisReplacement.replacementInfo = replacement;
			thisReplacement.Replacement = replacement.name;

			// Apply replacement.
			ApplyReplacement(thisReplacement);
		}


		/// <summary>
		/// Reverts all active all-network replacements and re-initialises the master dictionary.
		/// </summary>
		internal override void RevertAll()
		{
			// Iterate through each entry in the replacement list.
			foreach (BOBNetReplacement replacement in ConfigurationUtils.CurrentConfig.allNetworkProps)
			{
				// Revert this replacement (but don't remove the entry, as the dictionary is currently immutable while we're iterating through it).
				Revert(replacement, false);
			}

			// Re-initialise the dictionaries.
			Setup();
		}


		/// <summary>
		/// Reverts an all-network replacement.
		/// </summary>
		/// <param name="targetName">Targeted (original) tree/prop prefab name</param>
		/// <param name="removeEntries">True (default) to remove the reverted entries from the list of replacements, false to leave the list unchanged</param>
		/// <returns>True if the entire network record was removed from the dictionary (due to no remaining replacements for that prefab), false if the prefab remains in the dictionary (has other active replacements)</returns>
		internal void Revert(string targetName, bool removeEntries) => Revert(CurrentReplacement(targetName), removeEntries);


		/// <summary>
		/// Removes an entry from the list of all-network replacements currently applied to networks.
		/// </summary>
		/// <param name="netPrefab">Network prefab</param>
		/// <param name="target">Target prop info</param>
		/// <param name="laneIndex">Lane index</param>
		/// <param name="propIndex">Prop index</param>
		internal void RemoveEntry(NetInfo netPrefab, PrefabInfo target, int laneIndex, int propIndex)
		{
			// Check to see if we have an entry for this prefab.
			BOBNetReplacement thisReplacement = CurrentReplacement(target.name);
			if (thisReplacement != null)
			{
				// Iterate through each recorded prop reference.
				foreach (NetPropReference propReference in thisReplacement.references)
				{
					// Look for a network, lane and index match.
					if (propReference.network == netPrefab && propReference.laneIndex == laneIndex && propReference.propIndex == propIndex)
					{
						// Got a match!  Revert instance.
						if (target is PropInfo propTarget)
						{
							propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_finalProp = propTarget;
						}
						else
						{
							propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_finalTree = (TreeInfo)target;
						}
						netPrefab.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_angle = propReference.angle;
						netPrefab.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_position = propReference.position;
						netPrefab.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_probability = propReference.probability;

						// Stop iterating - we're done here.
						break;
					}
				}
			}
		}


		/// <summary>
		/// Checks if there's a currently active all-network replacement applied to the given network and lane/prop indexes, and if so, returns the *original* prefab..
		/// </summary>
		/// <param name="netPrefab">Network prefab to check</param>
		/// <param name="laneIndex">Lane index to check</param>
		/// <param name="propIndex">Prop index to check</param>
		/// <returns>Replacement record if an all-network replacement is currently applied, null if no all-network replacement is currently applied</returns>
		internal PrefabInfo GetOriginal(NetInfo netPrefab, int laneIndex, int propIndex)
		{
			// Iterate through each network replacment record in the current config.
			foreach (BOBNetReplacement netReplacement in ConfigurationUtils.CurrentConfig.allNetworkProps)
			{
				if (netReplacement.references != null)
				{
					// Iterate through each recorded replacement in this entry.
					foreach (NetPropReference propRef in netReplacement.references)
					{
						// Check for a network, lane, and prop index match.
						if (propRef.network == netPrefab && propRef.laneIndex == laneIndex && propRef.propIndex == propIndex)
						{
							// Match!  Return the original prefab.
							return netReplacement.targetInfo;
						}
					}
				}
			}

			// If we got here, no entry was found - return null to indicate no active replacement.
			return null;
		}


		/// <summary>
		/// Checks if there's a currently active all-network replacement applied to the given network and lane and prop indexes, and if so, returns the replacement record.
		/// </summary>
		/// <param name="netPrefab">Network prefab to check</param>
		/// <param name="laneIndex">Lane index to check</param>
		/// <param name="propIndex">Prop index to check</param>
		/// <returns>Replacement record if a all-network replacement is currently applied, null if no all-network replacement is currently applied</returns>
		internal override BOBNetReplacement ActiveReplacement(NetInfo netPrefab, int laneIndex, int propIndex)
		{
			// Iterate through each network replacment record in the current config.
			foreach (BOBNetReplacement netReplacement in ConfigurationUtils.CurrentConfig.allNetworkProps)
			{
				// Iterate through each network in this entry.
				foreach (NetPropReference propRef in netReplacement.references)
				{
					// Check for a network, lane, and prop index match.
					if (propRef.network == netPrefab && propRef.laneIndex == laneIndex && propRef.propIndex == propIndex)
					{
						// Match!  Return the replacement record.
						return netReplacement;
					}
				}
			}

			// If we got here, no entry was found - return null to indicate no active replacement.
			return null;
		}


		/// <summary>
		/// Restores an all-network replacement, if any (e.g. after a network replacement has been reverted).
		/// </summary>
		/// <param name="netPrefab">Network prefab</param>
		/// <param name="target">Target prop info</param>
		/// <param name="laneIndex">Lane index</param>
		/// <param name="propIndex">Prop index</param>
		/// <returns>True if a restoration was made, false otherwise</returns>
		internal bool Restore(NetInfo netPrefab, PrefabInfo target, int laneIndex, int propIndex)
		{
			// Check to see if we have an entry for this prefab.
			BOBNetReplacement thisReplacement = CurrentReplacement(target.name);
			if (thisReplacement != null)
			{
				// Yes - add reference data to the list.
				NetPropReference newReference = new NetPropReference
				{
					network = netPrefab,
					laneIndex = laneIndex,
					propIndex = propIndex,
					angle = netPrefab.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_angle,
					position = netPrefab.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_position,
					probability = netPrefab.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_probability
				};
				thisReplacement.references.Add(newReference);

				// Apply replacement and return true to indicate restoration.
				ReplaceProp(thisReplacement, newReference);
				return true;
			}

			// If we got here, no restoration was made.
			return false;
		}


		/// <summary>
		/// Deserialises an all-network element list.
		/// </summary>
		/// <param name="elementList">All-network element list to deserialise</param>
		internal void Deserialize(List<BOBNetReplacement> elementList)
		{
			// Iterate through each element in the provided list.
			foreach (BOBNetReplacement replacement in elementList)
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
		protected void ApplyReplacement(BOBNetReplacement replacement)
		{
			// Don't do anything if prefabs can't be found.
			if (replacement?.targetInfo == null || replacement.replacementInfo == null)
			{
				return;
			}

			// (Re)set replacement list.
			replacement.references = new List<NetPropReference>();

			// Iterate through each loaded network and record props to be replaced.
			for (int i = 0; i < PrefabCollection<NetInfo>.LoadedCount(); ++i)
			{
				// Get local reference.
				NetInfo netInfo = PrefabCollection<NetInfo>.GetLoaded((uint)i);

				// Skip any netorks without lanes.
				if (netInfo.m_lanes == null)
				{
					continue;
				}

				// Iterate through each lane.
				for (int laneIndex = 0; laneIndex < netInfo.m_lanes.Length; ++laneIndex)
				{
					// If no props in this lane, skip it and go to the next one.
					if (netInfo.m_lanes[laneIndex].m_laneProps?.m_props == null)
					{
						continue;
					}

					// Iterate through each prop in lane.
					for (int propIndex = 0; propIndex < netInfo.m_lanes[laneIndex].m_laneProps.m_props.Length; ++propIndex)
					{
						// Check for any currently active network replacement.
						if (NetworkReplacement.instance.GetOriginal(netInfo, laneIndex, propIndex) != null)
						{
							// Active network replacement; skip this one.
							continue;
						}

						// Check for any existing pack replacement.
						PrefabInfo thisProp = NetworkPackReplacement.instance.GetOriginal(netInfo, laneIndex, propIndex);
						if (thisProp == null)
						{
							// No active replacement; use current PropInfo.
							if (replacement.tree)
							{
								thisProp = netInfo.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_finalTree;
							}
							else
							{
								thisProp = netInfo.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_finalProp;
							}
						}

						// See if this prop matches our replacement.
						if (thisProp != null && thisProp == replacement.targetInfo)
						{
							// Match!  Add reference data to the list.
							replacement.references.Add(new NetPropReference
							{
								network = netInfo,
								laneIndex = laneIndex,
								propIndex = propIndex,
								angle = netInfo.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_angle,
								position = netInfo.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_position,
								probability = netInfo.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_probability
							});
						}
					}
				}
			}

			// Now, iterate through each entry found and apply the replacement to each one.
			foreach (NetPropReference propReference in replacement.references)
			{
				// Reset any pack replacements first.
				NetworkPackReplacement.instance.RemoveEntry(propReference.network, replacement.targetInfo, propReference.laneIndex, propReference.propIndex);

				ReplaceProp(replacement, propReference);
			}
		}


		/// <summary>
		/// Returns the current replacement record for the given target prefab record.
		/// </summary>
		/// <param name="targetName">Target all-building prefab name</param>
		/// <returns>Current replacement record (null if none)</returns>
		private BOBNetReplacement CurrentReplacement(string targetName) => ConfigurationUtils.CurrentConfig.allNetworkProps.Find(x => x.target.Equals(targetName));


		/// <summary>
		/// Reverts an all-network replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to revert</param>
		/// <param name="removeEntries">True (default) to remove the reverted entries from the list of replacements, false to leave the list unchanged</param>
		/// <returns>True if the entire network record was removed from the dictionary (due to no remaining replacements for that prefab), false if the prefab remains in the dictionary (has other active replacements)</returns>
		private void Revert(BOBNetReplacement replacement, bool removeEntries)
		{
			// Safety check for calls without any current replacement.
			if (replacement == null)
			{
				return;
			}

			if (replacement.references != null)
			{
				// Iterate through each entry in our list.
				foreach (NetPropReference propReference in replacement.references)
				{
					// Revert entry.
					if (replacement.tree)
					{
						// Tree.
						propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_finalTree = replacement.TargetTree;
					}
					else
					{
						// Prop.
						propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_finalProp = replacement.TargetProp;
					}
					propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_angle = propReference.angle;
					propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_position = propReference.position;
					propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_probability = propReference.probability;

					// Add network to dirty list.
					NetData.DirtyList.Add(propReference.network);

					// Restore any pack replacement.
					NetworkPackReplacement.instance.Restore(propReference.network, replacement.targetInfo, propReference.laneIndex, propReference.propIndex);
				}
			}

			// Remove entry from dictionary, if we're doing so.
			if (removeEntries)
			{
				ConfigurationUtils.CurrentConfig.allNetworkProps.Remove(replacement);
			}
		}
	}
}
