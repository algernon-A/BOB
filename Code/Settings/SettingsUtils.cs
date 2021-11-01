using System;
using System.IO;
using System.Xml.Serialization;


namespace BOB
{
	/// <summary>
	/// Static class for dealing with the settings file.
	/// </summary>
	internal static class SettingsUtils
	{
		internal static readonly string OldSettingsFileName = "TreePropReplacer-settings.xml";
        internal static readonly string NewSettingsFileName = "BOB-settings.xml";


        /// <summary>
        /// Load settings from XML file.
        /// </summary>
        internal static void LoadSettings()
        {
            try
            {
                // Attempt to read new settings file name; if that fails, try to read the old one.
                string fileName = NewSettingsFileName;
                if (!File.Exists(NewSettingsFileName))
                {
                    fileName = OldSettingsFileName;
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
        internal static void SaveSettings()
        {
            try
            {
                // Pretty straightforward.  Serialisation is within GBRSettingsFile class.
                using (StreamWriter writer = new StreamWriter(NewSettingsFileName))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(ModSettings));
                    xmlSerializer.Serialize(writer, new ModSettings());
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception saving XML settings file");
            }
        }
    }
}
