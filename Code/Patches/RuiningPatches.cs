using HarmonyLib;


namespace BOB
{
    /// <summary>
    /// Harmony patches to remove ruining.
    /// </summary>
    [HarmonyPatch]
    public static class RuiningPatches
    {
        /// <summary>
        /// Harmony Prefix patch for TreeInfo.InitializaePrefab, to implement ruining removal.
        /// </summary>
        /// <param name="__instance"></param>
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
        /// Harmony Prefix patch for TreeInfo.InitializaePrefab, to implement ruining removal.
        /// </summary>
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