using System.Collections.Generic;
using System.Linq;


namespace BOB
{
	/// <summary>
	/// Class to manage building prop and tree replacements.
	/// </summary>
	internal class BuildingReplacement : BuildingReplacementBase
	{
		// Instance reference.
		internal static BuildingReplacement instance;


		/// <summary>
		/// Constructor - initializes instance reference.
		/// </summary>
		internal BuildingReplacement()
		{
			instance = this;
		}


		/// <summary>
		/// Retrieves any currently-applied replacement entry for the given building and target prefab.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <param name="targetInfo">Target prop/tree prefab</param>
		/// <returns>Currently-applied replacement (null if none)</returns>
		internal override BOBBuildingReplacement Replacement(BuildingInfo buildingInfo, PrefabInfo targetInfo) => ConfigurationUtils.CurrentConfig.buildings.Find(x => x.buildingInfo == buildingInfo)?.replacements.Find(x => x.targetInfo == targetInfo);


		/// <summary>
		/// Applies a new (or updated) building replacement.
		/// </summary>
		/// <param name="buildingInfo">Targeted building prefab</param>
		/// <param name="targetInfo">Targeted (original) prop prefab</param>
		/// <param name="replacementInfo">Replacment prop prefab</param>
		/// <param name="targetIndex">Prop index to apply replacement to (ignored)</param>
		/// <param name="angle">Replacment prop angle adjustment</param>
		/// <param name="offsetX">Replacment X position offset</param>
		/// <param name="offsetY">Replacment Y position offset</param>
		/// <param name="offsetZ">Replacment Z position offset</param>
		/// <param name="probability">Replacement probability</param>
		internal override void Replace(BuildingInfo buildingInfo, PrefabInfo targetInfo, PrefabInfo replacementInfo, int _, float angle, float offsetX, float offsetY, float offsetZ, int probability)
		{
			// Make sure that target and replacement are the same type before doing anything.
			if (targetInfo?.name == null || replacementInfo?.name == null || (targetInfo is TreeInfo && !(replacementInfo is TreeInfo)) || (targetInfo is PropInfo) && !(replacementInfo is PropInfo))
			{
				return;
			}

			// Revert any current replacement entry for this prop.
			Revert(buildingInfo, targetInfo, true);

			// Get configuration file building entry record for this building, creating a new one if there's none already.
			BOBBuildingElement thisBuilding = BuildingElement(buildingInfo);
			if (thisBuilding == null)
			{
				thisBuilding = new BOBBuildingElement
				{
					building = buildingInfo.name,
					buildingInfo = buildingInfo,
					replacements = new List<BOBBuildingReplacement>()
				};
				ConfigurationUtils.CurrentConfig.buildings.Add(thisBuilding);
			}

			// Get current replacement after reversion above.
			BOBBuildingReplacement thisReplacement = Replacement(buildingInfo, targetInfo);

			// Create new replacement list entry if none already exists.
			if (thisReplacement == null)
			{
				thisReplacement = new BOBBuildingReplacement
				{
					buildingInfo = buildingInfo,
					target = targetInfo.name,
					targetInfo = targetInfo
				};
				thisBuilding.replacements.Add(thisReplacement);
			}

			// Add/replace dictionary replacement data.
			thisReplacement.tree = targetInfo is TreeInfo;
			thisReplacement.angle = angle;
			thisReplacement.offsetX = offsetX;
			thisReplacement.offsetY = offsetY;
			thisReplacement.offsetZ = offsetZ;
			thisReplacement.probability = probability;

			// Record replacement prop.
			thisReplacement.replacementInfo = replacementInfo;
			thisReplacement.Replacement = replacementInfo.name;

			// Apply replacement.
			ApplyReplacement(thisReplacement);
		}


		/// <summary>
		/// Reverts all active building replacements and re-initialises the master dictionary.
		/// </summary>
		internal override void RevertAll()
		{
			// Iterate through each entry in the replacement list.
			foreach (BOBBuildingElement buildingElement in ConfigurationUtils.CurrentConfig.buildings)
			{
				foreach (BOBBuildingReplacement replacement in buildingElement.replacements)
				{
					// Revert this replacement (but don't remove the entry, as the dictionary is currently immutable while we're iterating through it).
					Revert(replacement, false);
				}
			}

			// Re-initialise the dictionaries.
			Setup();
		}


