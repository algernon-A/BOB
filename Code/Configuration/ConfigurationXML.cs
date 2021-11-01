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
		public List<BOBRandomPrefab> randomProps = new List<BOBRandomPrefab>();

		[XmlArray("randomtrees")]
		[XmlArrayItem("randomtree")]
		public List<BOBRandomPrefab> randomTrees = new List<BOBRandomPrefab>();

		[XmlArray("propscaling")]
		[XmlArrayItem("propscale")]
		public List<BOBScalingElement> propScales = new List<BOBScalingElement>();

		[XmlArray("treescaling")]
		[XmlArrayItem("treescale")]
		public List<BOBScalingElement> treeScales = new List<BOBScalingElement>();

		[XmlArray("buildprops")]
		[XmlArrayItem("buildprop")]
		public List<BOBBuildingReplacement> allBuildingProps = new List<BOBBuildingReplacement>();

		[XmlArray("buildings")]
		[XmlArrayItem("building")]
		public List<BOBBuildingElement> buildings = new List<BOBBuildingElement>();

		[XmlArray("individuals")]
		[XmlArrayItem("individual")]
		public List<BOBBuildingElement> indBuildings = new List<BOBBuildingElement>();

		[XmlArray("netprops")]
		[XmlArrayItem("netprop")]
		public List<BOBNetReplacement> allNetworkProps = new List<BOBNetReplacement>();

		[XmlArray("networks")]
		[XmlArrayItem("network")]
		public List<BOBNetworkElement> networks = new List<BOBNetworkElement>();

		[XmlArray("netind")]
		[XmlArrayItem("netind")]
		public List<BOBNetworkElement> indNetworks = new List<BOBNetworkElement>();

		[XmlArray("activePacks")]
		[XmlArrayItem("activePacks")]
		public List<string> activePacks = new List<string>();
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


	/// <summary>
	/// Base replacement record XML format.
	/// </summary>
	public class BOBReplacementBase
	{
		[XmlAttribute("tree")]
		public bool isTree = false;

		[XmlAttribute("target")]
		public string target = string.Empty;

		[XmlAttribute("replacement")]
		public string Replacement
		{
			get => replacementInfo?.name ?? replacementName ?? string.Empty;
			set => replacementName = value;
		}

		[XmlAttribute("index")]
		public int propIndex = -1;

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
		public TreeInfo TargetTree => targetInfo as TreeInfo;

		[XmlIgnore]
		public PropInfo TargetProp => targetInfo as PropInfo;

		[XmlIgnore]
		public TreeInfo ReplacementTree => replacementInfo as TreeInfo;

		[XmlIgnore]
		public PropInfo ReplacementProp => replacementInfo as PropInfo;
	}


	/// <summary>
	/// Building replacement record XML format.
	/// </summary>
	public class BOBBuildingReplacement : BOBReplacementBase
	{
		[XmlIgnore]
		public BuildingInfo buildingInfo;

		[XmlIgnore]
		public List<BuildingPropReference> references = new List<BuildingPropReference>();
    }


	/// <summary>
	/// Network replacement record XML format.
	/// </summary>
	public class BOBNetReplacement : BOBReplacementBase
	{
		[XmlIgnore]
		public NetInfo netInfo;

		[XmlAttribute("lane")]
		public int laneIndex = -1;

		[XmlIgnore]
		public List<NetPropReference> references = new List<NetPropReference>();
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
		public List<BOBVariation> variations = new List<BOBVariation>();

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