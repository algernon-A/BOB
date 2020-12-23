using System.Collections.Generic;
using System.Xml.Serialization;


namespace BOB
{
	/// <summary>
	/// BOB configuration XML file format.
	/// </summary>
	[XmlRoot(ElementName = "BOBConfig", Namespace = "", IsNullable = false)]
	public class BOBConfigurationFile
	{
		[XmlAttribute("version")]
		public int version = 1;

		[XmlArray("buildprops", IsNullable = true)]
		[XmlArrayItem("buildprop", IsNullable = true)]
		public List<BOBBuildingReplacement> allBuildingProps { get; set; }

		[XmlArray("buildings", IsNullable = false)]
		[XmlArrayItem("building", IsNullable = false)]
		public List<BOBBuildingElement> buildings { get; set; }

		[XmlArray("individuals", IsNullable = false)]
		[XmlArrayItem("individual", IsNullable = false)]
		public List<BOBBuildingElement> indBuildings { get; set; }

		[XmlArray("netprops", IsNullable = true)]
		[XmlArrayItem("netprop", IsNullable = true)]
		public List<BOBNetReplacement> allNetworkProps { get; set; }

		[XmlArray("networks", IsNullable = false)]
		[XmlArrayItem("network", IsNullable = false)]
		public List<BOBNetworkElement> networks { get; set; }
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


	public class BOBNetworkElement
    {
		[XmlElement("network")]
		public string network = string.Empty;

		[XmlIgnore]
		public NetInfo netInfo;

		[XmlArray("replacements")]
		[XmlArrayItem("replacement")]
		public List<BOBNetReplacement> replacements;
	}

	public class BOBBuildingElement
	{
		[XmlElement("building")]
		public string building = string.Empty;

		[XmlIgnore]
		public BuildingInfo buildingInfo;

		[XmlArray("replacements")]
		[XmlArrayItem("replacement")]
		public List<BOBBuildingReplacement> replacements;
	}

	public class BOBBuildingReplacement : BOBNetReplacement
    {
		[XmlAttribute("index")]
		public int index = -1;

		[XmlIgnore]
		new public List<BuildingPropReference> references;
    }


	/// <summary>
	/// All-building network record XML format.
	/// </summary>
	public class BOBNetReplacement
	{
		[XmlAttribute("tree")]
		public bool tree = false;

		[XmlAttribute("target")]
		public string target = string.Empty;

		[XmlAttribute("replacement")]
		public string replacement = string.Empty;

		[XmlAttribute("angle")]
		public float angle = 0f;

		[XmlAttribute("offsetX")]
		public float offsetX = 0f;

		[XmlAttribute("offsetY")]
		public float offsetY = 0f;

		[XmlAttribute("offsetZ")]
		public float offsetZ = 0f;

		[XmlAttribute("probability")]
		public int probability = 100;

		[XmlIgnore]
		public PrefabInfo replacementInfo;

		[XmlIgnore]
		public PrefabInfo targetInfo;

		[XmlIgnore]
		public List<NetPropReference> references;
	}
}