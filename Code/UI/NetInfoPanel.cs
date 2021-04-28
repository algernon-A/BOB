using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework.UI;


namespace BOB
{
	/// <summary>
	/// BOB network tree/prop replacement panel.
	/// </summary>
	internal class BOBNetInfoPanel : BOBInfoPanel
	{
		// Current selection reference.
		NetInfo currentNet;


		// Button tooltips.
		protected override string ReplaceTooltipKey => "BOB_PNL_RTN";
		protected override string ReplaceAllTooltipKey => "BOB_PNL_RAN";


		// Replace button atlases.
		protected override UITextureAtlas ReplaceAtlas => TextureUtils.LoadSpriteAtlas("bob_road");
		protected override UITextureAtlas ReplaceAllAtlas => TextureUtils.LoadSpriteAtlas("bob_all_roads");


		/// <summary>
		/// Handles changes to the currently selected target prefab.
		/// </summary>
		internal override PropListItem CurrentTargetItem
        {
            set
            {
				// Call base.
				base.CurrentTargetItem = value;

				if (value != null)
				{
					// If we've got a replacement, update the offset fields with the replacement vlues
					if (CurrentTargetItem.replacementPrefab != null)
					{
						angleSlider.TrueValue = NetworkReplacement.instance.replacements[currentNet][CurrentTargetItem.originalPrefab].angle;
						xSlider.TrueValue = NetworkReplacement.instance.replacements[currentNet][CurrentTargetItem.originalPrefab].offsetX;
						ySlider.TrueValue = NetworkReplacement.instance.replacements[currentNet][CurrentTargetItem.originalPrefab].offsetY;
						zSlider.TrueValue = NetworkReplacement.instance.replacements[currentNet][CurrentTargetItem.originalPrefab].offsetZ;
						probabilitySlider.TrueValue = NetworkReplacement.instance.replacements[currentNet][CurrentTargetItem.originalPrefab].probability;
					}
					// Ditto for any all-network replacement.
					else if (CurrentTargetItem.allPrefab != null)
					{
						angleSlider.TrueValue = AllNetworkReplacement.instance.replacements[CurrentTargetItem.originalPrefab].angle;
						xSlider.TrueValue = AllNetworkReplacement.instance.replacements[CurrentTargetItem.originalPrefab].offsetX;
						ySlider.TrueValue = AllNetworkReplacement.instance.replacements[CurrentTargetItem.originalPrefab].offsetY;
						zSlider.TrueValue = AllNetworkReplacement.instance.replacements[CurrentTargetItem.originalPrefab].offsetZ;
						probabilitySlider.TrueValue = AllNetworkReplacement.instance.replacements[CurrentTargetItem.originalPrefab].probability;
					}
					else
					{
						// No current replacement; set all offset fields to original prop.
						angleSlider.TrueValue = 0f;
						xSlider.TrueValue = 0;
						ySlider.TrueValue = 0;
						zSlider.TrueValue = 0;
						probabilitySlider.TrueValue = value.originalProb;
					}
				}
            }
		}


		/// <summary>
		/// Performs initial setup 
		/// </summary>
		/// <param name="targetPrefabInfo">Currently selected target prefab</param>
		internal override void Setup(PrefabInfo targetPrefabInfo)
		{
			// Set target reference.
			currentNet = targetPrefabInfo as NetInfo;

			// Base setup.
			base.Setup(targetPrefabInfo);

			// Add pack button.
			UIButton packButton = UIControls.EvenSmallerButton(this, RightX - 200f, TitleHeight + (Margin / 2f), Translations.Translate("BOB_PNL_PKB"));
			packButton.eventClicked += (component, clickEvent) => PackPanelManager.Create();

			// Populate target list and select target item.
			TargetList();

			// Apply Harmony rendering patches.
			Patcher.PatchNetworkOverlays(true);
		}


		/// <summary>
		/// Replace button event handler.
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
		/// </summary>
		protected override void Replace(UIComponent control, UIMouseEventParameter mouseEvent)
		{
			// Make sure we have valid a target and replacement.
			if (CurrentTargetItem != null && replacementPrefab != null)
			{
				// Network replacements are always grouped.
				NetworkReplacement.instance.Apply(currentNet, CurrentTargetItem.originalPrefab ?? CurrentTargetItem.replacementPrefab, replacementPrefab, angleSlider.TrueValue, xSlider.TrueValue, ySlider.TrueValue, zSlider.TrueValue, (int)probabilitySlider.TrueValue);

				// Perform post-replacment updates.
				FinishUpdate();
			}
		}


