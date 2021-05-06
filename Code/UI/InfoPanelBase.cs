using System;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;


namespace BOB
{
	/// <summary>
	/// Abstract base class for all BOB tree/prop replacement panels.
	/// </summary>
	internal abstract class BOBInfoPanelBase : BOBPanelBase
	{
		// Display order state.
		internal enum OrderBy
		{
			NameAscending = 0,
			NameDescending
		}


		// Layout constants - X.
		protected const float LeftWidth = 400f;
		protected const float MidControlWidth = 128f;
		protected const float MiddleX = LeftWidth + Margin;
		protected const float MiddleWidth = MidControlWidth + (Margin * 2f);
		protected const float MidControlX = MiddleX + Margin;
		protected const float RightX = MiddleX + MiddleWidth;
		protected const float RightWidth = 320f;

		// Layout constants - Y.
		protected const float ListY = FilterY + FilterHeight;
		protected const float ListHeight = UIPropRow.RowHeight * 16f;
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
		protected UIButton replaceButton, revertButton;
		private readonly UIButton targetNameButton, loadedNameButton;

		// Search settings.
		protected int targetSearchStatus, loadedSearchStatus;


		// Button tooltips.
		protected abstract string ReplaceTooltipKey { get; }

		// Trees or props?
		protected virtual bool IsTree => treeCheck?.isChecked ?? false;

		// Replace button atlas.
		protected abstract UITextureAtlas ReplaceAtlas { get; }

		// Panel width.
		protected override float PanelWidth => RightX + RightWidth + Margin;

		// Panel height.
		protected override float PanelHeight => ListY + ListHeight + (Margin * 2f);

		// Panel opacity.
		protected override float PanelOpacity => 0.8f;


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
		/// Constructor.
		/// </summary>
		internal BOBInfoPanelBase()
        {
			try
			{
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
			}
			catch (Exception e)
			{
				Logging.LogException(e, "exception setting up InfoPanelBase");
			}
		}


		/// <summary>
		/// Performs initial setup 
		/// </summary>
		/// <param name="targetPrefabInfo">Currently selected target prefab</param>
		internal virtual void Setup(PrefabInfo targetPrefabInfo)
		{
			// Set target reference.
			selectedPrefab = targetPrefabInfo;

			// Title label.
			UILabel titleLabel = AddUIComponent<UILabel>();
			titleLabel.text = Translations.Translate("BOB_NAM") + ": " + GetDisplayName(targetPrefabInfo.name);
			titleLabel.relativePosition = new Vector2(50f, (TitleHeight - titleLabel.height) / 2f);
		}


		/// <summary>
		/// Populates the target fastlist with a list of target-specific trees or props.
		/// </summary>
		protected abstract void TargetList();


		/// <summary>
		/// Updates button states (enabled/disabled) according to current control states.
		/// </summary>
		protected abstract void UpdateButtonStates();


		/// <summary>
		/// Replace button event handler.
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
		/// </summary>
		protected abstract void Replace(UIComponent control, UIMouseEventParameter mouseEvent);


		/// <summary>
		/// Revert button event handler.
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
		/// </summary>
		protected abstract void Revert(UIComponent control, UIMouseEventParameter mouseEvent);


		/// <summary>
		/// Close button event handler.
		/// </summary>
		protected override void CloseEvent() => InfoPanelManager.Close();



		/// <summary>
		/// Prop check event handler.
		/// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="isChecked">New checked state</param>
		protected override void PropCheckChanged(UIComponent control, bool isChecked)
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
		protected override void TreeCheckChanged(UIComponent control, bool isChecked)
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
		protected override void LoadedList()
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
					// Set display name.
					string displayName = PrefabLists.GetDisplayName(loadedTree);

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
			else
			{
				// Prop - iterate through each prop in our list of loaded prefabs.
				for (int i = 0; i < PrefabLists.loadedProps.Length; ++i)
				{
					PropInfo loadedProp = PrefabLists.loadedProps[i];

					// Set display name.
					string displayName = PrefabLists.GetDisplayName(loadedProp);

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
		protected override void VanillaCheckChanged(UIComponent control, bool isChecked)
		{
			// Filter list.
			base.VanillaCheckChanged(control, isChecked);

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
