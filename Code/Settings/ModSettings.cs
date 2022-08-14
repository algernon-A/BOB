// <copyright file="ModSettings.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System;
    using System.IO;
    using System.Xml.Serialization;
    using AlgernonCommons;
    using AlgernonCommons.Keybinding;
    using AlgernonCommons.XML;
    using UnityEngine;

    /// <summary>
    /// Global mod settings.
    /// </summary>
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
        /// UUI key.
        /// </summary>
        [XmlIgnore]
        private static readonly UnsavedInputKey UUIKey = new UnsavedInputKey(name: "BOB hotkey", keyCode: KeyCode.B, control: false, shift: false, alt: true);

        // Private functional flags.
        private static bool _disableTreeTool = false;
        private static bool _thinnerWires = false;

        /// <summary>
        /// Gets or sets the tool hotkey.
        /// </summary>
        [XmlElement("PanelKey")]
        public Keybinding XMLToolKey
        {
            get => UUIKey.Keybinding;

            set => UUIKey.Keybinding = value;
        }

        /// <summary>
        /// Gets or sets the hotkey to toggle suppression of the vanilla network tree replacement tool.
        /// </summary>
        [XmlElement("TreeToolKey")]
        public Keybinding TreeToolKey
        {
            get => HotkeyThreading.TreeDisableKey;

            set => HotkeyThreading.TreeDisableKey = value;
        }

        /// <summary>
        /// Gets or sets the default grouping behaviour.
        /// </summary>
        [XmlElement("GroupDefault")]
        public int XMLGroupDefault { get => IndividualDefault; set => IndividualDefault = value; }

        /// <summary>
        /// Gets or sets a value indicating whether the last panel position should be rememembered.
        /// </summary>
        [XmlIgnore]
        [XmlElement("RememberPos")]
        public bool XMLRememberPos { get => RememberPosition; set => RememberPosition = value; }

        /// <summary>
        /// Gets or sets a value indicating whether tree ruining is disabled (true means ruining is disabled).
        /// </summary>
        [XmlElement("StopTreeRuining")]
        public bool XMLNoTreeRuining { get => StopTreeRuining; set => StopTreeRuining = value; }

        /// <summary>
        /// Gets or sets a value indicating whether prop ruining is disabled (true means ruining is disabled).
        /// </summary>
        [XmlElement("StopPropRuining")]
        public bool XMLNoPropRuining { get => StopPropRuining; set => StopPropRuining = value; }

        /// <summary>
        /// Gets or sets a value indicating whether thinner electrical wires are enabled (true means wires are made thinner).
        /// </summary>
        [XmlElement("ThinnerWires")]
        public bool XMLThinnerWires { get => _thinnerWires; set => _thinnerWires = value; }

        /// <summary>
        /// Gets or sets a value indicating whether the vanilla tree network replacement tool is disabled (true means disable).
        /// </summary>
        [XmlElement("DisableNetworkTreeTool")]
        public bool XMLDisableTreeTool { get => DisableTreeTool; set => DisableTreeTool = value; }

        /// <summary>
        /// Gets or sets the default grouping behaviour.
        /// </summary>
        [XmlIgnore]
        internal static int IndividualDefault { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the last selected vanilla filter state was active.
        /// </summary>
        [XmlIgnore]
        internal static bool HideVanilla { get; set; } = false;

        /// <summary>
        /// Gets or sets the last selected tree-or-prop state.
        /// </summary>
        [XmlIgnore]
        internal static BOBPanelBase.PropTreeModes LastPropTreeMode { get; set; } = BOBPanelBase.PropTreeModes.Prop;

        /// <summary>
        /// Gets or sets a value indicating whether the last panel position should be rememembered.
        /// </summary>
        [XmlIgnore]
        internal static bool RememberPosition { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether tree ruining is disabled (true means ruining is disabled).
        /// </summary>
        [XmlIgnore]
        internal static bool StopTreeRuining { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether prop ruining is disabled (true means ruining is disabled).
        /// </summary>
        [XmlIgnore]
        internal static bool StopPropRuining { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the vanilla tree network replacement tool is disabled (true means disable).
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

        /// <summary>
        /// Gets or sets a value indicating whether thinner electrical wires are enabled (true means wires are made thinner).
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