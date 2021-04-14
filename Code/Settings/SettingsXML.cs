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
					keyCode = (int)UIThreading.HotKey,
					control = UIThreading.HotCtrl,
					shift = UIThreading.HotShift,
					alt = UIThreading.HotAlt
				};
			}
			set
			{
				UIThreading.HotKey = (KeyCode)value.keyCode;
				UIThreading.HotCtrl = value.control;
				UIThreading.HotShift = value.shift;
				UIThreading.HotAlt = value.alt;
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

		// Tree ruining.
		[XmlElement("StopTreeRuining")]
		public bool StopTreeRuining
		{
			get => ModSettings.StopTreeRuining;
			set => ModSettings.StopTreeRuining = value;
		}

		// Prop ruining.
		[XmlElement("StopPropRuining")]
		public bool StopPropRuining
		{
			get => ModSettings.StopPropRuining;
			set => ModSettings.StopPropRuining = value;
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
