using System.Collections.Generic;
using UnityEngine;
using ColossalFramework.UI;


namespace BOB
{
	class BOBBuildingInfoPanel : BOBInfoPanel
	{
		// Current selection reference.
		BuildingInfo currentBuilding;

		// Panel components.
		private UICheckBox groupCheck;


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

				// If we've got an individuial building prop replacement, update the offset fields with the replacement values.
				if (currentTargetItem.replacementPrefab != null)
				{
					angleField.text = IndividualReplacement.replacements[currentBuilding][currentTargetItem.index].angle.ToString();
					xField.text = IndividualReplacement.replacements[currentBuilding][currentTargetItem.index].offsetX.ToString();
					yField.text = IndividualReplacement.replacements[currentBuilding][currentTargetItem.index].offsetY.ToString();
					zField.text = IndividualReplacement.replacements[currentBuilding][currentTargetItem.index].offsetZ.ToString();
					probabilityField.text = IndividualReplacement.replacements[currentBuilding][currentTargetItem.index].probability.ToString();
				}
				// Ditto for any building replacement.
				else if (currentTargetItem.replacementPrefab != null)
				{
					angleField.text = BuildingReplacement.replacements[currentBuilding][currentTargetItem.originalPrefab].angle.ToString();
					xField.text = BuildingReplacement.replacements[currentBuilding][currentTargetItem.originalPrefab].offsetX.ToString();
					yField.text = BuildingReplacement.replacements[currentBuilding][currentTargetItem.originalPrefab].offsetY.ToString();
					zField.text = BuildingReplacement.replacements[currentBuilding][currentTargetItem.originalPrefab].offsetZ.ToString();
					probabilityField.text = BuildingReplacement.replacements[currentBuilding][currentTargetItem.originalPrefab].probability.ToString();
				}
				// Ditto for any all-building replacement.
				else if (currentTargetItem.allPrefab != null)
				{
					angleField.text = AllBuildingReplacement.replacements[currentTargetItem.originalPrefab].angle.ToString();
					xField.text = AllBuildingReplacement.replacements[currentTargetItem.originalPrefab].offsetX.ToString();
					yField.text = AllBuildingReplacement.replacements[currentTargetItem.originalPrefab].offsetY.ToString();
					zField.text = AllBuildingReplacement.replacements[currentTargetItem.originalPrefab].offsetZ.ToString();
					probabilityField.text = AllBuildingReplacement.replacements[currentTargetItem.originalPrefab].probability.ToString();				}
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
			groupCheck = UIUtils.AddCheckBox(this, Translations.Translate("BOB_PNL_GRP"), 155f, TitleHeight);

