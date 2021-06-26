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
		// Controls - align bottom with bottom of lists, and work up.
		private const float SliderHeight = 38f;
		private const float FieldOffset = SliderHeight + Margin;
		private const float OffsetPanelBase = ListY + ListHeight;
		private const float OffsetLabelY = Margin;
		private const float XOffsetY = OffsetLabelY + 20f;
		private const float YOffsetY = XOffsetY + SliderHeight;
		private const float ZOffsetY = YOffsetY + SliderHeight;
		private const float OffsetPanelHeight = ZOffsetY + SliderHeight;
		private const float OffsetPanelY = OffsetPanelBase - OffsetPanelHeight;

		private const float AngleY = OffsetPanelY - FieldOffset;
		private const float ProbabilityY = AngleY - FieldOffset;


		// Current selections.
		protected int probability;

		// Panel components.
		protected UIButton replaceAllButton;
		protected BOBSlider probabilitySlider, angleSlider, xSlider, ySlider, zSlider;
		private readonly UICheckBox randomCheck;

		// Button tooltips.
		protected abstract string ReplaceAllTooltipKey { get; }

		// Initial tree/prop checked state.
		protected override bool InitialTreeCheckedState => ModSettings.treeSelected;

		// Replace all button atlas.
		protected abstract UITextureAtlas ReplaceAllAtlas { get; }


		/// <summary>
		/// Constructor.
		/// </summary>
		internal BOBInfoPanel()
		{
			// Replace all button.
			replaceAllButton = AddIconButton(this, MidControlX + replaceButton.width, ReplaceY, BigIconSize, ReplaceAllTooltipKey, ReplaceAllAtlas);
			replaceAllButton.eventClicked += ReplaceAll;

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

			// Set initial button states);
			UpdateButtonStates();

			// Normal/random toggle.
			randomCheck = UIControls.LabelledCheckBox((UIComponent)(object)this, hideVanilla.relativePosition.x, hideVanilla.relativePosition.y + hideVanilla.height + (Margin / 2f), Translations.Translate("BOB_PNL_RSW"), 12f, 0.7f);
			randomCheck.eventCheckChanged += RandomCheckChanged;

			// Random settings button.
			UIButton randomButton = AddIconButton(this, MiddleX, TitleHeight + Margin, ToggleSize, "BOB_PNL_RST", TextureUtils.LoadSpriteAtlas("bob_random_small"));
			randomButton.eventClicked += (control, clickEvent) => BOBRandomPanel.Create();
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
		/// Replace all button event handler.
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
		/// </summary>
		protected abstract void ReplaceAll(UIComponent control, UIMouseEventParameter mouseEvent);


		/// <summary>
		/// Updates button states (enabled/disabled) according to current control states.
		/// </summary>
		protected override void UpdateButtonStates()
		{
			// Disable by default (selectively (re)-enable if eligible).
			replaceButton.Disable();
			replaceAllButton.Disable();
			revertButton.Disable();

			// Buttons are only enabled if a current target item is selected.
			if (CurrentTargetItem != null)
			{
				// Reversion requires a currently active replacement (for the relevant target/all-building setting).
				if (CurrentTargetItem.individualPrefab != null || CurrentTargetItem.replacementPrefab != null || CurrentTargetItem.allPrefab != null)
				{
					revertButton.Enable();
				}
				else
				{
					revertButton.tooltip = Translations.Translate("BOB_PNL_REV_TIP");
				}

				// Replacement requires a valid replacement selection.
				if (replacementPrefab != null)
				{
					replaceButton.Enable();
					replaceAllButton.Enable();
				}
			}
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
						m_buffer = PrefabLists.randomTrees.OrderBy(x => x.name.ToLower()).ToArray(),
						m_size = PrefabLists.randomTrees.Count
					};
				}
				else
				{
					// Props.
					loadedList.rowsData = new FastList<object>
					{
						m_buffer = PrefabLists.randomProps.OrderBy(x => x.name.ToLower()).ToArray(),
						m_size = PrefabLists.randomProps.Count
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
