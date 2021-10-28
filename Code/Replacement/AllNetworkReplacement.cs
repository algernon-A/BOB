using System.Collections.Generic;


namespace BOB
{
	/// <summary>
	/// Class to manage all-network prop and tree replacements.
	/// </summary>
	internal class AllNetworkReplacement : NetworkReplacementBase
	{
		/// <summary>
		/// Constructor - initializes instance reference.
		/// </summary>
		internal AllNetworkReplacement()
		{
			Instance = this;
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
		/// Reverts all active replacements.
		/// </summary>
		internal override void RevertAll()
		{
			// Iterate through each entry in the replacement list.
			foreach (BOBNetReplacement replacement in ConfigurationUtils.CurrentConfig.allNetworkProps)
			{
				// Revert this replacement (but don't remove the entry, as the list is currently immutable while we're iterating through it).
				Revert(replacement, false);
			}
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

			// (Re)set replacement list.
			replacement.references = new List<NetPropReference>();

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
							if (replacement.tree)
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
							replacement.references.Add(CreateReference(netInfo, laneIndex, propIndex));
						}
					}
				}
			}

			// Now, iterate through each entry found and apply the replacement to each one.
			foreach (NetPropReference propReference in replacement.references)
			{
				// Reset any pack replacements first.
				NetworkPackReplacement.Instance.RemoveEntry(propReference.netInfo, replacement.targetInfo, propReference.laneIndex, propReference.propIndex);

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
	}
}
