// <copyright file="RandomPrefabs.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System;
    using System.Collections.Generic;
    using AlgernonCommons;
    using UnityEngine;

    /// <summary>
    /// Static class to manage random prop and tree prefabs.
    /// </summary>
    internal static class RandomPrefabs
    {
        /// <summary>
        /// Gets the current list of random props.
        /// </summary>
        internal static List<BOBRandomPrefab> RandomProps => ConfigurationUtils.CurrentConfig.RandomProps;

        /// <summary>
        /// Gets the current list of random trees.
        /// </summary>
        internal static List<BOBRandomPrefab> RandomTrees => ConfigurationUtils.CurrentConfig.RandomTrees;

        /// <summary>
        /// Checks to see if the given prop name is already in use.
        /// </summary>
        /// <param name="propName">Prop name to check.</param>
        /// <returns>True if name is already in use, false otherwise.</returns>
        internal static bool DuplicatePropName(string propName) => RandomProps.Find(x => x.Name.Equals(propName)) != null || Array.Find(PrefabLists.LoadedProps, x => x.name.Equals(propName)) != null;

        /// <summary>
        /// Checks to see if the given tree name is already in use.
        /// </summary>
        /// <param name="treeName">Tree name to check.</param>
        /// <returns>True if name is already in use, false otherwise.</returns>
        internal static bool DuplicateTreeName(string treeName) => RandomTrees.Find(x => x.Name.Equals(treeName)) != null || Array.Find(PrefabLists.LoadedTrees, x => x.name.Equals(treeName)) != null;

        /// <summary>
        /// Creates a new random prop prefab.
        /// </summary>
        /// <param name="propName">Name of prefab.</param>
        /// <returns>New random prop prefab, or null if creation failed.</returns>
        internal static BOBRandomPrefab NewRandomProp(string propName)
        {
            // Need unique name.
            if (DuplicatePropName(propName))
            {
                Logging.Error("duplicate prop name for random prop");
                return null;
            }

            PropInfo newProp = InstantiateProp(propName);

            if (newProp != null)
            {
                BOBRandomPrefab newPrefab = new BOBRandomPrefab
                {
                    Name = propName,
                    Prop = newProp,
                };

                // Add new tree to list and return direct reference.
                RandomProps.Add(newPrefab);
                return newPrefab;
            }

            // If we got here, something went wrong; return null.
            return null;
        }

        /// <summary>
        /// Creates a new random tree prefab.
        /// </summary>
        /// <param name="treeName">Name of prefab.</param>
        /// <returns>New random tree prefab, or null if creation failed.</returns>
        internal static BOBRandomPrefab NewRandomTree(string treeName)
        {
            // Need unique name.
            if (DuplicateTreeName(treeName))
            {
                Logging.Error("duplicate tree name for random tree");
                return null;
            }

            TreeInfo newTree = InstantiateTree(treeName);

            if (newTree != null)
            {
                BOBRandomPrefab newPrefab = new BOBRandomPrefab
                {
                    Name = treeName,
                    Tree = newTree,
                };

                // Add new tree to list and return direct reference.
                RandomTrees.Add(newPrefab);
                return newPrefab;
            }

            // If we got here, something went wrong; return null.
            return null;
        }

        /// <summary>
        /// Deserializes the list of random props from XML.
        /// </summary>
        /// <param name="randomPrefabList">List of random props to deserialize.</param>
        internal static void DeserializeRandomProps(List<BOBRandomPrefab> randomPrefabList)
        {
            // Shader reference.
            Shader shader = Shader.Find("Custom/Props/Prop/Default");

            // Iterate through each item and setup prefab.
            foreach (BOBRandomPrefab randomProp in randomPrefabList)
            {
                try
                {
                    // Create new prop.
                    randomProp.Prop = InstantiateProp(randomProp.Name);

                    // Don't do anything more with this one if we had a creation error.
                    if (randomProp.Prop == null)
                    {
                        continue;
                    }

                    Logging.Message("created random prop ", randomProp.Prop.name);

                    // Find and assign variation prefabs.
                    randomProp.Prop.m_variations = new PropInfo.Variation[randomProp.Variations.Count];
                    for (int j = 0; j < randomProp.Variations.Count; ++j)
                    {
                        PropInfo thisProp = PrefabCollection<PropInfo>.FindLoaded(randomProp.Variations[j].Name);
                        randomProp.Variations[j].Prefab = thisProp;
                        randomProp.Prop.m_variations[j] = new PropInfo.Variation
                        {
                            m_finalProp = thisProp,
                            m_probability = randomProp.Variations[j].Probability,
                        };

                        // Set 'not all loaded' flag as appropriate.
                        if (thisProp == null)
                        {
                            randomProp.MissingVariant = true;
                        }

                        // Set shader.
                        if (randomProp.Prop.m_material != null)
                        {
                            randomProp.Prop.m_material.shader = shader;
                        }

                        // Set shader.
                        if (randomProp.Prop.m_lodMaterial != null)
                        {
                            randomProp.Prop.m_lodMaterial.shader = shader;
                        }
                    }
                }
                catch (Exception e)
                {
                    // Don't let a single failure stop us.
                    Logging.LogException(e, "exception deserializing random prop");
                }
            }
        }

        /// <summary>
        /// Deserializes the list of random trees from XML.
        /// </summary>
        /// <param name="randomPrefabList">List of random trees to deserialize.</param>
        internal static void DeserializeRandomTrees(List<BOBRandomPrefab> randomPrefabList)
        {
            // Iterate through each item and setup prefab.
            foreach (BOBRandomPrefab randomTree in randomPrefabList)
            {
                try
                {
                    // Create new prop.
                    randomTree.Tree = InstantiateTree(randomTree.Name);

                    // Don't do anything more with this one if we had a creation error.
                    if (randomTree.Tree == null)
                    {
                        continue;
                    }

                    // Find and assign variation prefabs.
                    randomTree.Tree.m_variations = new TreeInfo.Variation[randomTree.Variations.Count];
                    for (int j = 0; j < randomTree.Variations.Count; ++j)
                    {
                        TreeInfo thisTree = PrefabCollection<TreeInfo>.FindLoaded(randomTree.Variations[j].Name);
                        randomTree.Variations[j].Prefab = thisTree;
                        randomTree.Tree.m_variations[j] = new TreeInfo.Variation
                        {
                            m_finalTree = thisTree,
                            m_probability = randomTree.Variations[j].Probability,
                        };

                        // Set 'not all loaded' flag as appropriate.
                        if (thisTree == null)
                        {
                            randomTree.MissingVariant = true;
                        }
                    }
                }
                catch (Exception e)
                {
                    // Don't let a single failure stop us.
                    Logging.LogException(e, "exception deserializing random tree");
                }
            }
        }

        /// <summary>
        /// Removes a random prop.
        /// </summary>
        /// <param name="prop">Prop prefab to remove.</param>
        internal static void RemoveRandomProp(PropInfo prop) => RandomProps.Remove(RandomProps.Find(x => x.Name.Equals(prop.name)));

        /// <summary>
        /// Removes a random tree.
        /// </summary>
        /// <param name="tree">Tree prefab to remove.</param>
        internal static void RemoveRandomTree(TreeInfo tree) => RandomTrees.Remove(RandomTrees.Find(x => x.Name.Equals(tree.name)));

        /// <summary>
        /// Instantiates a new PropInfp prefab.
        /// </summary>
        /// <param name="propName">Name to instantiate.</param>
        /// <returns>New tree prefab, or null if instantiation failed.</returns>
        internal static PropInfo InstantiateProp(string propName)
        {
            // Need valid name.
            if (propName == null)
            {
                Logging.Error("null prop name for random prop");
                return null;
            }

            // Instantiate prop template and use as base for new prop.
            if (PrefabLists.RandomPropTemplate != null)
            {
                GameObject objectInstance = UnityEngine.Object.Instantiate(PrefabLists.RandomPropTemplate.gameObject);
                objectInstance.name = propName;
                PropInfo randomProp = objectInstance.GetComponent<PropInfo>();
                objectInstance.SetActive(false);
                randomProp.m_isCustomContent = true;
                randomProp.m_prefabInitialized = false;
                randomProp.InitializePrefab();
                randomProp.m_prefabInitialized = true;
                return randomProp;
            }

            // If we got here, then we weren't able to find the random prop template.
            Logging.Error("random prop template not found");
            return null;
        }

        /// <summary>
        /// Instantiates a new TreeInfo prefab.
        /// </summary>
        /// <param name="treeName">Name to instantiate.</param>
        /// <returns>New tree prefab, or null if instantiation failed.</returns>
        private static TreeInfo InstantiateTree(string treeName)
        {
            // Need valid name.
            if (treeName == null)
            {
                Logging.Error("null tree name for random tree");
                return null;
            }

            // Instantiate tree template and use as base for new tree.
            if (PrefabLists.RandomTreeTemplate != null)
            {
                GameObject treeInstance = UnityEngine.Object.Instantiate(PrefabLists.RandomTreeTemplate.gameObject);
                treeInstance.name = treeName;
                TreeInfo randomTree = treeInstance.GetComponent<TreeInfo>();
                treeInstance.SetActive(false);
                randomTree.m_isCustomContent = true;
                randomTree.m_prefabInitialized = false;
                randomTree.InitializePrefab();
                randomTree.m_prefabInitialized = true;
                return randomTree;
            }

            // If we got here, then we weren't able to find the random tree template.
            Logging.Error("random tree template not found");
            return null;
        }
    }
}
