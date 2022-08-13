namespace BOB
{
	using AlgernonCommons;
	using System;
	using System.Collections.Generic;
	using UnityEngine;

	/// <summary>
	/// Static class to manage random prop and tree prefabs.
	/// </summary>
	internal static class RandomPrefabs
	{
		/// <summary>
		/// Current list of random props.
		/// </summary>
		internal static List<BOBRandomPrefab> RandomProps => ConfigurationUtils.CurrentConfig.randomProps;


		/// <summary>
		/// Current list of random trees.
		/// </summary>
		internal static List<BOBRandomPrefab> RandomTrees => ConfigurationUtils.CurrentConfig.randomTrees;


		/// <summary>
		/// Checks to see if the given prop name is already in use.
		/// </summary>
		/// <param name="propName">Prop name to check</param>
		/// <returns>True if name is already in use, false otherwise</returns>
		internal static bool DuplicatePropName(string propName) => RandomProps.Find(x => x.name.Equals(propName)) != null || Array.Find(PrefabLists.LoadedProps, x => x.name.Equals(propName)) != null;


		/// <summary>
		/// Checks to see if the given tree name is already in use.
		/// </summary>
		/// <param name="treeName">Tree name to check</param>
		/// <returns>True if name is already in use, false otherwise</returns>
		internal static bool DuplicateTreeName(string treeName) => RandomTrees.Find(x => x.name.Equals(treeName)) != null || Array.Find(PrefabLists.LoadedTrees, x => x.name.Equals(treeName)) != null;


		/// <summary>
		/// Creates a new random prop prefab.
		/// </summary>
		/// <param name="propName">Name of prefab</param>
		/// <returns>New random prop prefab, or null if creation failed</returns>
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
					name = propName,
					prop = newProp
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
		/// <param name="propName">Name of prefab</param>
		/// <returns>New random tree prefab, or null if creation failed</returns>
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
					name = treeName,
					tree = newTree
				};

				// Add new tree to list and return direct reference.
				RandomTrees.Add(newPrefab);
				return newPrefab;
			}

			// If we got here, something went wrong; return null.
			return null;
		}

		/// <summary>
		/// 
		/// Deserializes the list of random props from XML.
		/// </summary>
		/// <param name="randomPrefabList">List of random props to deserialize</param>
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
					randomProp.prop = InstantiateProp(randomProp.name);

					// Don't do anything more with this one if we had a creation error.
					if (randomProp.prop == null)
					{
						continue;
					}

					Logging.Message("created random prop ", randomProp.prop.name);

					// Find and assign variation prefabs.
					randomProp.prop.m_variations = new PropInfo.Variation[randomProp.variations.Count];
					for (int j = 0; j < randomProp.variations.Count; ++j)
					{
						PropInfo thisProp = PrefabCollection<PropInfo>.FindLoaded(randomProp.variations[j].name);
						randomProp.variations[j].prefab = thisProp;
						randomProp.prop.m_variations[j] = new PropInfo.Variation
						{
							m_finalProp = thisProp,
							m_probability = randomProp.variations[j].probability
						};

						// Set 'not all loaded' flag as appropriate.
						if (thisProp == null)
						{
							randomProp.missingVariant = true;
						}

						// Set shader.
						if (randomProp.prop.m_material != null)
						{
							randomProp.prop.m_material.shader = shader;
						}

						// Set shader.
						if (randomProp.prop.m_lodMaterial != null)
						{
							randomProp.prop.m_lodMaterial.shader = shader;
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
		/// <param name="randomPrefabList">List of random trees to deserialize</param>
		internal static void DeserializeRandomTrees(List<BOBRandomPrefab> randomPrefabList)
		{
			// Iterate through each item and setup prefab.
			foreach (BOBRandomPrefab randomTree in randomPrefabList)
			{
				try
				{
					// Create new prop.
					randomTree.tree = InstantiateTree(randomTree.name);

					// Don't do anything more with this one if we had a creation error.
					if (randomTree.tree == null)
					{
						continue;
					}

					// Find and assign variation prefabs.
					randomTree.tree.m_variations = new TreeInfo.Variation[randomTree.variations.Count];
					for (int j = 0; j < randomTree.variations.Count; ++j)
					{
						TreeInfo thisTree = PrefabCollection<TreeInfo>.FindLoaded(randomTree.variations[j].name);
						randomTree.variations[j].prefab = thisTree;
						randomTree.tree.m_variations[j] = new TreeInfo.Variation
						{
							m_finalTree = thisTree,
							m_probability = randomTree.variations[j].probability
						};

						// Set 'not all loaded' flag as appropriate.
						if (thisTree == null)
						{
							randomTree.missingVariant = true;
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
		/// <param name="prop">Prop prefab to remove</param>
		internal static void RemoveRandomProp(PropInfo prop) => RandomProps.Remove(RandomProps.Find(x => x.name.Equals(prop.name)));


		/// <summary>
		/// Removes a random tree.
		/// </summary>
		/// <param name="tree">Tree prefab to remove</param>
		internal static void RemoveRandomTree(TreeInfo tree) => RandomTrees.Remove(RandomTrees.Find(x => x.name.Equals(tree.name)));


		/// <summary>
		/// Instantiates a new PropInfp prefab.
		/// </summary>
		/// <param name="propName">Name to instantiate</param>
		/// <returns>New tree prefab, or null if instantiation fialed</returns>
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
		/// <param name="treeName">Name to instantiate</param>
		/// <returns>New tree prefab, or null if instantiation fialed</returns>
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
