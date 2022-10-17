// <copyright file="BOBRandomPrefab.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using UnityEngine;

    /// <summary>
    /// Random prefab XML record.
    /// </summary>
    public class BOBRandomPrefab
    {
        /// <summary>
        /// Gets or sets the base name of this random prefab.
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of variations.
        /// </summary>
        [XmlArray("variations")]
        [XmlArrayItem("variation")]
        public List<Variation> Variations { get; set; } = new List<Variation>();

        /// <summary>
        /// Gets or sets the prop prefab info for this random prefab.
        /// </summary>
        [XmlIgnore]
        public PropInfo Prop { get; set; }

        /// <summary>
        /// Gets or sets the tree prefab info for this random prefab.
        /// </summary>
        [XmlIgnore]
        public TreeInfo Tree { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this random prefab has a missing variant.
        /// </summary>
        [XmlIgnore]
        public bool MissingVariant { get; set; } = false;

        /// <summary>
        /// Random prefab variation XML record.
        /// </summary>
        public class Variation
        {
            [XmlIgnore]
            private int _probability;

            /// <summary>
            /// Gets or sets the variation's name.
            /// </summary>
            [XmlAttribute("name")]
            public string Name { get; set; }

            /// <summary>
            /// Gets the variation's display name.
            /// </summary>
            [XmlIgnore]
            public string DisplayName => Prefab == null ? PrefabLists.GetDisplayName(Name) : PrefabLists.GetDisplayName(Prefab);

            /// <summary>
            /// Gets or sets the variation probability.
            /// </summary>
            [XmlAttribute("probability")]
            public int Probability { get => _probability; set => _probability = Mathf.Max(1, value); }

            /// <summary>
            /// Gets or sets the variation prefab info.
            /// </summary>
            [XmlIgnore]
            public PrefabInfo Prefab { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether or not this variation is locked.
            /// </summary>
            [XmlIgnore]
            public bool ProbLocked { get; set; }
        }
    }
}