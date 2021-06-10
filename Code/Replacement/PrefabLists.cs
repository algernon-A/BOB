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
		internal static List<PropInfo> randomProps;
		internal static List<TreeInfo> randomTrees;

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
			randomProps = new List<PropInfo>();
			randomTrees = new List<TreeInfo>();

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
		/// <returns>New prop prefab</returns>
		internal static PropInfo NewRandomProp(string propName)
        {
			// Need valid name.
			if (propName == null)
            {
				Logging.Error("null prop name for random prop");
				return null;
			}

			// Need unique name.
			if (DuplicatePropName(propName))
			{
				Logging.Error("duplicate prop name for random prop");
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

				// Add new tree to list and return direct reference.
				randomProps.Add(randomProp);
				return randomProp;
			}

			// If we got here, then we weren't able to find the random prop template.
			Logging.Error("random prop template not found");
			return null;
		}


		/// <summary>
		/// Creates a new random tree prefab.
		/// </summary>
		/// <param name="propName">Name of prefab</param>
		/// <returns>New tree prefab</returns>
		internal static TreeInfo NewRandomTree(string treeName)
		{
			// Need valid name.
			if (treeName == null)
			{
				Logging.Error("null tree name for random tree");
				return null;
			}

			// Need unique name.
			if (DuplicateTreeName(treeName))
            {
				Logging.Error("duplicate tree name for random tree");
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

				// Add new tree to list and return direct reference.
				randomTrees.Add(randomTree);
				return randomTree;
			}

			// If we got here, then we weren't able to find the random tree template.
			Logging.Error("random tree template not found");
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
			// If h9t custom content, return full name preceeded by vanilla flag.
			if (!prefab.m_isCustomContent)
			{
				return "[v] " + prefab.name;
			}

			// Otherwise, omit the package number, and trim off any trailing _Data.
			int index = prefab.name.IndexOf('.');
			return prefab.name.Substring(index + 1).Replace("_Data", "");
		}


		/// <summary>
		/// Serializes the list of random props into XML.
		/// </summary>
		/// <returns>XML serialized random props</returns>
		internal static List<BOBRandomPrefab> SerializeRandomProps()
		{
			// Return list.
			List<BOBRandomPrefab> serializedProps = new List<BOBRandomPrefab>();

			if (randomProps != null)
			{
				// Iterate through random props and add to list.
				foreach (PropInfo randomProp in randomProps)
				{
					// Serialize prefab record.
					BOBRandomPrefab serializedPrefab = new BOBRandomPrefab
					{
						name = randomProp.name,
						variations = new List<BOBVariation>()
					};

					// Add variations.
					foreach (PropInfo.Variation variation in randomProp.m_variations)
					{
						serializedPrefab.variations.Add(new BOBVariation
						{
							name = variation.m_finalProp.name,
							probability = variation.m_probability
						});
					}

					// Add serialized prefab to list.
					serializedProps.Add(serializedPrefab);
				}
			}

			return serializedProps;
		}


		/// <summary>
		/// Serializes the list of random trees into XML.
		/// </summary>
		/// <returns>XML serialized random trees</returns>
		internal static List<BOBRandomPrefab> SerializeRandomTrees()
		{
			// Return list.
			List<BOBRandomPrefab> serializedTrees = new List<BOBRandomPrefab>();

			if (randomTrees != null)
			{
				// Iterate through random props and add to list.
				foreach (TreeInfo randomTree in randomTrees)
				{
					// Serialize prefab record.
					BOBRandomPrefab serializedPrefab = new BOBRandomPrefab
					{
						name = randomTree.name,
						variations = new List<BOBVariation>()
					};

					// Add variations.
					foreach (TreeInfo.Variation variation in randomTree.m_variations)
					{
						serializedPrefab.variations.Add(new BOBVariation
						{
							name = variation.m_finalTree.name,
							probability = variation.m_probability
						});
					}

					// Add serialized prefab to list.
					serializedTrees.Add(serializedPrefab);
				}
			}

			return serializedTrees;
		}


		/// <summary>
		/// Deserializes the list of random props from XML.
		/// </summary>
		internal static void DeserializeRandomProps(List<BOBRandomPrefab> randomPrefabList)
		{
			// Iterate through random prefab list.
			foreach (BOBRandomPrefab randomPrefab in randomPrefabList)
			{
				// Create new prop.
				PropInfo randomProp = NewRandomProp(randomPrefab.name);

				// Don't do anything more with this one if we had a creation error.
				if (randomProp == null)
                {
					continue;
                }

				// Deserialize variations.
				randomProp.m_variations = new PropInfo.Variation[randomPrefab.variations.Count];
				for (int i = 0; i < randomPrefab.variations.Count; ++i)
				{
					randomProp.m_variations[i] = new PropInfo.Variation
					{
						m_finalProp = PrefabCollection<PropInfo>.FindLoaded(randomPrefab.variations[i].name),
						m_probability = randomPrefab.variations[i].probability
					};
				}
			}
		}


		/// <summary>
		/// Deserializes the list of random trees from XML.
		/// </summary>
		internal static void DeserializeRandomTrees(List<BOBRandomPrefab> randomPrefabList)
		{
			// Iterate through random prefab list.
			foreach (BOBRandomPrefab randomPrefab in randomPrefabList)
			{
				// Create new prop.
				TreeInfo randomTree = NewRandomTree(randomPrefab.name);

				// Don't do anything more with this one if we had a creation error.
				if (randomTree == null)
				{
					continue;
				}

				// Deserialize variations.
				randomTree.m_variations = new TreeInfo.Variation[randomPrefab.variations.Count];
				for (int i = 0; i < randomPrefab.variations.Count; ++i)
				{
					randomTree.m_variations[i] = new TreeInfo.Variation
					{
						m_finalTree = PrefabCollection<TreeInfo>.FindLoaded(randomPrefab.variations[i].name),
						m_probability = randomPrefab.variations[i].probability
					};
				}
			}
		}
	}
}
