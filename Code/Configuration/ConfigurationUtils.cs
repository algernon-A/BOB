using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;


namespace BOB
{
	/// <summary>
	/// XML serialization/deserialization utilities class.
	/// </summary>
	internal static class ConfigurationUtils
	{
		// Configuration file name.
		private static readonly string SettingsFileName = "BOB-config.xml";

		/// <summary>
		/// Load settings from XML file.
		/// </summary>
		internal static void LoadConfig()
		{
			try
			{
				// Check to see if configuration file exists.
				if (File.Exists(SettingsFileName))
				{
					// Read it.
					using (StreamReader reader = new StreamReader(SettingsFileName))
					{
						XmlSerializer xmlSerializer = new XmlSerializer(typeof(BOBConfigurationFile));
						BOBConfigurationFile configFile = xmlSerializer.Deserialize(reader) as BOBConfigurationFile;

						if (configFile == null)
						{
							Logging.Error("couldn't deserialize settings file");
							return;
						}

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
					}
				}
				else
				{
					Logging.Message("no configuration file found");
				}
			}
			catch (Exception e)
			{
				Logging.LogException(e, "exception reading XML settings file");
			}
		}


		/// <summary>
		/// Save settings to XML file.
		/// </summary>
		internal static void SaveConfig()
		{
			try
			{
				using (StreamWriter textWriter = new StreamWriter(SettingsFileName, append: false))
				{
					XmlSerializer xmlSerializer = new XmlSerializer(typeof(BOBConfigurationFile));
					BOBConfigurationFile configFile = new BOBConfigurationFile();

					// Version 1.
					configFile.version = 1;

					// Serialise all-building replacements.
					configFile.allBuildingProps = AllBuildingReplacement.replacements.Values.ToList();

					// Serialise building replacements, per building.
					configFile.buildings = new List<BOBBuildingElement>();
					foreach (BuildingInfo building in BuildingReplacement.replacements.Keys)
					{
						// Create new element.
						configFile.buildings.Add(new BOBBuildingElement
						{
							building = building.name,
							replacements = BuildingReplacement.replacements[building].Values.ToList()
						});
					}

					// Serialise individual building prop replacements, per building.
					configFile.indBuildings = new List<BOBBuildingElement>();
					foreach (BuildingInfo building in IndividualReplacement.replacements.Keys)
					{
						// Create new element.
						configFile.indBuildings.Add(new BOBBuildingElement
						{
							building = building.name,
							replacements = IndividualReplacement.replacements[building].Values.ToList()
						});
					}

					// Serialise all-network replacements.
					configFile.allNetworkProps = AllNetworkReplacement.replacements.Values.ToList();

					// Serialise network replacements, per network.
					configFile.networks = new List<BOBNetworkElement>();
					foreach (NetInfo network in NetworkReplacement.replacements.Keys)
					{
						// Create new element.
						configFile.networks.Add(new BOBNetworkElement
						{
							network = network.name,
							replacements = NetworkReplacement.replacements[network].Values.ToList()
						});
					}

					// Write to file.
					xmlSerializer.Serialize(textWriter, configFile);
				}
			}
			catch (Exception e)
			{
				Logging.LogException(e, "exception saving XML settings file");
			}
		}


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
				PrefabInfo replacementPrefab = replacement.tree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacement.replacement) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacement.replacement);
				if (replacementPrefab == null)
				{
					Logging.Message("Couldn't find replacement prefab ", replacement.replacement);
					continue;
				}

				// If we got here, it's all good; apply the all-network replacement.
				AllNetworkReplacement.Apply(targetPrefab, replacementPrefab, replacement.angle, replacement.offsetX, replacement.offsetY, replacement.offsetZ, replacement.probability);
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
				PrefabInfo replacementPrefab = replacement.tree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacement.replacement) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacement.replacement);
				if (replacementPrefab == null)
				{
					Logging.Message("Couldn't find replacement prefab ", replacement.replacement);
					continue;
				}

				// If we got here, it's all good; apply the network replacement.
				NetworkReplacement.Apply(networkInfo, targetPrefab, replacementPrefab, replacement.angle, replacement.offsetX, replacement.offsetY, replacement.offsetZ, replacement.probability);
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
				PrefabInfo replacementPrefab = replacement.tree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacement.replacement) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacement.replacement);
				if (replacementPrefab == null)
				{
					Logging.Message("Couldn't find replacement prefab ", replacement.replacement);
					continue;
				}

				// If we got here, it's all good; apply the all-network replacement.
				Logging.Message("applying all-building replacement ", targetPrefab.name, " to ", replacementPrefab.name);
				AllBuildingReplacement.Apply(targetPrefab, replacementPrefab, replacement.angle, replacement.offsetX, replacement.offsetY, replacement.offsetZ, replacement.probability);
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
				PrefabInfo replacementPrefab = replacement.tree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacement.replacement) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacement.replacement);
				if (replacementPrefab == null)
				{
					Logging.Message("Couldn't find replacement prefab ", replacement.replacement);
					continue;
				}

				// If we got here, it's all good; apply the building replacement.
				Logging.Message("applying building replacement ", targetPrefab.name, " to ", replacementPrefab.name, " in ", buildingInfo.name);
				BuildingReplacement.Apply(buildingInfo, targetPrefab, replacementPrefab, replacement.angle, replacement.offsetX, replacement.offsetY, replacement.offsetZ, replacement.probability);
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
				PrefabInfo replacementPrefab = replacement.tree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacement.replacement) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacement.replacement);
				if (replacementPrefab == null)
				{
					Logging.Message("Couldn't find replacement prefab ", replacement.replacement);
					continue;
				}

				// If we got here, it's all good; apply the building replacement.
				IndividualReplacement.Apply(buildingInfo, targetPrefab, replacement.index, replacementPrefab, replacement.angle, replacement.offsetX, replacement.offsetY, replacement.offsetZ, replacement.probability);
			}
		}
	}
}
