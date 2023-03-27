// <copyright file="OverlayTranspilers.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using AlgernonCommons;
    using HarmonyLib;
    using UnityEngine;

    /// <summary>
    /// Harmony transpilers for registering overlays to render.
    /// </summary>
    public static class OverlayTranspilers
    {
        /// <summary>
        /// Harmony transpiler for BuildingAI.RenderProps, to insert calls to highlight selected props/trees.
        /// </summary>
        /// <param name="instructions">Original ILCode.</param>
        /// <param name="generator">IL generator.</param>
        /// <returns>Patched ILCode.</returns>
        public static IEnumerable<CodeInstruction> BuildingTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // ILCode local variable indexes.
            const int IVarIndex = 11;
            const int BuildingInfoPropIndex = 12;
            const int TreeVarIndex = 15;
            const int TreePositionVarIndex = 29;

            // ILCode argument indexes.
            const int BuildingArg = 3;

            // Original TreeInfo (pre-variations).
            bool foundTreeInfo = false;
            LocalBuilder originalTreeInfo = generator.DeclareLocal(typeof(TreeInfo));

            // Tree renderer call.
            MethodInfo renderTree = AccessTools.Method(
                typeof(global::TreeInstance),
                nameof(global::TreeInstance.RenderInstance),
                new Type[] { typeof(RenderManager.CameraInfo), typeof(TreeInfo), typeof(Vector3), typeof(float), typeof(float), typeof(Vector4), typeof(bool) });

            // Iterate through all instructions in original method.
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;

                // Is this stloc.s?
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand is LocalBuilder localBuilder)
                {
                    // Is this stloc.s 17 (should be only one in entire method)?
                    if (localBuilder.LocalIndex == 17)
                    {
                        // Yes - insert call to RenderOverlays.HighlightBuildingProp(i, prop, ref building)
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Ldloc_S, IVarIndex);
                        yield return new CodeInstruction(OpCodes.Ldloc_S, BuildingInfoPropIndex);
                        yield return new CodeInstruction(OpCodes.Ldarg_S, BuildingArg);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RenderOverlays), nameof(RenderOverlays.HighlightBuildingProp)));
                    }

                    // Otherwise, is this the first instance of stloc.s 15?
                    else if (!foundTreeInfo && localBuilder.LocalIndex == TreeVarIndex)
                    {
                        // Yes - record the original tree reference and flag that we've already got it (so no need to get again).
                        foundTreeInfo = true;
                        yield return new CodeInstruction(OpCodes.Ldloc_S, TreeVarIndex);
                        yield return new CodeInstruction(OpCodes.Stloc_S, originalTreeInfo);
                    }
                }

                // Is this new instruction a call to Void RenderInstance?
                if (instruction.Calls(renderTree))
                {
                    // Yes - insert call to PropOverlays.Highlight method after original call.
                    Logging.KeyMessage("adding building HighlightTree call after RenderInstance");
                    yield return new CodeInstruction(OpCodes.Ldloc_S, IVarIndex);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, originalTreeInfo);
                    yield return new CodeInstruction(OpCodes.Ldarg_S, BuildingArg);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, TreePositionVarIndex);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RenderOverlays), nameof(RenderOverlays.HighlightBuildingTree)));
                }
            }
        }

        /// <summary>
        /// Harmony transpiler for NetLane.RenderInstance, to insert calls to highlight selected props/tree.
        /// </summary>
        /// <param name="instructions">Original ILCode.</param>
        /// <returns>Patched ILCode.</returns>
        public static IEnumerable<CodeInstruction> NetTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            // ILCode local variable indexes.
            const int propLoopIndex = 11;
            const int PropVarIndex = 16;
            const int PropPositionVarIndex = 23;
            const int TreeVarIndex = 31;
            const int TreePositionVarIndex = 40;

            // ILCode argument indexes.
            const int LaneInfoArg = 4;

            // Prop renderer method 1.
            MethodInfo renderProp1 = AccessTools.Method(
                typeof(PropInstance),
                nameof(PropInstance.RenderInstance),
                new Type[] { typeof(RenderManager.CameraInfo), typeof(PropInfo), typeof(InstanceID), typeof(Vector3), typeof(float), typeof(float), typeof(Color), typeof(Vector4), typeof(bool) });

            // Prop renderer method 2.
            MethodInfo renderProp2 = AccessTools.Method(
                typeof(PropInstance),
                nameof(PropInstance.RenderInstance),
                new Type[] { typeof(RenderManager.CameraInfo), typeof(PropInfo), typeof(InstanceID), typeof(Vector3), typeof(float), typeof(float), typeof(Color), typeof(Vector4), typeof(bool), typeof(Texture), typeof(Vector4), typeof(Vector4), typeof(Texture), typeof(Vector4), typeof(Vector4) });

            // Tree renderer call.
            MethodInfo renderTree = AccessTools.Method(
                typeof(global::TreeInstance),
                nameof(global::TreeInstance.RenderInstance),
                new Type[] { typeof(RenderManager.CameraInfo), typeof(TreeInfo), typeof(Vector3), typeof(float), typeof(float), typeof(Vector4), typeof(bool) });

            // Iterate through all instructions in original method.
            foreach (CodeInstruction instruction in instructions)
            {
                // Get next instruction and add it to output.
                yield return instruction;

                if (instruction.Calls(renderProp1) || instruction.Calls(renderProp2))
                {
                    // Insert call to PropOverlays.Highlight method after original call.
                    Logging.KeyMessage("adding network HighlightProp call after RenderInstance");
                    yield return new CodeInstruction(OpCodes.Ldloc_S, PropVarIndex);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, PropPositionVarIndex);
                    yield return new CodeInstruction(OpCodes.Ldarg_2); // segment ID
                    yield return new CodeInstruction(OpCodes.Ldarg_S, LaneInfoArg); // NetInfo.Lane
                    yield return new CodeInstruction(OpCodes.Ldloc_S, propLoopIndex); // index
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RenderOverlays), nameof(RenderOverlays.HighlightNetworkProp)));
                }
                else
                if (instruction.Calls(renderTree))
                {
                    // Insert call to PropOverlays.Highlight method after original call.
                    Logging.KeyMessage("adding network HighlightTree call after RenderInstance");
                    yield return new CodeInstruction(OpCodes.Ldloc_S, TreeVarIndex);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, TreePositionVarIndex);
                    yield return new CodeInstruction(OpCodes.Ldarg_2); // segment ID
                    yield return new CodeInstruction(OpCodes.Ldarg_S, LaneInfoArg); // NetInfo.Lane
                    yield return new CodeInstruction(OpCodes.Ldloc_S, propLoopIndex); // index
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RenderOverlays), nameof(RenderOverlays.HighlightNetworkTree)));
                }
            }
        }

        /// <summary>
        /// Harmony transpiler for TreeInstance.RenderInstance, to insert calls to highlight selected props/tree.
        /// </summary>
        /// <param name="instructions">Original ILCode.</param>
        /// <returns>Patched ILCode.</returns>
        public static IEnumerable<CodeInstruction> TreeTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            // ILCode local variable indexes.
            const int TreeVarIndex = 0;
            const int TreePositionVarIndex = 1;

            // Tree renderer call.
            MethodInfo renderTree = AccessTools.Method(
                typeof(global::TreeInstance),
                nameof(global::TreeInstance.RenderInstance),
                new Type[] { typeof(RenderManager.CameraInfo), typeof(TreeInfo), typeof(Vector3), typeof(float), typeof(float), typeof(Vector4), typeof(bool) });

            // Iterate through all instructions in original method.
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;

                // Does this instruction call TreeInstance.RenderTree?
                if (instruction.Calls(renderTree))
                {
                    // Yes - insert call to PropOverlays.Highlight method after original call.
                    yield return new CodeInstruction(OpCodes.Ldloc_S, TreeVarIndex);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, TreePositionVarIndex);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RenderOverlays), nameof(RenderOverlays.HighlightTree)));
                }
            }
        }

        /// <summary>
        /// Harmony transpiler for PropInstance.RenderInstance, to insert calls to highlight selected map prop.
        /// </summary>
        /// <param name="instructions">Original ILCode.</param>
        /// <returns>Patched ILCode.</returns>
        public static IEnumerable<CodeInstruction> PropTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            // ILCode local variable indexes.
            const int PropVarIndex = 0;
            const int PropPositionVarIndex = 1;

            // Prop renderer method 1.
            MethodInfo renderProp1 = AccessTools.Method(
                typeof(PropInstance),
                nameof(PropInstance.RenderInstance),
                new Type[] { typeof(RenderManager.CameraInfo), typeof(PropInfo), typeof(InstanceID), typeof(Vector3), typeof(float), typeof(float), typeof(Color), typeof(Vector4), typeof(bool) });

            // Prop renderer method 2.
            MethodInfo renderProp2 = AccessTools.Method(
                typeof(PropInstance),
                nameof(PropInstance.RenderInstance),
                new Type[] { typeof(RenderManager.CameraInfo), typeof(PropInfo), typeof(InstanceID), typeof(Vector3), typeof(float), typeof(float), typeof(Color), typeof(Vector4), typeof(bool), typeof(Texture), typeof(Vector4), typeof(Vector4) });

            // Prop renderer method 3.
            MethodInfo renderProp3 = AccessTools.Method(
                typeof(PropInstance),
                nameof(PropInstance.RenderInstance),
                new Type[] { typeof(RenderManager.CameraInfo), typeof(PropInfo), typeof(InstanceID), typeof(Vector3), typeof(float), typeof(float), typeof(Color), typeof(Vector4), typeof(bool), typeof(Texture), typeof(Vector4), typeof(Vector4), typeof(Texture), typeof(Vector4), typeof(Vector4) });

            // Iterate through all instructions in original method.
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;

                // Is this instruction a call to PropInstance.RenderInstance?
                if (instruction.Calls(renderProp1) || instruction.Calls(renderProp2) || instruction.Calls(renderProp3))
                {
                    // Yes - insert call to PropOverlays.Highlight method after original call.
                    yield return new CodeInstruction(OpCodes.Ldloc_S, PropVarIndex);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, PropPositionVarIndex);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RenderOverlays), nameof(RenderOverlays.HighlightProp)));
                }
            }
        }
    }
}