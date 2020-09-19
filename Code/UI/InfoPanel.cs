using System;
using System.Collections.Generic;
using System.Linq;
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;


namespace BOB
{
	public class BOBInfoPanel : UIPanel
	{
		// Layout constants.
		private const float LeftWidth = 500f;
		private const float MiddleWidth = 200f;
		private const float RightWidth = 400f;
		private const float PanelHeight = 490f;
		private const float TitleHeight = 45f;
		private const float ToolbarHeight = 50f;
		private const float Margin = 5f;

		// Component locations.
		private const float ProbabilityY = 95f;
		private const float GlobalY = 155f;
		private const float ReplaceY = 185f;
		private const float RevertY = 220f;


		// Current selections.
		private BuildingInfo currentBuilding;
		private PropListItem currentBuildingItem;
		private PrefabInfo replacementPrefab;
		private int probability;

		// Panel components.
		private UIFastList buildingList;
		private UIFastList loadedList;
		internal UITextField probabilityField;
		private UIButton replaceButton;
		private UIButton revertButton;
		private UICheckBox allCheck;
		private UICheckBox treeCheck;
		private UICheckBox propCheck;
		private UICheckBox groupCheck;
		private UICheckBox hideVanilla;
		private UITextField nameFilter;
		private UILabel noPropsLabel;


		/// <summary>
		/// Sets the current building item and updates button states accordingly.
		/// </summary>
		internal PropListItem CurrentBuildingItem
		{
			set
			{
				currentBuildingItem = value;
				UpdateButtonStates();
			}
		}


		/// <summary>
		/// Sets the current replacement prefab and updates buttons states accordingly.
		/// </summary>
		internal PrefabInfo ReplacementPrefab
		{
			set
			{
				replacementPrefab = value;
				UpdateButtonStates();
			}
		}


