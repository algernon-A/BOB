using System.Collections.Generic;
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
		/// Applies a new (or updated) network replacement.
		/// </summary>
		/// <param name="network">Targeted network</param>
		/// <param name="target">Targeted (original) prop prefab</param>
		/// <param name="replacement">Replacment prop prefab</param>
		/// <param name="targetLane">Targeted lane index (in parent network)</param>
		/// <param name="targetIndex">Prop index to apply replacement to</param>
		/// <param name="angle">Replacment prop angle adjustment</param>
		/// <param name="offsetX">Replacment X position offset</param>
		/// <param name="offsetY">Replacment Y position offset</param>
		/// <param name="offsetZ">Replacment Z position offset</param>
		/// <param name="probability">Replacement probability</param>
		internal abstract void Replace(NetInfo network, PrefabInfo target, PrefabInfo replacement, int lane, int targetIndex, float angle, float offsetX, float offsetY, float offsetZ, int probability);


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
		protected virtual void Setup() { }


		/// <summary>
		/// Constructor - initializes instance reference and calls initial setup.
		/// </summary>
		internal NetworkReplacementBase()
		{
			Setup();
		}


		/// <summary>
		/// Deserialises a network replacement list.
		/// </summary>
		/// <param name="elementList">List of elements to deserialise</param>
		internal virtual void Deserialize(List<BOBNetworkElement> elementList)
		{
			// Iterate through each element in list.
			foreach (BOBNetworkElement networkElement in elementList)
			{
				// Try to find target network.
				NetInfo networkInfo = (NetInfo)PrefabCollection<NetInfo>.FindLoaded(networkElement.network);
				if (networkInfo == null)
				{
					Logging.Message("Couldn't find target network ", networkElement.network);
					return;
				}


				// Iterate through each element in the provided list.
				foreach (BOBNetReplacement replacement in networkElement.replacements)
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

					// If we got here, it's all good; apply the network replacement.
					Replace(networkInfo, targetPrefab, replacementPrefab, replacement.lane, replacement.index, replacement.angle, replacement.offsetX, replacement.offsetY, replacement.offsetZ, replacement.probability);
				}
			}
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


		/// <summary>
		/// Creates a new NetInfo.Lane instance for the specified network and lane index.
		/// Used to 'separate' target networks for individual and network prop replacement when the network uses shared m_laneProps (e.g. vanilla roads).
		/// </summary>
		/// <param name="network">Network prefab</param>
		/// <param name="lane">Lane index</param>
		protected void NewLanePropInstance(NetInfo network, int lane)
		{
			// Don't do anything if we've previously converted this one.
			if (network.m_lanes[lane].m_laneProps is NewNetLaneProps)
			{
				return;
			}

			Logging.Message("creating new m_laneProps instance for network ", network.name, " at lane ", lane.ToString());

			// Create new m_laneProps instance with new props list, using our custom class instead of NetLaneProps as a flag that we've already done this one.
			NewNetLaneProps newLaneProps = ScriptableObject.CreateInstance<NewNetLaneProps>();
			newLaneProps.m_props = new NetLaneProps.Prop[network.m_lanes[lane].m_laneProps.m_props.Length];

			// Iterate through each  in the existing instance
			for (int i = 0; i < newLaneProps.m_props.Length; ++i)
			{
				NetLaneProps.Prop existingNetLaneProp = network.m_lanes[lane].m_laneProps.m_props[i];

				newLaneProps.m_props[i] = new NetLaneProps.Prop
				{
					m_flagsRequired = existingNetLaneProp.m_flagsRequired,
					m_flagsForbidden = existingNetLaneProp.m_flagsForbidden,
					m_startFlagsRequired = existingNetLaneProp.m_startFlagsRequired,
					m_startFlagsForbidden = existingNetLaneProp.m_startFlagsForbidden,
					m_endFlagsRequired = existingNetLaneProp.m_endFlagsRequired,
					m_endFlagsForbidden = existingNetLaneProp.m_endFlagsForbidden,
					m_colorMode = existingNetLaneProp.m_colorMode,
					m_prop = existingNetLaneProp.m_prop,
					m_tree = existingNetLaneProp.m_tree,
					m_position = existingNetLaneProp.m_position,
					m_angle = existingNetLaneProp.m_angle,
					m_segmentOffset = existingNetLaneProp.m_segmentOffset,
					m_repeatDistance = existingNetLaneProp.m_repeatDistance,
					m_minLength = existingNetLaneProp.m_minLength,
					m_cornerAngle = existingNetLaneProp.m_cornerAngle,
					m_probability = existingNetLaneProp.m_probability,
					m_finalProp = existingNetLaneProp.m_finalProp,
					m_finalTree = existingNetLaneProp.m_finalTree
				};
			}

			// Replace network laneProps with our new instance.
			network.m_lanes[lane].m_laneProps = newLaneProps;
		}
	}


	/// <summary>
	/// 'Dummy' class for new NetLaneProps.Prop instances to overcome network NetLaneProps sharing.
	/// </summary>
	public class NewNetLaneProps : NetLaneProps
	{
    }
}