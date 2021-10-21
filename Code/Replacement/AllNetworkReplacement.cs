using System.Collections.Generic;
using System.Linq;


namespace BOB
{
	/// <summary>
	/// Class to manage all-network prop and tree replacements.
	/// </summary>
	internal class AllNetworkReplacement : NetworkReplacementBase
	{
		// Instance reference.
		internal static AllNetworkReplacement instance;

		// Master dictionary of replaced prop references.
		private Dictionary<PrefabInfo, BOBNetReplacement> replacements;


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
		internal BOBNetReplacement Replacement(PrefabInfo target)
		{
			if (replacements.TryGetValue(target, out BOBNetReplacement replacementEntry))
			{
				return replacementEntry;
			}

			// If we got here, something went wrong.
			Logging.Error("no all-network replacement entry for target ", target?.name ?? "null");
			return null;
		}


		/// <summary>
		/// Applies a new (or updated) all-network replacement.
		/// </summary>
		/// <param name="network">Targeted network</param>
		/// <param name="target">Targeted (original) prop prefab</param>
		/// <param name="replacement">Replacment prop prefab</param>
		/// <param name="targetLane">Targeted lane index (in parent network)</param>
		/// <param name="targetIndex">Prop index to apply replacement to</param>
		/// <param name="angle">Replacment prop angle adjustment</param>
		/// <param name="offsetX">Replacment X position offset</param>
		/// <param name="offsetY">Replacment Y position offset</param>
		/// <param name="offsetZ">Replacment Z position offset</param>
		/// <param name="probability">Replacement probability</param>
		internal override void Apply(NetInfo _, PrefabInfo target, PrefabInfo replacement, int lane, int targetIndex, float angle, float offsetX, float offsetY, float offsetZ, int probability)
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
				replacements.Add(target, new BOBNetReplacement());
			}

