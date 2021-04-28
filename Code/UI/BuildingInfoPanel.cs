using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework.UI;


namespace BOB
{
	/// <summary>
	/// BOB building tree/prop replacement panel.
	/// </summary>
	internal class BOBBuildingInfoPanel : BOBInfoPanel
	{
		// Current selection reference.
		BuildingInfo currentBuilding;
		BuildingInfo[] subBuildings;

		// Panel components.
		private UICheckBox indCheck;
		private UIDropDown subBuildingMenu;


		// Button tooltips.
		protected override string ReplaceTooltipKey => "BOB_PNL_RTB";
		protected override string ReplaceAllTooltipKey => "BOB_PNL_RAB";


		// Replace button atlases.
		protected override UITextureAtlas ReplaceAtlas => TextureUtils.LoadSpriteAtlas("bob_single_building");
		protected override UITextureAtlas ReplaceAllAtlas => TextureUtils.LoadSpriteAtlas("bob_buildings");



		/// <summary>
		/// Handles changes to the currently selected target prefab.
		/// </summary>
		internal override PropListItem CurrentTargetItem
		{
			set
			{
				// Call base.
				base.CurrentTargetItem = value;

				// Check for null.
				if (value == null)
				{
					return;
				}

				try
				{
					// If we've got an individuial building prop replacement, update the offset fields with the replacement values.
					if (CurrentTargetItem.individualPrefab != null)
					{
						Logging.Message("target changed: individual replacement for ", currentBuilding.name, " at index ", CurrentTargetItem.index.ToString());
						BOBBuildingReplacement thisReplacement = IndividualReplacement.instance.replacements[currentBuilding][CurrentTargetItem.index];

						angleSlider.TrueValue = thisReplacement.angle;
						xSlider.TrueValue = thisReplacement.offsetX;
						ySlider.TrueValue = thisReplacement.offsetY;	
						zSlider.TrueValue = thisReplacement.offsetZ;
						probabilitySlider.TrueValue = thisReplacement.probability;
					}
					// Ditto for any building replacement.
					else if (CurrentTargetItem.replacementPrefab != null)
					{
						Logging.Message("target changed: getting building replacement for ", currentBuilding.name, " with original prefab ", CurrentTargetItem.originalPrefab.name);
						BOBBuildingReplacement thisReplacement = BuildingReplacement.instance.replacements[currentBuilding][CurrentTargetItem.originalPrefab];

						angleSlider.TrueValue = thisReplacement.angle;
						xSlider.TrueValue = thisReplacement.offsetX;
						ySlider.TrueValue = thisReplacement.offsetY;
						zSlider.TrueValue = thisReplacement.offsetZ;
						probabilitySlider.TrueValue = thisReplacement.probability;
					}
					// Ditto for any all-building replacement.
					else if (CurrentTargetItem.allPrefab != null)
					{
						Logging.Message("target changed: getting all-building replacement for ", currentBuilding.name, " with original prefab ", CurrentTargetItem.originalPrefab.name);
						BOBBuildingReplacement thisReplacement = AllBuildingReplacement.replacements[CurrentTargetItem.originalPrefab];

						angleSlider.TrueValue = thisReplacement.angle;
						xSlider.TrueValue = thisReplacement.offsetX;
						ySlider.TrueValue = thisReplacement.offsetY;
						zSlider.TrueValue = thisReplacement.offsetZ;
						probabilitySlider.TrueValue = thisReplacement.probability;
					}
					else
					{
						// No current replacement; set all relative fields to zero, and absolute fields to final prop.
						angleSlider.TrueValue = 0f;
						xSlider.TrueValue = 0f;
						ySlider.TrueValue = 0f;
						zSlider.TrueValue = 0f;
						probabilitySlider.TrueValue = value.originalProb;
					}
				}
				catch (Exception e)
                {
					Logging.LogException(e, "exception accessing current target item ");
                }
			}
		}


