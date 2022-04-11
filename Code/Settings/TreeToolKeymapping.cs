using UnityEngine;
using ColossalFramework;


namespace BOB
{
    /// <summary>
    /// Keycode setting control for snap to MoveIt selection.
    /// </summary>
    public class TreeToolKeymapping : OptionsKeymapping
    {
        /// <summary>
        /// Link to game hotkey setting.
        /// </summary>
        protected override InputKey KeySetting
        {
            get => SavedInputKey.Encode(UIThreading.treeDisableKey, UIThreading.treeDisableCtrl, UIThreading.treeDisableShift, UIThreading.treeDisableAlt);

            set
            {
                UIThreading.treeDisableKey = (KeyCode)(value & 0xFFFFFFF);
                UIThreading.treeDisableCtrl = (value & 0x40000000) != 0;
                UIThreading.treeDisableShift = (value & 0x20000000) != 0;
                UIThreading.treeDisableAlt = (value & 0x10000000) != 0;
            }
        }


        /// <summary>
        /// Control label.
        /// </summary>
        protected override string Label => Translations.Translate("BOB_OPT_DTK");
    }
}