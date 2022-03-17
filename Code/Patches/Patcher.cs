using System;
using System.Reflection;
using HarmonyLib;
using CitiesHarmony.API;


namespace BOB
{
    /// <summary>
    /// Class to manage the mod's Harmony patches.
    /// </summary>
    public static class Patcher
    {
        // Unique harmony identifier.
        private const string harmonyID = "com.github.algernon-A.csl.bob";

        // Tree Anarchy Harmony identifier.
        private const string taHarmonyID = "quistar.treeanarchy.mod";


        // Target methods - overlays.
        private static MethodInfo buildingOverlayTarget, netOverlayTarget, propOverlayTarget, treeOverlayTarget;

        // Target methods - rendering.
        private static MethodInfo buildingRenderTarget, netRenderTarget, propRenderTarget, treeRenderTarget;

        // Patch methods.
        private static MethodInfo renderOverlayPatch, buildingTranspiler, netTranspiler, propRenderTranspiler, treeRenderTranspiler;

        // Target methods - tree anarchy.
        private static MethodInfo treeAnarchyMethod, treeAnarchyTarget, treeAnarchyPatch;


        // Flags.
        internal static bool Patched => patched;
        private static bool patched = false;
        private static bool buildingOverlaysPatched = false, netOverlaysPatched = false, treeOverlaysPatched = false;
        private static bool treeToolPatched = false;


        /// <summary>
        /// Apply all Harmony patches.
        /// </summary>
        public static void PatchAll()
        {
            // Don't do anything if already patched.
            if (!patched)
            {
                // Ensure Harmony is ready before patching.
                if (HarmonyHelper.IsHarmonyInstalled)
                {
                    Logging.KeyMessage("deploying Harmony patches");

                    // Apply all annotated patches and update flag.
                    Harmony harmonyInstance = new Harmony(harmonyID);
                    harmonyInstance.PatchAll();
                    patched = true;
                }
                else
                {
                    Logging.Error("Harmony not ready");
                }
            }
        }


        /// <summary>
        /// Remove all Harmony patches.
        /// </summary>
        public static void UnpatchAll()
        {
            // Only unapply if patches appplied.
            if (patched)
            {
                Logging.KeyMessage("reverting Harmony patches");

                // Unapply patches, but only with our HarmonyID.
                Harmony harmonyInstance = new Harmony(harmonyID);
                harmonyInstance.UnpatchAll(harmonyID);
                patched = false;
            }
        }


        /// <summary>
        /// Applies or unapplies overlayer patches for buildings.
        /// </summary>
        /// <param name="active">True to enable patches; false to disable</param>
        internal static void PatchBuildingOverlays(bool active)
        {
 
            // Don't do anything if we're already at the current state.
            if (buildingOverlaysPatched != active)
            {
                // Ensure Harmony is ready before patching.
                if (HarmonyHelper.IsHarmonyInstalled)
                {
                    Logging.KeyMessage(active ? "deploying" : "reverting", " building overlay Harmony patches");

                    // Manually patch building overlay renderer.
                    Harmony harmonyInstance = new Harmony(harmonyID);

                    // Safety check.
                    if (buildingOverlayTarget == null || renderOverlayPatch == null || buildingRenderTarget == null || buildingTranspiler == null)
                    {
                        Logging.Error("couldn't find required render overlay method");
                        return;
                    }

                    // Apply or remove patches according to flag.
                    if (active)
                    {
                        harmonyInstance.Patch(buildingOverlayTarget, postfix: new HarmonyMethod(renderOverlayPatch));
                        harmonyInstance.Patch(buildingRenderTarget, transpiler: new HarmonyMethod(buildingTranspiler));
                    }
                    else
                    {
                        harmonyInstance.Unpatch(buildingOverlayTarget, renderOverlayPatch);
                        harmonyInstance.Unpatch(buildingRenderTarget, buildingTranspiler);
                    }

                    // Update status flag.
                    buildingOverlaysPatched = active;
                }
                else
                {
                    Logging.Error("Harmony not ready");
                }
            }
        }


