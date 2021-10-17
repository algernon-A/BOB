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
		/// Reverts all active replacements and re-initialises the master dictionary.
		/// </summary>
		internal abstract void RevertAll();


		/// <summary>
		/// Checks if there's a currently active replacement applied to the given building prop index, and if so, returns the *replacement* record.
		/// </summary>
		/// <param name="buildingPrefab">Building prefab to check</param>
		/// <param name="propIndex">Prop index to check</param>
		/// <returns>Replacement record if a replacement is currently applied, null if no replacement is currently applied</returns>
		internal abstract BOBBuildingReplacement ActiveReplacement(BuildingInfo buildingPrefab, int propIndex);


		/// <summary>
		/// Performs setup and initialises the master dictionary.  Must be called prior to use.
		/// </summary>
		protected abstract void Setup();


		/// <summary>
		/// Constructor - initializes instance reference and calls initial setup.
		/// </summary>
		internal BuildingReplacementBase()
		{
			Setup();
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