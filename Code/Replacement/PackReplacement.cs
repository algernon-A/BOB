using System.Linq;
using System.Collections.Generic;


namespace BOB
{
	/// <summary>
	/// Static class to manage replacement packs.
	/// </summary>
	internal class PackReplacement
	{
		// Instance reference.
		internal static PackReplacement instance;

		// Master dictionary of prop pack replacements.
		private Dictionary<string, Dictionary<PrefabInfo, PropReplacement>> packRecords;

		// Master dictionary of replaced prop references.
		internal Dictionary<PrefabInfo, BOBNetReplacement> replacements;

		// Pack status dictionaries.
		private Dictionary<string, bool> packEnabled;
		private Dictionary<string, bool> packNotAllLoaded;


		/// <summary>
		/// Constructor - initializes instance reference and calls initial setup.
		/// </summary>
		internal PackReplacement()
		{
			instance = this;
			Setup();
		}


		/// <summary>
		/// Returns the current status of the named replacement pack.
		/// </summary>
		/// <param name="packName">Replacement pack name</param>
		/// <returns>True if enabled, false otherwise</returns>
		internal bool GetPackStatus(string packName)
        {
			if (packEnabled.ContainsKey(packName))
            {
				return packEnabled[packName];
            }

			return false;
        }


		/// <summary>
		/// Sets the status of the named replacement pack.
		/// </summary>
		/// <param name="packName">Replacement pack name</param>
		/// <param name="status">True to enable, false to disable</param>
		internal void SetPackStatus (string packName, bool status)
        {
			// Only do stuff if there's an actual change.
			if (status != packEnabled[packName])
            {
				// Enabling or disabling?
				if (status == true)
                {
					// Enable the pack; leave packStatus as false if application wasn't successful.
					packEnabled[packName] = ApplyPack(packName);
                }
				else
				{
					// Disable the pack.
					RevertPack(packName);
					packEnabled[packName] = false;
				}
			}
        }


		/// <summary>
		/// Checks to see if all replacement props are currently loaded for the specified pack.
		/// </summary>
		/// <param name="packName">Replacement pack name</param>
		/// <returns>True if all replacement props are NOT loaded, false otherwise</returns>
		internal bool PackNotAllLoaded(string packName)
        {
			// Return dictionary entry, if we have one.
			if (packNotAllLoaded.ContainsKey(packName))
            {
				return packNotAllLoaded[packName];
            }

			// If we got here, no 'not all loaded' flag was found.
			return false;
        }


		/// <summary>
		/// Returns a list of currently installed packs as a FastList for display.
		/// </summary>
		/// <returns>FastList of installed prop packs</returns>
		internal FastList<object> GetPackFastList()
		{
			// Create return list from signPacks array.
			FastList<object> fastList = new FastList<object>()
			{
				m_buffer = packRecords.Keys.OrderBy(x => x).ToArray(),
				m_size = packRecords.Count()
			};
			return fastList;
		}


		/// <summary>
		/// Checks to see if the given replacement pack conflicts with an active pack.
		/// </summary>
		/// <param name="packName">Pack name to check</param>
		/// <returns>True if the pack conflicts with an active pack, false if no conflicts</returns>
		internal bool Conflicts(string packName)
		{
			// Check for conflicts - iterate through all prefabs in this pack.
			foreach (PrefabInfo prefab in packRecords[packName].Keys)
			{
				// Check for a currently applied replacement of the same prefab.
				if (replacements.ContainsKey(prefab))
				{
					// Found one!  Log message and return true to indicate conflict.
					Logging.Message("replacement pack conflict with ", packName, " for prefab ", prefab.name);
					return true;
				}
			}

			// If we got here, then no conflict was detected.
			return false;
		}


		/// <summary>
		/// Applies a replacement pack.
		/// </summary>
		/// <returns>True if the pack was successfully applied, false otherwise</returns>
		private bool ApplyPack(string packName)
		{
			// Check for valid value.
			if (!string.IsNullOrEmpty(packName) && packRecords.ContainsKey(packName))
			{
				// Check for conflicts with a currently applied replacement.
				if (Conflicts(packName))
				{
					// Conflict detected - do nothing and return false to indicate no application.
					return false;
				}

				Logging.Message("applying pack ", packName);

				// Iterate through each entry in pack and apply.
				foreach (KeyValuePair<PrefabInfo, PropReplacement> entry in packRecords[packName])
				{
					Apply(entry.Key, entry.Value.replacementInfo, entry.Value.rotation, entry.Value.xOffset, entry.Value.yOffset, entry.Value.zOffset, entry.Value.hide ? 0 : 100);
				}

				// Return true to indicate sucessful application.
				return true;
			}

			// If we got here, then application wasn't successful.
			return false;
		}


		/// <summary>
		/// Reverts a replacement pack.
		/// </summary>
		private void RevertPack(string packName)
		{
			// Check for valid value.
			if (!string.IsNullOrEmpty(packName) && packRecords.ContainsKey(packName))
			{
				// Iterate through each entry in pack and revert.
				foreach (KeyValuePair<PrefabInfo, PropReplacement> entry in packRecords[packName])
				{
					Revert(entry.Key);
				}
			}
		}


