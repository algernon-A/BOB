using System.Collections.Generic;
using UnityEngine;


namespace BOB
{
	/// <summary>
	/// Static class to manage building prop and tree replacements.
	/// </summary>
	internal static class NetworkReplacement
	{
		// Master dictionary of network replacements.
		// Yes, this is a three-layer dictionary (prefab by lane by prop index).
		internal static Dictionary<NetInfo, SortedList<int, SortedList<int, NetReplacement>>> netDict;


		/// <summary>
		/// Performs setup and initialises the master dictionary.  Must be called prior to use.
		/// </summary>
		internal static void Setup()
		{
			netDict = new Dictionary<NetInfo, SortedList<int, SortedList<int, NetReplacement>>>();
		}


		/// <summary>
		/// Applies a new network replacement (replacing individual trees or props).
		/// </summary>
		/// <param name="netPrefab">Network prefab to apply to</param>
		/// <param name="replacement">Replacement to apply</param>
		/// <param name="oldAngle">Currently applied replacement angle (if any)</param>
		/// <param name="oldX">Currently applied position X offset (if any)</param>
		/// <param name="oldY">Currently applied position X offset (if any)</param>
		/// <param name="oldZ">Currently applied position X offset (if any)</param>
		internal static void ApplyReplacement(NetInfo netPrefab, NetReplacement replacement, float oldAngle = 0f, float oldX = 0f, float oldY = 0f, float oldZ = 0f)
		{
			// Just in case.
			if (replacement.targetInfo == null || replacement.targetName == null || replacement.replaceName == null || replacement.replacementInfo == null)
			{
				Debugging.Message("invalid replacement");
				return;
			}

			// Set new prop.
			netPrefab.m_lanes[replacement.lane].m_laneProps.m_props[replacement.targetIndex].m_finalProp = (PropInfo)replacement.replacementInfo;

			// Apply new angle, subtracting currently applied angle (if any).
			netPrefab.m_lanes[replacement.lane].m_laneProps.m_props[replacement.targetIndex].m_angle += replacement.angle - oldAngle;

			// Apply new offset, subtracting currently applied offset.
			Vector3 newOffset = new Vector3 { x = replacement.offsetX - oldX, y = replacement.offsetY - oldY, z = replacement.offsetZ - oldZ };
			netPrefab.m_lanes[replacement.lane].m_laneProps.m_props[replacement.targetIndex].m_position += newOffset;

			// Remove any currently applied all-building building replacement entry for this tree or prop.
			AllNetworkReplacement.RemoveEntry(netPrefab, replacement.targetInfo, replacement.lane, replacement.targetIndex);
		}


		/// <summary>
		/// Adds or upates a network prop or tree replacement.
		/// </summary>
		/// <param name="netPrefab">Network prefab to apply to</param>
		/// <param name="replacement">Replacement to apply</param>
		/// <param name="index">(Optional) target index override</param>
		/// <param name="lane">(Optional) target lane override</param>
		internal static void AddReplacement(NetInfo netPrefab, NetReplacement replacement, int index = -1, int lane = -1)
		{
			// Originally applied angle and offsets.
			float oldAngle = 0f, oldX = 0f, oldY = 0f, oldZ = 0f;

			// Clone the provided replacement record for adding to the master dictionary (so the original can be modified by the calling method without clobbering the dictionary entry, and so we can tweak the clone here prior to adding without affecting the original).
			NetReplacement clone = ReplacementUtils.Clone(replacement);

			// Check to see if an index override has been provided.
			if (index >= 0)
			{
				// Override provided - simply use the provided index as the target index.
				clone.targetIndex = index;
			}

			// Check to see if a lane override has been provided.
			if (lane >= 0)
			{
				// Override provided - simply use the provided index as the target index.
				clone.lane = lane;
			}

			// Check to see if we don't already have an entry for this net prefab in the master dictionary.
			if (!netDict.ContainsKey(netPrefab))
			{
				// No existing entry, so add one.
				netDict.Add(netPrefab, new SortedList<int, SortedList<int, NetReplacement>>());
			}

			// Check to see if we don't already have an entry for this lane in the master dictionary.
			if (!netDict[netPrefab].ContainsKey(clone.lane))
			{
				// No existing entry, so add one.
				netDict[netPrefab].Add(clone.lane, new SortedList<int, NetReplacement>());
			}

			// Check to see if we already have an entry for this replacement in the master dictionary.
			if (netDict[netPrefab][clone.lane].ContainsKey(clone.targetIndex))
			{
				// An entry already exists - update it, but record the angle and offset first.
				oldAngle = netDict[netPrefab][clone.lane][clone.targetIndex].angle;
				oldX = netDict[netPrefab][clone.lane][clone.targetIndex].offsetX;
				oldY = netDict[netPrefab][clone.lane][clone.targetIndex].offsetY;
				oldZ = netDict[netPrefab][clone.lane][clone.targetIndex].offsetZ;
				netDict[netPrefab][clone.lane][clone.targetIndex] = clone;
			}
			else
			{
				// No existing entry - add a new one.
				netDict[netPrefab][clone.lane].Add(clone.targetIndex, clone);
			}

			// Apply the actual tree/prop prefab replacement.
			ApplyReplacement(netPrefab, clone, oldAngle, oldX, oldY, oldZ);
		}


