using System.Collections.Generic;
using UnityEngine;


namespace BOB
{
	/// <summary>
	/// Records original prop data.
	/// </summary>
	public class BuildingPropReference
	{
		public BuildingInfo building;
		public int propIndex;
		public float radAngle;
		public Vector3 postion;
		public int probability;
	}


	/// <summary>
	/// Static class to manage building prop and tree replacements.
	/// </summary>
	internal class BuildingReplacement
	{
		// Instance reference.
		internal static BuildingReplacement instance;

		// Master dictionary of replaced prop references.
		internal Dictionary<BuildingInfo, Dictionary<PrefabInfo, BOBBuildingReplacement>> replacements;


		/// <summary>
		/// Constructor - initializes instance reference and calls initial setup.
		/// </summary>
		internal BuildingReplacement()
        {
			instance = this;
			Setup();
        }


		/// <summary>
		/// Performs setup and initialises the master dictionary.  Must be called prior to use.
		/// </summary>
		internal void Setup()
		{
			replacements = new Dictionary<BuildingInfo, Dictionary<PrefabInfo, BOBBuildingReplacement>>();
		}


		/// <summary>
		/// Reverts all active building replacements and re-initialises the master dictionary.
		/// </summary>
		internal void RevertAll()
		{
			foreach (BuildingInfo building in replacements.Keys)
			{
				// Iterate through each entry in the master prop dictionary.
				foreach (PrefabInfo prop in replacements[building].Keys)
				{
					// Revert this replacement (but don't remove the entry, as the dictionary is currently immutable while we're iterating through it).
					Revert(building, prop, removeEntries: false);
				}
			}

			// Re-initialise the dictionaries.
			Setup();
		}


		/// <summary>
		/// Reverts a building replacement.
		/// </summary>
		/// <param name="building">Targeted building</param>
		/// <param name="target">Targeted (original) tree/prop prefab</param>
		/// <param name="removeEntries">True (default) to remove the reverted entries from the master dictionary, false to leave the dictionary unchanged</param>
		/// <returns>True if the entire building record was removed from the dictionary (due to no remaining replacements for that prefab), false if the prefab remains in the dictionary (has other active replacements)</returns>
		internal void Revert(BuildingInfo building, PrefabInfo target, bool removeEntries = true)
		{
			// Safety check.
			if (building == null || !replacements.ContainsKey(building))
			{
				return;
			}

			// Iterate through each entry in our dictionary.
			foreach (BuildingPropReference propReference in replacements[building][target].references)
			{
				// Revert entry.
				if (target is PropInfo propTarget)
				{
					propReference.building.m_props[propReference.propIndex].m_finalProp = propTarget;
				}
				else
				{
					propReference.building.m_props[propReference.propIndex].m_finalTree = (TreeInfo)target;
				}
				propReference.building.m_props[propReference.propIndex].m_radAngle = propReference.radAngle;
				propReference.building.m_props[propReference.propIndex].m_position = propReference.postion;
				propReference.building.m_props[propReference.propIndex].m_probability = propReference.probability;

				// Restore any all-building replacement.
				AllBuildingReplacement.instance.Restore(building, target, propReference.propIndex);

				// Add building to dirty list.
				BuildingData.DirtyList.Add(propReference.building);
			}

			// Remove entry from dictionary, if we're doing so.
			if (removeEntries)
			{
				replacements[building].Remove(target);

				// Delete entire building entry if nothing left after removing this one.
				if (replacements[building].Count == 0)
				{
					replacements.Remove(building);
				}
			}
		}


		/// <summary>
		/// Removes an entry from the master dictionary of building replacements currently applied to building.
		/// </summary>
		/// <param name="buildingPrefab">Building prefab</param>
		/// <param name="target">Target prop info</param>
		/// <param name="laneIndex">Lane index</param>
		/// <param name="propIndex">Prop index</param>
		internal void RemoveEntry(BuildingInfo buildingPrefab, PrefabInfo target, int propIndex)
		{
			// Check to see if we have an entry for this prefab.
			if (replacements.ContainsKey(buildingPrefab))
			{
				if (replacements[buildingPrefab].ContainsKey(target))
				{
					// Yes - iterate through each recorded prop reference.
					for (int i = 0; i < replacements[buildingPrefab][target].references.Count; ++i)
					{
						// Look for a building and index match.
						BuildingPropReference propReference = replacements[buildingPrefab][target].references[i];
						if (propReference.building == buildingPrefab && propReference.propIndex == propIndex)
						{
							// Got a match!  Revert instance.
							if (target is PropInfo propTarget)
							{
								propReference.building.m_props[propReference.propIndex].m_finalProp = propTarget;
							}
							else
							{
								propReference.building.m_props[propReference.propIndex].m_finalTree = (TreeInfo)target;
							}
							buildingPrefab.m_props[propIndex].m_radAngle = propReference.radAngle;
							buildingPrefab.m_props[propIndex].m_position = propReference.postion;
							buildingPrefab.m_props[propIndex].m_probability = propReference.probability;

							// Remove this reference and return.
							replacements[buildingPrefab][target].references.Remove(replacements[buildingPrefab][target].references[i]);
							return;
						}
					}
				}
			}
		}