		/// <summary>
		/// Serializes the list of active replacement packs into a string list suitable for XML serialization.
		/// </summary>
		/// <returns>New string list of active replacement pack names</returns>
		internal List<string> SerializeActivePacks()
        {
			// Return list.
			List<string> activePacks = new List<string>();

			// Iterate through all pack settings.
			foreach (KeyValuePair<string, bool> entry in packEnabled)
            {
				// Look for enabled packs (value is true).
				if (entry.Value)
                {
					// Add to list.
					activePacks.Add(entry.Key);
                }
            }

			return activePacks;
		}


		/// <summary>
		/// Deserializes a list of active replacement packs into a list of active
		/// </summary>
		/// <param name="activePacks">List of pack names to deserialize</param>
		internal void DeserializeActivePacks(List<string> activePacks)
		{
			// Iterate through the list of active packs.
			foreach (string packName in activePacks)
			{
				// See if we currently have this pack loaded.
				if (packEnabled.ContainsKey(packName))
                {
					// Yes - activate it.
					SetPackStatus(packName, true);
                }
				else
                {
					Logging.Message("couldn't find replacement pack ", packName);
                }
			}
		}


		/// <summary>
		/// Performs setup, loads pack files, and initialises the dictionaries.  Must be called prior to use.
		/// </summary>
		internal void Setup()
		{
			// Initialise dictionaries.
			packRecords = new Dictionary<string, Dictionary<PrefabInfo, PropReplacement>>();
			replacements = new Dictionary<PrefabInfo, BOBNetReplacement>();
			packEnabled = new Dictionary<string, bool>();
			packNotAllLoaded = new Dictionary<string, bool>();

			// Read config files.
			List<BOBPackFile> packFiles = PackUtils.LoadPackFiles();

			foreach (BOBPackFile packFile in packFiles)
			{
				// Iterate through each prop pack loaded from the settings file.
				foreach (PropPack propPack in packFile.propPacks)
				{
					// Check to see if we already have a record for this pack.
					if (packRecords.ContainsKey(propPack.name))
					{
						// Yes - log the message and carry on.
						Logging.Message("duplicate record for replacement pack with name", propPack.name);
					}
					else
					{
						// No - add pack to our records.
						packRecords.Add(propPack.name, new Dictionary<PrefabInfo, PropReplacement>());
						packEnabled.Add(propPack.name, false);
					}

					// Iterate through each replacement in the pack.
					for (int i = 0; i < propPack.propReplacements.Count; ++i)
					{
						// Get reference.
						PropReplacement propReplacement = propPack.propReplacements[i];

						// Can we find both target and replacment?
						PrefabInfo targetInfo = propReplacement.isTree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(propReplacement.targetName) : PrefabCollection<PropInfo>.FindLoaded(propReplacement.targetName);
						propReplacement.replacementInfo = propReplacement.isTree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(propReplacement.replacementName) : PrefabCollection<PropInfo>.FindLoaded(propReplacement.replacementName);
						if (targetInfo == null)
						{
							// Target prop not found - log and continue.
							Logging.Message("couldn't find pack target prop");
						}
						else if (propReplacement.replacementInfo == null)
						{
							// Replacement prop not found - flag that pack wasn't all loaded.
							if (!packNotAllLoaded.ContainsKey(propPack.name))
                            {
								packNotAllLoaded.Add(propPack.name, true);
                            }
						}
						else
						{
							// Target and replacment both found - add this replacmeent to our pack dictionary entry.
							if (packRecords[propPack.name].ContainsKey(targetInfo))
							{
								// Skip any duplicates.
								Logging.Error("duplicate replacement ", targetInfo.name, " in replacement pack ", propPack.name);
							}
							else
							{
								packRecords[propPack.name].Add(targetInfo, propReplacement);
							}
						}
					}

					// Check to make sure we have at least one replacement; if not, remove the pack from our records.
					if (packRecords[propPack.name].Count == 0)
					{
						Logging.Message("replacement pack ", propPack.name, " has no valid replacements; removing from list");
						packRecords.Remove(propPack.name);
						packEnabled.Remove(propPack.name);
					}
				}
			}
		}


		/// <summary>
		/// Reverts all active pack replacements and re-initialises the master dictionaries.
		/// </summary>
		internal void RevertAll()
		{
			// Iterate through each entry in the master pack dictionary.
			foreach (string packName in packEnabled.Keys)
			{
				// Directly revert any applied packs (don't worry about properly processing dictionaries since we'll be wiping them anyway, and besides, if we try to change them while doing this it'll lead to out-of-sync errors).
				if (packEnabled[packName])
				{
					RevertPack(packName);
				}
			}

			// Re-initialise the dictionaries.
			Setup();
		}


