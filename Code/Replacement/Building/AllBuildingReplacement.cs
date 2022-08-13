using System.Collections.Generic;


namespace BOB
{
	/// <summary>
	/// Class to manage all-builing prop and tree replacements.
	/// </summary>
	internal class AllBuildingReplacement : BuildingReplacementBase
	{
		/// <summary>
		/// Constructor - initializes instance reference and replacement dictionary.
		/// </summary>
		internal AllBuildingReplacement()
		{
			Instance = this;
		}

		/// <summary>
		/// Instance reference.
		/// </summary>
		internal static AllBuildingReplacement Instance { get; private set; }

		/// <summary>
		/// Returns the config file list of building elements relevant to the current replacement type.
		/// </summary>
		protected override List<BOBBuildingElement> BuildingElementList => null;


		/// <summary>
		/// The priority level of this replacmeent type.
		/// </summary>
		protected override ReplacementPriority ThisPriority => ReplacementPriority.AllReplacement;


		/// <summary>
		/// Finds any existing replacement relevant to the provided arguments.
		/// </summary>
		/// <param name="buildingInfo">Building prefab (ignored)</param>
		/// <param name="propIndex">Prop index (ignored)</param>
		/// <param name="targetInfo">Target prop/tree prefab</param>
		/// <returns>Existing replacement entry, if one was found, otherwise null</returns>
		protected override BOBBuildingReplacement FindReplacement(BuildingInfo buildingInfo, int propIndex, PrefabInfo targetInfo) =>
			ReplacementList(buildingInfo)?.Find(x => x.targetInfo == targetInfo);


		/// <summary>
		/// Reverts all active replacements.
		/// </summary>
		internal override void RevertAll()
		{
			foreach (BOBBuildingReplacement replacement in ReplacementList(null))
			{
				// Remove any references to this replacement from all building handlers.
				BuildingHandlers.RemoveReplacement(replacement);
			}

			// Clear configuration file.
			ReplacementList(null).Clear();
		}


		/// <summary>
		/// Deserialises a building element list.
		/// </summary>
		/// <param name="elementList">Element list to deserialise</param>
		internal void Deserialize(List<BOBBuildingReplacement> elementList) => Deserialize(null, elementList);


		/// <summary>
		/// Applies a replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to apply</param>
		protected override void ApplyReplacement(BOBBuildingReplacement replacement)
        {
			// Don't do anything if prefabs can't be found.
			if (replacement?.targetInfo == null || replacement.replacementInfo == null)
            {
				return;
            }

			// Iterate through each loaded building and record props to be replaced.
			for (int i = 0; i < PrefabCollection<BuildingInfo>.LoadedCount(); ++i)
			{
				// Get local reference.
				BuildingInfo buildingInfo = PrefabCollection<BuildingInfo>.GetLoaded((uint)i);

				// If no props in this building, skip it and go to the next one.
				if (buildingInfo?.m_props == null)
				{
					continue;
				}

				// Iterate through each prop in building.
				for (int propIndex = 0; propIndex < buildingInfo.m_props.Length; ++propIndex)
				{
					// Local reference.
					BuildingInfo.Prop thisBuildingProp = buildingInfo.m_props[propIndex];

					// If invalid entry, skip this one.
					if (thisBuildingProp == null)
					{
						continue;
					}

					// Note current props.
					TreeInfo thisTree = thisBuildingProp.m_tree;
					PropInfo thisProp = thisBuildingProp.m_prop;

					// Get any active handler.
					BuildingPropHandler handler = BuildingHandlers.GetHandler(buildingInfo, propIndex);
					if (handler != null)
					{
						// Active reference found - use original values for checking eligibility (instead of currently active values).
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
							handler = BuildingHandlers.GetOrAddHandler(buildingInfo, propIndex);
						}

						// Set the new replacement.
						handler.SetReplacement(replacement, ThisPriority);
					}
				}
			}
		}


		/// <summary>
		/// Gets the relevant building replacement list entry from the active configuration file, if any.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <returns>Replacement list for the specified building prefab (null if none)</returns>
		protected override List<BOBBuildingReplacement> ReplacementList(BuildingInfo buildingInfo) => ConfigurationUtils.CurrentConfig.allBuildingProps;


		/// <summary>
		/// Gets the relevant building replacement list entry from the active configuration file, creating a new building entry if none already exists.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <returns>Replacement list for the specified building prefab</returns>
		protected override List<BOBBuildingReplacement> ReplacementEntry(BuildingInfo buildingInfo) => ReplacementList(buildingInfo);


		/// <summary>
		/// Removes a replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to revert</param>
		/// <param name="removeEntries">True to remove the reverted entries from the list of replacements, false to leave the list unchanged</param>
		/// <returns>Always false (all-building entries never remove parent network elements)</returns>
		internal override bool RemoveReplacement(BOBBuildingReplacement replacement, bool removeEntries = true)
		{
			// Safety check.
			if (replacement == null)
			{
				Logging.Error("null replacement passed to AllBuildingReplacement.RemoveReplacement");
				return false;
			}

			// Remove any references to this replacement from all prop handlers.
			BuildingHandlers.RemoveReplacement(replacement);

			// Remove replacement entry from list of replacements, if we're doing so.
			if (removeEntries)
			{
				// Remove from replacement list.
				ReplacementList(replacement.BuildingInfo).Remove(replacement);
			}

			// If we got here, we didn't remove any network entries from the list; return false.
			return false;
		}
	}
}
