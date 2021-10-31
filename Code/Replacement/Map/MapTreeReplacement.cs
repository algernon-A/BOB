using System.Collections.Generic;
using ColossalFramework;


namespace BOB
{
	/// <summary>
	/// Cass to manage map tree replacements.
	/// </summary>
	internal class MapTreeReplacement
	{
		// Instance reference.
		internal static MapTreeReplacement instance;

		// Master dictionary of replaced tree prefabs.
		internal Dictionary<TreeInfo, TreeInfo> replacements;


		/// <summary>
		/// Constructor - initializes instance reference and calls initial setup.
		/// </summary>
		internal MapTreeReplacement()
		{
			instance = this;
			Setup();
		}


		/// <summary>
		/// Performs setup and initialises the master dictionary.  Must be called prior to use.
		/// </summary>
		internal void Setup()
		{
			replacements = new Dictionary<TreeInfo, TreeInfo>();
		}


		/// <summary>
		/// Applies a new (or updated) map tree replacement.
		/// </summary>
		/// <param name="target">Tree to replace</param>
		/// <param name="replacement">Replacement tree</param>
		internal void Apply(TreeInfo target, TreeInfo replacement)
		{
			// Check to see if we already have a replacement entry for this tree - if so, revert the replacement first.
			if (replacements.ContainsKey(target))
			{
				Revert(target);
			}

			// Create new dictionary entry for tree if none already exists.
			if (!replacements.ContainsKey(replacement))
			{
				replacements.Add(replacement, target);
			}

			// Apply the replacement.
			ReplaceTrees(target, replacement);
		}


		/// <summary>
		/// Reverts a map tree replacement.
		/// </summary>
		/// <param name="tree">Applied replacment tree prefab</param>
		internal void Revert(TreeInfo tree)
		{
			// Safety check.
			if (tree == null || !replacements.ContainsKey(tree))
			{
				return;
			}

			// Restore original trees.
			ReplaceTrees(tree, replacements[tree]);

			// Remove dictionary entry.
			replacements.Remove(tree);
		}


		/// <summary>
		/// Checks if the given tree prefab has a currently recorded map replacement, and if so, returns the *original* tree prefab.
		/// </summary>
		/// <param name="treePrefab">Tree prefab to check</param>
		/// <returns>Original prefab if a map tree replacement is currently recorded, null if no map tree replacement is currently recorded</returns>
		internal TreeInfo GetOriginal(TreeInfo treePrefab)
		{
			// Safety check.
			if (treePrefab != null && replacements.ContainsKey(treePrefab))
			{
				// Return the original prefab.
				return replacements[treePrefab];
			}

			// If we got here, no entry was found - return null to indicate no active replacement.
			return null;
		}


		/// <summary>
		/// Replaces a map tree.
		/// </summary>
		/// <param name="target">Tree to replace</param>
		/// <param name="replacement">Replacement tree</param>
		private void ReplaceTrees(TreeInfo target, TreeInfo replacement)
		{
			// Check for valid parameters.
			if (target != null && replacement != null)
			{
				// Local references.
				TreeManager treeManager = Singleton<TreeManager>.instance;
				TreeInstance[] trees = treeManager.m_trees.m_buffer;

				// Iterate through each tree in map.
				for (uint treeIndex = 0; treeIndex < trees.Length; ++treeIndex)
				{
					// Local reference.
					TreeInstance tree = trees[treeIndex];

					// Skip non-existent trees (those with no flags).
					if (tree.m_flags == (ushort)TreeInstance.Flags.None)
					{
						continue;
					}

					// If tree matches, replace!
					if (tree.Info == target)
					{
						trees[treeIndex].Info = replacement;

						// Refresh tree render (to update LOD).
						treeManager.UpdateTreeRenderer(treeIndex, true);
					}
				}
			}
		}
	}
}
