using System;
using System.Linq;
using ColossalFramework.UI;
using UnityEngine;


namespace BOB
{
	/// <summary>
	/// Abstract class for building and network BOB tree/prop replacement panels.
	/// </summary>
	internal abstract class BOBInfoPanel : BOBInfoPanelBase
	{
		/// <summary>
		/// Replacement modes.
		/// </summary>
		internal enum ReplacementModes
		{
			Individual = 0,
			Grouped,
			All,
			NumModes
		}

		// Layout constants - mode buttons.
		private const float ModeX = Margin + (ToggleSize * 3f);
		protected const float ModeY = ToggleY;

		// Layout constants - detail controls - align bottom with bottom of lists, and work up.
		private const float SliderHeight = 38f;
		private const float FieldOffset = SliderHeight + Margin;
		private const float OffsetPanelBase = ListY + ListHeight;
		private const float OffsetLabelY = Margin;
		private const float XOffsetY = OffsetLabelY + 20f;
		private const float YOffsetY = XOffsetY + SliderHeight;
		private const float ZOffsetY = YOffsetY + SliderHeight;
		private const float OffsetPanelHeight = ZOffsetY + SliderHeight;
		private const float OffsetPanelY = OffsetPanelBase - OffsetPanelHeight;
		protected const float RandomButtonX = MiddleX + ToggleSize;
		private const float AngleY = OffsetPanelY - FieldOffset;
		private const float ProbabilityY = AngleY - FieldOffset;

		// Layout constants - detail controls - from top.
		private const float ReplaceY = ListY;
		private const float RevertY = ReplaceY + 40f;


		// Panel components.
		protected BOBSlider probabilitySlider, angleSlider, xSlider, ySlider, zSlider;
		private readonly UICheckBox randomCheck;
		private readonly UICheckBox[] modeChecks = new UICheckBox[(int)ReplacementModes.NumModes];

		// Status flag.
		private bool ignoreModeCheckChanged = false;
		protected bool ignoreSliderValueChange = true;


		/// <summary>
		/// Mode icon atlas names for prop modes.
		/// </summary>
		protected abstract string[] PropModeAtlas { get; }


		/// <summary>
		/// Mode icon atlas names for tree modes.
		/// </summary>
		protected abstract string[] TreeModeAtlas { get; }


		/// <summary>
		/// Mode icon tootlip keys for prop modes.
		/// </summary>
		protected abstract string[] PropModeTipKeys { get; }


		/// <summary>
		/// Mode icon tootlip keys for tree modes.
		/// </summary>
		protected abstract string[] TreeModeTipKeys { get; }


		/// <summary>
		// Initial tree/prop checked state.
		/// </summary>
		protected override bool InitialTreeCheckedState => ModSettings.treeSelected;


		/// <summary>
		/// Revert button Y-position.
		/// </summary>
		protected override float RevertButtonY => RevertY;


		/// <summary>
		/// Currently selected building.
		/// </summary>
		protected BuildingInfo SelectedBuilding => selectedPrefab as BuildingInfo;


		/// <summary>
		/// Currently selected network.
		/// </summary>
		protected NetInfo SelectedNet => selectedPrefab as NetInfo;


		/// <summary>
		/// Current replacement mode.
		/// </summary>
		protected ReplacementModes CurrentMode
        {
			get => _currentMode;

			private set
			{
				if (_currentMode != value)
				{
					_currentMode = value;

					if (_currentMode == ReplacementModes.Individual || _currentMode == ReplacementModes.Grouped)
					{
						RenderOverlays.CurrentBuilding = SelectedBuilding;
						RenderOverlays.CurrentNet = SelectedNet;
                    }
					else
                    {
						RenderOverlays.CurrentBuilding = null;
						RenderOverlays.CurrentNet = null;
                    }
				}
			}
		}
		// Current replacement mode.
		private ReplacementModes _currentMode = ReplacementModes.Grouped;


