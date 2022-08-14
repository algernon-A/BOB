// <copyright file="RuiningPatches.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using HarmonyLib;

    /// <summary>
    /// Harmony patches to remove ruining.
    /// </summary>
    [HarmonyPatch]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony")]
    public static class RuiningPatches
    {
        /// <summary>
        /// Harmony Prefix patch for TreeInfo.InitializePrefab, to implement ruining removal.
        /// </summary>
        /// <param name="__instance">TreeInfo instance.</param>
        [HarmonyPatch(typeof(TreeInfo), nameof(TreeInfo.InitializePrefab))]
        [HarmonyPrefix]
        public static void StopTreeRuining(TreeInfo __instance)
        {
            // Set ruining to false if we need to.
            if (ModSettings.StopTreeRuining)
            {
                __instance.m_createRuining = false;
            }
        }

        /// <summary>
        /// Harmony Prefix patch for TreeInfo.InitializePrefab, to implement ruining removal.
        /// </summary>
        /// <param name="__instance">PropInfo instance.</param>
        [HarmonyPatch(typeof(PropInfo), nameof(PropInfo.InitializePrefab))]
        [HarmonyPrefix]
        public static void StopPropRuining(PropInfo __instance)
        {
            // Set ruining to false if we need to.
            if (ModSettings.StopPropRuining)
            {
                __instance.m_createRuining = false;
            }
        }
    }
}