using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using ColossalFramework;


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
			// Diagnostics.
			Stopwatch sw = new Stopwatch();
			Logging.Message("commencing BuildLists");
			sw.Start();

			// Local references.
			TreeInstance[] treeBuffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
			PropInstance[] propBuffer = Singleton<PropManager>.instance.m_props.m_buffer;

			// Initialise lists.
			List<PropInfo> props = new List<PropInfo>();
			List<TreeInfo> trees = new List<TreeInfo>();
			ruinedChangedProps = new List<PropInfo>();
			ruinedChangedTrees = new List<TreeInfo>();
			List<uint> treesToUpdate = new List<uint>();
			List<uint> propsToUpdate = new List<uint>();

			// Are we removing existing tree ruining?
			if (ModSettings.RefreshTreeRuining && ModSettings.StopTreeRuining)
			{
				Logging.Message("calculating tree ruining");

				// Yes - iterate through all trees on map.
				for (uint i = 0; i < treeBuffer.Length; ++i)
				{
					if ((treeBuffer[i].m_flags & 1) == 1 && (treeBuffer[i].m_flags & 4) == 0 && treeBuffer[i].Info != null && treeBuffer[i].Position != null && treeBuffer[i].Info.m_createRuining)
					{
						// This one passed our filter - add to list.
						treesToUpdate.Add(i);
					}
				}

				Logging.Message("tree ruining calculation completed at ", sw.ElapsedMilliseconds.ToString(), "ms");
			}

			// Are we removing existing prop ruining?
			if (ModSettings.RefreshPropRuining && ModSettings.StopPropRuining)
			{
				Logging.Message("calculating prop ruining");

				// Yes - iterate through all props on map.
				for (uint i = 0; i < propBuffer.Length; ++i)
				{
					if ((propBuffer[i].m_flags & 1) == 1 && (propBuffer[i].m_flags & 4) == 0 && propBuffer[i].Info != null && propBuffer[i].Position != null && propBuffer[i].Info.m_createRuining)
					{
						// This one passed our filter - add to list.
						propsToUpdate.Add(i);
					}
				}

				Logging.Message("tree ruining calculation completed at ", sw.ElapsedMilliseconds.ToString(), "ms");
			}

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

			Logging.Message("loaded prop collection completed at ", sw.ElapsedMilliseconds.ToString(), "ms");

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

			Logging.Message("loaded tree collection completed at ", sw.ElapsedMilliseconds.ToString(), "ms");

			// Order lists by name.
			loadedProps = props.OrderBy(prop => GetDisplayName(prop.name)).ToList().ToArray();
			loadedTrees = trees.OrderBy(tree => GetDisplayName(tree.name)).ToList().ToArray();

			Logging.KeyMessage("completed prefab lists at ", sw.ElapsedMilliseconds.ToString(), "ms");

			// Process any trees marked for update.
			if (treesToUpdate.Count > 0)
			{
				Logging.KeyMessage("updating tree ruining with ", treesToUpdate.Count.ToString(), " instances");
				long elapsedTime = sw.ElapsedMilliseconds;

				foreach (int treeID in treesToUpdate)
				{
					treeBuffer[treeID].Info.m_createRuining = false;
					float minX = treeBuffer[treeID].Position.x - 4f;
					float minZ = treeBuffer[treeID].Position.z - 4f;
					float maxX = treeBuffer[treeID].Position.x + 4f;
					float maxZ = treeBuffer[treeID].Position.z + 4f;
					TerrainModify.UpdateArea(minX, minZ, maxX, maxZ, heights: false, surface: true, zones: false);

					if (sw.ElapsedMilliseconds - elapsedTime > 5000)
                    {
						Logging.KeyMessage("not crashed - still updating tree ruining, just processed tree ", treeID.ToString());
						elapsedTime = sw.ElapsedMilliseconds;
                    }
				}
				Logging.KeyMessage("completed tree ruining update at ", sw.ElapsedMilliseconds.ToString(), "ms");
			}

			if (propsToUpdate.Count > 0)
			{
				Logging.KeyMessage("updating prop ruining with ", treesToUpdate.Count.ToString(), " instances");

				// Process any props marked for update.
				foreach (int propID in propsToUpdate)
				{
					propBuffer[propID].Info.m_createRuining = false;
					float minX = propBuffer[propID].Position.x - 4f;
					float minZ = propBuffer[propID].Position.z - 4f;
					float maxX = propBuffer[propID].Position.x + 4f;
					float maxZ = propBuffer[propID].Position.z + 4f;
					TerrainModify.UpdateArea(minX, minZ, maxX, maxZ, heights: false, surface: true, zones: false);
				}

				Logging.KeyMessage("completed prop ruining update at ", sw.ElapsedMilliseconds.ToString(), "ms");
			}
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
