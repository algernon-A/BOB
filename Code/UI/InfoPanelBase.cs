using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;


namespace BOB
{
	/// <summary>
	/// Abstract base class for all BOB tree/prop replacement panels.
	/// </summary>
	public abstract class BOBInfoPanelBase : UIPanel
	{
		// Display order state.
		internal enum OrderBy
		{
			NameAscending = 0,
			NameDescending
		}


		// Layout constants - general.
		protected const float Margin = 5f;

		// Layout constants - X.
		protected const float LeftWidth = 400f;
		protected const float MidControlWidth = 128f;
		protected const float MiddleX = LeftWidth + Margin;
		protected const float MiddleWidth = MidControlWidth + (Margin * 2f);
		protected const float MidControlX = MiddleX + Margin;
		protected const float RightX = MiddleX + MiddleWidth;
		protected const float RightWidth = 320f;
		protected const float PanelWidth = RightX + RightWidth + Margin;

		// Layout constants - Y.
		protected const float TitleHeight = 40f;
		protected const float ToolbarHeight = 42f;
		protected const float FilterY = TitleHeight + ToolbarHeight;
		protected const float FilterHeight = 20f;
		protected const float ListY = FilterY + FilterHeight;
		protected const float ListHeight = UIPropRow.RowHeight * 16f;
		protected const float PanelHeight = ListY + ListHeight + (Margin * 2f);
		protected const float BigIconSize = 64f;

		// Component locations.
		protected const float ReplaceLabelY = ListY;
		protected const float ReplaceY = ReplaceLabelY + 25f;
		protected const float ReplaceAllY = ReplaceY + 30f;
		protected const float RevertY = ReplaceAllY + 45f;


		// Current selections.
		protected PrefabInfo selectedPrefab;
		private PropListItem currentTargetItem;
		protected PrefabInfo replacementPrefab;

		// Panel components.
		protected UIPanel rightPanel;
		protected UIFastList targetList, loadedList;
		protected UILabel noPropsLabel;
		protected UICheckBox hideVanilla, treeCheck, propCheck;
		protected UITextField nameFilter;
		protected UIButton replaceButton, revertButton;
		private UIButton targetNameButton, loadedNameButton;

		// Search settings.
		protected int targetSearchStatus, loadedSearchStatus;


		// Button tooltips.
		protected abstract string ReplaceTooltipKey { get; }

		// Trees or props?
		protected virtual bool IsTree => treeCheck?.isChecked ?? false;

		// Initial tree/prop checked state.
		protected abstract bool InitialTreeCheckedState { get; }

		// Replace button atlas.
		protected abstract UITextureAtlas ReplaceAtlas { get; }



		/// <summary>
		/// Populates the target fastlist with a list of target-specific trees or props.
		/// </summary>
		protected abstract void TargetList();


		/// <summary>
		/// Updates button states (enabled/disabled) according to current control states.
		/// </summary>
		protected abstract void UpdateButtonStates();


