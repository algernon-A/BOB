using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using ColossalFramework;


namespace BOB
{
	/// <summary>
	/// XML serialization/deserialization utilities class.
	/// </summary>
	internal static class ConfigurationUtils
	{
		// Filenames and locations.
		internal static readonly string GeneralSettingsFile = "BOB-config.xml";
		internal static readonly string ConfigDirectory = ColossalFramework.IO.DataLocation.localApplicationData + Path.DirectorySeparatorChar + ConfigDirectory + "BOBconfigs";

		internal static string currentConfig;


		/// <summary>
		/// Loads configuration from the current configuration file.
		/// </summary>
		internal static void LoadConfig() => LoadConfig(currentConfig);


		/// <summary>
		/// Loads configuration from the specified configuration file.
		/// </summary>
		/// <param name="config">Configuration name; null for default file (default null)</param>
		internal static void LoadConfig(string config = null)
		{
			try
			{
				// Set file location to save to.
				string fileName;
				if (config == null)
				{
					// No file name provided; use general settings file.
					fileName = GeneralSettingsFile;
				}
				else
				{
					// Filename provided - use this filename in the configuration settings directory.
					fileName = FullConfigPath(config);
				}

				// Check to see if configuration file exists.
				if (File.Exists(fileName))
				{
					// Read it.
					using (StreamReader reader = new StreamReader(fileName))
					{
						BOBConfigurationFile configFile = (BOBConfigurationFile)new XmlSerializer(typeof(BOBConfigurationFile)).Deserialize(reader);

						if (configFile == null)
						{
							Logging.Error("couldn't deserialize settings file");
							return;
						}

						// Deserialise random prefabs.
						PrefabLists.DeserializeRandomProps(configFile.randomProps);
						PrefabLists.DeserializeRandomTrees(configFile.randomTrees);

						// Deserialize scaling.
						Scaling.instance.DeserializeProps(configFile.propScales);
						Scaling.instance.DeserializeTrees(configFile.treeScales);

						// Deserialize all-building replacements.
						DeserializeAllBuilding(configFile.allBuildingProps);

						// Deserialise building replacements, per building.
						foreach (BOBBuildingElement building in configFile.buildings)
						{
							DeserializeBuilding(building);
						}

						// Deserialise individual building prop replacements, per building.
						foreach (BOBBuildingElement building in configFile.indBuildings)
						{
							DeserializeIndividual(building);
						}

						// Deserialise all-network replacements.
						DeserializeAllNetwork(configFile.allNetworkProps);

						// Deserialise network replacements, per network.
						foreach (BOBNetworkElement network in configFile.networks)
                        {
							DeserializeNetwork(network);
						}

						// Deserialise active replacement packs.
						PackReplacement.instance.DeserializeActivePacks(configFile.activePacks);
					}
				}
				else
				{
					Logging.Message("no configuration file found");
				}
			}
			catch (Exception e)
			{
				Logging.LogException(e, "exception reading XML configuration file");
			}
		}



		/// <summary>
		/// Saves current configuration to the current configuration file.
		/// <param name="clean">Set to true to generate a blank file (default false)</param>
		/// </summary>
		internal static void SaveConfig() => SaveConfig(currentConfig);


