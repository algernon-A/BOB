using UnityEngine;
using ColossalFramework.UI;


namespace BOB
{


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

		// Limit to visible range?
		public bool LimitToVisible { get; set; } = false;


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
				// Clamp value to visible slider range.
				float visibleValue = Mathf.Clamp(value, minValue, maxValue);

				// Are we limiting to visible range?
				if (LimitToVisible)
				{
					// Yes; use clamped value.
					trueValue = visibleValue;
				}
				else
				{
					// No - use raw (unclamped) value.
					trueValue = value;
				}

				// Set slider display value - clamped to slider extents.
				this.value = visibleValue;

				// Forcibly set text.
				ValueField.text = IsInt ? Mathf.RoundToInt(TrueValue).ToString() : TrueValue.ToString();
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