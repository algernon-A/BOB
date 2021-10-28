using System.Collections.Generic;


namespace BOB
{

	/// <summary>
	/// Class to manage individual network prop and tree replacements.
	/// </summary>
	internal class IndividualNetworkReplacement : NetworkReplacementBase
	{
		/// <summary>
		/// Constructor - initializes instance reference.
		/// </summary>
		internal IndividualNetworkReplacement()
		{
			Instance = this;
		}


		/// <summary>
		/// Instance reference.
		/// </summary>
		internal static IndividualNetworkReplacement Instance { get; private set; }


		/// <summary>
		/// Returns the config file list of network elements relevant to the current replacement type.
		/// </summary>
		protected override List<BOBNetworkElement> NetworkElementList => ConfigurationUtils.CurrentConfig.indNetworks;


		/// <summary>
		/// Retrieves a currently-applied replacement entry for the given network, lane and prop index.
		/// </summary>
		/// <param name="networkInfo">Network prefab</param>
		/// <param name="targetInfo">Target prop/tree prefab</param>
		/// <param name="laneIndex">Lane number</param>
		/// <param name="propIndex">Prop index number</param>
		/// <returns>Currently-applied individual network replacement (null if none)</returns>
		internal override BOBNetReplacement EligibileReplacement(NetInfo netInfo, PrefabInfo targetInfo, int laneIndex, int propIndex) => ReplacementList(netInfo)?.Find(x => x.laneIndex == laneIndex && x.propIndex == propIndex);


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

			// Check lane index.
			NetInfo.Lane thisLane = replacement.netInfo.m_lanes[replacement.laneIndex];
			if (thisLane == null)
			{
				return;
			}

			// Check prop index.
			NetLaneProps.Prop thisLaneProp = thisLane.m_laneProps.m_props[replacement.propIndex];
			if (thisLaneProp == null)
			{
				return;
			}

			// Reset any pack, network, or all-network replacements first.
			NetworkReplacement.Instance.RemoveEntry(replacement.netInfo, replacement.targetInfo, replacement.laneIndex, replacement.propIndex);
			AllNetworkReplacement.Instance.RemoveEntry(replacement.netInfo, replacement.targetInfo, replacement.laneIndex, replacement.propIndex);
			NetworkPackReplacement.Instance.RemoveEntry(replacement.netInfo, replacement.targetInfo, replacement.laneIndex, replacement.propIndex);

			// Create replacment entry.
			NetPropReference newPropReference = CreateReference(replacement.netInfo, replacement.laneIndex, replacement.propIndex);

			// Reset replacement list to be only our new replacement entry.
			replacement.references = new List<NetPropReference> { newPropReference };

			// Apply the replacement.
			ReplaceProp(replacement, newPropReference);
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
			// Restore any network replacement.
			if (!NetworkReplacement.Instance.Restore(netInfo, targetInfo, laneIndex, propIndex))
			{
				// No network restoration occured - restore any all-network replacement.
				if (!AllNetworkReplacement.Instance.Restore(netInfo, targetInfo, laneIndex, propIndex))
				{
					// No all-network restoration occured - restore any pack replacement.
					NetworkPackReplacement.Instance.Restore(netInfo, targetInfo, laneIndex, propIndex);
				}
			}
		}
	}
}