		/// <summary>
		/// Applies a new (or updated) building replacement.
		/// </summary>
		/// <param name="building">Targeted building</param>
		/// <param name="target">Targeted (original) prop prefab</param>
		/// <param name="replacement">Replacment prop prefab</param>
		/// <param name="angle">Replacment prop angle adjustment</param>
		/// <param name="offsetX">Replacment X position offset</param>
		/// <param name="offsetY">Replacment Y position offset</param>
		/// <param name="offsetZ">Replacment Z position offset</param>
		/// <param name="probability">Replacement probability</param>
		internal void Apply(BuildingInfo building, PrefabInfo target, PrefabInfo replacement, float angle, float offsetX, float offsetY, float offsetZ, int probability)
		{
			// Make sure that target and replacement are the same type before doing anything.
			if (target == null || replacement == null || (target is TreeInfo && !(replacement is TreeInfo)) || (target is PropInfo) && !(replacement is PropInfo))
			{
				return;
			}

			// Check to see if we already have a replacement entry for this prop - if so, revert the replacement first.
			if (replacements.ContainsKey(building) && replacements[building].ContainsKey(target))
			{
				Revert(building, target, true);
			}

			// Create new dictionary entry for building if none already exists.
			if (!replacements.ContainsKey(building))
			{
				replacements.Add(building, new Dictionary<PrefabInfo, BOBBuildingReplacement>());
			}

			// Create new dictionary entry for prop if none already exists.
			if (!replacements[building].ContainsKey(target))
			{
				replacements[building].Add(target, new BOBBuildingReplacement());
			}

			// Add/replace dictionary replacement data.
			replacements[building][target].references = new List<BuildingPropReference>();
			replacements[building][target].tree = target is TreeInfo;
			replacements[building][target].targetInfo = target;
			replacements[building][target].target = target.name;
			replacements[building][target].angle = angle;
			replacements[building][target].offsetX = offsetX;
			replacements[building][target].offsetY = offsetY;
			replacements[building][target].offsetZ = offsetZ;
			replacements[building][target].probability = probability;

			// Record replacement prop.
			replacements[building][target].replacementInfo = replacement;
			replacements[building][target].replacement = replacement.name;

			// Iterate through each prop in building.
			for (int propIndex = 0; propIndex < building.m_props.Length; ++propIndex)
			{
				// Check for any currently active individual building prop replacement.
				if (IndividualReplacement.instance.GetOriginal(building, propIndex) != null)
				{
					// Active individual building prop replacement; skip this one.
					continue;
				}

				// Check for any existing all-building replacement.
				PrefabInfo thisProp = AllBuildingReplacement.instance.GetOriginal(building, propIndex);
				if (thisProp == null)
				{
					// No active replacement; use current PropInfo.
					if (target is PropInfo)
					{
						thisProp = building.m_props[propIndex].m_finalProp;
					}
					else
					{
						thisProp = building.m_props[propIndex].m_finalTree;
					}
				}

				// See if this prop matches our replacement.
				if (thisProp != null && thisProp == target)
				{
					// Match!  Add reference data to the list.
					replacements[building][target].references.Add(new BuildingPropReference
					{
						building = building,
						propIndex = propIndex,
						radAngle = building.m_props[propIndex].m_radAngle,
						postion = building.m_props[propIndex].m_position,
						probability = building.m_props[propIndex].m_probability
					});
				}
			}

			// Now, iterate through each entry found.
			foreach (BuildingPropReference propReference in replacements[building][target].references)
			{
				// Reset any all-building replacements first.
				AllBuildingReplacement.instance.RemoveEntry(building, target, propReference.propIndex);

				// Apply the replacement.
				ReplaceProp(replacements[building][target], propReference);
			}
		}


