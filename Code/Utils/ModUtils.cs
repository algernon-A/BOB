// <copyright file="ModUtils.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System;
    using System.Reflection;
    using AlgernonCommons;
    using ColossalFramework.Plugins;

    /// <summary>
    /// Class that manages interactions with other mods.
    /// </summary>
    internal static class ModUtils
    {
        // NS2 reflection records.
        private static MethodInfo _ns2Recalculate;
        private static Type _networkSkin;
        private static Type _networkSkinManager;

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
                try
                {
                    foreach (Assembly assembly in plugin.GetAssemblies())
                    {
                        if (assembly.GetName().Name.Equals("NetworkSkins") && plugin.isEnabled)
                        {
                            Logging.Message("Found NetworkSkins");

                            // Found NetworkSkins.dll that's part of an enabled plugin; try to get its NetworkSkin class.
                            _networkSkin = assembly.GetType("NetworkSkins.Skins.NetworkSkin");
                            if (_networkSkin != null)
                            {
                                Logging.Message("found NetworkSkin");

                                // Success - now try to get NetworkSkinManager class.
                                _networkSkinManager = assembly.GetType("NetworkSkins.Skins.NetworkSkinManager");
                                if (_networkSkinManager != null)
                                {
                                    Logging.Message("found NetworkSkinManager");

                                    // Success - now try to get the Recalculate method from NetworkSkin.
                                    _ns2Recalculate = _networkSkin.GetMethod("Recalculate");
                                    if (_ns2Recalculate != null)
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
                catch
                {
                    // Don't care.
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
            if (_networkSkin == null || _networkSkinManager == null || _ns2Recalculate == null)
            {
                return;
            }

            // Get Network Skins 2 skin manager instance.
            PropertyInfo ns2SkinManagerInstance = _networkSkinManager.GetProperty("instance");
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

            // Iterate through each skin in list.
            for (int i = 0; i < count; ++i)
            {
                object appliedSkin = appliedSkinsFieldValue.GetType().GetProperty("Item").GetValue(appliedSkinsFieldValue, new object[] { i });

                // Invoke recalculate.
                _ns2Recalculate.Invoke(appliedSkin, null);
            }
        }

        /// <summary>
        /// Checks to see if another mod is installed and enabled, based on a provided assembly name, and if so, returns the assembly reference.
        /// Case-sensitive!  PloppableRICO is not the same as ploppablerico!.
        /// </summary>
        /// <param name="assemblyName">Name of the mod assembly.</param>
        /// <returns>Assembly reference if target is found and enabled, null otherwise.</returns>
        internal static Assembly GetEnabledAssembly(string assemblyName)
        {
            // Iterate through the full list of plugins.
            foreach (PluginManager.PluginInfo plugin in PluginManager.instance.GetPluginsInfo())
            {
                // Only looking at enabled plugins.
                if (plugin.isEnabled)
                {
                    foreach (Assembly assembly in plugin.GetAssemblies())
                    {
                        if (assembly.GetName().Name.Equals(assemblyName))
                        {
                            Logging.Message("found enabled mod assembly ", assemblyName, ", version ", assembly.GetName().Version);
                            return assembly;
                        }
                    }
                }
            }

            // If we've made it here, then we haven't found a matching assembly.
            Logging.Message("didn't find enabled assembly ", assemblyName);
            return null;
        }

        /// <summary>
        /// Attempts to find the EndRenderingImplPrefix method of Tree Anarchy.
        /// </summary>
        /// <returns>EndRenderingImplePrefix method of Tree Anarchy, or null if unsuccessful.</returns>
        internal static MethodInfo TreeAnarchyReflection()
        {
            Logging.KeyMessage("Attempting to find Tree Anarchy");

            // Get assembly.
            Assembly taAssembly = GetEnabledAssembly("TreeAnarchy");

            if (taAssembly == null)
            {
                Logging.Message("Tree Anarchy not found");
                return null;
            }

            // TreeAnarchy.Patches.TreeManagerPatches class.
            Type taPatches = taAssembly.GetType("TreeAnarchy.Patches.TreeManagerPatches");
            if (taPatches == null)
            {
                Logging.Error("TreeAnarchy.Patches.TreeManagerPatches not reflected");
                return null;
            }

            // Get EndRenderingImplPrefix method.
            MethodInfo taPrefix = taPatches.GetMethod("EndRenderingImplPrefix", BindingFlags.Static | BindingFlags.NonPublic);
            if (taPrefix == null)
            {
                Logging.Error("EndRenderingImplPrefix not reflected");
                return null;
            }

            Logging.Message("Tree Anarchy reflection complete");
            return taPrefix;
        }
    }
}
