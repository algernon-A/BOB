// <copyright file="NetworkSkins.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB.Skins
{
    using System;
    using ColossalFramework;

    /// <summary>
    /// Network skinning class.  Based on boformer's Network Skins mod.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Performant fields")]
    internal class NetworkSkins : IDisposable
    {
        /// <summary>
        /// Array of active skins (null if none), directly mapped to segment ID.
        /// </summary>
        public static NetworkSkin[] SegmentSkins;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkSkins"/> class.
        /// </summary>
        internal NetworkSkins()
        {
            // Set instance.
            Instance = this;

            // Initialize array of skins to match game's segment array.
            SegmentSkins = new NetworkSkin[Singleton<NetManager>.instance.m_segments.m_buffer.Length];
        }

        /// <summary>
        /// Gets the active instance.
        /// </summary>
        internal static NetworkSkins Instance { get; private set; }

        /// <summary>
        /// Disposes of this instance and frees arrays.
        /// </summary>
        public void Dispose()
        {
            // Clear arrays.
            SegmentSkins = null;

            // Clear instance reference.
            Instance = null;
        }
    }
}
