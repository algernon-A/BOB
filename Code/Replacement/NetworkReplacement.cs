using System.Collections.Generic;


namespace BOB
{
	/// <summary>
	/// Class to manage network prop and tree replacements.
	/// </summary>
	internal class NetworkReplacement : NetworkReplacementBase
	{
		/// <summary>
		/// Constructor - initializes instance reference.
		/// </summary>
		internal NetworkReplacement()
		{
			Instance = this;
		}


		/// <summary>
		/// Instance reference.
		/// </summary>
		internal static NetworkReplacement Instance { get; private set; }


		/// <summary>
		/// Returns the config file list of network elements relevant to the current replacement type.
		/// </summary>
		protected override List<BOBNetworkElement> NetworkElementList => ConfigurationUtils.CurrentConfig.networks;


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
		/// Applies a replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to apply</param>
		protected override void ApplyReplacement(BOBNetReplacement replacement)
		{
			// Don't do anything if prefabs can't be found.
			if (replacement?.targetInfo == null || replacement.replacementInfo == null || replacement.netInfo == null)
			{
				return;
			}

			// (Re)set replacement list.
			replacement.references = new List<NetPropReference>();

			// Iterate through each lane.
			for (int laneIndex = 0; laneIndex < replacement.netInfo.m_lanes.Length; ++laneIndex)
			{
				// Local reference.
				NetLaneProps.Prop[] theseLaneProps = replacement.netInfo.m_lanes[laneIndex]?.m_laneProps?.m_props;

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

					// Check for any currently active individual network prop replacement.
					if (IndividualNetworkReplacement.Instance.ActiveReplacement(replacement.netInfo, laneIndex, propIndex) != null)
					{
						// Active individual network prop replacement; skip this one.
						continue;
					}

					// Reset any pack or all-network replacements first.
					AllNetworkReplacement.Instance.RemoveEntry(replacement.netInfo, replacement.targetInfo, replacement.laneIndex, replacement.propIndex);
					NetworkPackReplacement.Instance.RemoveEntry(replacement.netInfo, replacement.targetInfo, replacement.laneIndex, replacement.propIndex);


					// Check for any existing all-network or pack replacement.
					PrefabInfo thisProp = NetworkPackReplacement.Instance.ActiveReplacement(replacement.netInfo, laneIndex, propIndex)?.targetInfo ?? AllNetworkReplacement.Instance.ActiveReplacement(replacement.netInfo, laneIndex, propIndex)?.targetInfo;
					if (thisProp == null)
					{
						// No active replacement; use current PropInfo.
						if (replacement.targetInfo is PropInfo)
						{
							thisProp = thisLaneProp.m_finalProp;
						}
						else
						{
							thisProp = thisLaneProp.m_finalTree;
						}
					}

					// See if this prop matches our replacement.
					if (thisProp != null && thisProp == replacement.targetInfo)
					{
						// Match!  Add reference data to the list.
						replacement.references.Add(CreateReference(replacement.netInfo, laneIndex, propIndex));
					}
				}
			}

			// Now, iterate through each entry found.
			foreach (NetPropReference propReference in replacement.references)
			{
				// Reset any pack or all-network replacements first.
				AllNetworkReplacement.Instance.RemoveEntry(propReference.netInfo, replacement.targetInfo, propReference.laneIndex, propReference.propIndex);
				NetworkPackReplacement.Instance.RemoveEntry(propReference.netInfo, replacement.targetInfo, propReference.laneIndex, propReference.propIndex);

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
		protected override void RestoreLower(NetInfo netInfo, PrefabInfo targetInfo, int laneIndex, int propIndex)
		{
			// Restore any all-network replacement.
			if (!AllNetworkReplacement.Instance.Restore(netInfo, targetInfo, laneIndex, propIndex))
			{
				// No all-network restoration occured - restore any pack replacement.
				NetworkPackReplacement.Instance.Restore(netInfo, targetInfo, laneIndex, propIndex);
			}
		}
	}
}
