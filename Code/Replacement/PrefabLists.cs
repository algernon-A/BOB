// <copyright file="PrefabLists.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System.Collections.Generic;
    using System.Linq;
    using ColossalFramework;
    using ColossalFramework.Packaging;
    using ColossalFramework.PlatformServices;

    /// <summary>
    /// Static class to manage lists of prefabs.
    /// </summary>
    internal static class PrefabLists
    {
        // Creator asset dictionary.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1311:Static readonly fields should begin with upper-case letter", Justification = "Private static readonly field")]
        private static readonly Dictionary<ulong, string> s_creators = new Dictionary<ulong, string>();

        /// <summary>
        /// Gets the list of loaded prop items.
        /// </summary>
        internal static LoadedPrefabItem[] LoadedPropItems { get; private set; }

        /// <summary>
        /// Gets the list of loaded tree items.
        /// </summary>
        internal static LoadedPrefabItem[] LoadedTreeItems { get; private set; }

        /// <summary>
        /// Gets the randomp prop template.
        /// </summary>
        internal static PropInfo RandomPropTemplate { get; private set; }

        /// <summary>
        /// Gets the random tree template.
        /// </summary>
        internal static TreeInfo RandomTreeTemplate { get; private set; }

        /// <summary>
        /// Gets the current list of random props.
        /// </summary>
        internal static List<BOBRandomPrefab> RandomProps => ConfigurationUtils.CurrentConfig.RandomProps;

        /// <summary>
        /// Gets the current list of random trees.
        /// </summary>
        internal static List<BOBRandomPrefab> RandomTrees => ConfigurationUtils.CurrentConfig.RandomTrees;

        /// <summary>
        /// Builds the lists of loaded trees and props.  Must be called before use.
        /// </summary>
        internal static void BuildLists()
        {
            // Initialise lists.
            List<LoadedPrefabItem> props = new List<LoadedPrefabItem>();
            List<LoadedPrefabItem> trees = new List<LoadedPrefabItem>();

            // Generate target prop list.
            for (uint i = 0u; i < PrefabCollection<PropInfo>.LoadedCount(); ++i)
            {
                // Get prop and add to our list, if it isn't null.
                PropInfo prop = PrefabCollection<PropInfo>.GetLoaded(i);
                if (prop?.name != null)
                {
                    // Try to find random prop template if it isn't already there.
                    if (RandomPropTemplate == null && prop.name.EndsWith("BOBRandomPropTemplate_Data"))
                    {
                        RandomPropTemplate = prop;
                    }
                    else
                    {
                        props.Add(new LoadedPrefabItem(prop));
                    }
                }
            }

            // Generate target tree list.
            for (uint i = 0u; i < PrefabCollection<TreeInfo>.LoadedCount(); ++i)
            {
                // Get tree and add to our list, if it isn't null.
                TreeInfo tree = PrefabCollection<TreeInfo>.GetLoaded(i);
                if (tree?.name != null)
                {
                    // Try to find random tree template if it isn't already there.
                    if (RandomTreeTemplate == null && tree.name.EndsWith("BOBRandomTreeTemplate_Data"))
                    {
                            RandomTreeTemplate = tree;
                    }
                    else
                    {
                        trees.Add(new LoadedPrefabItem(tree));
                    }
                }
            }

            // Order alphabetically.
            LoadedPropItems = props.OrderBy(prop => prop.DisplayName).ToArray();
            LoadedTreeItems = trees.OrderBy(tree => tree.DisplayName).ToArray();

            // Populate creators dictionary.
            GetCreators();
        }

        /// <summary>
        /// Sanitises a raw prefab name for display.
        /// Called by the settings panel fastlist.
        /// </summary>
        /// <param name="prefab">Original (raw) prefab.</param>
        /// <returns>Cleaned display name.</returns>
        internal static string GetDisplayName(PrefabInfo prefab)
        {
            // Null check.
            if (prefab?.name == null)
            {
                return "null";
            }

            // If not custom content, return full name preceeded by vanilla flag.
            if (!prefab.m_isCustomContent)
            {
                return "[v] " + prefab.name;
            }

            // Otherwise, return the sanitised display name.
            return GetDisplayName(prefab.name);
        }

        /// <summary>
        /// Sanitises a raw prefab name for display.
        /// Called by the settings panel fastlist.
        /// </summary>
        /// <param name="name">Original (raw) name.</param>
        /// <returns>Cleaned display name.</returns>
        internal static string GetDisplayName(string name)
        {
            // Otherwise, omit the package number, and trim off any trailing _Data.
            int index = name.IndexOf('.');
            return name.Substring(index + 1).Replace("_Data", string.Empty).TrimStart();
        }

        /// <summary>
        /// Gets the name of the creator of the given prefab.
        /// </summary>
        /// <param name="prefab">Prefab to check.</param>
        /// <returns>Creator name (empty string if none).</returns>
        internal static string GetCreator(PrefabInfo prefab)
        {
            // See if we can parse network workshop number from network name (number before period).
            if (prefab?.name != null)
            {
                int period = prefab.name.IndexOf(".");
                if (period > 0)
                {
                    // Attempt to parse substring before period.
                    if (ulong.TryParse(prefab.name.Substring(0, period), out ulong steamID))
                    {
                        // Check to see if we have an entry.
                        if (s_creators.ContainsKey(steamID))
                        {
                            return s_creators[steamID];
                        }
                    }
                }
            }

            // If we got here, we didn't find a valid creator.
            return string.Empty;
        }

        /// <summary>
        /// Populates the creators dictionary.
        /// </summary>
        private static void GetCreators()
        {
            // Iterate through all loaded packages.
            foreach (Package.Asset asset in PackageManager.FilterAssets(new Package.AssetType[] { UserAssetType.CustomAssetMetaData }))
            {
                if (asset?.package != null)
                {
                    // Try to get steam ID of this package.
                    if (ulong.TryParse(asset.package.packageName, out ulong steamID) && !asset.package.packageAuthor.IsNullOrWhiteSpace())
                    {
                        // Check to see if we already have a record for the steam ID.
                        if (!s_creators.ContainsKey(steamID))
                        {
                            // No existing record - get package author name and add to dictionary.
                            if (ulong.TryParse(asset.package.packageAuthor.Substring("steamid:".Length), out ulong authorID))
                            {
                                s_creators.Add(steamID, new Friend(new UserID(authorID)).personaName);
                            }
                        }
                    }
                }
            }
        }
    }
}
