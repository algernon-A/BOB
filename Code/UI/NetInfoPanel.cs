using UnityEngine;
using System.Collections.Generic;


namespace BOB
{
	class BOBNetInfoPanel : BOBInfoPanel
	{
		NetInfo currentNet;


		/// <summary>
		/// Performs initial setup 
		/// </summary>
		/// <param name="parentTransform">Parent transform</param>
		/// <param name="targetPrefabInfo">Currently selected target prefab</param>
		internal override void Setup(Transform parentTransform, PrefabInfo targetPrefabInfo)
		{
			// Set target reference.
			currentNet = targetPrefabInfo as NetInfo;

			// Base setup.
			base.Setup(parentTransform, targetPrefabInfo);
		}


		/// <summary>
		/// Populates a fastlist with a list of network-specific trees or props.
		/// </summary>
		/// <param name="isTree">True for a list of trees, false for props</param>
		/// <returns>Populated fastlist of loaded prefabs</returns>
		protected override FastList<object> TargetList(bool isTree)
		{
			// List of prefabs that have passed filtering.
			List<PropListItem> propList = new List<PropListItem>();

			// Check to see if this building contains any props.
			if (currentNet.m_lanes == null || currentNet.m_lanes.Length == 0)
			{
				// No props - show 'no props' label and return an empty list.
				noPropsLabel.Show();
				return new FastList<object>();
			}

			// Local reference.
			NetInfo.Lane[] lanes = currentNet.m_lanes;

			// Iterate through each lane.
			for (int lane = 0; lane < lanes.Length; ++lane)
			{
				// Local reference.
				NetLaneProps.Prop[] laneProps = lanes[lane].m_laneProps.m_props;

				// Iterate through each prop in lane.
				for (int propIndex = 0; propIndex < laneProps.Length; ++propIndex)
				{
					// Create new list item.
					PropListItem propListItem = new PropListItem();

					// Try to get relevant prefab (prop/tree).
					PrefabInfo prefabInfo = isTree ? (PrefabInfo)laneProps[propIndex]?.m_finalTree : (PrefabInfo)laneProps[propIndex]?.m_finalProp;

					if (prefabInfo?.name == null)
					{
						prefabInfo = isTree ? (PrefabInfo)laneProps[propIndex]?.m_tree : (PrefabInfo)laneProps[propIndex]?.m_prop;
					}

					// Check to see if we were succesful - if not (e.g. we only want trees and this is a prop), continue on to next lane prop.
					if (prefabInfo?.name == null)
					{
						continue;
					}

					Debugging.Message("found lane prop " + prefabInfo.name + " at index " + propIndex + " in lane " + lane);

					propListItem.originalPrefab = prefabInfo;
					propListItem.index = propIndex;

					propListItem.angle = laneProps[propIndex].m_angle;
					propListItem.probability = laneProps[propIndex].m_probability;

					// Add this item to our list.
					propList.Add(propListItem);
				}
			}

			// Create return fastlist from our filtered list, ordering by name.
			FastList<object> fastList = new FastList<object>();
			fastList.m_buffer = propList.ToArray();
			fastList.m_size = propList.Count;

			// If the list is empty, show the 'no props' label; otherwise, hide it.
			if (fastList.m_size == 0)
			{
				noPropsLabel.Show();
			}
			else
			{
				noPropsLabel.Hide();
			}

			return fastList;
		}
	}
}
