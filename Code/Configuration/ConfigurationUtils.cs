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

						// Deserialise global replacements.
						DeserializeGlobal(configFile.globalTrees, isTree: true);
						DeserializeGlobal(configFile.globalProps, isTree: false);

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

					// Serialise global replacements.
					configFile.globalTrees = SerializeGlobal(GlobalReplacement.globalTreeReplacements);
					configFile.globalProps = SerializeGlobal(GlobalReplacement.globalPropReplacements);

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
		/// Serialises a global dictionary into a global element list.
		/// </summary>
		/// <param name="dictionary">Dictionary to serialise</param>
		/// <returns>Serialised list of global elements</returns>
		private static List<BOBGlobalElement> SerializeGlobal(Dictionary<PrefabInfo, PrefabInfo> dictionary)
		{
			// Return list.
			List<BOBGlobalElement> elementList = new List<BOBGlobalElement>();

			// Only serialise if number of entries is greater than zero.
			if (dictionary.Count > 0)
			{
				// Iterate through each entry in the provided dictionary.
				foreach (PrefabInfo prefab in dictionary.Keys)
				{
					// Create a new global replacement element with matching values from the dictionary.
					BOBGlobalElement globalElement = new BOBGlobalElement()
					{
						target = prefab.name,
						replacement = dictionary[prefab].name
					};

					// Add new element to the return list.
					elementList.Add(globalElement);
				}
			}
			return elementList;
		}


		/// <summary>
		/// Deserialises a global element list.
		/// </summary>
		/// <param name="elementList">Global element list to deserialise</param>
		/// <param name="isTree">True if the list is a list of tree replacements, false if a list of props</param>
		private static void DeserializeGlobal(List<BOBGlobalElement> elementList, bool isTree)
		{
			// Iterate through each element in the proivided list.
			foreach (BOBGlobalElement globalElement in elementList)
			{
				// Try to find target prefab.
				PrefabInfo targetPrefab = isTree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(globalElement.target) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(globalElement.target);
				if (targetPrefab == null)
				{
					Debugging.Message("Couldn't find target prefab " + globalElement.target);
					continue;
				}

				// Try to find replacement prefab.
				PrefabInfo replacementPrefab = isTree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(globalElement.replacement) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(globalElement.replacement);
				if (replacementPrefab == null)
				{
					Debugging.Message("Couldn't find replacement prefab " + globalElement.replacement);
					continue;
				}

				// If we got here, it's all good; apply the global replacement.
				GlobalReplacement.ApplyGlobal(targetPrefab, replacementPrefab);
			}
		}
	}
}
