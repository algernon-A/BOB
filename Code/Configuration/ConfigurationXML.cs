using System.Collections.Generic;
using System.Xml.Serialization;


namespace BOB
{
	/// <summary>
	/// BOB configuration XML file format.
	/// </summary>
	[XmlRoot(ElementName = "TreePropReplacer", Namespace = "", IsNullable = false)]
	public class BOBConfigurationFile
	{
		[XmlAttribute("version")]
		public int version = 0;

		[XmlArray("trees", IsNullable = true)]
		[XmlArrayItem("tree", IsNullable = true)]
		public List<BOBGlobalElement> globalTrees { get; set; }

		[XmlArray("props", IsNullable = true)]
		[XmlArrayItem("prop", IsNullable = true)]
		public List<BOBGlobalElement> globalProps { get; set; }

		[XmlArray("buildings", IsNullable = false)]
		[XmlArrayItem("building", IsNullable = false)]
		public List<BOBBuildingElement> buildings { get; set; }
	}


	/// <summary>
	/// Building replacment record XML format.
	/// </summary>
	public class BOBBuildingElement
	{
		[XmlAttribute("prefab")]
		public string prefab = string.Empty;

		[XmlArray("replacements")]
		[XmlArrayItem("replacement")]
		public List<Replacement> replacements;
	}


	/// <summary>
	/// Global replacment record XML format.
	/// </summary>
	public class BOBGlobalElement
	{
		[XmlAttribute("target")]
		public string target = string.Empty;

		[XmlAttribute("replacement")]
		public string replacement = string.Empty;
	}
}