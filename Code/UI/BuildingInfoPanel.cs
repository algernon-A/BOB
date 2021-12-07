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

		// Sub-buildings.
		private BuildingInfo[] subBuildings;
		internal string[] SubBuildingNames { get; private set; }

		// Panel components.
		private UIPanel subBuildingPanel;
		private UIFastList subBuildingList;


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
		/// Sets the current sub-building selection to the specified index.
		/// </summary>
		/// <param name="index">Index number of specified sub-building</param>
		internal void SetSubBuilding(int index)
		{
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
				// Call base.
				base.CurrentTargetItem = value;

				// Ensure valid selections before proceeding.
				if (CurrentTargetItem != null && currentBuilding != null)
				{

					// If we've got an individuial building prop replacement, update the offset fields with the replacement values.
					if (CurrentTargetItem.individualPrefab != null)
					{
						// Use IndividualIndex to handle case of switching from individual to grouped props (index will be -1, actual index in relevant list).
						SetSliders(IndividualBuildingReplacement.Instance.EligibileReplacement(currentBuilding, CurrentTargetItem.originalPrefab, IndividualIndex));

						// All done here.
						return;
					}
					// Ditto for any building replacement.
					else if (CurrentTargetItem.replacementPrefab != null)
					{
						SetSliders(BuildingReplacement.Instance.EligibileReplacement(currentBuilding, CurrentTargetItem.originalPrefab, -1));

						// All done here.
						return;
					}
					// Ditto for any all-building replacement.
					else if (CurrentTargetItem.allPrefab != null)
					{
						SetSliders(AllBuildingReplacement.Instance.EligibileReplacement(null, CurrentTargetItem.originalPrefab, -1));

						// All done here.
						return;
					}
				}

				// If we got here, there's no valid current selection; set all offset fields to defaults.
				angleSlider.TrueValue = 0f;
				xSlider.TrueValue = 0;
				ySlider.TrueValue = 0;
				zSlider.TrueValue = 0;
				probabilitySlider.TrueValue = value != null ? value.originalProb : 0;
			}
		}


		/// <summary>
		/// Constructor.
		/// </summary>
		internal BOBBuildingInfoPanel()
		{
			try
			{
				// Populate loaded list.
				LoadedList();
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
			currentBuilding = SelectedBuilding;

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

			// Apply Harmony rendering patches.
			RenderOverlays.CurrentBuilding = selectedPrefab as BuildingInfo;
			Patcher.PatchBuildingOverlays(true);
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
				// Make sure we have valid a target and replacement.
				if (CurrentTargetItem != null && ReplacementPrefab != null)
				{
					switch (CurrentMode)
					{
						case ReplacementModes.Individual:
							// Individual replacement.
							Logging.Message("applying individual building replacement for building ", currentBuilding.name ?? "null", ", target ", CurrentTargetItem.originalPrefab.name ?? "null", ", and replacement ", ReplacementPrefab.name ?? "null");
							IndividualBuildingReplacement.Instance.Replace(currentBuilding, CurrentTargetItem.originalPrefab, ReplacementPrefab, CurrentTargetItem.index, angleSlider.TrueValue, xSlider.TrueValue, ySlider.TrueValue, zSlider.TrueValue, (int)probabilitySlider.TrueValue);

							// Update current target.
							CurrentTargetItem.individualPrefab = ReplacementPrefab;
							CurrentTargetItem.individualProb = (int)probabilitySlider.TrueValue;
							break;

						case ReplacementModes.Grouped:
							// Grouped replacement.
							Logging.Message("applying grouped building replacement for building ", currentBuilding.name ?? "null", ", target ", CurrentTargetItem.originalPrefab.name ?? "null", ", and replacement ", ReplacementPrefab.name ?? "null");
							BuildingReplacement.Instance.Replace(currentBuilding, CurrentTargetItem.originalPrefab, ReplacementPrefab, -1, angleSlider.TrueValue, xSlider.TrueValue, ySlider.TrueValue, zSlider.TrueValue, (int)probabilitySlider.TrueValue);

							// Update current target.
							CurrentTargetItem.replacementPrefab = ReplacementPrefab;
							CurrentTargetItem.replacementProb = (int)probabilitySlider.TrueValue;
							break;

						case ReplacementModes.All:
							// All- replacement.
							Logging.Message("applying all-building replacement");
							AllBuildingReplacement.Instance.Replace(null, CurrentTargetItem.originalPrefab ?? CurrentTargetItem.replacementPrefab, ReplacementPrefab, -1, angleSlider.TrueValue, xSlider.TrueValue, ySlider.TrueValue, zSlider.TrueValue, (int)probabilitySlider.TrueValue);

							// Update current target.
							CurrentTargetItem.allPrefab = ReplacementPrefab;
							CurrentTargetItem.allProb = (int)probabilitySlider.TrueValue;
							break;

						default:
							Logging.Error("invalid replacement mode at BuildingInfoPanel.Apply");
							return;
					}

					// Perform post-replacment updates.
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

					// Perform post-replacment updates.
					FinishUpdate();
				}
				else if (CurrentTargetItem.replacementPrefab != null)
				{
					// Grouped reversion.
					BuildingReplacement.Instance.Revert(currentBuilding, CurrentTargetItem.originalPrefab, -1, true);

					// Clear current target replacement prefab.
					CurrentTargetItem.replacementPrefab = null;

					// Perform post-replacment updates.
					FinishUpdate();
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

						// Perform post-replacment updates.
						FinishUpdate();
					}
				}
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

			// All-building replacement and original probability (if any).
			BOBBuildingReplacement allBuildingReplacement = AllBuildingReplacement.Instance.ActiveReplacement(currentBuilding, propIndex);
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
			BOBBuildingReplacement buildingReplacement = BuildingReplacement.Instance.ActiveReplacement(currentBuilding, propIndex);
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
			BOBBuildingReplacement individualReplacement = IndividualBuildingReplacement.Instance.ActiveReplacement(currentBuilding, propIndex);
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
				return;
			}

			// Iterate through each prop in building.
			for (int propIndex = 0; propIndex < currentBuilding.m_props.Length; ++propIndex)
			{
				// Create new list item.
				TargetListItem targetListItem = new TargetListItem();

				// Try to get relevant prefab (prop/tree), using finalProp.
				PrefabInfo finalInfo = IsTree ? (PrefabInfo)currentBuilding.m_props[propIndex]?.m_finalTree : (PrefabInfo)currentBuilding.m_props[propIndex]?.m_finalProp;

				// Check to see if we were succesful - if not (e.g. we only want trees and this is a prop), continue on to next building prop.
				if (finalInfo?.name == null)
				{
					continue;
				}

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

				// Get original (pre-replacement) tree/prop prefab and current probability (as default original probability).
				targetListItem.originalPrefab = finalInfo;
				targetListItem.originalProb = currentBuilding.m_props[propIndex].m_probability;
				targetListItem.originalAngle = (currentBuilding.m_props[propIndex].m_radAngle * 180f) / Mathf.PI;

				// All-building replacement and original probability (if any).
				BOBBuildingReplacement allBuildingReplacement = AllBuildingReplacement.Instance.ActiveReplacement(currentBuilding, propIndex);
				if (allBuildingReplacement != null)
				{
					targetListItem.allPrefab = allBuildingReplacement.replacementInfo;
					targetListItem.allProb = allBuildingReplacement.probability;

					// Update original prop reference.
					targetListItem.originalPrefab = allBuildingReplacement.targetInfo;
				}

				// Building replacement and original probability (if any).
				BOBBuildingReplacement buildingReplacement = BuildingReplacement.Instance.ActiveReplacement(currentBuilding, propIndex);
				if (buildingReplacement != null)
				{
					targetListItem.replacementPrefab = buildingReplacement.replacementInfo;
					targetListItem.replacementProb = buildingReplacement.probability;

					// Update original prop reference.
					targetListItem.originalPrefab = buildingReplacement.targetInfo;
				}

				// Individual replacement and original probability (if any).
				BOBBuildingReplacement individualReplacement = IndividualBuildingReplacement.Instance.ActiveReplacement(currentBuilding, propIndex);
				if (individualReplacement != null)
				{
					targetListItem.individualPrefab = individualReplacement.replacementInfo;
					targetListItem.individualProb = individualReplacement.probability;

					// Update original prop reference.
					targetListItem.originalPrefab = individualReplacement.targetInfo;
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
						if (item.originalPrefab == targetListItem.originalPrefab && item.individualPrefab == targetListItem.individualPrefab && item.replacementPrefab == targetListItem.replacementPrefab && targetListItem.allPrefab == item.allPrefab)
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
	}
}