			// Event handler for prop checkbox.
			propCheck.eventCheckChanged += (control, isChecked) =>
			{
				if (isChecked)
				{
					// Props are now selected - unset tree check.
					treeCheck.isChecked = false;

					// Reset current items.
					currentTargetItem = null;
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
					currentTargetItem = null;
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
			groupCheck.eventCheckChanged += (control, isChecked) =>
			{
				// Rebuild target list.
				targetList.rowsData = TargetList(treeCheck.isChecked);

				// Store current group state as most recent state.
				ModSettings.lastGroup = isChecked;
			};

			// Replace button event handler.
			replaceButton.eventClicked += (control, clickEvent) =>
			{
				// Make sure we have valid a target and replacement.
				if (currentTargetItem != null && replacementPrefab != null)
				{
					// Try to parse textfields.
					float angle, xOffset, yOffset, zOffset;
					int probability;
					float.TryParse(angleField.text, out angle);
					float.TryParse(xField.text, out xOffset);
					float.TryParse(yField.text, out yOffset);
					float.TryParse(zField.text, out zOffset);
					int.TryParse(probabilityField.text, out probability);

					// Update text fields to match parsed values.
					angleField.text = angle.ToString();
					xField.text = xOffset.ToString();
					yField.text = yOffset.ToString();
					zField.text = zOffset.ToString();
					probabilityField.text = probability.ToString();

					// Grouped or individual?
					if (currentTargetItem.index < 0)
					{
						// Grouped replacement.
						BuildingReplacement.Apply(currentBuilding, currentTargetItem.originalPrefab, replacementPrefab, angle, xOffset, yOffset, zOffset, probability);

						// Update current target.
						currentTargetItem.replacementPrefab = replacementPrefab;
						currentTargetItem.replacementProb = probability;
					}
					else
					{
						// Individual replacement.
						IndividualReplacement.Apply(currentBuilding, currentTargetItem.originalPrefab, currentTargetItem.index, replacementPrefab, angle, xOffset, yOffset, zOffset, probability);

						// Update current target.
						currentTargetItem.individualPrefab = replacementPrefab;
						currentTargetItem.individualProb = probability;
					}

					// Save configuration file and refresh target list (to reflect our changes).
					ConfigurationUtils.SaveConfig();
					targetList.Refresh();

					// Update button states.
					UpdateButtonStates();
				}
			};

			// All building button event handler.
			replaceAllButton.eventClicked += (control, clickEvent) =>
			{
				// Try to parse text fields.
				float angle, xOffset, yOffset, zOffset;
				int probability;
				float.TryParse(angleField.text, out angle);
				float.TryParse(xField.text, out xOffset);
				float.TryParse(yField.text, out yOffset);
				float.TryParse(zField.text, out zOffset);
				int.TryParse(probabilityField.text, out probability);

				// Update text fields to match parsed values.
				angleField.text = angle.ToString();
				xField.text = xOffset.ToString();
				yField.text = yOffset.ToString();
				zField.text = zOffset.ToString();
				probabilityField.text = probability.ToString();

				// Apply replacement.
				AllBuildingReplacement.Apply(currentTargetItem.originalPrefab ?? currentTargetItem.replacementPrefab, replacementPrefab, angle, xOffset, yOffset, zOffset, probability);

				// Update current target.
				currentTargetItem.allPrefab = replacementPrefab;
				currentTargetItem.allProb = probability;

				// Save configuration file and refresh building list (to reflect our changes).
				ConfigurationUtils.SaveConfig();
				targetList.Refresh();

				// Update button states.
				UpdateButtonStates();
			};

			// Revert button event handler.
			revertButton.eventClicked += (control, clickEvent) =>
			{
				// Individual building prop reversion?
				if (currentTargetItem.individualPrefab != null)
				{
					// Individual building prop reversion - ensuire that we've got a current selection before doing anything.
					if (currentTargetItem != null && currentTargetItem is PropListItem currentItem)
					{
						// Individual reversion.
						IndividualReplacement.Revert(currentBuilding, currentTargetItem.index, true);

						// Clear current target replacement prefab.
						currentTargetItem.individualPrefab = null;
					}

					// Save configuration file and refresh building list (to reflect our changes).
					ConfigurationUtils.SaveConfig();
					targetList.Refresh();

					// Update button states.
					UpdateButtonStates();
				}
				else if (currentTargetItem.replacementPrefab != null)
				{
					// Building reversion - ensuire that we've got a current selection before doing anything.
					if (currentTargetItem != null && currentTargetItem is PropListItem currentItem)
					{
						// Grouped reversion.
						BuildingReplacement.Revert(currentBuilding, currentTargetItem.originalPrefab, true);

						// Clear current target replacement prefab.
						currentTargetItem.replacementPrefab = null;
					}

					// Save configuration file and refresh building list (to reflect our changes).
					ConfigurationUtils.SaveConfig();
					targetList.Refresh();

					// Update button states.
					UpdateButtonStates();
				}
				else if (currentTargetItem.allPrefab != null)
				{
					// All-building reversion - make sure we've got a currently active replacement before doing anything.
					if (currentTargetItem.originalPrefab)
					{
						// Apply all-building reversion.
						AllBuildingReplacement.Revert(currentTargetItem.originalPrefab, true);

						// Clear current target 'all' prefab.
						currentTargetItem.allPrefab = null;

						// Save configuration file and refresh target list (to reflect our changes).
						ConfigurationUtils.SaveConfig();
						targetList.Refresh();

						// Update button states.
						UpdateButtonStates();
					}
				}
			};

			// Set grouped checkbox initial state according to preferences.
			switch (ModSettings.groupDefault)
			{
				case 0:
					// Most recent state.
					groupCheck.isChecked = ModSettings.lastGroup;
					break;
				case 1:
					// Grouping off by default.
					groupCheck.isChecked = false;
					break;
				case 2:
					// Grouping on by default.
					groupCheck.isChecked = true;
					break;
			}

			// Set remaining check states from previous (OR default) settings.
			propCheck.isChecked = !ModSettings.treeSelected;
			treeCheck.isChecked = ModSettings.treeSelected;
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
				if (groupCheck.isChecked)
				{
					// Grouped - set index to -1 and add to our list of indexes.
					propListItem.index = -1;
					propListItem.indexes.Add(propIndex);
				}
				else
				{
					// Individual - set index to the current building prop indexes.
					propListItem.index = propIndex;
				}

				// Get original (pre-replacement) tree/prop prefab and current probability (as default original probability).
				propListItem.originalPrefab = BuildingReplacement.GetOriginal(currentBuilding, propIndex) ?? AllBuildingReplacement.GetOriginal(currentBuilding, propIndex) ?? finalInfo;
				propListItem.originalProb = currentBuilding.m_props[propIndex].m_probability;
				propListItem.originalAngle = (currentBuilding.m_props[propIndex].m_radAngle * 180f) / Mathf.PI;

				// All-building replacement and original probability (if any).
				BOBBuildingReplacement allBuildingReplacement = AllBuildingReplacement.ActiveReplacement(currentBuilding, propIndex);
				if (allBuildingReplacement != null)
				{
					propListItem.allPrefab = allBuildingReplacement.replacementInfo;
					propListItem.allProb = allBuildingReplacement.probability;
				}

				// Building replacement and original probability (if any).
				BOBBuildingReplacement buildingReplacement = BuildingReplacement.ActiveReplacement(currentBuilding, propIndex);
				if (buildingReplacement != null)
				{
					propListItem.replacementPrefab = buildingReplacement.replacementInfo;
					propListItem.replacementProb = buildingReplacement.probability;
				}

				// Vuilding replacement and original probability (if any).
				BOBBuildingReplacement individualReplacement = IndividualReplacement.ActiveReplacement(currentBuilding, propIndex);
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
