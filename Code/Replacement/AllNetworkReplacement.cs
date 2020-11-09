using System.Collections.Generic;
using UnityEngine;


namespace BOB
{
	/// <summary>
	/// Static class to manage all-building prop and tree replacements.
	/// </summary>
	internal static class AllNetworkReplacement
	{
		// Master dictionaries of currently active all-network replacements.
		internal static Dictionary<PrefabInfo, BOBAllNetworkElement> propReplacements;

		// Master dictionary of active all-network replacements currently applied to network prefabs.
		internal static Dictionary<NetInfo, SortedList<int, SortedList<int, NetReplacement>>> allNetworkDict;



		/// <summary>
		/// Performs setup and initialises the master dictionaries.  Must be called prior to use.
		/// </summary>
		internal static void Setup()
		{
			allNetworkDict = new Dictionary<NetInfo, SortedList<int, SortedList<int, NetReplacement>>>();
			propReplacements = new Dictionary<PrefabInfo, BOBAllNetworkElement>();
		}

		
		/// <summary>
		/// Reverts all active all-network replacements and re-initialises the master dictionaries.
		/// </summary>
		internal static void RevertAll()
		{
			// Iterate through each entry in the master prop dictionary.
			foreach (PrefabInfo prop in propReplacements.Keys)
			{
				// Revert this replacement (but don't remove the entry, as the dictionary is currently immutable while we're iterating through it).
				Revert(prop, propReplacements[prop].targetInfo, removeEntries: false);
			}

			// Re-initialise the dictionaries.
			Setup();
		}


		/// <summary>
		/// Reverts an all-network replacement.
		/// </summary>
		/// <param name="target">Targeted (original) tree/prop prefab</param>
		/// <param name="replacement">Applied replacment tree/prop prefab</param>
		/// <param name="removeEntries">True (default) to remove the reverted entries from the master dictionary, false to leave the dictionary unchanged</param>
		/// <returns>True if the entire network record was removed from the dictionary (due to no remaining replacements for that prefab), false if the prefab remains in the dictionary (has other active replacements)</returns>
		internal static void Revert(PrefabInfo target, PrefabInfo replacement, bool removeEntries = true)
		{
			// List of reverted entries.
			List<KeyValuePair<NetInfo, KeyValuePair<int, int>>> list = new List<KeyValuePair<NetInfo, KeyValuePair<int, int>>>();

			// Get original offset for this replacement.
			BOBAllNetworkElement replacementEntry = propReplacements[target];
			Vector3 offset = new Vector3 { x = replacementEntry.offsetX, y = replacementEntry.offsetY, z = replacementEntry.offsetZ };

 			// Iterate through all buildings in the applied all-network replacements dictionary.
			foreach (NetInfo network in allNetworkDict.Keys)
			{
				// Iterate through each lane in this network.
				foreach (int laneIndex in allNetworkDict[network].Keys)
				{
					// Iterate through each applied replacement for this lane and network.
					foreach (int propIndex in allNetworkDict[network][laneIndex].Keys)
					{
						// Get currently active replacement and check to see if it matches our reversion parameters (target and replacement match).
						NetReplacement currentReplacement = allNetworkDict[network][laneIndex][propIndex];
						if (currentReplacement.targetInfo == target && currentReplacement.replacementInfo == replacement)
						{
							// Match - revert to original (including reverting angle and position).
							network.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_finalProp = (PropInfo)target;
							network.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_angle -= currentReplacement.angle;
							network.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_position -= offset;

							// Add this to our list of reverted entries.
							list.Add(new KeyValuePair<NetInfo, KeyValuePair<int, int>>(network, new KeyValuePair<int, int>(laneIndex, propIndex)));
						}
					}
				}
			}

			// If we're not removing entries from the dictionaries, we're done here; return.
			if (!removeEntries)
			{
				return;
			}

			// Remove reverted entries from our dictionary of replacements applied to networks.
			foreach (KeyValuePair<NetInfo, KeyValuePair<int, int>> item in list)
			{
				RemoveEntry(item.Key, item.Value.Key, item.Value.Value);
			}

			// Remove entry from our master dictionary of tree/prop replacements.
			propReplacements.Remove(target);
		}


		/// <summary>
		/// Removes an entry from the master dictionary of all-network replacements currently applied to networks.
		/// </summary>
		/// <param name="buildingPrefab">Network prefab</param>
		/// <param name="laneIndex">Lane index</param>
		/// <param name="propIndex">Prop index</param>
		internal static void RemoveEntry(NetInfo netPrefab, int laneIndex, int propIndex)
		{
			// Check to see if we have an entry for this building.
			if (allNetworkDict.ContainsKey(netPrefab))
			{
				// Yes - check to see if we have an entry for this lane.
				if (allNetworkDict[netPrefab].ContainsKey(laneIndex))
				{
					// Yes - remove the given index.
					allNetworkDict[netPrefab][laneIndex].Remove(propIndex);

					// Check to see if there are any remaining replacements for this lane.
					if (allNetworkDict[netPrefab][laneIndex].Count == 0)
					{
						// No remaining replacements - remove the entire lane entry.
						allNetworkDict[netPrefab].Remove(laneIndex);
					}

					// Check to see if there are any remaining replacements for this network prefab.
					if (allNetworkDict[netPrefab].Count == 0)
					{
						// No remaining replacements - remove the entire network prefab entry.
						allNetworkDict.Remove(netPrefab);
					}
				}
			}
		}


