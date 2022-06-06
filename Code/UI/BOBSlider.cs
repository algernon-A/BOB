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

				// Set value according to 'limit to visible' setting
				trueValue = LimitToVisible ? visibleValue : value;

				// Set slider and textfield values (visble and true values accordingly), suppressing events if they aren't already suppressed.
				bool oldSuppressEvents = suppressEvents;
				suppressEvents = true;
				this.value = visibleValue;
				SetText();
				suppressEvents = oldSuppressEvents;
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
		/// Returns the current step multiplier based on modifier key states.
		/// For float 0.1/0.01 for none/Ctrl, for Int just 1
		/// </summary>
		private float Multiplier
		{
			get
			{
				// Integer or float?
				if (!IsInt)
				{
					if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
					{
						// Control modifier.
						return 0.01f;
					}

					// Default float multiplier.
					return 0.1f;
				}

				// Default multiplier.
				return 1;
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
					TrueValue = result.RoundToNearest(Multiplier);
				}

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
				// Apply current multiplier and update TrueValue.
				TrueValue = value.RoundToNearest(Multiplier);
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

			// Set current value according to multiplier state.
			TrueValue = trueValue.RoundToNearest(multiplier) + (mouseEvent.wheelDelta * multiplier);

			// Use event and invoke any handlers.
			mouseEvent.Use();
			Invoke("OnMouseWheel", mouseEvent);
		}


		/// <summary>
		/// Updates the displayed textfield values.
		/// </summary>
		private void SetText()
		{
			if (ValueField != null)
			{
				ValueField.text = TrueValue.RoundToNearest(Multiplier).ToString();
			}
		}
	}
}