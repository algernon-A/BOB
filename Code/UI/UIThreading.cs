using UnityEngine;
using ICities;
using ColossalFramework;


namespace BOB
{
    /// <summary>
    /// Threading to capture hotkeys.
    /// </summary>
    public class UIThreading : ThreadingExtensionBase
    {
        // Instance reference.
        private static UIThreading instance;

        // Flags.
        private bool operating = false;
        private bool hotkeyProcessed = false, treeDisableProcessed = false;

        // Hotkey local reference.
        private readonly SavedInputKey savedKey;

        // Tree tool disable mode key settings.
        internal static KeyCode treeDisableKey = KeyCode.T;
        internal static bool treeDisableCtrl = true;
        internal static bool treeDisableAlt = false;
        internal static bool treeDisableShift = true;


        /// <summary>
        /// Constructor - sets instance reference.
        /// </summary>
        public UIThreading()
        {
            // Set instance reference.
            instance = this;

            // Set hotkey reference.
            savedKey = ModSettings.PanelSavedKey;

            // 
        }


        /// <summary>
        /// Activates/deactivates hotkey.
        /// </summary>
        internal static bool Operating
        {
            set
            {
                if (instance != null)
                {
                    instance.operating = value;
                }
            }
        }
        

        /// <summary>
        /// Look for keypress to activate tool.
        /// </summary>
        /// <param name="realTimeDelta"></param>
        /// <param name="simulationTimeDelta"></param>
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            // Don't do anything if not active.
            if (operating)
            {
                // Check modifier keys according to settings.
                bool altPressed = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr);
                bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

                // Has hotkey been pressed?
                if (savedKey.Key != KeyCode.None && Input.GetKey(savedKey.Key))
                {
                    // Modifiers have to *exactly match* settings, e.g. "alt-E" should not trigger on "ctrl-alt-E".
                    bool altOkay = altPressed == savedKey.Alt;
                    bool ctrlOkay = ctrlPressed == savedKey.Control;
                    bool shiftOkay = shiftPressed == savedKey.Shift;

                    // Process keystroke.
                    if (altOkay && ctrlOkay && shiftOkay)
                    {
                        // Only process if we're not already doing so.
                        if (!hotkeyProcessed)
                        {
                            // Set processed flag.
                            hotkeyProcessed = true;

                            // Toggle tool status.
                            BOBTool.ToggleTool();
                        }
                    }
                    else
                    {
                        // Relevant keys aren't pressed anymore; this keystroke is over, so reset and continue.
                        hotkeyProcessed = false;
                    }
                }
                else
                {
                    // Relevant keys aren't pressed anymore; this keystroke is over, so reset and continue.
                    hotkeyProcessed = false;
                }


                // Check for diable tree tool hotkey.
                if (Input.GetKey(treeDisableKey))
                {
                    // Modifiers have to *exactly match* settings, e.g. "alt-E" should not trigger on "ctrl-alt-E".
                    bool altOkay = altPressed == treeDisableAlt;
                    bool ctrlOkay = ctrlPressed == treeDisableCtrl;
                    bool shiftOkay = shiftPressed == treeDisableShift;

                    // Process keystroke.
                    if (altOkay && ctrlOkay && shiftOkay)
                    {
                        // Only process if we're not already doing so.
                        if (!treeDisableProcessed)
                        {
                            // Set processed flag.
                            treeDisableProcessed = true;

                            // Toggle tree tool disablement.
                            ModSettings.DisableTreeTool = !ModSettings.DisableTreeTool;
                        }
                    }
                    else
                    {
                        // Relevant keys aren't pressed anymore; this keystroke is over, so reset and continue.
                        treeDisableProcessed = false;
                    }
                }
                else
                {
                    // Relevant keys aren't pressed anymore; this keystroke is over, so reset and continue.
                    treeDisableProcessed = false;
                }
            }
        }
    }
}