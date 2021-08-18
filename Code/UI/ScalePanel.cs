using System;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;


namespace BOB
{
	/// <summary>
	/// Panel for prop scale selection.
	/// </summary>
	internal class BOBScalePanel : BOBPanelBase
	{
		// Layout constants - X.
		private const float ControlX = (Margin * 2f);
		private const float ControlWidth = 250f;
		private const float LoadedX = ControlX + ControlWidth + (Margin * 2f);
		private const float LoadedWidth = 320f;

		// Layout constants - Y.
		private const float SliderHeight = 38f;
		private const float ToolY = TitleHeight + Margin;
		private const float ListY = ToolY + ToolbarHeight + Margin;
		private const float MinOffsetY = ListY;
		private const float MaxOffsetY = MinOffsetY + SliderHeight;
		private const float RevertY = MaxOffsetY + SliderHeight + 45f;
		private const float ListHeight = UIPropRow.RowHeight * 16f;


		// Instance references.
		private static GameObject uiGameObject;
		private static BOBScalePanel panel;
		internal static BOBScalePanel Panel => panel;

		// Panel components.
		private readonly UIFastList loadedList;
		private readonly BOBSlider minScaleSlider, maxScaleSlider;
		private readonly UIButton revertButton;

		// Current selection.
		private PrefabInfo selectedLoadedPrefab;

		// Status.
		private bool disableEvents = false;


		/// <summary>
		/// Initial tree/prop checked state.
		/// </summary>
		protected override bool InitialTreeCheckedState => false;


		// Panel width.
		protected override float PanelWidth => LoadedX + LoadedWidth + Margin;

		// Panel height.
		protected override float PanelHeight => ListY + ListHeight + Margin;

		// Panel opacity.
		protected override float PanelOpacity => 1f;


		/// <summary>
		/// Sets the currently selected loaded prefab.
		/// </summary>
		internal PrefabInfo SelectedLoadedPrefab
		{
			set
			{
				// Disable events, otherwise slider value changes will mess things up.
				disableEvents = true;

				// Set value.
				selectedLoadedPrefab = value;

				// Prop or tree?  Set slider values accordingly.
				if (selectedLoadedPrefab is PropInfo prop)
				{
					minScaleSlider.value = prop.m_minScale;
					maxScaleSlider.value = prop.m_maxScale;

					// Enable revert button.
					revertButton.Enable();
				}
				else if (selectedLoadedPrefab is TreeInfo tree)
                {
					minScaleSlider.value = tree.m_minScale;
					maxScaleSlider.value = tree.m_maxScale;

					// Enable revert button.
					revertButton.Enable();
				}
				else
                {
					// Neither prop nor tree, presumably null - set sliders to default values.
					minScaleSlider.value = 1f;
					maxScaleSlider.value = 1f;

					// Disable revert button if no valid selection.
					revertButton.Disable();
                }

				// Restore events.
				disableEvents = false;
			}
		}


		/// <summary>
		/// Creates the panel object in-game and displays it.
		/// </summary>
		internal static void Create()
		{
			try
			{
				// If no GameObject instance already set, create one.
				if (uiGameObject == null)
				{
					// Give it a unique name for easy finding with ModTools.
					uiGameObject = new GameObject("BOBScalePanel");
					uiGameObject.transform.parent = UIView.GetAView().transform;

					// Create new panel instance and add it to GameObject.
					panel = uiGameObject.AddComponent<BOBScalePanel>();
					panel.transform.parent = uiGameObject.transform.parent;

					// Hide previous window, if any.
					InfoPanelManager.Panel?.Hide();
				}
			}
			catch (Exception e)
			{
				Logging.LogException(e, "exception creating scale panel");
			}
		}


		/// <summary>
		/// Closes the panel by destroying the object (removing any ongoing UI overhead).
		/// </summary>
		internal static void Close()
		{
			// Don't do anything if no panel.
			if (panel == null)
			{
				return;
			}

			// Destroy game objects.
			GameObject.Destroy(panel);
			GameObject.Destroy(uiGameObject);

			// Let the garbage collector do its work (and also let us know that we've closed the object).
			panel = null;
			uiGameObject = null;

			// Show previous window, if any.
			InfoPanelManager.Panel?.Show();
		}

