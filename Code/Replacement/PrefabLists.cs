using System.Collections.Generic;
using System.Linq;


namespace BOB
{
	/// <summary>
	/// Static class to manage lists of prefabs.
	/// </summary>
	internal static class PrefabLists
	{
		// Lists lf loaded trees and props.
		internal static List<PropInfo> loadedProps;
		internal static List<TreeInfo> loadedTrees;


		/// <summary>
		/// Builds the lists of loaded trees props.  Must be called before use.
		/// </summary>
		internal static void BuildLists()
		{
			// Initialise lists.
			loadedProps = new List<PropInfo>();
			loadedTrees = new List<TreeInfo>();

			// Iterate through all loaded props.
			for (uint i = 0u; i < PrefabCollection<PropInfo>.LoadedCount(); ++i)
			{
				// Get prop and add to our list, if it isn't null.
				PropInfo prop = PrefabCollection<PropInfo>.GetLoaded(i);
				if (prop?.name != null)
				{
					loadedProps.Add(prop);
				}
			}

			// Iterate through all loaded trees.
			for (uint i = 0u; i < PrefabCollection<TreeInfo>.LoadedCount(); ++i)
			{
				// Get tree and add to our list, if it isn't null.
				TreeInfo tree = PrefabCollection<TreeInfo>.GetLoaded(i);
				if (tree?.name != null)
				{
					loadedTrees.Add(tree);
				}
			}

			// Order lists by name.
			loadedProps = loadedProps.OrderBy(prop => UIUtils.GetDisplayName(prop.name)).ToList();
			loadedTrees = loadedTrees.OrderBy(tree => UIUtils.GetDisplayName(tree.name)).ToList();
		}
	}
}
