// <copyright file="BOBConfig.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// BOB configuration XML file format.
    /// </summary>
    [XmlRoot(ElementName = "BOBConfig")]
    public class BOBConfig
    {
        /// <summary>
        /// Gets or sets the file format version.
        /// </summary>
        [XmlAttribute("version")]
        public int Version { get; set; } = 1;

        /// <summary>
        /// Gets or sets the list of random props.
        /// </summary>
        [XmlArray("randomprops")]
        [XmlArrayItem("randomprop")]
        public List<BOBRandomPrefab> RandomProps { get; set; } = new List<BOBRandomPrefab>();

        /// <summary>
        /// Gets or sets the list of random trees.
        /// </summary>
        [XmlArray("randomtrees")]
        [XmlArrayItem("randomtree")]
        public List<BOBRandomPrefab> RandomTrees { get; set; } = new List<BOBRandomPrefab>();

        /// <summary>
        /// Gets or sets the list of prop scaling records.
        /// </summary>
        [XmlArray("propscaling")]
        [XmlArrayItem("propscale")]
        public List<BOBScalingElement> PropScales { get; set; } = new List<BOBScalingElement>();

        /// <summary>
        /// Gets or sets the list of tree scaling records.
        /// </summary>
        [XmlArray("treescaling")]
        [XmlArrayItem("treescale")]
        public List<BOBScalingElement> TreeScales { get; set; } = new List<BOBScalingElement>();

        /// <summary>
        /// Gets or sets the list of all-building prop replacements.
        /// </summary>
        [XmlArray("buildprops")]
        [XmlArrayItem("buildprop")]
        public List<BuildingReplacement> AllBuildingProps { get; set; } = new List<BuildingReplacement>();

        /// <summary>
        /// Gets or sets the list of grouped building prop replacements.
        /// </summary>
        [XmlArray("buildings")]
        [XmlArrayItem("building")]
        public List<BuildingElement> Buildings { get; set; } = new List<BuildingElement>();

        /// <summary>
        /// Gets or sets the list of individual building prop replacements.
        /// </summary>
        [XmlArray("individuals")]
        [XmlArrayItem("individual")]
        public List<BuildingElement> IndBuildings { get; set; } = new List<BuildingElement>();

        /// <summary>
        /// Gets or sets the list of all-network prop replacements.
        /// </summary>
        [XmlArray("netprops")]
        [XmlArrayItem("netprop")]
        public List<NetReplacement> AllNetworkProps { get; set; } = new List<NetReplacement>();

        /// <summary>
        /// Gets or sets the list of grouped network prop replacements.
        /// </summary>
        [XmlArray("networks")]
        [XmlArrayItem("network")]
        public List<NetworkElement> Networks { get; set; } = new List<NetworkElement>();

        /// <summary>
        /// Gets or sets the list of individual network prop replacements.
        /// </summary>
        [XmlArray("netind")]
        [XmlArrayItem("netind")]
        public List<NetworkElement> IndNetworks { get; set; } = new List<NetworkElement>();

        /// <summary>
        /// Gets or sets the list of active replacement packs.
        /// </summary>
        [XmlArray("activePacks")]
        [XmlArrayItem("activePacks")]
        public List<string> ActivePacks { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of added building props.
        /// </summary>
        [XmlArray("addedBuildingProps")]
        [XmlArrayItem("addedBuildingProp")]
        public List<BuildingElement> AddedBuildingProps { get; set; } = new List<BuildingElement>();

        /// <summary>
        /// Gets or sets the list of added network props.
        /// </summary>
        [XmlArray("addedNetworkProps")]
        [XmlArrayItem("addedNetworkProp")]
        public List<NetworkElement> AddedNetworkProps { get; set; } = new List<NetworkElement>();

        /// <summary>
        /// Building/network replacement element base class.
        /// </summary>
        public abstract class ElementBase
        {
            /// <summary>
            /// Gets or sets the element's prefab info.
            /// </summary>
            [XmlIgnore]
            public PrefabInfo Prefab { get; set; }
        }

        /// <summary>
        ///  Network element record - for recording per-network replacements.
        /// </summary>
        public class NetworkElement : ElementBase
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="NetworkElement"/> class.
            /// </summary>
            public NetworkElement()
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="NetworkElement"/> class.
            /// </summary>
            /// <param name="netInfo">Network prefab to record.</param>
            public NetworkElement(NetInfo netInfo)
            {
                Network = netInfo.name;
                Prefab = netInfo;
            }

            /// <summary>
            /// Gets or sets the network name.
            /// </summary>
            [XmlElement("network")]
            public string Network { get; set; } = string.Empty;

            /// <summary>
            /// Gets the prefab info as NetInfo.
            /// </summary>
            [XmlIgnore]
            public NetInfo NetInfo => Prefab as NetInfo;

            /// <summary>
            /// Gets or sets the list of replacements.
            /// </summary>
            [XmlArray("replacements")]
            [XmlArrayItem("replacement")]
            public List<NetReplacement> Replacements { get; set; } = new List<NetReplacement>();
        }

        /// <summary>
        /// Building element record - for recording per-building replacments.
        /// </summary>
        public class BuildingElement : ElementBase
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="BuildingElement"/> class.
            /// </summary>
            public BuildingElement()
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="BuildingElement"/> class.
            /// </summary>
            /// <param name="buildingInfo">Building prefab to record.</param>
            public BuildingElement(BuildingInfo buildingInfo)
            {
                Building = buildingInfo.name;
                Prefab = buildingInfo;
            }

            /// <summary>
            /// Gets or sets the building name.
            /// </summary>
            [XmlElement("building")]
            public string Building { get; set; } = string.Empty;

            /// <summary>
            /// Gets the prefab info as BuildingInfo.
            /// </summary>
            [XmlIgnore]
            public BuildingInfo BuildingInfo => Prefab as BuildingInfo;

            /// <summary>
            /// Gets or sets the list of replacements.
            /// </summary>
            [XmlArray("replacements")]
            [XmlArrayItem("replacement")]
            public List<BuildingReplacement> Replacements { get; set; } = new List<BuildingReplacement>();
        }

        /// <summary>
        /// Base replacement record XML format.
        /// </summary>
        public abstract class Replacement
        {
            // Replacement name.
            [XmlIgnore]
            private string _replacementName;

            /// <summary>
            /// Gets or sets a value indicating whether this replacement record is for a tree or a prop.
            /// </summary>
            [XmlAttribute("tree")]
            public bool IsTree { get; set; } = false;

            /// <summary>
            /// Gets or sets the target prefab name.
            /// </summary>
            [XmlAttribute("target")]
            public string Target { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets the replacement prefab name.
            /// </summary>
            [XmlAttribute("replacement")]
            public string ReplacementName
            {
                get => ReplacementInfo?.name ?? _replacementName ?? string.Empty;
                set => _replacementName = value;
            }

            /// <summary>
            /// Gets or sets the target prop index.
            /// </summary>
            [XmlAttribute("index")]
            public int PropIndex { get; set; } = -1;

            /// <summary>
            /// Gets or sets the angle (rotation) adjustment.
            /// </summary>
            [XmlAttribute("angle")]
            public float Angle { get; set; } = 0f;

            /// <summary>
            /// Gets or sets the X position offset.
            /// </summary>
            [XmlAttribute("offsetX")]
            public float OffsetX { get; set; } = 0f;

            /// <summary>
            /// Gets or sets the Y position offset.
            /// </summary>
            [XmlAttribute("offsetY")]
            public float OffsetY { get; set; } = 0f;

            /// <summary>
            /// Gets or sets the Z position offset.
            /// </summary>
            [XmlAttribute("offsetZ")]
            public float OffsetZ { get; set; } = 0f;

            /// <summary>
            /// Gets or sets the new probability.
            /// </summary>
            [XmlAttribute("probability")]
            public int Probability { get; set; } = 100;

            /// <summary>
            /// Gets or sets the parent prefab (building or network) info.
            /// </summary>
            [XmlIgnore]
            public PrefabInfo ParentInfo { get; set; }

            /// <summary>
            /// Gets or sets the target prefab info.
            /// </summary>
            [XmlIgnore]
            public PrefabInfo TargetInfo { get; set; }

            /// <summary>
            /// Gets or sets the replacement prefab info.
            /// </summary>
            [XmlIgnore]
            public PrefabInfo ReplacementInfo { get; set; }

            /// <summary>
            /// Gets the target info as TreeInfo.
            /// </summary>
            [XmlIgnore]
            public TreeInfo TargetTree => TargetInfo as TreeInfo;

            /// <summary>
            /// Gets the target prefab info as PropInfo.
            /// </summary>
            [XmlIgnore]
            public PropInfo TargetProp => TargetInfo as PropInfo;

            /// <summary>
            /// Gets the replacement prefab info as TreeInfo.
            /// </summary>
            [XmlIgnore]
            public TreeInfo ReplacementTree => ReplacementInfo as TreeInfo;

            /// <summary>
            /// Gets the replacement prefab info as PropInfo.
            /// </summary>
            [XmlIgnore]
            public PropInfo ReplacementProp => ReplacementInfo as PropInfo;
        }

        /// <summary>
        /// Building replacement record XML format.
        /// </summary>
        public class BuildingReplacement : Replacement
        {
            /// <summary>
            /// Gets or sets a value indicating whether this replacement has a custom height (Y-position offset).
            /// </summary>
            [XmlAttribute("customHeight")]
            public bool CustomHeight { get; set; } = true;

            /// <summary>
            /// Gets the parent prefab info as BuildingInfo.
            /// </summary>
            [XmlIgnore]
            public BuildingInfo BuildingInfo => ParentInfo as BuildingInfo;
        }

        /// <summary>
        /// Network replacement record XML format.
        /// </summary>
        public class NetReplacement : Replacement
        {
            /// <summary>
            /// Gets or sets the target lane index.
            /// </summary>
            [XmlAttribute("lane")]
            public int LaneIndex { get; set; } = -1;

            /// <summary>
            /// Gets or sets the replacement's repeat distance.
            /// </summary>
            [XmlAttribute("repeatDistance")]
            public float RepeatDistance { get; set; } = -1f;

            /// <summary>
            /// Gets the parent prefab info as NetInfo.
            /// </summary>
            [XmlIgnore]
            public NetInfo NetInfo => ParentInfo as NetInfo;
        }
    }
}