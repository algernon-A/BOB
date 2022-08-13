// <copyright file="HotkeyThreading.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the Apache license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using AlgernonCommons.Keybinding;
    using ICities;
    using UnityEngine;

    /// <summary>
    /// Threading to capture hotkeys.
    /// </summary>
    public class HotkeyThreading : ThreadingExtensionBase
    {
        // Instance reference.
        private static HotkeyThreading s_instance;

        // Hotkey.
        private static Keybinding s_treeDisableKey = new Keybinding(KeyCode.T, false, false, true);

        // Flags.
        private bool _operating = true;
        private bool _processed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="HotkeyThreading"/> class.
        /// </summary>
        public HotkeyThreading()
        {
            // Set instance reference.
            s_instance = this;
        }

        /// <summary>
        /// Sets a value indicating whether hotkey detection is active.
        /// </summary>
        internal static bool Operating
        {
            set
            {
                if (s_instance != null)
                {
                    s_instance._operating = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the hotkey.
        /// </summary>
        internal static Keybinding TreeDisableKey { get => s_treeDisableKey; set => s_treeDisableKey = value; }

        /// <summary>
        /// Look for keypress to toggle vanilla tree tool activation.
        /// </summary>
        /// <param name="realTimeDelta">Real-time delta since last update.</param>
        /// <param name="simulationTimeDelta">Simulation time delta since last update.</param>
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            // Don't do anything if not active.
            if (_operating)
            {
                // Has hotkey been pressed?
                if (s_treeDisableKey.IsPressed())
                {
                    // Cancel if key input is already queued for processing.
                    if (_processed)
                    {
                        return;
                    }

                    _processed = true;

                    // Toggle tree tool disablement.
                    ModSettings.DisableTreeTool = !ModSettings.DisableTreeTool;
                }
                else
                {
                    // Relevant keys aren't pressed anymore; this keystroke is over, so reset and continue.
                    _processed = false;
                }
            }
        }
    }
}