using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ColossalFramework.UI;


namespace BOB
{
    class BOBBuildingInfoPanel : BOBInfoPanel
	{
		// Current selection reference.
		BuildingInfo currentBuilding;

		// Panel components.
		internal UITextField probabilityField;
		private UICheckBox treeCheck;
		private UICheckBox propCheck;
		private UICheckBox groupCheck;


		// Button labels.
		protected override string ReplaceLabel => Translations.Translate("BOB_PNL_RTB");

		protected override string ReplaceAllLabel => Translations.Translate("BOB_PNL_RAB");


		// Trees or props?
		protected override bool IsTree => treeCheck.isChecked;


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

			// Add checkboxes.
			propCheck = UIUtils.AddCheckBox(this, Translations.Translate("BOB_PNL_PRP"), Margin, TitleHeight);
			treeCheck = UIUtils.AddCheckBox(this, Translations.Translate("BOB_PNL_TRE"), Margin, TitleHeight + Margin + propCheck.height);
			groupCheck = UIUtils.AddCheckBox(this, Translations.Translate("BOB_PNL_GRP"), 155f, TitleHeight);

			// Probability label and textfield.
			UILabel probabilityLabel = AddUIComponent<UILabel>();
			probabilityLabel.relativePosition = new Vector2(LeftWidth + (Margin * 2), ProbabilityY);
			probabilityLabel.text = Translations.Translate("BOB_PNL_PRB");

			probabilityField = UIUtils.AddTextField(this, 190f, 30f);
			probabilityField.relativePosition = new Vector2(LeftWidth + (Margin * 2), ProbabilityY + probabilityLabel.height);

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
					// Try to read the probability text field.
					if (int.TryParse(probabilityField.text, out int result))
					{
						// Successful read - set probability.
						probability = result;
					}

					// (Re) set prpbability textfield text to what we currently have.
					probabilityField.text = probability.ToString();

				// Make sure we have valid a target and replacement.
				if (currentTargetItem != null && replacementPrefab != null)
				{
					// Create new replacement record with current info.
					Replacement replacement = new Replacement
					{
						isTree = treeCheck.isChecked,
						probability = probability,
						originalProb = currentTargetItem.originalProb,
						angle = currentTargetItem.angle,
						targetIndex = currentTargetItem.index,
						replacementInfo = replacementPrefab,
						replaceName = replacementPrefab.name,

						// Original prefab is null if no active replacement; in which case, use the current prefab (which IS the original prefab).
						targetInfo = currentTargetItem.originalPrefab ?? currentTargetItem.currentPrefab
					};
					replacement.targetName = replacement.targetInfo.name;

					// Individual or grouped replacement?
					if (currentTargetItem.index >= 0)
					{
						// Individual replacement - add as-is.
						BuildingReplacement.AddReplacement(currentBuilding, replacement);
					}
					else
					{
						// Grouped replacement - iterate through each index in the list.
						foreach (int index in currentTargetItem.indexes)
						{
							// Add the replacement, providing an index override to the current index.
							BuildingReplacement.AddReplacement(currentBuilding, replacement, index);
						}
					}

					// Save configuration file and refresh target list (to reflect our changes).
					ConfigurationUtils.SaveConfig();
					TargetListRefresh();
				}
			};

			// All building button event handler.
			replaceAllButton.eventClicked += (control, clickEvent) =>
			{
				// Apply replacement.
				AllBuildingReplacement.Apply(currentTargetItem.originalPrefab ?? currentTargetItem.currentPrefab, replacementPrefab);

				// Save configuration file and refresh building list (to reflect our changes).
				ConfigurationUtils.SaveConfig();
				TargetListRefresh();
			};

