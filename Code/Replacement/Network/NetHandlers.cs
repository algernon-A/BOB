﻿// <copyright file="NetHandlers.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System.Collections.Generic;
    using AlgernonCommons;

    /// <summary>
    /// Central coordination and tracking of active network replacmenets.
    /// </summary>
    internal static class NetHandlers
    {
        // The master dicationary of all network props being handled.
        private static readonly Dictionary<NetInfo.Lane, Dictionary<int, LanePropHandler>> Handlers = new Dictionary<NetInfo.Lane, Dictionary<int, LanePropHandler>>();

        /// <summary>
        /// Gets any existing prop handler for the given parameters, creating a new one if one doesn't already exist.
        /// </summary>
        /// <param name="netInfo">Network prefab for this prop.</param>
        /// <param name="laneInfo">Netlane for this prop.</param>
        /// <param name="propIndex">NetLaneProp prop index.</param>
        /// <returns>Prop handler for this prop (creating a new handler if required).</returns>
        internal static LanePropHandler GetOrAddHandler(NetInfo netInfo, NetInfo.Lane laneInfo, int propIndex)
        {
            // Check for an existing lane entry.
            if (!Handlers.TryGetValue(laneInfo, out Dictionary<int, LanePropHandler> laneDict))
            {
                // No building entry exists; add one.
                laneDict = new Dictionary<int, LanePropHandler>();
                Handlers.Add(laneInfo, laneDict);
            }

            // Check for existing prop index reference.
            if (!laneDict.TryGetValue(propIndex, out LanePropHandler propHandler))
            {
                // No existing handler for this prop index - create a new one and add to the dictionary.
                propHandler = CreateHandler(netInfo, laneInfo, propIndex);
                laneDict.Add(propIndex, propHandler);
            }

            return propHandler;
        }

        /// <summary>
        /// Gets any existing prop handler for the given parameters, returning null if one doesn't already exist.
        /// </summary>
        /// <param name="laneInfo">Netlane for this prop.</param>
        /// <param name="propIndex">NetLaneProp prop index.</param>
        /// <returns>Prop handler (null if none).</returns>
        internal static LanePropHandler GetHandler(NetInfo.Lane laneInfo, int propIndex)
        {
            // Check for an existing lane entry.
            if (Handlers.TryGetValue(laneInfo, out Dictionary<int, LanePropHandler> laneDict))
            {
                // Entry found; check for and existing prop index reference.
                if (laneDict.TryGetValue(propIndex, out LanePropHandler propHandler))
                {
                    // Found a matching entry; return it.
                    return propHandler;
                }
            }

            // If we got here, no matching entry was found; return null.
            return null;
        }

        /// <summary>
        /// Returns the replacement of the given priority for the given parameters, if any.
        /// </summary>
        /// <param name="laneInfo">Netlane for this prop.</param>
        /// <param name="propIndex">NetLaneProp prop index.</param>
        /// <param name="priority">Replacement priority to return.</param>
        /// <returns>Specified network replacment entry (null if none).</returns>
        internal static BOBConfig.NetReplacement GetReplacement(NetInfo.Lane laneInfo, int propIndex, ReplacementPriority priority) => GetHandler(laneInfo, propIndex)?.GetReplacement(priority);

        /// <summary>
        /// Removes the given replacement from all existing handlers.
        /// Automatically updates any target props whose active replacements change as a result.
        /// </summary>
        /// <param name="replacement">Replacement to remove.</param>
        internal static void RemoveReplacement(BOBConfig.NetReplacement replacement)
        {
            // Iterate through all references in the dictionary.
            foreach (KeyValuePair<NetInfo.Lane, Dictionary<int, LanePropHandler>> laneEntry in Handlers)
            {
                foreach (KeyValuePair<int, LanePropHandler> entry in laneEntry.Value)
                {
                    // Clear any of this replacement contained in this reference.
                    entry.Value.ClearReplacement(replacement);
                }
            }
        }

        /// <summary>
        /// Creates a new BuildingPropHandler from the provided building prefab and prop index.
        /// </summary>
        /// <param name="netInfo">Network prefab for this prop.</param>
        /// <param name="laneInfo">Netlane for this prop.</param>
        /// <param name="propIndex">NetLaneProp prop index.</param>
        /// <returns>Newly-created reference (null if creation failed).</returns>
        internal static LanePropHandler CreateHandler(NetInfo netInfo, NetInfo.Lane laneInfo, int propIndex)
        {
            // Safety checks to ensure prop reference is valid.
            NetLaneProps.Prop[] props = laneInfo?.m_laneProps?.m_props;
            if (props != null && propIndex >= 0 && propIndex < props.Length)
            {
                // Check that actual prop value isn't null.
                NetLaneProps.Prop prop = props[propIndex];
                if (prop != null)
                {
                    // Create and return new reference, recording original values.
                    return new LanePropHandler(netInfo, laneInfo, propIndex, prop);
                }
            }

            // If we got here, something went wrong; return null.
            Logging.Error("invalid argument passed to LanePropHandler.CreateHandler");
            return null;
        }
    }
}
