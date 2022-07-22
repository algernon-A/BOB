using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework.UI;


namespace BOB
{
	/// <summary>
	/// Prop row fastlist item for sub-buildings.
	/// </summary>
	public class UISubBuildingRow : UIPropRow
	{
		// Sub-building reference index.
		private int subBuildingIndex;


		/// <summary>
		/// Called when this item is selected.
		/// </summary>
		public override void UpdateSelection()
		{
			// Update currently selected target prefab.
			if (InfoPanelManager.Panel is BOBBuildingInfoPanel buildingPanel)
			{
				buildingPanel.SetSubBuilding(subBuildingIndex);
			}
		}


		/// <summary>
		/// Called when list item is displayed.
		/// </summary>
		public override void Display(object data, bool isRowOdd)
		{
			// Perform initial setup for new rows.
			if (nameLabel == null)
			{
				isVisible = true;
				canFocus = true;
				isInteractive = true;
				width = parent.width;
				height = RowHeight;

				// Add object name label.
				nameLabel = AddUIComponent<UILabel>();
				nameLabel.width = this.width - 10f;
				nameLabel.textScale = TextScale;
			}

			// Get sub-building index number.
			subBuildingIndex = (int)data;

			// Set display text.
			nameLabel.text = ((InfoPanelManager.Panel as BOBBuildingInfoPanel).SubBuildingNames[subBuildingIndex] ?? "");

			// Set label position
			nameLabel.relativePosition = new Vector2(5f, PaddingY);

			// Set initial background as deselected state.
			Deselect(isRowOdd);
		}
	}


	/// <summary>
	/// BOB building tree/prop replacement panel.
	/// </summary>
	internal class BOBBuildingInfoPanel : BOBInfoPanel
	{
		// Current selection reference.
		private BuildingInfo currentBuilding;

		// Original selection values.
		private List<BuildingPropReference> originalValues = new List<BuildingPropReference>();

		// Sub-buildings.
		private BuildingInfo[] subBuildings;
		internal string[] SubBuildingNames { get; private set; }

		// Panel components.
		private UIPanel subBuildingPanel;
		private UIFastList subBuildingList;
		private UICheckBox customHeightCheck;


		/// <summary>
		/// Mode icon atlas names for prop modes.
		/// </summary>
		protected override string[] PropModeAtlas => new string[(int)ReplacementModes.NumModes]
		{
			"BOB-ThisPropSmall",
			"BOB-SamePropSmall",
			"BOB-BuildingsSmall"
		};


		/// <summary>
		/// Mode icon atlas names for tree modes.
		/// </summary>
		protected override string[] TreeModeAtlas => new string[(int)ReplacementModes.NumModes]
		{
			"BOB-ThisTreeSmall",
			"BOB-SameTreeSmall",
			"BOB-BuildingsSmall"
		};


		/// <summary>
		/// Mode icon tootlip keys for prop modes.
		/// </summary>
		protected override string[] PropModeTipKeys => new string[(int)ReplacementModes.NumModes]
		{
			"BOB_PNL_M_PIB",
			"BOB_PNL_M_PGB",
			"BOB_PNL_M_PAB"
		};


		/// <summary>
		/// Mode icon tootlip keys for tree modes.
		/// </summary>
		protected override string[] TreeModeTipKeys => new string[(int)ReplacementModes.NumModes]
		{
			"BOB_PNL_M_TIB",
			"BOB_PNL_M_TGB",
			"BOB_PNL_M_TAB"
		};


		/// <summary>
		/// Currently selected building.
		/// </summary>
		protected override BuildingInfo SelectedBuilding => currentBuilding;


		/// <summary>
		/// Sets the current sub-building selection to the specified index.
		/// </summary>
		/// <param name="index">Index number of specified sub-building</param>
		internal void SetSubBuilding(int index)
		{
			// Revert any preview.
			RevertPreview();

			// Set current building.
			currentBuilding = subBuildings[index];

			// Reset current items.
			CurrentTargetItem = null;
			ReplacementPrefab = null;

			// Reset loaded lists.
			LoadedList();
			TargetList();

			// Update overlay.
			RenderOverlays.CurrentBuilding = currentBuilding;
		}


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

