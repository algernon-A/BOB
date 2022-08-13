using System.Collections.Generic;


namespace BOB
{
	/// <summary>
	/// Class to manage building prop and tree replacements.
	/// </summary>
	internal class BuildingReplacement : BuildingReplacementBase
	{
		/// <summary>
		/// Constructor - initializes instance reference.
		/// </summary>
		internal BuildingReplacement()
		{
			Instance = this;
		}


		/// <summary>
		/// Instance reference.
		/// </summary>
		internal static BuildingReplacement Instance { get; private set; }


		/// <summary>
		/// Returns the config file list of building elements relevant to the current replacement type.
		/// </summary>
		protected override List<BOBBuildingElement> BuildingElementList => ConfigurationUtils.CurrentConfig.buildings;


		/// <summary>
		/// The priority level of this replacmeent type.
		/// </summary>
		protected override ReplacementPriority ThisPriority => ReplacementPriority.GroupedReplacement;


		/// <summary>
		/// Finds any existing replacement relevant to the provided arguments.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <param name="propIndex">Prop index (ignored)</param>
		/// <param name="targetInfo">Target prop/tree prefab</param>
		/// <returns>Existing replacement entry, if one was found, otherwise null</returns>
		protected override BOBBuildingReplacement FindReplacement(BuildingInfo buildingInfo, int propIndex, PrefabInfo targetInfo) =>
			ReplacementList(buildingInfo)?.Find(x => x.targetInfo == targetInfo);


		/// <summary>
		/// Applies a replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to apply</param>
		protected override void ApplyReplacement(BOBBuildingReplacement replacement)
		{
			// Don't do anything if prefabs can't be found, or if building prefab has no prop array.
			if (replacement?.targetInfo == null || replacement.replacementInfo == null || replacement.BuildingInfo?.m_props == null)
			{
				Logging.Error("null value passed to BuildingReplacement.ApplyReplacement");
				return;
			}

			// Iterate through each prop in building.
			for (int propIndex = 0; propIndex < replacement.BuildingInfo.m_props.Length; ++propIndex)
			{
				// Local reference.
				BuildingInfo.Prop thisBuildingProp = replacement.BuildingInfo.m_props[propIndex];
				
				// If invalid entry, skip this one.
				if (thisBuildingProp == null)
				{
					continue;
				}
				
				// Note current props.
				TreeInfo thisTree = thisBuildingProp.m_tree;
				PropInfo thisProp = thisBuildingProp.m_prop;

				// Get any active handler.
				BuildingPropHandler handler = BuildingHandlers.GetHandler(replacement.BuildingInfo, propIndex);
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
						handler = BuildingHandlers.GetOrAddHandler(replacement.BuildingInfo, propIndex);
					}

					// Set the new replacement.
					handler.SetReplacement(replacement, ThisPriority);
				}
			}
		}
	}
}
