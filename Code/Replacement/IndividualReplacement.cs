﻿using System.Collections.Generic;
using UnityEngine;


namespace BOB
{
	/// <summary>
	/// Static class to manage individual building prop and tree replacements.
	/// </summary>
	internal static class IndividualReplacement
	{
		// Master dictionary of replaced prop references.
		internal static Dictionary<BuildingInfo, Dictionary<int, BOBBuildingReplacement>> replacements;



		/// <summary>
		/// Performs setup and initialises the master dictionary.  Must be called prior to use.
		/// </summary>
		internal static void Setup()
		{
			replacements = new Dictionary<BuildingInfo, Dictionary<int, BOBBuildingReplacement>>();
		}


		/// <summary>
		/// Reverts all active individual building replacements and re-initialises the master dictionary.
		/// </summary>
		internal static void RevertAll()
		{
			foreach (BuildingInfo building in replacements.Keys)
			{
				// Iterate through each entry in the master prop dictionary.
				foreach (int prop in replacements[building].Keys)
				{
					// Revert this replacement (but don't remove the entry, as the dictionary is currently immutable while we're iterating through it).
					Revert(building, prop, removeEntries: false);
				}
			}

			// Re-initialise the dictionaries.
			Setup();
		}


		/// <summary>
		/// Reverts an individual building prop replacement.
		/// </summary>
		/// <param name="building">Targeted building</param>
		/// <param name="targetIndex">Targeted (original) tree/prop prefab index</param>
		/// <param name="replacement">Applied replacment tree/prop prefab</param>
		/// <param name="removeEntries">True (default) to remove the reverted entries from the master dictionary, false to leave the dictionary unchanged</param>
		/// <returns>True if the entire building record was removed from the dictionary (due to no remaining replacements for that prefab), false if the prefab remains in the dictionary (has other active replacements)</returns>
		internal static void Revert(BuildingInfo building, int targetIndex, bool removeEntries = true)
		{
			// Safety check.
			if (building == null || !replacements.ContainsKey(building))
			{
				return;
			}

			// Iterate through each entry in our dictionary.
			foreach (BuildingPropReference propReference in replacements[building][targetIndex].references)
			{
				// Local reference.
				PrefabInfo target = replacements[building][targetIndex].targetInfo;

				// Revert entry.
				if (target is PropInfo)
				{
					propReference.building.m_props[targetIndex].m_finalProp = (PropInfo)target;
				}
				else
				{
					propReference.building.m_props[targetIndex].m_finalTree = (TreeInfo)target;
				}
				propReference.building.m_props[targetIndex].m_radAngle = propReference.radAngle;
				propReference.building.m_props[targetIndex].m_position = propReference.postion;
				propReference.building.m_props[targetIndex].m_probability = propReference.probability;

				// Restore any building replacement.
				if (!BuildingReplacement.Restore(building, target, propReference.propIndex))
				{
					// No building restoration occured - restore any all-building replacement.
					AllBuildingReplacement.Restore(building, target, propReference.propIndex);
				}
			}

			// Remove entry from dictionary, if we're doing so.
			if (removeEntries)
			{
				replacements[building].Remove(targetIndex);

				// Delete entire building entry if nothing left after removing this one.
				if (replacements[building].Count == 0)
				{
					replacements.Remove(building);
				}
			}
		}


