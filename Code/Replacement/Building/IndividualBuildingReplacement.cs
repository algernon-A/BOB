using System.Collections.Generic;


namespace BOB
{
	/// <summary>
	/// Class to manage individual building prop and tree replacements.
	/// </summary>
	internal class IndividualBuildingReplacement : BuildingReplacementBase
	{
		/// <summary>
		/// Constructor - initializes instance reference.
		/// </summary>
		internal IndividualBuildingReplacement()
		{
			Instance = this;
		}


		/// <summary>
		/// Instance reference.
		/// </summary>
		internal static IndividualBuildingReplacement Instance { get; private set; }


		/// <summary>
		/// Returns the config file list of building elements relevant to the current replacement type.
		/// </summary>
		protected override List<BOBBuildingElement> BuildingElementList => ConfigurationUtils.CurrentConfig.indBuildings;


		/// <summary>
		/// The priority level of this replacmeent type.
		/// </summary>
		protected override ReplacementPriority ThisPriority => ReplacementPriority.IndividualReplacement;


		/// <summary>
		/// Finds any existing replacement relevant to the provided arguments.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <param name="propIndex">Prop index</param>
		/// <param name="targetInfo">Target prop/tree prefab (ignored)</param>
		/// <returns>Existing replacement entry, if one was found, otherwise null</returns>
		protected override BOBBuildingReplacement FindReplacement(BuildingInfo buildingInfo, int propIndex, PrefabInfo targetInfo) => 
			ReplacementList(buildingInfo)?.Find(x => x.propIndex == propIndex);


		/// <summary>
		/// Applies a replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to apply</param>
		protected override void ApplyReplacement(BOBBuildingReplacement replacement)
		{
			// Don't do anything if prefabs can't be found.
			if (replacement?.targetInfo == null || replacement.replacementInfo == null || replacement.BuildingInfo == null)
			{
				return;
			}
			
			// Check index bounds.
			if (replacement.BuildingInfo.m_props == null || replacement.propIndex >= replacement.BuildingInfo.m_props.Length)
            {
				Logging.Message("ignoring invalid individual building replacement index ", replacement.propIndex, " for building ", replacement.BuildingInfo.name);
				return;
			}

			// Don't apply replacement if this is an added prop.
			if (AddedBuildingProps.Instance.IsAdded(replacement.BuildingInfo, replacement.propIndex))
			{
				return;
			}

			// Check prop index.
			BuildingInfo.Prop thisProp = replacement.BuildingInfo.m_props[replacement.propIndex];
			if (thisProp == null)
            {
				return;
			}

			// Set the new replacement.
			BuildingHandlers.GetOrAddHandler(replacement.BuildingInfo, replacement.propIndex).SetReplacement(replacement, ThisPriority);
		}
	}
}