		/// <summary>
		/// Reverts an all-building replacement.
		/// </summary>
		/// <param name="buildingInfo">Targeted building</param>
		/// <param name="targetInfo">Targeted (original) tree/prop prefab</param>
		/// <param name="removeEntries">True (default) to remove the reverted entries from the list of replacements, false to leave the list unchanged</param>
		internal void Revert(BuildingInfo buildingInfo, PrefabInfo targetInfo, bool removeEntries) => Revert(Replacement(buildingInfo, targetInfo), removeEntries);


		/// <summary>
		/// Removes an entry from the list of building replacements currently applied to building.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <param name="targetInfo">Target prop info</param>
		/// <param name="laneIndex">Lane index</param>
		/// <param name="propIndex">Prop index</param>
		internal void RemoveEntry(BuildingInfo buildingInfo, PrefabInfo targetInfo, int propIndex)
		{
			// Check to see if we have an entry for this prefab and target.
			BOBBuildingReplacement thisReplacement = Replacement(buildingInfo, targetInfo);
			if (thisReplacement != null)
			{
				BuildingPropReference thisPropReference = null;

				// Iterate through each recorded prop reference.
				foreach (BuildingPropReference propReference in thisReplacement.references)
				{
					// Look for a building and index match.
					if (propReference.building == buildingInfo && propReference.propIndex == propIndex)
					{
						// Got a match!  Revert instance.
						if (targetInfo is PropInfo propTarget)
						{
							propReference.building.m_props[propReference.propIndex].m_finalProp = propTarget;
						}
						else
						{
							propReference.building.m_props[propReference.propIndex].m_finalTree = (TreeInfo)targetInfo;
						}
						buildingInfo.m_props[propIndex].m_radAngle = propReference.radAngle;
						buildingInfo.m_props[propIndex].m_position = propReference.postion;
						buildingInfo.m_props[propIndex].m_probability = propReference.probability;

						// Record the matching reference and stop iterating - we're done here.
						thisPropReference = propReference;
						break;
					}
				}

				// Remove replacement if one was found.
				if (thisPropReference != null)
				{
					thisReplacement.references.Remove(thisPropReference);
				}
				return;
			}
		}


		/// <summary>
		/// Checks if there's a currently active building replacement applied to the given building and prop index, and if so, returns the replacement record.
		/// </summary>
		/// <param name="buildingPrefab">Building prefab to check</param>
		/// <param name="propIndex">Prop index to check</param>
		/// <returns>Replacement record if a building replacement is currently applied, null if no building replacement is currently applied</returns>
		internal override BOBBuildingReplacement ActiveReplacement(BuildingInfo buildingInfo, int propIndex)
		{
			// Get the relevant building element record.
			BOBBuildingElement buildingElement = BuildingElement(buildingInfo);
			if (buildingElement?.replacements != null)
			{
				// Iterate through each replacement record for this building.
				foreach (BOBBuildingReplacement buildingReplacement in buildingElement.replacements)
				{
					// Iterate through each prop reference in this record. 
					foreach (BuildingPropReference propRef in buildingReplacement.references)
					{
						// Check for an index match.
						if (propRef.propIndex == propIndex)
						{
							// Match!  Return the replacement record.
							return buildingReplacement;
						}
					}
				}
			}

			// If we got here, no entry was found - return null to indicate no active replacement.
			return null;
		}