		/// <summary>
		/// Performs initial setup 
		/// </summary>
		/// <param name="parentTransform">Parent transform</param>
		/// <param name="buildingInfo">Currently selected building</param>
		internal virtual void Setup(Transform parentTransform, BuildingInfo buildingInfo)
		{
			try
			{
				// Set building reference.
				currentBuilding = buildingInfo;

				// Basic behaviour.
				transform.parent = parentTransform;
				autoLayout = false;
				canFocus = true;
				isInteractive = true;

				// Appearance.
				backgroundSprite = "MenuPanel2";
				opacity = 0.8f;

				// Size and position.
				size = new Vector2(LeftWidth + MiddleWidth + RightWidth + (Margin * 4), PanelHeight + TitleHeight + ToolbarHeight + (Margin * 3));
				relativePosition = new Vector2(Mathf.Floor((GetUIView().fixedWidth - width) / 2), Mathf.Floor((GetUIView().fixedHeight - height) / 2));

				// Drag bar.
				UIDragHandle dragHandle = AddUIComponent<UIDragHandle>();
				dragHandle.width = this.width - 50f;
				dragHandle.height = this.height;
				dragHandle.relativePosition = Vector3.zero;
				dragHandle.target = this;

				// Title label.
				UILabel titleLabel = AddUIComponent<UILabel>();
				titleLabel.relativePosition  = new Vector2(50f, 13f);
				titleLabel.text = Translations.Translate("BOB_NAM");

				// Close button.
				UIButton closeButton = AddUIComponent<UIButton>();
				closeButton.relativePosition = new Vector3(width - 35, 2);
				closeButton.normalBgSprite = "buttonclose";
				closeButton.hoveredBgSprite = "buttonclosehover";
				closeButton.pressedBgSprite = "buttonclosepressed";
				closeButton.eventClick += (component, clickEvent) =>
				{
					InfoPanelManager.Close();
				};

				// Add checkboxes.
				propCheck = UIUtils.AddCheckBox(this, Translations.Translate("BOB_PNL_PRP"), Margin, TitleHeight);
				treeCheck = UIUtils.AddCheckBox(this, Translations.Translate("BOB_PNL_TRE"), Margin, TitleHeight + Margin + propCheck.height);
				groupCheck = UIUtils.AddCheckBox(this, Translations.Translate("BOB_PNL_GRP"), 155f, TitleHeight);
				allCheck = UIUtils.AddCheckBox(this, Translations.Translate("BOB_PNL_ALB"), LeftWidth + (Margin * 2), GlobalY);

				// Building prop list.
				UIPanel leftPanel = AddUIComponent<UIPanel>();
				leftPanel.width = LeftWidth;
				leftPanel.height = PanelHeight;
				leftPanel.relativePosition = new Vector2(Margin, TitleHeight + ToolbarHeight);
				buildingList = UIFastList.Create<UIBuildingPropRow>(leftPanel);
				ListSetup(buildingList);

				// Loaded prop list.
				UIPanel rightPanel = AddUIComponent<UIPanel>();
				rightPanel.width = RightWidth;
				rightPanel.height = PanelHeight;
				rightPanel.relativePosition = new Vector2(LeftWidth + MiddleWidth + (Margin * 3), TitleHeight + ToolbarHeight);
				loadedList = UIFastList.Create<UILoadedPropRow>(rightPanel);
				ListSetup(loadedList);

				// 'No props' label (starts hidden).
				noPropsLabel = leftPanel.AddUIComponent<UILabel>();
				noPropsLabel.relativePosition = new Vector2(Margin, Margin);
				noPropsLabel.Hide();

				// Name filter.
				nameFilter = UIUtils.LabelledTextField(this, Translations.Translate("BOB_FIL_NAME"));
				nameFilter.relativePosition = new Vector2(width - nameFilter.width - Margin, 40f);

				// Vanilla filter.
				hideVanilla = UIUtils.AddCheckBox((UIComponent)(object)this, Translations.Translate("BOB_PNL_HDV"), nameFilter.relativePosition.x, 75f);
				hideVanilla.eventCheckChanged += (control, isChecked) =>
				{
					// Filter list.
					loadedList.rowsData = LoadedList(treeCheck.isChecked);

					// Store state.
					ModSettings.hideVanilla = isChecked;
				};

				// Replace button.
				replaceButton = UIUtils.CreateButton(this, Translations.Translate("BOB_PNL_REP"), 190f, LeftWidth + (Margin * 2), ReplaceY);
				replaceButton.eventClicked += (control, clickEvent) =>
				{
					// Global or local replacement?
					if (allCheck.isChecked)
					{
						// Global replacement - apply.
						GlobalReplacement.ApplyGlobal(currentBuildingItem.originalPrefab ?? currentBuildingItem.currentPrefab, replacementPrefab);

						// Save configuration file and refresh building list (to reflect our changes).
						ConfigurationUtils.SaveConfig();
						BuildingListRefresh();
					}
					else
					{
						// Local replacement.
						// Try to read the probability text field.
						if (int.TryParse(probabilityField.text, out int result))
						{
							// Successful read - set probability.
							probability = result;
						}

						// (Re) set prpbability textfield text to what we currently have.
						probabilityField.text = probability.ToString();

						// Make sure we have valid a target and replacement.
						if (currentBuildingItem != null && replacementPrefab != null)
						{
							// Create new replacement record with current info.
							Replacement replacement = new Replacement();
							replacement.isTree = treeCheck.isChecked;
							replacement.probability = probability;
							replacement.originalProb = currentBuildingItem.originalProb;
							replacement.angle = currentBuildingItem.angle;
							replacement.targetIndex = currentBuildingItem.index;
							replacement.replacementInfo = replacementPrefab;
							replacement.replaceName = replacementPrefab.name;

							// Original prefab is null if no active replacement; in which case, use the current prefab (which IS the original prefab).
							replacement.targetInfo = currentBuildingItem.originalPrefab ?? currentBuildingItem.currentPrefab;
							replacement.targetName = replacement.targetInfo.name;

							// Individual or grouped replacement?
							if (currentBuildingItem.index >= 0)
							{
								// Individual replacement - add as-is.
								BuildingReplacement.AddReplacement(currentBuilding, replacement);
							}
							else
							{
								// Grouped replacement - iterate through each index in the list.
								foreach (int index in currentBuildingItem.indexes)
								{
									// Add the replacement, providing an index override to the current index.
									BuildingReplacement.AddReplacement(currentBuilding, replacement, index);
								}
							}

							// Save configuration file and refresh building list (to reflect our changes).
							ConfigurationUtils.SaveConfig();
							BuildingListRefresh();
						}
					}
				};


				// Revert button.
				revertButton = UIUtils.CreateButton(this, Translations.Translate("BOB_PNL_REV"), 190f, LeftWidth + (Margin * 2), RevertY);
				revertButton.eventClicked += (control, clickEvent) =>
				{
					// Building or global reversion?
					if (allCheck.isChecked)
					{
						// Global reversion - make sure we've got a currently active replacement before doing anything.
						if (currentBuildingItem.originalPrefab != null && currentBuildingItem.globalPrefab != null)
						{
							// Apply global reversion.
							GlobalReplacement.RevertGlobal(currentBuildingItem.originalPrefab, currentBuildingItem.globalPrefab);

							// Save configuration file and refresh building list (to reflect our changes).
							ConfigurationUtils.SaveConfig();
							BuildingListRefresh();
						}
					}
					else
					{
						// Building reversion - ensuire that we've got a current selection before 
						if (currentBuildingItem != null)
						{
							// Individual or grouped reversion?
							if (currentBuildingItem.index >= 0)
							{
								// Individual reversion.
								BuildingReplacement.Revert(currentBuilding, currentBuildingItem.index);
							}
							else
							{
								// Grouped replacement - iterate through each index in the list.
								foreach (int index in currentBuildingItem.indexes)
								{
									// Revert the replacement, providing an index override to the current index.
									BuildingReplacement.Revert(currentBuilding, index);
								}
							}

							// Revert probability textfield value.
							probabilityField.text = currentBuildingItem.originalProb.ToString();

							// Save configuration file and refresh building list (to reflect our changes).
							ConfigurationUtils.SaveConfig();
							BuildingListRefresh();
						}
					}
				};

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
						currentBuildingItem = null;
						replacementPrefab = null;

						// Set loaded lists to 'props'.
						loadedList.rowsData = LoadedList(isTree: false);
						buildingList.rowsData = BuildingList(isTree: false);

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
						currentBuildingItem = null;
						replacementPrefab = null;

						// Set loaded lists to 'trees'.
						loadedList.rowsData = LoadedList(isTree: true);
						buildingList.rowsData = BuildingList(isTree: true);

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
					// Rebuild building list.
					buildingList.rowsData = BuildingList(treeCheck.isChecked);

					// Store current group state as most recent state.
					ModSettings.lastGroup = isChecked;
				};

				// Event handlers for name filter textbox.
				nameFilter.eventTextChanged += (control, text) =>
				{
					loadedList.rowsData = LoadedList(treeCheck.isChecked);
				};
				nameFilter.eventTextSubmitted += (control, text) =>
				{
					loadedList.rowsData = LoadedList(treeCheck.isChecked);
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
				hideVanilla.isChecked = ModSettings.hideVanilla;

				// Set initial button states.
				UpdateButtonStates();
			}
			catch (Exception exception)
			{
				Debugging.LogException(exception);
			}
		}

		
		/// <summary>
		/// Refreshes the building prop list according to current settings.
		/// </summary>
		private void BuildingListRefresh()
		{
			// Save current list position.
			float listPosition = buildingList.listPosition;

			// Rebuild list.
			buildingList.rowsData = BuildingList(treeCheck.isChecked);

			// Restore list position and (re)select the current building item in the list.
			buildingList.listPosition = listPosition;
			buildingList.FindItem(currentBuildingItem);

			// Update button states.
			UpdateButtonStates();
		}


