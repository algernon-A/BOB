using UnityEngine;
using ICities;


namespace BOB
{
    public class UIThreading : ThreadingExtensionBase
    {
        // Key settings - static (central state).
        private static KeyCode staticKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), "B");
        private static bool staticCtrl = false, staticAlt = true, staticShift = false;

        // Key settings -instance (hopefully avoiding a cache miss).
        private KeyCode hotKey;
        private bool hotCtrl, hotAlt, hotShift;

        // Instance reference.
        private static UIThreading instance;

        // Flags.
        private bool operating = true;
        private bool _processed = false;


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
        /// Current hotkey.
        /// </summary>
        internal static KeyCode HotKey
        {
            get => instance != null ? instance.hotKey : staticKey;

            set
            {
                staticKey = value;

                if (instance != null)
                {
                    instance.hotKey = value;
                }
            }
        }


        /// <summary>
        /// Status of control as a hotkey modifier.
        /// </summary>
        internal static bool HotCtrl
        {
            get => instance != null ? instance.hotCtrl : staticCtrl;

            set
            {
                staticCtrl = value;

                if (instance != null)
                {
                    instance.hotCtrl = value;
                }
            }
        }


        /// <summary>
        /// Status of alt as a hotkey modifier.
        /// </summary>
        internal static bool HotAlt
        {
            get => instance != null ? instance.hotAlt : staticAlt;

            set
            {
                staticAlt = value;

                if (instance != null)
                {
                    instance.hotAlt = value;
                }
            }
        }


        /// <summary>
        /// Status of shift as a hotkey modifier.
        /// </summary>
        internal static bool HotShift
        {
            get => instance != null ? instance.hotShift : staticShift;

            set
            {
                staticShift = value;

                if (instance != null)
                {
                    instance.hotShift = value;
                }
            }
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        public UIThreading()
        {
            // Set instance reference.
            instance = this;

            // Set initial hotkey values from current settings.
            hotKey = staticKey;
            hotCtrl = staticCtrl;
            hotAlt = staticAlt;
            hotShift = staticShift;
        }


        /// <summary>
        /// Look for keypress to open GUI.
        /// </summary>
        /// <param name="realTimeDelta"></param>
        /// <param name="simulationTimeDelta"></param>
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            // Don't do anything if not active.
            if (operating)
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

                            // Is a BOB info panel already open?
                            if (InfoPanelManager.Panel != null)
                            {
                                // Yes - close it.
                                InfoPanelManager.Close();
                            }
                            // Otherwise, check to see if the select tool is currently active.
                            else if (ToolsModifierControl.toolController?.CurrentTool is BOBTool)
                            {
                                // Select tool is currently active - deactivate it by activating the default tool instead.
                                ToolsModifierControl.SetTool<DefaultTool>();
                            }
                            else
                            {
                                // Select tool is not currently active - select it.
                                ToolsModifierControl.toolController.CurrentTool = BOBTool.Instance;
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
}