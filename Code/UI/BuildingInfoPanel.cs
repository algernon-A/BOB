﻿using System;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework.UI;


namespace BOB
{
	/// <summary>
	/// BOB building tree/prop replacement panel.
	/// </summary>
	public class BOBBuildingInfoPanel : BOBInfoPanel
	{
		// Current selection reference.
		BuildingInfo currentBuilding;
		BuildingInfo[] subBuildings;

		// Panel components.
		private UICheckBox indCheck;
		private UIDropDown subBuildingMenu;


		// Button labels.
		protected override string ReplaceLabel => Translations.Translate("BOB_PNL_RTB");

		protected override string ReplaceAllLabel => Translations.Translate("BOB_PNL_RAB");


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

						angleField.text = thisReplacement.angle.ToString();
						xField.text = thisReplacement.offsetX.ToString();
						yField.text = thisReplacement.offsetY.ToString();	
						zField.text = thisReplacement.offsetZ.ToString();
						probabilityField.text = thisReplacement.probability.ToString();
					}
					// Ditto for any building replacement.
					else if (CurrentTargetItem.replacementPrefab != null)
					{
						Logging.Message("target changed: getting building replacement for ", currentBuilding.name, " with original prefab ", CurrentTargetItem.originalPrefab.name);
						BOBBuildingReplacement thisReplacement = BuildingReplacement.instance.replacements[currentBuilding][CurrentTargetItem.originalPrefab];

						angleField.text = thisReplacement.angle.ToString();
						xField.text = thisReplacement.offsetX.ToString();
						yField.text = thisReplacement.offsetY.ToString();
						zField.text = thisReplacement.offsetZ.ToString();
						probabilityField.text = thisReplacement.probability.ToString();
					}
					// Ditto for any all-building replacement.
					else if (CurrentTargetItem.allPrefab != null)
					{
						Logging.Message("target changed: getting all-building replacement for ", currentBuilding.name, " with original prefab ", CurrentTargetItem.originalPrefab.name);
						BOBBuildingReplacement thisReplacement = AllBuildingReplacement.replacements[CurrentTargetItem.originalPrefab];

						angleField.text = thisReplacement.angle.ToString();
						xField.text = thisReplacement.offsetX.ToString();
						yField.text = thisReplacement.offsetY.ToString();
						zField.text = thisReplacement.offsetZ.ToString();
						probabilityField.text = thisReplacement.probability.ToString();
					}
					else
					{
						// No current replacement; set all relative fields to zero, and absolute fields to final prop.
						angleField.text = "0";
						xField.text = "0";
						yField.text = "0";
						zField.text = "0";
						probabilityField.text = value.originalProb.ToString();
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
		internal override void Setup(Transform parentTransform, PrefabInfo targetPrefabInfo)
		{
			// Set target reference.
			currentBuilding = targetPrefabInfo as BuildingInfo;

			// Base setup.
			base.Setup(parentTransform, targetPrefabInfo);

			// Add group checkbox.
			indCheck = UIControls.AddCheckBox(this, 155f, TitleHeight, Translations.Translate("BOB_PNL_IND"));

			// Does this building have sub-buildings?
			if (currentBuilding.m_subBuildings != null && currentBuilding.m_subBuildings.Length > 0)
			{
				// Yes - create lists of sub-buildings (names and infos).
				int numSubs = currentBuilding.m_subBuildings.Length;
				int numChoices = numSubs + 1;
				string[] subBuildingNames = new string[numChoices];
				subBuildings = new BuildingInfo[numChoices];
				subBuildingNames[0] = UIUtils.GetDisplayName(currentBuilding.name);
				subBuildings[0] = currentBuilding;

				for (int i = 0; i < numSubs; ++i)
                {
					subBuildingNames[i + 1] = UIUtils.GetDisplayName(currentBuilding.m_subBuildings[i].m_buildingInfo.name);
					subBuildings[i + 1] = currentBuilding.m_subBuildings[i].m_buildingInfo;
				}

				// Add sub-building menu.
				subBuildingMenu = UIControls.AddLabelledDropDown(this, 155f, 65f, Translations.Translate("BOB_PNL_SUB"), 250f);
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
					loadedList.rowsData = LoadedList(IsTree);
					targetList.rowsData = TargetList(IsTree);
				};
			}

			// Event handler for prop checkbox.
			propCheck.eventCheckChanged += (control, isChecked) =>
			{
				if (isChecked)
				{
					// Props are now selected - unset tree check.
					treeCheck.isChecked = false;

					// Reset current items.
					CurrentTargetItem = null;
					replacementPrefab = null;

					// Set loaded lists to 'props'.
					loadedList.rowsData = LoadedList(isTree: false);
					targetList.rowsData = TargetList(isTree: false);

					// Set 'no props' label text.
					noPropsLabel.text = Translations.Translate("BOB_PNL_NOP");
				}
				else
				{
					// Props are now unselected - set tree check if it isn't already (letting tree check event handler do the work required).
					if (!treeCheck.isChecked)
					{
						treeCheck.isChecked = true;
					}
				}

				// Save state.
				ModSettings.treeSelected = !isChecked;
			};

			// Event handler for tree checkbox.
			treeCheck.eventCheckChanged += (control, isChecked) =>
			{
				if (isChecked)
				{
					// Trees are now selected - unset prop check.
					propCheck.isChecked = false;

					// Reset current items.
					CurrentTargetItem = null;
					replacementPrefab = null;

					// Set loaded lists to 'trees'.
					loadedList.rowsData = LoadedList(isTree: true);
					targetList.rowsData = TargetList(isTree: true);

					// Set 'no props' label text.
					noPropsLabel.text = Translations.Translate("BOB_PNL_NOT");
				}
				else
				{
					// Trees are now unselected - set prop check if it isn't already (letting prop check event handler do the work required).
					if (!propCheck.isChecked)
					{
						propCheck.isChecked = true;
					}
				}

				// Save state.
				ModSettings.treeSelected = isChecked;
			};

			// Event handler for group checkbox.
			indCheck.eventCheckChanged += (control, isChecked) =>
			{
				// Rebuild target list.
				targetList.rowsData = TargetList(treeCheck.isChecked);

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

			// Replace button event handler.
			replaceButton.eventClicked += (control, clickEvent) =>
			{
				// Make sure we have valid a target and replacement.
				if (CurrentTargetItem != null && replacementPrefab != null)
				{
					// Try to parse textfields.
					float.TryParse(angleField.text, out float angle);
					float.TryParse(xField.text, out float xOffset);
					float.TryParse(yField.text, out float yOffset);
					float.TryParse(zField.text, out float zOffset);
					int.TryParse(probabilityField.text, out int probability);

					// Update text fields to match parsed values.
					angleField.text = angle.ToString();
					xField.text = xOffset.ToString();
					yField.text = yOffset.ToString();
					zField.text = zOffset.ToString();
					probabilityField.text = probability.ToString();

					// Grouped or individual?
					if (CurrentTargetItem.index < 0)
					{
						// Grouped replacement.
						BuildingReplacement.instance.Apply(currentBuilding, CurrentTargetItem.originalPrefab, replacementPrefab, angle, xOffset, yOffset, zOffset, probability);

						// Update current target.
						CurrentTargetItem.replacementPrefab = replacementPrefab;
						CurrentTargetItem.replacementProb = probability;
					}
					else
					{
						// Individual replacement.
						IndividualReplacement.instance.Apply(currentBuilding, CurrentTargetItem.originalPrefab, CurrentTargetItem.index, replacementPrefab, angle, xOffset, yOffset, zOffset, probability);

						// Update current target.
						CurrentTargetItem.individualPrefab = replacementPrefab;
						CurrentTargetItem.individualProb = probability;
					}

					// Perform post-replacment updates.
					FinishUpdate();
				}
			};

			// All building button event handler.
			replaceAllButton.eventClicked += (control, clickEvent) =>
			{
				// Saftey net - don't do anything if individual check is selected.
				if (indCheck.isChecked)
                {
					return;
                }

				// Try to parse text fields.
				float.TryParse(angleField.text, out float angle);
				float.TryParse(xField.text, out float xOffset);
				float.TryParse(yField.text, out float yOffset);
				float.TryParse(zField.text, out float zOffset);
				int.TryParse(probabilityField.text, out int probability);

				// Update text fields to match parsed values.
				angleField.text = angle.ToString();
				xField.text = xOffset.ToString();
				yField.text = yOffset.ToString();
				zField.text = zOffset.ToString();
				probabilityField.text = probability.ToString();

				// Apply replacement.
				AllBuildingReplacement.instance.Apply(CurrentTargetItem.originalPrefab ?? CurrentTargetItem.replacementPrefab, replacementPrefab, angle, xOffset, yOffset, zOffset, probability);

				// Update current target.
				CurrentTargetItem.allPrefab = replacementPrefab;
				CurrentTargetItem.allProb = probability;

				// Perform post-replacment updates.
				FinishUpdate();
			};

			// Revert button event handler.
			revertButton.eventClicked += (control, clickEvent) =>
			{
				// Individual building prop reversion?
				if (CurrentTargetItem.individualPrefab != null)
				{
					// Individual building prop reversion - ensuire that we've got a current selection before doing anything.
					if (CurrentTargetItem != null && CurrentTargetItem is PropListItem currentItem)
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
					if (CurrentTargetItem != null && CurrentTargetItem is PropListItem currentItem)
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

			// Set remaining check states from previous (OR default) settings and update button states.
			propCheck.isChecked = !ModSettings.treeSelected;
			treeCheck.isChecked = ModSettings.treeSelected;
			UpdateButtonStates();

			// Apply Harmony rendering patches.
			RenderOverlays.CurrentBuilding = selectedPrefab as BuildingInfo;
			Patcher.PatchBuildingOverlays(true);
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
		/// Populates a fastlist with a list of building-specific trees or props.
		/// </summary>
		/// <param name="isTree">True for a list of trees, false for props</param>
		/// <returns>Populated fastlist of loaded prefabs</returns>
		protected override FastList<object> TargetList(bool isTree)
		{
			// List of prefabs that have passed filtering.
			List<PropListItem> propList = new List<PropListItem>();

			// Check to see if this building contains any props.
			if (currentBuilding.m_props == null || currentBuilding.m_props.Length == 0)
			{
				// No props - show 'no props' label and return an empty list.
				noPropsLabel.Show();
				return new FastList<object>();
			}


			// Iterate through each prop in building.
			for (int propIndex = 0; propIndex < currentBuilding.m_props.Length; ++propIndex)
			{
				// Create new list item.
				PropListItem propListItem = new PropListItem();

				// Try to get relevant prefab (prop/tree), using finalProp.
				PrefabInfo finalInfo = isTree ? (PrefabInfo)currentBuilding.m_props[propIndex]?.m_finalTree : (PrefabInfo)currentBuilding.m_props[propIndex]?.m_finalProp;

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

			// Create return fastlist from our filtered list, ordering by name.
			FastList<object> fastList = new FastList<object>
			{
				m_buffer = propList.ToArray(),
				m_size = propList.Count
			};

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


		/// <summary>
		/// Performs actions to be taken once an update (application or reversion) has been applied, including saving data, updating button states, and refreshing renders.
		/// </summary>
		private void FinishUpdate()
		{
			// Save configuration file and refresh target list (to reflect our changes).
			ConfigurationUtils.SaveConfig();
			UpdateTargetList();

			// Update button states.
			UpdateButtonStates();

			// Update any dirty building renders.
			BuildingData.Update();
		}
	}
}
