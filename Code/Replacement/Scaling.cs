// <copyright file="Scaling.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AlgernonCommons;

    /// <summary>
    /// Class to manage prop and tree scaling.
    /// </summary>
    internal class Scaling
    {
        // Master dictionary of scaling records..
        private readonly Dictionary<string, BOBScalingElement> _treeScales;
        private readonly Dictionary<string, BOBScalingElement> _propScales;

        /// <summary>
        /// Initializes a new instance of the <see cref="Scaling"/> class.
        /// </summary>
        internal Scaling()
        {
            Instance = this;
            _treeScales = new Dictionary<string, BOBScalingElement>();
            _propScales = new Dictionary<string, BOBScalingElement>();
        }

        /// <summary>
        /// Gets the active instance reference.
        /// </summary>
        internal static Scaling Instance { get; private set; }

        /// <summary>
        /// Deserialise prop scaling.
        /// </summary>
        /// <param name="elements">List of prop scaling elements to deserialise.</param>
        internal void DeserializeProps(List<BOBScalingElement> elements)
        {
            // Iterate through each element.
            foreach (BOBScalingElement element in elements)
            {
                try
                {
                    // Copy element struct.
                    BOBScalingElement scalingElement = element;

                    // Catch and correct any invalid data.
                    scalingElement.EnsureBounds();

                    // Try to find prefab in loaded collection.
                    PropInfo thisProp = PrefabCollection<PropInfo>.FindLoaded(scalingElement.PrefabName);
                    if (thisProp != null)
                    {
                        // Found it - record original values.
                        scalingElement.Prefab = thisProp;
                        scalingElement.OriginalMin = thisProp.m_minScale;
                        scalingElement.OriginalMax = thisProp.m_maxScale;

                        // Apply new values.
                        thisProp.m_minScale = scalingElement.MinScale;
                        thisProp.m_maxScale = scalingElement.MaxScale;
                    }

                    // In any case, record to dictionary (to retain records of any prefabs not found).
                    _propScales.Add(scalingElement.PrefabName, scalingElement);
                }
                catch (Exception e)
                {
                    // Don't let a single failure stop us.
                    Logging.LogException(e, "exception deserializing prop scaling element");
                }
            }
        }

        /// <summary>
        /// Deserialise tree scaling.
        /// </summary>
        /// <param name="elements">List of tree scaling elements to deserialise.</param>
        internal void DeserializeTrees(List<BOBScalingElement> elements)
        {
            foreach (BOBScalingElement element in elements)
            {
                try
                {
                    // Copy element struct.
                    BOBScalingElement scalingElement = element;

                    // Catch and correct any invalid data.
                    scalingElement.EnsureBounds();

                    // Try to find prefab in loaded collection, and if so, apply the recorded scaling.
                    TreeInfo thisTree = PrefabCollection<TreeInfo>.FindLoaded(scalingElement.PrefabName);
                    if (thisTree != null)
                    {
                        // Found it - record original values.
                        scalingElement.Prefab = thisTree;
                        scalingElement.OriginalMin = thisTree.m_minScale;
                        scalingElement.OriginalMax = thisTree.m_maxScale;

                        // Apply new values.
                        thisTree.m_minScale = scalingElement.MinScale;
                        thisTree.m_maxScale = scalingElement.MaxScale;
                    }

                    // In any case, record to dictionary (to retain records of any prefabs not found).
                    _treeScales.Add(scalingElement.PrefabName, scalingElement);
                }
                catch (Exception e)
                {
                    // Don't let a single failure stop us.
                    Logging.LogException(e, "exception deserializing tree scaling element");
                }
            }
        }

        /// <summary>
        /// Apply new minimum scale.
        /// </summary>
        /// <param name="prefab">Prefab to apply to.</param>
        /// <param name="minScale">New minimum scale.</param>
        internal void ApplyMinScale(PrefabInfo prefab, float minScale)
        {
            // Apply scale to prefab.
            if (prefab is PropInfo prop)
            {
                PropScale(prop, minScale, prop.m_maxScale);
            }
            else if (prefab is TreeInfo tree)
            {
                TreeScale(tree, minScale, tree.m_maxScale);
            }
        }

        /// <summary>
        /// Apply new maximum scale.
        /// </summary>
        /// <param name="prefab">Prefab to apply to.</param>
        /// <param name="maxScale">New maximum scale.</param>
        internal void ApplyMaxScale(PrefabInfo prefab, float maxScale)
        {
            // Apply scale to prefab.
            if (prefab is PropInfo prop)
            {
                PropScale(prop, prop.m_minScale, maxScale);
            }
            else if (prefab is TreeInfo tree)
            {
                TreeScale(tree, tree.m_minScale, maxScale);
            }
        }

        /// <summary>
        /// Revert a prefab to original scaling.
        /// </summary>
        /// <param name="prefab">Prefab to revert.</param>
        /// <param name="removeEntries">True to remove the reverted entries from the list of replacements, false to leave the list unchanged.</param>
        internal void Revert(PrefabInfo prefab, bool removeEntries)
        {
            // Prop or tree?
            if (prefab is PropInfo prop)
            {
                // Prop - check if we have a record.
                if (_propScales.ContainsKey(prop.name))
                {
                    // Local reference.
                    BOBScalingElement element = _propScales[prop.name];

                    // Reset prop scale.
                    prop.m_minScale = element.OriginalMin;
                    prop.m_maxScale = element.OriginalMax;

                    // Remove record from dictionary, if we're doing so.
                    if (removeEntries)
                    {
                        _propScales.Remove(prop.name);
                    }

                    // Save configuration file.
                    ConfigurationUtils.SaveConfig();
                }
            }
            else if (prefab is TreeInfo tree)
            {
                // Tree - check if we have a record.
                if (_treeScales.ContainsKey(tree.name))
                {
                    // Local reference.
                    BOBScalingElement element = _treeScales[tree.name];

                    // Reset prop scale.
                    tree.m_minScale = element.OriginalMin;
                    tree.m_maxScale = element.OriginalMax;

                    // Remove record from dictionary, if we're doing so.
                    if (removeEntries)
                    {
                        _treeScales.Remove(tree.name);
                    }

                    // Save configuration file.
                    ConfigurationUtils.SaveConfig();
                }
            }
        }

        /// <summary>
        /// Reverts all applied scaling and resets dictionaries.
        /// </summary>
        internal void RevertAll()
        {
            // Revert props - iterate through each recorded element.
            foreach (BOBScalingElement propElement in _propScales.Values)
            {
                Revert(propElement.Prefab, false);
            }

            // Revert trees - iterate through each recorded element.
            foreach (BOBScalingElement treeElement in _treeScales.Values)
            {
                Revert(treeElement.Prefab, false);
            }

            // Reset dictionaries.
            _treeScales.Clear();
            _propScales.Clear();
        }

        /// <summary>
        /// Serializes prop scaling records.
        /// </summary>
        /// <returns>New list of serialized prop scaling records.</returns>
        internal List<BOBScalingElement> SerializePropScales() => _propScales.Values.ToList();

        /// <summary>
        /// Serializes tree scaling records.
        /// </summary>
        /// <returns>New list of serialized tree scaling records.</returns>
        internal List<BOBScalingElement> SerializeTreeScales() => _treeScales.Values.ToList();

        /// <summary>
        /// Applies scaling to props and updates the dictionary records.
        /// </summary>
        /// <param name="prop">Prop prefab.</param>
        /// <param name="minScale">Minimum scale.</param>
        /// <param name="maxScale">Maximum scale.</param>
        private void PropScale(PropInfo prop, float minScale, float maxScale)
        {
            // If we don't have an existing record, create one, recording original prop scaling.
            if (!_propScales.TryGetValue(prop.name, out BOBScalingElement scalingElement))
            {
                // Record original values.
                scalingElement.PrefabName = prop.name;
                scalingElement.Prefab = prop;
                scalingElement.OriginalMin = prop.m_minScale;
                scalingElement.OriginalMax = prop.m_maxScale;
            }

            // Update record with new scale values.
            UpdateScalingElement(ref scalingElement, minScale, maxScale, prop.name, _propScales);

            // Apply new scales.
            prop.m_minScale = scalingElement.MinScale;
            prop.m_maxScale = scalingElement.MaxScale;
        }

        /// <summary>
        /// Applies scaling to trees and updates the dictionary records.
        /// </summary>
        /// <param name="tree">Tree prefab.</param>
        /// <param name="minScale">Minimum scale.</param>
        /// <param name="maxScale">Maximum scale.</param>
        private void TreeScale(TreeInfo tree, float minScale, float maxScale)
        {
            // If we don't have an existing record, create one, recording original prop scaling.
            if (!_treeScales.TryGetValue(tree.name, out BOBScalingElement scalingElement))
            {
                // Record original values.
                scalingElement.PrefabName = tree.name;
                scalingElement.Prefab = tree;
                scalingElement.OriginalMin = tree.m_minScale;
                scalingElement.OriginalMax = tree.m_maxScale;
            }

            // Update record with new scale values.
            UpdateScalingElement(ref scalingElement, minScale, maxScale, tree.name, _treeScales);

            // Apply new scales.
            tree.m_minScale = scalingElement.MinScale;
            tree.m_maxScale = scalingElement.MaxScale;
        }

        /// <summary>
        /// Updates a scaling element and the associated dictionary.
        /// </summary>
        /// <param name="scalingElement">Scaling element to update.</param>
        /// <param name="minScale">Minimum scale to apply.</param>
        /// <param name="maxScale">Maximum scale to apply.</param>
        /// <param name="prefabName">Target prefab name.</param>
        /// <param name="dict">Relevant scaling dictionary.</param>
        private void UpdateScalingElement(ref BOBScalingElement scalingElement, float minScale, float maxScale, string prefabName, Dictionary<string, BOBScalingElement> dict)
        {
            // Update record with new scale values.
            scalingElement.SetMinScale(minScale);
            scalingElement.SetMaxScale(maxScale);

            // Remove record if minimum and maximum scales both match the default.
            if (scalingElement.MinScale == scalingElement.OriginalMin && scalingElement.MaxScale == scalingElement.OriginalMax)
            {
                dict.Remove(prefabName);
            }
            else
            {
                // Otherwise, save our new/updated record.
                dict[prefabName] = scalingElement;
            }

            // Save updated configuration.
            ConfigurationUtils.SaveConfig();
        }
    }
}
