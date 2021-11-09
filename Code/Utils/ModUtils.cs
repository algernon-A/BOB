using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using ICities;
using ColossalFramework.Plugins;


namespace BOB
{
    /// <summary>
    /// Class that manages interactions with other mods, including compatibility and functionality checks.
    /// </summary>
    internal static class ModUtils
    {
        // NS2 reflection records.
        internal static MethodInfo ns2Recalculate;
        internal static Type networkSkin;
        internal static Type networkSkinManager;


        /// <summary>
        /// Returns the filepath of the current mod assembly.
        /// </summary>
        /// <returns>Mod assembly filepath</returns>
        internal static string GetAssemblyPath()
        {
            // Get list of currently active plugins.
            IEnumerable<PluginManager.PluginInfo> plugins = PluginManager.instance.GetPluginsInfo();

            // Iterate through list.
            foreach (PluginManager.PluginInfo plugin in plugins)
            {
                try
                {
                    // Get all (if any) mod instances from this plugin.
                    IUserMod[] mods = plugin.GetInstances<IUserMod>();

                    // Check to see if the primary instance is this mod.
                    if (mods.FirstOrDefault() is BOBMod)
                    {
                        // Found it! Return path.
                        return plugin.modPath;
                    }
                }
                catch
                {
                    // Don't care.
                }
            }

            // If we got here, then we didn't find the assembly.
            Logging.Error("assembly path not found");
            throw new FileNotFoundException(BOBMod.ModName + ": assembly path not found!");
        }


        /// <summary>
        /// Uses reflection to find the NetworkSkinManager.instance.AppliedSkin.Recalculate method of Network Skins 2.
        /// If successful, sets ns2Recalculate field.
        /// </summary>
        internal static void NS2Reflection()
        {
            Logging.Message("looking for Network Skins");

            // Iterate through each loaded plugin assembly.
            foreach (PluginManager.PluginInfo plugin in PluginManager.instance.GetPluginsInfo())
            {
                foreach (Assembly assembly in plugin.GetAssemblies())
                {
                    if (assembly.GetName().Name.Equals("NetworkSkins") && plugin.isEnabled)
                    {
                        Logging.Message("Found NetworkSkins");

                        // Found NetworkSkins.dll that's part of an enabled plugin; try to get its NetworkSkin class.
                        networkSkin = assembly.GetType("NetworkSkins.Skins.NetworkSkin");
                        if (networkSkin != null)
                        {
                            Logging.Message("found NetworkSkin");

                            // Success - now try to get NetworkSkinManager class.
                            networkSkinManager = assembly.GetType("NetworkSkins.Skins.NetworkSkinManager");
                            if (networkSkinManager != null)
                            {
                                Logging.Message("found NetworkSkinManager");

                                // Success - now try to get the Recalculate method from NetworkSkin.
                                ns2Recalculate = networkSkin.GetMethod("Recalculate");
                                if (ns2Recalculate != null)
                                {
                                    Logging.Message("found NetworkSkin.Recalculate");
                                }
                            }
                        }

                        // At this point, we're done; return.
                        return;
                    }
                }
            }

            // If we got here, we were unsuccessful.
            Logging.Message("Network Skins 2 not found");
        }


        /// <summary>
        /// Triggers a recalculation of Network Skins 2 applied skins.
        /// Should be called whenever a network changes.
        /// </summary>
        internal static void NS2Recalculate()
        {
            // Make sure we've got valid references before doing anything.
            if (networkSkin == null || networkSkinManager == null || ns2Recalculate == null)
            {
                return;
            }

            // Get Network Skins 2 skin manager instance.
            PropertyInfo ns2SkinManagerInstance = networkSkinManager.GetProperty("instance");
            if (ns2SkinManagerInstance == null)
            {
                Logging.Error("couldn't find NS2 NetworkSkinManager.instance");
                return;
            }

            // Invoke instance getter.
            MethodInfo ns2InstanceGetter = ns2SkinManagerInstance.GetGetMethod(true);
            if (ns2InstanceGetter == null)
            {
                Logging.Error("couldn't find NS2 NetworkSkinManager.instance gettter");
                return;
            }

            // Get actual instance.
            object ns2Instance = ns2InstanceGetter.Invoke(ns2SkinManagerInstance, null);
            if (ns2Instance == null)
            {
                Logging.Error("couldn't get NS2 NetworkSkinManager instance value");
                return;
            }

            // Get applied skins field.
            FieldInfo appliedSkinsField = ns2Instance.GetType().GetField("AppliedSkins");
            if (appliedSkinsField == null)
            {
                Logging.Error("couldn't get NS2 applied skins list reference");
                return;
            }

            // Get actual applied skins list list.
            object appliedSkinsFieldValue = appliedSkinsField.GetValue(ns2Instance);
            if (appliedSkinsFieldValue == null)
            {
                Logging.Error("couldn't get NS2 applied skins list value");
                return;
            }

            // Check that we actually got a list.
            if (!typeof(System.Collections.IList).IsAssignableFrom(appliedSkinsFieldValue.GetType()))
            {
                Logging.Error("NS2 applied skins list was not a list");
                return;
            }

            // Get list count.
            int count = (int)appliedSkinsFieldValue.GetType().GetProperty("Count").GetValue(appliedSkinsFieldValue, null);
            Logging.Message(count, " applied NS2 skins found");

            // Iterate through each skin in list.
            for (int i = 0; i < count; ++i)
            {
                object appliedSkin = appliedSkinsFieldValue.GetType().GetProperty("Item").GetValue(appliedSkinsFieldValue, new object[] { i });
                Logging.Message("Recalculating NS2 applied skin", appliedSkin);

                // Invoke recalculate.
                ns2Recalculate.Invoke(appliedSkin, null);
            }
        }


        /// <summary>
        /// Checks to see if another mod is installed, based on a provided assembly name.
        /// </summary>
        /// <param name="assemblyName">Name of the mod assembly</param>
        /// <param name="enabledOnly">True if the mod needs to be enabled for the purposes of this check; false if it doesn't matter</param>
        /// <returns>True if the mod is installed (and, if enabledOnly is true, is also enabled), false otherwise</returns>
        internal static bool IsModInstalled(string assemblyName, bool enabledOnly = false)
        {
            // Convert assembly name to lower case.
            string assemblyNameLower = assemblyName.ToLower();

            // Iterate through the full list of plugins.
            foreach (PluginManager.PluginInfo plugin in PluginManager.instance.GetPluginsInfo())
            {
                foreach (Assembly assembly in plugin.GetAssemblies())
                {
                    if (assembly.GetName().Name.ToLower().Equals(assemblyNameLower))
                    {
                        Logging.Message("found mod assembly ", assemblyName);
                        if (enabledOnly)
                        {
                            return plugin.isEnabled;
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
            }

            // If we've made it here, then we haven't found a matching assembly.
            return false;
        }
    }
}
