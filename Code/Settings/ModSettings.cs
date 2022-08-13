namespace BOB
{
    using System;
    using System.IO;
    using System.Xml.Serialization;
    using AlgernonCommons;
    using AlgernonCommons.Keybinding;
    using AlgernonCommons.Translation;
    using AlgernonCommons.XML;
    using ColossalFramework;
    using UnityEngine;


    /// <summary>
    /// Global mod settings.
    /// </summary>
    /// 
    [XmlRoot("TreePropReplacer")]
	public class ModSettings : SettingsXMLBase
	{
        // BOB settings file name (old).
        [XmlIgnore]
        private static readonly string OldSettingsFileName = "TreePropReplacer-settings.xml";

        // BOB settings file name (current).
        [XmlIgnore]
        private static readonly string NewSettingsFileName = "BOB-settings.xml";

        // User settings directory.
        [XmlIgnore]
        private static readonly string UserSettingsDir = ColossalFramework.IO.DataLocation.localApplicationData;

        /// <summary>
        /// Gets the settings file name.
        /// </summary>
        [XmlIgnore]
        private static readonly string SettingsFileName = Path.Combine(ColossalFramework.IO.DataLocation.localApplicationData, NewSettingsFileName);

        /// <summary>
        /// UUI key.
        /// </summary>
        [XmlIgnore]
        private static readonly UnsavedInputKey UUIKey = new UnsavedInputKey(name: "BOB hotkey", keyCode: KeyCode.B, control: false, shift: false, alt: true);

        // Default behaviour of the show individual props setting.
        [XmlIgnore]
        internal static int indDefault;

        [XmlIgnore]
        // Last selected vanilla filter state.
        internal static bool hideVanilla = false;

        [XmlIgnore]
        // Last selected tree-or-prop state.
        internal static BOBPanelBase.PropTreeModes lastPropTreeMode = BOBPanelBase.PropTreeModes.Prop;

        [XmlIgnore]
        // Remember last panel position.
        internal static bool rememberPosition = false;
        
        // What's new notification version.
        [XmlIgnore]
        internal static string whatsNewVersion = "0.0";

        // What's new beta notification version.
        [XmlIgnore]
        internal static int whatsNewBetaVersion = 0;

        // UUI hotkey.
        [XmlIgnore]
        private static readonly UnsavedInputKey uuiKey = new UnsavedInputKey("BOB hotkey", keyCode: KeyCode.B, control: false, shift: false, alt: true);


        /// <summary>
        /// Disable tree ruining (true means ruining is disabled).
        /// </summary>
        [XmlIgnore]
        internal static bool StopTreeRuining { get; set; } = false;


        /// <summary>
        /// Disable prop ruining (true means ruining is disabled).
        /// </summary>
        [XmlIgnore]
        internal static bool StopPropRuining { get; set; } = false;


        /// <summary>
        /// Disable/enable vanilla tree network replacement tool (true means disable).
        /// </summary>
        [XmlIgnore]
        internal static bool DisableTreeTool
        {
            get => _disableTreeTool;

            set
            {
                _disableTreeTool = value;
                Patcher.Instance.DisableTreeTool(value);
            }
        }
        private static bool _disableTreeTool = false;


        /// <summary>
        /// Enable thinner electrical wires (true means wires are made thinner).
        /// </summary>
        [XmlIgnore]
        internal static bool ThinnerWires
        {
            get => _thinnerWires;

            set
            {
                // Don't do anything if no change.
                if (value != _thinnerWires)
                {
                    // Update reference.
                    _thinnerWires = value;

                    // Apply/revert thinner wires according to toggle state.
                    if (value)
                    {
                        ElectricalWires.Instance.ApplyThinnerWires();
                    }
                    else
                    {
                        ElectricalWires.Instance.RevertThinnerWires();
                    }
                }
            }
        }
        private static bool _thinnerWires = false;
        
        /// <summary>

        /// <summary>
        /// Gets or sets the tool hotkey.
        /// </summary>
        [XmlElement("PanelKey")]
        public Keybinding XMLToolKey
        {
            get => UUIKey.Keybinding;

            set => UUIKey.Keybinding = value;
        }


        // Hotkey element.
        [XmlElement("TreeToolKey")]
        public Keybinding TreeToolKey
        {
            get => HotkeyThreading.TreeDisableKey;

            set => HotkeyThreading.TreeDisableKey = value;
        }


        // Grouping behaviour.
        [XmlElement("GroupDefault")]
		public int XMLGroupDefault { get => indDefault; set => indDefault = value; }

		// Remember position.
		[XmlElement("RememberPos")]
		public bool XMLRememberPos { get => rememberPosition; set => rememberPosition = value; }

        // Disable tree ruining.
        [XmlElement("StopTreeRuining")]
        public bool XMLNoTreeRuining { get => StopTreeRuining; set => StopTreeRuining = value; }

        // Disable prop ruining.
        [XmlElement("StopPropRuining")]
        public bool XMLNoPropRuining { get => StopPropRuining; set => StopPropRuining = value; }

        // Thinner wires.
        [XmlElement("ThinnerWires")]
        public bool XMLThinnerWires { get => _thinnerWires; set => _thinnerWires = value; }

        // Disable vanilla tree tool network replacement.
        [XmlElement("DisableNetworkTreeTool")]
        public bool XMLDisableTreeTool { get => DisableTreeTool; set => DisableTreeTool = value; }

        /// <summary>
        /// Gets the current hotkey as a UUI UnsavedInputKey.
        /// </summary>
        [XmlIgnore]
        internal static UnsavedInputKey ToolKey => UUIKey;

        /// <summary>
        /// Load settings from XML file.
        /// </summary>
        internal static void Load()
        {
            try
            {
                // Attempt to read new settings file (in user settings directory).
                string fileName = Path.Combine(UserSettingsDir, NewSettingsFileName);
                if (!File.Exists(fileName))
                {
                    // No settings file in user directory; use application directory instead; if that fails, try to read the old one.
                    fileName = NewSettingsFileName;
                    if (!File.Exists(NewSettingsFileName))
                    {
                        fileName = OldSettingsFileName;
                    }
                }

                // Check to see if configuration file exists.
                if (File.Exists(fileName))
                {
                    // Read it.
                    using (StreamReader reader = new StreamReader(fileName))
                    {
                        XmlSerializer xmlSerializer = new XmlSerializer(typeof(ModSettings));
                        if (!(xmlSerializer.Deserialize(reader) is ModSettings settingsFile))
                        {
                            Logging.Error("couldn't deserialize settings file");
                        }
                    }
                }
                else
                {
                    Logging.Message("no settings file found");
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception reading XML settings file");
            }
        }


        /// <summary>
        /// Save settings to XML file.
        /// </summary>
        internal static void Save()
        {
            try
            {
                // Save into user local settings.
                using (StreamWriter writer = new StreamWriter(Path.Combine(UserSettingsDir, NewSettingsFileName)))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(ModSettings));
                    xmlSerializer.Serialize(writer, new ModSettings());
                }

                // Cleaning up after ourselves - delete any old config file in the application directory.
                if (File.Exists(NewSettingsFileName))
                {
                    File.Delete(NewSettingsFileName);
                }

                if (File.Exists(OldSettingsFileName))
                {
                    File.Delete(OldSettingsFileName);
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception saving XML settings file");
            }
        }
    }
}