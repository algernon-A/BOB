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

		// Current selections.
		protected PrefabInfo selectedPrefab;
		private TargetListItem currentTargetItem;
		private PrefabInfo replacementPrefab;

		// Panel components.
		protected UIButton replaceButton;
		protected UIPanel rightPanel;
		protected UIFastList targetList, loadedList;
		protected UILabel noPropsLabel;
		protected UIButton revertButton;
		private readonly UIButton targetNameButton;
		private readonly PreviewPanel previewPanel;

		// Search settings.
		protected int targetSearchStatus;


		/// <summary>
		// Current tree/prop state.
		/// </summary>
		protected virtual bool IsTree => treeCheck?.isChecked ?? false;


		/// <summary>
		/// Panel width.
		/// </summary>
		protected override float PanelWidth => RightX + RightWidth + Margin;


		/// <summary>
		/// Panel height.
		/// </summary>
		protected override float PanelHeight => ListY + ListHeight + (Margin * 2f);

		/// <summary>
		/// Panel opacity.
		/// </summary>
		protected override float PanelOpacity => 0.8f;


		/// <summary>
		/// Revert button Y-position.
		/// </summary>
		protected abstract float RevertButtonY { get; }


		/// <summary>
		/// Returns the current individual index number of the current selection.  This could be either the direct index or in the index array, depending on situation.
		/// </summary>
		protected int IndividualIndex => CurrentTargetItem.index < 0 ? CurrentTargetItem.indexes[0] : CurrentTargetItem.index;



		/// <summary>
		/// Checks for and displays any exception message.
		/// Called by the game every update cycle.
		/// </summary>
        public override void Update()
        {
            base.Update();

			InfoPanelManager.CheckException();
		}


        /// <summary>
        /// Sets the current target item and updates button states accordingly.
        /// </summary>
        internal virtual TargetListItem CurrentTargetItem
		{
			get => currentTargetItem;

			set
			{
				currentTargetItem = value;

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
					// No valid current selection - clear selection.
					targetList.selectedIndex = -1;

					// Clear highlighting.
					RenderOverlays.CurrentIndex = -1;
					RenderOverlays.CurrentLane = null;
					RenderOverlays.CurrentProp = null;
					RenderOverlays.CurrentTree = null;
				}

				UpdateButtonStates();
			}
		}


		/// <summary>
		/// Sets the current replacement prefab and updates button states accordingly.
		/// </summary>
		internal virtual PrefabInfo ReplacementPrefab
		{
			get => replacementPrefab;

			set
			{
				replacementPrefab = value;
				UpdateButtonStates();
				previewPanel.SetTarget(value);
			}
		}


		/// <summary>
		/// Updates all items in the target list.
		/// </summary>
		internal void UpdateTargetList()
		{
			// Iterate through each item in list.
			foreach (object item in targetList.rowsData)
			{
				if (item is TargetListItem targetListItem)
				{
					// Update status.
					UpdateTargetItem(targetListItem);
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

				// Revert button.
				revertButton = UIControls.AddSmallerButton(this, MidControlX, RevertButtonY, Translations.Translate("BOB_PNL_REV"), MidControlWidth);
				revertButton.eventClicked += Revert;

				// Extra functions label.
				UILabel functionsLabel = UIControls.AddLabel(this, MiddleX, ToggleHeaderY, Translations.Translate("BOB_PNL_FUN"), textScale: 0.8f);

				// Scale button.
				UIButton scaleButton = AddIconButton(this, MiddleX, ToggleY, ToggleSize, "BOB_PNL_SCA", TextureUtils.LoadSpriteAtlas("BOB-Scale"));
				scaleButton.eventClicked += (control, clickEvent) => BOBScalePanel.Create(IsTree, replacementPrefab);

				// Preview image.
				previewPanel = AddUIComponent<PreviewPanel>();
				previewPanel.relativePosition = new Vector2(this.width + Margin, ListY);
			}
			catch (Exception e)
			{
				// Log and report any exception.
				Logging.LogException(e, "exception creating base info panel");
			}
		}


		/// <summary>
		/// Sets the target prefab.
		/// </summary>
		/// <param name="targetPrefabInfo">Target prefab to set</param>
		internal virtual void SetTarget(PrefabInfo targetPrefabInfo)
		{
			// Don't do anything if we're already selected.
			if (selectedPrefab != targetPrefabInfo)
			{
				// Set target reference.
				selectedPrefab = targetPrefabInfo;

				// Clear selection.
				CurrentTargetItem = null;
				targetList.listPosition = 0;
				targetList.selectedIndex = -1;
			}
		}


		/// <summary>
		/// Performs any actions-on-close for the panel.
		/// </summary>
		internal virtual void Close()
        {
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
				ReplacementPrefab = null;

				// Set loaded lists to 'props'.
				LoadedList();
				Logging.Message("PropCheckChanged TargetList");
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
				ReplacementPrefab = null;

				// Set loaded lists to 'trees'.
				LoadedList();
				Logging.Message("TreeCheckChanged TargetList");
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
		protected virtual void UpdateTargetItem(TargetListItem targetListItem)
		{
		}


		/// <summary>
		/// Populates a fastlist with a filtered list of loaded trees or props.
		/// </summary>
		protected override void LoadedList()
		{
			// Clear current selection.
			loadedList.selectedIndex = -1;

			// List of prefabs that have passed filtering.
			List<PrefabInfo> list = new List<PrefabInfo>();

			bool nameFilterActive = !nameFilter.text.IsNullOrWhiteSpace();

			// Tree or prop?
			if (IsTree)
			{
				// Tree - iterate through each tree in our list of loaded prefabs.
				foreach (TreeInfo loadedTree in PrefabLists.LoadedTrees)
				{
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
				foreach (PropInfo loadedProp in PrefabLists.LoadedProps)
				{
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
			loadedList.rowsData = new FastList<object>
			{
				m_buffer = list.ToArray(),
				m_size = list.Count
			};

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
			Logging.Message("SortTargets TargetList");
			TargetList();
		}
	}
}
