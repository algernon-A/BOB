namespace BOB
{
	using System.Collections.Generic;
	using AlgernonCommons;

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
		/// The priority level of this replacmeent type.
		/// </summary>
		protected override ReplacementPriority ThisPriority => ReplacementPriority.IndividualReplacement;


		/// <summary>
		/// Finds any existing replacement relevant to the provided arguments.
		/// </summary>
		/// <param name="netInfo">Network ifno</param>
		/// <param name="laneIndex">Lane index</param>
		/// <param name="propIndex">Prop index</param>
		/// <param name="targetInfo">Target prop/tree prefab</param>
		/// <returns>Existing replacement entry, if one was found, otherwise null</returns>
		protected override BOBNetReplacement FindReplacement(NetInfo netInfo, int laneIndex, int propIndex, PrefabInfo targetInfo) =>
			ReplacementList(netInfo)?.Find(x => x.laneIndex == laneIndex && x.propIndex == propIndex);


		/// <summary>
		/// Applies a replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to apply</param>
		protected override void ApplyReplacement(BOBNetReplacement replacement)
		{
			// Don't do anything if prefabs can't be found.
			if (replacement?.targetInfo == null || replacement.replacementInfo == null || replacement.NetInfo?.m_lanes == null)
			{
				return;
			}

			// Check lane index.
			if (replacement.laneIndex < 0 || replacement.laneIndex >= replacement.NetInfo.m_lanes.Length)
			{
				Logging.Message("ignoring invalid individual network replacement lane index ", replacement.laneIndex, " for network ", replacement.NetInfo.name);
				return;
			}

			// Check lane record.
			NetInfo.Lane thisLane = replacement.NetInfo.m_lanes[replacement.laneIndex];
			if (thisLane == null)
			{
				Logging.Message("ignoring invalid individual network replacement lane index ", replacement.laneIndex, " for network ", replacement.NetInfo.name);
				return;
			}

			// Check prop index.
			if (thisLane.m_laneProps?.m_props == null || replacement.propIndex < 0 || replacement.propIndex >= thisLane.m_laneProps.m_props.Length)
			{
				Logging.Message("ignoring invalid individual network replacement prop index ", replacement.propIndex, " for network ", replacement.NetInfo.name);
				return;
			}

			// Don't apply replacement if this is an added prop.
			if (AddedNetworkProps.Instance.IsAdded(replacement.NetInfo, replacement.laneIndex, replacement.propIndex))
			{
				return;
			}

			// Check prop.
			NetLaneProps.Prop thisLaneProp = thisLane.m_laneProps.m_props[replacement.propIndex];
			if (thisLaneProp == null)
			{
				return;
			}

			// Set the new replacement.
			NetHandlers.GetOrAddHandler(replacement.NetInfo, thisLane, replacement.propIndex).SetReplacement(replacement, ThisPriority);
		}
	}
}