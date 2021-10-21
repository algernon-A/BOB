using System.Collections.Generic;
using System.Linq;


namespace BOB
{

	/// <summary>
	/// Class to manage individual building prop and tree replacements.
	/// </summary>
	internal class IndividualNetworkReplacement : NetworkReplacementBase
	{
		// Instance reference.
		internal static IndividualNetworkReplacement instance;

		// Master dictionary of replaced prop references.
		private Dictionary<NetInfo, Dictionary<KeyValuePair<int, int>, BOBNetReplacement>> replacements;


		/// <summary>
		/// Retrieves a currently-applied individual network replacement entry for the given network, lane and prop index.
		/// </summary>
		/// <param name="network">Network prefab</param>
		/// <param name="lane">Lane number</param>
		/// <param name="index">Prop index number</param>
		/// <returns>Currently-applied individual network replacement (null if none)</returns>
		internal BOBNetReplacement Replacement(NetInfo network, int lane, int index)
        {
			if (replacements.TryGetValue(network, out Dictionary<KeyValuePair<int, int>, BOBNetReplacement> networkEntry))
            {
				if (networkEntry.TryGetValue(new KeyValuePair<int, int>(lane, index), out BOBNetReplacement replacementEntry))
                {
					return replacementEntry;
                }
            }

			// If we got here, something went wrong.
			Logging.Error("no individual network replacement entry for network ", network?.name ?? "null", " with lane ", lane.ToString(), " and index ", index.ToString());
			return null;
        }


		/// <summary>
		/// Constructor - initializes instance reference.
		/// </summary>
		internal IndividualNetworkReplacement()
		{
			instance = this;
		}


		/// <summary>
		/// Reverts all active individual network replacements and re-initialises the master dictionary.
		/// </summary>
		internal override void RevertAll()
		{
			foreach (NetInfo network in replacements.Keys)
			{
				// Iterate through each entry in the master prop dictionary.
				foreach (KeyValuePair<int, int> key in replacements[network].Keys)
				{
					// Revert this replacement (but don't remove the entry, as the dictionary is currently immutable while we're iterating through it).
					Revert(network, key.Key, key.Value, removeEntries: false);
				}
			}

			// Re-initialise the dictionaries.
			Setup();
		}


		/// <summary>
		/// Reverts an individual network replacement.
		/// </summary>
		/// <param name="network">Targeted network</param>
		/// <param name="targetIndex">Targeted (original) tree/prop lane</param>
		/// <param name="targetIndex">Targeted (original) tree/prop prefab index</param>
		/// <param name="target">Targeted (original) tree/prop prefab</param>
		/// <param name="removeEntries">True (default) to remove the reverted entries from the master dictionary, false to leave the dictionary unchanged</param>
		/// <returns>True if the entire network record was removed from the dictionary (due to no remaining replacements for that prefab), false if the prefab remains in the dictionary (has other active replacements)</returns>
		internal void Revert(NetInfo network, int lane, int targetIndex, bool removeEntries = true)
		{
			// Safety check.
			if (network == null || !replacements.ContainsKey(network))
			{
				return;
			}

			// Create reference index from lane and prop index.
			KeyValuePair<int, int> targetKey = new KeyValuePair<int, int>(lane, targetIndex);

			// Iterate through each entry in our dictionary.
			foreach (NetPropReference propReference in replacements[network][targetKey].references)
			{
				// Local reference.
				PrefabInfo target = replacements[network][targetKey].targetInfo;

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

				// Restore any network replacement.
				if (!NetworkReplacement.instance.Restore(network, propReference.laneIndex, target, propReference.propIndex))
				{
					// Restore any all-network replacement.
					if (!AllNetworkReplacement.instance.Restore(network, target, propReference.laneIndex, propReference.propIndex))
					{
						// No all-network restoration occured - restore any pack replacement.
						NetworkPackReplacement.instance.Restore(network, target, propReference.laneIndex, propReference.propIndex);
					}
				}

				// Add network to dirty list.
				NetData.DirtyList.Add(propReference.network);
			}

			// Remove entry from dictionary, if we're doing so.
			if (removeEntries)
			{
				replacements[network].Remove(targetKey);

				// Delete entire network entry if nothing left after removing this one.
				if (replacements[network].Count == 0)
				{
					replacements.Remove(network);
				}
			}
		}