		/// <summary>
		/// Checks if there's a currently active building replacement applied to the given building prop index, and if so, returns the *original* prefab.
		/// </summary>
		/// <param name="buildingPrefab">Building prefab to check</param>
		/// <param name="propIndex">Prop index to check</param>
		/// <returns>Original prefab if a building replacement is currently applied, null if no building replacement is currently applied</returns>
		internal PrefabInfo GetOriginal(BuildingInfo buildingPrefab, int propIndex)
		{
			// Safety check.
			if (buildingPrefab != null && replacements.ContainsKey(buildingPrefab))
			{
				// Iterate through each entry in master dictionary.
				foreach (PrefabInfo target in replacements[buildingPrefab].Keys)
				{
					BOBBuildingReplacement reference = replacements[buildingPrefab][target];
					// Iterate through each prop reference in this entry.
					foreach (BuildingPropReference propRef in reference.references)
					{
						// Check for a building and prop index match.
						if (propRef.building == buildingPrefab && propRef.propIndex == propIndex)
						{
							// Match!  Return the original prefab.
							return target;
						}
					}
				}
			}

			// If we got here, no entry was found - return null to indicate no active replacement.
			return null;
		}


		/// <summary>
		/// Checks if there's a currently active building replacement applied to the given building prop index, and if so, returns the replacement record.
		/// </summary>
		/// <param name="buildingPrefab">Building prefab to check</param>
		/// <param name="propIndex">Prop index to check</param>
		/// <returns>Replacement record if a building replacement is currently applied, null if no building replacement is currently applied</returns>
		internal BOBBuildingReplacement ActiveReplacement(BuildingInfo buildingPrefab, int propIndex)
		{
			// Safety check.
			if (buildingPrefab != null && replacements.ContainsKey(buildingPrefab))
			{
				// Iterate through each entry in master dictionary.
				foreach (PrefabInfo target in replacements[buildingPrefab].Keys)
				{
					BOBBuildingReplacement reference = replacements[buildingPrefab][target];
					// Iterate through each building reference in this entry.
					foreach (BuildingPropReference propRef in reference.references)
					{
						// Check for a building and prop index match.
						if (propRef.building == buildingPrefab && propRef.propIndex == propIndex)
						{
							// Match!  Return the original prefab.
							return replacements[buildingPrefab][target];
						}
					}
				}
			}

			// If we got here, no entry was found - return null to indicate no active replacement.
			return null;
		}


		/// <summary>
		/// Replaces a prop using a building replacement.
		/// </summary>
		/// <param name="buildingElement">Building replacement element to apply</param>
		/// <param name="propReference">Individual prop reference to apply to</param>
		internal void ReplaceProp(BOBBuildingReplacement buildingElement, BuildingPropReference propReference)
		{
			// Convert offset to Vector3.
			Vector3 offset = new Vector3
			{
				x = buildingElement.offsetX,
				y = buildingElement.offsetY,
				z = buildingElement.offsetZ
			};

			// Apply replacement.
			if (buildingElement.replacementInfo is PropInfo propInfo)
			{
				propReference.building.m_props[propReference.propIndex].m_finalProp = propInfo;
			}
			else
			{
				propReference.building.m_props[propReference.propIndex].m_finalTree = (TreeInfo)buildingElement.replacementInfo;
			}

			// Angle and offset.
			propReference.building.m_props[propReference.propIndex].m_radAngle = propReference.radAngle + ((buildingElement.angle * Mathf.PI) / 180f);
			propReference.building.m_props[propReference.propIndex].m_position = propReference.postion + offset;

			// Probability.
			propReference.building.m_props[propReference.propIndex].m_probability = buildingElement.probability;

			// Add building to dirty list.
			BuildingData.DirtyList.Add(propReference.building);
		}


		/// <summary>
		/// Restores a building replacement, if any (e.g. after a individual building prop replacement has been reverted).
		/// </summary>
		/// <param name="buildingPrefab">Building prefab</param>
		/// <param name="target">Target prop info</param>
		/// <param name="propIndex">Prop index</param>
		/// <returns>True if a restoration was made, false otherwise</returns>
		internal bool Restore(BuildingInfo buildingPrefab, PrefabInfo target, int propIndex)
		{
			// Check to see if we have an entry for this prefab.
			if (replacements.ContainsKey(buildingPrefab))
			{
				if (replacements[buildingPrefab].ContainsKey(target))
				{
					// Yes - add reference data to the list.
					BuildingPropReference newReference = new BuildingPropReference
					{
						building = buildingPrefab,
						propIndex = propIndex,
						radAngle = buildingPrefab.m_props[propIndex].m_radAngle,
						postion = buildingPrefab.m_props[propIndex].m_position
					};

					replacements[buildingPrefab][target].references.Add(newReference);

					// Apply replacement and return true to indicate restoration.
					ReplaceProp(replacements[buildingPrefab][target], newReference);

					return true;
				}
			}

			// If we got here, no restoration was made.
			return false;
		}
	}
}
