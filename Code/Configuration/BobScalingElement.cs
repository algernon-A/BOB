// <copyright file="BobScalingElement.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System.Xml.Serialization;

    /// <summary>
    /// Tree or prop scaling record XML format.
    /// </summary>
    public struct BOBScalingElement
    {
        /// <summary>
        /// Target prefab name.
        /// </summary>
        [XmlAttribute("prefab")]
        public string PrefabName;

        /// <summary>
        /// Minimum scae.
        /// </summary>
        [XmlAttribute("Minimum scale")]
        public float MinScale;

        /// <summary>
        /// Maximum scale.
        /// </summary>
        [XmlAttribute("Maximum scale")]
        public float MaxScale;

        /// <summary>
        /// Prefab info.
        /// </summary>
        [XmlIgnore]
        public PrefabInfo Prefab;

        /// <summary>
        /// Original minimum scale.
        /// </summary>
        [XmlIgnore]
        public float OriginalMin;

        /// <summary>
        /// Original maximum scale.
        /// </summary>
        [XmlIgnore]
        public float OriginalMax;
    }
}