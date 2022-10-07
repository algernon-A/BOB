// <copyright file="BobScalingElement.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System.Xml.Serialization;
    using UnityEngine;

    /// <summary>
    /// Tree or prop scaling record XML format.
    /// </summary>
    public struct BOBScalingElement
    {
        /// <summary>
        /// Minimum permitted scale factor.
        /// </summary>
        public const float MinimumScale = 0.5f;

        /// <summary>
        /// Maximum permitted scale factor.
        /// </summary>
        public const float MaximumScale = 2.0f;

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

        /// <summary>
        /// Sets the minimum scale, including a minimum bounds check.
        /// </summary>
        /// <param name="scale">Minimum scaling factor to set.</param>
        public void SetMinScale(float scale)
        {
            MinScale = Mathf.Clamp(scale, MinimumScale, MaximumScale);
        }

        /// <summary>
        /// Sets the maximum scale, including a minimum bounds check.
        /// </summary>
        /// <param name="scale">Maximum scaling factor to set.</param>
        public void SetMaxScale(float scale)
        {
            MaxScale = Mathf.Clamp(scale, MinimumScale, MaximumScale);
        }

        /// <summary>
        /// Ensures that scaling values are within acceptible bounds.
        /// </summary>
        public void EnsureBounds()
        {
            SetMinScale(MinScale);
            SetMaxScale(MaxScale);
        }
    }
}