        /// <summary>
        /// Applies or unapplies overlayer patches for networks.
        /// </summary>
        /// <param name="active">True to enable patches; false to disable</param>
        internal static void PatchNetworkOverlays(bool active)
        {
            // Don't do anything if we're already at the current state.
            if (netOverlaysPatched != active)
            {
                // Ensure Harmony is ready before patching.
                if (HarmonyHelper.IsHarmonyInstalled)
                {
                    Logging.KeyMessage(active ? "deploying" : "reverting", " network overlay Harmony patches");

                    // Manually patch building overlay renderer.
                    Harmony harmonyInstance = new Harmony(harmonyID);

                    // Safety check.
                    if (netOverlayTarget == null || renderOverlayPatch == null || netRenderTarget == null || netTranspiler == null)
                    {
                        Logging.Error("couldn't find required render overlay method");
                        return;
                    }

                    // Apply or remove patches according to flag.
                    if (active)
                    {
                        harmonyInstance.Patch(netOverlayTarget, postfix: new HarmonyMethod(renderOverlayPatch));
                        harmonyInstance.Patch(netRenderTarget, transpiler: new HarmonyMethod(netTranspiler));
                    }
                    else
                    {
                        harmonyInstance.Unpatch(netOverlayTarget, renderOverlayPatch);
                        harmonyInstance.Unpatch(netRenderTarget, netTranspiler);
                    }

                    // Update status flag.
                    netOverlaysPatched = active;
                }
                else
                {
                    Logging.Error("Harmony not ready");
                }
            }
        }


        /// <summary>
        /// Applies or unapplies overlayer patches for map trees and props.
        /// </summary>
        /// <param name="active">True to enable patches; false to disable</param>
        internal static void PatchMapOverlays(bool active)
        {
            // Don't do anything if we're already at the current state.
            if (treeOverlaysPatched != active)
            {
                // Ensure Harmony is ready before patching.
                if (HarmonyHelper.IsHarmonyInstalled)
                {
                    Logging.KeyMessage(active ? "deploying" : "reverting", " map overlay Harmony patches");

                    // Manually patch building overlay renderer.
                    Harmony harmonyInstance = new Harmony(harmonyID);


                    // Safety check.
                    if (treeOverlayTarget == null || propOverlayTarget == null || renderOverlayPatch == null || treeRenderTarget == null || treeRenderTranspiler == null || propRenderTarget == null || propRenderTranspiler == null)
                    {
                        Logging.Error("couldn't find required render overlay method");
                        return;
                    }

                    // Apply or remove patches according to flag.
                    if (active)
                    {
                        // Patch game.
                        harmonyInstance.Patch(treeOverlayTarget, postfix: new HarmonyMethod(renderOverlayPatch));
                        harmonyInstance.Patch(treeRenderTarget, transpiler: new HarmonyMethod(treeRenderTranspiler));
                        harmonyInstance.Patch(propOverlayTarget, postfix: new HarmonyMethod(renderOverlayPatch));
                        harmonyInstance.Patch(propRenderTarget, transpiler: new HarmonyMethod(propRenderTranspiler));

                        // Tree anarchy.
                        if (treeAnarchyMethod != null)
                        {
                            // Remove existing Tree Anarchy prefix.
                            Harmony taHarmony = new Harmony(taHarmonyID);
                            taHarmony.Unpatch(treeAnarchyTarget, treeAnarchyMethod);

                            // Apply our own modified replacement.
                            TreeAnarchyRender.Setup();
                            harmonyInstance.Patch(treeAnarchyTarget, prefix: new HarmonyMethod(treeAnarchyPatch));
                        }
                    }
                    else
                    {
                        // Unpatch game.
                        harmonyInstance.Unpatch(treeOverlayTarget, renderOverlayPatch);
                        harmonyInstance.Unpatch(treeRenderTarget, treeRenderTranspiler);
                        harmonyInstance.Unpatch(propOverlayTarget, renderOverlayPatch);
                        harmonyInstance.Unpatch(propRenderTarget, propRenderTranspiler);

                        // Tree anarchy.
                        if (treeAnarchyMethod != null)
                        {
                            // Remove our patch.
                            harmonyInstance.Unpatch(treeAnarchyTarget, treeAnarchyPatch);

                            // Re-apply Tree Anarchy patch.
                            Harmony taHarmony = new Harmony(taHarmonyID);
                            taHarmony.Patch(treeAnarchyTarget, prefix: new HarmonyMethod(treeAnarchyMethod));
                        }
                    }

                    // Update status flag.
                    treeOverlaysPatched = active;
                }
                else
                {
                    Logging.Error("Harmony not ready");
                }
            }
        }


