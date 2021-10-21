using UnityEngine;


namespace BOB
{
	/// <summary>
	/// Records original prop data.
	/// </summary>
	public class NetPropReference
	{
		public NetInfo network;
		public int laneIndex;
		public int propIndex;
		public float angle;
		public Vector3 position;
		public int probability;
	}


	/// <summary>
	/// Base class for building replacement.
	/// </summary>
	internal abstract class NetworkReplacementBase
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
		internal abstract BOBNetReplacement ActiveReplacement(NetInfo netPrefab, int laneIndex, int propIndex);


		/// <summary>
		/// Performs setup and initialises the master dictionary.  Must be called prior to use.
		/// </summary>
		protected abstract void Setup();


		/// <summary>
		/// Constructor - initializes instance reference and calls initial setup.
		/// </summary>
		internal NetworkReplacementBase()
		{
			Setup();
		}


		/// <summary>
		/// Replaces a prop using a network replacement.
		/// </summary>
		/// <param name="netElement">Network replacement element to apply</param>
		/// <param name="propReference">Individual prop reference to apply to</param>
		protected void ReplaceProp(BOBNetReplacement netElement, NetPropReference propReference)
		{
			// Convert offset to Vector3.
			Vector3 offset = new Vector3
			{
				x = netElement.offsetX,
				y = netElement.offsetY,
				z = netElement.offsetZ
			};

			NetInfo.Lane thisLane = propReference.network.m_lanes[propReference.laneIndex];

			// Apply replacement.
			if (netElement.replacementInfo is PropInfo propInfo)
			{
				thisLane.m_laneProps.m_props[propReference.propIndex].m_finalProp = propInfo;
			}
			else if (netElement.replacementInfo is TreeInfo treeInfo)
			{
				thisLane.m_laneProps.m_props[propReference.propIndex].m_finalTree = treeInfo;
			}
			else
			{
				Logging.Error("invalid replacement ", netElement.replacementInfo?.name ?? "null", " passed to NetworkReplacement.ReplaceProp");
			}

			// Invert x offset to match original prop x position.
			if (thisLane.m_position + propReference.position.x < 0)
			{
				offset.x = 0 - offset.x;
			}

			// Angle and offset.
			thisLane.m_laneProps.m_props[propReference.propIndex].m_angle = propReference.angle + netElement.angle;
			thisLane.m_laneProps.m_props[propReference.propIndex].m_position = propReference.position + offset;

			// Probability.
			thisLane.m_laneProps.m_props[propReference.propIndex].m_probability = netElement.probability;

			// Add network to dirty list.
			NetData.DirtyList.Add(propReference.network);
		}
	}
}