		internal BOBScalePanel()
		{
			// Default position - centre in screen.
			relativePosition = new Vector2(Mathf.Floor((GetUIView().fixedWidth - width) / 2), Mathf.Floor((GetUIView().fixedHeight - height) / 2));

			// Title label.
			SetTitle(Translations.Translate("BOB_NAM") + " : " + Translations.Translate("BOB_SCA_TIT"));

			// Minimum scale slider.
			minScaleSlider = AddBOBSlider(this, ControlX, MinOffsetY, ControlWidth - (Margin * 2f), Translations.Translate("BOB_SCA_MIN"), 0.5f, 2f, 0.5f, "MinScale");
			minScaleSlider.eventValueChanged += MinScaleValue;
			minScaleSlider.value = 1f;
			maxScaleSlider = AddBOBSlider(this, ControlX, MaxOffsetY + 40f, ControlWidth - (Margin * 2f), Translations.Translate("BOB_SCA_MAX"), 0.5f, 2f, 0.5f, "MaxScale");
			maxScaleSlider.eventValueChanged += MaxScaleValue;
			maxScaleSlider.value = 1f;

			// Revert button.
			revertButton = UIControls.AddSmallerButton(this, ControlX, RevertY, Translations.Translate("BOB_PNL_REV"), ControlWidth);
			revertButton.eventClicked += Revert;
			revertButton.Disable();

			// Loaded prop list.
			UIPanel loadedPanel = AddUIComponent<UIPanel>();
			loadedPanel.width = LoadedWidth;
			loadedPanel.height = ListHeight;
			loadedPanel.relativePosition = new Vector2(LoadedX, ListY);
			loadedList = UIFastList.Create<UILoadedScalingPropRow>(loadedPanel);
			ListSetup(loadedList);

			// Order button.
			loadedNameButton = ArrowButton(this, LoadedX + 10f, ListY - 20f);
			loadedNameButton.eventClicked += SortLoaded;

			// Default is name ascending.
			SetFgSprites(loadedNameButton, "IconUpArrow2");

			// Populate loaded list.
			LoadedList();

			// Bring to front.
			BringToFront();
		}


		/// <summary>
		/// Close button event handler.
		/// </summary>
		protected override void CloseEvent() => Close();


		/// <summary>
		/// Populates a fastlist with a filtered list of loaded trees or props.
		/// </summary>
		protected override void LoadedList()
		{
			// List of prefabs that have passed filtering.
			List<PrefabInfo> list = new List<PrefabInfo>();

			bool nameFilterActive = !nameFilter.text.IsNullOrWhiteSpace();

			if (IsTree)
			{
				// Tree - iterate through each prop in our list of loaded prefabs.
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

					// Skip any props that require height or water maps.
					if (loadedProp.m_requireHeightMap || loadedProp.m_requireWaterMap)
					{
						continue;
					}

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
		}


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

				// Reset current item.
				selectedLoadedPrefab = null;

				// Set loaded lists to 'props'.
				LoadedList();
			}
			else
			{
				// Props are now unselected - set tree check if it isn't already (letting tree check event handler do the work required).
				if (!treeCheck.isChecked)
				{
					treeCheck.isChecked = true;
				}
			}
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

				// Reset current item.
				selectedLoadedPrefab = null;

				// Set loaded lists to 'trees'.
				LoadedList();
			}
			else
			{
				// Trees are now unselected - set prop check if it isn't already (letting prop check event handler do the work required).
				if (!propCheck.isChecked)
				{
					propCheck.isChecked = true;
				}
			}
		}

		/// <summary>
		/// Revert button event handler.
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
		/// </summary>
		private void Revert(UIComponent control, UIMouseEventParameter mouseEvent)
		{
			// Null check.
			if (selectedLoadedPrefab?.name != null)
			{
				// Revert current selection.
				Scaling.instance.Revert(selectedLoadedPrefab);

				// Reset prefab record to reset slider valies.
				SelectedLoadedPrefab = selectedLoadedPrefab;
			}
		}


		/// <summary>
		/// Minimum scale slider event handler.
		/// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="value">New value</param>
		private void MinScaleValue(UIComponent control, float value)
		{
			// Don't apply changes if events are disabled.
			if (!disableEvents)
			{
				Scaling.instance.ApplyMinScale(selectedLoadedPrefab, value);
			}
		}


		/// <summary>
		/// Maximum scale slider event handler.
		/// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="value">New value</param>
		private void MaxScaleValue(UIComponent control, float value)
		{
			// Don't apply changes if events are disabled.
			if (!disableEvents)
			{
				Scaling.instance.ApplyMaxScale(selectedLoadedPrefab, value);
			}
		}


		// Trees or props?
		private bool IsTree => treeCheck?.isChecked ?? false;
	}
}