		/// <summary>
		/// Updates button states (enabled/disabled) according to current control states.
		/// </summary>
		private void UpdateButtonStates()
		{
			// Disable by default (selectively (re)-enable if eligible).
			replaceButton.Disable();
			revertButton.Disable();

			// Buttons are only enabled if a current building item is selected.
			if (currentBuildingItem != null)
			{
				// Reversion requires a currently active replacement (for the relevant building/global setting).
				if ((!allCheck.isChecked && currentBuildingItem.currentPrefab != null) || (allCheck.isChecked && currentBuildingItem.globalPrefab != null))
				{
					revertButton.Enable();
				}

				// Replacement requires a valid replacement selection.
				if (replacementPrefab != null)
				{
					replaceButton.Enable();
				}
			}
		}


		/// <summary>
		/// Performs initial fastlist setup.
		/// </summary>
		/// <param name="fastList">Fastlist to set up</param>
		private void ListSetup(UIFastList fastList)
		{
			// Apperance, size and position.
			fastList.backgroundSprite = "UnlockingPanel";
			fastList.width = fastList.parent.width;
			fastList.height = fastList.parent.height;
			fastList.relativePosition = Vector2.zero;
			fastList.rowHeight = 30f;

			// Behaviour.
			fastList.canSelect = true;
			fastList.autoHideScrollbar = true;

			// Data.
			fastList.rowsData = new FastList<object>();
			fastList.selectedIndex = -1;
		}


