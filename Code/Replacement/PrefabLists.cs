using System.Collections.Generic;
using System.Linq;
using UnityEngine;


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

		// Lists of random trees and props.
		internal static List<BOBRandomPrefab> randomProps;
		internal static List<BOBRandomPrefab> randomTrees;

		// Random tree and prop templates.
		private static PropInfo randomPropTemplate;
		private static TreeInfo randomTreeTemplate;


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
					if (randomPropTemplate == null)
                    {
						if (prop.name.EndsWith("BOBRandomPropTemplate_Data"))
                        {
							randomPropTemplate = prop;
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
					if (randomTreeTemplate == null)
					{
						if (tree.name.EndsWith("BOBRandomTreeTemplate_Data"))
						{
							randomTreeTemplate = tree;
						}
					}
				}
			}

			// Initialise random lists.
			randomProps = new List<BOBRandomPrefab>();
			randomTrees = new List<BOBRandomPrefab>();

			// Order lists by name.
			loadedProps = props.OrderBy(prop => GetDisplayName(prop)).ToList().ToArray();
			loadedTrees = trees.OrderBy(tree => GetDisplayName(tree)).ToList().ToArray();
		}


		/// <summary>
		/// Checks to see if the given prop name is already in use.
		/// </summary>
		/// <param name="propName">Prop name to check</param>
		/// <returns>True if name is already in use, false otherwise</returns>
		internal static bool DuplicatePropName(string propName) => randomProps.Find(x => x.name.Equals(propName)) != null || System.Array.Find(loadedProps, x => x.name.Equals(propName)) != null;


		/// <summary>
		/// Checks to see if the given tree name is already in use.
		/// </summary>
		/// <param name="treeName">Tree name to check</param>
		/// <returns>True if name is already in use, false otherwise</returns>
		internal static bool DuplicateTreeName(string treeName) => randomTrees.Find(x => x.name.Equals(treeName)) != null || System.Array.Find(loadedTrees, x => x.name.Equals(treeName)) != null;


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
					prop = newProp,
					variations = new List<BOBVariation>()
				};

				// Add new tree to list and return direct reference.
				randomProps.Add(newPrefab);
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
					tree = newTree,
					variations = new List<BOBVariation>()
				};

				// Add new tree to list and return direct reference.
				randomTrees.Add(newPrefab);
				return newPrefab;
			}

			// If we got here, something went wrong; return null.
			return null;
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
			return name.Substring(index + 1).Replace("_Data", "");
		}


		/// <summary>
		/// Serializes the list of random props into XML.
		/// </summary>
		/// <returns>XML serialized random props</returns>
		internal static List<BOBRandomPrefab> SerializeRandomProps() => randomProps;


		/// <summary>
		/// Serializes the list of random trees into XML.
		/// </summary>
		/// <returns>XML serialized random trees</returns>
		internal static List<BOBRandomPrefab> SerializeRandomTrees() => randomTrees;


		/// <summary>
		/// Deserializes the list of random props from XML.
		/// </summary>
		internal static void DeserializeRandomProps(List<BOBRandomPrefab> randomPrefabList)
		{
			// Use read list.
			randomProps = randomPrefabList;

			// Iterate through each item and setup prefab.
			for (int i = 0; i < randomProps.Count; ++i)
			{
				// Create new prop.
				randomProps[i].prop = InstantiateProp(randomProps[i].name);

				// Don't do anything more with this one if we had a creation error.
				if (randomProps[i].prop == null)
                {
					continue;
                }

				Logging.Message("created random prop ", randomProps[i].prop.name);

				// Find and assign variation prefabs.
				randomProps[i].prop.m_variations = new PropInfo.Variation[randomProps[i].variations.Count];
				for (int j = 0; j < randomProps[i].variations.Count; ++j)
				{
					PropInfo thisProp = PrefabCollection<PropInfo>.FindLoaded(randomProps[i].variations[j].name);
					randomProps[i].variations[j].prefab = thisProp;
					randomProps[i].prop.m_variations[j] = new PropInfo.Variation
					{
						m_finalProp = thisProp,
						m_probability = randomProps[i].variations[j].probability
					};

					// Set 'not all loaded' flag as appropriate.
					if (thisProp == null)
                    {
						randomProps[i].missingVariant = true;
					}
				}
			}
		}


		/// <summary>
		/// Deserializes the list of random trees from XML.
		/// </summary>
		internal static void DeserializeRandomTrees(List<BOBRandomPrefab> randomPrefabList)
		{
			// Use read list.
			randomTrees = randomPrefabList;

			// Iterate through each item and setup prefab.
			for (int i = 0; i < randomTrees.Count; ++i)
			{
				// Create new prop.
				randomTrees[i].tree = InstantiateTree(randomTrees[i].name);

				// Don't do anything more with this one if we had a creation error.
				if (randomTrees[i].tree == null)
				{
					continue;
				}

				// Find and assign variation prefabs.
				randomTrees[i].tree.m_variations = new TreeInfo.Variation[randomTrees[i].variations.Count];
				for (int j = 0; j < randomTrees[i].variations.Count; ++j)
				{
					TreeInfo thisTree = PrefabCollection<TreeInfo>.FindLoaded(randomTrees[i].variations[j].name);
					randomTrees[i].variations[j].prefab = thisTree;
					randomTrees[i].tree.m_variations[j] = new TreeInfo.Variation
					{
						m_finalTree = thisTree,
						m_probability = randomTrees[i].variations[j].probability
					};

					// Set 'not all loaded' flag as appropriate.
					if (thisTree == null)
					{
						randomTrees[i].missingVariant = true;
					}
				}
			}
		}


		/// <summary>
		/// Removes a random prop.
		/// </summary>
		/// <param name="prop">Prop prefab to remove</param>
		internal static void RemoveRandomProp(PropInfo prop) => randomProps.Remove(randomProps.Find(x => x.name.Equals(prop.name)));


		/// <summary>
		/// Removes a random tree.
		/// </summary>
		/// <param name="tree">Tree prefab to remove</param>
		internal static void RemoveRandomTree(TreeInfo tree) => randomTrees.Remove(randomTrees.Find(x => x.name.Equals(tree.name)));


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
			if (randomPropTemplate != null)
			{
				GameObject objectInstance = Object.Instantiate(randomPropTemplate.gameObject);
				objectInstance.name = propName;
				PropInfo randomProp = objectInstance.GetComponent<PropInfo>();
				objectInstance.SetActive(false);
				randomProp.m_isCustomContent = true;
				randomProp.m_prefabInitialized = false;
				randomProp.InitializePrefab();
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
			if (randomTreeTemplate != null)
			{
				GameObject treeInstance = Object.Instantiate(randomTreeTemplate.gameObject);
				treeInstance.name = treeName;
				TreeInfo randomTree = treeInstance.GetComponent<TreeInfo>();
				treeInstance.SetActive(false);
				randomTree.m_isCustomContent = true;
				randomTree.m_prefabInitialized = false;
				randomTree.InitializePrefab();
				return randomTree;
			}

			// If we got here, then we weren't able to find the random tree template.
			Logging.Error("random tree template not found");
			return null;
		}
	}
}