		/// <summary>
		/// Sets the current target item and updates button states accordingly.
		/// </summary>
		internal virtual PropListItem CurrentTargetItem
		{
			get => currentTargetItem;

			set
			{
				currentTargetItem = value;

				// Refresh loaded list if needed.
				if (value != null && (loadedList.m_rowsData?.m_buffer == null || loadedList.m_rowsData.m_size == 0))
                {
					LoadedList();
				}

				// Check if actual item has been set.
				if (currentTargetItem != null)
				{
					PrefabInfo effectivePrefab = currentTargetItem.individualPrefab ?? currentTargetItem.replacementPrefab ?? currentTargetItem.allPrefab ?? currentTargetItem.originalPrefab;

					// Select current replacement prefab.
					loadedList.FindItem(effectivePrefab);

					// Set current panel selection.
					ReplacementPrefab = effectivePrefab;

					// Set highlighting.
					RenderOverlays.CurrentIndex = currentTargetItem.index;
					RenderOverlays.CurrentProp = effectivePrefab as PropInfo;
					RenderOverlays.CurrentTree = effectivePrefab as TreeInfo;
				}
				else
				{
					loadedList.selectedIndex = -1;
					RenderOverlays.CurrentIndex = -1;
					RenderOverlays.CurrentProp = null;
					RenderOverlays.CurrentTree = null;
				}

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
		/// Updates all items in the target list.
		/// </summary>
		internal void UpdateTargetList()
		{
			// Iterate through each item in list.
			foreach (object item in targetList.m_rowsData)
			{
				if (item is PropListItem propListItem)
				{
					// Update status.
					UpdateTargetItem(propListItem);
				}
			}

			// Refresh list display.
			targetList.Refresh();
		}


		/// <summary>
		/// Performs initial setup 
		/// </summary>
		/// <param name="parentTransform">Parent transform</param>
		/// <param name="targetPrefabInfo">Currently selected target prefab</param>
		internal virtual void Setup(Transform parentTransform, PrefabInfo targetPrefabInfo)
		{
			// Set target reference.
			selectedPrefab = targetPrefabInfo;

			// Basic behaviour.
			transform.parent = parentTransform;
			autoLayout = false;
			canFocus = true;
			isInteractive = true;

			// Appearance.
			backgroundSprite = "MenuPanel2";
			opacity = 0.8f;

			// Size.
			width = PanelWidth;
			height = PanelHeight;

			// Position - are we restoring the previous position?.
			if (ModSettings.rememberPosition && (InfoPanelManager.lastX != 0f || InfoPanelManager.lastY != 0f))
			{
				// 'Remember default position' is active and at least one of X and Y positions is non-zero.
				relativePosition = new Vector2(InfoPanelManager.lastX, InfoPanelManager.lastY);
			}
			else
			{
				// Default position - centre in screen.
				relativePosition = new Vector2(Mathf.Floor((GetUIView().fixedWidth - width) / 2), Mathf.Floor((GetUIView().fixedHeight - height) / 2));
			}

			// Drag bar.
			UIDragHandle dragHandle = AddUIComponent<UIDragHandle>();
			dragHandle.width = this.width - 50f;
			dragHandle.height = this.height;
			dragHandle.relativePosition = Vector3.zero;
			dragHandle.target = this;

			// Title label.
			UILabel titleLabel = AddUIComponent<UILabel>();
			titleLabel.text = Translations.Translate("BOB_NAM") + ": " + GetDisplayName(targetPrefabInfo.name);
			titleLabel.relativePosition = new Vector2(50f, (TitleHeight - titleLabel.height) / 2f);

			// Close button.
			UIButton closeButton = AddUIComponent<UIButton>();
			closeButton.relativePosition = new Vector2(width - 35, 2);
			closeButton.normalBgSprite = "buttonclose";
			closeButton.hoveredBgSprite = "buttonclosehover";
			closeButton.pressedBgSprite = "buttonclosepressed";
			closeButton.eventClick += (component, clickEvent) => InfoPanelManager.Close();


			// Order buttons.
			targetNameButton = ArrowButton(this, 30f, FilterY);
			loadedNameButton = ArrowButton(this, RightX + 10f, FilterY);

			targetNameButton.eventClicked += SortTargets;
			loadedNameButton.eventClicked += SortLoaded;

			// Default is name ascending.
			SetFgSprites(targetNameButton, "IconUpArrow2");
			SetFgSprites(loadedNameButton, "IconUpArrow2");

			// Target prop list.
			UIPanel leftPanel = AddUIComponent<UIPanel>();
			leftPanel.width = LeftWidth;
			leftPanel.height = ListHeight;
			leftPanel.relativePosition = new Vector2(Margin, ListY);
			targetList = UIFastList.Create<UIPrefabPropRow>(leftPanel);
			ListSetup(targetList);

			// Loaded prop list.
			rightPanel = AddUIComponent<UIPanel>();
			rightPanel.width = RightWidth;
			rightPanel.height = ListHeight;
			rightPanel.relativePosition = new Vector2(RightX, ListY);
			loadedList = UIFastList.Create<UILoadedPropRow>(rightPanel);
			ListSetup(loadedList);

			// Tree/Prop checkboxes.
			propCheck = IconToggleCheck(this, Margin, TitleHeight + Margin, "bob_props3", "BOB_PNL_PRP");
			treeCheck = IconToggleCheck(this, Margin + propCheck.width, TitleHeight + Margin, "bob_trees_small", "BOB_PNL_TRE");
			propCheck.isChecked = !InitialTreeCheckedState;
			treeCheck.isChecked = InitialTreeCheckedState;
			propCheck.eventCheckChanged += PropCheckChanged;
			treeCheck.eventCheckChanged += TreeCheckChanged;

			// 'No props' label (starts hidden).
			noPropsLabel = leftPanel.AddUIComponent<UILabel>();
			noPropsLabel.relativePosition = new Vector2(Margin, Margin);
			noPropsLabel.Hide();

			// Replace text label.
			UILabel replaceLabel = AddUIComponent<UILabel>();
			replaceLabel.text = Translations.Translate("BOB_PNL_REP");
			replaceLabel.relativePosition = new Vector2(MidControlX, ReplaceLabelY);

			// Replace button.
			replaceButton = AddIconButton(this, MidControlX, ReplaceY, BigIconSize, ReplaceTooltipKey, ReplaceAtlas);
			replaceButton.eventClicked += Replace;

			// Revert button.
			revertButton = UIControls.AddSmallerButton(this, MidControlX, RevertY, Translations.Translate("BOB_PNL_REV"), MidControlWidth);
			revertButton.eventClicked += Revert;

			// Name filter.
			nameFilter = UIControls.SmallLabelledTextField(this, width - 200f - Margin, TitleHeight + Margin, Translations.Translate("BOB_FIL_NAME"));
			// Event handlers for name filter textbox.
			nameFilter.eventTextChanged += (control, text) => LoadedList();
			nameFilter.eventTextSubmitted += (control, text) => LoadedList();

			// Vanilla filter.
			hideVanilla = UIControls.LabelledCheckBox((UIComponent)(object)this, nameFilter.relativePosition.x, nameFilter.relativePosition.y + nameFilter.height + (Margin / 2f), Translations.Translate("BOB_PNL_HDV"), 12f, 0.7f);
			hideVanilla.isChecked = ModSettings.hideVanilla;
			hideVanilla.eventCheckChanged += VanillaCheckChanged;
		}


		/// <summary>
		/// Replace button event handler.
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
		/// </summary>
		protected abstract void Replace(UIComponent control, UIMouseEventParameter mouseEvent);


		/// <summary>
		/// Revertt button event handler.
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
		/// </summary>
		protected abstract void Revert(UIComponent control, UIMouseEventParameter mouseEvent);


		/// <summary>
		/// Prop check event handler.
		/// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="isChecked">New checked state</param>
		protected virtual void PropCheckChanged(UIComponent control, bool isChecked)
		{
			if (isChecked)
			{
				// Props are now selected - unset tree check.
				treeCheck.isChecked = false;

				// Reset current items.
				CurrentTargetItem = null;
				replacementPrefab = null;

				// Set loaded lists to 'props'.
				LoadedList();
				TargetList();

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
		}


		/// <summary>
		/// Tree check event handler.
		/// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="isChecked">New checked state</param>
		protected void TreeCheckChanged(UIComponent control, bool isChecked)
		{
			if (isChecked)
			{
				// Trees are now selected - unset prop check.
				propCheck.isChecked = false;

				// Reset current items.
				CurrentTargetItem = null;
				replacementPrefab = null;

				// Set loaded lists to 'trees'.
				LoadedList();
				TargetList();

				// Set 'no trees' label text.
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
		}


		/// <summary>
		/// Updates the target item record for changes in replacement status (e.g. after applying or reverting changes).
		/// </summary>
		/// <param name="propListItem">Target item</param>
		protected virtual void UpdateTargetItem(PropListItem propListItem)
		{

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
					LoadedList();
					TargetList();

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
		}


		/// <summary>
		/// Populates a fastlist with a filtered list of loaded trees or props.
		/// </summary>
		protected virtual void LoadedList()
		{
			// List of prefabs that have passed filtering.
			List<PrefabInfo> list = new List<PrefabInfo>();

			bool nameFilterActive = !nameFilter.text.IsNullOrWhiteSpace();

			// Tree or prop?
			if (IsTree)
			{
				// Tree - iterate through each tree in our list of loaded prefabs.
				for (int i = 0; i < PrefabLists.loadedTrees.Length; ++i)
				{
					TreeInfo loadedTree = PrefabLists.loadedTrees[i];
					{
						// Set display name.
						string displayName = PrefabLists.GetDisplayName(loadedTree.name);

						// Apply vanilla filtering if selected.
						if (!hideVanilla.isChecked || !displayName.StartsWith("[v]"))
						{
							// Apply name filter.
							if (!nameFilterActive || displayName.ToLower().Contains(nameFilter.text.Trim().ToLower()))
							{
								// Filtering passed - add this prefab to our list.
								list.Add(loadedTree);
							}
						}
					}
				}
			}
			else
			{
				// Prop - iterate through each prop in our list of loaded prefabs.
				for (int i = 0; i < PrefabLists.loadedProps.Length; ++i)
				{
					PropInfo loadedProp = PrefabLists.loadedProps[i];

					// Set display name.
					string displayName = PrefabLists.GetDisplayName(loadedProp.name);

					// Apply vanilla filtering if selected.
					if (!hideVanilla.isChecked || !displayName.StartsWith("[v]"))
					{
						// Apply name filter.
						if (!nameFilterActive || displayName.ToLower().Contains(nameFilter.text.Trim().ToLower()))
						{
							// Filtering passed - add this prefab to our list.
							list.Add(loadedProp);
						}
					}
				}
			}

			// Master lists should already be sorted by display name so no need to sort again here.
			// Reverse order of filtered list if we're searching name descending.
			if (loadedSearchStatus == (int)OrderBy.NameDescending)
			{
				list.Reverse();
			}

			// Create return fastlist from our filtered list.
			loadedList.m_rowsData = new FastList<object>
			{
				m_buffer = list.ToArray(),
				m_size = list.Count
			};
			loadedList.Refresh();

			// Select current replacement prefab, if any.
			if (replacementPrefab != null)
			{
				loadedList.FindItem(replacementPrefab);
			}
			else
			{
				// No current selection.
				loadedList.selectedIndex = -1;
			}
		}


		/// <summary>
		/// Adds an icon-style button to the specified component at the specified coordinates.
		/// </summary>
		/// <param name="parent">Parent UIComponent</param>
		/// <param name="xPos">Relative X position</param>
		/// <param name="yPos">Relative Y position</param>
		/// <param name="size">Button size (square)</param>
		/// <param name="tooltipKey">Tooltip translation key</param>
		/// <param name="atlas">Icon atlas</param>
		/// <returns>New UIButton</returns>
		protected UIButton AddIconButton(UIComponent parent, float xPos, float yPos, float size, string tooltipKey, UITextureAtlas atlas)
		{
			UIButton newButton = parent.AddUIComponent<UIButton>();

			// Size and position.
			newButton.relativePosition = new Vector2(xPos, yPos);
			newButton.height = size;
			newButton.width = size;

			// Appearance.
			newButton.atlas = atlas;

			newButton.normalFgSprite = "normal";
			newButton.focusedFgSprite = "normal";
			newButton.hoveredFgSprite = "hovered";
			newButton.disabledFgSprite = "disabled";
			newButton.pressedFgSprite = "pressed";

			// Tooltip.
			newButton.tooltip = Translations.Translate(tooltipKey);

			return newButton;
		}


		/// <summary>
		/// Adds an icon toggle checkbox.
		/// </summary>
		/// <param name="parent">Parent component</param>
		/// <param name="xPos">Relative X position</param>
		/// <param name="yPos">Relative Y position</param>
		/// <param name="atlasName">Atlas name (for loading from file)</param>
		/// <param name="tooltipKey">Tooltip translation key</param>
		/// <returns>New checkbox</returns>
		private UICheckBox IconToggleCheck(UIComponent parent, float xPos, float yPos, string atlasName, string tooltipKey)
		{
			const float ToggleSpriteSize = 32f;

			// Size and position.
			UICheckBox checkBox = parent.AddUIComponent<UICheckBox>();
			checkBox.width = ToggleSpriteSize;
			checkBox.height = ToggleSpriteSize;
			checkBox.clipChildren = true;
			checkBox.relativePosition = new Vector2(xPos, yPos);

			// Checkbox sprites.
			UISprite sprite = checkBox.AddUIComponent<UISprite>();
			sprite.atlas = TextureUtils.LoadSpriteAtlas(atlasName);
			sprite.spriteName = "disabled";
			sprite.size = new Vector2(ToggleSpriteSize, ToggleSpriteSize);
			sprite.relativePosition = Vector3.zero;

			checkBox.checkedBoxObject = sprite.AddUIComponent<UISprite>();
			((UISprite)checkBox.checkedBoxObject).atlas = TextureUtils.LoadSpriteAtlas(atlasName);
			((UISprite)checkBox.checkedBoxObject).spriteName = "pressed";
			checkBox.checkedBoxObject.size = new Vector2(ToggleSpriteSize, ToggleSpriteSize);
			checkBox.checkedBoxObject.relativePosition = Vector3.zero;

			checkBox.tooltip = Translations.Translate(tooltipKey);

			return checkBox;
		}


		/// <summary>
		/// Performs actions to be taken once an update (application or reversion) has been applied, including saving data, updating button states, and refreshing renders.
		/// </summary>
		protected virtual void FinishUpdate()
		{
			// Save configuration file and refresh target list (to reflect our changes).
			ConfigurationUtils.SaveConfig();
			UpdateTargetList();

			// Update button states.
			UpdateButtonStates();

			// Refresh current target item to update highlighting.
			CurrentTargetItem = CurrentTargetItem;
		}


		/// <summary>
		/// Hide vanilla check event handler.
		/// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="isChecked">New checked state</param>
		private void VanillaCheckChanged(UIComponent control, bool isChecked)
		{
			// Filter list.
			LoadedList();

			// Store state.
			ModSettings.hideVanilla = isChecked;
		}



		/// <summary>
		/// Loaded list sort button event handler.
		/// <param name="control">Calling component</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
		/// </summary>
		private void SortLoaded(UIComponent control, UIMouseEventParameter mouseEvent)
		{
			// Toggle status (set to descending if we're currently ascending, otherwise set to ascending).
			if (loadedSearchStatus == (int)OrderBy.NameAscending)
			{
				// Order by name descending.
				loadedSearchStatus = (int)OrderBy.NameDescending;
			}
			else
			{
				// Order by name ascending.
				loadedSearchStatus = (int)OrderBy.NameAscending;
			}

			// Reset name order buttons.
			SetSortButton(loadedNameButton, loadedSearchStatus);


			// Regenerate loaded list.
			LoadedList();
		}


		/// <summary>
		/// Target list sort button event handler.
		/// <param name="control">Calling component</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
		/// </summary>
		private void SortTargets(UIComponent control, UIMouseEventParameter mouseEvent)
		{
				// Toggle status (set to descending if we're currently ascending, otherwise set to ascending).
				if (targetSearchStatus == (int)OrderBy.NameAscending)
				{
					// Order by name descending.
					targetSearchStatus = (int)OrderBy.NameDescending;
				}
				else
				{
					// Order by name ascending.
					targetSearchStatus = (int)OrderBy.NameAscending;
				}

				// Reset name order buttons.
				SetSortButton(control as UIButton, targetSearchStatus);

			// Regenerate loaded list.
			TargetList();
		}


		/// <summary>
		/// Sets the states of the two given sort buttons to match the given search status.
		/// </summary>
		/// <param name="activeButton">Currently active sort button</param>
		/// <param name="inactiveButton">Inactive button (other sort button for same list)</param>
		/// <param name="searchStatus">Search status to apply</param>
		private void SetSortButton(UIButton activeButton, int searchStatus)
		{
			// Null check.
			if (activeButton == null)
            {
				return;
            }

			bool ascending = searchStatus == (int)OrderBy.NameAscending;

			// Toggle status (set to descending if we're currently ascending, otherwise set to ascending).
			if (ascending)
			{
				// Order ascending.
				SetFgSprites(activeButton, "IconUpArrow2");
			}
			else
			{
				// Order descending.
				SetFgSprites(activeButton, "IconDownArrow2");
			}
		}


		/// <summary>
		/// Performs initial fastlist setup.
		/// </summary>
		/// <param name="fastList">Fastlist to set up</param>
		protected void ListSetup(UIFastList fastList)
		{
			// Apperance, size and position.
			fastList.backgroundSprite = "UnlockingPanel";
			fastList.width = fastList.parent.width;
			fastList.height = fastList.parent.height;
			fastList.relativePosition = Vector2.zero;
			fastList.rowHeight = UIPropRow.RowHeight;

			// Behaviour.
			fastList.canSelect = true;
			fastList.autoHideScrollbar = true;

			// Data.
			fastList.rowsData = new FastList<object>();
			fastList.selectedIndex = -1;
		}


		/// <summary>
		/// Adds an arrow button.
		/// </summary>
		/// <param name="parent">Parent component</param>
		/// <param name="posX">Relative X postion</param>
		/// <param name="posY">Relative Y position</param>
		/// <param name="width">Button width (default 32)</param>
		/// <param name="height">Button height (default 20)</param>
		/// <returns>New arrow button</returns>
		private UIButton ArrowButton(UIComponent parent, float posX, float posY, float width = 32f, float height = 20f)
		{
			UIButton button = parent.AddUIComponent<UIButton>();

			// Size and position.
			button.size = new Vector2(width, height);
			button.relativePosition = new Vector2(posX, posY);

			// Appearance.
			SetFgSprites(button, "IconUpArrow2");
			button.canFocus = false;

			return button;
		}


		/// <summary>
		/// Sets the foreground sprites for the given button to the specified sprite.
		/// </summary>
		/// <param name="button">Targeted button</param>
		/// <param name="spriteName">Sprite name</param>
		private void SetFgSprites(UIButton button, string spriteName)
		{
			button.normalFgSprite = button.hoveredFgSprite = button.pressedFgSprite = button.focusedFgSprite = spriteName;
		}


		/// <summary>
		/// Returns a cleaned-up display name for the given prefab.
		/// </summary>
		/// <param name="prefabName">Raw prefab name</param>
		/// <returns>Cleaned display name</returns>
		private string GetDisplayName(string prefabName) => prefabName.Substring(prefabName.IndexOf('.') + 1).Replace("_Data", "");
	}
}
