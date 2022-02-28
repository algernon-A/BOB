using System;
using System.Linq;
using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.Packaging;
using ColossalFramework.PlatformServices;


namespace BOB
{
	/// <summary>
	/// Static class to manage lists of prefabs.
	/// </summary>
	internal static class PrefabLists
	{
		// Random tree and prop templates.
		internal static PropInfo RandomPropTemplate { get; private set; }
		internal static TreeInfo RandomTreeTemplate { get; private set; }


		// Lists of loaded trees and props.
		internal static PropInfo[] LoadedProps { get; private set; }
		internal static TreeInfo[] LoadedTrees { get; private set; }


		// Creator asset dictionary.
		private readonly static Dictionary<ulong, string> creators = new Dictionary<ulong, string>();


		/// <summary>
		/// Current list of random props.
		/// </summary>
		internal static List<BOBRandomPrefab> RandomProps => ConfigurationUtils.CurrentConfig.randomProps;


		/// <summary>
		/// Current list of random trees.
		/// </summary>
		internal static List<BOBRandomPrefab> RandomTrees => ConfigurationUtils.CurrentConfig.randomTrees;


		/// <summary>
		/// Builds the lists of loaded trees props.  Must be called before use.
		/// </summary>
		internal static void BuildLists()
		{
			// Initialise lists.
			List<PropInfo> props = new List<PropInfo>();
			List<TreeInfo> trees = new List<TreeInfo>();

			// Iterate through all loaded props.
			for (uint i = 0u; i < PrefabCollection<PropInfo>.LoadedCount(); ++i)
			{
				// Get prop and add to our list, if it isn't null.
				PropInfo prop = PrefabCollection<PropInfo>.GetLoaded(i);
				if (prop?.name != null)
				{
					props.Add(prop);

					// Try to find random prop template if it isn't already there.
					if (RandomPropTemplate == null)
                    {
						if (prop.name.EndsWith("BOBRandomPropTemplate_Data"))
                        {
							RandomPropTemplate = prop;
                        }
                    }
				}
			}

			// Iterate through all loaded trees.
			for (uint i = 0u; i < PrefabCollection<TreeInfo>.LoadedCount(); ++i)
			{
				// Get tree and add to our list, if it isn't null.
				TreeInfo tree = PrefabCollection<TreeInfo>.GetLoaded(i);
				if (tree?.name != null)
				{
					trees.Add(tree);

					// Try to find random tree template if it isn't already there.
					if (RandomTreeTemplate == null)
					{
						if (tree.name.EndsWith("BOBRandomTreeTemplate_Data"))
						{
							RandomTreeTemplate = tree;
						}
					}
				}
			}

			// Order lists by name.
			LoadedProps = props.OrderBy(prop => GetDisplayName(prop)).ToList().ToArray();
			LoadedTrees = trees.OrderBy(tree => GetDisplayName(tree)).ToList().ToArray();

			// Populate creators dictionary.
			GetCreators();
		}


		/// <summary>
		/// Sanitises a raw prefab name for display.
		/// Called by the settings panel fastlist.
		/// </summary>
		/// <param name="prefab">Original (raw) prefab</param>
		/// <returns>Cleaned display name</returns>
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

			// Otherwise, omit the package number, and trim off any trailing _Data.
			int index = prefab.name.IndexOf('.');
			return prefab.name.Substring(index + 1).Replace("_Data", "");
		}


		/// <summary>
		/// Sanitises a raw prefab name for display.
		/// Called by the settings panel fastlist.
		/// </summary>
		/// <param name="name">Original (raw) name</param>
		/// <returns>Cleaned display name</returns>
		internal static string GetDisplayName(string name)
		{
			// Otherwise, omit the package number, and trim off any trailing _Data.
			int index = name.IndexOf('.');
			return name.Substring(index + 1).Replace("_Data", "").TrimStart();
		}


		/// <summary>
		/// Gets the name of the creator of the given prefab.
		/// </summary>
		/// <param name="network">Prefab to check</param>
		/// <returns>Creator name (empty string if none)</returns>
		internal static string GetCreator(PrefabInfo prefab)
		{
			// See if we can parse network workshop number from network name (number before period).
			if (prefab?.name != null)
			{
				int period = prefab.name.IndexOf(".");
				if (period > 0)
				{
					// Attempt to parse substring before period.
					if (UInt64.TryParse(prefab.name.Substring(0, period), out ulong steamID))
					{
						// Check to see if we have an entry.
						if (creators.ContainsKey(steamID))
						{
							return creators[steamID];
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
					if (UInt64.TryParse(asset.package.packageName, out ulong steamID) && !asset.package.packageAuthor.IsNullOrWhiteSpace())
					{
						// Check to see if we already have a record for the steam ID.
						if (!creators.ContainsKey(steamID))
						{
							// No existing record - get package author name and add to dictionary.
							if (UInt64.TryParse(asset.package.packageAuthor.Substring("steamid:".Length), out ulong authorID))
							{
								creators.Add(steamID, new Friend(new UserID(authorID)).personaName);
							}
						}
					}
				}
			}
		}
	}
}