		/// <summary>
		/// Constructor.
		/// </summary>
		internal BOBInfoPanel()
		{
			try
			{
				// Replacement mode buttons.
				for (int i = 0; i < (int)ReplacementModes.NumModes; ++i)
                {
					modeChecks[i] = IconToggleCheck(this, ModeX + (i * ToggleSize), ModeY, IsTree ? TreeModeAtlas[i] : PropModeAtlas[i], IsTree ? TreeModeTipKeys[i] : PropModeTipKeys[i]);
					modeChecks[i].objectUserData = i;
					modeChecks[i].eventCheckChanged += ModeCheckChanged;
                }
				// Set initial mode state.
				modeChecks[(int)CurrentMode].isChecked = true;

				// Revert button.
				replaceButton = UIControls.AddSmallerButton(this, MidControlX, ReplaceY, Translations.Translate("BOB_PNL_APP"), MidControlWidth);
				replaceButton.eventClicked += (control, clickEvent) => Apply();

				// Probability.
				UIPanel probabilityPanel = Sliderpanel(this, MidControlX, ProbabilityY, SliderHeight);
				probabilitySlider = AddBOBSlider(probabilityPanel, Margin, 0f, MidControlWidth - (Margin * 2f), "BOB_PNL_PRB", 0, 100, 1, "Probability");
				probabilitySlider.TrueValue = 100f;
				probabilitySlider.LimitToVisible = true;

				// Angle.
				UIPanel anglePanel = Sliderpanel(this, MidControlX, AngleY, SliderHeight);
				angleSlider = AddBOBSlider(anglePanel, Margin, 0f, MidControlWidth - (Margin * 2f), "BOB_PNL_ANG", -180, 180, 1, "Angle");

				// Offset panel.
				UIPanel offsetPanel = Sliderpanel(this, MidControlX, OffsetPanelY, OffsetPanelHeight);
				UILabel offsetLabel = UIControls.AddLabel(offsetPanel, 0f, OffsetLabelY, Translations.Translate("BOB_PNL_OFF"));
				offsetLabel.textAlignment = UIHorizontalAlignment.Center;
				while (offsetLabel.width > MidControlWidth)
				{
					offsetLabel.textScale -= 0.05f;
					offsetLabel.PerformLayout();
				}
				offsetLabel.relativePosition = new Vector2((offsetPanel.width - offsetLabel.width) / 2f, OffsetLabelY);

				// Offset sliders.
				xSlider = AddBOBSlider(offsetPanel, Margin, XOffsetY, MidControlWidth - (Margin * 2f), "BOB_PNL_XOF", -8f, 8f, 0.01f, "X offset");
				ySlider = AddBOBSlider(offsetPanel, Margin, YOffsetY, MidControlWidth - (Margin * 2f), "BOB_PNL_YOF", -8f, 8f, 0.01f, "Y offset");
				zSlider = AddBOBSlider(offsetPanel, Margin, ZOffsetY, MidControlWidth - (Margin * 2f), "BOB_PNL_ZOF", -8f, 8f, 0.01f, "Z offset");

				// Live application of position changes.
				xSlider.eventValueChanged += (control, value) => SliderChange();
				ySlider.eventValueChanged += (control, value) => SliderChange();
				zSlider.eventValueChanged += (control, value) => SliderChange();
				angleSlider.eventValueChanged += (control, value) => SliderChange();
				probabilitySlider.eventValueChanged += (control, value) => SliderChange();

				// Normal/random toggle.
				randomCheck = UIControls.LabelledCheckBox((UIComponent)(object)this, hideVanilla.relativePosition.x, hideVanilla.relativePosition.y + hideVanilla.height + (Margin / 2f), Translations.Translate("BOB_PNL_RSW"), 12f, 0.7f);
				randomCheck.eventCheckChanged += RandomCheckChanged;

				// Random settings button.
				UIButton randomButton = AddIconButton(this, RandomButtonX, TitleHeight + Margin, ToggleSize, "BOB_PNL_RST", TextureUtils.LoadSpriteAtlas("BOB-Random"));
				randomButton.eventClicked += (control, clickEvent) => BOBRandomPanel.Create();
			}
			catch (Exception e)
			{
				// Log and report any exception.
				Logging.LogException(e, "exception creating info panel");
			}
		}


		/// <summary>
		/// Performs any actions-on-close for the panel.
		/// </summary>
		internal override void Close()
		{
			// Perform post-update tasks, such as saving the config file and refreshing renders.
			FinishUpdate();
		}


