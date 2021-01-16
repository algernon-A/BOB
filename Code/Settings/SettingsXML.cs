using System.Xml.Serialization;
using UnityEngine;


namespace BOB
{
	[XmlRoot("TreePropReplacer")]
	public class BOBSettingsFile
	{
		[XmlElement("WhatsNewVersion")]
		public string WhatsNewVersion { get => ModSettings.whatsNewVersion; set => ModSettings.whatsNewVersion = value; }

		// Language.
		[XmlElement("Language")]
		public string language
		{
			get => Translations.Language;
			
			set => Translations.Language = value;
		}

		// New building details panel hotkey element.
		[XmlElement("PanelKey")]
		public KeyBinding PanelKey
		{
			get
			{
				return new KeyBinding
				{
					keyCode = (int)UIThreading.hotKey,
					control = UIThreading.hotCtrl,
					shift = UIThreading.hotShift,
					alt = UIThreading.hotAlt
				};
			}
			set
			{
				UIThreading.hotKey = (KeyCode)value.keyCode;
				UIThreading.hotCtrl = value.control;
				UIThreading.hotShift = value.shift;
				UIThreading.hotAlt = value.alt;
			}
		}

		// Grouping behaviour.
		[XmlElement("GroupDefault")]
		public int GroupDefault
		{
			get => ModSettings.indDefault;
			set => ModSettings.indDefault = value;
		}

		// Remember position.
		[XmlElement("RememberPos")]
		public bool RememberPos
        {
			get => ModSettings.rememberPosition;
			set => ModSettings.rememberPosition = value;
        }
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
