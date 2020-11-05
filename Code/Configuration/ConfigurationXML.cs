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
		public List<BOBAllBuildingElement> allBuildingTrees { get; set; }

		[XmlArray("props", IsNullable = true)]
		[XmlArrayItem("prop", IsNullable = true)]
		public List<BOBAllBuildingElement> allBuildingProps { get; set; }

		[XmlArray("buildings", IsNullable = false)]
		[XmlArrayItem("building", IsNullable = false)]
		public List<BOBBuildingElement> buildings { get; set; }

		[XmlArray("netprops", IsNullable = true)]
		[XmlArrayItem("netprop", IsNullable = true)]
		public List<BOBAllNetworkElement> allNetworkProps { get; set; }

		[XmlArray("networks", IsNullable = false)]
		[XmlArrayItem("network", IsNullable = false)]
		public List<BOBNetworkElement> networks { get; set; }
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
	/// Network replacment record XML format.
	/// </summary>
	public class BOBNetworkElement
	{
		[XmlAttribute("prefab")]
		public string prefab = string.Empty;

		[XmlArray("replacements")]
		[XmlArrayItem("replacement")]
		public List<NetReplacement> replacements;
	}


	/// <summary>
	/// All-building replacment record XML format.
	/// </summary>
	public class BOBAllBuildingElement
	{
		[XmlAttribute("target")]
		public string target = string.Empty;

		[XmlAttribute("replacement")]
		public string replacement = string.Empty;
	}


	/// <summary>
	/// All-building network record XML format.
	/// </summary>
	public class BOBAllNetworkElement
	{
		[XmlAttribute("target")]
		public string target = string.Empty;

		[XmlAttribute("replacement")]
		public string replacement = string.Empty;

		[XmlAttribute("angle")]
		public float angle = 0f;

		[XmlAttribute("OffsetX")]
		public float offsetX = 0f;

		[XmlAttribute("OffsetY")]
		public float offsetY = 0f;

		[XmlAttribute("OffsetZ")]
		public float offsetZ = 0f;

		[XmlIgnore]
		public PrefabInfo replacementInfo;

		[XmlIgnore]
		public PrefabInfo targetInfo;
	}
}