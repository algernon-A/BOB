using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using System.Collections.Generic;
using ICities;
using ColossalFramework.Plugins;


namespace BOB
{
    /// <summary>
    /// Static utility class for managing prop replacement pack files.
    /// </summary>
    public static class PackUtils
    {
        internal static readonly string PackFileName = "BOBPacks.xml";


        /// <summary>
        /// Returns the path of the current assembly.
        /// </summary>
        private static string AssemblyPath
        {
            get
            {
                // Step through each plugin, looking for a match for this one.
                PluginManager pluginManager = PluginManager.instance;
                IEnumerable<PluginManager.PluginInfo> plugins = pluginManager.GetPluginsInfo();

                foreach (PluginManager.PluginInfo plugin in plugins)
                {
                    try
                    {
                        IUserMod[] instances = plugin.GetInstances<IUserMod>();

                        if (!(instances.FirstOrDefault() is BOBMod))
                        {
                            continue;
                        }

                        // Got it!  Return.
                        return plugin.modPath + Path.DirectorySeparatorChar + "ReplacementPacks" + Path.DirectorySeparatorChar;
                    }
                    catch
                    {
                        // Don't care.
                    }
                }

                // If we got here, we didn't find it.
                Logging.Error("couldn't find assembly path");
                throw new DllNotFoundException("BOB assembly not found.");
            }
        }


        /// <summary>
        /// Loads an XML configuration file.
        /// </summary>
        /// <returns>Loaded XML configuration file instance (null if failed)</returns>
        internal static List<BOBPackFile> LoadPackFiles()
        {
            // Return list.
            List<BOBPackFile> fileList = new List<BOBPackFile>();

            // Iterate through each xml file in directory.
            string[] fileNames = Directory.GetFiles(AssemblyPath, "*.xml", SearchOption.AllDirectories);
            foreach (string fileName in fileNames)
            {
                try
                {
                    // Read it.
                    using (StreamReader reader = new StreamReader(fileName))
                    {
                        XmlSerializer xmlSerializer = new XmlSerializer(typeof(BOBPackFile));
                        if (!(xmlSerializer.Deserialize(reader) is BOBPackFile configurationFile))
                        {
                            Logging.Error("couldn't deserialize pack file");
                        }
                        else
                        {
                            fileList.Add(configurationFile);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logging.LogException(e, "exception reading XML pack file ", fileName);
                }
            }

            return fileList;
        }
    }
}