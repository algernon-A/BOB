// <copyright file="ConfigurationUtils.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Serialization;
    using AlgernonCommons;
    using ColossalFramework;

    /// <summary>
    /// XML serialization/deserialization utilities class.
    /// </summary>
    internal static class ConfigurationUtils
    {
        // Filenames and locations.
        private static readonly string UserDirPath = ColossalFramework.IO.DataLocation.localApplicationData;
        private static readonly string GeneralConfigName = "BOB-config.xml";
        private static readonly string GeneralConfigFile = Path.Combine(UserDirPath, GeneralConfigName);
        private static readonly string ConfigDirectory = Path.Combine(UserDirPath, "BOBconfigs");

        /// <summary>
        /// Gets the currently active configuration file.
        /// </summary>
        internal static BOBConfig CurrentConfig { get; private set; }

        /// <summary>
        /// Gets or sets the configuration file name to save as default for current save.
        /// </summary>
        internal static string CurrentSavedConfigName { get; set; }

        /// <summary>
        /// Loads configuration from the current configuration file (will default to global config if CurrentSavedConfigName is null).
        /// </summary>
        internal static void LoadConfig() => LoadConfig(CurrentSavedConfigName);

        /// <summary>
        /// Loads configuration from the specified configuration file.
        /// </summary>
        /// <param name="config">Configuration name; null for default file (default null).</param>
        internal static void LoadConfig(string config = null)
        {
            // Default file location is the general config file.
            string fileName = GeneralConfigFile;

            try
            {
                // See if we've got an assigned custom config.
                if (config == null)
                {
                    // No assigned  custom config; check if general config exists in userdir.
                    if (!File.Exists(fileName))
                    {
                        // Userdir config doesn't exist; try copying file from application directory.
                        if (File.Exists(GeneralConfigName))
                        {
                            fileName = GeneralConfigName;
                        }
                    }
                }
                else
                {
                    // Custom config provided - use this filename in the configuration settings directory.
                    fileName = FullConfigPath(config);
                }

                // Check to see if configuration file exists.
                if (File.Exists(fileName))
                {
                    // Read it.
                    using (StreamReader reader = new StreamReader(fileName))
                    {
                        Logging.KeyMessage("loading configuration file ", fileName);
                        CurrentConfig = (BOBConfig)new XmlSerializer(typeof(BOBConfig)).Deserialize(reader);

                        // If we couldn't read it, log error and create new empty config.
                        if (CurrentConfig == null)
                        {
                            Logging.Error("couldn't deserialize XML configuration file");
                            CurrentConfig = new BOBConfig();
                            return;
                        }

                        // Deserialise random prefabs.
                        RandomPrefabs.DeserializeRandomProps(CurrentConfig.RandomProps);
                        RandomPrefabs.DeserializeRandomTrees(CurrentConfig.RandomTrees);

                        // Deserialize scaling.
                        Scaling.Instance.DeserializeProps(CurrentConfig.PropScales);
                        Scaling.Instance.DeserializeTrees(CurrentConfig.TreeScales);

                        // Deserialize all-building replacements.
                        AllBuildingReplacement.Instance.Deserialize(CurrentConfig.AllBuildingProps);

                        // Deserialise building replacements.
                        GroupedBuildingReplacement.Instance.Deserialize(CurrentConfig.Buildings);

                        // Deserialise individual building prop replacements.
                        IndividualBuildingReplacement.Instance.Deserialize(CurrentConfig.IndBuildings);

                        // Deserialise all-network replacements.
                        AllNetworkReplacement.Instance.Deserialize(CurrentConfig.AllNetworkProps);

                        // Deserialise network replacements.
                        GroupedNetworkReplacement.Instance.Deserialize(CurrentConfig.Networks);

                        // Deserialise individual network replacements.
                        IndividualNetworkReplacement.Instance.Deserialize(CurrentConfig.IndNetworks);

                        // Deserialise active replacement packs.
                        NetworkPackReplacement.Instance.DeserializeActivePacks(CurrentConfig.ActivePacks);

                        // Deserialize added props.
                        AddedBuildingProps.Instance.Deserialize(CurrentConfig.AddedBuildingProps);

                        // Deserialize added props.
                        AddedNetworkProps.Instance.Deserialize(CurrentConfig.AddedNetworkProps);
                    }
                }
                else
                {
                    Logging.Message("configuration file ", fileName ?? "null", " not found");

                    // Create a new empty configuration file record.
                    CurrentConfig = new BOBConfig();
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception reading XML configuration file ", fileName ?? "null");

                // If we didn't even get as far as creating a configuration file instanece, create a new blank one now.
                if (CurrentConfig == null)
                {
                    CurrentConfig = new BOBConfig();
                }
            }
        }

        /// <summary>
        /// Creates a blank BOB configuration file.
        /// </summary>
        /// <param name="configName">New configuration file name.</param>
        internal static void BlankConfig(string configName)
        {
            if (!configName.IsNullOrWhiteSpace())
            {
                // Get full pathname for new config file.
                string fileName = GetConfigPath(configName);

                try
                {
                    using (StreamWriter textWriter = new StreamWriter(fileName, append: false))
                    {
                        XmlSerializer xmlSerializer = new XmlSerializer(typeof(BOBConfig));

                        BOBConfig blankFile = new BOBConfig();

                        // Write to file.
                        xmlSerializer.Serialize(textWriter, blankFile);
                    }
                }
                catch (Exception e)
                {
                    Logging.LogException(e, "exception saving XML configuration file ", fileName ?? "null");
                }
            }
        }

        /// <summary>
        /// Saves current configuration to the current configuration file; will default to general configuration file if CurrentSavedConfigName is null.
        /// </summary>
        internal static void SaveConfig() => SaveConfig(CurrentSavedConfigName);

        /// <summary>
        /// Save current configuration to the specified config file.
        /// </summary>
        /// <param name="config">Configuration file name; null for default file (default null).</param>
        internal static void SaveConfig(string config = null)
        {
            // Default file location is the general config file.
            string fileName = GeneralConfigFile;

            try
            {
                // Check if we've got an assigned custom config.
                if (config != null)
                {
                    // Get full filepath.
                    fileName = GetConfigPath(config);
                }

                // Open specified file.
                using (StreamWriter textWriter = new StreamWriter(fileName, append: false))
                {
                    Logging.KeyMessage("saving configuration file ", fileName);
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(BOBConfig));

                    // Create new config if there isn't one.
                    if (CurrentConfig == null)
                    {
                        Logging.Message("CurrentConfig was null; generating new configuration");
                        CurrentConfig = new BOBConfig();
                    }

                    // Serialise scales.
                    try
                    {
                        CurrentConfig.PropScales = Scaling.Instance.SerializePropScales();
                        CurrentConfig.TreeScales = Scaling.Instance.SerializeTreeScales();
                    }
                    catch (Exception e)
                    {
                        // Don't let a single failure stop us.
                        Logging.LogException(e, "exception serializing scaling elements");
                    }

                    // Serialise active replacement packs.
                    try
                    {
                        CurrentConfig.ActivePacks = NetworkPackReplacement.Instance.SerializeActivePacks();
                    }
                    catch (Exception e)
                    {
                        // Don't let a single failure stop us.
                        Logging.LogException(e, "exception serializing active replacement packs");
                    }

                    // Write to file.
                    xmlSerializer.Serialize(textWriter, CurrentConfig);
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception saving XML configuration file ", fileName ?? "null");
            }

            // Display any exception message that occured during save.
            BOBPanelManager.CheckException();
        }

        /// <summary>
        /// Copies the selected configuration to a new file.
        /// </summary>
        /// <param name="configName">Configuration to copy.</param>
        /// <param name="newConfigName">New copy name.</param>
        /// <returns>Error message if copying was unsuccessful, null if copy was successful.</returns>
        internal static string CopyCurrent(string configName, string newConfigName)
        {
            try
            {
                // Make sure source exists, and destination file doesn't, before copying.
                string sourceConfig = FullConfigPath(configName);
                string newConfig = FullConfigPath(newConfigName);

                // Make sure source exists.
                if (File.Exists(sourceConfig))
                {
                    if (!File.Exists(newConfig))
                    {
                        // All good - copy file and return null to indicate success.
                        File.Copy(sourceConfig, newConfig);
                        return null;
                    }
                    else
                    {
                        return "File already exists";
                    }
                }
                else
                {
                    return "Source file not found";
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception copying XML configuration file");
                return "Error copying file";
            }
        }

        /// <summary>
        /// Deletes the specified config's file.
        /// </summary>
        /// <param name="configName">Config to delete.</param>
        internal static void DeleteConfig(string configName)
        {
            try
            {
                File.Delete(FullConfigPath(configName));
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception deleting config file");
            }
        }

        /// <summary>
        /// Returns a list of valid BOB config files in the configuration directory.
        /// </summary>
        /// <returns>Fastlis of valid BOB config file names, sorted alphabetically.</returns>
        internal static FastList<object> GetConfigFastList()
        {
            List<string> fileList = new List<string>();

            // Get BOB directory.
            if (Directory.Exists(ConfigDirectory))
            {
                // Directory exists; parse each file in directory, looking for xml.
                string[] fileNames = Directory.GetFiles(ConfigDirectory, "*.xml");
                for (int i = 0; i < fileNames.Length; ++i)
                {
                    // Local reference.
                    string fileName = fileNames[i];

                    try
                    {
                        // Try to read the file as a BOB configuration file.
                        using (StreamReader reader = new StreamReader(fileName))
                        {
                            // Read config file and add to list.
                            BOBConfig configFile = (BOBConfig)new XmlSerializer(typeof(BOBConfig)).Deserialize(reader);
                            fileList.Add(Path.GetFileNameWithoutExtension(fileName));
                        }
                    }
                    catch (Exception e)
                    {
                        Logging.LogException(e, "exception reading local config file ", fileName);
                    }
                }
            }

            FastList<object> fastList = new FastList<object>()
            {
                m_buffer = fileList.OrderBy(x => x).ToArray(),
                m_size = fileList.Count(),
            };
            return fastList;
        }

        /// <summary>
        /// Attempts to find the replacement prefab with the specified name.
        /// </summary>
        /// <param name="replacementName">Prefab name to find.</param>
        /// <param name="isTree">True if the desired prefab is a tree, false if it's a prop.</param>
        /// <returns>Requested prefab, or null if not found.</returns>
        internal static PrefabInfo FindReplacementPrefab(string replacementName, bool isTree)
        {
            // Null check.
            if (replacementName.IsNullOrWhiteSpace())
            {
                Logging.Error("invalid replacement prop name");
                return null;
            }

            // Attempt to load from prefab collection.
            PrefabInfo replacementPrefab = isTree ? PrefabCollection<TreeInfo>.FindLoaded(replacementName) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacementName);

            // If we couldn't find the tree/prop, try the other type instead.
            if (!replacementPrefab)
            {
                replacementPrefab = isTree ? PrefabCollection<PropInfo>.FindLoaded(replacementName) : (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacementName);
            }

            // If we couldn't load from prefab collection, attempt to find in our list of random prefabs (if we have a config file, otherwise there's no random prefabs).
            if (replacementPrefab == null && CurrentConfig != null)
            {
                replacementPrefab = CurrentConfig.RandomTrees.Find(x => x.Name.Equals(replacementName))?.Tree ?? (PrefabInfo)CurrentConfig.RandomProps.Find(x => x.Name.Equals(replacementName))?.Prop;
            }

            // Return what we have, even if it's null.
            return replacementPrefab;
        }

        /// <summary>
        /// Cleans up the current configuration by removing any entries that feature unloaded prefabs (buildings, networks, props, trees).
        /// </summary>
        internal static void Cleanup()
        {
            // Cleanup buildngs.
            // Remove replacments referring to unloaded props/trees.
            CleanReplacements(CurrentConfig.AllBuildingProps);
            CleanElements(CurrentConfig.Buildings);
            CleanElements(CurrentConfig.IndBuildings);
            CleanElements(CurrentConfig.AddedBuildingProps);

            // Cleanup networks.
            CleanReplacements(CurrentConfig.AllNetworkProps);
            CleanElements(CurrentConfig.Networks);
            CleanElements(CurrentConfig.IndNetworks);
            CleanElements(CurrentConfig.AddedNetworkProps);

            // Save config.
            SaveConfig();
        }

        /// <summary>
        /// Cleans up elements from the specified element list.
        /// </summary>
        /// <param name="elementList">Element list to clean.</param>
        private static void CleanElements<T>(List<T> elementList)
            where T : BOBConfig.ElementBase
        {
            // List of networkl elements to remove.
            List<T> removeElementList = new List<T>();

            // Clean up networks.
            foreach (T element in elementList)
            {
                // Remove any reference to unloaded prefabs, or references with null lists.
                if (element?.Prefab == null)
                {
                    removeElementList.Add(element);
                    continue;
                }

                if (element is BOBConfig.NetworkElement networkElement)
                {
                    // Remove replacments referring to unloaded props/trees.
                    CleanReplacements(networkElement.Replacements);

                    // If all replacements have been removed, remove this element as well.
                    if (networkElement.Replacements.Count == 0)
                    {
                        removeElementList.Add(element);
                    }
                }
                else if (element is BOBConfig.BuildingElement buildingElement)
                {
                    // Remove replacments referring to unloaded props/trees.
                    CleanReplacements(buildingElement.Replacements);

                    // If all replacements have been removed, remove this element as well.
                    if (buildingElement.Replacements.Count == 0)
                    {
                        removeElementList.Add(element);
                    }
                }
            }

            // Remove all replacements recorded in remove list.
            foreach (T element in removeElementList)
            {
                elementList.Remove(element);
            }
        }

        /// <summary>
        /// Removes any replacements in the specified list that refer to unloaded props or trees.
        /// </summary>
        /// <param name="replacementList">Replacement list to clean.</param>
        private static void CleanReplacements<T>(List<T> replacementList)
            where T : BOBConfig.Replacement
        {
            // Iterate through replacements for this building and remove any that refer to unloaded props/trees.
            List<T> removeReplacementList = new List<T>();
            foreach (T replacement in replacementList)
            {
                if (replacement.TargetInfo == null || replacement.ReplacementInfo == null)
                {
                    removeReplacementList.Add(replacement);
                }
            }

            // Remove all replacements recorded in remove list.
            foreach (T replacement in removeReplacementList)
            {
                replacementList.Remove(replacement);
            }
        }

        /// <summary>
        /// Returns the full filepath to the given config file, creating the config directory if it doesn't already exist.
        /// </summary>
        /// <param name="configName">Configuration file name.</param>
        /// <returns>Full config filepath.</returns>
        private static string GetConfigPath(string configName)
        {
            // Check if config directory exists.
            if (!Directory.Exists(ConfigDirectory))
            {
                // Doesn't exist - create it.
                Directory.CreateDirectory(ConfigDirectory);
            }

            return Path.Combine(ConfigDirectory, configName + ".xml");
        }

        /// <summary>
        /// Returns the absolute filepath of the config file for the given config name.
        /// </summary>
        /// <param name="configName">Config filepath.</param>
        /// <returns>Absolute config filepath.</returns>
        private static string FullConfigPath(string configName) => Path.Combine(ConfigDirectory, configName + ".xml");
    }
}
