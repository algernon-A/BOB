using System.Collections.Generic;
using ColossalFramework;
using EManagersLib.API;


namespace BOB
{
	/// <summary>
	/// Class to manage map prop replacements.
	/// </summary>
	internal class MapPropReplacement
	{
		// Instance reference.
		internal static MapPropReplacement instance;

		// Master dictionary of replaced tree prefabs.
		internal Dictionary<PropInfo, PropInfo> replacements;


		/// <summary>
		/// Constructor - initializes instance reference and calls initial setup.
		/// </summary>
		internal MapPropReplacement()
		{
			instance = this;
			Setup();
		}


		/// <summary>
		/// Performs setup and initialises the master dictionary.  Must be called prior to use.
		/// </summary>
		internal void Setup()
		{
			replacements = new Dictionary<PropInfo, PropInfo>();
		}


		/// <summary>
		/// Applies a new (or updated) map prop replacement.
		/// </summary>
		/// <param name="target">Prop to replace</param>
		/// <param name="replacement">Replacement prop</param>
		internal void Apply(PropInfo target, PropInfo replacement)
		{
			// Null checks.
			if (target == null || replacement == null)
			{
				Logging.Error("null parameter passet to MapPropReplacement.Apply");
			}

			// Check to see if we already have a replacement entry for this prop - if so, revert the replacement first.
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
			ReplaceProps(target, replacement);
		}


		/// <summary>
		/// Reverts a map prop replacement.
		/// </summary>
		/// <param name="tree">Applied replacment tree prefab</param>
		internal void Revert(PropInfo prop)
		{
			// Safety check.
			if (prop == null || !replacements.ContainsKey(prop))
			{
				return;
			}

			// Restore original trees.
			ReplaceProps(prop, replacements[prop]);

			// Remove dictionary entry.
			replacements.Remove(prop);
		}


		/// <summary>
		/// Checks if the given prop prefab has a currently recorded map replacement, and if so, returns the *original* prop prefab.
		/// </summary>
		/// <param name="treePrefab">Prop prefab to check</param>
		/// <returns>Original prefab if a map prop replacement is currently recorded, null if no map prop replacement is currently recorded</returns>
		internal PropInfo GetOriginal(PropInfo propPrefab)
		{
			// Safety check.
			if (propPrefab != null && replacements.ContainsKey(propPrefab))
			{
				// Return the original prefab.
				return replacements[propPrefab];
			}

			// If we got here, no entry was found - return null to indicate no active replacement.
			return null;
		}


		/// <summary>
		/// Replaces a map prop.
		/// </summary>
		/// <param name="target">Prop to replace</param>
		/// <param name="replacement">Replacement prop</param>
		protected virtual void ReplaceProps(PropInfo target, PropInfo replacement)
		{
			// Check for valid parameters.
			if (target != null && replacement != null)
			{
				// Iterate through each prop in map.
				for (uint propIndex = 0; propIndex < PropAPI.PropBufferLen; ++propIndex)
				{
					// Skip non-existent props (those with no flags).
					if (PropAPI.Wrapper.GetFlags(propIndex) == (ushort)PropInstance.Flags.None)
					{
						continue;
					}

					// If props matches, replace!
					if (PropAPI.Wrapper.GetInfo(propIndex) == target)
					{
						// Replace via direct reference to buffer, EPropInstance[] or PropInstance[] depending on whether or not EML is intalled.
						if (PropAPI.m_isEMLInstalled)
                        {
							EMLPropWrapper.m_defBuffer[propIndex].Info = replacement;
                        }
						else
						{
							DefPropWrapper.m_defBuffer[propIndex].Info = replacement;
						}

						// Update prop render (to update LOD) via simulation thread, creating local propID reference to avoid race condition.
						uint propID = propIndex;
						Singleton<SimulationManager>.instance.AddAction(delegate { PropAPI.Wrapper.UpdatePropRenderer(propID, true); });
					}
				}
			}
		}
	}
}
