using System;
using ColossalFramework.UI;
using UnityEngine;


namespace BOB
{
	/// <summary>
	/// Abstract class for building and network BOB tree/prop replacement panels.
	/// </summary>
	public abstract class BOBInfoPanel : BOBInfoPanelBase
	{
		// Component locations.
		private const float LabelHeight = 17f;
		private const float TextFieldHeight = 15f;
		private const float Padding = 10f;
		private const float FieldOffset = LabelHeight + TextFieldHeight + Padding;
		protected const float ProbabilityY = RevertY + 45f;
		private const float AngleY = ProbabilityY + FieldOffset;
		private const float XOffsetY = AngleY + FieldOffset;
		private const float YOffsetY = XOffsetY + FieldOffset;
		private const float ZOffsetY = YOffsetY + FieldOffset;

		// Current selections.
		protected int probability;

		// Panel components.
		protected UIButton replaceAllButton, configButton;
		protected UICheckBox treeCheck, propCheck;
		protected BOBSlider probabilitySlider, angleSlider, xSlider, ySlider, zSlider;

		// Button tooltips.
		protected abstract string ReplaceAllTooltipKey { get; }

		// Replace all button atlas.
		protected abstract UITextureAtlas ReplaceAllAtlas { get; }


		// Trees or props?
		protected override bool IsTree => treeCheck?.isChecked ?? false;


		/// <summary>
		/// Performs initial setup 
		/// </summary>
		/// <param name="parentTransform">Parent transform</param>
		/// <param name="targetPrefabInfo">Currently selected target prefab</param>
		internal override void Setup(Transform parentTransform, PrefabInfo targetPrefabInfo)
		{
			try
			{
				// Perform basic panel setup.
				base.Setup(parentTransform, targetPrefabInfo);

				// Add checkboxes.
				propCheck = IconToggleCheck(this, Margin, TitleHeight + Margin, "bob_props3", "BOB_PNL_PRP");
				treeCheck = IconToggleCheck(this, Margin + propCheck.width, TitleHeight + Margin, "bob_trees_small", "BOB_PNL_TRE");

				// Replace all button.
				replaceAllButton = AddIconButton(this, MidControlX + replaceButton.width, ReplaceY, BigIconSize, ReplaceAllTooltipKey, ReplaceAllAtlas);

				// Probability.
				probabilitySlider = AddBOBSlider(this, MidControlX, ProbabilityY, "BOB_PNL_PRB");
				probabilitySlider.maxValue = 100f;
				probabilitySlider.minValue = 0f;
				probabilitySlider.stepSize = 1f;
				probabilitySlider.TrueValue = 0f;
				probabilitySlider.IsInt = true;

				// Angle.
				angleSlider = AddBOBSlider(this, MidControlX, AngleY, "BOB_PNL_ANG");
				angleSlider.maxValue = 180f;
				angleSlider.minValue = -180f;
				angleSlider.stepSize = 1f;
				angleSlider.TrueValue = 0f;

				// Offset X position.
				xSlider = AddBOBSlider(this, MidControlX, XOffsetY, "BOB_PNL_XOF");
				xSlider.maxValue = 8f;
				xSlider.minValue = -8f;
				xSlider.stepSize = 0.1f;
				xSlider.TrueValue = 0f;

				// Offset Y position.
				ySlider = AddBOBSlider(this, MidControlX, YOffsetY, "BOB_PNL_YOF");
				ySlider.maxValue = 8f;
				ySlider.minValue = -8f;
				ySlider.stepSize = 0.1f;
				ySlider.TrueValue = 0f;

				// Offset Z position.
				zSlider = AddBOBSlider(this, MidControlX, ZOffsetY, "BOB_PNL_ZOF");
				zSlider.maxValue = 8f;
				zSlider.minValue = -8f;
				zSlider.stepSize = 0.1f;
				zSlider.TrueValue = 0f;


				// Set initial button and checkbox states.
				hideVanilla.isChecked = ModSettings.hideVanilla;
				UpdateButtonStates();

			}
			catch (Exception e)
			{
				Logging.LogException(e, "exception setting up InfoPanel");
			}
		}


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
					revertButton.tooltip = "Revert is not available as there is no active replacement for this item";
                }