		/// <summary>
		/// Revert button event handler.
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
		/// </summary>
		protected override void Revert(UIComponent control, UIMouseEventParameter mouseEvent)
		{
			// Network or all-network reversion?
			if (CurrentTargetItem.replacementPrefab != null)
			{
				// Individual network reversion - ensuire that we've got a current selection before doing anything.
				if (CurrentTargetItem != null && CurrentTargetItem is NetPropListItem)
				{
					// Network replacements are always grouped.
					NetworkReplacement.instance.Revert(currentNet, CurrentTargetItem.originalPrefab, true);

					// Perform post-reversion updates.
					FinishUpdate();
				}
			}
			else if (CurrentTargetItem.allPrefab != null)
			{
				// All-network reversion - make sure we've got a currently active replacement before doing anything.
				if (CurrentTargetItem.originalPrefab)
				{
					// Apply all-network reversion.
					AllNetworkReplacement.instance.Revert(CurrentTargetItem.originalPrefab, true);

					// Save configuration file and refresh target list (to reflect our changes).
					ConfigurationUtils.SaveConfig();

					// Perform post-reversion updates.
					FinishUpdate();
				}
			}
		}


		/// <summary>
		/// Replace all button event handler.
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
		/// </summary>
		protected override void ReplaceAll(UIComponent control, UIMouseEventParameter mouseEvent)
		{
			// Apply replacement.
			AllNetworkReplacement.instance.Apply(CurrentTargetItem.originalPrefab ?? CurrentTargetItem.replacementPrefab, replacementPrefab, angleSlider.TrueValue, xSlider.TrueValue, ySlider.TrueValue, zSlider.TrueValue, (int)probabilitySlider.TrueValue);

			// Perform post-replacment updates.
			FinishUpdate();
		}


		/// <summary>
		/// Updates the target item record for changes in replacement status (e.g. after applying or reverting changes).
		/// </summary>
		/// <param name="propListItem">Target item</param>
		protected override void UpdateTargetItem(PropListItem propListItem)
		{
			if (propListItem is NetPropListItem netItem)
			{
				// Determine lane and index to test - just grab first one from list.
				int lane = netItem.lanes[0];
				int propIndex = netItem.indexes[0];

				// All-network replacement and original probability (if any).
				BOBNetReplacement allNetReplacement = AllNetworkReplacement.instance.ActiveReplacement(currentNet, lane, propIndex);
				if (allNetReplacement != null)
				{
					propListItem.allPrefab = allNetReplacement.replacementInfo;
					propListItem.allProb = allNetReplacement.probability;
				}
				else
				{
					// If no active current record, ensure that it's reset to null.
					propListItem.allPrefab = null;
				}

				// Individual network replacement and original probability (if any).
				BOBNetReplacement netReplacement = NetworkReplacement.instance.ActiveReplacement(currentNet, lane, propIndex);
				if (netReplacement != null)
				{
					propListItem.replacementPrefab = netReplacement.replacementInfo;
					propListItem.replacementProb = netReplacement.probability;
				}
				else
				{
					// If no active current record, ensure that it's reset to null.
					propListItem.replacementPrefab = null;
				}

				// Replacement pack replacement and original probability (if any).
				BOBNetReplacement packReplacement = PackReplacement.instance.ActiveReplacement(currentNet, lane, propIndex);
				if (packReplacement != null)
				{
					propListItem.packagePrefab = packReplacement.replacementInfo;
				}
				else
				{
					// If no active current record, ensure that it's reset to null.
					propListItem.packagePrefab = null;
				}
			}
		}