		/// <summary>
		/// Applies a new (or updated) individual network replacement.
		/// </summary>
		/// <param name="network">Targeted network</param>
		/// <param name="target">Targeted (original) prop prefab</param>
		/// <param name="targetLane">Targeted lane index (in parent network)</param>
		/// <param name="targetIndex">Prop index to apply replacement to</param>
		/// <param name="replacement">Replacment prop prefab</param>
		/// <param name="angle">Replacment prop angle adjustment</param>
		/// <param name="offsetX">Replacment X position offset</param>
		/// <param name="offsetY">Replacment Y position offset</param>
		/// <param name="offsetZ">Replacment Z position offset</param>
		/// <param name="probability">Replacement probability</param>
		internal void Apply(NetInfo network, PrefabInfo target, int lane, int targetIndex, PrefabInfo replacement, float angle, float offsetX, float offsetY, float offsetZ, int probability)
		{
			Logging.Message("applying individual network replacement");

			// Safety check.
			if (network?.m_lanes == null)
			{
				return;
			}

			// Make sure that target and replacement are the same type before doing anything.
			if (target == null || replacement == null || (target is TreeInfo && !(replacement is TreeInfo)) || (target is PropInfo) && !(replacement is PropInfo))
			{
				return;
			}


			Logging.Message("individual network replacement of ", target.name, " with ", replacement.name, " for lane ", lane.ToString(), " and index ", targetIndex.ToString());

			// Create key.
			KeyValuePair<int, int> indexKey = new KeyValuePair<int, int>(lane, targetIndex);

			// Check to see if we already have a replacement entry for this prop - if so, revert the replacement first.
			if (replacements.ContainsKey(network) && replacements[network].ContainsKey(indexKey))
			{
				Revert(network, lane, targetIndex, true);
			}

			// Create new dictionary entry for network if none already exists.
			if (!replacements.ContainsKey(network))
			{
				replacements.Add(network, new Dictionary<KeyValuePair<int, int>, BOBNetReplacement>());
			}

			// Create new dictionary entry for prop if none already exists.
			if (!replacements[network].ContainsKey(indexKey))
			{
				replacements[network].Add(indexKey, new BOBNetReplacement());
			}

			// Add/replace dictionary replacement data.
			replacements[network][indexKey].references = new List<NetPropReference>();
			replacements[network][indexKey].tree = target is TreeInfo;
			replacements[network][indexKey].targetInfo = target;
			replacements[network][indexKey].replacementInfo = replacement;
			replacements[network][indexKey].target = target.name;
			replacements[network][indexKey].lane = lane;
			replacements[network][indexKey].index = targetIndex;
			replacements[network][indexKey].angle = angle;
			replacements[network][indexKey].offsetX = offsetX;
			replacements[network][indexKey].offsetY = offsetY;
			replacements[network][indexKey].offsetZ = offsetZ;
			replacements[network][indexKey].probability = probability;

			// Create replacement record.
			NetPropReference propReference = new NetPropReference
			{
				network = network,
				laneIndex = lane,
				propIndex = targetIndex,
				angle = network.m_lanes[lane].m_laneProps.m_props[targetIndex].m_angle,
				position = network.m_lanes[lane].m_laneProps.m_props[targetIndex].m_position,
				probability = network.m_lanes[lane].m_laneProps.m_props[targetIndex].m_probability
			};

			// Add reference data to the list (only entry....)
			replacements[network][indexKey].references.Add(propReference);

			// Reset any pack, network, or all-network replacements first.
			NetworkPackReplacement.instance.RemoveEntry(network, target, propReference.laneIndex, propReference.propIndex);
			AllNetworkReplacement.instance.RemoveEntry(network, target, propReference.laneIndex, propReference.propIndex);
			NetworkReplacement.instance.RemoveEntry(network, target, propReference.laneIndex, propReference.propIndex);


			// If this is a vanilla network, then we've probably got shared NetLaneProp references, so need to copy to a new instance.
			// If the name doesn't contain a period (c.f. 12345.MyNetwok_Data), then assume it's vanilla - may be a mod or not shared, but better safe than sorry.
			if (!network.name.Contains("."))
			{
				Logging.Message("creating new m_laneProps instance for network ", network.name);

				// Create new m_laneProps instance with new props list.
				NetLaneProps newLaneProps = new NetLaneProps
				{
					m_props = new NetLaneProps.Prop[network.m_lanes[lane].m_laneProps.m_props.Length]
				};

				// Iterate through each  in the existing instance
				for (int i = 0; i < newLaneProps.m_props.Length; ++i)
				{
					NetLaneProps.Prop existingNetLaneProp = network.m_lanes[lane].m_laneProps.m_props[i];

					newLaneProps.m_props[i] = new NetLaneProps.Prop
					{
						m_flagsRequired = existingNetLaneProp.m_flagsRequired,
						m_flagsForbidden = existingNetLaneProp.m_flagsForbidden,
						m_startFlagsRequired = existingNetLaneProp.m_startFlagsRequired,
						m_startFlagsForbidden = existingNetLaneProp.m_startFlagsForbidden,
						m_endFlagsRequired = existingNetLaneProp.m_endFlagsRequired,
						m_endFlagsForbidden = existingNetLaneProp.m_endFlagsForbidden,
						m_colorMode = existingNetLaneProp.m_colorMode,
						m_prop = existingNetLaneProp.m_prop,
						m_tree = existingNetLaneProp.m_tree,
						m_position = existingNetLaneProp.m_position,
						m_angle = existingNetLaneProp.m_angle,
						m_segmentOffset = existingNetLaneProp.m_segmentOffset,
						m_repeatDistance = existingNetLaneProp.m_repeatDistance,
						m_minLength = existingNetLaneProp.m_minLength,
						m_cornerAngle = existingNetLaneProp.m_cornerAngle,
						m_probability = existingNetLaneProp.m_probability,
						m_finalProp = existingNetLaneProp.m_finalProp,
						m_finalTree = existingNetLaneProp.m_finalTree
					};
				}

				// Replace network laneProps with our new instance.
				network.m_lanes[lane].m_laneProps = newLaneProps;
			}

			// Apply the replacement.
			ReplaceProp(replacements[network][indexKey], propReference);
		}