		/// <summary>
		/// Applies a new (or updated) all-network replacement.
		/// </summary>
		/// <param name="target">Targeted (original) prop prefab</param>
		/// <param name="replacement">Replacment prop prefab</param>
		/// <param name="angle">Replacment prop angle adjustment</param>
		/// <param name="offsetX">Replacment X position offset</param>
		/// <param name="offsetY">Replacment Y position offset</param>
		/// <param name="offsetZ">Replacment Z position offset</param>
		internal static void Apply(PrefabInfo target, PrefabInfo replacement, float angle, float offsetX, float offsetY, float offsetZ)
		{
			// Initialize old offsets.
			float oldX = 0f, oldY = 0f, oldZ = 0f;

			// Set our initial targeted prefab to the provided target. 
			PrefabInfo targetedPrefab = target;

			// Make sure that target and replacement are the same type before doing anything.
			if (target == null || replacement == null || (target is TreeInfo && !(replacement is TreeInfo)) || (target is PropInfo) && !(replacement is PropInfo))
			{
				return;
			}

			// Prop - see if we already have a replacement for this prop.
			if (propReplacements.ContainsKey(target))
			{
				// We currently have a replacement - change the targeted prefab to replace to match the currently active replacement.
				targetedPrefab = propReplacements[target].targetInfo;

				// Get previous offset.
				oldX = propReplacements[target].offsetX;
				oldY = propReplacements[target].offsetY;
				oldZ = propReplacements[target].offsetZ;

				// Update dictionary with this replacement.
				propReplacements[target].replacementInfo = target;
				propReplacements[target].angle = angle;
				propReplacements[target].offsetX = offsetX;
				propReplacements[target].offsetY = offsetY;
				propReplacements[target].offsetZ = offsetZ;
			}
			else
			{
				// No current replacement - add this one to the dictionary (retaining the default targeted prefab).
				propReplacements.Add(target, new BOBAllNetworkElement { replacementInfo = replacement, angle = angle, offsetX = offsetX, offsetY = offsetY, offsetZ = offsetZ });
			}

			// Iterate through each loaded network to apply replacements.
			for (int i = 0; i < PrefabCollection<NetInfo>.LoadedCount(); ++i)
			{
				// Get local reference.
				NetInfo loaded = PrefabCollection<NetInfo>.GetLoaded((uint)i);

				// Skip any netorks without lanes.
				if (loaded.m_lanes == null)
				{
					continue;
				}

				// Iterate through each lane.
				for (int lane = 0; lane < loaded.m_lanes.Length; ++lane)
				{
					// If no props in this lane, skip it and go to the next one.
					if (loaded.m_lanes[lane].m_laneProps?.m_props == null)
					{
						continue;
					}

					// Iterate through each prop in lane.
					for (int propIndex = 0; propIndex < loaded.m_lanes[lane].m_laneProps.m_props.Length; ++propIndex)
					{
						// Check for any currently active network replacement.
						if (NetworkReplacement.GetOriginal(loaded, lane, propIndex) != null)
						{
							// Active network replacement; skip this one.
							continue;
						}

						// See if this prop matches our replacement.
						if (loaded.m_lanes[lane].m_laneProps.m_props[propIndex].m_finalProp != null && loaded.m_lanes[lane].m_laneProps.m_props[propIndex].m_finalProp == targetedPrefab)
						{
							// Match!  Add to dictionary of currently active replacements.
							AddEntry(loaded, target, replacement, lane, propIndex, angle, offsetX, offsetY, offsetZ);

							// Apply replacement (including angle and offset).
							Vector3 newOffset = new Vector3 { x = offsetX - oldX, y = offsetY - oldY, z = offsetZ - oldZ };
							loaded.m_lanes[lane].m_laneProps.m_props[propIndex].m_finalProp = (PropInfo)replacement;
							loaded.m_lanes[lane].m_laneProps.m_props[propIndex].m_angle += angle;
							loaded.m_lanes[lane].m_laneProps.m_props[propIndex].m_position += newOffset;
						}
					}
				}
			}
		}


