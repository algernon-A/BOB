using System.Collections.Generic;


namespace BOB
{
	/// <summary>
	/// Class to manage prop and tree scaling.
	/// </summary>
	internal class Scaling
	{
		// Instance reference.
		internal static Scaling instance;

		// Master dictionary of replaced prop references.
		internal Dictionary<string, BOBScalingElement> treeScales, propScales;


		/// <summary>
		/// Constructor - initializes instance reference and calls initial setup.
		/// </summary>
		internal Scaling()
		{
			instance = this;
			Setup();
		}


		/// <summary>
		/// Performs setup and initialises the master dictionary.  Must be called prior to use.
		/// </summary>
		internal void Setup()
		{
			treeScales = new Dictionary<string, BOBScalingElement>();
			propScales = new Dictionary<string, BOBScalingElement>();
		}


		/// <summary>
		/// Deserialise prop scaling.
		/// </summary>
		/// <param name="elements">List of prop scaling elements to deserialise.</param>
		internal void DeserializeProps(List<BOBScalingElement> elements)
		{
			// Iterate through each element.
			foreach (BOBScalingElement element in elements)
			{
				// Add record to dictionary (to retain records of any prefabs not found).
				propScales.Add(element.prefabName, element);

				// Try to find prefab in loaded collection.
				PropInfo thisProp = PrefabCollection<PropInfo>.FindLoaded(element.prefabName);
				if (thisProp != null)
				{
					// Found it - record original values.
					element.prefab = thisProp;
					element.originalMin = thisProp.m_minScale;
					element.originalMax = thisProp.m_maxScale;

					// Apply new values.
					thisProp.m_minScale = element.minScale;
					thisProp.m_maxScale = element.maxScale;
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
				// Add record to dictionary (to retain records of any prefabs not found).
				treeScales.Add(element.prefabName, element);

				// Try to find prefab in loaded collection, and if so, apply the recorded scaling.
				TreeInfo thisTree = PrefabCollection<TreeInfo>.FindLoaded(element.prefabName);
				if (thisTree != null)
				{
					// Found it - record original values.
					element.prefab = thisTree;
					element.originalMin = thisTree.m_minScale;
					element.originalMax = thisTree.m_maxScale;

					// Apply new values.
					thisTree.m_minScale = element.minScale;
					thisTree.m_maxScale = element.maxScale;
				}
			}
		}


		/// <summary>
		/// Apply new minimum scale.
		/// </summary>
		/// <param name="prefab">Prefab to apply to</param>
		/// <param name="minScale">New minimum scale</param>
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
		/// <param name="prefab">Prefab to apply to</param>
		/// <param name="maxScale">New maximum scale</param>
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
		/// <param name="prefab">Prefab to revert</param>
		/// <param name="removeEntries">True to remove the reverted entries from the list of replacements, false to leave the list unchanged</param>
		internal void Revert(PrefabInfo prefab, bool removeEntries)
		{
			// Prop or tree?
			if (prefab is PropInfo prop)
			{
				// Prop - check if we have a record.
				if (propScales.ContainsKey(prop.name))
				{
					// Local reference.
					BOBScalingElement element = propScales[prop.name];

					// Reset prop scale.
					prop.m_minScale = element.originalMin;
					prop.m_maxScale = element.originalMax;

					// Remove record from dictionary, if we're doing so.
					if (removeEntries)
					{
						propScales.Remove(prop.name);
					}

					// Save configuration file.
					ConfigurationUtils.SaveConfig();
				}
			}
			else if (prefab is TreeInfo tree)
			{
				// Tree - check if we have a record.
				if (treeScales.ContainsKey(tree.name))
				{
					// Local reference.
					BOBScalingElement element = treeScales[tree.name];

					// Reset prop scale.
					tree.m_minScale = element.originalMin;
					tree.m_maxScale = element.originalMax;

					// Remove record from dictionary, if we're doing so.
					if (removeEntries)
					{
						treeScales.Remove(tree.name);
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
			foreach(BOBScalingElement propElement in propScales.Values)
            {
				Revert(propElement.prefab, false);
			}

			// Revert trees - iterate through each recorded element.
			foreach (BOBScalingElement treeElement in treeScales.Values)
			{
				Revert(treeElement.prefab, false);
			}

			// Reset dictionaries.
			Setup();
		}


		/// <summary>
		/// Applies scaling to props and updates the dictionary records.
		/// </summary>
		/// <param name="prop">Prop prefab</param>
		/// <param name="minScale">Minimum scale</param>
		/// <param name="maxScale">Maximum scale</param>
		private void PropScale(PropInfo prop, float minScale, float maxScale)
		{
			// If we don't have an existing record, create one.
			if (!propScales.ContainsKey(prop.name))
			{
				// Record original values.
				propScales.Add(prop.name, new BOBScalingElement
				{
					prefabName = prop.name,
					prefab = prop,
					originalMin = prop.m_minScale,
					originalMax = prop.m_maxScale
				});
			}

			// Local reference.
			BOBScalingElement element = propScales[prop.name];

			// Update record with new scale values.
			element.minScale = minScale;
			element.maxScale = maxScale;

			// Remove record if minimum and maximum scales both match the default.
			if (element.minScale == element.originalMin && element.maxScale == element.originalMax)
            {
				propScales.Remove(prop.name);
            }

			// Apply new scales and save updated configuration.
			prop.m_minScale = minScale;
			prop.m_maxScale = maxScale;
			ConfigurationUtils.SaveConfig();
		}


		/// <summary>
		/// Applies scaling to trees and updates the dictionary records.
		/// </summary>
		/// <param name="prop">Tree prefab</param>
		/// <param name="minScale">Minimum scale</param>
		/// <param name="maxScale">Maximum scale</param>
		private void TreeScale(TreeInfo tree, float minScale, float maxScale)
		{
			// If we don't have an existing record, create one.
			if (!treeScales.ContainsKey(tree.name))
			{
				// Record original values.
				treeScales.Add(tree.name, new BOBScalingElement
				{
					prefabName = tree.name,
					prefab = tree,
					originalMin = tree.m_minScale,
					originalMax = tree.m_maxScale
				});
			}

			// Local reference.
			BOBScalingElement element = treeScales[tree.name];

			// Update record with new scale values.
			element.minScale = minScale;
			element.maxScale = maxScale;

			// Remove record if minimum and maximum scales both match the default.
			if (element.minScale == element.originalMin && element.maxScale == element.originalMax)
			{
				propScales.Remove(tree.name);
			}

			// Apply new scales and save updated configuration.
			tree.m_minScale = minScale;
			tree.m_maxScale = maxScale;
			ConfigurationUtils.SaveConfig();
		}
	}
}