		/// <summary>
		/// Reverts a pack replacement.
		/// </summary>
		/// <param name="target">Targeted (original) tree/prop prefab</param>
		/// <returns>True if the entire network record was removed from the dictionary (due to no remaining replacements for that prefab), false if the prefab remains in the dictionary (has other active replacements)</returns>
		private void Revert(PrefabInfo target)
		{
			// Don't revert if there's no entry for this reference.
			if (replacements.ContainsKey(target))
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
					propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_position = propReference.postion;
					propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_probability = propReference.probability;

					// Add network to dirty list.
					NetData.DirtyList.Add(propReference.network);
				}

				// Remove entry from dictionary.
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
						netPrefab.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_position = propReference.postion;
						netPrefab.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_probability = propReference.probability;

						// Remove this reference and return.
						replacements[target].references.Remove(replacements[target].references[i]);
						return;
					}
				}
			}
		}


		/// <summary>
		/// Applies a new (or updated) pack replacement; basically an all-network replacement.
		/// </summary>
		/// <param name="target">Targeted (original) prop prefab</param>
		/// <param name="replacement">Replacment prop prefab</param>
		/// <param name="angle">Replacment prop angle adjustment</param>
		/// <param name="offsetX">Replacment X position offset</param>
		/// <param name="offsetY">Replacment Y position offset</param>
		/// <param name="offsetZ">Replacment Z position offset</param>
		/// <param name="probability">Replacement probability</param>
		private void Apply(PrefabInfo target, PrefabInfo replacement, float angle, float offsetX, float offsetY, float offsetZ, int probability)
		{
			// Make sure that target and replacement are the same type before doing anything.
			if (target == null || replacement == null || (target is TreeInfo && !(replacement is TreeInfo)) || (target is PropInfo) && !(replacement is PropInfo))
			{
				return;
			}

			Logging.Message("applying pack replacement for ", target.name);

			// Check to see if we already have a replacement entry for this prop - if so, revert the replacement first.
			if (replacements.ContainsKey(target))
			{
				Revert(target);
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
			replacements[target].replacement = replacement.name;

			// Iterate through each loaded network and record props to be replaced.
			for (int i = 0; i < PrefabCollection<NetInfo>.LoadedCount(); ++i)
			{
				// Get local reference.
				NetInfo network = PrefabCollection<NetInfo>.GetLoaded((uint)i);

				// Skip any netorks without lanes.
				if (network.m_lanes == null)
				{
					continue;
				}

				// Iterate through each lane.
				for (int laneIndex = 0; laneIndex < network.m_lanes.Length; ++laneIndex)
				{
					// If no props in this lane, skip it and go to the next one.
					if (network.m_lanes[laneIndex].m_laneProps?.m_props == null)
					{
						continue;
					}

					// Iterate through each prop in lane.
					for (int propIndex = 0; propIndex < network.m_lanes[laneIndex].m_laneProps.m_props.Length; ++propIndex)
					{
						// Check for any currently active conflicting replacements.
						if (NetworkReplacement.instance.GetOriginal(network, laneIndex, propIndex) != null)
						{
							// Active network replacement; skip this one.
							continue;
						}
						else if (AllNetworkReplacement.instance.GetOriginal(network, laneIndex, propIndex) != null)
						{
							// Active all-network replacement; skip this one.
							continue;
						}
						else
						{
							// Active pack replacement; skip this one.
							PrefabInfo original = GetOriginal(network, laneIndex, propIndex);

							if (original != null)
							{
								continue;
							}
						}

						// Get this prop from network.
						PrefabInfo thisProp = target is PropInfo ? (PrefabInfo)network.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_finalProp : (PrefabInfo)network.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_finalTree;

						// See if this prop matches our replacement.
						if (thisProp != null && thisProp == target)
						{
							// Match!  Add reference data to the list.
							replacements[target].references.Add(new NetPropReference
							{
								network = network,
								laneIndex = laneIndex,
								propIndex = propIndex,
								angle = network.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_angle,
								postion = network.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_position,
								probability = network.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_probability
							});
						}
					}
				}
			}

			// Now, iterate through each entry found and apply the replacement to each one.
			foreach (NetPropReference propReference in replacements[target].references)
			{
				NetworkReplacement.instance.ReplaceProp(replacements[target], propReference);
			}
		}


		/// <summary>
		/// Checks if there's a currently active pack replacement applied to the given network prop index, and if so, returns the *original* prefab..
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
		/// Checks if there's a currently active pack replacement applied to the given network prop index, and if so, returns the replacement record.
		/// </summary>
		/// <param name="netPrefab">Network prefab to check</param>
		/// <param name="laneIndex">Lane index to check</param>
		/// <param name="propIndex">Prop index to check</param>
		/// <returns>Replacement record if a all-network replacement is currently applied, null if no all-network replacement is currently applied</returns>
		internal BOBNetReplacement ActiveReplacement(NetInfo netPrefab, int laneIndex, int propIndex)
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
		/// Restores a pack replacement, if any (e.g. after a network replacement has been reverted).
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
					postion = netPrefab.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_position
				};

				replacements[target].references.Add(newReference);

				// Apply replacement and return true to indicate restoration.
				NetworkReplacement.instance.ReplaceProp(replacements[target], newReference);
				return true;
			}

			// If we got here, no restoration was made.
			return false;
		}
	}
}