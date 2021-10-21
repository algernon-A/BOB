using System.Linq;
using System.Collections.Generic;
using ColossalFramework.UI;


namespace BOB
{
	/// <summary>
	/// BOB network tree/prop replacement panel.
	/// </summary>
	internal class BOBNetInfoPanel : BOBInfoPanel
	{
		// Layout constants.
		private const float PackButtonX = RandomButtonX + ToggleSize;


		// Current selection reference.
		private NetInfo currentNet;


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

				if (value is NetPropListItem netItem)
				{
					// TODO: use properties instead of direct access.  Also for Building Info Panel.
					// If we've got an individual replacement, update the offset fields with the replacement values.
					if (CurrentTargetItem.individualPrefab != null)
					{
						// Handle case of switching from individual to grouped props (lane and index will be -1, actual lane and index in relevant lists).
						int thisLane = netItem.lane;
						int thisIndex = netItem.index;
						if (thisIndex < 0)
                        {
							thisIndex = netItem.indexes[0];
						}
						if (thisLane < 0)
						{
							thisLane = netItem.lanes[0];
						}

						// Get replacement and update control values.
						BOBNetReplacement individualReplacement = IndividualNetworkReplacement.instance.Replacement(currentNet, thisLane, thisIndex);
						if (individualReplacement != null)
						{
							angleSlider.TrueValue = individualReplacement.angle;
							xSlider.TrueValue = individualReplacement.offsetX;
							ySlider.TrueValue = individualReplacement.offsetY;
							zSlider.TrueValue = individualReplacement.offsetZ;
							probabilitySlider.TrueValue = individualReplacement.probability;
						}
					}
					// Ditto for any network replacement
					else if (CurrentTargetItem.replacementPrefab != null)
					{
						// Get replacement and update control values.
						BOBNetReplacement networkReplacement = NetworkReplacement.instance.Replacement(currentNet, CurrentTargetItem.originalPrefab);
						if (networkReplacement != null)
						{
							angleSlider.TrueValue = networkReplacement.angle;
							xSlider.TrueValue = networkReplacement.offsetX;
							ySlider.TrueValue = networkReplacement.offsetY;
							zSlider.TrueValue = networkReplacement.offsetZ;
							probabilitySlider.TrueValue = networkReplacement.probability;
						}
					}
					// Ditto for any all-network replacement.
					else if (CurrentTargetItem.allPrefab != null)
					{
						// Get replacement and update control values.
						BOBNetReplacement allNetReplacement = AllNetworkReplacement.instance.Replacement(CurrentTargetItem.originalPrefab);
						if (allNetReplacement != null)
						{
							angleSlider.TrueValue = allNetReplacement.angle;
							xSlider.TrueValue = allNetReplacement.offsetX;
							ySlider.TrueValue = allNetReplacement.offsetY;
							zSlider.TrueValue = allNetReplacement.offsetZ;
							probabilitySlider.TrueValue = allNetReplacement.probability;
						}
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

					// Set lane highlighting selection for individual items.
					if (netItem.lane > -1)
					{
						RenderOverlays.CurrentLane = currentNet.m_lanes[netItem.lane];
					}
				}
            }
		}


		/// <summary>
		/// Constructor.
		/// </summary>
		internal BOBNetInfoPanel()
        {
			// Add pack button.
			UIButton packButton = AddIconButton(this, PackButtonX, TitleHeight + Margin, ToggleSize, "BOB_PNL_PKB", TextureUtils.LoadSpriteAtlas("bob_prop_pack"));
			packButton.eventClicked += (component, clickEvent) => PackPanelManager.Create();

			// Populate loaded list.
			LoadedList();
		}


