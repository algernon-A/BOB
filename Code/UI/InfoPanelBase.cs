﻿using System.Linq;
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
		// Layout constants.
		protected const float Margin = 5f;
		protected const float LeftWidth = 400f;
		protected const float ButtonWidth = 150f;
		protected const float MiddleX = LeftWidth + Margin;
		protected const float MiddleWidth = ButtonWidth + (Margin * 2f);
		protected const float RightX = MiddleX + MiddleWidth;
		protected const float RightWidth = 320f;
		protected const float TitleHeight = 45f;
		protected const float ToolbarHeight = 50f;
		protected const float ListY = TitleHeight + ToolbarHeight;
		protected const float ListHeight = UIPropRow.RowHeight * 16f;
		protected const float PanelWidth = RightX + RightWidth + Margin;
		protected const float PanelHeight = ListY + ListHeight + (Margin * 2f);

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
		protected UIFastList targetList;
		protected UIFastList loadedList;
		protected UILabel noPropsLabel;
		protected UICheckBox hideVanilla;
		protected UITextField nameFilter;
		protected UIButton replaceButton, revertButton;


		// Button labels.
		protected abstract string ReplaceLabel { get; }

		// Trees or props?
		protected abstract bool IsTree { get; }

		/// <summary>
		/// Populates a fastlist with a list of target-specific trees or props.
		/// </summary>
		/// <param name="isTree">True for a list of trees, false for props</param>
		/// <returns>Populated fastlist of loaded prefabs</returns>
		protected abstract FastList<object> TargetList(bool isTree);

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

				// Check if actual item has been set.
				if (currentTargetItem != null)
				{
					PrefabInfo effectivePrefab = currentTargetItem.replacementPrefab ?? currentTargetItem.allPrefab ?? currentTargetItem.originalPrefab;

					// Select current replacement prefab.
					loadedList.FindItem(effectivePrefab);

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
			titleLabel.relativePosition = new Vector2(50f, 13f);
			titleLabel.text = Translations.Translate("BOB_NAM");

			// Close button.
			UIButton closeButton = AddUIComponent<UIButton>();
			closeButton.relativePosition = new Vector2(width - 35, 2);
			closeButton.normalBgSprite = "buttonclose";
			closeButton.hoveredBgSprite = "buttonclosehover";
			closeButton.pressedBgSprite = "buttonclosepressed";
			closeButton.eventClick += (component, clickEvent) => InfoPanelManager.Close();

			// Target prop list.
			UIPanel leftPanel = AddUIComponent<UIPanel>();
			leftPanel.width = LeftWidth;
			leftPanel.height = ListHeight;
			leftPanel.relativePosition = new Vector2(Margin, ListY);
			targetList = UIFastList.Create<UIPrefabPropRow>(leftPanel);
			ListSetup(targetList);

			// Loaded prop list.
			UIPanel rightPanel = AddUIComponent<UIPanel>();
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
			replaceLabel.relativePosition = new Vector2(LeftWidth + (Margin * 2), ReplaceLabelY);

			// Replace button.
			replaceButton = UIControls.AddSmallerButton(this, LeftWidth + (Margin * 2), ReplaceY, ReplaceLabel, ButtonWidth);

			// Revert button.
			revertButton = UIControls.AddSmallerButton(this, LeftWidth + (Margin * 2), RevertY, Translations.Translate("BOB_PNL_REV"), ButtonWidth);

			// Name filter.
			nameFilter = UIControls.BigLabelledTextField(this, width - 200f - Margin, 40f, Translations.Translate("BOB_FIL_NAME"));
			// Event handlers for name filter textbox.
			nameFilter.eventTextChanged += (control, text) => loadedList.rowsData = LoadedList(IsTree);
			nameFilter.eventTextSubmitted += (control, text) => loadedList.rowsData = LoadedList(IsTree);

			// Vanilla filter.
			hideVanilla = UIControls.AddCheckBox((UIComponent)(object)this, nameFilter.relativePosition.x, 75f, Translations.Translate("BOB_PNL_HDV"));
			hideVanilla.eventCheckChanged += (control, isChecked) =>
			{
					// Filter list.
					loadedList.rowsData = LoadedList(IsTree);

					// Store state.
					ModSettings.hideVanilla = isChecked;
			};


			// Set initial checkbox state.
			hideVanilla.isChecked = ModSettings.hideVanilla;
		}


		/// <summary>
		/// Updates the target item record for changes in replacement status (e.g. after applying or reverting changes).
		/// </summary>
		/// <param name="propListItem">Target item</param>
		protected virtual void UpdateTargetItem(PropListItem propListItem)
        {
        }


		/// <summary>
		/// Populates a fastlist with a filtered list of loaded trees or props.
		/// </summary>
		/// <param name="isTree">True for a list of trees, false for props</param>
		/// <returns>Populated fastlist of loaded prefabs</returns>
		protected FastList<object> LoadedList(bool isTree)
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
			fastList.rowHeight = UIPropRow.RowHeight;

			// Behaviour.
			fastList.canSelect = true;
			fastList.autoHideScrollbar = true;

			// Data.
			fastList.rowsData = new FastList<object>();
			fastList.selectedIndex = -1;
		}
	}
}
