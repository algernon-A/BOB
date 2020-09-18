using System.Xml.Serialization;
using UnityEngine;


namespace BOB
{
	[XmlRoot("TreePropReplacer")]
	public class BOBSettingsFile
	{
		// Language.
		[XmlElement("Language")]
		public string language
		{
			get
			{
				return Translations.Language;
			}
			set
			{
				Translations.Language = value;
			}
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


		// Group 
		[XmlElement("GroupDefault")]
		public int GroupDefault
		{
			get
			{
				return ModSettings.groupDefault;
			}
			set
			{
				ModSettings.groupDefault = value;
			}
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
