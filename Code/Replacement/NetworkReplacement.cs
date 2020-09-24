using System.Collections.Generic;


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
		internal static void ApplyReplacement(NetInfo netPrefab, NetReplacement replacement)
		{
			// Just in case.
			if (replacement.targetInfo == null || replacement.targetName == null || replacement.replaceName == null || replacement.replacementInfo == null)
			{
				Debugging.Message("invalid replacement");
				return;
			}

			// Set new prop.
			netPrefab.m_lanes[replacement.lane].m_laneProps.m_props[replacement.targetIndex].m_finalProp = (PropInfo)replacement.replacementInfo;

			/*
			// Iterate through each lane.
			for (int i = 0; i < netPrefab.m_lanes.Length; ++i)
			{
				// Get lane props.
				NetLaneProps.Prop[] laneProps = netPrefab.m_lanes[i].m_laneProps?.m_props;

				if (laneProps != null && laneProps.Length > 0)
				{
					// Iterate through each lane prop.
					for (int j = 0; j < laneProps.Length; ++j)
					{
						// Replace prop, if we have a match.
						if (laneProps[j].m_prop == replacement.targetInfo)
                        {
							laneProps[j].m_prop = (PropInfo)replacement.replacementInfo;
                        }

						// Replace prop, if we have a match.
						if (laneProps[j].m_finalProp == replacement.targetInfo)
						{
							laneProps[j].m_finalProp = (PropInfo)replacement.replacementInfo;
						}
					}
				}
            }*/
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
				// An entry already exists - update it.
				netDict[netPrefab][clone.lane][clone.targetIndex] = clone;
			}
			else
			{
				// No existing entry - add a new one.
				netDict[netPrefab][clone.lane].Add(clone.targetIndex, clone);
			}

			// Apply the actual tree/prop prefab replacement.
			ApplyReplacement(netPrefab, clone);
		}
	}
}