		/// <summary>
		/// Deserialises a building element list.
		/// </summary>
		/// <param name="elementList">All-building element list to deserialise</param>
		internal override void Deserialize(List<BOBBuildingElement> elementList)
		{
			// Iterate through each element in the provided list.
			foreach (BOBBuildingElement element in elementList)
			{
				// Try to find building prefab.
				element.buildingInfo = PrefabCollection<BuildingInfo>.FindLoaded(element.building);
				if (element.buildingInfo != null)
				{
					// Iterate through each replacement record in the element.
					foreach (BOBBuildingReplacement replacement in element.replacements)
					{
						replacement.buildingInfo = element.buildingInfo;

						// Try to find target prefab.
						replacement.targetInfo = replacement.tree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacement.target) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacement.target);

						// Try to find replacement prefab.
						replacement.replacementInfo = ConfigurationUtils.FindReplacementPrefab(replacement.Replacement, replacement.tree);

						// Try to apply the replacement.
						ApplyReplacement(replacement);
					}
				}
			}
		}


		/// <summary>
		/// Applies an all-building prop replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to apply</param>
		protected void ApplyReplacement(BOBBuildingReplacement replacement)
		{
			// Don't do anything if prefabs can't be found.
			if (replacement?.targetInfo == null || replacement.replacementInfo == null || replacement.buildingInfo == null)
			{
				return;
			}

			// (Re)set replacement list.
			replacement.references = new List<BuildingPropReference>();

			// Iterate through each prop in building.
			for (int propIndex = 0; propIndex < replacement.buildingInfo.m_props.Length; ++propIndex)
			{
				// Check for any currently active individual building prop replacement.
				if (IndividualBuildingReplacement.instance.ActiveReplacement(replacement.buildingInfo, propIndex) != null)
				{
					// Active individual building prop replacement; skip this one.
					continue;
				}

				// Check for any existing all-building replacement.
				PrefabInfo thisProp = AllBuildingReplacement.instance.ActiveReplacement(replacement.buildingInfo, propIndex)?.targetInfo;
				if (thisProp == null)
				{
					// No active replacement; use current PropInfo.
					if (replacement.targetInfo is PropInfo)
					{
						thisProp = replacement.buildingInfo.m_props[propIndex].m_finalProp;
					}
					else
					{
						thisProp = replacement.buildingInfo.m_props[propIndex].m_finalTree;
					}
				}

				// See if this prop matches our replacement.
				if (thisProp != null && thisProp == replacement.targetInfo)
				{
					// Match!  Add reference data to the list.
					replacement.references.Add(new BuildingPropReference
					{
						building = replacement.buildingInfo,
						propIndex = propIndex,
						radAngle = replacement.buildingInfo.m_props[propIndex].m_radAngle,
						postion = replacement.buildingInfo.m_props[propIndex].m_position,
						probability = replacement.buildingInfo.m_props[propIndex].m_probability
					});
				}
			}

			// Now, iterate through each entry found.
			foreach (BuildingPropReference propReference in replacement.references)
			{
				// Reset any all-building replacements first.
				AllBuildingReplacement.instance.RemoveEntry(replacement.buildingInfo, replacement.targetInfo, propReference.propIndex);

				// Apply the replacement.
				ReplaceProp(replacement, propReference);
			}
		}


		/// <summary>
		/// Reverts a building replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to revert</param>
		/// <param name="removeEntries">True (default) to remove the reverted entries from the list of replacements, false to leave the list unchanged</param>
		/// <returns>True if the entire building record was removed from the list (due to no remaining replacements for that prefab), false if the prefab remains in the list (has other active replacements)</returns>
		private bool Revert(BOBBuildingReplacement replacement, bool removeEntries)
		{
			// Safety check for calls without any current replacement.
			if (replacement == null)
			{
				return false;
			}

			// Get building element.
			BOBBuildingElement thisElement = BuildingElement(replacement.buildingInfo);
			if (thisElement?.replacements == null)
            {
				return false;
            }


			if (replacement.references != null)
			{
				// Iterate through each entry in our list.
				foreach (BuildingPropReference propReference in replacement.references)
				{
					// Revert entry.
					if (replacement.tree)
					{
						propReference.building.m_props[propReference.propIndex].m_finalTree = replacement.TargetTree;
					}
					else
					{
						propReference.building.m_props[propReference.propIndex].m_finalProp = replacement.TargetProp;
					}
					propReference.building.m_props[propReference.propIndex].m_radAngle = propReference.radAngle;
					propReference.building.m_props[propReference.propIndex].m_position = propReference.postion;
					propReference.building.m_props[propReference.propIndex].m_probability = propReference.probability;

					// Restore any all-building replacement.
					AllBuildingReplacement.instance.Restore(propReference.building, replacement.targetInfo, propReference.propIndex);

					// Add building to dirty list.
					BuildingData.DirtyList.Add(propReference.building);
				}

				// Remove entry from list, if we're doing so.
				if (removeEntries)
				{
					thisElement.replacements.Remove(replacement);

					// Delete entire building entry if nothing left after removing this one, and return true to indicate that we've done so.
					if (thisElement.replacements.Count == 0)
					{
						ConfigurationUtils.CurrentConfig.buildings.Remove(thisElement);
						return true;
					}
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
		private BOBBuildingElement BuildingElement(BuildingInfo buildingInfo) => ConfigurationUtils.CurrentConfig.buildings.Find(x => x.buildingInfo == buildingInfo);
	}
}
