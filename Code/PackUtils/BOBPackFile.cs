// <copyright file="BOBPackFile.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// Replacement pack file class.
    /// </summary>
    [XmlType("BOBPackFile")]
    public struct BOBPackFile
    {
        /// <summary>
        /// The list of prop packs.
        /// </summary>
        [XmlArray("propPacks")]
        public List<PropPack> PropPacks;

        /// <summary>
        /// Prop replacement pack data structure.
        /// </summary>
        [XmlType("replacementpack")]
        public struct PropPack
        {
            /// <summary>
            /// Replacement pack name.
            /// </summary>
            [XmlAttribute("name")]
            [DefaultValue("")]
            public string Name;

            /// <summary>
            /// The list of replacements.
            /// </summary>
            [XmlArray("propReplacements")]
            public List<PropReplacement> PropReplacements;
        }

        /// <summary>
        /// Individual prop replacement data structure.
        /// </summary>
        [XmlType("replacement")]
        public struct PropReplacement
        {
            /// <summary>
            /// Whether this replacement is a tree (true) or prop (false).
            /// </summary>
            [XmlAttribute("isTree")]
            [DefaultValue(false)]
            public bool IsTree;

            /// <summary>
            /// Target prefab name.
            /// </summary>
            [XmlAttribute("target")]
            public string TargetName;

            /// <summary>
            /// Replacement prefab name.
            /// </summary>
            [XmlAttribute("replacement")]
            public string ReplacementName;

            /// <summary>
            /// Replacement prefab info.
            /// </summary>
            [XmlIgnore]
            public PrefabInfo ReplacementInfo;

            /// <summary>
            /// Replacement X position offset.
            /// </summary>
            [XmlAttribute("xOffset")]
            [DefaultValue(0)]
            public float Xoffset;

            /// <summary>
            /// Replacement Y position offset.
            /// </summary>
            [XmlAttribute("yOffset")]
            [DefaultValue(0)]
            public float Yoffset;

            /// <summary>
            /// Replacement Z position offset.
            /// </summary>
            [XmlAttribute("zOffset")]
            [DefaultValue(0)]
            public float Zoffset;

            /// <summary>
            /// Replacement rotation adjustment.
            /// </summary>
            [XmlAttribute("rotation")]
            [DefaultValue(0)]
            public float Rotation;

            /// <summary>
            /// Whether or not the replacement should be hidden.
            /// </summary>
            [XmlAttribute("hide")]
            [DefaultValue(false)]
            public bool Hide;
        }
    }
}