			// Add/replace dictionary replacement data.
			replacements[target].references = new List<NetPropReference>();
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
			replacements[target].Replacement = replacement.name;

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
							if (target is PropInfo)
							{
								thisProp = netInfo.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_finalProp;
							}
							else
							{
								thisProp = netInfo.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_finalTree;
							}
						}

						// See if this prop matches our replacement.
						if (thisProp != null && thisProp == target)
						{
							// Match!  Add reference data to the list.
							replacements[target].references.Add(new NetPropReference
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
			foreach (NetPropReference propReference in replacements[target].references)
			{
				// Reset any pack replacements first.
				NetworkPackReplacement.instance.RemoveEntry(propReference.network, target, propReference.laneIndex, propReference.propIndex);

				ReplaceProp(replacements[target], propReference);
			}
		}


		/// <summary>
		/// Reverts all active all-network replacements and re-initialises the master dictionary.
		/// </summary>
		internal override void RevertAll()
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
		/// Reverts an all-network replacement.
		/// </summary>
		/// <param name="target">Targeted (original) tree/prop prefab</param>
		/// <param name="removeEntries">True (default) to remove the reverted entries from the master dictionary, false to leave the dictionary unchanged</param>
		/// <returns>True if the entire network record was removed from the dictionary (due to no remaining replacements for that prefab), false if the prefab remains in the dictionary (has other active replacements)</returns>
		internal void Revert(PrefabInfo target, bool removeEntries = true)
		{
			// Iterate through each entry in our dictionary.
			foreach (NetPropReference propReference in replacements[target].references)
			{
				// Revert entry.
				if (target is PropInfo propTarget)
				{
					propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_finalProp = propTarget;
				}
				else
                {
					propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_finalTree = (TreeInfo)target;
				}
				propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_angle = propReference.angle;
				propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_position = propReference.position;
				propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_probability = propReference.probability;

				// Add network to dirty list.
				NetData.DirtyList.Add(propReference.network);

				// Restore any pack replacement.
				NetworkPackReplacement.instance.Restore(propReference.network, target, propReference.laneIndex, propReference.propIndex);
			}

			// Remove entry from dictionary, if we're doing so.
			if (removeEntries)
            {
				replacements.Remove(target);
			}
		}


		/// <summary>
		/// Removes an entry from the master dictionary of all-network replacements currently applied to networks.
		/// </summary>
		/// <param name="netPrefab">Network prefab</param>
		/// <param name="target">Target prop info</param>
		/// <param name="laneIndex">Lane index</param>
		/// <param name="propIndex">Prop index</param>
		internal void RemoveEntry(NetInfo netPrefab, PrefabInfo target, int laneIndex, int propIndex)
		{
			// Check to see if we have an entry for this prefab.
			if (replacements.ContainsKey(target))
			{
				// Yes - iterate through each recorded prop reference.
				for (int i = 0; i < replacements[target].references.Count; ++i)
				{
					// Look for a network, lane and index match.
					NetPropReference propReference = replacements[target].references[i];
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

						// Remove this reference and return.
						replacements[target].references.Remove(replacements[target].references[i]);
						return;
                    }
                }
			}
		}


		/// <summary>
		/// Checks if there's a currently active all-network replacement applied to the given network prop index, and if so, returns the *original* prefab..
		/// </summary>
		/// <param name="netPrefab">Network prefab to check</param>
		/// <param name="laneIndex">Lane index to check</param>
		/// <param name="propIndex">Prop index to check</param>
		/// <returns>Replacement record if an all-network replacement is currently applied, null if no all-network replacement is currently applied</returns>
		internal PrefabInfo GetOriginal(NetInfo netPrefab, int laneIndex, int propIndex)
		{
			// Iterate through each entry in master dictionary.
			foreach (PrefabInfo target in replacements.Keys)
			{
				BOBNetReplacement reference = replacements[target];
				// Iterate through each network in this entry.
				foreach (NetPropReference propRef in reference.references)
				{
					// Check for a network, lane, and prop index match.
					if (propRef.network == netPrefab && propRef.laneIndex == laneIndex && propRef.propIndex == propIndex)
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
		/// Checks if there's a currently active all-network replacement applied to the given network prop index, and if so, returns the replacement record.
		/// </summary>
		/// <param name="netPrefab">Network prefab to check</param>
		/// <param name="laneIndex">Lane index to check</param>
		/// <param name="propIndex">Prop index to check</param>
		/// <returns>Replacement record if a all-network replacement is currently applied, null if no all-network replacement is currently applied</returns>
		internal override BOBNetReplacement ActiveReplacement(NetInfo netPrefab, int laneIndex, int propIndex)
		{
			// Iterate through each entry in master dictionary.
			foreach (PrefabInfo target in replacements.Keys)
			{
				BOBNetReplacement reference = replacements[target];
				// Iterate through each network in this entry.
				foreach (NetPropReference propRef in reference.references)
				{
					// Check for a network, lane, and prop index match.
					if (propRef.network == netPrefab && propRef.laneIndex == laneIndex && propRef.propIndex == propIndex)
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
		/// Restores a all-network replacement, if any (e.g. after a network replacement has been reverted).
		/// </summary>
		/// <param name="netPrefab">Network prefab</param>
		/// <param name="target">Target prop info</param>
		/// <param name="laneIndex">Lane index</param>
		/// <param name="propIndex">Prop index</param>
		/// <returns>True if a restoration was made, false otherwise</returns>
		internal bool Restore(NetInfo netPrefab, PrefabInfo target, int laneIndex, int propIndex)
		{
			// Check to see if we have an entry for this prefab.
			if (replacements.ContainsKey(target))
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

				replacements[target].references.Add(newReference);

				// Apply replacement and return true to indicate restoration.
				ReplaceProp(replacements[target], newReference);
				return true;
			}

			// If we got here, no restoration was made.
			return false;
		}


		/// <summary>
		/// Serializes network replacement dictionary to XML format.
		/// </summary>
		/// <returns>List of network replacement entries in XML Format</returns>
		internal List<BOBNetReplacement> Serialize() => replacements.Values.ToList();


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
				PrefabInfo targetPrefab = replacement.tree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacement.target) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacement.target);
				if (targetPrefab == null)
				{
					Logging.Message("Couldn't find target prefab ", replacement.target);
					continue;
				}

				// Try to find replacement prefab.
				PrefabInfo replacementPrefab = ConfigurationUtils.FindReplacementPrefab(replacement.Replacement, replacement.tree);
				if (replacementPrefab == null)
				{
					Logging.Message("Couldn't find replacement prefab ", replacement.Replacement);
					continue;
				}

				// If we got here, it's all good; apply the all-network replacement.
				Apply(null, targetPrefab, replacementPrefab, replacement.lane, replacement.index, replacement.angle, replacement.offsetX, replacement.offsetY, replacement.offsetZ, replacement.probability);
			}
		}


		/// <summary>
		/// Performs setup and initialises the master dictionary.  Must be called prior to use.
		/// </summary>
		protected override void Setup()
		{
			replacements = new Dictionary<PrefabInfo, BOBNetReplacement>();
		}
	}
}