		/// <summary>
		/// Save current configuration to the specified config file.
		/// </summary>
		/// <param name="config">Configuration file name; null for default file (default null)</param>
		/// <param name="clean">Set to true to generate a blank file (default false)</param>
		internal static void SaveConfig(string config = null, bool clean = false)
		{
			try
			{
				// Set file location to save to.
				string fileName;
				if (config == null)
				{
					// No file name provided; use general settings file.
					fileName = GeneralSettingsFile;
				}
				else
                {
					// Filename provided - use this filename in the configuration settings directory (creating directory if it doesn't already exist).
					if (!Directory.Exists(ConfigDirectory))
					{
						Directory.CreateDirectory(ConfigDirectory);
					}
					fileName = FullConfigPath(config);
				}

				// Open specified file.
				using (StreamWriter textWriter = new StreamWriter(fileName, append: false))
				{
					XmlSerializer xmlSerializer = new XmlSerializer(typeof(BOBConfigurationFile));
					BOBConfigurationFile configFile = new BOBConfigurationFile
					{
						// Version 1.
						version = 1
					};

					// Don't populate file if we're doing a clean save.
					if (!clean)
					{
						// Serialise random prefabs.
						configFile.randomProps = PrefabLists.SerializeRandomProps();
						configFile.randomTrees = PrefabLists.SerializeRandomTrees();

						// Serialise scales.
						configFile.propScales = Scaling.instance.propScales.Values.ToList();
						configFile.treeScales = Scaling.instance.treeScales.Values.ToList();

						// Serialise all-building replacements.
						configFile.allBuildingProps = AllBuildingReplacement.replacements.Values.ToList();

						// Serialise building replacements, per building.
						configFile.buildings = new List<BOBBuildingElement>();
						foreach (BuildingInfo building in BuildingReplacement.instance.replacements.Keys)
						{
							// Create new element.
							configFile.buildings.Add(new BOBBuildingElement
							{
								building = building.name,
								replacements = BuildingReplacement.instance.replacements[building].Values.ToList()
							});
						}

						// Serialise individual building prop replacements, per building.
						configFile.indBuildings = new List<BOBBuildingElement>();
						foreach (BuildingInfo building in IndividualReplacement.instance.replacements.Keys)
						{
							// Create new element.
							configFile.indBuildings.Add(new BOBBuildingElement
							{
								building = building.name,
								replacements = IndividualReplacement.instance.replacements[building].Values.ToList()
							});
						}

						// Serialise all-network replacements.
						configFile.allNetworkProps = AllNetworkReplacement.instance.replacements.Values.ToList();

						// Serialise network replacements, per network.
						configFile.networks = new List<BOBNetworkElement>();
						foreach (NetInfo network in NetworkReplacement.instance.replacements.Keys)
						{
							// Create new element.
							configFile.networks.Add(new BOBNetworkElement
							{
								network = network.name,
								replacements = NetworkReplacement.instance.replacements[network].Values.ToList()
							});
						}

						// Serialise active replacement packs.
						configFile.activePacks = PackReplacement.instance.SerializeActivePacks();
					}

					// Write to file.
					xmlSerializer.Serialize(textWriter, configFile);
				}
			}
			catch (Exception e)
			{
				Logging.LogException(e, "exception saving XML configuration file");
			}
		}


