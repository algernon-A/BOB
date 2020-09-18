using UnityEngine;
using ICities;


namespace BOB
{
    public class UIThreading : ThreadingExtensionBase
    {
        // Key settings.
        public static KeyCode hotKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), "B");
        public static bool hotCtrl = false;
        public static bool hotAlt = true;
        public static bool hotShift = false;

        // Flag.
        private bool _processed = false;


        /// <summary>
        /// Look for keypress to open GUI.
        /// </summary>
        /// <param name="realTimeDelta"></param>
        /// <param name="simulationTimeDelta"></param>
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            // Has hotkey been pressed?
            if (hotKey != KeyCode.None && Input.GetKey(hotKey))
            {
                // Check modifier keys according to settings.
                bool altPressed = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr);
                bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

                // Modifiers have to *exactly match* settings, e.g. "alt-E" should not trigger on "ctrl-alt-E".
                bool altOkay = altPressed == hotAlt;
                bool ctrlOkay = ctrlPressed == hotCtrl;
                bool shiftOkay = shiftPressed == hotShift;

                // Process keystroke.
                if (altOkay && ctrlOkay && shiftOkay)
                {
                    // Only process if we're not already doing so.
                    if (!_processed)
                    {
                        // Set processed flag.
                        _processed = true;

                        // Check to see if the select tool is currently active.
                        if (ToolsModifierControl.toolController?.CurrentTool is SelectTool)
                        {
                            // Select tool is currently active - deactivate it by activating the default tool instead.
                            ToolsModifierControl.SetTool<DefaultTool>();
                        }
                        else
                        {
                            // Select tool is not currently active - select it.
                            ToolsModifierControl.toolController.CurrentTool = SelectTool.Instance;
                        }
                    }
                }
                else
                {
                    // Relevant keys aren't pressed anymore; this keystroke is over, so reset and continue.
                    _processed = false;
                }
            }
            else
            {
                // Relevant keys aren't pressed anymore; this keystroke is over, so reset and continue.
                _processed = false;
            }
        }
    }

}