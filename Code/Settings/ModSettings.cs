using System.Xml.Serialization;
using UnityEngine;
using ColossalFramework;


namespace BOB
{
	/// <summary>
	/// Global mod settings.
	/// </summary>
	/// 
	[XmlRoot("TreePropReplacer")]
	public class ModSettings
	{
        // Tree ruining.
        [XmlElement("StopTreeRuining")]
        public bool NoTreeRuining { get => StopTreeRuining; set => StopTreeRuining = value; }

        // Prop ruining.
        [XmlElement("StopPropRuining")]
        public bool NoPropRuining { get => StopPropRuining; set => StopPropRuining = value; }

        // Whether or not we're running with Extended Manager Library's EPropManager.
        [XmlIgnore]
        internal static bool ePropManager = false;

        // Default behaviour of the show individual props setting.
        [XmlIgnore]
        internal static int indDefault;

        // Last selected individual setting.
        [XmlIgnore]
        internal static bool lastInd;

        [XmlIgnore]
        // Last selected vanilla filter state.
        internal static bool hideVanilla = false;

        [XmlIgnore]
        // Last selected tree-or-prop state.
        internal static bool treeSelected = false;

        [XmlIgnore]
        // Remember last panel position.
        internal static bool rememberPosition = false;

        // What's new notification version.
        [XmlIgnore]
        internal static string whatsNewVersion = "0.0";

        // What's new beta notification version.
        [XmlIgnore]
        internal static int whatsNewBetaVersion = 0;

        // SavedInputKey reference for communicating with UUI.
        [XmlIgnore]
        private static readonly SavedInputKey uuiSavedKey = new SavedInputKey("BOB hotkey", "BOB hotkey", key: KeyCode.B, control: false, shift: false, alt: true, false);


        // Tree ruining.
        [XmlIgnore]
        internal static bool StopTreeRuining { get; set; } = false;

        // Prop ruining.
        [XmlIgnore]
        internal static bool StopPropRuining { get; set; } = false;


        // "What's new" notification last notified version.

        [XmlElement("WhatsNewVersion")]
		public string WhatsNewVersion { get => whatsNewVersion; set => whatsNewVersion = value; }


		// Language.
		[XmlElement("Language")]
		public string Language
		{
			get => Translations.Language;

			set => Translations.Language = value;
        }


        // Hotkey element.
        [XmlElement("PanelKey")]
        public KeyBinding PanelKey
        {
            get
            {
                return new KeyBinding
                {
                    keyCode = (int)PanelSavedKey.Key,
                    control = PanelSavedKey.Control,
                    shift = PanelSavedKey.Shift,
                    alt = PanelSavedKey.Alt
                };
            }
            set
            {
                uuiSavedKey.Key = (KeyCode)value.keyCode;
                uuiSavedKey.Control = value.control;
                uuiSavedKey.Shift = value.shift;
                uuiSavedKey.Alt = value.alt;
            }
        }


        /// <summary>
        /// Panel hotkey as ColossalFramework SavedInputKey.
        /// </summary>
        [XmlIgnore]
        internal static SavedInputKey PanelSavedKey => uuiSavedKey;


        /// <summary>
        /// The current hotkey settings as ColossalFramework InputKey.
        /// </summary>
        [XmlIgnore]
        internal static InputKey CurrentHotkey
        {
            get => uuiSavedKey.value;

            set => uuiSavedKey.value = value;
        }


		// Grouping behaviour.
		[XmlElement("GroupDefault")]
		public int GroupDefault { get => indDefault; set => indDefault = value; }

		// Remember position.
		[XmlElement("RememberPos")]
		public bool RememberPos { get => rememberPosition; set => rememberPosition = value; }

    }


    /// <summary>
    /// Basic keybinding class - code and modifiers.
    /// </summary>
    public class KeyBinding
    {
        [XmlAttribute("KeyCode")]
        public int keyCode;

        [XmlAttribute("Control")]
        public bool control;

        [XmlAttribute("Shift")]
        public bool shift;

        [XmlAttribute("Alt")]
        public bool alt;
    }
}