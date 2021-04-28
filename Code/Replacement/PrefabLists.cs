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
				}
			}

			randomProps = new List<PropInfo>();
			randomTrees = new List<TreeInfo>();

			// Order lists by name.
			loadedProps = props.OrderBy(prop => GetDisplayName(prop.name)).ToList().ToArray();
			loadedTrees = trees.OrderBy(tree => GetDisplayName(tree.name)).ToList().ToArray();
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

			// Instantiate existing prop and use as base for new prop.
			GameObject objectInstance = Object.Instantiate(PrefabCollection<PropInfo>.FindLoaded("Amp Stack").gameObject);
			objectInstance.name = propName;
			PropInfo randomProp = objectInstance.GetComponent<PropInfo>();
			objectInstance.SetActive(false);
			randomProp.m_prefabInitialized = false;
			randomProp.InitializePrefab();

			// Add new prop to list and return direct reference.
			randomProps.Add(randomProp);
			return randomProp;
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

			// Instantiate existing tree and use as base for new tree.
			GameObject treeInstance = Object.Instantiate(PrefabCollection<TreeInfo>.FindLoaded("Tree2variant").gameObject);
			treeInstance.name = treeName;
			TreeInfo randomTree = treeInstance.GetComponent<TreeInfo>();
			treeInstance.SetActive(false);
			randomTree.m_prefabInitialized = false;
			randomTree.InitializePrefab();

			// Add new tree to list and return direct reference.
			randomTrees.Add(randomTree);
			return randomTree;
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