		/// <summary>
		/// Copies the selected configuration to a new file.
		/// </summary>
		/// <param name="configName">Configuration to copy</param>
		/// <param name="newConfigName">New copy name</param>
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
				return ("Error copying file");
            }
		}

		
		/// <summary>
		/// Deletes the specified config's file.
		/// </summary>
		/// <param name="configName">Config to delete</param>
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
		/// <returns>Fastlis of valid BOB config file names, sorted alphabetically</returns>
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
							BOBConfigurationFile configFile = (BOBConfigurationFile)new XmlSerializer(typeof(BOBConfigurationFile)).Deserialize(reader);
							//if (configFile != null)
							{
								// Found valid config file; add to list.
								fileList.Add(Path.GetFileNameWithoutExtension(fileName));
							}
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
				m_size = fileList.Count()
			};
			return fastList;
		}


		/// <summary>
		/// Returns the absolute filepath of the config file for the given config name.
		/// </summary>
		/// <param name="configName">Config filepath</param>
		/// <returns></returns>
		private static string FullConfigPath(string configName) => Path.Combine(ConfigDirectory, configName + ".xml");


		/// <summary>
		/// Deserialises an all-network element list.
		/// </summary>
		/// <param name="elementList">All-network element list to deserialise</param>
		private static void DeserializeAllNetwork(List<BOBNetReplacement> elementList)
		{
			// Iterate through each element in the provided list.
			foreach (BOBNetReplacement replacement in elementList)
			{
				// Try to find target prefab.
				PrefabInfo targetPrefab = replacement.tree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacement.target) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacement.target);
				if (targetPrefab == null)
				{
					Logging.Message("Couldn't find target prefab ", replacement.target);
					continue;
				}

				// Try to find replacement prefab.
				PrefabInfo replacementPrefab = FindReplacementPrefab(replacement.Replacement, replacement.tree);
				if (replacementPrefab == null)
				{
					Logging.Message("Couldn't find replacement prefab ", replacement.Replacement);
					continue;
				}

				// If we got here, it's all good; apply the all-network replacement.
				AllNetworkReplacement.instance.Apply(targetPrefab, replacementPrefab, replacement.angle, replacement.offsetX, replacement.offsetY, replacement.offsetZ, replacement.probability);
			}
		}


		/// <summary>
		/// Deserialises a network replacement list.
		/// </summary>
		/// <param name="elementList">Network element list to deserialise</param>
		private static void DeserializeNetwork(BOBNetworkElement networkElement)
		{
			// Try to find target network.
			NetInfo networkInfo = (NetInfo)PrefabCollection<NetInfo>.FindLoaded(networkElement.network);
			if (networkInfo == null)
			{
				Logging.Message("Couldn't find target network ", networkElement.network);
				return;
			}


			// Iterate through each element in the provided list.
			foreach (BOBNetReplacement replacement in networkElement.replacements)
			{
				// Try to find target prefab.
				PrefabInfo targetPrefab = replacement.tree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacement.target) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacement.target);
				if (targetPrefab == null)
				{
					Logging.Message("Couldn't find target prefab ", replacement.target);
					continue;
				}

				// Try to find replacement prefab.
				PrefabInfo replacementPrefab = FindReplacementPrefab(replacement.Replacement, replacement.tree);
				if (replacementPrefab == null)
				{
					Logging.Message("Couldn't find replacement prefab ", replacement.Replacement);
					continue;
				}

				// If we got here, it's all good; apply the network replacement.
				NetworkReplacement.instance.Apply(networkInfo, targetPrefab, replacementPrefab, replacement.angle, replacement.offsetX, replacement.offsetY, replacement.offsetZ, replacement.probability);
			}
		}


		/// <summary>
		/// Deserialises an all-building element list.
		/// </summary>
		/// <param name="elementList">All-building element list to deserialise</param>
		private static void DeserializeAllBuilding(List<BOBBuildingReplacement> elementList)
		{
			// Iterate through each element in the provided list.
			foreach (BOBBuildingReplacement replacement in elementList)
			{
				// Try to find target prefab.
				PrefabInfo targetPrefab = replacement.tree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacement.target) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacement.target);
				if (targetPrefab == null)
				{
					Logging.Message("Couldn't find target prefab ", replacement.target);
					continue;
				}

				// Try to find replacement prefab.
				PrefabInfo replacementPrefab = FindReplacementPrefab(replacement.Replacement, replacement.tree);
				if (replacementPrefab == null)
				{
					Logging.Message("Couldn't find replacement prefab ", replacement.Replacement);
					continue;
				}

				// If we got here, it's all good; apply the all-network replacement.
				Logging.Message("applying all-building replacement ", targetPrefab.name, " to ", replacementPrefab.name);
				AllBuildingReplacement.instance.Apply(targetPrefab, replacementPrefab, replacement.angle, replacement.offsetX, replacement.offsetY, replacement.offsetZ, replacement.probability);
			}
		}


		/// <summary>
		/// Deserialises a building replacement list.
		/// </summary>
		/// <param name="elementList">Building element list to deserialise</param>
		private static void DeserializeBuilding(BOBBuildingElement buildingElement)
		{
			// Try to find target network.
			BuildingInfo buildingInfo = (BuildingInfo)PrefabCollection<BuildingInfo>.FindLoaded(buildingElement.building);
			if (buildingInfo == null)
			{
				Logging.Message("Couldn't find target building ", buildingElement.building);
				return;
			}


			// Iterate through each element in the provided list.
			foreach (BOBBuildingReplacement replacement in buildingElement.replacements)
			{
				// Try to find target prefab.
				PrefabInfo targetPrefab = replacement.tree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacement.target) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacement.target);
				if (targetPrefab == null)
				{
					Logging.Message("Couldn't find target prefab ", replacement.target);
					continue;
				}

				// Try to find replacement prefab.
				PrefabInfo replacementPrefab = FindReplacementPrefab(replacement.Replacement, replacement.tree);
				if (replacementPrefab == null)
				{
					Logging.Message("Couldn't find replacement prefab ", replacement.Replacement);
					continue;
				}

				// If we got here, it's all good; apply the building replacement.
				Logging.Message("applying building replacement ", targetPrefab.name, " to ", replacementPrefab.name, " in ", buildingInfo.name);
				BuildingReplacement.instance.Apply(buildingInfo, targetPrefab, replacementPrefab, replacement.angle, replacement.offsetX, replacement.offsetY, replacement.offsetZ, replacement.probability);
			}
		}


		/// <summary>
		/// Deserialises an individual building prop replacement list.
		/// </summary>
		/// <param name="elementList">Building element list to deserialise</param>
		private static void DeserializeIndividual(BOBBuildingElement buildingElement)
		{
			// Try to find target network.
			BuildingInfo buildingInfo = (BuildingInfo)PrefabCollection<BuildingInfo>.FindLoaded(buildingElement.building);
			if (buildingInfo == null)
			{
				Logging.Message("Couldn't find target building ", buildingElement.building);
				return;
			}

			// Iterate through each element in the provided list.
			foreach (BOBBuildingReplacement replacement in buildingElement.replacements)
			{
				// Try to find target prefab.
				PrefabInfo targetPrefab = replacement.tree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacement.target) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacement.target);
				if (targetPrefab == null)
				{
					Logging.Message("Couldn't find target prefab ", replacement.target);
					continue;
				}

				// Try to find replacement prefab.
				PrefabInfo replacementPrefab = FindReplacementPrefab(replacement.Replacement, replacement.tree);
				if (replacementPrefab == null)
				{
					Logging.Message("Couldn't find replacement prefab ", replacement.Replacement);
					continue;
				}

				// If we got here, it's all good; apply the building replacement.
				IndividualReplacement.instance.Apply(buildingInfo, targetPrefab, replacement.index, replacementPrefab, replacement.angle, replacement.offsetX, replacement.offsetY, replacement.offsetZ, replacement.probability);
			}
		}


		/// <summary>
		/// Attempts to find the replacement prefab with the specified name.
		/// </summary>
		/// <param name="replacementName">Prefab name to find</param>
		/// <param name="isTree">True if the desired prefab is a tree, false if it's a prop</param>
		/// <returns>Requested prefab, or null if not found</returns>
		private static PrefabInfo FindReplacementPrefab(string replacementName, bool isTree)
        {
			// Null check.
			if (replacementName.IsNullOrWhiteSpace())
            {
				Logging.Error("invalid replacement prop name");
				return null;
            }

			// Attempt to load from prefab collection.
			PrefabInfo replacementPrefab = isTree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacementName) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacementName);

			if (replacementPrefab == null)
			{
				// If we couldn't load from prefab collection, attempt to find in our list of replacement prefabs.
				replacementPrefab = isTree ? (PrefabInfo)PrefabLists.randomTrees.Find(x => x.name.Equals(replacementName)).tree : (PrefabInfo)PrefabLists.randomProps.Find(x => x.name.Equals(replacementName)).prop;
			}

			// Return what we have.
			return replacementPrefab;
		}
	}
}