		/// <summary>
		/// Checks if there's a currently active network replacement applied to the given network prop index, and if so, returns the replacement record.
		/// </summary>
		/// <param name="netPrefab">Network prefab to check</param>
		/// <param name="laneIndex">Lane index to check</param>
		/// <param name="propIndex">Prop index to check</param>
		/// <returns>Replacement record if a network replacement is currently applied, null if no network replacement is currently applied</returns>
		internal override BOBNetReplacement ActiveReplacement(NetInfo netPrefab, int laneIndex, int propIndex)
		{
			// Safety check.
			if (netPrefab != null && replacements.ContainsKey(netPrefab))
			{
				KeyValuePair<int, int> targetKey = new KeyValuePair<int, int>(laneIndex, propIndex);

				// Just check for a match in our dictionary.
				if (replacements[netPrefab].ContainsKey(targetKey))
				{
					// Got a match - simply return the entry.
					return replacements[netPrefab][targetKey];
				}
			}

			// If we got here, no entry was found - return null to indicate no active replacement.
			return null;
		}


		/// <summary>
		/// Serializes network replacement dictionary to XML format.
		/// </summary>
		/// <returns>List of network replacement entries in XML Format</returns>
		internal List<BOBNetworkElement> Serialize()
		{
			// Serialise network replacements, per network.
			List<BOBNetworkElement> elementList = new List<BOBNetworkElement>();
			foreach (NetInfo network in replacements.Keys)
			{
				elementList.Add(new BOBNetworkElement
				{
					network = network.name,
					replacements = replacements[network].Values.ToList()
				});
			}

			return elementList;
		}


		/// <summary>
		/// Performs setup and initialises the master dictionary.  Must be called prior to use.
		/// </summary>
		protected override void Setup()
		{
			replacements = new Dictionary<NetInfo, Dictionary<KeyValuePair<int, int>, BOBNetReplacement>>();
		}
	}
}