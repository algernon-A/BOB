using System.Collections.Generic;
using System.Linq;


namespace BOB
{
	/// <summary>
	/// Static class to manage lists of prefabs.
	/// </summary>
	internal static class PrefabLists
	{
		// Lists of loaded trees and props.
		internal static PropInfo[] loadedProps;
		internal static TreeInfo[] loadedTrees;

		/// <summary>
		/// Builds the lists of loaded trees props.  Must be called before use.
		/// </summary>
		internal static void BuildLists()
		{
			Logging.Message("building prop list with ", PrefabCollection<PropInfo>.LoadedCount().ToString(), " loaded prefabs");

			// Initialise lists.
			List<PropInfo> props = new List<PropInfo>();
			List<TreeInfo> trees = new List<TreeInfo>();

			// Iterate through all loaded prop prefabs.
			for (uint i = 0u; i < PrefabCollection<PropInfo>.LoadedCount(); ++i)
			{
				// Get prop and add to our list, if it isn't null.
				PropInfo prop = PrefabCollection<PropInfo>.GetLoaded(i);
				if (prop?.name != null)
				{
					props.Add(prop);
				}
			}

			Logging.Message("building tree list with ", PrefabCollection<TreeInfo>.LoadedCount().ToString(), " loaded prefabs");

			// Iterate through all loaded tree prefabs.
			for (uint i = 0u; i < PrefabCollection<TreeInfo>.LoadedCount(); ++i)
			{
				// Get tree and add to our list, if it isn't null.
				TreeInfo tree = PrefabCollection<TreeInfo>.GetLoaded(i);
				if (tree?.name != null)
				{
					trees.Add(tree);
				}
			}

			Logging.Message("list build completed");

			// Order lists by name.
			loadedProps = props.OrderBy(prop => GetDisplayName(prop.name)).ToList().ToArray();
			loadedTrees = trees.OrderBy(tree => GetDisplayName(tree.name)).ToList().ToArray();

			Logging.Message("list ordering complete");
		}


		/// <summary>
		/// Sanitises a raw prefab name for display.
		/// Called by the settings panel fastlist.
		/// </summary>
		/// <param name="fullName">Original (raw) prefab name</param>
		/// <returns>Cleaned display name</returns>
		internal static string GetDisplayName(string fullName)
		{
			// Find any leading period (Steam package number).
			int num = fullName.IndexOf('.');

			// If no period, assume vanilla asset; return full name preceeded by vanilla flag.
			if (num < 0)
			{
				return "[v] " + fullName;
			}

			// Otherwise, omit the package number, and trim off any trailing _Data.
			return fullName.Substring(num + 1).Replace("_Data", "");
		}
	}
}