		/// <summary>
		/// Sets the target network.
		/// </summary>
		/// <param name="targetPrefabInfo">Target network to set</param>
		internal override void SetTarget(PrefabInfo targetPrefabInfo)
		{
			// Don't do anything if target hasn't changed.
			if (currentNet == targetPrefabInfo)
			{
				return;
			}

			// Set target reference.
			currentNet = targetPrefabInfo as NetInfo;

			// Base setup.
			base.SetTarget(targetPrefabInfo);

			// Populate target list and select target item.
			TargetList();

			// Apply Harmony rendering patches.
			RenderOverlays.CurrentNet = currentNet;
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
			if (CurrentTargetItem != null && ReplacementPrefab != null)
			{
				// Grouped or individual?
				if (CurrentTargetItem.index < 0)
				{
					// Grouped replacement.
					NetworkReplacement.instance.Apply(currentNet, CurrentTargetItem.originalPrefab ?? CurrentTargetItem.replacementPrefab, ReplacementPrefab, angleSlider.TrueValue, xSlider.TrueValue, ySlider.TrueValue, zSlider.TrueValue, (int)probabilitySlider.TrueValue);

					// Update current target.
					CurrentTargetItem.replacementPrefab = ReplacementPrefab;
					CurrentTargetItem.replacementProb = (int)probabilitySlider.TrueValue;
				}
				else
				{
					// Individual replacement.
					NetPropListItem netItem = CurrentTargetItem as NetPropListItem;

					IndividualNetworkReplacement.instance.Apply(currentNet, CurrentTargetItem.originalPrefab ?? CurrentTargetItem.replacementPrefab, netItem.lane, CurrentTargetItem.index, ReplacementPrefab, angleSlider.TrueValue, xSlider.TrueValue, ySlider.TrueValue, zSlider.TrueValue, (int)probabilitySlider.TrueValue);

					// Update current target.
					CurrentTargetItem.individualPrefab = ReplacementPrefab;
					CurrentTargetItem.individualProb = (int)probabilitySlider.TrueValue;
				}

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
			// Individual prop reversion?
			if (CurrentTargetItem.individualPrefab != null)
			{
				// Individual prop reversion - ensuire that we've got a current selection before doing anything.
				if (CurrentTargetItem is NetPropListItem currentNetTargetItem)
				{
					// Individual reversion.
					IndividualNetworkReplacement.instance.Revert(currentNet, currentNetTargetItem.lane, currentNetTargetItem.index, true);

					// Clear current target replacement prefab.
					CurrentTargetItem.individualPrefab = null;
				}

				// Perform post-replacment updates.
				FinishUpdate();
			}
			else if (CurrentTargetItem.replacementPrefab != null)
			{
				// Network reversion - ensuire that we've got a current selection before doing anything.
				if (CurrentTargetItem is NetPropListItem currentNetTargetItem)
				{
					// Network reversion.
					NetworkReplacement.instance.Revert(currentNet, currentNetTargetItem.originalPrefab, true);

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
			AllNetworkReplacement.instance.Apply(CurrentTargetItem.originalPrefab ?? CurrentTargetItem.replacementPrefab, ReplacementPrefab, angleSlider.TrueValue, xSlider.TrueValue, ySlider.TrueValue, zSlider.TrueValue, (int)probabilitySlider.TrueValue);

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
				// Determine index to test - if no individual index, just grab first one from list.
				int propIndex = netItem.index;
				if (propIndex < 0)
				{
					propIndex = netItem.indexes[0];
				}

				// Determine lane to test - if no individual lane, just grab first one from list.
				int lane = netItem.lane;
				if (lane < 0)
				{
					lane = netItem.lanes[0];
				}

				// Replacement pack replacement and original probability (if any).
				BOBNetReplacement packReplacement = NetworkPackReplacement.instance.ActiveReplacement(currentNet, lane, propIndex);
				if (packReplacement != null)
				{
					propListItem.packagePrefab = packReplacement.replacementInfo;
				}
				else
				{
					// If no active current record, ensure that it's reset to null.
					propListItem.packagePrefab = null;
				}

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

				// Network replacement and original probability (if any).
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

				// Individual replacement and original probability (if any).
				BOBNetReplacement individualReplacement = IndividualNetworkReplacement.instance.ActiveReplacement(currentNet, lane, propIndex);
				if (individualReplacement != null)
				{
					propListItem.individualPrefab = individualReplacement.replacementInfo;
					propListItem.individualProb = individualReplacement.probability;
				}
				else
				{
					// If no active current record, ensure that it's reset to null.
					propListItem.individualPrefab = null;
				}
			}
		}


		/// <summary>
		/// Populates the target fastlist with a list of target-specific trees or props.
		/// </summary>
		protected override void TargetList()
		{
			// Clear current selection.
			targetList.selectedIndex = -1;

			// List of prefabs that have passed filtering.
			List<NetPropListItem> propList = new List<NetPropListItem>();

			// Check to see if this building contains any props.
			if (currentNet.m_lanes == null || currentNet.m_lanes.Length == 0)
			{
				// No props - show 'no props' label and return an empty list.
				noPropsLabel.Show();
				targetList.rowsData = new FastList<object>();
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

					// Grouped or individual?
					if (indCheck.isChecked)
					{
						// Individual - set index to the current prop indexes.
						propListItem.index = propIndex;
						propListItem.lane = lane;
					}
					else
					{
						// Grouped - set index to -1 and add to our list of indexes.
						propListItem.index = -1;
						propListItem.lane = -1;
						propListItem.indexes.Add(propIndex);
						propListItem.lanes.Add(lane);
					}

					// Get original (pre-replacement) tree/prop prefab and current probability (as default original probability).
					propListItem.originalPrefab = NetworkReplacement.instance.GetOriginal(currentNet, lane, propIndex) ?? AllNetworkReplacement.instance.GetOriginal(currentNet, lane, propIndex) ?? NetworkPackReplacement.instance.GetOriginal(currentNet, lane, propIndex) ?? finalInfo;
					propListItem.originalProb = laneProps[propIndex].m_probability;
					propListItem.originalAngle = laneProps[propIndex].m_angle;

					// All-network replacement and original probability (if any).
					BOBNetReplacement allNetReplacement = AllNetworkReplacement.instance.ActiveReplacement(currentNet, lane, propIndex);
					if (allNetReplacement != null)
					{
						propListItem.allPrefab = allNetReplacement.replacementInfo;
						propListItem.allProb = allNetReplacement.probability;
					}

					// Network replacement and original probability (if any).
					BOBNetReplacement netReplacement = NetworkReplacement.instance.ActiveReplacement(currentNet, lane, propIndex);
					if (netReplacement != null)
					{
						propListItem.replacementPrefab = netReplacement.replacementInfo;
						propListItem.replacementProb = netReplacement.probability;
					}

					// Individual replacement and original probability (if any).
					BOBNetReplacement individualReplacement = IndividualNetworkReplacement.instance.ActiveReplacement(currentNet, lane, propIndex);
					if (individualReplacement != null)
					{
						propListItem.individualPrefab = individualReplacement.replacementInfo;
						propListItem.individualProb = individualReplacement.probability;
					}

					// Replacement pack replacement and original probability (if any).
					BOBNetReplacement packReplacement = NetworkPackReplacement.instance.ActiveReplacement(currentNet, lane, propIndex);
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
							// Check to see if we already have this in the list - matching original prefab, individual replacement prefab, network replacement prefab, all-network replacement prefab, and probability.
							if (item.originalPrefab == propListItem.originalPrefab && item.individualPrefab == propListItem.individualPrefab && item.replacementPrefab == propListItem.replacementPrefab && propListItem.allPrefab == item.allPrefab)
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
			targetList.rowsData = new FastList<object>()
			{
				m_buffer = targetSearchStatus == (int)OrderBy.NameDescending ? propList.OrderByDescending(item => item.DisplayName).ToArray() : propList.OrderBy(item => item.DisplayName).ToArray(),
				m_size = propList.Count
			};

			targetList.Refresh();

			// If the list is empty, show the 'no props' label; otherwise, hide it.
			if (propList.Count == 0)
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