			// Revert button event handler.
			revertButton.eventClicked += (control, clickEvent) =>
			{
				// Building or all-building reversion?
				if (currentTargetItem.allPrefab != null)
				{
					// All-building reversion - make sure we've got a currently active replacement before doing anything.
					if (currentTargetItem.originalPrefab != null)
					{
						// Apply all-building reversion.
						AllBuildingReplacement.Revert(currentTargetItem.originalPrefab, currentTargetItem.allPrefab);

						// Save configuration file and refresh target list (to reflect our changes).
						ConfigurationUtils.SaveConfig();
						TargetListRefresh();
					}
				}
				else
				{
					// Building reversion - ensuire that we've got a current selection before doing anything.
					if (currentTargetItem != null)
					{
						// Individual or grouped reversion?
						if (currentTargetItem.index >= 0)
						{
							// Individual reversion.
							BuildingReplacement.Revert(currentBuilding, currentTargetItem.index);
						}
						else
						{
							// Grouped reversion - iterate through each index in the list.
							foreach (int index in currentTargetItem.indexes)
							{
								// Revert the replacement, providing an index override to the current index.
								BuildingReplacement.Revert(currentBuilding, index);
							}
						}

						// Revert probability textfield value.
						probabilityField.text = currentTargetItem.originalProb.ToString();

						// Save configuration file and refresh building list (to reflect our changes).
						ConfigurationUtils.SaveConfig();
						TargetListRefresh();
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

			// Check to see if this target contains any props.
			if (currentBuilding?.m_props == null || currentBuilding.m_props.Length == 0)
			{
				// No props - show 'no props' label and return an empty list.
				noPropsLabel.Show();
				return new FastList<object>();
			}

			// Local reference.
			BuildingInfo.Prop[] props = currentBuilding.m_props;

			// Iterate through each building prop.
			for (int i = 0; i < props.Length; ++i)
			{
				// Create new list item.
				PropListItem propListItem = new PropListItem();

				// Try to get relevant prefab (prop/tree).
				PrefabInfo prefabInfo = isTree ? (PrefabInfo)props[i]?.m_finalTree : (PrefabInfo)props[i]?.m_finalProp;

				// Check to see if we were succesful - if not (e.g. we only want trees and this is a prop), continue on to next building prop.
				if (prefabInfo?.name == null)
				{
					continue;
				}

				// Grouped or individual?
				if (groupCheck.isChecked)
				{
					// Grouped - set index to -1 and add to our list of indexes.
					propListItem.index = -1;
					propListItem.indexes.Add(i);
				}
				else
				{
					// Individual - set index to the current building prop indexes.
					propListItem.index = i;
				}

				// Try to get original (pre-replacement) tree/prop prefab.
				propListItem.originalPrefab = BuildingReplacement.GetOriginal(currentBuilding, i);

				// If the above returned null, there's no currently active building replacement.
				if (propListItem.originalPrefab == null)
				{
					// Check for currently active all-building replacement.
					propListItem.originalPrefab = AllBuildingReplacement.ActiveReplacement(currentBuilding, i);
					if (propListItem.originalPrefab == null)
					{
						// No currently active all-building replacement - therefore, the current prefab IS the original, so set original prefab record accordingly.
						propListItem.originalPrefab = prefabInfo;
					}
					else
					{
						// There's a currently active all-building replacement - add that to our record.
						propListItem.allPrefab = prefabInfo;
					}
				}
				else
				{
					// There's a currently active building replacement - add that to our record.
					propListItem.currentPrefab = prefabInfo;
				}

				// Angle and probability.
				propListItem.angle = props[i].m_angle;
				propListItem.probability = props[i].m_probability;
				propListItem.originalProb = BuildingReplacement.OriginalProbability(currentBuilding, i);

				// Are we grouping?
				if (propListItem.index == -1)
				{
					// Yes, grouping - initialise a flag to show if we've matched.
					bool matched = false;

					// Iterate through each item in our existing list of props.
					foreach (PropListItem item in propList)
					{
						// Check to see if we already have this in the list - matching original prefab, building replacement prefab, all-building replacement prefab, and probability.
						if (item.originalPrefab == propListItem.originalPrefab && item.currentPrefab == propListItem.currentPrefab && propListItem.allPrefab == item.allPrefab && item.probability == propListItem.probability)
						{
							// We've already got an identical grouped instance of this item - add this index to the list of indexes under that item and set the flag to indicate that we've done so.
							item.indexes.Add(i);
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
			object[] array = fastList.m_buffer = propList.OrderBy(item => UIUtils.GetDisplayName(item.originalPrefab.name)).ToArray();
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


		/// <summary>
		/// Refreshes the target prop list according to current settings.
		/// </summary>
		private void TargetListRefresh()
		{
			// Save current list position.
			float listPosition = targetList.listPosition;
			targetList.Refresh();

			// Rebuild list.
			targetList.rowsData = TargetList(treeCheck.isChecked);

			// Restore list position and (re)select the current target item in the list.
			targetList.listPosition = listPosition;
			targetList.FindItem(currentTargetItem);

			// Update button states.
			UpdateButtonStates();
		}
	}
}
