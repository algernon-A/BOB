// <copyright file="NetworkPackReplacement.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System.Collections.Generic;
    using System.Linq;
    using AlgernonCommons;

    /// <summary>
    /// Class to manage replacement packs.
    /// </summary>
    internal class NetworkPackReplacement : NetworkReplacementBase
    {
        // Master dictionary of prop pack replacements.
        private Dictionary<string, Dictionary<PrefabInfo, BOBPackFile.PropReplacement>> _packRecords;

        // Master dictionary of replaced prop references.
        private Dictionary<PrefabInfo, BOBConfig.NetReplacement> _replacements;

        // Pack status dictionaries.
        private Dictionary<string, bool> _packEnabled;
        private Dictionary<string, bool> _packNotAllLoaded;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkPackReplacement"/> class.
        /// Constructor - initializes instance reference.
        /// </summary>
        internal NetworkPackReplacement()
        {
            Instance = this;
            Setup();
        }

        /// <summary>
        /// Gets the active instance.
        /// </summary>
        internal static NetworkPackReplacement Instance { get; private set; }

        /// <summary>
        /// Gets the config file list of elements relevant to the current replacement type.
        /// </summary>
        protected override List<BOBConfig.NetworkElement> NetworkElementList => null;

        /// <summary>
        /// Gets the priority level of this replacmeent type.
        /// </summary>
        protected override ReplacementPriority ThisPriority => ReplacementPriority.PackReplacement;

        /// <summary>
        /// Returns the current status of the named replacement pack.
        /// </summary>
        /// <param name="packName">Replacement pack name.</param>
        /// <returns>True if enabled, false otherwise.</returns>
        internal bool GetPackStatus(string packName)
        {
            if (_packEnabled.ContainsKey(packName))
            {
                return _packEnabled[packName];
            }

            return false;
        }

        /// <summary>
        /// Sets the status of the named replacement pack.
        /// </summary>
        /// <param name="packName">Replacement pack name.</param>
        /// <param name="status">True to enable, false to disable.</param>
        internal void SetPackStatus(string packName, bool status)
        {
            // Only do stuff if there's an actual change.
            if (status != _packEnabled[packName])
            {
                // Enabling or disabling?
                if (status == true)
                {
                    // Enable the pack; leave packStatus as false if application wasn't successful.
                    _packEnabled[packName] = ApplyPack(packName);
                }
                else
                {
                    // Disable the pack.
                    RevertPack(packName);
                    _packEnabled[packName] = false;
                }
            }
        }

        /// <summary>
        /// Checks to see if all replacement props are currently loaded for the specified pack.
        /// </summary>
        /// <param name="packName">Replacement pack name.</param>
        /// <returns>True if all replacement props are NOT loaded, false otherwise.</returns>
        internal bool PackNotAllLoaded(string packName)
        {
            // Return dictionary entry, if we have one.
            if (_packNotAllLoaded.ContainsKey(packName))
            {
                return _packNotAllLoaded[packName];
            }

            // If we got here, no 'not all loaded' flag was found.
            return false;
        }

        /// <summary>
        /// Returns a list of currently installed packs as a FastList for display.
        /// </summary>
        /// <returns>FastList of installed prop packs.</returns>
        internal FastList<object> GetPackFastList()
        {
            // Create return list from signPacks array.
            FastList<object> fastList = new FastList<object>()
            {
                m_buffer = _packRecords.Keys.OrderBy(x => x).ToArray(),
                m_size = _packRecords.Count(),
            };
            return fastList;
        }

        /// <summary>
        /// Checks to see if the given replacement pack conflicts with an active pack.
        /// </summary>
        /// <param name="packName">Pack name to check.</param>
        /// <returns>True if the pack conflicts with an active pack, false if no conflicts.</returns>
        internal bool Conflicts(string packName)
        {
            // Check for conflicts - iterate through all prefabs in this pack.
            foreach (PrefabInfo prefab in _packRecords[packName].Keys)
            {
                // Check for a currently applied replacement of the same prefab.
                if (_replacements.ContainsKey(prefab))
                {
                    // Found one!  Log message and return true to indicate conflict.
                    Logging.Message("replacement pack conflict with ", packName, " for prefab ", prefab.name);
                    return true;
                }
            }

            // If we got here, then no conflict was detected.
            return false;
        }

        /// <summary>
        /// Serializes the list of active replacement packs into a string list suitable for XML serialization.
        /// </summary>
        /// <returns>New string list of active replacement pack names.</returns>
        internal List<string> SerializeActivePacks()
        {
            // Return list.
            List<string> activePacks = new List<string>();

            // Iterate through all pack settings.
            foreach (KeyValuePair<string, bool> entry in _packEnabled)
            {
                // Look for enabled packs (value is true).
                if (entry.Value)
                {
                    // Add to list.
                    activePacks.Add(entry.Key);
                }
            }

            return activePacks;
        }

        /// <summary>
        /// Deserializes a list of active replacement packs.
        /// </summary>
        /// <param name="activePacks">List of pack names to deserialize.</param>
        internal void DeserializeActivePacks(List<string> activePacks)
        {
            // Iterate through the list of active packs.
            foreach (string packName in activePacks)
            {
                // See if we currently have this pack loaded.
                if (_packEnabled.ContainsKey(packName))
                {
                    // Yes - activate it.
                    SetPackStatus(packName, true);
                }
                else
                {
                    Logging.Message("couldn't find replacement pack ", packName);
                }
            }
        }

        /// <summary>
        /// Reverts all active pack replacements and re-initialises the master dictionaries.
        /// </summary>
        internal override void RevertAll()
        {
            // Iterate through each entry in the master pack dictionary.
            foreach (string packName in _packEnabled.Keys)
            {
                // Directly revert any applied packs (don't worry about properly processing dictionaries since we'll be wiping them anyway, and besides, if we try to change them while doing this it'll lead to out-of-sync errors).
                if (_packEnabled[packName])
                {
                    RevertPack(packName);
                }
            }

            // Re-initialise the dictionaries.
            Setup();
        }

        /// <summary>
        /// Dummy entry - DO NOT USE.
        /// </summary>
        /// <param name="replacement">Replacement record to apply.</param>
        protected override void ApplyReplacement(BOBConfig.NetReplacement replacement)
        {
        }

        /// <summary>
        /// Finds any existing replacement relevant to the provided arguments.
        /// </summary>
        /// <param name="netInfo">Network info.</param>
        /// <param name="laneIndex">Lane index.</param>
        /// <param name="propIndex">Prop index.</param>
        /// <param name="targetInfo">Target prop/tree prefab.</param>
        /// <returns>Existing replacement entry, if one was found, otherwise null.</returns>
        protected override BOBConfig.NetReplacement FindReplacement(NetInfo netInfo, int laneIndex, int propIndex, PrefabInfo targetInfo) =>
            ReplacementList(netInfo)?.Find(x => x.TargetInfo == targetInfo);

        /// <summary>
        /// Applies a replacement pack.
        /// </summary>
        /// <returns>True if the pack was successfully applied, false otherwise.</returns>
        private bool ApplyPack(string packName)
        {
            // Check for valid value.
            if (!string.IsNullOrEmpty(packName) && _packRecords.ContainsKey(packName))
            {
                // Check for conflicts with a currently applied replacement.
                if (Conflicts(packName))
                {
                    // Conflict detected - do nothing and return false to indicate no application.
                    return false;
                }

                Logging.Message("applying pack ", packName);

                // Iterate through each entry in pack and apply.
                foreach (KeyValuePair<PrefabInfo, BOBPackFile.PropReplacement> entry in _packRecords[packName])
                {
                    Apply(entry.Key, entry.Value.ReplacementInfo, entry.Value.Rotation, entry.Value.Xoffset, entry.Value.Yoffset, entry.Value.Zoffset, entry.Value.Hide ? 0 : 100);
                }

                // Return true to indicate sucessful application.
                return true;
            }

            // If we got here, then application wasn't successful.
            return false;
        }

        /// <summary>
        /// Reverts a replacement pack.
        /// </summary>
        private void RevertPack(string packName)
        {
            // Check for valid value.
            if (!string.IsNullOrEmpty(packName) && _packRecords.ContainsKey(packName))
            {
                // Iterate through each entry in pack and revert.
                foreach (KeyValuePair<PrefabInfo, BOBPackFile.PropReplacement> entry in _packRecords[packName])
                {
                    Revert(entry.Key);
                }
            }
        }

        /// <summary>
        /// Reverts a pack replacement.
        /// </summary>
        /// <param name="target">Targeted (original) tree/prop prefab.</param>
        private void Revert(PrefabInfo target)
        {
            // Don't revert if there's no entry for this reference.
            if (_replacements.TryGetValue(target, out BOBConfig.NetReplacement packReplacement))
            {
                // Revert the replacement.
                NetHandlers.RemoveReplacement(packReplacement);

                // Remove entry from dictionary.
                _replacements.Remove(target);
            }
        }

        /// <summary>
        /// Performs setup, loads pack files, and initialises the dictionaries.  Must be called prior to use.
        /// </summary>
        private void Setup()
        {
            // Initialise dictionaries.
            _packRecords = new Dictionary<string, Dictionary<PrefabInfo, BOBPackFile.PropReplacement>>();
            _replacements = new Dictionary<PrefabInfo, BOBConfig.NetReplacement>();
            _packEnabled = new Dictionary<string, bool>();
            _packNotAllLoaded = new Dictionary<string, bool>();

            // Read config files.
            List<BOBPackFile> packFiles = PackUtils.LoadPackFiles();

            foreach (BOBPackFile packFile in packFiles)
            {
                // Iterate through each prop pack loaded from the settings file.
                foreach (BOBPackFile.PropPack propPack in packFile.PropPacks)
                {
                    // Check to see if we already have a record for this pack.
                    if (_packRecords.ContainsKey(propPack.Name))
                    {
                        // Yes - log the message and carry on.
                        Logging.Message("duplicate record for replacement pack with name", propPack.Name);
                    }
                    else
                    {
                        // No - add pack to our records.
                        _packRecords.Add(propPack.Name, new Dictionary<PrefabInfo, BOBPackFile.PropReplacement>());
                        _packEnabled.Add(propPack.Name, false);
                    }

                    // Iterate through each replacement in the pack.
                    for (int i = 0; i < propPack.PropReplacements.Count; ++i)
                    {
                        // Get reference.
                        BOBPackFile.PropReplacement propReplacement = propPack.PropReplacements[i];

                        // Can we find both target and replacment?
                        PrefabInfo targetInfo = propReplacement.IsTree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(propReplacement.TargetName) : PrefabCollection<PropInfo>.FindLoaded(propReplacement.TargetName);
                        propReplacement.ReplacementInfo = propReplacement.IsTree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(propReplacement.ReplacementName) : PrefabCollection<PropInfo>.FindLoaded(propReplacement.ReplacementName);
                        if (targetInfo != null)
                        {
                            if (propReplacement.ReplacementInfo == null)
                            {
                                // Replacement prop not found - flag that pack wasn't all loaded.
                                if (!_packNotAllLoaded.ContainsKey(propPack.Name))
                                {
                                    _packNotAllLoaded.Add(propPack.Name, true);
                                }
                            }
                            else
                            {
                                // Target and replacment both found - add this replacmeent to our pack dictionary entry.
                                if (_packRecords[propPack.Name].ContainsKey(targetInfo))
                                {
                                    // Skip any duplicates.
                                    Logging.Error("duplicate replacement ", targetInfo.name, " in replacement pack ", propPack.Name);
                                }
                                else
                                {
                                    _packRecords[propPack.Name].Add(targetInfo, propReplacement);
                                }
                            }
                        }
                    }

                    // Check to make sure we have at least one replacement; if not, remove the pack from our records.
                    if (_packRecords[propPack.Name].Count == 0)
                    {
                        _packRecords.Remove(propPack.Name);
                        _packEnabled.Remove(propPack.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Applies a new (or updated) pack replacement; basically an all-network replacement.
        /// </summary>
        /// <param name="target">Targeted (original) prop prefab.</param>
        /// <param name="replacement">Replacment prop prefab.</param>
        /// <param name="angle">Replacment prop angle adjustment.</param>
        /// <param name="offsetX">Replacment X position offset.</param>
        /// <param name="offsetY">Replacment Y position offset.</param>
        /// <param name="offsetZ">Replacment Z position offset.</param>
        /// <param name="probability">Replacement probability.</param>
        private void Apply(PrefabInfo target, PrefabInfo replacement, float angle, float offsetX, float offsetY, float offsetZ, int probability)
        {
            // Make sure that target and replacement are the same type before doing anything.
            if (target == null || replacement == null || (target is TreeInfo && !(replacement is TreeInfo)) || ((target is PropInfo) && !(replacement is PropInfo)))
            {
                return;
            }

            Logging.Message("applying pack replacement for ", target.name);

            // Check to see if we already have a replacement entry for this prop - if so, revert the replacement first.
            if (_replacements.ContainsKey(target))
            {
                Revert(target);
            }

            // Create new dictionary entry if none already exists.
            if (!_replacements.ContainsKey(target))
            {
                _replacements.Add(target, new BOBConfig.NetReplacement());
            }

            // Add/replace dictionary replacement data.
            _replacements[target].IsTree = target is TreeInfo;
            _replacements[target].TargetInfo = target;
            _replacements[target].Target = target.name;
            _replacements[target].Angle = angle;
            _replacements[target].OffsetX = offsetX;
            _replacements[target].OffsetY = offsetY;
            _replacements[target].OffsetZ = offsetZ;
            _replacements[target].Probability = probability;
            _replacements[target].RepeatDistance = -1;

            // Record replacement prop.
            _replacements[target].ReplacementInfo = replacement;
            _replacements[target].ReplacementName = replacement.name;

            // Don't do anything if prefabs can't be found.
            if (_replacements[target]?.TargetInfo == null || _replacements[target].ReplacementInfo == null)
            {
                return;
            }

            // Iterate through each loaded network and record props to be replaced.
            for (int i = 0; i < PrefabCollection<NetInfo>.LoadedCount(); ++i)
            {
                // Get local reference.
                NetInfo netInfo = PrefabCollection<NetInfo>.GetLoaded((uint)i);

                // Skip any null networks, or netorks without lanes.
                if (netInfo?.m_lanes == null)
                {
                    continue;
                }

                // Iterate through each lane.
                for (int laneIndex = 0; laneIndex < netInfo.m_lanes.Length; ++laneIndex)
                {
                    // Local references.
                    NetInfo.Lane thisLane = netInfo.m_lanes[laneIndex];
                    NetLaneProps.Prop[] theseLaneProps = thisLane?.m_laneProps?.m_props;

                    // If no props in this lane, skip it and go to the next one.
                    if (theseLaneProps == null)
                    {
                        continue;
                    }

                    // Iterate through each prop in lane.
                    for (int propIndex = 0; propIndex < theseLaneProps.Length; ++propIndex)
                    {
                        // Local reference.
                        NetLaneProps.Prop thisLaneProp = theseLaneProps[propIndex];

                        // If invalid entry, skip this one.
                        if (thisLaneProp == null)
                        {
                            continue;
                        }

                        // Note current props.
                        TreeInfo thisTree = thisLaneProp.m_tree;
                        PropInfo thisProp = thisLaneProp.m_prop;

                        // Get any active handler.
                        LanePropHandler handler = NetHandlers.GetHandler(thisLane, propIndex);
                        if (handler != null)
                        {
                            // Active handler found - use original values for checking eligibility (instead of currently active values).
                            thisTree = handler.OriginalTree;
                            thisProp = handler.OriginalProp;
                        }

                        // See if this prop matches our replacement.
                        bool treeMatch = _replacements[target].IsTree && thisTree != null && thisTree == _replacements[target].TargetInfo;
                        bool propMatch = !_replacements[target].IsTree && thisProp != null && thisProp == _replacements[target].TargetInfo;
                        if (treeMatch | propMatch)
                        {
                            // Match!  Create new handler if there wasn't an existing one.
                            if (handler == null)
                            {
                                handler = NetHandlers.GetOrAddHandler(_replacements[target].NetInfo, thisLane, propIndex);
                            }

                            // Set the new replacement.
                            handler.SetReplacement(_replacements[target], ThisPriority);
                        }
                    }
                }
            }
        }
    }
}