		/// <summary>
		/// Populates a fastlist with a filtered list of loaded trees or props.
		/// </summary>
		/// <param name="isTree">True for a list of trees, false for props</param>
		/// <returns>Populated fastlist of loaded prefabs</returns>
		private FastList<object> LoadedList(bool isTree)
		{
			// List of prefabs that have passed filtering.
			List<PrefabInfo> list = new List<PrefabInfo>();

			// Clear current selection.
			loadedList.selectedIndex = -1;

			// Tree or prop?
			if (isTree)
			{
				// Tree - iterate through each tree in our list of loaded prefabs.
				foreach (TreeInfo loadedTree in PrefabLists.loadedTrees)
				{
					// Set display name.
					string displayName = UIUtils.GetDisplayName(loadedTree.name);

					// Apply vanilla filtering if selected.
					if (!hideVanilla.isChecked || !displayName.StartsWith("[v]"))
					{
						// Apply name filter.
						if (StringExtensions.IsNullOrWhiteSpace(nameFilter.text.Trim()) || displayName.ToLower().Contains(nameFilter.text.Trim().ToLower()))
						{
							// Filtering passed - add this prefab to our list.
							list.Add(loadedTree);
						}
					}
				}
			}
			else
			{
				// Prop - iterate through each prop in our list of loaded prefabs.
				foreach (PropInfo loadedProp in PrefabLists.loadedProps)
				{
					// Set display name.
					string displayName = UIUtils.GetDisplayName(loadedProp.name);

					// Apply vanilla filtering if selected.
					if (!hideVanilla.isChecked || !displayName.StartsWith("[v]"))
					{
						// Apply name filter.
						if (StringExtensions.IsNullOrWhiteSpace(nameFilter.text.Trim()) || displayName.ToLower().Contains(nameFilter.text.Trim().ToLower()))
						{
							// Filtering passed - add this prefab to our list.
							list.Add(loadedProp);
						}
					}
				}
			}

			// Create return fastlist from our filtered list, ordering by name.
			FastList<object> fastList = new FastList<object>();
			object[] array = fastList.m_buffer = list.OrderBy(item => UIUtils.GetDisplayName(item.name)).ToArray();
			fastList.m_size = list.Count;
			return fastList;
		}


		/// <summary>
		/// Populates a fastlist with a list of building-specific trees or props.
		/// </summary>
		/// <param name="isTree">True for a list of trees, false for props</param>
		/// <returns>Populated fastlist of loaded prefabs</returns>
		private FastList<object> BuildingList(bool isTree)
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
					// Grouped - set index to -1 and create a list of indexes.
					propListItem.indexes.Add(i);
					propListItem.index = -1;
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
					// Check for currently active global replacement.
					propListItem.originalPrefab = GlobalReplacement.ActiveReplacement(currentBuilding, i);
					if (propListItem.originalPrefab == null)
					{
						// No currently active global replacement - therefore, the current prefab IS the original, so set original prefab record accordingly.
						propListItem.originalPrefab = prefabInfo;
					}
					else
					{
						// There's a currently active global replacement - add that to our record.
						propListItem.globalPrefab = prefabInfo;
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
						// Check to see if we already have this in the list - matching original prefab, building replacement prefab, global replacement prefab, and probability.
						if (item.originalPrefab == propListItem.originalPrefab && item.currentPrefab == propListItem.currentPrefab && propListItem.globalPrefab == item.globalPrefab && item.probability == propListItem.probability)
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
	}
}