				// Replacement requires a valid replacement selection.
				if (replacementPrefab != null)
				{
					replaceButton.Enable();
					replaceAllButton.Enable();
				}
                else
                {
					replaceButton.tooltip = "No valid replacement selected";
					replaceAllButton.tooltip = "No valid replacement selected";
                }
			}
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
		/// Adds a BOB slider to the specified component.
		/// </summary>
		/// <param name="parent">Parent component</param>
		/// <param name="xPos">Relative X position</param>
		/// <param name="yPos">Relative Y position</param>
		/// <param name="labelKey">Text label translation key</param>
		/// <returns>New BOBSlider</returns>
		private BOBSlider AddBOBSlider(UIComponent parent, float xPos, float yPos, string labelKey)
		{
			const float SliderPanelMargin = 2f;
			const float LabelY = SliderPanelMargin;
			const float LabelHeight = 14f;
			const float SliderY = LabelY + LabelHeight + SliderPanelMargin;
			const float SliderHeight = 18f;
			const float SliderPanelHeight = SliderY + SliderHeight + SliderPanelMargin;
			const float TextFieldWidth = 40f;


			// Slider panel.
			UIPanel sliderPanel = parent.AddUIComponent<UIPanel>();
			sliderPanel.atlas = TextureUtils.InGameAtlas;
			sliderPanel.backgroundSprite = "GenericPanel";
			sliderPanel.color = new Color32(206, 206, 206, 255);
			sliderPanel.size = new Vector2(MidControlWidth, SliderPanelHeight);
			sliderPanel.relativePosition = new Vector2(xPos, yPos);

			// Title label.
			UIControls.AddLabel(sliderPanel, Margin, 6f, Translations.Translate(labelKey), textScale: 0.7f);

			// Value field.
			UITextField valueField = UIControls.TinyTextField(sliderPanel, sliderPanel.width - TextFieldWidth - Margin, 3f, TextFieldWidth);
			valueField.relativePosition = new Vector2(sliderPanel.width - valueField.width - Margin, LabelY);

			// Slider control.
			BOBSlider newSlider = sliderPanel.AddUIComponent<BOBSlider>();
			newSlider.size = new Vector2(sliderPanel.width - (Margin * 2f), SliderHeight);
			newSlider.relativePosition = new Vector2(Margin, SliderY);

			// Slider track.
			UISlicedSprite sliderSprite = newSlider.AddUIComponent<UISlicedSprite>();
			sliderSprite.atlas = TextureUtils.InGameAtlas;
			sliderSprite.spriteName = "BudgetSlider";
			sliderSprite.size = new Vector2(newSlider.width, 9f);
			sliderSprite.relativePosition = new Vector2(0f, 4f);

			// Slider thumb.
			UISlicedSprite sliderThumb = newSlider.AddUIComponent<UISlicedSprite>();
			sliderThumb.atlas = TextureUtils.InGameAtlas;
			sliderThumb.spriteName = "SliderBudget";
			newSlider.thumbObject = sliderThumb;

			// Set references.
			newSlider.ValueField = valueField;

			// Event handlers.
			newSlider.eventValueChanged += newSlider.OnSliderUpdate;
			newSlider.ValueField.eventTextSubmitted += newSlider.OnTextSubmitted;

			return newSlider;
		}
	}

#pragma warning disable IDE0060 // Remove unused parameter

	/// <summary>
	/// Slider with integrated components.
	/// </summary>
	public class BOBSlider : UISlider
	{
		// State flag (to avoid infinite recursive update loops).
		private bool suppressEvents = false;

		// Float or integer slider?
		public bool IsInt { get; set; } = false;

		// Sub-components.
		public UITextField ValueField { get; set; }


		/// <summary>
		/// 'True' (not just displayed) slider value; use this instead of value to ensure proper operation.
		/// </summary>
		public float TrueValue { get=> this.value; set => this.value = value; }


		/// <summary>
		/// Handles slider value change; should be added as eventValueChanged event handler.
		/// </summary>
		/// <param name="control">Calling component(unused)</param>
		/// <param name="value">New slider value</param>
		public void OnSliderUpdate(UIComponent control, float value)
        {
			// Don't do anything is events are suppressed.
			if (!suppressEvents)
			{
				// Suppress events while we change things, to avoid infinite recursive update loops.
				suppressEvents = true;

				// Update displayed textfield value to current slider value.
				ValueField.text = IsInt ? Mathf.RoundToInt(TrueValue).ToString() : TrueValue.ToString();

				// Restore event handling.
				suppressEvents = false;
			}
        }


		/// <summary>
		/// Handles textfield value change; should be added as eventTextSubmitted event handler.
		/// </summary>
		/// <param name="control">Calling component(unused)</param>
		/// <param name="text">New text</param>
		public void OnTextSubmitted(UIComponent control, string text)
		{
			// Don't do anything is events are suppressed.
			if (!suppressEvents)
			{
				// Suppress events while we change things, to avoid infinite recursive update loops.
				suppressEvents = true;

				// Attempt to parse textfield value.
				if (float.TryParse(text, out float result))
                {
					// Successful parse - set slider value.
					this.value = IsInt ? Mathf.RoundToInt(result) : result;
                }

				// Set textfield to active value.
				ValueField.text = IsInt ? Mathf.RoundToInt(TrueValue).ToString() : TrueValue.ToString();

				// Restore event handling.
				suppressEvents = false;
            }
        }
	}

#pragma warning restore IDE0060 // Remove unused parameter

}
