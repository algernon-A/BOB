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

		// Lists of trees/prop prefabs whose ruining state has been changed by the mod.
		private static List<PropInfo> ruinedChangedProps;
		private static List<TreeInfo> ruinedChangedTrees;


		/// <summary>
		/// Builds the lists of loaded trees props.  Must be called before use.
		/// </summary>
		internal static void BuildLists()
		{
			// Initialise lists.
			List<PropInfo> props = new List<PropInfo>();
			List<TreeInfo> trees = new List<TreeInfo>();
			ruinedChangedProps = new List<PropInfo>();
			ruinedChangedTrees = new List<TreeInfo>();

			// Iterate through all loaded prop prefabs.
			for (uint i = 0u; i < PrefabCollection<PropInfo>.LoadedCount(); ++i)
			{
				// Get prop and add to our list, if it isn't null.
				PropInfo prop = PrefabCollection<PropInfo>.GetLoaded(i);
				if (prop?.name != null)
				{
					props.Add(prop);

					// Check if we're using 'stop ruining' for props.
					if (ModSettings.StopPropRuining)
					{
						// If prop doesn't already have m_createRuining set to false, change it and add to the list of changed prop prefabs.
						if (prop.m_createRuining)
						{
							prop.m_createRuining = false;
							ruinedChangedProps.Add(prop);
						}
					}
				}
			}

			// Iterate through all loaded tree prefabs.
			for (uint i = 0u; i < PrefabCollection<TreeInfo>.LoadedCount(); ++i)
			{
				// Get tree and add to our list, if it isn't null.
				TreeInfo tree = PrefabCollection<TreeInfo>.GetLoaded(i);
				if (tree?.name != null)
				{
					trees.Add(tree);

					// Check if we're using 'stop ruining' for trees.
					if (ModSettings.StopTreeRuining)
					{
						// If tree doesn't already have m_createRuining set to false, change it and add to our list of changed tree prefabs.
						if (tree.m_createRuining)
						{
							// Otherwise, stop prop ruining if that option is set.
							tree.m_createRuining = false;
							ruinedChangedTrees.Add(tree);
						}
					}
				}
			}

			// Order lists by name.
			loadedProps = props.OrderBy(prop => GetDisplayName(prop.name)).ToList().ToArray();
			loadedTrees = trees.OrderBy(tree => GetDisplayName(tree.name)).ToList().ToArray();
		}


		/// <summary>
		/// Clears or restores the 'm_createRuining' flag for all prop prefabs.
		/// <param name="value">True to disable prop ruining, false to restore</param>
		/// </summary>
		internal static void DisablePropRuining(bool disable)
        {
			// Set mod setting.
			ModSettings.StopPropRuining = disable;

			// Don't do anything if list hasn't been initialised yet (e.g. we're not in-game).
			if (ruinedChangedProps != null)
			{
				// Disabling ruining?
				if (disable)
				{
					// Disabling - iterate through all loaded prop prefabs.
					foreach (PropInfo prop in loadedProps)
					{
						// If prop doesn't already have m_createRuining set to false, change it and add to the list of changed prop prefabs.
						if (prop.m_createRuining)
						{
							prop.m_createRuining = false;
							ruinedChangedProps.Add(prop);
						}
					}
				}
				else
                {
					// Restoring - iterate through all changed prop prefabs and restore.
					foreach (PropInfo prop in ruinedChangedProps)
					{
						// Reset ruining.
						prop.m_createRuining = true;
					}

					// Clear list.
					ruinedChangedProps.Clear();
				}
			}
		}


		/// <summary>
		/// Clears or restores the 'm_createRuining' flag for all tree prefabs.
		/// <param name="value">True to disable tree ruining, false to restore</param>
		/// </summary>
		internal static void DisableTreeRuining(bool disable)
		{
			// Set mod setting.
			ModSettings.StopTreeRuining = disable;

			// Don't do anything if list hasn't been initialised yet (e.g. we're not in-game).
			if (ruinedChangedTrees != null)
			{
				// Disabling ruining?
				if (disable)
				{
					// Disabling - iterate through all loaded tree prefabs.
					foreach (TreeInfo tree in loadedTrees)
					{
						// If tree doesn't already have m_createRuining set to false, change it and add to the list of changed tree prefabs.
						if (tree.m_createRuining)
						{
							tree.m_createRuining = false;
							ruinedChangedTrees.Add(tree);
						}
					}
				}
				else
				{
					// Restoring - iterate through all changed tree prefabs and restore.
					foreach (TreeInfo tree in ruinedChangedTrees)
					{
						// Reset ruining.
						tree.m_createRuining = true;
					}

					// Clear list.
					ruinedChangedTrees.Clear();
				}
			}
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
