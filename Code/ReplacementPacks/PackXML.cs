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
        [XmlAttribute("isTree")]
        [DefaultValue(false)]
        public bool isTree;

        [XmlAttribute("target")]
        public string targetName;

        [XmlAttribute("replacement")]
        public string replacementName;

        [XmlIgnore]
        public PrefabInfo replacementInfo;

        [XmlAttribute("xOffset")]
        [DefaultValue(0)]
        public float xOffset;

        [XmlAttribute("yOffset")]
        [DefaultValue(0)]
        public float yOffset;

        [XmlAttribute("zOffset")]
        [DefaultValue(0)]
        public float zOffset;

        [XmlAttribute("rotation")]
        [DefaultValue(0)]
        public float rotation;
    }
}