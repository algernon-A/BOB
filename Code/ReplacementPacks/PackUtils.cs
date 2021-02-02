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
                        return plugin.modPath + Path.DirectorySeparatorChar;
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
        internal static BOBPackFile LoadPackFile()
        {
            string filePath = AssemblyPath + PackFileName;


            try
            {
                // Check to see if configuration file exists.
                if (File.Exists(filePath))
                {
                    // Read it.
                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        XmlSerializer xmlSerializer = new XmlSerializer(typeof(BOBPackFile));
                        if (!(xmlSerializer.Deserialize(reader) is BOBPackFile configurationFile))
                        {
                            Logging.Error("couldn't deserialize pack file");
                        }
                        else
                        {
                            return configurationFile;
                        }
                    }
                }
                else
                {
                    Logging.Message("no pack file found at ", filePath);
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception reading XML pack file");
            }

            // If we got here, we failed; return.
            return null;
        }
    }
}