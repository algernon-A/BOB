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


		// Button tooltips.
		protected override string ReplaceTooltipKey => "BOB_PNL_RTB";
		protected override string ReplaceAllTooltipKey => "BOB_PNL_RAB";


		// Replace button atlases.
		protected override UITextureAtlas ReplaceAtlas => TextureUtils.LoadSpriteAtlas("bob_single_building");
		protected override UITextureAtlas ReplaceAllAtlas => TextureUtils.LoadSpriteAtlas("bob_buildings");


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
		internal override PropListItem CurrentTargetItem
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
						BOBBuildingReplacement thisReplacement = IndividualBuildingReplacement.instance.Replacement(currentBuilding, IndividualIndex);

						angleSlider.TrueValue = thisReplacement.angle;
						xSlider.TrueValue = thisReplacement.offsetX;
						ySlider.TrueValue = thisReplacement.offsetY;
						zSlider.TrueValue = thisReplacement.offsetZ;
						probabilitySlider.TrueValue = thisReplacement.probability;

						// All done here.
						return;
					}
					// Ditto for any building replacement.
					else if (CurrentTargetItem.replacementPrefab != null)
					{
						BOBBuildingReplacement thisReplacement = BuildingReplacement.instance.Replacement(currentBuilding, CurrentTargetItem.originalPrefab);

						angleSlider.TrueValue = thisReplacement.angle;
						xSlider.TrueValue = thisReplacement.offsetX;
						ySlider.TrueValue = thisReplacement.offsetY;
						zSlider.TrueValue = thisReplacement.offsetZ;
						probabilitySlider.TrueValue = thisReplacement.probability;

						// All done here.
						return;
					}
					// Ditto for any all-building replacement.
					else if (CurrentTargetItem.allPrefab != null)
					{
						BOBBuildingReplacement thisReplacement = AllBuildingReplacement.instance.Replacement(CurrentTargetItem.originalPrefab);

						angleSlider.TrueValue = thisReplacement.angle;
						xSlider.TrueValue = thisReplacement.offsetX;
						ySlider.TrueValue = thisReplacement.offsetY;
						zSlider.TrueValue = thisReplacement.offsetZ;
						probabilitySlider.TrueValue = thisReplacement.probability;

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
			// Populate loaded list.
			LoadedList();
		}


		/// <summary>
		/// Sets the target prefab.
		/// </summary>
		/// <param name="targetPrefabInfo">Target prefab to set</param>
		internal override void SetTarget(PrefabInfo targetPrefabInfo)
		{
			// Don't do anything if target hasn't changed.
			if (currentBuilding == targetPrefabInfo)
            {
				return;
            }

			// Set target reference.
			currentBuilding = targetPrefabInfo as BuildingInfo;

			// Base setup.
			base.SetTarget(targetPrefabInfo);

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

			// Set grouped checkbox initial state according to preferences.
			switch (ModSettings.indDefault)
			{
				case 0:
					// Most recent state.
					indCheck.isChecked = ModSettings.lastInd;
					break;
				case 1:
					// Grouping off by default.
					indCheck.isChecked = false;
					break;
				case 2:
					// Grouping on by default.
					indCheck.isChecked = true;
					break;
			}

			// Populate target list and select target item.
			TargetList();

			// Apply Harmony rendering patches.
			RenderOverlays.CurrentBuilding = selectedPrefab as BuildingInfo;
			Patcher.PatchBuildingOverlays(true);
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
					BuildingReplacement.instance.Replace(currentBuilding, CurrentTargetItem.originalPrefab, ReplacementPrefab, -1, angleSlider.TrueValue, xSlider.TrueValue, ySlider.TrueValue, zSlider.TrueValue, (int)probabilitySlider.TrueValue);

					// Update current target.
					CurrentTargetItem.replacementPrefab = ReplacementPrefab;
					CurrentTargetItem.replacementProb = (int)probabilitySlider.TrueValue;
				}
				else
				{
					// Individual replacement.
					IndividualBuildingReplacement.instance.Replace(currentBuilding, CurrentTargetItem.originalPrefab, ReplacementPrefab, CurrentTargetItem.index, angleSlider.TrueValue, xSlider.TrueValue, ySlider.TrueValue, zSlider.TrueValue, (int)probabilitySlider.TrueValue);

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
			// Make sure we've got a valid selection.
			if (CurrentTargetItem == null)
			{
				return;
			}

			// Individual building prop reversion?
			if (CurrentTargetItem.individualPrefab != null)
			{
				// Individual reversion - use IndividualIndex to ensure valid value for current context is used.
				IndividualBuildingReplacement.instance.Revert(currentBuilding, IndividualIndex, true);

				// Clear current target replacement prefab.
				CurrentTargetItem.individualPrefab = null;

				// Perform post-replacment updates.
				FinishUpdate();
			}
			else if (CurrentTargetItem.replacementPrefab != null)
			{
				// Grouped reversion.
				BuildingReplacement.instance.Revert(currentBuilding, CurrentTargetItem.originalPrefab, true);

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
					AllBuildingReplacement.instance.Revert(CurrentTargetItem.originalPrefab.name, true);

					// Clear current target 'all' prefab.
					CurrentTargetItem.allPrefab = null;

					// Perform post-replacment updates.
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
			// Saftey net - don't do anything if individual check is selected.
			if (indCheck.isChecked)
			{
				return;
			}

			// Apply replacement.
			AllBuildingReplacement.instance.Replace(null, CurrentTargetItem.originalPrefab ?? CurrentTargetItem.replacementPrefab, ReplacementPrefab, -1, angleSlider.TrueValue, xSlider.TrueValue, ySlider.TrueValue, zSlider.TrueValue, (int)probabilitySlider.TrueValue);

			// Update current target.
			CurrentTargetItem.allPrefab = ReplacementPrefab;
			CurrentTargetItem.allProb = (int)probabilitySlider.TrueValue;

			// Perform post-replacment updates.
			FinishUpdate();
		}


		/// <summary>
		/// Updates the target item record for changes in replacement status (e.g. after applying or reverting changes).
		/// </summary>
		/// <param name="propListItem">Target item</param>
		protected override void UpdateTargetItem(PropListItem propListItem)
		{
			// Determine index to test - if no individual index, just grab first one from list.
			int propIndex = propListItem.index;
			if (propIndex < 0)
            {
				propIndex = propListItem.indexes[0];
            }

			// All-building replacement and original probability (if any).
			BOBBuildingReplacement allBuildingReplacement = AllBuildingReplacement.instance.ActiveReplacement(currentBuilding, propIndex);
			if (allBuildingReplacement != null)
			{
				propListItem.allPrefab = allBuildingReplacement.replacementInfo;
				propListItem.allProb = allBuildingReplacement.probability;
			}
			else
			{
				// If no active current record, ensure that it's reset to null.
				propListItem.allPrefab = null;
			}

			// Building replacement and original probability (if any).
			BOBBuildingReplacement buildingReplacement = BuildingReplacement.instance.ActiveReplacement(currentBuilding, propIndex);
			if (buildingReplacement != null)
			{
				propListItem.replacementPrefab = buildingReplacement.replacementInfo;
				propListItem.replacementProb = buildingReplacement.probability;
			}
			else
			{
				// If no active current record, ensure that it's reset to null.
				propListItem.replacementPrefab = null;
			}

			// Individual replacement and original probability (if any).
			BOBBuildingReplacement individualReplacement = IndividualBuildingReplacement.instance.ActiveReplacement(currentBuilding, propIndex);
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


		/// <summary>
		/// Populates the target fastlist with a list of target-specific trees or props.
		/// </summary>
		protected override void TargetList()
		{
			// Clear current selection.
			targetList.selectedIndex = -1;

			// List of prefabs that have passed filtering.
			List<PropListItem> propList = new List<PropListItem>();

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
				PropListItem propListItem = new PropListItem();

				// Try to get relevant prefab (prop/tree), using finalProp.
				PrefabInfo finalInfo = IsTree ? (PrefabInfo)currentBuilding.m_props[propIndex]?.m_finalTree : (PrefabInfo)currentBuilding.m_props[propIndex]?.m_finalProp;

				// Check to see if we were succesful - if not (e.g. we only want trees and this is a prop), continue on to next building prop.
				if (finalInfo?.name == null)
				{
					continue;
				}

				// Grouped or individual?
				if (indCheck.isChecked)
				{
					// Individual - set index to the current building prop indexes.
					propListItem.index = propIndex;
				}
				else
				{
					// Grouped - set index to -1 and add to our list of indexes.
					propListItem.index = -1;
					propListItem.indexes.Add(propIndex);
				}

				// Get original (pre-replacement) tree/prop prefab and current probability (as default original probability).
				propListItem.originalPrefab = BuildingReplacement.instance.ActiveReplacement(currentBuilding, propIndex)?.targetInfo ?? AllBuildingReplacement.instance.ActiveReplacement(currentBuilding, propIndex)?.targetInfo ?? finalInfo;
				propListItem.originalProb = currentBuilding.m_props[propIndex].m_probability;
				propListItem.originalAngle = (currentBuilding.m_props[propIndex].m_radAngle * 180f) / Mathf.PI;

				// All-building replacement and original probability (if any).
				BOBBuildingReplacement allBuildingReplacement = AllBuildingReplacement.instance.ActiveReplacement(currentBuilding, propIndex);
				if (allBuildingReplacement != null)
				{
					propListItem.allPrefab = allBuildingReplacement.replacementInfo;
					propListItem.allProb = allBuildingReplacement.probability;
				}

				// Building replacement and original probability (if any).
				BOBBuildingReplacement buildingReplacement = BuildingReplacement.instance.ActiveReplacement(currentBuilding, propIndex);
				if (buildingReplacement != null)
				{
					propListItem.replacementPrefab = buildingReplacement.replacementInfo;
					propListItem.replacementProb = buildingReplacement.probability;
				}

				// Individual replacement and original probability (if any).
				BOBBuildingReplacement individualReplacement = IndividualBuildingReplacement.instance.ActiveReplacement(currentBuilding, propIndex);
				if (individualReplacement != null)
				{
					propListItem.individualPrefab = individualReplacement.replacementInfo;
					propListItem.individualProb = individualReplacement.probability;
				}

				// Are we grouping?
				if (propListItem.index == -1)
				{
					// Yes, grouping - initialise a flag to show if we've matched.
					bool matched = false;

					// Iterate through each item in our existing list of props.
					foreach (PropListItem item in propList)
					{
						// Check to see if we already have this in the list - matching original prefab, individual replacement prefab, building replacement prefab, all-building replacement prefab, and probability.
						if (item.originalPrefab == propListItem.originalPrefab && item.individualPrefab == propListItem.individualPrefab && item.replacementPrefab == propListItem.replacementPrefab && propListItem.allPrefab == item.allPrefab)
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
				propList.Add(propListItem);
			}

			// Create return fastlist from our filtered list, ordering by name.
			targetList.rowsData = new FastList<object>
			{
				m_buffer = targetSearchStatus == (int)OrderBy.NameDescending ? propList.OrderByDescending(item => item.DisplayName).ToArray() : propList.OrderBy(item => item.DisplayName).ToArray(),
				m_size = propList.Count
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
