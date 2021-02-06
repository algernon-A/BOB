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

		[XmlArray("buildprops")]
		[XmlArrayItem("buildprop")]
		public List<BOBBuildingReplacement> allBuildingProps;

		[XmlArray("buildings")]
		[XmlArrayItem("building")]
		public List<BOBBuildingElement> buildings;

		[XmlArray("individuals")]
		[XmlArrayItem("individual")]
		public List<BOBBuildingElement> indBuildings;

		[XmlArray("netprops")]
		[XmlArrayItem("netprop")]
		public List<BOBNetReplacement> allNetworkProps;

		[XmlArray("networks")]
		[XmlArrayItem("network")]
		public List<BOBNetworkElement> networks;

		[XmlArray("activePacks")]
		[XmlArrayItem("activePacks")]
		public List<string> activePacks;
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