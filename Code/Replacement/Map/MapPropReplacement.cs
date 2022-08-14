// <copyright file="MapPropReplacement.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System.Collections.Generic;
    using AlgernonCommons;
    using ColossalFramework;
    using EManagersLib.API;

    /// <summary>
    /// Class to manage map prop replacements.
    /// </summary>
    internal class MapPropReplacement
    {
        // Master dictionary of replaced tree prefabs.
        private readonly Dictionary<PropInfo, PropInfo> _replacements;

        /// <summary>
        /// Initializes a new instance of the <see cref="MapPropReplacement"/> class.
        /// Constructor - initializes instance reference and calls initial setup.
        /// </summary>
        internal MapPropReplacement()
        {
            Instance = this;
            _replacements = new Dictionary<PropInfo, PropInfo>();
        }

        /// <summary>
        /// Gets the active instance.
        /// </summary>
        internal static MapPropReplacement Instance { get; private set; }

        /// <summary>
        /// Gets the replacements dictionary.
        /// </summary>
        internal Dictionary<PropInfo, PropInfo> Replacements => _replacements;

        /// <summary>
        /// Applies a new (or updated) map prop replacement.
        /// </summary>
        /// <param name="target">Prop to replace.</param>
        /// <param name="replacement">Replacement prop.</param>
        internal void Apply(PropInfo target, PropInfo replacement)
        {
            // Null checks.
            if (target == null || replacement == null)
            {
                Logging.Error("null parameter passet to MapPropReplacement.Apply");
            }

            // Check to see if we already have a replacement entry for this prop - if so, revert the replacement first.
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
            ReplaceProps(target, replacement);
        }

        /// <summary>
        /// Reverts a map prop replacement.
        /// </summary>
        /// <param name="prop">Applied replacment tree prefab.</param>
        internal void Revert(PropInfo prop)
        {
            // Safety check.
            if (prop == null || !_replacements.ContainsKey(prop))
            {
                return;
            }

            // Restore original trees.
            ReplaceProps(prop, _replacements[prop]);

            // Remove dictionary entry.
            _replacements.Remove(prop);
        }

        /// <summary>
        /// Checks if the given prop prefab has a currently recorded map replacement, and if so, returns the *original* prop prefab.
        /// </summary>
        /// <param name="propPrefab">Prop prefab to check.</param>
        /// <returns>Original prefab if a map prop replacement is currently recorded, null if no map prop replacement is currently recorded.</returns>
        internal PropInfo GetOriginal(PropInfo propPrefab)
        {
            // Safety check.
            if (propPrefab != null && _replacements.ContainsKey(propPrefab))
            {
                // Return the original prefab.
                return _replacements[propPrefab];
            }

            // If we got here, no entry was found - return null to indicate no active replacement.
            return null;
        }

        /// <summary>
        /// Replaces a map prop.
        /// </summary>
        /// <param name="target">Prop to replace.</param>
        /// <param name="replacement">Replacement prop.</param>
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
                        Singleton<SimulationManager>.instance.AddAction(() => PropAPI.Wrapper.UpdatePropRenderer(propID, true));
                    }
                }
            }
        }
    }
}
