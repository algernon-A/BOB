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


	/// <summary>
	/// Building/network element base class.
	/// </summary>
	public abstract class BOBElementBase
    {
		[XmlIgnore]
		public PrefabInfo prefab;
    }


	/// <summary>
	///  Network element record - for recording per-network replacements.
	/// </summary>
	public class BOBNetworkElement : BOBElementBase
    {
		[XmlElement("network")]
		public string network = string.Empty;

		[XmlIgnore]
		public NetInfo NetInfo => prefab as NetInfo;

		[XmlArray("replacements")]
		[XmlArrayItem("replacement")]
		public readonly List<BOBNetReplacement> replacements;


		/// <summary>
		/// Constructor - default.
		/// </summary>
		public BOBNetworkElement()
		{
			replacements = new List<BOBNetReplacement>();
		}


		/// <summary>
		/// Constructor - provided net prefab.
		/// </summary>
		/// <param name="netInfo">Network prefab to record</param>
		public BOBNetworkElement(NetInfo netInfo)
		{
			network = netInfo.name;
			prefab = netInfo;
			replacements = new List<BOBNetReplacement>();
		}
	}


	/// <summary>
	/// Building element record - for recording per-building replacments.
	/// </summary>
	public class BOBBuildingElement : BOBElementBase
	{
		[XmlElement("building")]
		public string building = string.Empty;

		[XmlIgnore]
		public BuildingInfo BuildingInfo => prefab as BuildingInfo;

		[XmlArray("replacements")]
		[XmlArrayItem("replacement")]
		public readonly List<BOBBuildingReplacement> replacements;


		/// <summary>
		/// Constructor - default.
		/// </summary>
		public BOBBuildingElement()
		{
			replacements = new List<BOBBuildingReplacement>();
		}


		/// <summary>
		/// Constructor - provided building prefab.
		/// </summary>
		/// <param name="buildingInfo">Building prefab to record</param>
		public BOBBuildingElement(BuildingInfo buildingInfo)
		{
			building = buildingInfo.name;
			prefab = buildingInfo;
			replacements = new List<BOBBuildingReplacement>();
		}
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
		public PrefabInfo parentInfo;

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
		[XmlAttribute("customHeight")]
		public bool customHeight = true;

		[XmlIgnore]
		public BuildingInfo BuildingInfo => parentInfo as BuildingInfo;

		[XmlIgnore]
		public List<BuildingPropReference> references = new List<BuildingPropReference>();
    }


	/// <summary>
	/// Network replacement record XML format.
	/// </summary>
	public class BOBNetReplacement : BOBReplacementBase
	{
		[XmlIgnore]
		public NetInfo NetInfo => parentInfo as NetInfo;

		[XmlAttribute("lane")]
		public int laneIndex = -1;

		[XmlAttribute("repeatDistance")]
		public float repeatDistance = -1f;

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