				// Ensure valid selections before proceeding.
				if (CurrentTargetItem != null && currentBuilding != null)
				{
					// Set custom height checkbox.
					customHeightCheck.isChecked = currentBuilding.m_props[IndividualIndex].m_fixedHeight;

					// Is this an added prop?
					if (CurrentTargetItem.isAdded)
					{
						Logging.Message("setting sliders for added prop at index ", IndividualIndex);

						// Yes - set sliders directly.
						// Disable events.
						ignoreSliderValueChange = true;

						// Set slider values.
						BuildingInfo.Prop buildingProp = currentBuilding.m_props[IndividualIndex];
						angleSlider.TrueValue = buildingProp.m_radAngle * Mathf.Rad2Deg;
						xSlider.TrueValue = buildingProp.m_position.x;
						ySlider.TrueValue = buildingProp.m_position.y;
						zSlider.TrueValue = buildingProp.m_position.z;
						probabilitySlider.TrueValue = buildingProp.m_probability;

						// Re-enable events.
						ignoreSliderValueChange = false;

						// All done here.
						return;
					}
					else
					{
						// If we've got an individuial building prop replacement, update the offset fields with the replacement values.
						if (CurrentTargetItem.individualPrefab != null)
						{
							// Use IndividualIndex to handle case of switching from individual to grouped props (index will be -1, actual index in relevant list).
							SetSliders(IndividualBuildingReplacement.Instance.ActiveReplacement(currentBuilding, CurrentTargetItem.originalPrefab, IndividualIndex));

							// All done here.
							return;
						}
						// Ditto for any building replacement.
						else if (CurrentTargetItem.replacementPrefab != null)
						{
							SetSliders(BuildingReplacement.Instance.ActiveReplacement(currentBuilding, CurrentTargetItem.originalPrefab, -1));

							// All done here.
							return;
						}
						// Ditto for any all-building replacement.
						else if (CurrentTargetItem.allPrefab != null)
						{
							SetSliders(AllBuildingReplacement.Instance.ActiveReplacement(null, CurrentTargetItem.originalPrefab, -1));

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
				base.CurrentMode = value;

				// Show/hide add new prop button based on mode.
				bool eligibleMode = CurrentMode == ReplacementModes.Individual | CurrentMode == ReplacementModes.Grouped;
				addButton.isVisible = eligibleMode;
				removeButton.isVisible = eligibleMode;
			}
		}


		/// <summary>
		/// Constructor.
		/// </summary>
		internal BOBBuildingInfoPanel()
		{
			try
			{
				// Fixed height checkbox.
				customHeightCheck = UIControls.LabelledCheckBox(heightPanel, Margin, FixedHeightY, Translations.Translate("BOB_PNL_CUH"), tooltip: Translations.Translate("BOB_PNL_CUH_TIP"));
				customHeightCheck.eventCheckChanged += CustomHeightChange;

				// Adjust y-slider position and panel height.
				ySlider.relativePosition += new Vector3(0f, 20f);
				ySlider.ValueField.relativePosition += new Vector3(0f, 20f);
				heightPanel.height = HeightPanelFullHeight;

				// Populate loaded list.
				LoadedList();

				// Update button states.
				UpdateButtonStates();
			}
			catch (Exception e)
			{
				// Log and report any exception.
				Logging.LogException(e, "exception creating building panel");
			}
		}


		/// <summary>
		/// Sets the target prefab.
		/// </summary>
		/// <param name="targetPrefabInfo">Target prefab to set</param>
		internal override void SetTarget(PrefabInfo targetPrefabInfo)
		{
			// Don't do anything if invalid target, or target hasn't changed.
			if (!(targetPrefabInfo is BuildingInfo) || selectedPrefab == targetPrefabInfo)
			{
				return;
			}

			// Base setup.
			base.SetTarget(targetPrefabInfo);

			// Set target reference.
			currentBuilding = targetPrefabInfo as BuildingInfo;

			// Does this building have sub-buildings?
			if (currentBuilding.m_subBuildings != null && currentBuilding.m_subBuildings.Length > 0)
			{
				// Yes - create lists of sub-buildings (names and infos).
				int numSubs = currentBuilding.m_subBuildings.Length;
				int numChoices = numSubs + 1;
				SubBuildingNames = new string[numChoices];
				subBuildings = new BuildingInfo[numChoices];
				SubBuildingNames[0] = PrefabLists.GetDisplayName(currentBuilding);
				subBuildings[0] = currentBuilding;

				object[] subBuildingIndexes = new object[numChoices];
				subBuildingIndexes[0] = 0;

				for (int i = 0; i < numSubs; ++i)
				{
					SubBuildingNames[i + 1] = PrefabLists.GetDisplayName(currentBuilding.m_subBuildings[i].m_buildingInfo);
					subBuildings[i + 1] = currentBuilding.m_subBuildings[i].m_buildingInfo;
					subBuildingIndexes[i + 1] = i + 1;
				}

				// Add sub-building menu, if it doesn't already exist.
				if (subBuildingPanel == null)
				{
					subBuildingPanel = this.AddUIComponent<UIPanel>();

					// Basic behaviour.
					subBuildingPanel.autoLayout = false;
					subBuildingPanel.canFocus = true;
					subBuildingPanel.isInteractive = true;

					// Appearance.
					subBuildingPanel.backgroundSprite = "MenuPanel2";
					subBuildingPanel.opacity = PanelOpacity;

					// Size and position.
					subBuildingPanel.size = new Vector2(200f, PanelHeight - TitleHeight);
					subBuildingPanel.relativePosition = new Vector2(-205f, TitleHeight);

					// Heading.
					UILabel subTitleLabel = UIControls.AddLabel(subBuildingPanel, 5f, 5f, Translations.Translate("BOB_PNL_SUB"), 190f);
					subTitleLabel.textAlignment = UIHorizontalAlignment.Center;
					subTitleLabel.relativePosition = new Vector2(5f, (TitleHeight - subTitleLabel.height) / 2f);

					// List panel.
					UIPanel subBuildingListPanel = subBuildingPanel.AddUIComponent<UIPanel>();
					subBuildingListPanel.relativePosition = new Vector2(Margin, TitleHeight);
					subBuildingListPanel.width = subBuildingPanel.width - (Margin * 2f);
					subBuildingListPanel.height = subBuildingPanel.height - TitleHeight - (Margin * 2f);
					subBuildingList = UIFastList.Create<UISubBuildingRow>(subBuildingListPanel);
					ListSetup(subBuildingList);

					// Create return fastlist from our filtered list.
					subBuildingList.rowsData = new FastList<object>
					{
						m_buffer = subBuildingIndexes,
						m_size = subBuildingIndexes.Length
					};
				}
				else
				{
					// If the sub-building panel has already been created. just make sure it's visible.
					subBuildingPanel.Show();
				}
			}
			else
			{
				// Otherwise, hide the sub-building panel (if it exists).
				subBuildingPanel?.Hide();
			}

			// Populate target list and select target item.
			TargetList();
			
			// Record original stats for preview.
			RecordOriginal();

			// Apply Harmony rendering patches.
			RenderOverlays.CurrentBuilding = currentBuilding;
			Patcher.PatchBuildingOverlays(true);
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

			// Update preview for each recorded reference.
			foreach (BuildingPropReference reference in originalValues)
			{
				PreviewChange(reference);
			}

			// Update renders.
			BuildingData.Update();

			// Update highlighting target.
			RenderOverlays.CurrentProp = ReplacementPrefab as PropInfo;
			RenderOverlays.CurrentTree = ReplacementPrefab as TreeInfo;

			// Update apply button icon to indicate change.
			UnappliedChanges = true;
		}


		/// <summary>
		/// Reverts any previewed changes back to original prop/tree state.
		/// </summary>
		protected override void RevertPreview()
		{
			// Iterate through each original value.
			foreach (BuildingPropReference reference in originalValues)
			{
				// Sanity check index.
				int propIndex = reference.propIndex;
				if (propIndex >= reference.buildingInfo.m_props.Length)
				{
					continue;
				}

				// Restore original values.
				if (reference.buildingInfo.m_props[propIndex] is BuildingInfo.Prop thisProp)
				{
					thisProp.m_prop = reference.originalProp;
					thisProp.m_finalProp = reference.originalFinalProp;
					thisProp.m_tree = reference.originalTree;
					thisProp.m_finalTree = reference.originalFinalTree;
					thisProp.m_radAngle = reference.radAngle;
					thisProp.m_position = reference.position;
					thisProp.m_probability = reference.probability;
					thisProp.m_fixedHeight = reference.fixedHeight;

					// Add building to dirty list.
					BuildingData.DirtyList.Add(reference.buildingInfo);
				}
			}

			// Update prefabs.
			BuildingData.Update();

			// Reset apply button icon.
			UnappliedChanges = false;
		}


		/// <summary>
		/// Apply button event handler.
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
		/// </summary>
		protected override void Apply(UIComponent control, UIMouseEventParameter mouseEvent)
		{
			// First, undo any preview.
			RevertPreview();

			try
			{
				// Make sure we have valid a target and replacement.
				if (CurrentTargetItem != null && ReplacementPrefab != null)
				{
					// Check for added prop - instead of replacing, we update the original added prop reference.
					if (CurrentTargetItem.isAdded)
					{
						AddedBuildingProps.Instance.Update(currentBuilding, CurrentTargetItem.originalPrefab, ReplacementPrefab, CurrentTargetItem.index, angleSlider.TrueValue, xSlider.TrueValue, ySlider.TrueValue, zSlider.TrueValue, (int)probabilitySlider.TrueValue, customHeightCheck.isChecked);

						// Update current target.
						CurrentTargetItem.originalPrefab = ReplacementPrefab;
						CurrentTargetItem.originalProb = (int)probabilitySlider.TrueValue;
					}
					else
					{
						// Not an added prop.
						switch (CurrentMode)
						{
							case ReplacementModes.Individual:
								// Individual replacement.
								IndividualBuildingReplacement.Instance.Replace(currentBuilding, CurrentTargetItem.originalPrefab, ReplacementPrefab, CurrentTargetItem.index, angleSlider.TrueValue, xSlider.TrueValue, ySlider.TrueValue, zSlider.TrueValue, (int)probabilitySlider.TrueValue, customHeightCheck.isChecked);

								// Update current target.
								CurrentTargetItem.individualPrefab = ReplacementPrefab;
								CurrentTargetItem.individualProb = (int)probabilitySlider.TrueValue;
								break;

							case ReplacementModes.Grouped:
								// Grouped replacement.
								BuildingReplacement.Instance.Replace(currentBuilding, CurrentTargetItem.originalPrefab, ReplacementPrefab, -1, angleSlider.TrueValue, xSlider.TrueValue, ySlider.TrueValue, zSlider.TrueValue, (int)probabilitySlider.TrueValue, customHeightCheck.isChecked);

								// Update current target.
								CurrentTargetItem.replacementPrefab = ReplacementPrefab;
								CurrentTargetItem.replacementProb = (int)probabilitySlider.TrueValue;
								break;

							case ReplacementModes.All:
								// All- replacement.
								AllBuildingReplacement.Instance.Replace(null, CurrentTargetItem.originalPrefab ?? CurrentTargetItem.replacementPrefab, ReplacementPrefab, -1, angleSlider.TrueValue, xSlider.TrueValue, ySlider.TrueValue, zSlider.TrueValue, (int)probabilitySlider.TrueValue, customHeightCheck.isChecked);

								// Update current target.
								CurrentTargetItem.allPrefab = ReplacementPrefab;
								CurrentTargetItem.allProb = (int)probabilitySlider.TrueValue;
								break;

							default:
								Logging.Error("invalid replacement mode at BuildingInfoPanel.Apply");
								return;
						}
					}

					// Update any dirty building renders.
					BuildingData.Update();

					// Record updated original data.
					RecordOriginal();

					// Update target list and buttons.
					targetList.Refresh();
					UpdateButtonStates();

					// Update highlighting target.
					RenderOverlays.CurrentProp = ReplacementPrefab as PropInfo;
					RenderOverlays.CurrentTree = ReplacementPrefab as TreeInfo;

					// Perform post-replacement processing.
					FinishUpdate();
				}
			}
			catch (Exception e)
			{
				// Log and report any exception.
				Logging.LogException(e, "exception applying building replacement");
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
				if (CurrentTargetItem == null)
				{
					return;
				}

				// Individual building prop reversion?
				if (CurrentTargetItem.individualPrefab != null)
				{
					// Individual reversion - use IndividualIndex to ensure valid value for current context is used.
					IndividualBuildingReplacement.Instance.Revert(currentBuilding, CurrentTargetItem.originalPrefab, IndividualIndex, true);

					// Clear current target replacement prefab.
					CurrentTargetItem.individualPrefab = null;
				}
				else if (CurrentTargetItem.replacementPrefab != null)
				{
					// Grouped reversion.
					BuildingReplacement.Instance.Revert(currentBuilding, CurrentTargetItem.originalPrefab, -1, true);

					// Clear current target replacement prefab.
					CurrentTargetItem.replacementPrefab = null;
				}
				else if (CurrentTargetItem.allPrefab != null)
				{
					// All-building reversion - make sure we've got a currently active replacement before doing anything.
					if (CurrentTargetItem.originalPrefab)
					{
						// Apply all-building reversion.
						AllBuildingReplacement.Instance.Revert(currentBuilding, CurrentTargetItem.originalPrefab, CurrentTargetItem.index, true);

						// Clear current target 'all' prefab.
						CurrentTargetItem.allPrefab = null;
					}
				}

				// Re-record originals (need to do this before updating controls).
				RecordOriginal();

				// Update current item.
				UpdateTargetItem(CurrentTargetItem);

				// Update controls.
				CurrentTargetItem = CurrentTargetItem;

				// Update target list.
				targetList.Refresh();

				// Perform post-replacement processing.
				FinishUpdate();
			}
			catch (Exception e)
			{
				// Log and report any exception.
				Logging.LogException(e, "exception perforiming building reversion");
			}
		}


		/// <summary>
		/// Updates the target item record for changes in replacement status (e.g. after applying or reverting changes).
		/// </summary>
		/// <param name="propListItem">Target item</param>
		protected override void UpdateTargetItem(TargetListItem targetListItem)
		{
			// Determine index to test - if no individual index, just grab first one from list.
			int propIndex = targetListItem.index;
			if (propIndex < 0)
			{
				propIndex = targetListItem.indexes[0];
			}

			// Is this an added prop?
			if (AddedBuildingProps.Instance.IsAdded(currentBuilding, propIndex))
			{
				targetListItem.index = propIndex;
				targetListItem.isAdded = true;
			}
			else
			{
				// All-building replacement and original probability (if any).
				BOBBuildingReplacement allBuildingReplacement = AllBuildingReplacement.Instance.ActiveReplacement(currentBuilding, propIndex, out _);
				if (allBuildingReplacement != null)
				{
					targetListItem.allPrefab = allBuildingReplacement.replacementInfo;
					targetListItem.allProb = allBuildingReplacement.probability;
				}
				else
				{
					// If no active current record, ensure that it's reset to null.
					targetListItem.allPrefab = null;
				}

				// Building replacement and original probability (if any).
				BOBBuildingReplacement buildingReplacement = BuildingReplacement.Instance.ActiveReplacement(currentBuilding, propIndex, out _);
				if (buildingReplacement != null)
				{
					targetListItem.replacementPrefab = buildingReplacement.replacementInfo;
					targetListItem.replacementProb = buildingReplacement.probability;
				}
				else
				{
					// If no active current record, ensure that it's reset to null.
					targetListItem.replacementPrefab = null;
				}

				// Individual replacement and original probability (if any).
				BOBBuildingReplacement individualReplacement = IndividualBuildingReplacement.Instance.ActiveReplacement(currentBuilding, propIndex, out _);
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

			// Check to see if this building contains any props.
			if (currentBuilding.m_props == null || currentBuilding.m_props.Length == 0)
			{
				// No props - show 'no props' label and return an empty list.
				noPropsLabel.Show();
				targetList.rowsData = new FastList<object>();

				// Force clearance of current target item.
				CurrentTargetItem = null;

				return;
			}

			// Iterate through each prop in building.
			for (int propIndex = 0; propIndex < currentBuilding.m_props.Length; ++propIndex)
			{
				// Create new list item.
				TargetListItem targetListItem = new TargetListItem();

				// Try to get relevant prefab (prop/tree), falling back to the other type if null (to allow for tree-prop changes), using finalProp.
				PrefabInfo finalInfo = null;
				if (PropTreeMode == PropTreeModes.Tree)
				{
					finalInfo = (PrefabInfo)currentBuilding.m_props[propIndex]?.m_finalTree ?? currentBuilding.m_props[propIndex]?.m_finalProp;
				}
				else
				{
					finalInfo = (PrefabInfo)currentBuilding.m_props[propIndex]?.m_finalProp ?? currentBuilding.m_props[propIndex]?.m_finalTree;
				}

				// Check to see if we were succesful - if not (e.g. we only want trees and this is a prop), continue on to next building prop.
				if (finalInfo?.name == null)
				{
					continue;
				}

				// Get original (pre-replacement) tree/prop prefab and current probability (as default original probability).
				targetListItem.originalPrefab = finalInfo;
				targetListItem.originalProb = currentBuilding.m_props[propIndex].m_probability;
				targetListItem.originalAngle = currentBuilding.m_props[propIndex].m_radAngle * Mathf.Rad2Deg;

				// Is this an added prop?
				if (AddedBuildingProps.Instance.IsAdded(currentBuilding, propIndex))
				{
					targetListItem.index = propIndex;
					targetListItem.isAdded = true;
				}
				else
				{
					// Grouped or individual?
					if (CurrentMode == ReplacementModes.Individual)
					{
						// Individual - set index to the current building prop indexes.
						targetListItem.index = propIndex;
					}
					else
					{
						// Grouped - set index to -1 and add to our list of indexes.
						targetListItem.index = -1;
						targetListItem.indexes.Add(propIndex);
					}

					// To record original data if a replacement is in effect.
					BuildingPropReference propReference = null;

					// All-building replacement and original probability (if any).
					BOBBuildingReplacement allBuildingReplacement = AllBuildingReplacement.Instance.ActiveReplacement(currentBuilding, propIndex, out propReference);
					if (allBuildingReplacement != null)
					{
						targetListItem.allPrefab = allBuildingReplacement.replacementInfo;
						targetListItem.allProb = allBuildingReplacement.probability;
						targetListItem.originalPrefab = allBuildingReplacement.targetInfo;
					}

					// Building replacement and original probability (if any).
					BOBBuildingReplacement buildingReplacement = BuildingReplacement.Instance.ActiveReplacement(currentBuilding, propIndex, out propReference);
					if (buildingReplacement != null)
					{
						targetListItem.replacementPrefab = buildingReplacement.replacementInfo;
						targetListItem.replacementProb = buildingReplacement.probability;
						targetListItem.originalPrefab = buildingReplacement.targetInfo;
					}

					// Individual replacement and original probability (if any).
					BOBBuildingReplacement individualReplacement = IndividualBuildingReplacement.Instance.ActiveReplacement(currentBuilding, propIndex, out propReference);
					if (individualReplacement != null)
					{
						targetListItem.individualPrefab = individualReplacement.replacementInfo;
						targetListItem.individualProb = individualReplacement.probability;
						targetListItem.originalPrefab = individualReplacement.targetInfo;
					}

					// If we found an active replacement, update original reference values.
					if (propReference != null)
					{
						targetListItem.originalPrefab = propReference.OriginalInfo;
						targetListItem.originalAngle = propReference.radAngle * Mathf.Rad2Deg;
						targetListItem.originalProb = propReference.probability;
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
					foreach (TargetListItem item in itemList)
					{
						// Check to see if we already have this in the list - matching original prefab, individual replacement prefab, building replacement prefab, all-building replacement prefab, and probability.
						if (item.originalPrefab == targetListItem.originalPrefab &&
							item.individualPrefab == targetListItem.individualPrefab &&
							item.replacementPrefab == targetListItem.replacementPrefab &&
							item.allPrefab == targetListItem.allPrefab &&
							item.originalProb == targetListItem.originalProb &&
							item.replacementProb == targetListItem.replacementProb)
						{
							// We've already got an identical grouped instance of this item - add this index and lane to the lists of indexes and lanes under that item and set the flag to indicate that we've done so.
							item.indexes.Add(propIndex);
							matched = true;

							// No point going any further through the list, since we've already found our match.
							break;
						}
					}

					// Did we get a match?
					if (matched)
					{
						// Yes - continue on to next building prop (without adding this item separately to the list).
						continue;
					}
				}

				// Add this item to our list.
				itemList.Add(targetListItem);
			}

			// Create return fastlist from our filtered list, ordering by name.
			targetList.rowsData = new FastList<object>
			{
				m_buffer = targetSearchStatus == (int)OrderBy.NameDescending ? itemList.OrderByDescending(item => item.DisplayName).ToArray() : itemList.OrderBy(item => item.DisplayName).ToArray(),
				m_size = itemList.Count
			};

			// If the list is empty, show the 'no props' label; otherwise, hide it.
			if (targetList.rowsData.m_size == 0)
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

			// Update any dirty building renders.
			BuildingData.Update();
		}


		/// <summary>
		/// Adds a new tree or prop.
		/// </summary>
		protected override void AddNew()
		{
			// Make sure a valid replacement prefab is set.
			if (ReplacementPrefab != null)
			{
				// Revert any preview.
				RevertPreview();

				// Add new prop.
				BOBBuildingReplacement newProp = new BOBBuildingReplacement
				{
					isTree = ReplacementPrefab is TreeInfo,
					Replacement = ReplacementPrefab.name,
					angle = angleSlider.TrueValue,
					offsetX = xSlider.TrueValue,
					offsetY = ySlider.TrueValue,
					offsetZ = zSlider.TrueValue,
					probability = (int)probabilitySlider.TrueValue,
					parentInfo = currentBuilding,
					replacementInfo = ReplacementPrefab,
					customHeight = customHeightCheck.isChecked
				};
				AddedBuildingProps.Instance.AddNew(newProp);

				// Post-action cleanup.
				UpdateAddedProps();
			}
		}


		/// <summary>
		/// Removes an added tree or prop.
		/// </summary>
		protected override void RemoveProp()
		{
			// Safety first - need an individual index that's an added prop.
			if (CurrentTargetItem == null || CurrentTargetItem.index < 0 || !AddedBuildingProps.Instance.IsAdded(currentBuilding, CurrentTargetItem.index))
			{
				return;
			}

			// First, revert any preview (to prevent any clobbering when preview is reverted).
			RevertPreview();

			// Create new props array with one fewer entry, and copy the old props to it.
			// Remove prop reference and update other references as appropriate.
			AddedBuildingProps.Instance.RemoveNew(currentBuilding, CurrentTargetItem.index);

			// Post-action cleanup.
			UpdateAddedProps();
		}


		/// <summary>
		/// Record original prop values before previewing.
		/// </summary>
		protected override void RecordOriginal()
		{
			// Clear existing list.
			originalValues.Clear();

			// Don't do anything if no valid selection.
			if (CurrentTargetItem?.originalPrefab == null || currentBuilding == null)
			{
				return;
			}

			// Check current mode.
			if (CurrentMode == ReplacementModes.All)
			{
				// All-building replacement; iterate through all prefabs and find matching prop references.
				for (uint i = 0; i < PrefabCollection<BuildingInfo>.LoadedCount(); ++i)
				{
					BuildingInfo prefab = PrefabCollection<BuildingInfo>.GetLoaded(i);
					if (prefab?.m_props != null)
					{
						for (int j = 0; j < prefab.m_props.Length; ++j)
						{
							if (prefab.m_props[j].m_prop == CurrentTargetItem.originalPrefab | prefab.m_props[j].m_tree == CurrentTargetItem.originalPrefab)
							{
								originalValues.Add(GetOriginalData(prefab, j));
							}
						}
					}
				}
			}
			else if (CurrentTargetItem.index < 0)
			{
				// Grouped replacement - iterate through each instance and record values.
				for (int i = 0; i < CurrentTargetItem.indexes.Count; ++i)
				{
					originalValues.Add(GetOriginalData(currentBuilding, CurrentTargetItem.indexes[i]));
				}
			}
			else
			{
				// Individual replacement - record original values.
				originalValues.Add(GetOriginalData(currentBuilding, CurrentTargetItem.index));
			}
		}


		/// <summary>
		/// Custom height checkbox event handler.
		/// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="isChecked">New checked state</param>
		private void CustomHeightChange(UIComponent control, bool isChecked)
		{
			// Show/hide Y position slider based on value.
			ySlider.isVisible = isChecked;
			ySlider.ValueField.isVisible = isChecked;
		}


		/// <summary>
		/// Called after any added prop manipulations (addition or removal) to perform cleanup.
		/// </summary>
		private void UpdateAddedProps()
		{
			// Update building prop references.
			currentBuilding.CheckReferences();

			// Clear current selection.
			CurrentTargetItem = null;

			// Perform regular post-processing.
			FinishUpdate();
			TargetList();

			// Rebuild recorded originals list.
			RecordOriginal();
		}


		/// <summary>
		/// Previews the change for the given prop index.
		/// </summary>
		/// <param name="propReference">Prop reference</param>
		private void PreviewChange(BuildingPropReference propReference)
		{
			// Original position and angle.
			Vector3 basePosition = Vector3.zero;
			float baseAngle = 0f;

			if (!CurrentTargetItem.isAdded)
			{
				basePosition = propReference.position - propReference.adjustment;
				baseAngle = propReference.radAngle - propReference.radAngleAdjustment;
			}

			// Null check.
			BuildingInfo.Prop thisProp = propReference.buildingInfo?.m_props?[propReference.propIndex];
			if (thisProp == null)
			{
				return;
			}

			// Preview new position, probability, rotation, and fixed height setting.
			thisProp.m_position = basePosition + new Vector3(xSlider.TrueValue, ySlider.TrueValue, zSlider.TrueValue); ;
			thisProp.m_probability = (int)probabilitySlider.TrueValue;
			thisProp.m_fixedHeight = customHeightCheck.isChecked;
			thisProp.m_radAngle = baseAngle + (angleSlider.TrueValue * Mathf.Deg2Rad);

			// If a replacement prefab has been selected, then update it too.
			if (ReplacementPrefab != null)
			{
				thisProp.m_prop = ReplacementPrefab as PropInfo;
				thisProp.m_tree = ReplacementPrefab as TreeInfo;
				thisProp.m_finalProp = ReplacementPrefab as PropInfo;
				thisProp.m_finalTree = ReplacementPrefab as TreeInfo;
			}

			// Add building to dirty list.
			BuildingData.DirtyList.Add(propReference.buildingInfo);
		}


		/// <summary>
		/// Gets original (current) prop data.
		/// </summary>
		/// <param name="buildingInfo">Building prefab</param>
		/// <param name="propIndex">Prop index</param>
		/// <returns>Original prop data</returns>
		private BuildingPropReference GetOriginalData(BuildingInfo buildingInfo, int propIndex)
		{
			// Ensure that the index is valid before proceeding.
			if (buildingInfo?.m_props == null || buildingInfo.m_props.Length <= propIndex)
			{
				Logging.Error("invalid prop index reference of ", propIndex, " for selected building ", buildingInfo?.name ?? "null");
				return null;
			}

			// Local reference.
			BuildingInfo.Prop thisProp = buildingInfo.m_props[propIndex];

			// Get any position and angle adjustments from active replacements, checking in priority order.
			Vector3 adjustment = Vector3.zero;
			float angleAdjustment = 0f;
			if (IndividualBuildingReplacement.Instance.ActiveReplacement(currentBuilding, propIndex, out _) is BOBBuildingReplacement individualReplacement)
			{
				// Individual replacement.
				adjustment.x = individualReplacement.offsetX;
				adjustment.y = individualReplacement.offsetY;
				adjustment.z = individualReplacement.offsetZ;
				angleAdjustment = individualReplacement.angle;
			}
			else if (BuildingReplacement.Instance.ActiveReplacement(currentBuilding, propIndex, out _) is BOBBuildingReplacement buildingReplacement)
			{
				// Grouped replacement.
				adjustment.x = buildingReplacement.offsetX;
				adjustment.y = buildingReplacement.offsetY;
				adjustment.z = buildingReplacement.offsetZ;
				angleAdjustment = buildingReplacement.angle;
			}
			else if (AllBuildingReplacement.Instance.ActiveReplacement(currentBuilding, propIndex, out _) is BOBBuildingReplacement allBuildingReplacement)
			{
				// All- replacement.
				adjustment.x = allBuildingReplacement.offsetX;
				adjustment.y = allBuildingReplacement.offsetY;
				adjustment.z = allBuildingReplacement.offsetZ;
				angleAdjustment = allBuildingReplacement.angle;
			}

			// Return original data.
			return new BuildingPropReference
			{
				buildingInfo = buildingInfo,
				propIndex = propIndex,
				originalProp = thisProp.m_prop,
				originalTree = thisProp.m_tree,
				originalFinalProp = thisProp.m_finalProp,
				originalFinalTree = thisProp.m_finalTree,
				radAngle = thisProp.m_radAngle,
				radAngleAdjustment = angleAdjustment * Mathf.Deg2Rad,
				position = thisProp.m_position,
				adjustment = adjustment,
				probability = thisProp.m_probability,
				fixedHeight = thisProp.m_fixedHeight
			};
		}
	}
}
