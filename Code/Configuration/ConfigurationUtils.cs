using System;
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
		// Configuration file name.
		private static readonly string SettingsFileName = "TreePropReplacer-config.xml";

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
							Debugging.Message("couldn't deserialize settings file");
							return;
						}

						// Deserialise all-building replacements.
						DeserializeAllBuilding(configFile.allBuildingTrees, isTree: true);
						DeserializeAllBuilding(configFile.allBuildingProps, isTree: false);

						// Deserialise building replacements, per building.
						foreach (BOBBuildingElement building in configFile.buildings)
						{
							// Iterate through each replacement recored in each building.
							foreach (Replacement replacement in building.replacements)
							{
								// Check for null elements.
								if (StringExtensions.IsNullOrWhiteSpace(building.prefab) || StringExtensions.IsNullOrWhiteSpace(replacement.targetName) || StringExtensions.IsNullOrWhiteSpace(replacement.replaceName))
								{
									Debugging.Message("Null element name in configuration file");
									continue;
								}

								// Attempt to find building prefab.
								BuildingInfo buildingInfo = PrefabCollection<BuildingInfo>.FindLoaded(building.prefab);
								if (buildingInfo == null)
								{
									Debugging.Message("Couldn't find building prefab " + building.prefab);
									continue;
								}

								// Attempt to find target prefab.
								replacement.targetInfo = replacement.isTree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacement.targetName) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacement.targetName);
								if (replacement.targetInfo == null)
								{
									Debugging.Message("Couldn't find target prefab " + replacement.targetName);
									continue;
								}

								// Attempt to find replacement prefab.
								replacement.replacementInfo = replacement.isTree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacement.replaceName) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacement.replaceName);
								if (replacement.replacementInfo == null)
								{
									Debugging.Message("Couldn't find replacement prefab " + replacement.replaceName);
									continue;
								}

								// If we got here, then all prefabs were found sucesfully; assign original probability and activate the replacement.
								replacement.originalProb = buildingInfo.m_props[replacement.targetIndex].m_probability;
								BuildingReplacement.AddReplacement(buildingInfo, replacement);
							}
						}

						// Deserialise network replacements, per network.
						foreach (BOBNetworkElement network in configFile.networks)
                        {
							// Iterate through each replacement recored in each building.
							foreach (NetReplacement replacement in network.replacements)
							{
								// Check for null elements.
								if (StringExtensions.IsNullOrWhiteSpace(network.prefab) || StringExtensions.IsNullOrWhiteSpace(replacement.targetName) || StringExtensions.IsNullOrWhiteSpace(replacement.replaceName))
								{
									Debugging.Message("Null element name in configuration file");
									continue;
								}

								// Attempt to find network prefab.
								NetInfo netInfo = PrefabCollection<NetInfo>.FindLoaded(network.prefab);
								if (netInfo == null)
								{
									Debugging.Message("Couldn't find network prefab " + network.prefab);
									continue;
								}

								// Attempt to find target prefab.
								replacement.targetInfo = replacement.isTree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacement.targetName) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacement.targetName);
								if (replacement.targetInfo == null)
								{
									Debugging.Message("Couldn't find target prefab " + replacement.targetName);
									continue;
								}

								// Attempt to find replacement prefab.
								replacement.replacementInfo = replacement.isTree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacement.replaceName) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacement.replaceName);
								if (replacement.replacementInfo == null)
								{
									Debugging.Message("Couldn't find replacement prefab " + replacement.replaceName);
									continue;
								}

								// If we got here, then all prefabs were found sucesfully; activate the replacement.
								NetworkReplacement.AddReplacement(netInfo, replacement);
							}
						}
					}
				}
				else
				{
					Debugging.Message("no configuration file found");
				}
			}
			catch (Exception exception)
			{
				Debugging.Message("exception reading XML settings file");
				Debugging.LogException(exception);
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

					// Serialise all-building replacements.
					configFile.allBuildingTrees = SerializeAllBuilding(AllBuildingReplacement.treeReplacements);
					configFile.allBuildingProps = SerializeAllBuilding(AllBuildingReplacement.propReplacements);

					// Serialise building replacements, per building.
					configFile.buildings = new List<BOBBuildingElement>();

					// Serialise each building.
					foreach (BuildingInfo building in BuildingReplacement.buildingDict.Keys)
					{
						// Create new element.
						BOBBuildingElement buildingElement = new BOBBuildingElement();
						buildingElement.prefab = building.name;
						buildingElement.replacements = new List<Replacement>();

						// Serialise each replacement record for this building.
						foreach (KeyValuePair<int, Replacement> entry in BuildingReplacement.buildingDict[building])
						{
							buildingElement.replacements.Add(entry.Value);
						}
						configFile.buildings.Add(buildingElement);
					}

					// Serialise network replacements, per network.
					configFile.networks = new List<BOBNetworkElement>();

					// Serialise each network.
					foreach (NetInfo network in NetworkReplacement.netDict.Keys)
					{
						// Create new element.
						BOBNetworkElement netElement = new BOBNetworkElement();
						netElement.prefab = network.name;
						netElement.replacements = new List<NetReplacement>();

						// Serialise each replacement record for this network.
						foreach (int lane in NetworkReplacement.netDict[network].Keys)
						{
							foreach (int index in NetworkReplacement.netDict[network][lane].Keys)
							{
								netElement.replacements.Add(NetworkReplacement.netDict[network][lane][index]);
							}
						}
						configFile.networks.Add(netElement);
					}

					// Write to file.
					xmlSerializer.Serialize(textWriter, configFile);
				}
			}
			catch (Exception exception)
			{
				Debugging.Message("exception saving XML settings file");
				Debugging.LogException(exception);
			}
		}


		/// <summary>
		/// Serialises an all-building dictionary into an all-building element list.
		/// </summary>
		/// <param name="dictionary">Dictionary to serialise</param>
		/// <returns>Serialised list of all-building elements</returns>
		private static List<BOBAllBuildingElement> SerializeAllBuilding(Dictionary<PrefabInfo, PrefabInfo> dictionary)
		{
			// Return list.
			List<BOBAllBuildingElement> elementList = new List<BOBAllBuildingElement>();

			// Only serialise if number of entries is greater than zero.
			if (dictionary.Count > 0)
			{
				// Iterate through each entry in the provided dictionary.
				foreach (PrefabInfo prefab in dictionary.Keys)
				{
					// Create a new all-building replacement element with matching values from the dictionary.
					BOBAllBuildingElement allBuildElement = new BOBAllBuildingElement()
					{
						target = prefab.name,
						replacement = dictionary[prefab].name
					};

					// Add new element to the return list.
					elementList.Add(allBuildElement);
				}
			}
			return elementList;
		}


		/// <summary>
		/// Deserialises an all-building element list.
		/// </summary>
		/// <param name="elementList">All-building element list to deserialise</param>
		/// <param name="isTree">True if the list is a list of tree replacements, false if a list of props</param>
		private static void DeserializeAllBuilding(List<BOBAllBuildingElement> elementList, bool isTree)
		{
			// Iterate through each element in the proivided list.
			foreach (BOBAllBuildingElement allBuildElement in elementList)
			{
				// Try to find target prefab.
				PrefabInfo targetPrefab = isTree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(allBuildElement.target) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(allBuildElement.target);
				if (targetPrefab == null)
				{
					Debugging.Message("Couldn't find target prefab " + allBuildElement.target);
					continue;
				}

				// Try to find replacement prefab.
				PrefabInfo replacementPrefab = isTree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(allBuildElement.replacement) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(allBuildElement.replacement);
				if (replacementPrefab == null)
				{
					Debugging.Message("Couldn't find replacement prefab " + allBuildElement.replacement);
					continue;
				}

				// If we got here, it's all good; apply the all-building replacement.
				AllBuildingReplacement.Apply(targetPrefab, replacementPrefab);
			}
		}
	}
}
