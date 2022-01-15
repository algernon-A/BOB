using System;
using System.Collections.Generic;
using UnityEngine;


namespace BOB
{
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
		/// Retrieves any currently-applied replacement entry that affects the given building and target prefab and prop index.
		/// </summary>
		/// <param name="buildingInfo">Targeted building prefab</param>
		/// <param name="targetInfo">Target prop/tree prefab</param>
		/// <param name="propIndex">Target prop/tree index</param>
		/// <returns>Currently-applied replacement (null if none)</returns>
		internal abstract BOBBuildingReplacement EligibileReplacement(BuildingInfo buildingInfo, PrefabInfo targetInfo, int propIndex);


		/// <summary>
		/// Retuns the list of active prop references for the given replacement value(s).
		/// </summary>
		/// <param name="buildingInfo">Targeted building prefab</param>
		/// <param name="targetInfo">Targeted (original) prop prefab</param>
		/// <param name="propIndex">Targeted prop index (in lanme)</param>
		/// <returns>List of active prop references for the given replacment values (null if none)</returns>
		internal virtual List<BuildingPropReference> ReferenceList(BuildingInfo buildingInfo, PrefabInfo targetInfo, int propIndex) => EligibileReplacement(buildingInfo, targetInfo, propIndex)?.references;


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
					// Revert all references for this replacement.
					RevertReferences(replacement.BuildingInfo, replacement.references);
				}
			}

			// Clear configuration file entry.
			BuildingElementList.Clear();
		}


		/// <summary>
		/// Checks if there's a currently active replacement applied to the given building and prop index, and if so, returns the replacement record.
		/// </summary>
		/// <param name="buildingInfo">Building prefab to check</param>
		/// <param name="propIndex">Prop index to check</param>
		/// <returns>Replacement record if a replacement is currently applied, null if no replacement is currently applied</returns>
		internal virtual BOBBuildingReplacement ActiveReplacement(BuildingInfo buildingInfo, int propIndex)
		{
			// See if we've got a replacement entry for this building.
			List<BOBBuildingReplacement> replacementList = ReplacementList(buildingInfo);
			if (replacementList == null)
			{
				// No entry - return null.
				return null;
			}

			// Iterate through each replacement record for this building.
			foreach (BOBBuildingReplacement buildingReplacement in replacementList)
			{
				// Iterate through each prop reference in this record. 
				foreach (BuildingPropReference propRef in buildingReplacement.references)
				{
					// Check for building (due to all- replacement) and index match.
					if (propRef.buildingInfo == buildingInfo && propRef.propIndex == propIndex)
					{
						// Match!  Return the replacement record.
						return buildingReplacement;
					}
				}
			}

			// If we got here, no entry was found - return null to indicate no active replacement.
			return null;
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
		internal void Replace(BuildingInfo buildingInfo, PrefabInfo targetInfo, PrefabInfo replacementInfo, int propIndex, float angle, float offsetX, float offsetY, float offsetZ, int probability, bool customHeight)
		{
			// Make sure that target and replacement are the same type before doing anything.
			if (targetInfo?.name == null || replacementInfo?.name == null || (targetInfo is TreeInfo && !(replacementInfo is TreeInfo)) || (targetInfo is PropInfo) && !(replacementInfo is PropInfo))
			{
				return;
			}

			// Revert any current replacement entry for this prop.
			Revert(buildingInfo, targetInfo, propIndex, true);

			// Get configuration file building list entry.
			List<BOBBuildingReplacement> replacementsList = ReplacementEntry(buildingInfo);

			// Get current replacement after reversion above.
			BOBBuildingReplacement thisReplacement = EligibileReplacement(buildingInfo, targetInfo, propIndex);

			// Create new replacement list entry if none already exists.
			if (thisReplacement == null)
			{
				thisReplacement = new BOBBuildingReplacement
				{
					parentInfo = buildingInfo,
					target = targetInfo.name,
					targetInfo = targetInfo,
					propIndex = propIndex
				};
				replacementsList.Add(thisReplacement);
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
		/// Reverts a replacement.
		/// </summary>
		/// <param name="buildingInfo">Targeted building</param>
		/// <param name="targetInfo">Targeted (original) tree/prop prefab</param>
		/// <param name="propIndex">Targeted (original) tree/prop index</param>
		/// <param name="removeEntries">True to remove the reverted entries from the list of replacements, false to leave the list unchanged</param>
		internal void Revert(BuildingInfo buildingInfo, PrefabInfo targetInfo, int propIndex, bool removeEntries) => Revert(EligibileReplacement(buildingInfo, targetInfo, propIndex), removeEntries);


		/// <summary>
		/// Restores a replacement, if any (e.g. after a higher-priority replacement has been reverted).
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <param name="targetInfo">Target prop info</param>
		/// <param name="propIndex">Prop index</param>
		/// <returns>True if a restoration was made, false otherwise</returns>
		internal bool Restore(BuildingInfo buildingInfo, PrefabInfo targetInfo, int propIndex)
		{
			// See if we have a relevant replacement record.
			BOBBuildingReplacement thisReplacement = EligibileReplacement(buildingInfo, targetInfo, propIndex);
			if (thisReplacement != null)
			{
				// Yes - add reference data to the list.
				BuildingPropReference newReference = CreateReference(buildingInfo, targetInfo, propIndex, thisReplacement.isTree);
				AddReference(thisReplacement, newReference);

				// Apply replacement and return true to indicate restoration.
				ReplaceProp(thisReplacement, newReference);

				return true;
			}

			// If we got here, no restoration was made.
			return false;
		}


		/// <summary>
		/// Unapplies a particular replacement instance to defer to a higher-priority replacement.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <param name="targetInfo">Target prop info</param>
		/// <param name="propIndex">Prop index</param>
		internal void RemoveEntry(BuildingInfo buildingInfo, PrefabInfo targetInfo,int propIndex)
		{
			// Check to see if we have an entry for this prefab and target.
			List<BuildingPropReference> referenceList = ReferenceList(buildingInfo, targetInfo, propIndex);
			if (referenceList != null)
			{
				// Got an active reference list; create a variable to store any matching reference for later removal.
				BuildingPropReference thisPropReference = null;

				// Iterate through each recorded prop reference.
				foreach (BuildingPropReference propReference in referenceList)
				{
					// Look for a building and index match.
					if (propReference.buildingInfo == buildingInfo && propReference.propIndex == propIndex)
					{
						// Got a match!  Revert instance.
						RevertReference(propReference);

						// Record the matching reference and stop iterating - we're done here.
						thisPropReference = propReference;
						break;
					}
				}

				// Remove replacement if one was found.
				if (thisPropReference != null)
				{
					referenceList.Remove(thisPropReference);
				};
			}
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
		/// Restores any replacements from lower-priority replacements after a reversion.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <param name="targetInfo">Target prop info</param>
		/// <param name="propIndex">Prop index</param>
		protected abstract void RestoreLower(BuildingInfo buildingInfo, PrefabInfo targetInfo, int propIndex);


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
		/// Reverts a replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to revert</param>
		/// <param name="removeEntries">True to remove the reverted entries from the list of replacements, false to leave the list unchanged</param>
		/// <returns>True if the entire building record was removed from the list (due to no remaining replacements for that prefab), false if the prefab remains in the list (has other active replacements)</returns>
		protected virtual bool Revert(BOBBuildingReplacement replacement, bool removeEntries)
		{
			// Safety check for calls without any current replacement.
			if (replacement?.targetInfo == null || replacement.references == null)
			{
				return false;
			}

			if (replacement.references != null)
			{
				// Revert all entries in list.
				RevertReferences(replacement.targetInfo, replacement.references);

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
			}

			// If we got here, we didn't remove any building entries from the list; return false.
			return false;
		}


		/// <summary>
		/// Adds the given prop reference to the record for the given replacement.
		/// </summary>
		/// <param name="replacement">Replacement reference</param>
		/// <param name="propReference">Pop reference to store</param>
		protected virtual void AddReference(BOBBuildingReplacement replacement, BuildingPropReference propReference) => replacement.references.Add(propReference);


		/// <summary>
		/// Reverts all prop references in the provided list using the given replacement list and original target prefab.
		/// </summary>
		/// <param name="originalPrefab">Original prop/tree prefab</param>
		/// <param name="references">List of prop references to revert</param>
		protected void RevertReferences(PrefabInfo originalPrefab, List<BuildingPropReference> references)
		{
			// Iterate through each entry in our list.
			foreach (BuildingPropReference propReference in references)
			{
				// Revert entry.
				RevertReference(propReference);

				// Restore any lower-priority replacements.
				RestoreLower(propReference.buildingInfo, originalPrefab, propReference.propIndex);

				// Add building to dirty list.
				BuildingData.DirtyList.Add(propReference.buildingInfo);
			}
		}


		/// <summary>
		/// Creates a new PropReference from the provided building prefab and prop index.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <param name="originalPrefab">Original prop/tree prefab (for later restoration if needed)</param>
		/// <param name="propIndex">Prop index</param>
		/// <param name="isTree">True if this is a tree reference, false if this is a prop reference</param>
		/// <returns>Newly-created reference (null if creation failed)</returns>
		protected BuildingPropReference CreateReference(BuildingInfo buildingInfo, PrefabInfo originalPrefab, int propIndex, bool isTree)
		{
			// Safety checks.
			if (buildingInfo?.m_props != null && propIndex >= 0)
			{
				// Local reference.
				BuildingInfo.Prop thisProp = buildingInfo.m_props[propIndex];

				// Create and return new reference.
				return new BuildingPropReference
				{
					buildingInfo = buildingInfo,
					propIndex = propIndex,
					isTree = isTree,
					originalProp = isTree ? thisProp.m_finalProp : originalPrefab as PropInfo,
					originalTree = isTree ? originalPrefab as TreeInfo : thisProp.m_finalTree,
					radAngle = thisProp.m_radAngle,
					fixedHeight = thisProp.m_fixedHeight,
					position = thisProp.m_position,
					probability = thisProp.m_probability
				};
			}

			// If we got here, something went wrong; return null.
			Logging.Error("invalid argument passed to BuildingReplacementBase.CreateReference");
			return null;
		}


		/// <summary>
		/// Creates a new PropReference from the provided building prefab and prop index.
		/// </summary>
		/// <param name="reference">Referene to revert</param>
		protected void RevertReference(BuildingPropReference reference)
		{
			// Local reference.
			BuildingInfo.Prop thisProp = reference.buildingInfo.m_props[reference.propIndex];
			if (thisProp != null)
			{
				thisProp.m_prop = reference.originalProp;
				thisProp.m_tree = reference.originalTree;
				thisProp.m_radAngle = reference.radAngle;
				thisProp.m_fixedHeight = reference.fixedHeight;
				thisProp.m_position = reference.position;
				thisProp.m_probability = reference.probability;

				// Update building.
				reference.buildingInfo.CheckReferences();
				BuildingData.DirtyList.Add(reference.buildingInfo);
			}
		}


		/// <summary>
		/// Replaces a prop, using a building replacement.
		/// </summary>
		/// <param name="replacement">Building replacement element to apply</param>
		/// <param name="propReference">Individual prop reference to apply to</param>
		protected void ReplaceProp(BOBBuildingReplacement replacement, BuildingPropReference propReference)
		{
			// Convert offset to Vector3.
			Vector3 offset = new Vector3
			{
				x = replacement.offsetX,
				y = replacement.offsetY,
				z = replacement.offsetZ
			};

			// Apply replacement.
			if (replacement.replacementInfo is PropInfo propInfo)
			{
				propReference.buildingInfo.m_props[propReference.propIndex].m_prop = propInfo;
			}
			else if (replacement.replacementInfo is TreeInfo treeInfo)
			{
				propReference.buildingInfo.m_props[propReference.propIndex].m_tree = treeInfo;
			}
			else
			{
				Logging.Error("invalid replacement ", replacement.replacementInfo?.name ?? "null", " passed to BuildingInfo.ReplaceProp");
				return;
			}

			// Set m_fixedHeight.
			propReference.buildingInfo.m_props[propReference.propIndex].m_fixedHeight = replacement.customHeight;

			// Angle and offset.
			propReference.buildingInfo.m_props[propReference.propIndex].m_radAngle = propReference.radAngle + ((replacement.angle * Mathf.PI) / 180f);
			propReference.buildingInfo.m_props[propReference.propIndex].m_position = propReference.position + offset;

			// Probability.
			propReference.buildingInfo.m_props[propReference.propIndex].m_probability = replacement.probability;

			// Update building prop references.
			propReference.buildingInfo.CheckReferences();

			// Add building to dirty list.
			BuildingData.DirtyList.Add(propReference.buildingInfo);
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