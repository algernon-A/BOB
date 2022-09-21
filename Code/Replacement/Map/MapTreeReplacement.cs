// <copyright file="MapTreeReplacement.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System.Collections.Generic;
    using AlgernonCommons;
    using ColossalFramework;

    /// <summary>
    /// Class to manage map tree replacements.
    /// </summary>
    internal class MapTreeReplacement
    {
        // Master dictionary of replaced tree prefabs.
        private readonly Dictionary<TreeInfo, TreeInfo> _replacements;

        /// <summary>
        /// Initializes a new instance of the <see cref="MapTreeReplacement"/> class.
        /// Constructor - initializes instance reference and calls initial setup.
        /// </summary>
        internal MapTreeReplacement()
        {
            Instance = this;
            _replacements = new Dictionary<TreeInfo, TreeInfo>();
        }

        /// <summary>
        /// Gets the active instance.
        /// </summary>
        internal static MapTreeReplacement Instance { get; private set; }

        /// <summary>
        /// Gets the replacements dictionary.
        /// </summary>
        internal Dictionary<TreeInfo, TreeInfo> Replacements => _replacements;

        /// <summary>
        /// Applies a new (or updated) map tree replacement.
        /// </summary>
        /// <param name="target">Tree to replace.</param>
        /// <param name="replacement">Replacement tree.</param>
        internal void Apply(TreeInfo target, TreeInfo replacement)
        {
            // Null checks.
            if (target == null || replacement == null)
            {
                Logging.Error("null parameter passet to MapPropReplacement.Apply");
            }

            // Check to see if we already have a replacement entry for this tree - if so, revert the replacement first.
            if (_replacements.ContainsKey(target))
            {
                Revert(target);
            }

            // Create new dictionary entry for tree if none already exists.
            if (!_replacements.ContainsKey(replacement))
            {
                _replacements.Add(replacement, target);
            }

            // Apply the replacement.
            ReplaceTrees(target, replacement);
        }

        /// <summary>
        /// Reverts a map tree replacement.
        /// </summary>
        /// <param name="tree">Applied replacment tree prefab.</param>
        internal void Revert(TreeInfo tree)
        {
            // Safety check.
            if (tree == null || !_replacements.ContainsKey(tree))
            {
                return;
            }

            // Restore original trees.
            ReplaceTrees(tree, _replacements[tree]);

            // Remove dictionary entry.
            _replacements.Remove(tree);
        }

        /// <summary>
        /// Checks if the given tree prefab has a currently recorded map replacement, and if so, returns the *original* tree prefab.
        /// </summary>
        /// <param name="treePrefab">Tree prefab to check.</param>
        /// <returns>Original prefab if a map tree replacement is currently recorded, null if no map tree replacement is currently recorded.</returns>
        internal TreeInfo GetOriginal(TreeInfo treePrefab)
        {
            // Safety check.
            if (treePrefab != null && _replacements.ContainsKey(treePrefab))
            {
                // Return the original prefab.
                return _replacements[treePrefab];
            }

            // If we got here, no entry was found - return null to indicate no active replacement.
            return null;
        }

        /// <summary>
        /// Replaces a map tree.
        /// </summary>
        /// <param name="target">Tree to replace.</param>
        /// <param name="replacement">Replacement tree.</param>
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

                        // Update tree render (to update LOD) via simulation thread, creating local treeID reference to avoid race condition.
                        uint treeID = treeIndex;
                        Singleton<SimulationManager>.instance.AddAction(() => Singleton<TreeManager>.instance.UpdateTreeRenderer(treeID, true));
                    }
                }
            }
        }
    }
}
