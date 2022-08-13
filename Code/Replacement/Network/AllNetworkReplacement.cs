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
		/// The priority level of this replacmeent type.
		/// </summary>
		protected override ReplacementPriority ThisPriority => ReplacementPriority.AllReplacement;

		/// <summary>
		/// Finds any existing replacement relevant to the provided arguments.
		/// </summary>
		/// <param name="netInfo">Network ifno</param>
		/// <param name="laneIndex">Lane index</param>
		/// <param name="propIndex">Prop index</param>
		/// <param name="targetInfo">Target prop/tree prefab</param>
		/// <returns>Existing replacement entry, if one was found, otherwise null</returns>
		protected override BOBNetReplacement FindReplacement(NetInfo netInfo, int laneIndex, int propIndex, PrefabInfo targetInfo) =>
			ReplacementList(netInfo)?.Find(x => x.targetInfo == targetInfo);

		/// <summary>
		/// Reverts all active replacements.
		/// </summary>
		internal override void RevertAll()
		{
			foreach (BOBNetReplacement replacement in ReplacementList(null))
			{
				// Remove any references to this replacement from all network handlers.
				NetHandlers.RemoveReplacement(replacement);
			}

			// Clear configuration file.
			ReplacementList(null).Clear();
		}


		/// <summary>
		/// Deserialises a network element list.
		/// </summary>
		/// <param name="elementList">Element list to deserialise</param>
		internal void Deserialize(List<BOBNetReplacement> elementList) => Deserialize(null, elementList);


		/// <summary>
		/// Applies an all-network prop replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to apply</param>
		protected override void ApplyReplacement(BOBNetReplacement replacement)
		{
			// Don't do anything if prefabs can't be found.
			if (replacement?.targetInfo == null || replacement.replacementInfo == null)
			{
				return;
			}

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
					// Local references.
					NetInfo.Lane thisLane = netInfo.m_lanes[laneIndex];
					NetLaneProps.Prop[] theseLaneProps = thisLane?.m_laneProps?.m_props;

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

						// Note current props.
						TreeInfo thisTree = thisLaneProp.m_tree;
						PropInfo thisProp = thisLaneProp.m_prop;

						// Get any active handler.
						LanePropHandler handler = NetHandlers.GetHandler(thisLane, propIndex);
						if (handler != null)
						{
							// Active handler found - use original values for checking eligibility (instead of currently active values).
							thisTree = handler.OriginalTree;
							thisProp = handler.OriginalProp;
						}

						// See if this prop matches our replacement.
						bool treeMatch = replacement.isTree && thisTree != null && thisTree == replacement.targetInfo;
						bool propMatch = !replacement.isTree && thisProp != null && thisProp == replacement.targetInfo;
						if (treeMatch | propMatch)
						{
							// Match!  Create new handler if there wasn't an existing one.
							if (handler == null)
							{
								handler = NetHandlers.GetOrAddHandler(netInfo, thisLane, propIndex);
							}

							// Set the new replacement.
							handler.SetReplacement(replacement, ThisPriority);
						}
					}
				}
			}
		}

		/// <summary>
		/// Gets the relevant replacement list entry from the active configuration file, if any.
		/// </summary>
		/// <param name="netInfo">Network prefab</param>
		/// <returns>Replacement list for the specified network prefab (null if none)</returns>
		protected override List<BOBNetReplacement> ReplacementList(NetInfo netInfo) => ConfigurationUtils.CurrentConfig.allNetworkProps;


		/// <summary>
		/// Gets the relevant network replacement list entry from the active configuration file, creating a new network entry if none already exists.
		/// </summary>
		/// <param name="netInfo">Network prefab</param>
		/// <returns>Replacement list for the specified network prefab</returns>
		protected override List<BOBNetReplacement> ReplacementEntry(NetInfo netInfo) => ReplacementList(netInfo);


		/// <summary>
		/// Removes a replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to remove</param>
		/// <param name="removeEntries">True to remove the reverted entries from the list of replacements, false to leave the list unchanged</param>
		/// <returns>True if the entire network record was removed from the list (due to no remaining replacements for that prefab), false if the prefab remains in the list (has other active replacements)</returns>
		internal override bool RemoveReplacement(BOBNetReplacement replacement, bool removeEntries = true)
		{
			// Safety check.
			if (replacement == null)
			{
				Logging.Error("null replacement passed to AllNetworkReplacement.RemoveReplacement");
				return false;
			}

			// Remove any references to this replacement from all prop handlers.
			NetHandlers.RemoveReplacement(replacement);

			// Remove replacement entry from list of replacements, if we're doing so.
			if (removeEntries)
			{
				// Remove from replacement list.
				ReplacementList(replacement.NetInfo).Remove(replacement);
			}

			// If we got here, we didn't remove any network entries from the list; return false.
			return false;
		}
	}
}