		/// <summary>
		/// Checks if there's a currently active all-network replacement applied to the given network prop index.
		/// </summary>
		/// <param name="netPrefab">Network prefab to check</param>
		/// <param name="laneIndex">Lane index to check</param>
		/// <param name="propIndex">Prop index to check</param>
		/// <returns>Original prefab if a all-building replacement is currently applied, null if no all-building replacement is currently applied</returns>
		internal static PrefabInfo ActiveReplacement(NetInfo netPrefab, int laneIndex, int propIndex)
		{
			// Try to find an entry for this index of this building and index in the master dictionary.
			if (allNetworkDict.ContainsKey(netPrefab) && allNetworkDict[netPrefab].ContainsKey(laneIndex) && allNetworkDict[netPrefab][laneIndex].ContainsKey(propIndex))
			{
				// Entry found - return the stored original prefab.
				return allNetworkDict[netPrefab][laneIndex][propIndex].targetInfo;
			}

			// No entry found - return null to indicate no active replacement.
			return null;
		}


		/// <summary>
		/// Restores a all-network replacement, if any (e.g. after a network replacement has been reverted).
		/// </summary>
		/// <param name="netPrefab">Network prefab</param>
		/// <param name="laneIndex">Lane index</param>
		/// <param name="propIndex">Prop index</param>
		internal static void Restore(NetInfo netPrefab, int laneIndex, int propIndex)
		{
			// Get current prop.
			NetLaneProps.Prop prop = netPrefab.m_lanes[laneIndex].m_laneProps.m_props[propIndex];

			PrefabInfo original = null;
			PrefabInfo replacement = null;

			// Does this lane prop record contain a prop?
			if (prop.m_finalProp != null)
			{
				// It does - check for active all-network replacement for this prop.
				if (propReplacements.ContainsKey(prop.m_finalProp))
				{
					// Found an active replacement - apply it.
					original = prop.m_finalProp;
					replacement = propReplacements[original].targetInfo;
					prop.m_finalProp = (PropInfo)replacement;

					// Adjust angle.
					prop.m_angle += propReplacements[original].angle;

					// Adjust position.
					prop.m_position += new Vector3 { x = propReplacements[original].offsetX, y = propReplacements[original].offsetY, z = propReplacements[original].offsetZ };
				}
			}

			// If we made a replacement (original has been set to a non-null value), add it to our dictionary of replacements applied to networks.
			if (original != null)
			{
				AddEntry(netPrefab, original, replacement, laneIndex, propIndex, propReplacements[original].angle, propReplacements[original].offsetX, propReplacements[original].offsetY, propReplacements[original].offsetZ);
			}
		}


		/// <summary>
		/// Adds an entry to the master dictionary of all-network replacements currently applied to networks.
		/// </summary>
		/// <param name="netPrefab">Network prefab</param>
		/// <param name="target">Targeted (original) tree/prop prefab</param>
		/// <param name="replacement">Replacment tree/prop prefab</param>
		/// <param name="laneIndex">Lane index</param>
		/// <param name="propIndex">Prop index</param>
		/// <param name="angle">Prop angle adjustment</param>
		/// <param name="offsetX">Replacment X position offset</param>
		/// <param name="offsetY">Replacment Y position offset</param>
		/// <param name="offsetZ">Replacment Z position offset</param>
		private static void AddEntry(NetInfo netPrefab, PrefabInfo target, PrefabInfo replacement, int laneIndex, int propIndex, float angle, float offsetX, float offsetY, float offsetZ)
		{
			// Check to see if we don't already have an entry for this network prefab in the master dictionary.
			if (!allNetworkDict.ContainsKey(netPrefab))
			{
				// No existing entry, so add one.
				allNetworkDict.Add(netPrefab, new SortedList<int, SortedList<int, NetReplacement>>());
			}

			// Check to see if we don't already have an entry for this lane in the master dictionary.
			if (!allNetworkDict[netPrefab].ContainsKey(laneIndex))
			{
				// No existing entry, so add one.
				allNetworkDict[netPrefab].Add(laneIndex, new SortedList<int, NetReplacement>());
			}

			// Check to see if we already have an entry for this prop index in the master dictionary.
			if (allNetworkDict[netPrefab][laneIndex].ContainsKey(propIndex))
			{
				// An entry already exists - just update the replacement info.
				allNetworkDict[netPrefab][laneIndex][propIndex].replacementInfo = replacement;
				allNetworkDict[netPrefab][laneIndex][propIndex].angle = angle;
				allNetworkDict[netPrefab][laneIndex][propIndex].offsetX = offsetX;
				allNetworkDict[netPrefab][laneIndex][propIndex].offsetY = offsetY;
				allNetworkDict[netPrefab][laneIndex][propIndex].offsetZ = offsetZ;
			}
			else
			{
                // No existing entry - create one.
                NetReplacement newReplacement = new NetReplacement
                {
                    targetInfo = target,
                    replacementInfo = replacement,
					angle = angle,
					offsetX = offsetX,
					offsetY = offsetY,
					offsetZ = offsetZ
                };
                allNetworkDict[netPrefab][laneIndex].Add(propIndex, newReplacement);
			}
		}
	}
}
