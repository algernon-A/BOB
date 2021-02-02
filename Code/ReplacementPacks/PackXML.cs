using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using System.Xml.Serialization;


namespace BOB
{
    /// <summary>
    /// Configuration file class.
    /// </summary>
    [XmlType("BOBPackFile")]
    public class BOBPackFile
    {
        public List<PropPack> propPacks;
    }


    /// <summary>
    /// Prop replacement pack data structure.
    /// </summary>
    [XmlType("replacementpack")]
    public class PropPack
    {
        [XmlAttribute("name")]
        [DefaultValue("")]
        public string name;

        public List<PropReplacement> propReplacements;
    }


    /// <summary>
    /// Individual prop replacement data structure.
    /// </summary>
    [XmlType("replacement")]
    public struct PropReplacement
    {
        [XmlAttribute("target")]
        [DefaultValue("")]
        public string targetName;

        [XmlAttribute("replacement")]
        [DefaultValue("")]
        public string replacementName;

        [XmlIgnore]
        public PrefabInfo replacementInfo;

        [XmlAttribute("rotation")]
        [DefaultValue(0)]
        public float rotation;
    }
}