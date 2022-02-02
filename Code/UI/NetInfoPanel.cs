using System;
using System.Linq;
using System.Collections.Generic;
using ColossalFramework.UI;
using UnityEngine;


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
		private NetTargetListItem currentNetItem;

		// Original selection values.
		private NetPropReference[] originalValues;


		/// <summary>
		/// Returns the current individual lane number of the current selection.  This could be either the direct lane or in the lane array, depending on situation.
		/// </summary>
		private int IndividualLane => currentNetItem.lane < 0 ? currentNetItem.lanes[0] : currentNetItem.lane;


		/// <summary>
		/// Mode icon atlas names for prop modes.
		/// </summary>
		protected override string[] PropModeAtlas => new string[(int)ReplacementModes.NumModes]
		{
			"BOB-ThisPropSmall",
			"BOB-SamePropSmall",
			"BOB-RoadsSmall"
		};


		/// <summary>
		/// Mode icon atlas names for tree modes.
		/// </summary>
		protected override string[] TreeModeAtlas => new string[(int)ReplacementModes.NumModes]
		{
			"BOB-ThisTreeSmall",
			"BOB-SameTreeSmall",
			"BOB-RoadsSmall"
		};


		/// <summary>
		/// Mode icon tootlip keys for prop modes.
		/// </summary>
		protected override string[] PropModeTipKeys => new string[(int)ReplacementModes.NumModes]
		{
			"BOB_PNL_M_PIN",
			"BOB_PNL_M_PGN",
			"BOB_PNL_M_PAN"
		};


		/// <summary>
		/// Mode icon tootlip keys for tree modes.
		/// </summary>
		protected override string[] TreeModeTipKeys => new string[(int)ReplacementModes.NumModes]
		{
			"BOB_PNL_M_TIN",
			"BOB_PNL_M_TGN",
			"BOB_PNL_M_TAN"
		};


		/// <summary>
		/// Handles changes to the currently selected target prefab.
		/// </summary>
		internal override TargetListItem CurrentTargetItem
        {
			set
			{
				// First, undo any preview.
				RevertPreview();

				// Call base, while ignoring replacement prefab change live application.
				ignoreSelectedPrefabChange = true;
				base.CurrentTargetItem = value;
				ignoreSelectedPrefabChange = false;

				// Set net item reference.
				currentNetItem = value as NetTargetListItem;

				// Record original stats for preview.
				RecordOriginal();

				// Ensure valid selections before proceeding.
				if (currentNetItem != null && SelectedNet != null)
				{
					// Set lane highlighting selection for individual items.
					if (currentNetItem.lane > -1)
					{
						RenderOverlays.CurrentLane = SelectedNet.m_lanes[currentNetItem.lane];
					}

					// If we've got an individual replacement, update the offset fields with the replacement values.
					if (CurrentTargetItem.individualPrefab != null)
					{
						// Use IndividualIndex and IndividualLane to handle case of switching from individual to grouped props (values will be -1, actual values in relevant lists).
						SetSliders(IndividualNetworkReplacement.Instance.EligibileReplacement(SelectedNet, CurrentTargetItem.originalPrefab, IndividualLane, IndividualIndex));

						// All done here.
						return;
					}
					// Ditto for any network replacement.
					else if (CurrentTargetItem.replacementPrefab != null)
					{
						// Get replacement and update control values.
						SetSliders(NetworkReplacement.Instance.EligibileReplacement(SelectedNet, CurrentTargetItem.originalPrefab, -1, -1));

						// All done here.
						return;
					}
					// Ditto for any all-network replacement.
					else if (CurrentTargetItem.allPrefab != null)
					{
						// Get replacement and update control values.
						SetSliders(AllNetworkReplacement.Instance.EligibileReplacement(SelectedNet, CurrentTargetItem.originalPrefab, -1, -1));

						// All done here.
						return;
					}
				}

				// If we got here, there's no valid current selection; set all offset fields to defaults by passing null to SetSliders().
				SetSliders(null);
			}
		}


		/// <summary>
		/// Constructor.
		/// </summary>
		internal BOBNetInfoPanel()
        {
			try
			{
				// Add pack button.
				UIButton packButton = AddIconButton(this, PackButtonX, ToggleY, ToggleSize, "BOB_PNL_PKB", TextureUtils.LoadSpriteAtlas("BOB-PropPack"));
				packButton.eventClicked += (component, clickEvent) => PackPanelManager.Create();

				// Populate loaded list.
				LoadedList();
			}
			catch (Exception e)
			{
				// Log and report any exception.
				Logging.LogException(e, "exception creating network panel");
			}
		}


		/// <summary>
		/// Sets the target network.
		/// </summary>
		/// <param name="targetPrefabInfo">Target network to set</param>
		internal override void SetTarget(PrefabInfo targetPrefabInfo)
		{
			// Don't do anything if target hasn't changed.
			if (SelectedNet == targetPrefabInfo)
			{
				return;
			}

			// Base setup.
			base.SetTarget(targetPrefabInfo);

			// Populate target list and select target item.
			TargetList();

			// Apply Harmony rendering patches.
			RenderOverlays.CurrentNet = SelectedNet;
			Patcher.PatchNetworkOverlays(true);
		}

		/// <summary>
		/// Record original prop values before previewing.
		/// </summary>
		private void RecordOriginal()
		{
			// Do we have a valid selection?
			if (currentNetItem?.originalPrefab != null)
			{
				// Create new array of original values.
				originalValues = new NetPropReference[currentNetItem.index < 0 ? currentNetItem.indexes.Count() : 1];

				if (currentNetItem.index < 0)
				{
					// Grouped replacement - iterate through each instance and record values.
					for (int i = 0; i < originalValues.Length; ++i)
					{
						originalValues[i] = GetOriginalData(currentNetItem.lanes[i], currentNetItem.indexes[i]);
					}
				}
				else
				{
					// Individual replacement - record original values.
					originalValues[0] = GetOriginalData(currentNetItem.lane, currentNetItem.index);
				}
			}
			else
			{
				originalValues = null;
			}
		}


		/// <summary>
		/// Previews the current change.
		/// </summary>
		protected override void PreviewChange()
		{
			// Don't do anything if no current selection.
			if (CurrentTargetItem == null)
			{
				return;
			}

			// Don't do anything if no changes.
			if (xSlider.value == 0f &&
				ySlider.value == 0f &&
				zSlider.value == 0f &&
				angleSlider.value == 0f &&
				probabilitySlider.value.RoundToNearest(1) == CurrentTargetItem.originalProb &&
				ReplacementPrefab == CurrentTargetItem.CurrentPrefab)
			{
				// Reset apply button icon.
				UnappliedChanges = false;

				return;
			}

			// Grouped or individual replacement?
			if (CurrentTargetItem.index < 0)
			{
				// Grouped; iterate through each index and apply preview.
				for (int i = 0; i < CurrentTargetItem.indexes.Count; ++i)
				{
					PreviewChange(currentNetItem.lanes[i], currentNetItem.indexes[i]);
				}
			}
			else
			{
				// Individual; apply preview.
				PreviewChange(currentNetItem.lane, currentNetItem.index);
			}
		}


		/// <summary>
		/// Reverts any previewed changes back to original prop/tree state.
		/// </summary>
		protected override void RevertPreview()
		{
			// Make sure that we've got valid original values to revert to.
			NetInfo.Lane[] selectedNetLanes = SelectedNet?.m_lanes;
			if (originalValues != null && originalValues.Length > 0 && selectedNetLanes != null)
			{
				// Iterate through each original value.
				for (int i = 0; i < originalValues.Length; ++i)
				{
					// Null check in case any original values failed.
					if (originalValues[i] == null)
					{
						continue;
					}

					// Sanity check indexes.
					int laneIndex = originalValues[i].laneIndex;
					int propIndex = originalValues[i].propIndex;

					if (laneIndex >= selectedNetLanes.Length ||
						selectedNetLanes[laneIndex].m_laneProps == null ||
						propIndex >= selectedNetLanes[laneIndex].m_laneProps.m_props.Length)
                    {
						continue;
                    }


					// Local reference.
					NetLaneProps.Prop thisProp = SelectedNet.m_lanes[laneIndex].m_laneProps.m_props[propIndex];

					// Restore original values.
					thisProp.m_prop = originalValues[i].originalProp;
					thisProp.m_finalProp = originalValues[i].originalFinalProp;
					thisProp.m_tree = originalValues[i].originalTree;
					thisProp.m_finalTree = originalValues[i].originalFinalTree;
					thisProp.m_angle = originalValues[i].angle;
					thisProp.m_position = originalValues[i].position;
					thisProp.m_probability = originalValues[i].probability;
				}
			}

			// Clear recorded values.
			originalValues = null;

			// Reset apply button icon
			UnappliedChanges = false;
		}


		/// <summary>
		/// Apply button event handler.
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
		/// </summary>
		protected override void Apply(UIComponent control, UIMouseEventParameter mouseEvent)
		{
			try
			{
				// First, undo any preview.
				RevertPreview();

				// Make sure we have valid a target and replacement.
				if (CurrentTargetItem is NetTargetListItem netItem && ReplacementPrefab != null)
				{
					switch (CurrentMode)
					{
						case ReplacementModes.Individual:
							// Individual replacement.
							IndividualNetworkReplacement.Instance.Replace(SelectedNet, CurrentTargetItem.originalPrefab ?? CurrentTargetItem.replacementPrefab, ReplacementPrefab, netItem.lane, CurrentTargetItem.index, angleSlider.TrueValue, xSlider.TrueValue, ySlider.TrueValue, zSlider.TrueValue, (int)probabilitySlider.TrueValue);

							// Update current target.
							CurrentTargetItem.individualPrefab = ReplacementPrefab;
							CurrentTargetItem.individualProb = (int)probabilitySlider.TrueValue;
							break;

						case ReplacementModes.Grouped:
							// Grouped replacement.
							NetworkReplacement.Instance.Replace(SelectedNet, CurrentTargetItem.originalPrefab ?? CurrentTargetItem.replacementPrefab, ReplacementPrefab, -1, -1, angleSlider.TrueValue, xSlider.TrueValue, ySlider.TrueValue, zSlider.TrueValue, (int)probabilitySlider.TrueValue);

							// Update current target.
							CurrentTargetItem.replacementPrefab = ReplacementPrefab;
							CurrentTargetItem.replacementProb = (int)probabilitySlider.TrueValue;
							break;

						case ReplacementModes.All:
							// All- replacement.
							AllNetworkReplacement.Instance.Replace(null, CurrentTargetItem.originalPrefab ?? CurrentTargetItem.replacementPrefab, ReplacementPrefab, -1, -1, angleSlider.TrueValue, xSlider.TrueValue, ySlider.TrueValue, zSlider.TrueValue, (int)probabilitySlider.TrueValue);

							// Update current target.
							CurrentTargetItem.allPrefab = ReplacementPrefab;
							CurrentTargetItem.allProb = (int)probabilitySlider.TrueValue;
							break;

						default:
							Logging.Error("invalid replacement mode at NetInfoPanel.Apply");
							return;
					}

					// Update target list and buttons.
					targetList.Refresh();
					UpdateButtonStates();

					// Update highlighting target.
					RenderOverlays.CurrentProp = ReplacementPrefab as PropInfo;
					RenderOverlays.CurrentTree = ReplacementPrefab as TreeInfo;

					// Record updated original data.
					RecordOriginal();
				}
			}
			catch (Exception e)
			{
				// Log and report any exception.
				Logging.LogException(e, "exception applying network replacement");
			}
		}


		/// <summary>
		/// Revert button event handler.
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
		/// </summary>
		protected override void Revert(UIComponent control, UIMouseEventParameter mouseEvent)
		{
			// Revert any unapplied changes first.
			if (UnappliedChanges)
			{
				RevertPreview();

				// Reset slider values by reassigning the current target item.
				CurrentTargetItem = CurrentTargetItem;

				return;
			}

			// First, undo any preview.
			RevertPreview();

			try
			{
				// Make sure we've got a valid selection.
				if (currentNetItem == null)
				{
					return;
				}

				// Individual prop reversion?
				if (currentNetItem.individualPrefab != null)
				{
					// Individual reversion - use IndividualLane and IndividualIndex to ensure valid values for current context are used.
					IndividualNetworkReplacement.Instance.Revert(SelectedNet, currentNetItem.originalPrefab, IndividualLane, IndividualIndex, true);

					// Clear current target replacement prefab.
					CurrentTargetItem.individualPrefab = null;
				}
				else if (currentNetItem.replacementPrefab != null)
				{
					// Network reversion.
					NetworkReplacement.Instance.Revert(SelectedNet, currentNetItem.originalPrefab, -1, -1, true);
				}
				else if (currentNetItem.allPrefab != null)
				{
					// All-network reversion.
					AllNetworkReplacement.Instance.Revert(SelectedNet, currentNetItem.originalPrefab, -1, -1, true);
				}

				// Update current item.
				UpdateTargetItem(currentNetItem);

				// Update controls.
				CurrentTargetItem = currentNetItem;

				// Update target list.
				targetList.Refresh();
			}
			catch (Exception e)
			{
				// Log and report any exception.
				Logging.LogException(e, "exception perforiming network reversion");
			}
		}


		/// <summary>
		/// Updates the target item record for changes in replacement status (e.g. after applying or reverting changes).
		/// </summary>
		/// <param name="propListItem">Target item</param>
		protected override void UpdateTargetItem(TargetListItem targetListItem)
		{
			if (targetListItem is NetTargetListItem netItem)
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
				BOBNetReplacement packReplacement = NetworkPackReplacement.Instance.ActiveReplacement(SelectedNet, lane, propIndex);
				if (packReplacement != null)
				{
					targetListItem.packagePrefab = packReplacement.replacementInfo;
				}
				else
				{
					// If no active current record, ensure that it's reset to null.
					targetListItem.packagePrefab = null;
				}

				// All-network replacement and original probability (if any).
				BOBNetReplacement allNetReplacement = AllNetworkReplacement.Instance.ActiveReplacement(SelectedNet, lane, propIndex);
				if (allNetReplacement != null)
				{
					targetListItem.allPrefab = allNetReplacement.replacementInfo;
					targetListItem.allProb = allNetReplacement.probability;
				}
				else
				{
					// If no active current record, ensure that it's reset to null.
					targetListItem.allPrefab = null;
				}

				// Network replacement and original probability (if any).
				BOBNetReplacement netReplacement = NetworkReplacement.Instance.ActiveReplacement(SelectedNet, lane, propIndex);
				if (netReplacement != null)
				{
					targetListItem.replacementPrefab = netReplacement.replacementInfo;
					targetListItem.replacementProb = netReplacement.probability;
				}
				else
				{
					// If no active current record, ensure that it's reset to null.
					targetListItem.replacementPrefab = null;
				}

				// Individual replacement and original probability (if any).
				BOBNetReplacement individualReplacement = IndividualNetworkReplacement.Instance.ActiveReplacement(SelectedNet, lane, propIndex);
				if (individualReplacement != null)
				{
					targetListItem.individualPrefab = individualReplacement.replacementInfo;
					targetListItem.individualProb = individualReplacement.probability;
				}
				else
				{
					// If no active current record, ensure that it's reset to null.
					targetListItem.individualPrefab = null;
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
			List<TargetListItem> itemList = new List<TargetListItem>();

			// Check to see if this building contains any lanes.
			if (SelectedNet?.m_lanes == null || SelectedNet.m_lanes.Length == 0)
			{
				// No lanes - show 'no props' label and return an empty list.
				noPropsLabel.Show();
				targetList.rowsData = new FastList<object>();

				// Force clearance of current target item.
				CurrentTargetItem = null;

				return;
			}

			// Local reference.
			NetInfo.Lane[] lanes = SelectedNet.m_lanes;

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
					NetTargetListItem targetListItem = new NetTargetListItem();

					// Try to get relevant prefab (prop/tree), using finalProp.
					PrefabInfo finalInfo = IsTree ? (PrefabInfo)laneProps[propIndex]?.m_finalTree : (PrefabInfo)laneProps[propIndex]?.m_finalProp;

					// Check to see if we were succesful - if not (e.g. we only want trees and this is a prop), continue on to next building prop.
					if (finalInfo?.name == null)
					{
						continue;
					}

					// Get original (pre-replacement) tree/prop prefab and current probability (as default original probability).
					targetListItem.originalPrefab = finalInfo;
					targetListItem.originalProb = laneProps[propIndex].m_probability;
					targetListItem.originalAngle = laneProps[propIndex].m_angle;

					// Grouped or individual?
					if (CurrentMode == (int)ReplacementModes.Individual)
					{
						// Individual - set index to the current prop indexes.
						targetListItem.index = propIndex;
						targetListItem.lane = lane;
					}
					else
					{
						// Grouped - set index to -1 and add to our list of indexes.
						targetListItem.index = -1;
						targetListItem.lane = -1;
						targetListItem.indexes.Add(propIndex);
						targetListItem.lanes.Add(lane);
					}

					// All-network replacement and original probability (if any).
					BOBNetReplacement allNetReplacement = AllNetworkReplacement.Instance.ActiveReplacement(SelectedNet, lane, propIndex);
					if (allNetReplacement != null)
					{
						targetListItem.allPrefab = allNetReplacement.replacementInfo;
						targetListItem.allProb = allNetReplacement.probability;

						// Update original prop reference.
						targetListItem.originalPrefab = allNetReplacement.targetInfo;


						// See if we can find an active reference.
						Logging.Message("finding original probability - all-network");
						NetPropReference originalReference = allNetReplacement?.references?.Find(x =>  x.netInfo == SelectedNet && x.laneIndex == lane && x.propIndex == propIndex);
						if (originalReference != null)
                        {
							// Original reference found; update original probability.
							targetListItem.originalProb = originalReference.probability;
                        }
						Logging.Message("original probability not found");
					}

					// Network replacement and original probability (if any).
					BOBNetReplacement netReplacement = NetworkReplacement.Instance.ActiveReplacement(SelectedNet, lane, propIndex);
					if (netReplacement != null)
					{
						targetListItem.replacementPrefab = netReplacement.replacementInfo;
						targetListItem.replacementProb = netReplacement.probability;

						// Update original prop reference.
						targetListItem.originalPrefab = netReplacement.targetInfo;
						Logging.Message("finding original probability - grouped");
						targetListItem.originalProb = netReplacement.references.Find(x => x.laneIndex == lane && x.propIndex == propIndex).probability;
						Logging.Message("original probability found");
					}

					// Individual replacement and original probability (if any).
					BOBNetReplacement individualReplacement = IndividualNetworkReplacement.Instance.ActiveReplacement(SelectedNet, lane, propIndex);
					if (individualReplacement != null)
					{
						targetListItem.individualPrefab = individualReplacement.replacementInfo;
						targetListItem.individualProb = individualReplacement.probability;
						Logging.Message("finding original probability - individual");
						targetListItem.originalProb = individualReplacement.references.Find(x => x.laneIndex == lane && x.propIndex == propIndex).probability;
						Logging.Message("original probability found");

						// Update original prop reference.
						targetListItem.originalPrefab = individualReplacement.targetInfo;
					}

					// Replacement pack replacement and original probability (if any).
					BOBNetReplacement packReplacement = NetworkPackReplacement.Instance.ActiveReplacement(SelectedNet, lane, propIndex);
					if (packReplacement != null)
					{
						targetListItem.packagePrefab = packReplacement.replacementInfo;

						// Update original prop reference.
						targetListItem.originalPrefab = packReplacement.targetInfo;
						targetListItem.originalProb = packReplacement.references.Find(x => x.laneIndex == lane && x.propIndex == propIndex).probability;
					}

					// Are we grouping?
					if (targetListItem.index == -1)
					{
						// Yes, grouping - initialise a flag to show if we've matched.
						bool matched = false;

						// Iterate through each item in our existing list of props.
						foreach (NetTargetListItem item in itemList)
						{
							// Check to see if we already have this in the list - matching original prefab, individual replacement prefab, network replacement prefab, all-network replacement prefab, and probability.
							if (item.originalPrefab == targetListItem.originalPrefab && item.individualPrefab == targetListItem.individualPrefab && item.replacementPrefab == targetListItem.replacementPrefab && targetListItem.allPrefab == item.allPrefab)
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
					itemList.Add(targetListItem);
				}
			}

			// Create return fastlist from our filtered list, ordering by name.
			targetList.rowsData = new FastList<object>()
			{
				m_buffer = targetSearchStatus == (int)OrderBy.NameDescending ? itemList.OrderByDescending(item => item.DisplayName).ToArray() : itemList.OrderBy(item => item.DisplayName).ToArray(),
				m_size = itemList.Count
			};

			targetList.Refresh();

			// If the list is empty, show the 'no props' label; otherwise, hide it.
			if (itemList.Count == 0)
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


		/// <summary>
		/// Previews the change for the given prop index.
		/// </summary>
		/// <param name="lane">Lane index</param>
		/// <param name="index">Prop index</param>
		private void PreviewChange(int lane, int index)
		{
			// Ensure that original values have been recorded before proceeding.
			if (originalValues == null)
			{
				return;
			}

			// Original position.
			Vector3 basePosition = new Vector3();

			// Find matching prop reference (by lane and index match) in original values.
			foreach (NetPropReference propReference in originalValues)
			{
				if (propReference != null && propReference.laneIndex == lane && propReference.propIndex == index)
				{
					// Found a match - retrieve original position.
					basePosition = propReference.position - propReference.adjustment;
					break;
				}
			}

			// Null check.
			NetInfo.Lane thisLane = SelectedNet?.m_lanes?[lane];
			NetLaneProps.Prop thisProp = thisLane?.m_laneProps?.m_props?[index];
			if (thisProp == null)
			{
				return;
			}

			// Calculate preview X position, taking into account mirrored trees/props, inverting x offset to match original prop x position.
			float offsetX = xSlider.TrueValue;
			if (thisLane.m_position + basePosition.x < 0)
			{
				offsetX = 0 - offsetX;
			}

			// Preview new position and probability setting.
			thisProp.m_position = basePosition + new Vector3(offsetX, ySlider.TrueValue, zSlider.TrueValue);
			thisProp.m_probability = (int)probabilitySlider.TrueValue;
			thisProp.m_angle = (int)angleSlider.TrueValue;

			// If a replacement prefab has been selected, then update it too.
			if (ReplacementPrefab != null)
			{
				thisProp.m_prop = ReplacementPrefab as PropInfo;
				thisProp.m_tree = ReplacementPrefab as TreeInfo;
				thisProp.m_finalProp = ReplacementPrefab as PropInfo;
				thisProp.m_finalTree = ReplacementPrefab as TreeInfo;

				// Update highlighting target.
				RenderOverlays.CurrentProp = ReplacementPrefab as PropInfo;
				RenderOverlays.CurrentTree = ReplacementPrefab as TreeInfo;
			}

			// Update apply button icon to indicate change.
			UnappliedChanges = true;
		}


		/// <summary>
		/// Gets original (current) prop data.
		/// </summary>
		/// <param name="lane">Lane index</param>
		/// <param name="propIndex">Prop index</param>
		/// <returns>Original prop data</returns>
		private NetPropReference GetOriginalData(int lane, int propIndex)
		{
			// Ensure that the indexes are valid before proceeding.
			if (SelectedNet?.m_lanes == null || SelectedNet.m_lanes.Length <= lane)
			{
				Logging.Error("invalid lane index reference of ", lane, " for selected network ", SelectedNet?.name ?? "null");
				return null;
			}
			NetLaneProps.Prop[] propBuffer = SelectedNet.m_lanes[lane]?.m_laneProps?.m_props;
			if (propBuffer == null || propBuffer.Length <= propIndex)
            {
				Logging.Error("invalid prop index reference of ", propIndex, " for lane ", lane, " of selected network ", SelectedNet?.name ?? "null");
				return null;
			}

			// Local reference.
			NetLaneProps.Prop thisProp = propBuffer[propIndex];

			// Get any position adjustments from active replacements, checking in priority order.
			Vector3 adjustment = Vector3.zero;
			if (IndividualNetworkReplacement.Instance.ActiveReplacement(SelectedNet, lane, propIndex) is BOBNetReplacement individualReplacement)
			{
				// Individual replacement.
				adjustment.x = individualReplacement.offsetX;
				adjustment.y = individualReplacement.offsetY;
				adjustment.z = individualReplacement.offsetZ;
			}
			else if (NetworkReplacement.Instance.ActiveReplacement(SelectedNet, lane, propIndex) is BOBNetReplacement netReplacement)
			{
				// Grouped replacement.
				adjustment.x = netReplacement.offsetX;
				adjustment.y = netReplacement.offsetY;
				adjustment.z = netReplacement.offsetZ;
			}
			else if (AllNetworkReplacement.Instance.ActiveReplacement(SelectedNet, lane, propIndex) is BOBNetReplacement allNetReplacement)
			{
				// All- replacement.
				adjustment.x = allNetReplacement.offsetX;
				adjustment.y = allNetReplacement.offsetY;
				adjustment.z = allNetReplacement.offsetZ;
			}

			// Return original data.
			return new NetPropReference
			{
				laneIndex = lane,
				propIndex = propIndex,
				originalProp = thisProp.m_prop,
				originalTree = thisProp.m_tree,
				originalFinalProp = thisProp.m_finalProp,
				originalFinalTree = thisProp.m_finalTree,
				angle = thisProp.m_angle,
				position = thisProp.m_position,
				adjustment = adjustment,
				probability = thisProp.m_probability
			};
		}
	}
}
