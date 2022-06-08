using System;
using System.Linq;
using System.Collections.Generic;
using ColossalFramework;
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
		protected const float LaneX = ModeX + (ToggleSize * 3f) + Margin;


		// Current selection reference.
		private NetTargetListItem currentNetItem;

		// Original selection values.
		private NetPropReference[] originalValues;

		// Panel components.
		private UIDropDown laneMenu;
		private BOBSlider repeatSlider;

		// Event suppression.
		private bool ignoreIndexChange = false;


		/// <summary>
		/// Returns the current individual lane number of the current selection.  This could be either the direct lane or in the lane array, depending on situation.
		/// </summary>
		private int IndividualLane => currentNetItem.lane < 0 ? currentNetItem.lanes[0] : currentNetItem.lane;


		/// <summary>
		/// Currently selected lane.
		/// </summary>
		private int SelectedLane => laneMenu.selectedIndex - 1;


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

					// Is this an added prop?
					if (CurrentTargetItem.isAdded)
					{
						Logging.Message("setting sliders for added prop at index ", IndividualIndex);

						// Yes - set sliders directly.
						// Disable events.
						ignoreSliderValueChange = true;

						// Valid replacement - set slider values.
						NetInfo.Lane lane = SelectedNet.m_lanes[IndividualLane];
						NetLaneProps.Prop laneProp = lane.m_laneProps.m_props[IndividualIndex];
						angleSlider.TrueValue = laneProp.m_angle;
						xSlider.TrueValue = lane.m_position < 0 ? -laneProp.m_position.x : laneProp.m_position.x;
						ySlider.TrueValue = laneProp.m_position.y;
						zSlider.TrueValue = laneProp.m_position.z;
						probabilitySlider.TrueValue = laneProp.m_probability;
						repeatSlider.TrueValue = laneProp.m_repeatDistance;
						repeatSlider.parent.Show();

						// Re-enable events.
						ignoreSliderValueChange = false;

						// All done here.
						return;
					}
					else
					{
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
				}

				// If we got here, there's no valid current selection; set all offset fields to defaults by passing null to SetSliders().
				SetSliders(null);
			}
		}


		/// <summary>
		/// Current replacement mode.
		/// </summary>
		protected override ReplacementModes CurrentMode
		{
			set
			{
				// Add and remove buttons, lane menu, and repeat distance slider are only valid in individual mode.
				bool isIndividual = value == ReplacementModes.Individual;
				repeatSlider.parent.isVisible = isIndividual;
				addButton.isVisible = isIndividual;
				removeButton.isVisible = isIndividual;
				laneMenu.isVisible = isIndividual;

				base.CurrentMode = value;
			}
		}


		/// <summary>
		/// Constructor.
		/// </summary>
		internal BOBNetInfoPanel()
		{
			try
			{
				// Add lane menu.
				// Mode label.
				laneMenu = UIControls.AddDropDown(this, LaneX, ToggleY + 3f, MiddleX - LaneX);
				UIControls.AddLabel(laneMenu, 0f, -ToggleHeaderHeight - 3f, Translations.Translate("BOB_PNL_LAN"), textScale: 0.8f);
				laneMenu.tooltipBox = TooltipUtils.TooltipBox;
				laneMenu.tooltip = Translations.Translate("BOB_PNL_LAN_TIP");
				laneMenu.eventSelectedIndexChanged += LaneIndexChanged;
				laneMenu.isVisible = CurrentMode == ReplacementModes.Individual;

				// Add pack button.
				UIButton packButton = AddIconButton(this, PackButtonX, ToggleY, ToggleSize, "BOB_PNL_PKB", TextureUtils.LoadSpriteAtlas("BOB-PropPack"));
				packButton.eventClicked += (component, clickEvent) => PackPanelManager.Create();

				// Add repeat slider.
				UIPanel repeatPanel = Sliderpanel(this, MidControlX, RepeatSliderY + Margin, SliderHeight);
				repeatSlider = AddBOBSlider(repeatPanel, Margin, 0f, MidControlWidth - (Margin * 2f), "BOB_PNL_REP", 1.1f, 50f, 0.1f, "Repeat");
				repeatSlider.tooltip = Translations.Translate("BOB_PNL_REP_TIP");
				repeatSlider.parent.isVisible = CurrentMode == ReplacementModes.Individual;
				repeatSlider.eventTrueValueChanged += SliderChange;

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

			// Build lane menu selection list, with 'all lanes' at index 0, selected by default.
			ignoreIndexChange = true; ;
			string[] laneMenuItems = new string[SelectedNet.m_lanes.Length + 1];
			laneMenuItems[0] = Translations.Translate("BOB_PNL_LAN_ALL");
			for (int i = 1; i < laneMenuItems.Length; ++i)
			{
				// Offset by one to allow for 'all' selection at index zero.
				laneMenuItems[i] = (i - 1).ToString();
			}
			laneMenu.items = laneMenuItems;

			// Set selection to default 'all' and resume lane selection event handling.
			laneMenu.selectedIndex = 0;
			ignoreIndexChange = false;

			// Populate target list and select target item.
			TargetList();

			// Apply Harmony rendering patches.
			RenderOverlays.CurrentNet = SelectedNet;
			Patcher.PatchNetworkOverlays(true);
		}


		/// <summary>
		/// Adds a new tree or prop.
		/// </summary>
		protected override void AddNew()
		{
			// Make sure a valid replacement prefab is set and we have a valid lane selection.
			if (ReplacementPrefab != null && laneMenu.selectedIndex > 0)
			{
				// Revert any preview.
				RevertPreview();

				// Add new prop.
				BOBNetReplacement newProp = new BOBNetReplacement
				{
					laneIndex = SelectedLane,
					isTree = ReplacementPrefab is TreeInfo,
					Replacement = ReplacementPrefab.name,
					angle = angleSlider.TrueValue,
					offsetX = xSlider.TrueValue,
					offsetY = ySlider.TrueValue,
					offsetZ = zSlider.TrueValue,
					probability = (int)probabilitySlider.TrueValue,
					parentInfo = SelectedNet,
					replacementInfo = ReplacementPrefab,
					repeatDistance = repeatSlider.TrueValue
				};
				AddedNetworkProps.Instance.AddNew(newProp);

				// Post-action cleanup.
				UpdateAddedPops();
			}
		}


		/// <summary>
		/// Removes an added tree or prop.
		/// </summary>
		protected override void RemoveProp()
		{
			// Safety first - need an individual index that's an added prop.
			if (CurrentTargetItem == null || CurrentTargetItem.index < 0 || currentNetItem.lane < 0 || !AddedNetworkProps.Instance.IsAdded(SelectedNet, currentNetItem.lane, CurrentTargetItem.index))
			{
				return;
			}

			// First, revert any preview (to prevent any clobbering when preview is reverted).
			RevertPreview();

			// Create new props array with one fewer entry, and copy the old props to it.
			// Remove prop reference and update other references as appropriate.
			AddedNetworkProps.Instance.RemoveNew(SelectedNet, currentNetItem.lane, CurrentTargetItem.index);

			// Post-action cleanup.
			UpdateAddedPops();
		}


		/// <summary>
		/// Called after any added prop manipulations (addition or removal) to perform cleanup.
		/// </summary>
		private void UpdateAddedPops()
		{
			// Clear current selection.
			CurrentTargetItem = null;

			// Perform regular post-processing.
			FinishUpdate();
			TargetList();
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
			if (xSlider.TrueValue == 0f &&
				ySlider.TrueValue == 0f &&
				zSlider.TrueValue == 0f &&
				angleSlider.TrueValue == 0f &&
				probabilitySlider.TrueValue.RoundToNearest(1) == CurrentTargetItem.originalProb &&
				ReplacementPrefab == CurrentTargetItem.CurrentPrefab &&
				repeatSlider.TrueValue == currentNetItem.originalRepeat)
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
					thisProp.m_repeatDistance = originalValues[i].repeatDistance;
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
					// Check for added prop - instead of replacing, we update the original added prop reference.
					if (CurrentTargetItem.isAdded)
					{
						AddedNetworkProps.Instance.Update(SelectedNet, CurrentTargetItem.originalPrefab, ReplacementPrefab, netItem.lane, netItem.index, angleSlider.TrueValue, xSlider.TrueValue, ySlider.TrueValue, zSlider.TrueValue, (int)probabilitySlider.TrueValue, repeatSlider.TrueValue);

						// Update current target.
						CurrentTargetItem.originalPrefab = ReplacementPrefab;
						CurrentTargetItem.originalProb = (int)probabilitySlider.TrueValue;
						netItem.originalRepeat = repeatSlider.TrueValue;
					}
					else
					{
						switch (CurrentMode)
						{
							case ReplacementModes.Individual:
								// Individual replacement.
								IndividualNetworkReplacement.Instance.Replace(SelectedNet, CurrentTargetItem.originalPrefab ?? CurrentTargetItem.replacementPrefab, ReplacementPrefab, netItem.lane, netItem.index, angleSlider.TrueValue, xSlider.TrueValue, ySlider.TrueValue, zSlider.TrueValue, (int)probabilitySlider.TrueValue, repeatSlider.TrueValue);

								// Update current target.
								CurrentTargetItem.individualPrefab = ReplacementPrefab;
								CurrentTargetItem.individualProb = (int)probabilitySlider.TrueValue;
								netItem.individualRepeat = repeatSlider.TrueValue;
								break;

							case ReplacementModes.Grouped:
								// Grouped replacement.
								NetworkReplacement.Instance.Replace(SelectedNet, CurrentTargetItem.originalPrefab ?? CurrentTargetItem.replacementPrefab, ReplacementPrefab, -1, -1, angleSlider.TrueValue, xSlider.TrueValue, ySlider.TrueValue, zSlider.TrueValue, (int)probabilitySlider.TrueValue, -1);

								// Update current target.
								CurrentTargetItem.replacementPrefab = ReplacementPrefab;
								CurrentTargetItem.replacementProb = (int)probabilitySlider.TrueValue;
								break;

							case ReplacementModes.All:
								// All- replacement.
								AllNetworkReplacement.Instance.Replace(null, CurrentTargetItem.originalPrefab ?? CurrentTargetItem.replacementPrefab, ReplacementPrefab, -1, -1, angleSlider.TrueValue, xSlider.TrueValue, ySlider.TrueValue, zSlider.TrueValue, (int)probabilitySlider.TrueValue, -1);

								// Update current target.
								CurrentTargetItem.allPrefab = ReplacementPrefab;
								CurrentTargetItem.allProb = (int)probabilitySlider.TrueValue;
								break;

							default:
								Logging.Error("invalid replacement mode at NetInfoPanel.Apply");
								return;
						}
					}

					// Update target list and buttons.
					targetList.Refresh();
					UpdateButtonStates();

					// Update highlighting target.
					RenderOverlays.CurrentProp = ReplacementPrefab as PropInfo;
					RenderOverlays.CurrentTree = ReplacementPrefab as TreeInfo;

					// Record updated original data.
					RecordOriginal();

					// Perform post-replacement processing.
					FinishUpdate();
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
				// Reset slider values by reassigning the current target item - this will also revert any preview.
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

				// Perform post-replacement processing.
				FinishUpdate();
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

				// Is this an added prop?
				if (AddedNetworkProps.Instance.IsAdded(SelectedNet, lane, propIndex))
				{
					targetListItem.index = propIndex;
					targetListItem.isAdded = true;
				}
				else
				{
					// Replacement pack replacement and original probability (if any).
					BOBNetReplacement packReplacement = NetworkPackReplacement.Instance.ActiveReplacement(SelectedNet, lane, propIndex, out _);
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
					BOBNetReplacement allNetReplacement = AllNetworkReplacement.Instance.ActiveReplacement(SelectedNet, lane, propIndex, out _);
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
					BOBNetReplacement netReplacement = NetworkReplacement.Instance.ActiveReplacement(SelectedNet, lane, propIndex, out _);
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
					BOBNetReplacement individualReplacement = IndividualNetworkReplacement.Instance.ActiveReplacement(SelectedNet, lane, propIndex, out _);
					if (individualReplacement != null)
					{
						targetListItem.individualPrefab = individualReplacement.replacementInfo;
						targetListItem.individualProb = individualReplacement.probability;
						netItem.individualRepeat = individualReplacement.repeatDistance;
					}
					else
					{
						// If no active current record, ensure that it's reset to null.
						targetListItem.individualPrefab = null;
					}
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
				if (CurrentMode == (int)ReplacementModes.Individual)
				{
					// If individual mode and a lane has been selected, skip any lanes not selected.
					if (CurrentMode == (int)ReplacementModes.Individual && laneMenu.selectedIndex > 0 && lane != SelectedLane)
					{
						continue;
					}
				}
				
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

					// Try to get relevant prefab (prop/tree), falling back to the other type if null (to allow for tree-prop changes), using finalProp.
					PrefabInfo finalInfo = null;
					if (PropTreeMode == PropTreeModes.Tree)
					{
						finalInfo = (PrefabInfo)laneProps[propIndex]?.m_finalTree ?? laneProps[propIndex]?.m_finalProp;
					}
					else
					{
						finalInfo = (PrefabInfo)laneProps[propIndex]?.m_finalProp ?? laneProps[propIndex]?.m_finalTree;
					}

					// Check to see if we were succesful - if not (e.g. we only want trees and this is a prop), continue on to next building prop.
					if (finalInfo?.name == null)
					{
						continue;
					}

					// Get original (pre-replacement) tree/prop prefab and current probability (as default original probability).
					targetListItem.originalPrefab = finalInfo;
					targetListItem.originalProb = laneProps[propIndex].m_probability;
					targetListItem.originalAngle = laneProps[propIndex].m_angle;
					targetListItem.originalRepeat = laneProps[propIndex].m_repeatDistance;

					// Is this an added prop?
					if (AddedNetworkProps.Instance.IsAdded(lanes[lane], propIndex))
					{
						targetListItem.index = propIndex;
						targetListItem.lane = lane;
						targetListItem.isAdded = true;
					}
					else
					{
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

						// To record original data if a replacement is in effect.
						NetPropReference propReference = null;

						// Replacement pack replacement and original probability (if any).
						BOBNetReplacement packReplacement = NetworkPackReplacement.Instance.ActiveReplacement(SelectedNet, lane, propIndex, out propReference);
						if (packReplacement != null)
						{
							targetListItem.packagePrefab = packReplacement.replacementInfo;
							targetListItem.originalPrefab = packReplacement.targetInfo;
						}

						// All-network replacement and original probability (if any).
						BOBNetReplacement allNetReplacement = AllNetworkReplacement.Instance.ActiveReplacement(SelectedNet, lane, propIndex, out propReference);
						if (allNetReplacement != null)
						{
							targetListItem.allPrefab = allNetReplacement.replacementInfo;
							targetListItem.allProb = allNetReplacement.probability;
							targetListItem.originalPrefab = allNetReplacement.targetInfo;
						}

						// Network replacement and original probability (if any).
						BOBNetReplacement netReplacement = NetworkReplacement.Instance.ActiveReplacement(SelectedNet, lane, propIndex, out propReference);
						if (netReplacement != null)
						{
							targetListItem.replacementPrefab = netReplacement.replacementInfo;
							targetListItem.replacementProb = netReplacement.probability;
							targetListItem.originalPrefab = netReplacement.targetInfo;
						}

						// Individual replacement and original probability (if any).
						BOBNetReplacement individualReplacement = IndividualNetworkReplacement.Instance.ActiveReplacement(SelectedNet, lane, propIndex, out propReference);
						if (individualReplacement != null)
						{
							targetListItem.individualPrefab = individualReplacement.replacementInfo;
							targetListItem.individualProb = individualReplacement.probability;
							targetListItem.individualRepeat = individualReplacement.repeatDistance;
							targetListItem.originalPrefab = individualReplacement.targetInfo;
						}

						// If we found an active replacement, update original reference values.
						if (propReference != null)
						{
							//targetListItem.originalPrefab = propReference.OriginalInfo;
							targetListItem.originalAngle = propReference.angle;
							targetListItem.originalProb = propReference.probability;
							targetListItem.originalRepeat = propReference.repeatDistance;
						}
					}

					// Check for match with 'prop' mode - either original or replacement needs to be prop.
					if (PropTreeMode == PropTreeModes.Prop && !(finalInfo is PropInfo) && !(targetListItem.originalPrefab is PropInfo))
					{
						continue;
					}

					// Check for match with 'tree' mode - either original or replacement needs to be tree.
					if (PropTreeMode == PropTreeModes.Tree && !(finalInfo is TreeInfo) && !(targetListItem.originalPrefab is TreeInfo))
					{
						continue;
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
		/// Updates button states (enabled/disabled) according to current control states.
		/// </summary>
		protected override void UpdateButtonStates()
		{
			base.UpdateButtonStates();

			// Make sure add button is only enabled if the lane menu is visible and has a valid lane selection.
			if (addButton != null)
			{
				addButton.isVisible &= laneMenu.isVisible;
				removeButton.isVisible &= laneMenu.isVisible;
				addButton.isEnabled &= laneMenu.selectedIndex > 0;
			}
		}


		/// <summary>
		/// Sets the sliders to the values specified in the given replacement record.
		/// </summary>
		/// <param name="replacement">Replacement record to use</param>
		protected override void SetSliders(BOBReplacementBase replacement)
		{
			// Disable events.
			ignoreSliderValueChange = true;

			// Are we eligible for repeat distance (eligibile target and in individual mode).
			if (CurrentMode == ReplacementModes.Individual && currentNetItem != null && (currentNetItem.originalRepeat > 1f || currentNetItem.isAdded))
			{
				// Yes - do we have a replacement?
				if (replacement is BOBNetReplacement netReplacement && netReplacement.repeatDistance > 1f)
				{
					// Yes - set repeat distance slider value and show the slider.
					repeatSlider.TrueValue = netReplacement.repeatDistance;
				}
				else
				{
					// No replacement; show original value.
					repeatSlider.TrueValue = currentNetItem.originalRepeat;
				}

				// Show slider.
				repeatSlider.parent.Show();
			}
			else
			{
				// Hide repeat slider if no value to show.
				repeatSlider.TrueValue = 0f;
				repeatSlider.parent.Hide();
			}

			base.SetSliders(replacement);
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

			// Original position and angle adjustment.
			Vector3 basePosition = new Vector3();
			float baseAngle = 0f;

			if (!CurrentTargetItem.isAdded)
			{
				// Find matching prop reference (by lane and index match) in original values.
				foreach (NetPropReference propReference in originalValues)
				{
					if (propReference != null && propReference.laneIndex == lane && propReference.propIndex == index)
					{
						// Found a match - retrieve original position and angle.
						basePosition = propReference.position - propReference.adjustment;
						baseAngle = propReference.angle - propReference.angleAdjustment;
						break;
					}
				}
			}

			// Null check.
			NetInfo.Lane thisLane = SelectedNet?.m_lanes?[lane];
			NetLaneProps.Prop thisProp = thisLane?.m_laneProps?.m_props?[index];
			if (thisProp == null)
			{
				return;
			}

			// Calculate preview X position and angle, taking into account mirrored trees/props, inverting x offset to match original prop x position.
			float offsetX = xSlider.TrueValue;
			Logging.Message("xSlsider TrueValue is ", offsetX);
			float angleMult = 1;
			if (thisLane.m_position + basePosition.x < 0)
			{
				offsetX = 0 - offsetX;
				angleMult = -1;
			}

			// Preview new position and probability setting.
			thisProp.m_position = basePosition + new Vector3(offsetX, ySlider.TrueValue, zSlider.TrueValue);
			thisProp.m_probability = (int)probabilitySlider.TrueValue;
			thisProp.m_angle = baseAngle + (angleSlider.TrueValue * angleMult);

			// Set repeat distance, if valid.
			if (repeatSlider.parent.isVisible)
			{
				thisProp.m_repeatDistance = repeatSlider.TrueValue;
			}

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

			// Get any position and angle adjustments from active replacements, checking in priority order.
			Vector3 adjustment = Vector3.zero;
			float angleAdjustment = 0f;
			if (IndividualNetworkReplacement.Instance.ActiveReplacement(SelectedNet, lane, propIndex, out _) is BOBNetReplacement individualReplacement)
			{
				// Individual replacement.
				adjustment.x = individualReplacement.offsetX;
				adjustment.y = individualReplacement.offsetY;
				adjustment.z = individualReplacement.offsetZ;
				angleAdjustment = individualReplacement.angle;
			}
			else if (NetworkReplacement.Instance.ActiveReplacement(SelectedNet, lane, propIndex, out _) is BOBNetReplacement netReplacement)
			{
				// Grouped replacement.
				adjustment.x = netReplacement.offsetX;
				adjustment.y = netReplacement.offsetY;
				adjustment.z = netReplacement.offsetZ;
				angleAdjustment = netReplacement.angle;
			}
			else if (AllNetworkReplacement.Instance.ActiveReplacement(SelectedNet, lane, propIndex, out _) is BOBNetReplacement allNetReplacement)
			{
				// All- replacement.
				adjustment.x = allNetReplacement.offsetX;
				adjustment.y = allNetReplacement.offsetY;
				adjustment.z = allNetReplacement.offsetZ;
				angleAdjustment = allNetReplacement.angle;
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
				angleAdjustment = angleAdjustment,
				position = thisProp.m_position,
				adjustment = adjustment,
				probability = thisProp.m_probability,
				repeatDistance = thisProp.m_repeatDistance
			};
		}


		/// <summary>
		/// Lane menu index changed event handler.
		/// <param name="control">Calling component (unused)</param>
		/// <param name="index">New index</param>
		/// </summary>
		private void LaneIndexChanged(UIComponent control, int index)
		{
			// Clear the tool's list of lanes to render.
			BOBTool tool = BOBTool.Instance;
			tool.renderLanes.Clear();

			// If the index is greater, there's a lane selection to highlight.
			if (index > 0)
			{
				// Local references.
				NetManager netManager = Singleton<NetManager>.instance;
				NetSegment[] segments = netManager.m_segments.m_buffer;
				NetLane[] lanes = netManager.m_lanes.m_buffer;

				// Lane index is offset for menu index by 1 to allow for the 'All' item at menu index 0.
				int laneIndex = index - 1;

				// Iterate through all segments on map.
				for (int i = 0; i < segments.Length; ++i)
				{
					// Check for valid segments that match the selected NetInfo.
					if ((segments[i].m_flags & NetSegment.Flags.Created) == 0 || segments[i].Info != SelectedNet)
					{
						continue;
					}

					// Iterate through segment lanes until we reach the one we need.
					uint laneID = segments[i].m_lanes;
					for (int j = 0; j < laneIndex; ++j)
					{
						// Safety check.
						if (laneID == 0)
						{
							break;
						}

						// Get ID of next lane in segment.
						laneID = lanes[laneID].m_nextLane;
					}

					// If we ended up with a valid lane ID, add the bezier to the list of lane overlays to be rendered.
					if (laneID != 0)
					{
						tool.renderLanes.Add(lanes[laneID].m_bezier);
					}
				}
			}

			// Regenerate target list and update controls if events aren't suspended.
			if (!ignoreIndexChange)
			{
				TargetList();
				UpdateButtonStates();
			}
		}
	}
}