		/// <summary>
		/// Populates the target fastlist with a list of target-specific trees or props.
		/// </summary>
		protected override void TargetList()
		{
			// List of prefabs that have passed filtering.
			List<NetPropListItem> propList = new List<NetPropListItem>();

			// Check to see if this building contains any props.
			if (currentNet.m_lanes == null || currentNet.m_lanes.Length == 0)
			{
				// No props - show 'no props' label and return an empty list.
				noPropsLabel.Show();
				targetList.m_rowsData = new FastList<object>();
				return;
			}

			// Local reference.
			NetInfo.Lane[] lanes = currentNet.m_lanes;

			// Iterate through each lane.
			for (int lane = 0; lane < lanes.Length; ++lane)
			{
				// Local reference.
				NetLaneProps.Prop[] laneProps = lanes[lane].m_laneProps?.m_props;

				// If no props in this lane, skip it and go to the next one.
				if (laneProps == null)
                {
					continue;
				}

				// Iterate through each prop in lane.
				for (int propIndex = 0; propIndex < laneProps.Length; ++propIndex)
				{
					// Create new list item.
					NetPropListItem propListItem = new NetPropListItem();

					// Try to get relevant prefab (prop/tree), using finalProp.
					PrefabInfo finalInfo = IsTree ? (PrefabInfo)laneProps[propIndex]?.m_finalTree : (PrefabInfo)laneProps[propIndex]?.m_finalProp;

					// Check to see if we were succesful - if not (e.g. we only want trees and this is a prop), continue on to next building prop.
					if (finalInfo?.name == null)
					{
						continue;
					}

					// Networks are always grouped - set index and lane to -1 and add to our lists of indexes and lanes.
					propListItem.index = -1;
					propListItem.lane = -1;
					propListItem.indexes.Add(propIndex);
					propListItem.lanes.Add(lane);

					// Get original (pre-replacement) tree/prop prefab and current probability (as default original probability).
					propListItem.originalPrefab = NetworkReplacement.instance.GetOriginal(currentNet, lane, propIndex) ?? AllNetworkReplacement.instance.GetOriginal(currentNet, lane, propIndex) ?? PackReplacement.instance.GetOriginal(currentNet, lane, propIndex) ?? finalInfo;
					propListItem.originalProb = laneProps[propIndex].m_probability;
					propListItem.originalAngle = laneProps[propIndex].m_angle;

					// All-network replacement and original probability (if any).
					BOBNetReplacement allNetReplacement = AllNetworkReplacement.instance.ActiveReplacement(currentNet, lane, propIndex);
					if (allNetReplacement != null)
					{
						propListItem.allPrefab = allNetReplacement.replacementInfo;
						propListItem.allProb = allNetReplacement.probability;
					}

					// Individual network replacement and original probability (if any).
					BOBNetReplacement netReplacement = NetworkReplacement.instance.ActiveReplacement(currentNet, lane, propIndex);
					if (netReplacement != null)
					{
						propListItem.replacementPrefab = netReplacement.replacementInfo;
						propListItem.replacementProb = netReplacement.probability;
					}

					// Replacement pack replacement and original probability (if any).
					BOBNetReplacement packReplacement = PackReplacement.instance.ActiveReplacement(currentNet, lane, propIndex);
					if (packReplacement != null)
					{
						propListItem.packagePrefab = packReplacement.replacementInfo;
					}

					// Are we grouping?
					if (propListItem.index == -1)
					{
						// Yes, grouping - initialise a flag to show if we've matched.
						bool matched = false;

						// Iterate through each item in our existing list of props.
						foreach (NetPropListItem item in propList)
						{
							// Check to see if we already have this in the list - matching original prefab, building replacement prefab, all-building replacement prefab, and probability.
							if (item.originalPrefab == propListItem.originalPrefab && item.replacementPrefab == propListItem.replacementPrefab && propListItem.allPrefab == item.allPrefab)
							{
								// We've already got an identical grouped instance of this item - add this index and lane to the lists of indexes and lanes under that item and set the flag to indicate that we've done so.
								item.indexes.Add(propIndex);
								item.lanes.Add(lane);
								matched = true;

								// No point going any further through the list, since we've already found our match.
								break;
							}
						}

						// Did we get a match?
						if (matched)
						{
							// Yes - continue on to next network prop (without adding this item separately to the list).
							continue;
						}
					}

					// Add this item to our list.
					propList.Add(propListItem);
				}
			}

			// Create return fastlist from our filtered list, ordering by name.
			targetList.m_rowsData = new FastList<object>()
			{
				m_buffer = targetSearchStatus == (int)OrderBy.NameDescending ? propList.OrderByDescending(item => item.DisplayName).ToArray() : propList.OrderBy(item => item.DisplayName).ToArray(),
				m_size = propList.Count
			};
			targetList.Refresh();

			// If the list is empty, show the 'no props' label; otherwise, hide it.
			if (targetList.m_rowsData.m_size == 0)
			{
				noPropsLabel.Show();
			}
			else
			{
				noPropsLabel.Hide();
			}
		}


		/// <summary>
		/// Performs actions to be taken once an update (application or reversion) has been applied, including saving data, updating button states, and refreshing renders.
		/// </summary>
		protected override void FinishUpdate()
        {
			base.FinishUpdate();

			// Update any dirty net renders.
			NetData.Update();
		}
	}
}
