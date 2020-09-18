using System.Collections.Generic;
using System.Xml.Serialization;


namespace BOB
{
    /// <summary>
    /// XML language class - from BloodyPenguin's translation framework.
    /// </summary>
    [XmlRoot(ElementName = "Language", Namespace = "", IsNullable = false)]
    public class Language
    {
        // Dictionary of translations for this language.
        [XmlIgnore]
        public Dictionary<string, string> translationDictionary = new Dictionary<string, string>();

        // Language unique name.
        [XmlAttribute("UniqueName")]
        public string uniqueName = string.Empty;

        // Language human-readable name.
        [XmlAttribute("ReadableName")]
        public string readableName = string.Empty;

        // Translation array.
        [XmlArray("Translations", IsNullable = false)]
        [XmlArrayItem("Translation", IsNullable = false)]
        public Translation[] translations
        {
            get
            {
                // Return array - same size as our dictionary.
                Translation[] newTranslations = new Translation[translationDictionary.Count];

                // Iterate through each conversion in the dictionary, adding the equivalent entry to our return array.
                int index = 0;
                foreach (KeyValuePair<string, string> fileTranslation in translationDictionary)
                {
                    newTranslations[index++] = new Translation() { key = fileTranslation.Key, value = fileTranslation.Value };
                }

                return translations;
            }

            set
            {
                // Iterate through each conversion from the file and add it to our dictionary.
                foreach (Translation translation in value)
                {
                    translationDictionary[translation.key] = translation.value;
                }
            }
        }
    }


    /// <summary>
    /// Class for an individual translation  - from BloodyPenguin's translation framework.
    /// </summary>
    public class Translation
    {
        [XmlAttribute("ID")]
        public string key = "";

        [XmlAttribute("String")]
        public string value = "";
    }
}