		/// <summary>
		/// Tree check event handler.
		/// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="isChecked">New checked state</param>
		protected override void TreeCheckChanged(UIComponent control, bool isChecked)
        {
			// Perform the usual required tasks first.
			base.TreeCheckChanged(control, isChecked);

			// Update mode icons and tooltips to reflect the new tree/prop state.
			if (isChecked)
            {
				UpdateModeIcons(TreeModeAtlas, TreeModeTipKeys);
            }
			else
            {
				UpdateModeIcons(PropModeAtlas, PropModeTipKeys);
            }
        }


		/// <summary>
		/// Updates mode icons and tooltips (when switching between trees and props).
		/// </summary>
		/// <param name="atlasNames">Array of atlas names to apply</param>
		/// <param name="tipKeys">Array of tooltip translation keys to apply</param>
		private void UpdateModeIcons(string[] atlasNames, string[] tipKeys)
        {
			// Iterate through all mode checks.
			for (int i = 0; i < (int)ReplacementModes.NumModes; ++i)
			{
				// Load atlas.
				UITextureAtlas checkAtlas = TextureUtils.LoadSpriteAtlas(atlasNames[i]);

				// Update unchecked sprite. 
				UISprite uncheckedSprite = modeChecks[i].Find<UISprite>("UncheckedSprite");
				uncheckedSprite.atlas = checkAtlas;

				// Update checked sprite.
				((UISprite)modeChecks[i].checkedBoxObject).atlas = checkAtlas;

				// Update tooltip.
				modeChecks[i].tooltip = Translations.Translate(tipKeys[i]);
			}
        }


		/// <summary>
		/// Sets the target prefab.
		/// </summary>
		/// <param name="targetPrefabInfo">Target prefab to set</param>
		internal override void SetTarget(PrefabInfo targetPrefabInfo)
		{
			base.SetTarget(targetPrefabInfo);

			// Title label.
			SetTitle(Translations.Translate("BOB_NAM") + ": " + GetDisplayName(targetPrefabInfo.name));
		}


		/// <summary>
		/// Apply button event handler.
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
		/// </summary>
		protected abstract void Apply();


		/// <summary>
		/// Updates button states (enabled/disabled) according to current control states.
		/// </summary>
		protected override void UpdateButtonStates()
		{
		}


		/// <summary>
		/// Populates a fastlist with a filtered list of loaded trees or props.
		/// </summary>
		protected override void LoadedList()
		{
			// Are we using random props?
			if (randomCheck.isChecked)
			{
				// Yes - show only random trees/props.
				if (IsTree)
				{
					// Trees.
					loadedList.rowsData = new FastList<object>
					{
						m_buffer = PrefabLists.RandomTrees.OrderBy(x => x.name.ToLower()).ToArray(),
						m_size = PrefabLists.RandomTrees.Count
					};
				}
				else
				{
					// Props.
					loadedList.rowsData = new FastList<object>
					{
						m_buffer = PrefabLists.RandomProps.OrderBy(x => x.name.ToLower()).ToArray(),
						m_size = PrefabLists.RandomProps.Count
					};
				}

				// Reverse order of filtered list if we're searching name descending.
				if (loadedSearchStatus == (int)OrderBy.NameDescending)
				{
					Array.Reverse(loadedList.rowsData.m_buffer);
					loadedList.Refresh();
				}

				// Clear selections.
				loadedList.selectedIndex = -1;
				selectedPrefab = null;
			}
			else
			{
				// No - show normal loaded prefab list.
				base.LoadedList();
			}
		}


		/// <summary>
		/// Sets the sliders to the values specified in the given replacement record.
		/// </summary>
		/// <param name="replacement">Replacement record to use</param>
		protected void SetSliders(BOBReplacementBase replacement)
        {
			// Disable events.
			ignoreSliderValueChange = true;

			// Null check first.
			if (replacement == null)
			{
				// In the absense of valid data, set all offset fields to defaults.
				angleSlider.TrueValue = 0f;
				xSlider.TrueValue = 0;
				ySlider.TrueValue = 0;
				zSlider.TrueValue = 0;
				probabilitySlider.TrueValue = CurrentTargetItem != null ? CurrentTargetItem.originalProb : 100;
			}
			else
			{
				// Valid replacement - set slider values.
				angleSlider.TrueValue = replacement.angle;
				xSlider.TrueValue = replacement.offsetX;
				ySlider.TrueValue = replacement.offsetY;
				zSlider.TrueValue = replacement.offsetZ;
				probabilitySlider.TrueValue = replacement.probability;
			}

			// Re-enable events.
			ignoreSliderValueChange = false;
		}


