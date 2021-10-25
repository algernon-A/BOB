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
	/// Base class for building replacement.
	/// </summary>
	internal abstract class BuildingReplacementBase
	{
		/// <summary>
		/// Applies a new (or updated) building prop replacement.
		/// </summary>
		/// <param name="buildingInfo">Targeted building prefab</param>
		/// <param name="targetInfo">Targeted (original) prop prefab</param>
		/// <param name="replacementInfo">Replacment prop prefab</param>
		/// <param name="targetIndex">Prop index to apply replacement to</param>
		/// <param name="angle">Replacment prop angle adjustment</param>
		/// <param name="offsetX">Replacment X position offset</param>
		/// <param name="offsetY">Replacment Y position offset</param>
		/// <param name="offsetZ">Replacment Z position offset</param>
		/// <param name="probability">Replacement probability</param>
		internal abstract void Replace(BuildingInfo buildingInfo, PrefabInfo targetInfo, PrefabInfo replacementInfo, int targetIndex, float angle, float offsetX, float offsetY, float offsetZ, int probability);


		/// <summary>
		/// Reverts all active replacements and re-initialises the master dictionary.
		/// </summary>
		internal abstract void RevertAll();


		/// <summary>
		/// Retrieves any currently-applied replacement entry for the given building and target prefab.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <param name="targetInfo">Target prop/tree prefab</param>
		/// <returns>Currently-applied replacement (null if none)</returns>
		internal abstract BOBBuildingReplacement Replacement(BuildingInfo buildingInfo, PrefabInfo targetInfo);


		/// <summary>
		/// Checks if there's a currently active replacement applied to the given building prop index, and if so, returns the *replacement* record.
		/// </summary>
		/// <param name="buildingInfo">Building prefab to check</param>
		/// <param name="propIndex">Prop index to check</param>
		/// <returns>Replacement record if a replacement is currently applied, null if no replacement is currently applied</returns>
		internal abstract BOBBuildingReplacement ActiveReplacement(BuildingInfo buildingInfo, int propIndex);


		/// <summary>
		/// Restores a building replacement, if any (e.g. after a individual building prop replacement has been reverted).
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <param name="targetInfo">Target prop info</param>
		/// <param name="propIndex">Prop index</param>
		/// <returns>True if a restoration was made, false otherwise</returns>
		internal virtual bool Restore(BuildingInfo buildingInfo, PrefabInfo targetInfo, int propIndex)
		{
			// See if we have a relevant building replacement record.
			BOBBuildingReplacement thisReplacement = Replacement(buildingInfo, targetInfo);
			if (thisReplacement != null)
			{
				// Yes - add reference data to the list.
				BuildingPropReference newReference = new BuildingPropReference
				{
					building = buildingInfo,
					propIndex = propIndex,
					radAngle = buildingInfo.m_props[propIndex].m_radAngle,
					postion = buildingInfo.m_props[propIndex].m_position
				};
				thisReplacement.references.Add(newReference);

				// Apply replacement and return true to indicate restoration.
				ReplaceProp(thisReplacement, newReference);

				return true;
			}

			// If we got here, no restoration was made.
			return false;
		}


		/// <summary>
		/// Performs setup and initialises the master dictionary.  Must be called prior to use.
		/// </summary>
		protected virtual void Setup() { }


		/// <summary>
		/// Constructor - initializes instance reference and calls initial setup.
		/// </summary>
		internal BuildingReplacementBase()
		{
			Setup();
		}


		/// <summary>
		/// Deserialises an individual building prop replacement list.
		/// </summary>
		/// <param name="elementList">Building element list to deserialise</param>
		internal virtual void Deserialize(List<BOBBuildingElement> elementList)
		{
			// Iterate through each element in list.
			foreach (BOBBuildingElement buildingElement in elementList)
			{
				// Try to find target network.
				BuildingInfo buildingInfo = (BuildingInfo)PrefabCollection<BuildingInfo>.FindLoaded(buildingElement.building);
				if (buildingInfo == null)
				{
					Logging.Message("Couldn't find target building ", buildingElement.building);
					return;
				}

				// Iterate through each element in the provided list.
				foreach (BOBBuildingReplacement replacement in buildingElement.replacements)
				{
					// Try to find target prefab.
					PrefabInfo targetPrefab = replacement.tree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacement.target) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacement.target);
					if (targetPrefab == null)
					{
						Logging.Message("Couldn't find target prefab ", replacement.target);
						continue;
					}

					// Try to find replacement prefab.
					PrefabInfo replacementPrefab = ConfigurationUtils.FindReplacementPrefab(replacement.Replacement, replacement.tree);
					if (replacementPrefab == null)
					{
						Logging.Message("Couldn't find replacement prefab ", replacement.Replacement);
						continue;
					}

					// If we got here, it's all good; apply the building replacement.
					Replace(buildingInfo, targetPrefab, replacementPrefab, replacement.index, replacement.angle, replacement.offsetX, replacement.offsetY, replacement.offsetZ, replacement.probability);
				}
			}
		}


		/// <summary>
		/// Replaces a prop using a building replacement.
		/// </summary>
		/// <param name="buildingElement">Building replacement element to apply</param>
		/// <param name="propReference">Individual prop reference to apply to</param>
		protected void ReplaceProp(BOBBuildingReplacement buildingElement, BuildingPropReference propReference)
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
	}
}