		/// <summary>
		/// Reverts an individual building replacement.
		/// </summary>
		/// <param name="netPrefab">Targeted network prefab</param>
		/// <param name="lane">Replacement lane to revert</param>
		/// <param name="index">Replacement index to revert</param>
		/// <param name="removeEntries">True (default) to remove the reverted entries from the master dictionary, false to leave the dictionary unchanged</param>
		/// <returns>True if the entire network record was removed from the dictionary (due to no remaining replacements for that prefab), false if the prefab remains in the dictionary (has other active replacements)</returns>
		internal static bool Revert(NetInfo netPrefab, int lane, int index, bool removeEntries = true)
		{
			// Get original prop.
			PrefabInfo prefabInfo = GetOriginal(netPrefab, lane, index);

			// Only revert if there is an active replacement (GetOriginal returns null if there's no active replacement).
			if (prefabInfo != null)
			{
				// Local reference.
				NetReplacement netReplacement = netDict[netPrefab][lane][index];

				// Prop - restore original.
				netPrefab.m_lanes[lane].m_laneProps.m_props[index].m_finalProp = prefabInfo as PropInfo;

				// Restore original angle.
				netPrefab.m_lanes[lane].m_laneProps.m_props[index].m_angle -= netReplacement.angle;

				// Undo offset.
				Vector3 oldOffset = new Vector3 { x = netReplacement.offsetX, y = netReplacement.offsetY, z = netReplacement.offsetZ };
				netPrefab.m_lanes[lane].m_laneProps.m_props[index].m_position -= oldOffset;

				// Apply any all-network replacement.
				AllNetworkReplacement.Restore(netPrefab, prefabInfo, lane, index);

				// Remove dictionary entries if that setting is enabled.
				if (removeEntries)
				{
					// Remove individual replacement record.
					netDict[netPrefab][lane].Remove(index);

					// Check to see if there are any remaining replacements for this lane.
					if (netDict[netPrefab][lane].Count == 0)
					{
						// No remaining replacements - remove the entire lane entry.
						netDict[netPrefab].Remove(lane);
					}

					// Check to see if there are any remaining replacements for this network prefab.
					if (netDict[netPrefab].Count == 0)
					{
						// No remaining replacements - remove the entire network prefab entry and return true to indicate that we've done so.
						netDict.Remove(netPrefab);
						return true;
					}
				}
			}

			// If we got here, we haven't removed the building prefab entry from the master dictionary - return false to indicate that.
			return false;
		}


		/// <summary>
		/// Retrieves the original tree/prop prefab for the given lane and index (returns null if there's no active replacement).
		/// </summary>
		/// <param name="netPrefab">Network prefab prefab</param>
		/// <param name="lane">Lane index</param>
		/// <param name="index">Prop index</param>
		/// <returns>PrefabInfo of the original prefab, or null if there's no currently active replacement</returns>
		internal static PrefabInfo GetOriginal(NetInfo netPrefab, int lane, int index)
		{
			// Try to find an entry for this index of this network in the master dictionary.
			if (netDict.ContainsKey(netPrefab) && netDict[netPrefab].ContainsKey(lane) && netDict[netPrefab][lane].ContainsKey(index))
			{
				// Entry found - return the stored original prefab.
				return netDict[netPrefab][lane][index].targetInfo;
			}

			// No entry found - return null to indicate no active replacement.
			return null;
		}
	}
}