		/// <summary>
		/// Applies a new (or updated) individual building prop replacement.
		/// </summary>
		/// <param name="building">Targeted building</param>
		/// <param name="target">Targeted (original) prop prefab</param>
		/// <param name="replacement">Replacment prop prefab</param>
		/// <param name="angle">Replacment prop angle adjustment</param>
		/// <param name="offsetX">Replacment X position offset</param>
		/// <param name="offsetY">Replacment Y position offset</param>
		/// <param name="offsetZ">Replacment Z position offset</param>
		/// <param name="probability">Replacement probability</param>
		internal static void Apply(BuildingInfo building, int targetIndex, PrefabInfo replacement, float angle, float offsetX, float offsetY, float offsetZ, int probability)
		{
			// Local reference.
			BuildingInfo.Prop targetProp = building?.m_props?[targetIndex];

			// Bail out if no building prop.
			if (targetProp == null)
			{
				return;
			}

			// Tree or prop?
			PrefabInfo target = replacement is TreeInfo ? (PrefabInfo)targetProp.m_finalTree : (PrefabInfo)targetProp.m_finalProp;

			// Bail out if no valid target.
			if (target == null)
			{
				return;
			}

			// Check to see if we already have a replacement entry for this prop - if so, revert the replacement first.
			if (replacements.ContainsKey(building) && replacements[building].ContainsKey(targetIndex))
			{
				Revert(building, targetIndex, true);
			}

			// Create new dictionary entry for building if none already exists.
			if (!replacements.ContainsKey(building))
			{
				replacements.Add(building, new Dictionary<int, BOBBuildingReplacement>());
			}

			// Create new dictionary entry for prop if none already exists.
			if (!replacements[building].ContainsKey(targetIndex))
			{
				replacements[building].Add(targetIndex, new BOBBuildingReplacement());
			}

			// Add/replace dictionary replacement data.
			replacements[building][targetIndex].index = targetIndex;
			replacements[building][targetIndex].references = new List<BuildingPropReference>();
			replacements[building][targetIndex].tree = target is TreeInfo;
			replacements[building][targetIndex].targetInfo = target;
			replacements[building][targetIndex].target = target.name;
			replacements[building][targetIndex].angle = angle;
			replacements[building][targetIndex].offsetX = offsetX;
			replacements[building][targetIndex].offsetY = offsetY;
			replacements[building][targetIndex].offsetZ = offsetZ;
			replacements[building][targetIndex].probability = probability;

			// Record replacement prop.
			replacements[building][targetIndex].replacementInfo = replacement;
			replacements[building][targetIndex].replacement = replacement.name;

			// Create replacement record.
			BuildingPropReference propReference = new BuildingPropReference
			{
				building = building,
				propIndex = targetIndex,
				radAngle = building.m_props[targetIndex].m_radAngle,
				postion = building.m_props[targetIndex].m_position,
				probability = building.m_props[targetIndex].m_probability
			};

			// Add reference data to the list (only entry....)
			replacements[building][targetIndex].references.Add(propReference);

			// Reset any building or all-building replacements first.
			BuildingReplacement.RemoveEntry(building, target, targetIndex);
			AllBuildingReplacement.RemoveEntry(building, target, targetIndex);

			// Apply the replacement.
			BuildingReplacement.ReplaceProp(replacements[building][targetIndex], propReference);
		}


		/// <summary>
		/// Checks if there's a currently active individual building prop replacement applied to the given building prop index, and if so, returns the *original* prefab.
		/// </summary>
		/// <param name="buildingPrefab">Building prefab to check</param>
		/// <param name="propIndex">Prop index to check</param>
		/// <returns>Original prefab if an invidividual building prop replacement is currently applied, null if no individual building prop replacement is currently applied</returns>
		internal static PrefabInfo GetOriginal(BuildingInfo buildingPrefab, int propIndex)
		{
			// Just check for a match in our dictionary.
			if (buildingPrefab != null && replacements.ContainsKey(buildingPrefab))
			{
				if (replacements[buildingPrefab].ContainsKey(propIndex))
				{
					// Got a match - simply return the entry.
					return replacements[buildingPrefab][propIndex].targetInfo;
				}
			}

			// If we got here, no entry was found - return null to indicate no active replacement.
			return null;
		}


		/// <summary>
		/// Checks if there's a currently active individual building prop applied to the given building prop index, and if so, returns the replacement record.
		/// </summary>
		/// <param name="buildingPrefab">Building prefab to check</param>
		/// <param name="propIndex">Prop index to check</param>
		/// <returns>Replacement record if an invidividual building prop replacement is currently applied, null if no individual building prop replacement is currently applied</returns>
		internal static BOBBuildingReplacement ActiveReplacement(BuildingInfo buildingPrefab, int propIndex)
		{
			// Just check for a match in our dictionary.
			if (buildingPrefab != null && replacements.ContainsKey(buildingPrefab))
			{
				if (replacements[buildingPrefab].ContainsKey(propIndex))
				{
					// Got a match - simply return the entry.
					return replacements[buildingPrefab][propIndex];
				}
			}

			// If we got here, no entry was found - return null to indicate no active replacement.
			return null;
		}
	}
}