		/// <summary>
		/// Performs initial setup 
		/// </summary>
		/// <param name="parentTransform">Parent transform</param>
		/// <param name="targetPrefabInfo">Currently selected target prefab</param>
		internal override void Setup(PrefabInfo targetPrefabInfo)
		{
			// Set target reference.
			currentBuilding = targetPrefabInfo as BuildingInfo;

			// Base setup.
			base.Setup(targetPrefabInfo);

			// Add group checkbox.
			indCheck = UIControls.LabelledCheckBox(this, 155f, TitleHeight + Margin, Translations.Translate("BOB_PNL_IND"), 12f, 0.7f);

			// Does this building have sub-buildings?
			if (currentBuilding.m_subBuildings != null && currentBuilding.m_subBuildings.Length > 0)
			{
				// Yes - create lists of sub-buildings (names and infos).
				int numSubs = currentBuilding.m_subBuildings.Length;
				int numChoices = numSubs + 1;
				string[] subBuildingNames = new string[numChoices];
				subBuildings = new BuildingInfo[numChoices];
				subBuildingNames[0] = PrefabLists.GetDisplayName(currentBuilding.name);
				subBuildings[0] = currentBuilding;

				for (int i = 0; i < numSubs; ++i)
                {
					subBuildingNames[i + 1] = PrefabLists.GetDisplayName(currentBuilding.m_subBuildings[i].m_buildingInfo.name);
					subBuildings[i + 1] = currentBuilding.m_subBuildings[i].m_buildingInfo;
				}

				// Add sub-building menu.
				subBuildingMenu = UIControls.AddLabelledDropDown(this, 155f, indCheck.relativePosition.y + indCheck.height + (Margin / 2f), Translations.Translate("BOB_PNL_SUB"), 250f, 20f, 0.7f, 15, 4);
				subBuildingMenu.listBackground = "GenericPanelDark";
				subBuildingMenu.items = subBuildingNames;
				subBuildingMenu.selectedIndex = 0;

				// Sub-building menu event handler.
				subBuildingMenu.eventSelectedIndexChanged += (control, index) =>
				{
					// Set current building.
					currentBuilding = subBuildings[index];

					// Reset current items.
					CurrentTargetItem = null;
					replacementPrefab = null;

					// Reset loaded lists.
					LoadedList();
					TargetList();
				};
			}

			// Event handler for group checkbox.
			indCheck.eventCheckChanged += (control, isChecked) =>
			{
				// Rebuild target list.
				TargetList();

				// Clear selection.
				targetList.selectedIndex = -1;
				CurrentTargetItem = null;

				// Store current group state as most recent state.
				ModSettings.lastInd = isChecked;

				// Toggle replace all button visibility.
				if (isChecked)
                {
					replaceAllButton.Hide();
                }
				else
                {
					replaceAllButton.Show();
                }
			};

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
			if (CurrentTargetItem != null && replacementPrefab != null)
			{
				// Grouped or individual?
				if (CurrentTargetItem.index < 0)
				{
					// Grouped replacement.
					BuildingReplacement.instance.Apply(currentBuilding, CurrentTargetItem.originalPrefab, replacementPrefab, angleSlider.TrueValue, xSlider.TrueValue, ySlider.TrueValue, zSlider.TrueValue, (int)probabilitySlider.TrueValue);

					// Update current target.
					CurrentTargetItem.replacementPrefab = replacementPrefab;
					CurrentTargetItem.replacementProb = probability;
				}
				else
				{
					// Individual replacement.
					IndividualReplacement.instance.Apply(currentBuilding, CurrentTargetItem.originalPrefab, CurrentTargetItem.index, replacementPrefab, angleSlider.TrueValue, xSlider.TrueValue, ySlider.TrueValue, zSlider.TrueValue, (int)probabilitySlider.TrueValue);

					// Update current target.
					CurrentTargetItem.individualPrefab = replacementPrefab;
					CurrentTargetItem.individualProb = probability;
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
			// Individual building prop reversion?
			if (CurrentTargetItem.individualPrefab != null)
			{
				// Individual building prop reversion - ensuire that we've got a current selection before doing anything.
				if (CurrentTargetItem != null && CurrentTargetItem is PropListItem)
				{
					// Individual reversion.
					IndividualReplacement.instance.Revert(currentBuilding, CurrentTargetItem.index, true);

					// Clear current target replacement prefab.
					CurrentTargetItem.individualPrefab = null;
				}

				// Perform post-replacment updates.
				FinishUpdate();
			}
			else if (CurrentTargetItem.replacementPrefab != null)
			{
				// Building reversion - ensuire that we've got a current selection before doing anything.
				if (CurrentTargetItem != null && CurrentTargetItem is PropListItem)
				{
					// Grouped reversion.
					BuildingReplacement.instance.Revert(currentBuilding, CurrentTargetItem.originalPrefab, true);

					// Clear current target replacement prefab.
					CurrentTargetItem.replacementPrefab = null;
				}

				// Perform post-replacment updates.
				FinishUpdate();
			}
			else if (CurrentTargetItem.allPrefab != null)
			{
				// All-building reversion - make sure we've got a currently active replacement before doing anything.
				if (CurrentTargetItem.originalPrefab)
				{
					// Apply all-building reversion.
					AllBuildingReplacement.instance.Revert(CurrentTargetItem.originalPrefab, true);

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
			AllBuildingReplacement.instance.Apply(CurrentTargetItem.originalPrefab ?? CurrentTargetItem.replacementPrefab, replacementPrefab, angleSlider.TrueValue, xSlider.TrueValue, ySlider.TrueValue, zSlider.TrueValue, (int)probabilitySlider.TrueValue);

			// Update current target.
			CurrentTargetItem.allPrefab = replacementPrefab;
			CurrentTargetItem.allProb = probability;

			// Perform post-replacment updates.
			FinishUpdate();
		}


		/// <summary>
		/// Updates the target item record for changes in replacement status (e.g. after applying or reverting changes).
		/// </summary>
		/// <param name="propListItem">Target item</param>
		protected override void UpdateTargetItem(PropListItem propListItem)
		{
			// Determine index to test - if no individaul index, just grab first one from list.
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
			BOBBuildingReplacement individualReplacement = IndividualReplacement.instance.ActiveReplacement(currentBuilding, propIndex);
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
			System.Diagnostics.Stopwatch targetStopwatch = new System.Diagnostics.Stopwatch();
			targetStopwatch.Start();

			// Clear current selection.
			targetList.selectedIndex = -1;

			// List of prefabs that have passed filtering.
			List<PropListItem> propList = new List<PropListItem>();

			// Check to see if this building contains any props.
			if (currentBuilding.m_props == null || currentBuilding.m_props.Length == 0)
			{
				// No props - show 'no props' label and return an empty list.
				noPropsLabel.Show();
                targetList.m_rowsData = new FastList<object>();
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
				propListItem.originalPrefab = BuildingReplacement.instance.GetOriginal(currentBuilding, propIndex) ?? AllBuildingReplacement.instance.GetOriginal(currentBuilding, propIndex) ?? finalInfo;
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
				BOBBuildingReplacement individualReplacement = IndividualReplacement.instance.ActiveReplacement(currentBuilding, propIndex);
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
						// Check to see if we already have this in the list - matching original prefab, building replacement prefab, all-building replacement prefab, and probability.
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

			Logging.Message("basic target list setup time ", targetStopwatch.ElapsedMilliseconds.ToString());

			// Create return fastlist from our filtered list, ordering by name.
			targetList.m_rowsData = new FastList<object>
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

			targetStopwatch.Stop();
			Logging.Message("target list sort time ", targetStopwatch.ElapsedMilliseconds.ToString());
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
