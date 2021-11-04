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
		internal static readonly string GeneralConfigFile = "BOB-config.xml";
		internal static readonly string ConfigDirectory = ColossalFramework.IO.DataLocation.localApplicationData + Path.DirectorySeparatorChar + ConfigDirectory + "BOBconfigs";


		/// <summary>
		/// Currently active configuration file.
		/// </summary>
		internal static BOBConfigurationFile CurrentConfig { get; private set; }


		/// <summary>
		/// Configuration file name to save as default for current save.
		/// </summary>
		internal static string CurrentSavedConfigName { get; set; }


		/// <summary>
		/// Loads configuration from the current configuration file (will default to global config if CurrentSavedConfigName is null).
		/// </summary>
		internal static void LoadConfig() => LoadConfig(CurrentSavedConfigName);


		/// <summary>
		/// Loads configuration from the specified configuration file.
		/// </summary>
		/// <param name="config">Configuration name; null for default file (default null)</param>
		internal static void LoadConfig(string config = null)
		{
			string fileName = null;


			try
			{
				// Set file location to save to.
				if (config == null)
				{
					// No file name provided; use general configuration file.
					fileName = GeneralConfigFile;
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
						CurrentConfig = (BOBConfigurationFile)new XmlSerializer(typeof(BOBConfigurationFile)).Deserialize(reader);

						// If we couldn't read it, log error and create new empty config.
						if (CurrentConfig == null)
						{
							Logging.Error("couldn't deserialize settings file");
							CurrentConfig = new BOBConfigurationFile();
							return;
						}

						// Deserialise random prefabs.
						RandomPrefabs.DeserializeRandomProps(CurrentConfig.randomProps);
						RandomPrefabs.DeserializeRandomTrees(CurrentConfig.randomTrees);

						// Deserialize scaling.
						Scaling.Instance.DeserializeProps(CurrentConfig.propScales);
						Scaling.Instance.DeserializeTrees(CurrentConfig.treeScales);

						// Deserialize all-building replacements.
						AllBuildingReplacement.Instance.Deserialize(CurrentConfig.allBuildingProps);

						// Deserialise building replacements.
						BuildingReplacement.Instance.Deserialize(CurrentConfig.buildings);

						// Deserialise individual building prop replacements.
						IndividualBuildingReplacement.Instance.Deserialize(CurrentConfig.indBuildings);

						// Deserialise all-network replacements.
						AllNetworkReplacement.Instance.Deserialize(CurrentConfig.allNetworkProps);

						// Deserialise network replacements.
						NetworkReplacement.Instance.Deserialize(CurrentConfig.networks);

						// Deserialise individual network replacements.
						IndividualNetworkReplacement.Instance.Deserialize(CurrentConfig.indNetworks);

						// Deserialise active replacement packs.
						NetworkPackReplacement.Instance.DeserializeActivePacks(CurrentConfig.activePacks);
					}
				}
				else
				{
					Logging.Message("configuration file ", fileName ?? "null", " not found");

					// Create a new empty configuration file record.
					CurrentConfig = new BOBConfigurationFile();
				}
			}
			catch (Exception e)
			{
				Logging.LogException(e, "exception reading XML configuration file ", fileName ?? "null");

				// If we didn't even get as far as creating a configuration file instanece, create a new blank one now.
				if (CurrentConfig == null)
				{
					CurrentConfig = new BOBConfigurationFile();
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
		/// <param name="config">Configuration file name; null for default file (default null)</param>
		/// <param name="clean">Set to true to generate a blank file (default false)</param>
		internal static void SaveConfig(string config = null, bool clean = false)
		{
			string fileName = null;


			try
			{
				// Set file location to save to.
				if (config == null)
				{
					// No file name provided; use general configuration file.
					fileName = GeneralConfigFile;
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

					// Create new config if there isn't one.
					if (CurrentConfig == null)
					{
						CurrentConfig = new BOBConfigurationFile
						{
							// Version 1.
							version = 1
						};
					}

					// Don't populate file if we're doing a clean save.
					if (!clean)
					{
						// Serialise scales.
						try
						{
							CurrentConfig.propScales = Scaling.Instance.propScales.Values.ToList();
							CurrentConfig.treeScales = Scaling.Instance.treeScales.Values.ToList();
						}
						catch (Exception e)
						{
							// Don't let a single failure stop us.
							Logging.LogException(e, "exception serializing scaling elements");
						}

						// Serialise active replacement packs.
						try
						{
							CurrentConfig.activePacks = NetworkPackReplacement.Instance.SerializeActivePacks();
						}
						catch (Exception e)
						{
							// Don't let a single failure stop us.
							Logging.LogException(e, "exception serializing active replacement packs");
						}
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
			InfoPanelManager.CheckException();
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
		/// Attempts to find the replacement prefab with the specified name.
		/// </summary>
		/// <param name="replacementName">Prefab name to find</param>
		/// <param name="isTree">True if the desired prefab is a tree, false if it's a prop</param>
		/// <returns>Requested prefab, or null if not found</returns>
		internal static PrefabInfo FindReplacementPrefab(string replacementName, bool isTree)
		{
			// Null check.
			if (replacementName.IsNullOrWhiteSpace())
			{
				Logging.Error("invalid replacement prop name");
				return null;
			}

			// Attempt to load from prefab collection.
			PrefabInfo replacementPrefab = isTree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacementName) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacementName);
			if (replacementPrefab == null && CurrentConfig != null)
			{
				// If we couldn't load from prefab collection, attempt to find in our list of replacement prefabs.
				replacementPrefab = isTree ? (PrefabInfo)CurrentConfig.randomTrees.Find(x => x.name.Equals(replacementName))?.tree : (PrefabInfo)CurrentConfig.randomProps.Find(x => x.name.Equals(replacementName))?.prop;
			}

			// Return what we have.
			return replacementPrefab;
		}


		/// <summary>
		/// Returns the absolute filepath of the config file for the given config name.
		/// </summary>
		/// <param name="configName">Config filepath</param>
		/// <returns></returns>
		private static string FullConfigPath(string configName) => Path.Combine(ConfigDirectory, configName + ".xml");
	}
}