		/// <summary>
		/// Event handler for mode checkbox changes.
		/// </summary>
		/// <param name="control">Calling component</param>
		/// <param name="isChecked">New checked state</param>
		private void ModeCheckChanged(UIComponent control, bool isChecked)
		{
			// Don't do anything if we're ignoring events.
			if (ignoreModeCheckChanged)
			{
				return;
			}

			// Suspend event handling while processing.
			ignoreModeCheckChanged = true;

			if (control is UICheckBox thisCheck)
			{
				// If this checkbox is being enabled, uncheck all others:
				if (isChecked)
				{
					// Don't do anything if the selected mode index isn't different to the current mode.
					if (thisCheck.objectUserData is int index && index != (int)CurrentMode)
					{
						// Iterate through all checkboxes, unchecking all those that aren't this one (checkbox index stored in objectUserData).
						for (int i = 0; i < (int)ReplacementModes.NumModes; ++i)
						{
							if (i != index)
							{
								modeChecks[i].isChecked = false;
							}
						}

						// Set current replacement mode, while saving old value.
						ReplacementModes oldMode = CurrentMode;
						CurrentMode = (ReplacementModes)index;

						// Update target list if we've changed between individual and grouped modes (we've already filtered out non-changes, so checking for any individual mode will do).
						if (oldMode == ReplacementModes.Individual || CurrentMode == ReplacementModes.Individual)
						{
							// Rebuild target list.
							TargetList();

							// Clear selection.
							targetList.selectedIndex = -1;
							CurrentTargetItem = null;
						}
					}
				}
				else
				{
					// If no other check is checked, force this one to still be checked.
					thisCheck.isChecked = true;
				}
			}

			// Resume event handling.
			ignoreModeCheckChanged = false;
		}


		/// <summary>
		/// Event handler for applying live changes on slider value change.
		/// </summary>
		private void SliderChange()
		{
			// Don't do anything if already ignoring events.
			if (!ignoreSliderValueChange)
			{
				// Disable events while applying changes.
				ignoreSliderValueChange = true;
				Apply();
				ignoreSliderValueChange = false;
			}
		}


		/// <summary>
		/// Random check event handler.
		/// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="isChecked">New checked state</param>
		private void RandomCheckChanged(UIComponent control, bool isChecked)
		{
			// Regenerate loaded list.
			LoadedList();
		}


		/// <summary>
		/// Adds a slider panel to the specified component.
		/// </summary>
		/// <param name="parent">Parent component</param>
		/// <param name="parent">Parent component</param>
		/// <param name="xPos">Relative X position</param>
		/// <param name="yPos">Relative Y position</param>
		/// <param name="height">Panel height</param>
		/// <returns>New UIPanel</returns>
		private UIPanel Sliderpanel(UIComponent parent, float xPos, float yPos, float height)
		{
			// Slider panel.
			UIPanel sliderPanel = parent.AddUIComponent<UIPanel>();
			sliderPanel.atlas = TextureUtils.InGameAtlas;
			sliderPanel.backgroundSprite = "GenericPanel";
			sliderPanel.color = new Color32(206, 206, 206, 255);
			sliderPanel.size = new Vector2(MidControlWidth, height);
			sliderPanel.relativePosition = new Vector2(xPos, yPos);

			return sliderPanel;
		}


		/// <summary>
		/// Returns a cleaned-up display name for the given prefab.
		/// </summary>
		/// <param name="prefabName">Raw prefab name</param>
		/// <returns>Cleaned display name</returns>
		private string GetDisplayName(string prefabName) => prefabName.Substring(prefabName.IndexOf('.') + 1).Replace("_Data", "");
	}
}
