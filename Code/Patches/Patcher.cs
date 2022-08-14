// <copyright file="Patcher.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System;
    using System.Reflection;
    using AlgernonCommons;
    using AlgernonCommons.Patching;
    using CitiesHarmony.API;
    using HarmonyLib;

    /// <summary>
    /// Class to manage the mod's Harmony patches.
    /// </summary>
    public class Patcher : PatcherBase
    {
        // Tree Anarchy Harmony identifier.
        private const string TAharmonyID = "quistar.treeanarchy.mod";

        // Target methods - overlays.
        private MethodInfo _buildingOverlayTarget;
        private MethodInfo _netOverlayTarget;
        private MethodInfo _propOverlayTarget;
        private MethodInfo _treeOverlayTarget;

        // Target methods - rendering.
        private MethodInfo _buildingRenderTarget;
        private MethodInfo _netRenderTarget;
        private MethodInfo _propRenderTarget;
        private MethodInfo _treeRenderTarget;

        // Patch methods.
        private MethodInfo _renderOverlayPatch;
        private MethodInfo _buildingTranspiler;
        private MethodInfo _netTranspiler;
        private MethodInfo _propRenderTranspiler;
        private MethodInfo _treeRenderTranspiler;

        // Target methods - tree anarchy.
        private MethodInfo _treeAnarchyMethod;
        private MethodInfo _treeAnarchyTarget;
        private MethodInfo _treeAnarchyPatch;

        // Flags.
        private bool _buildingOverlaysPatched = false;
        private bool _netOverlaysPatched = false;
        private bool _treeOverlaysPatched = false;
        private bool _treeToolPatched = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Patcher"/> class.
        /// </summary>
        /// <param name="harmonyID">This mod's unique Harmony identifier.</param>
        public Patcher(string harmonyID)
            : base(harmonyID)
        {
        }

        /// <summary>
        /// Gets the active instance reference.
        /// </summary>
        public static new Patcher Instance
        {
            get
            {
                // Auto-initializing getter.
                if (s_instance == null)
                {
                    s_instance = new Patcher(PatcherMod.Instance.HarmonyID);
                }

                return s_instance as Patcher;
            }
        }

        /// <summary>
        /// Applies or unapplies overlayer patches for buildings.
        /// </summary>
        /// <param name="active">True to enable patches; false to disable.</param>
        internal void PatchBuildingOverlays(bool active)
        {
            // Don't do anything if we're already at the current state.
            if (_buildingOverlaysPatched != active)
            {
                // Ensure Harmony is ready before patching.
                if (HarmonyHelper.IsHarmonyInstalled)
                {
                    Logging.KeyMessage(active ? "deploying" : "reverting", " building overlay Harmony patches");

                    // Manually patch building overlay renderer.
                    Harmony harmonyInstance = new Harmony(HarmonyID);

                    // Safety check.
                    if (_buildingOverlayTarget == null || _renderOverlayPatch == null || _buildingRenderTarget == null || _buildingTranspiler == null)
                    {
                        Logging.Error("couldn't find required render overlay method");
                        return;
                    }

                    // Apply or remove patches according to flag.
                    if (active)
                    {
                        harmonyInstance.Patch(_buildingOverlayTarget, postfix: new HarmonyMethod(_renderOverlayPatch));
                        harmonyInstance.Patch(_buildingRenderTarget, transpiler: new HarmonyMethod(_buildingTranspiler));
                    }
                    else
                    {
                        harmonyInstance.Unpatch(_buildingOverlayTarget, _renderOverlayPatch);
                        harmonyInstance.Unpatch(_buildingRenderTarget, _buildingTranspiler);
                    }

                    // Update status flag.
                    _buildingOverlaysPatched = active;
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
        /// <param name="active">True to enable patches; false to disable.</param>
        internal void PatchNetworkOverlays(bool active)
        {
            // Don't do anything if we're already at the current state.
            if (_netOverlaysPatched != active)
            {
                // Ensure Harmony is ready before patching.
                if (HarmonyHelper.IsHarmonyInstalled)
                {
                    Logging.KeyMessage(active ? "deploying" : "reverting", " network overlay Harmony patches");

                    // Manually patch building overlay renderer.
                    Harmony harmonyInstance = new Harmony(HarmonyID);

                    // Safety check.
                    if (_netOverlayTarget == null || _renderOverlayPatch == null || _netRenderTarget == null || _netTranspiler == null)
                    {
                        Logging.Error("couldn't find required render overlay method");
                        return;
                    }

                    // Apply or remove patches according to flag.
                    if (active)
                    {
                        harmonyInstance.Patch(_netOverlayTarget, postfix: new HarmonyMethod(_renderOverlayPatch));
                        harmonyInstance.Patch(_netRenderTarget, transpiler: new HarmonyMethod(_netTranspiler));
                    }
                    else
                    {
                        harmonyInstance.Unpatch(_netOverlayTarget, _renderOverlayPatch);
                        harmonyInstance.Unpatch(_netRenderTarget, _netTranspiler);
                    }

                    // Update status flag.
                    _netOverlaysPatched = active;
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
        /// <param name="active">True to enable patches; false to disable.</param>
        internal void PatchMapOverlays(bool active)
        {
            // Don't do anything if we're already at the current state.
            if (_treeOverlaysPatched != active)
            {
                // Ensure Harmony is ready before patching.
                if (HarmonyHelper.IsHarmonyInstalled)
                {
                    Logging.KeyMessage(active ? "deploying" : "reverting", " map overlay Harmony patches");

                    // Manually patch building overlay renderer.
                    Harmony harmonyInstance = new Harmony(HarmonyID);

                    // Safety check.
                    if (_treeOverlayTarget == null || _propOverlayTarget == null || _renderOverlayPatch == null || _treeRenderTarget == null || _treeRenderTranspiler == null || _propRenderTarget == null || _propRenderTranspiler == null)
                    {
                        Logging.Error("couldn't find required render overlay method");
                        return;
                    }

                    // Apply or remove patches according to flag.
                    if (active)
                    {
                        // Patch game.
                        harmonyInstance.Patch(_treeOverlayTarget, postfix: new HarmonyMethod(_renderOverlayPatch));
                        harmonyInstance.Patch(_treeRenderTarget, transpiler: new HarmonyMethod(_treeRenderTranspiler));
                        harmonyInstance.Patch(_propOverlayTarget, postfix: new HarmonyMethod(_renderOverlayPatch));
                        harmonyInstance.Patch(_propRenderTarget, transpiler: new HarmonyMethod(_propRenderTranspiler));

                        // Tree anarchy.
                        if (_treeAnarchyMethod != null)
                        {
                            // Remove existing Tree Anarchy prefix.
                            Harmony taHarmony = new Harmony(TAharmonyID);
                            taHarmony.Unpatch(_treeAnarchyTarget, _treeAnarchyMethod);

                            // Apply our own modified replacement.
                            TreeAnarchyRender.Setup();
                            harmonyInstance.Patch(_treeAnarchyTarget, prefix: new HarmonyMethod(_treeAnarchyPatch));
                        }
                    }
                    else
                    {
                        // Unpatch game.
                        harmonyInstance.Unpatch(_treeOverlayTarget, _renderOverlayPatch);
                        harmonyInstance.Unpatch(_treeRenderTarget, _treeRenderTranspiler);
                        harmonyInstance.Unpatch(_propOverlayTarget, _renderOverlayPatch);
                        harmonyInstance.Unpatch(_propRenderTarget, _propRenderTranspiler);

                        // Tree anarchy.
                        if (_treeAnarchyMethod != null)
                        {
                            // Remove our patch.
                            harmonyInstance.Unpatch(_treeAnarchyTarget, _treeAnarchyPatch);

                            // Re-apply Tree Anarchy patch.
                            Harmony taHarmony = new Harmony(TAharmonyID);
                            taHarmony.Patch(_treeAnarchyTarget, prefix: new HarmonyMethod(_treeAnarchyMethod));
                        }
                    }

                    // Update status flag.
                    _treeOverlaysPatched = active;
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
        internal void ReflectOverlays()
        {
            // Argument list for correct BuildingAI.RenderProps target.
            Type[] buildingRenderPropsTypes = { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(Building).MakeByRefType(), typeof(int), typeof(RenderManager.Instance).MakeByRefType(), typeof(bool), typeof(bool), typeof(bool) };

            // Building patches.
            _buildingOverlayTarget = typeof(BuildingManager).GetMethod("EndOverlay");
            _renderOverlayPatch = typeof(RenderOverlays).GetMethod(nameof(RenderOverlays.RenderOverlay));
            _buildingRenderTarget = typeof(BuildingAI).GetMethod("RenderProps", BindingFlags.NonPublic | BindingFlags.Instance, null, buildingRenderPropsTypes, null);
            _buildingTranspiler = typeof(OverlayTranspilers).GetMethod(nameof(OverlayTranspilers.BuildingTranspiler));

            // Network patches.
            _netOverlayTarget = typeof(NetManager).GetMethod("EndOverlay");
            _netRenderTarget = typeof(NetLane).GetMethod(nameof(NetLane.RenderInstance));
            _netTranspiler = typeof(OverlayTranspilers).GetMethod(nameof(OverlayTranspilers.NetTranspiler));

            // Map patches.
            _treeOverlayTarget = typeof(TreeManager).GetMethod("EndOverlay");
            _propOverlayTarget = typeof(PropManager).GetMethod("EndOverlay");
            _treeRenderTarget = typeof(TreeInstance).GetMethod(nameof(TreeInstance.RenderInstance), BindingFlags.Public | BindingFlags.Instance);
            _treeRenderTranspiler = typeof(OverlayTranspilers).GetMethod(nameof(OverlayTranspilers.TreeTranspiler));
            _propRenderTarget = typeof(PropInstance).GetMethod(nameof(PropInstance.RenderInstance), new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(int) });
            _propRenderTranspiler = typeof(OverlayTranspilers).GetMethod(nameof(OverlayTranspilers.PropTranspiler));

            // Tree anarchy patch.
            _treeAnarchyMethod = ModUtils.TreeAnarchyReflection();
            if (_treeAnarchyMethod != null)
            {
                _treeAnarchyTarget = AccessTools.Method(typeof(TreeManager), "EndRenderingImpl");
                _treeAnarchyPatch = AccessTools.Method(typeof(TreeAnarchyRender), nameof(TreeAnarchyRender.EndRenderingImplPrefix));
            }
        }

        /// <summary>
        /// Patches the game's tree tool to enable/disable network tree replacement.
        /// </summary>
        /// <param name="enabled">True to enable the patch (disable network tree replacement), false to revert patch.</param>
        internal void DisableTreeTool(bool enabled)
        {
            // Target and patch.
            MethodInfo treeToolTarget = typeof(TreeTool).GetMethod(nameof(TreeTool.SimulationStep));
            MethodInfo treeToolPatch = typeof(TreeToolPatch).GetMethod(nameof(TreeToolPatch.Transpiler));
            Harmony harmonyInstance = new Harmony(HarmonyID);

            if (enabled)
            {
                // Apply patch, if it isn't already.
                if (!_treeToolPatched)
                {
                    Logging.Message("applying TreeTool patch");
                    harmonyInstance.Patch(treeToolTarget, transpiler: new HarmonyMethod(treeToolPatch));
                    _treeToolPatched = true;
                }
            }
            else
            {
                // Unapply patch, if we haven't already.
                if (_treeToolPatched)
                {
                    Logging.Message("reverting TreeTool patch");
                    harmonyInstance.Unpatch(treeToolTarget, treeToolPatch);
                    _treeToolPatched = false;
                }
            }
        }
    }
}