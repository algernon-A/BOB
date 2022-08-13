namespace BOB
{
	using System;
	using System.Collections.Generic;
	using AlgernonCommons;

	/// <summary>
	/// Base class for building replacement.
	/// </summary>
	internal abstract class BuildingReplacementBase
	{
		/// <summary>
		/// Returns the config file list of building elements relevant to the current replacement type.
		/// </summary>
		protected abstract List<BOBBuildingElement> BuildingElementList { get; }


		/// <summary>
		/// The priority level of this replacmeent type.
		/// </summary>
		protected abstract ReplacementPriority ThisPriority { get; }


		/// <summary>
		/// Reverts all active replacements.
		/// </summary>
		internal virtual void RevertAll()
		{
			// Iterate through each entry in the replacement list.
			foreach (BOBBuildingElement buildingElement in BuildingElementList)
			{
				foreach (BOBBuildingReplacement replacement in buildingElement.replacements)
				{
					// Remove any references to this replacement from all building handlers.
					BuildingHandlers.RemoveReplacement(replacement);
				}
			}

			// Clear configuration file entry.
			BuildingElementList.Clear();
		}


		/// <summary>
		/// Applies a new (or updated) replacement.
		/// </summary>
		/// <param name="buildingInfo">Targeted building prefab</param>
		/// <param name="targetInfo">Targeted (original) prop prefab</param>
		/// <param name="replacementInfo">Replacment prop prefab</param>
		/// <param name="propIndex">Prop index to apply replacement to (ignored)</param>
		/// <param name="angle">Replacment prop angle adjustment</param>
		/// <param name="offsetX">Replacment X position offset</param>
		/// <param name="offsetY">Replacment Y position offset</param>
		/// <param name="offsetZ">Replacment Z position offset</param>
		/// <param name="probability">Replacement probability</param>
		/// <param name="customHeight">Replacement custom height flag</param>
		/// <param name="existingReplacement">Existing replacement record (null if none)</param>
		internal void Replace(BuildingInfo buildingInfo, PrefabInfo targetInfo, PrefabInfo replacementInfo, int propIndex, float angle, float offsetX, float offsetY, float offsetZ, int probability, bool customHeight, BOBBuildingReplacement existingReplacement)
		{
			// Null checks.
			if (targetInfo?.name == null || replacementInfo?.name == null)
			{
				return;
			}

			// Was an existing replacement provided?
			BOBBuildingReplacement thisReplacement = existingReplacement;
			if (thisReplacement == null)
			{
				// No existing replacement was provided - try to find an existing match.
				thisReplacement = FindReplacement(buildingInfo, propIndex, targetInfo);

				// If a match wasn't found, create a new replacement entry.
				if (thisReplacement == null)
				{
					thisReplacement = new BOBBuildingReplacement
					{
						parentInfo = buildingInfo,
						target = targetInfo.name,
						targetInfo = targetInfo,
						propIndex = propIndex
					};
					ReplacementEntry(buildingInfo).Add(thisReplacement);
				}
			}

			// Add/replace replacement data.
			thisReplacement.isTree = targetInfo is TreeInfo;
			thisReplacement.angle = angle;
			thisReplacement.offsetX = offsetX;
			thisReplacement.offsetY = offsetY;
			thisReplacement.offsetZ = offsetZ;
			thisReplacement.probability = probability;
			thisReplacement.customHeight = customHeight;

			// Record replacement prop.
			thisReplacement.replacementInfo = replacementInfo;
			thisReplacement.Replacement = replacementInfo.name;

			// Apply replacement.
			ApplyReplacement(thisReplacement);
		}

		/// <summary>
		/// Deserialises a building element list.
		/// </summary>
		/// <param name="elementList">Element list to deserialise</param>
		internal void Deserialize(List<BOBBuildingElement> elementList)
		{
			// Iterate through each element in the provided list.
			foreach (BOBBuildingElement element in elementList)
			{
				// Try to find building prefab.
				element.prefab = PrefabCollection<BuildingInfo>.FindLoaded(element.building);

				// Don't bother deserializing further if the building info wasn't found.
				if (element.BuildingInfo != null)
				{
					Deserialize(element.BuildingInfo, element.replacements);
				}
			}
		}


		/// <summary>
		/// Applies a replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to apply</param>
		protected abstract void ApplyReplacement(BOBBuildingReplacement replacement);


		/// <summary>
		/// Finds any existing replacement relevant to the provided arguments.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <param name="propIndex">Prop index</param>
		/// <param name="targetInfo">Target prop/tree prefab</param>
		/// <returns>Existing replacement entry, if one was found, otherwise null</returns>
		protected abstract BOBBuildingReplacement FindReplacement(BuildingInfo buildingInfo, int propIndex, PrefabInfo targetInfo);


		/// <summary>
		/// Gets the relevant replacement list entry from the active configuration file, if any.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <returns>Replacement list for the specified building prefab (null if none)</returns>
		protected virtual List<BOBBuildingReplacement> ReplacementList(BuildingInfo buildingInfo) => BuildingElement(buildingInfo)?.replacements;


		/// <summary>
		/// Gets the relevant building replacement list entry from the active configuration file, creating a new building entry if none already exists.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <returns>Replacement list for the specified building prefab</returns>
		protected virtual List<BOBBuildingReplacement> ReplacementEntry(BuildingInfo buildingInfo)
		{
			// Get existing entry for this building.
			BOBBuildingElement thisBuilding = BuildingElement(buildingInfo);

			// If no existing entry, create a new one.
			if (thisBuilding == null)
			{
				thisBuilding = new BOBBuildingElement(buildingInfo);
				BuildingElementList.Add(thisBuilding);
			}

			// Return replacement list from this entry.
			return thisBuilding.replacements;
		}


		/// <summary>
		/// Removes a replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to remove</param>
		/// <param name="removeEntries">True to remove the removed entries from the list of replacements, false to leave the list unchanged</param>
		/// <returns>True if the entire building record was removed from the list (due to no remaining replacements for that prefab), false if the prefab remains in the list (has other active replacements)</returns>
		internal virtual bool RemoveReplacement(BOBBuildingReplacement replacement, bool removeEntries = true)
		{
			// Safety check.
			if (replacement == null)
			{
				Logging.Error("null replacement passed to BuildingReplacementBase.RemoveReplacement");
				return false;
			}

			// Remove all active replacement references.
			BuildingHandlers.RemoveReplacement(replacement);

			// Remove replacement entry from list of replacements, if we're doing so.
			if (removeEntries)
			{
				// Remove from replacement list.
				ReplacementList(replacement.BuildingInfo).Remove(replacement);

				// See if we've got a parent building element record, and if so, if it has any remaining replacement entries.
				BOBBuildingElement thisElement = BuildingElement(replacement.BuildingInfo);
				if (thisElement != null && (thisElement.replacements == null || thisElement.replacements.Count == 0))
				{
					// No replacement entries left - delete entire building entry and return true to indicate that we've done so.
					BuildingElementList.Remove(thisElement);
					return true;
				}
			}

			// If we got here, we didn't remove any building entries from the list; return false.
			return false;
		}


		/// <summary>
		/// Returns the configuration file record for the specified building prefab.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <returns>Replacement record for the specified building prefab (null if none)</returns>
		protected BOBBuildingElement BuildingElement(BuildingInfo buildingInfo) => buildingInfo == null ? null : BuildingElementList?.Find(x => x.BuildingInfo == buildingInfo);


		/// <summary>
		/// Deserialises a replacement list.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <param name="elementList">Replacement list to deserialise</param>
		protected void Deserialize(BuildingInfo buildingInfo, List<BOBBuildingReplacement> replacementList)
		{
			// Iterate through each element in the provided list.
			foreach (BOBBuildingReplacement replacement in replacementList)
			{
				try
				{
					// Assign building info.
					replacement.parentInfo = buildingInfo;

					// Try to find target prefab.
					replacement.targetInfo = replacement.isTree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacement.target) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacement.target);

					// Try to find replacement prefab.
					replacement.replacementInfo = ConfigurationUtils.FindReplacementPrefab(replacement.Replacement, replacement.isTree);

					// Try to apply the replacement.
					ApplyReplacement(replacement);
				}
				catch (Exception e)
				{
					// Don't let a single failure stop us.
					Logging.LogException(e, "exception deserializing building replacement");
				}
			}
		}
	}
}