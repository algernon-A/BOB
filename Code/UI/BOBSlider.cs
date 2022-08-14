// <copyright file="BOBSlider.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Slider with integrated components.
    /// </summary>
    public class BOBSlider : UISlider
    {
        // State flag (to avoid infinite recursive update loops).
        private bool suppressEvents = false;

        // True (not displayed) value.
        private float trueValue;

        // Linked slider value textfield.
        private UITextField _valueTextField;

        /// <summary>
        /// Value changed event (includes true value, i.e. value changes beyond the visibile range that won't trigger the default slider OnValueChanged event).
        /// </summary>
        public event PropertyChangedEventHandler<float> EventTrueValueChanged;

        /// <summary>
        /// Gets or sets a value indicating whether this is an integer slider (true) or floating-point slider (false).
        /// </summary>
        public bool IsInt { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the slider range should be limited to the visisble slider range (default false).
        /// </summary>
        public bool LimitToVisible { get; set; } = false;

        /// <summary>
        /// Gets or sets the linked value display textfield instance.
        /// </summary>
        public UITextField ValueField
        {
            get => _valueTextField;

            set
            {
                // Don't do anything if no change.
                if (value != _valueTextField)
                {
                    // Remove any attached event handler before changing the linked field.
                    if (_valueTextField != null)
                    {
                        _valueTextField.eventTextSubmitted -= OnTextSubmitted;
                    }

                    // Update value.
                    _valueTextField = value;

                    // Add event handler if there's an active new instance.
                    if (_valueTextField != null)
                    {
                        _valueTextField.eventTextSubmitted += OnTextSubmitted;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the 'true' (not just displayed) slider value; use this instead of value to ensure proper operation.
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

                // Trigger value changed events, if any.
                EventTrueValueChanged?.Invoke(this, trueValue);

                // Restore previous event state.
                suppressEvents = oldSuppressEvents;
            }
        }

        /// <summary>
        /// Sets the minimum slider step size.  Setting to 1 will make this an integer slider.
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
        /// Gets the current step multiplier based on modifier key states.
        /// For float 0.1/0.01 for none/Ctrl, for Int just 1.
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
        /// <param name="p">Mouse event parameter.</param>
        protected override void OnMouseWheel(UIMouseEventParameter p)
        {
            // Get current multiplier.
            float multiplier = Multiplier;

            // Set current value according to multiplier state.
            TrueValue = trueValue.RoundToNearest(multiplier) + (p.wheelDelta * multiplier);

            // Use event and invoke any handlers.
            p.Use();
            Invoke("OnMouseWheel", p);
        }

        /// <summary>
        /// Updates the displayed textfield values.
        /// </summary>
        private void SetText()
        {
            if (_valueTextField != null)
            {
                _valueTextField.text = TrueValue.RoundToNearest(Multiplier).ToString();
            }
        }

        /// <summary>
        /// Linked textfield value change event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="text">New text.</param>
        private void OnTextSubmitted(UIComponent c, string text)
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
    }
}