        /// <summary>
        /// Performs reflection required for overlay patching.
        /// </summary>
        internal static void ReflectOverlays()
        {
            // Argument list for correct BuildingAI.RenderProps target.
            Type[] buildingRenderPropsTypes = { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(Building).MakeByRefType(), typeof(int), typeof(RenderManager.Instance).MakeByRefType(), typeof(bool), typeof(bool), typeof(bool) };

            // Building patches.
            buildingOverlayTarget = typeof(BuildingManager).GetMethod("EndOverlay");
            renderOverlayPatch = typeof(RenderOverlays).GetMethod(nameof(RenderOverlays.RenderOverlay));
            buildingRenderTarget = typeof(BuildingAI).GetMethod("RenderProps", BindingFlags.NonPublic | BindingFlags.Instance, null, buildingRenderPropsTypes, null);
            buildingTranspiler = typeof(OverlayTranspilers).GetMethod(nameof(OverlayTranspilers.BuildingTranspiler));

            // Network patches.
            netOverlayTarget = typeof(NetManager).GetMethod("EndOverlay");
            netRenderTarget = typeof(NetLane).GetMethod(nameof(NetLane.RenderInstance));
            netTranspiler = typeof(OverlayTranspilers).GetMethod(nameof(OverlayTranspilers.NetTranspiler));

            // Map patches.
            treeOverlayTarget = typeof(TreeManager).GetMethod("EndOverlay");
            propOverlayTarget = typeof(PropManager).GetMethod("EndOverlay");
            treeRenderTarget = typeof(TreeInstance).GetMethod(nameof(TreeInstance.RenderInstance), BindingFlags.Public | BindingFlags.Instance);
            treeRenderTranspiler = typeof(OverlayTranspilers).GetMethod(nameof(OverlayTranspilers.TreeTranspiler));
            propRenderTarget = typeof(PropInstance).GetMethod(nameof(PropInstance.RenderInstance), new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(int) });
            propRenderTranspiler = typeof(OverlayTranspilers).GetMethod(nameof(OverlayTranspilers.PropTranspiler));

            // Tree anarchy patch.
            treeAnarchyMethod = ModUtils.TreeAnarchyReflection();
            if (treeAnarchyMethod != null)
            {
                treeAnarchyTarget = AccessTools.Method(typeof(TreeManager), "EndRenderingImpl");
                treeAnarchyPatch = AccessTools.Method(typeof(TreeAnarchyRender), nameof(TreeAnarchyRender.EndRenderingImplPrefix));
            }
        }


        /// <summary>
        /// Patches the game's tree tool to enable/disable network tree replacement.
        /// </summary>
        /// <param name="enabled">True to enable the patch (disable network tree replacement), false to revert patch</param>
        internal static void DisableTreeTool(bool enabled)
        {
            // Target and patch.
            MethodInfo treeToolTarget = typeof(TreeTool).GetMethod(nameof(TreeTool.SimulationStep));
            MethodInfo treeToolPatch = typeof(TreeToolPatch).GetMethod(nameof(TreeToolPatch.Transpiler));
            Harmony harmonyInstance = new Harmony(harmonyID);

            if (enabled)
            {
                // Apply patch, if it isn't already.
                if (!treeToolPatched)
                {
                    Logging.Message("applying TreeTool patch");
                    harmonyInstance.Patch(treeToolTarget, transpiler: new HarmonyMethod(treeToolPatch));
                    treeToolPatched = true;
                }
            }
            else
            {
                // Unapply patch, if we haven't already.
                if (treeToolPatched)
                {
                    Logging.Message("reverting TreeTool patch");
                    harmonyInstance.Unpatch(treeToolTarget, treeToolPatch);
                    treeToolPatched = false;
                }
            }
        }
    }
}