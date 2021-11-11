﻿using System.Collections.Generic;
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
		/// <param name="replacement">Replacement tree</param>
		internal void Apply(PropInfo target, PropInfo replacement)
		{
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
				// Local references.
				PropManager propManager = Singleton<PropManager>.instance;
				PropInstance[] props = propManager.m_props.m_buffer;

				// Iterate through each prop in map.
				for (uint propIndex = 0; propIndex < props.Length; ++propIndex)
				{
					// Local reference.
					PropInstance prop = props[propIndex];

					// Skip non-existent trees (those with no flags).
					if (prop.m_flags == (ushort)PropInstance.Flags.None)
					{
						continue;
					}

					// If tree matches, replace!
					if (prop.Info == target)
					{
						props[propIndex].Info = replacement;

						// Update prop render (to update LOD) via simulation thread, creating local propID reference to avoid race condition.

						// Using EPropManager?
						if (ModSettings.ePropManager)
						{
							// EPropManager.
							uint propID = (ushort)propIndex;
							Singleton<SimulationManager>.instance.AddAction(delegate { EPropManager.UpdatePropRenderer(Singleton<PropManager>.instance, propID, true); });
						}
						else
						{
							// Vanilla PropManager.
							ushort propID = (ushort)propIndex;
							Singleton<SimulationManager>.instance.AddAction(delegate { Singleton<PropManager>.instance.UpdatePropRenderer(propID, true); });
						}
					}
				}
			}
		}
	}
}
