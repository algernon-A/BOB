namespace BOB
{
	using System.Collections.Generic;
	using AlgernonCommons;

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
		/// The priority level of this replacmeent type.
		/// </summary>
		protected override ReplacementPriority ThisPriority => ReplacementPriority.GroupedReplacement;


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
		/// Applies a replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to apply</param>
		protected override void ApplyReplacement(BOBNetReplacement replacement)
		{
			// Don't do anything if prefabs can't be found.
			if (replacement?.targetInfo == null || replacement.replacementInfo == null || replacement.NetInfo?.m_lanes == null)
			{
				Logging.Error("null value passed to NetworkReplacement.ApplyReplacement");
				return;
			}

			// (Re)set replacement list.
			replacement.references = new List<NetPropReference>();

			// Iterate through each lane.
			for (int laneIndex = 0; laneIndex < replacement.NetInfo.m_lanes.Length; ++laneIndex)
			{
				// Local references.
				NetInfo.Lane thisLane = replacement.NetInfo.m_lanes[laneIndex];
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
							handler = NetHandlers.GetOrAddHandler(replacement.NetInfo, thisLane, propIndex);
						}

						// Set the new replacement.
						handler.SetReplacement(replacement, ThisPriority);
					}
				}
			}
		}
	}
}
