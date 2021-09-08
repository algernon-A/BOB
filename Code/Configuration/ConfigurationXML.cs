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

		[XmlArray("randomprops")]
		[XmlArrayItem("randomprop")]
		public List<BOBRandomPrefab> randomProps;

		[XmlArray("randomtrees")]
		[XmlArrayItem("randomtree")]
		public List<BOBRandomPrefab> randomTrees;

		[XmlArray("propscaling")]
		[XmlArrayItem("propscale")]
		public List<BOBScalingElement> propScales;

		[XmlArray("treescaling")]
		[XmlArrayItem("treescale")]
		public List<BOBScalingElement> treeScales;

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
	/// Tree or prop scaling record XML format.
	/// </summary>
	public class BOBScalingElement
    {
		[XmlAttribute("prefab")]
		public string prefabName = string.Empty;

		[XmlAttribute("min")]
		public float minScale = 1f;

		[XmlAttribute("max")]
		public float maxScale = 1f;

		[XmlIgnore]
		public PrefabInfo prefab;

		[XmlIgnore]
		public float originalMin = 1f;

		[XmlIgnore]
		public float originalMax = 1f;
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
		public string Replacement
		{
			get => replacementInfo?.name ?? replacementName ?? string.Empty;
			set => replacementName = value;
		}

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
		private string replacementName;

		[XmlIgnore]
		public PrefabInfo replacementInfo;

		[XmlIgnore]
		public PrefabInfo targetInfo;

		[XmlIgnore]
		public List<NetPropReference> references;
	}


	/// <summary>
	/// Random prefab XML record.
	/// </summary>
	public class BOBRandomPrefab
    {
		[XmlAttribute("name")]
		public string name = string.Empty;

		[XmlArray("variations")]
		[XmlArrayItem("variation")]
		public List<BOBVariation> variations;

		[XmlIgnore]
		public PropInfo prop;

		[XmlIgnore]
		public TreeInfo tree;

		[XmlIgnore]
		public bool missingVariant = false;
	}


	/// <summary>
	/// Random prefab variation XML record.
	/// </summary>
	public class BOBVariation
    {
		[XmlAttribute("name")]
		public string name;

		[XmlIgnore]
		public string DisplayName => prefab == null ? PrefabLists.GetDisplayName(name) : PrefabLists.GetDisplayName(prefab);

		[XmlAttribute("probability")]
		public int probability;

		[XmlIgnore]
		public PrefabInfo prefab;

		[XmlIgnore]
		public bool probLocked;
	}
}