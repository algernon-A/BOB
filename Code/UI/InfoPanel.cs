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
		protected UIButton replaceAllButton, configButton;
		protected BOBSlider probabilitySlider, angleSlider, xSlider, ySlider, zSlider;

		// Button tooltips.
		protected abstract string ReplaceAllTooltipKey { get; }

		// Replace all button atlas.
		protected abstract UITextureAtlas ReplaceAllAtlas { get; }


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

				// Replace all button.
				replaceAllButton = AddIconButton(this, MidControlX + replaceButton.width, ReplaceY, BigIconSize, ReplaceAllTooltipKey, ReplaceAllAtlas);

				// Probability.
				UIPanel probabilityPanel = Sliderpanel(this, MidControlX, ProbabilityY, SliderHeight);
				probabilitySlider = AddBOBSlider(probabilityPanel, 0f, "BOB_PNL_PRB", 0, 100, 1);
				probabilitySlider.TrueValue = 100f;

				// Angle.
				UIPanel anglePanel = Sliderpanel(this, MidControlX, AngleY, SliderHeight);
				angleSlider = AddBOBSlider(anglePanel, 0f, "BOB_PNL_ANG", -180, 180, 1);

				// Offset panel.
				UIPanel offsetPanel = Sliderpanel(this, MidControlX, OffsetPanelY, OffsetPanelHeight);
				UILabel offsetLabel = UIControls.AddLabel(offsetPanel, 0f, OffsetLabelY, Translations.Translate("BOB_PNL_OFF"), MidControlWidth);
				offsetLabel.textAlignment = UIHorizontalAlignment.Center;

				// Offset sliders.
				xSlider = AddBOBSlider(offsetPanel, XOffsetY, "BOB_PNL_XOF", -8f, 8f, 0.01f);
				ySlider = AddBOBSlider(offsetPanel, YOffsetY, "BOB_PNL_YOF", -8f, 8f, 0.01f);
				zSlider = AddBOBSlider(offsetPanel, ZOffsetY, "BOB_PNL_ZOF", -8f, 8f, 0.01f);

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
			}
		}


		/// <summary>
		/// Adds a BOB slider to the specified component.
		/// </summary>
		/// <param name="parent">Parent component</param>
		/// <param name="yPos">Relative Y position</param>
		/// <param name="labelKey">Text label translation key</param>
		/// <param name="minValue">Minimum displayed value</param
		/// <param name="maxValue">Maximum displayed value</param>
		/// <param name="stepSize">Minimum slider step size</param>
		/// <returns>New BOBSlider</returns>
		private BOBSlider AddBOBSlider(UIComponent parent, float yPos, string labelKey, float minValue, float maxValue, float stepSize)
		{
			const float SliderY = 18f;
			const float ValueY = 3f;
			const float LabelY = -13f;
			const float SliderHeight = 18f;
			const float FloatTextFieldWidth = 45f;
			const float IntTextFieldWidth = 38f;


			// Slider control.
			BOBSlider newSlider = parent.AddUIComponent<BOBSlider>();
			newSlider.size = new Vector2(MidControlWidth - (Margin * 2f), SliderHeight);
			newSlider.relativePosition = new Vector2(Margin, yPos + SliderY);

			// Title label.
			UIControls.AddLabel(newSlider, 0f, LabelY, Translations.Translate(labelKey), textScale: 0.7f);

			// Value field - added to parent, not to slider, otherwise slider catches all input attempts.  Integer textfields (stepsize == 1) have shorter widths.
			float textFieldWidth = stepSize == 1 ? IntTextFieldWidth : FloatTextFieldWidth;
			UITextField valueField = UIControls.TinyTextField(parent, Margin + newSlider.width - textFieldWidth, yPos + ValueY, textFieldWidth);

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

			// Event handler for textfield.
			newSlider.ValueField.eventTextSubmitted += newSlider.OnTextSubmitted;

			// Set initial values.
			newSlider.StepSize = stepSize;
			newSlider.maxValue = maxValue;
			newSlider.minValue = minValue;
			newSlider.TrueValue = 0f;

			return newSlider;
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
	}


	/// <summary>
	/// Slider with integrated components.
	/// </summary>
	public class BOBSlider : UISlider
	{
		// State flag (to avoid infinite recursive update loops).
		private bool suppressEvents = false;

		// True (not displayed) value.
		private float trueValue;

		// Float or integer slider?
		public bool IsInt { get; set; } = false;


		// Sub-components.
		public UITextField ValueField { get; set; }


		/// <summary>
		/// 'True' (not just displayed) slider value; use this instead of value to ensure proper operation.
		/// </summary>
		public float TrueValue
		{
			get => trueValue;

			set
			{
				trueValue = value;

				// Set slider display value - clamped to slider extents.
				this.value = Mathf.Clamp(value, minValue, maxValue);
			}
		}


		/// <summary>
		/// Minimum slider step size.  Setting to 1 will make this an integer slider.
		/// </summary>
		public float StepSize
		{
			set
			{
				if (value == 1)
				{
					// Set as integer.
					IsInt = true;
					stepSize = value;
				}
				else
				{
					// For non-integers, underlying step size is 1/10th of value, to ensure small changes aren't quantized out.
					stepSize = value / 10f;
				}
			}
		}


		/// <summary>
		/// Handles textfield value change; should be added as eventTextSubmitted event handler.
		/// </summary>
		/// <param name="control">Calling component(unused)</param>
		/// <param name="text">New text</param>
		public void OnTextSubmitted(UIComponent _, string text)
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
					TrueValue = IsInt ? Mathf.RoundToInt(result) : result;
				}

				// Set textfield to active value.
				ValueField.text = IsInt ? Mathf.RoundToInt(TrueValue).ToString() : TrueValue.ToString();

				// Restore event handling.
				suppressEvents = false;
			}
		}


		/// <summary>
		/// Called by game when slider value is changed.
		/// </summary>
		protected override void OnValueChanged()
		{
			// Don't do anything special if events are suppressed.
			if (!suppressEvents)
			{
				// Suppress events while we change things, to avoid infinite recursive update loops.
				suppressEvents = true;

				// Apply current multiplier.
				float multiplier = Multiplier;
				value = value.RoundToNearest(multiplier);

				// Update displayed textfield value to current slider value (need to round again for display to avoid ocassional off-by-0.001).
				TrueValue = value;
				ValueField.text = TrueValue.RoundToNearest(multiplier).ToString();

				// Restore event handling.
				suppressEvents = false;
			}

			// Complete normal slider value change processing (update thumb position, invoke events, etc.).
			base.OnValueChanged();
		}


		/// <summary>
		/// Called by game when mousewheel is scrolled.
		/// </summary>
		/// <param name="mouseEvent">Mouse event parameter</param>
		protected override void OnMouseWheel(UIMouseEventParameter mouseEvent)
		{
			// Get current multiplier.
			float multiplier = Multiplier;

			// Set current value according to multiplier state, suppressing events first to avoid value clamping, and manuall updating textfield.
			suppressEvents = true;
			TrueValue = trueValue.RoundToNearest(multiplier) + (mouseEvent.wheelDelta * multiplier);
			ValueField.text = TrueValue.RoundToNearest(multiplier).ToString();

			// Use event and invoke any handlers.
			mouseEvent.Use();
			Invoke("OnMouseWheel", mouseEvent);
		}


		/// <summary>
		/// Returns the current step multiplier based on modifier key states.
		/// For float 1/0.1/0.01 for Alt/none/Ctrl, for Int 5/1 for Alt/not Alt.
		/// </summary>
		private float Multiplier
        {
			get
            {
				if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr))
				{
					// Alt modifier.
					return IsInt ? 10 : 1f;
				}
				else if (!IsInt && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
				{
					// Control modifier.
					return 0.01f;
				}
				else
				{
					// Default multiplier.
					return IsInt ? 1 : 0.1f;
				}
			}
        }
	}
}
