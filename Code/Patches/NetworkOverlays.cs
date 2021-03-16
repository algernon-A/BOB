using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using ColossalFramework;
using HarmonyLib;


namespace BOB
{
    /// <summary>
    /// Harmony patches and associated methods for rendering selection overlays for buildings.
    /// </summary>
    public static class NetOverlays
    {
        // List of positions to highlight.
        private readonly static List<Vector3> effectPositions = new List<Vector3>();


        // Prop to highlight.
        internal static PropInfo CurrentProp => BuildingOverlays.CurrentProp;
        internal static TreeInfo CurrentTree => BuildingOverlays.CurrentTree;


        /// <summary>
        /// Render highlight for each selected prop or tree.
        /// Should be called as a Harmony Postfix to BuildingManager.EndOverlay.
        /// </summary>
        /// <param name="cameraInfo">Current camera</param>
        public static void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            // 'Contracting circle' effect constants.
            const float Lifetime = 0.5f;
            const float Distance = 4f;
            Color circleColor = new Color(1f, 0f, 1f, 1f);

            // Instance reference.
            RenderManager renderManager = Singleton<RenderManager>.instance;

            // 'Contracting circle' outside actual highlighted circle.  Alpha increases as it moves inwards.
            float effectRadius = Mathf.Lerp(Distance, 0f, (Singleton<SimulationManager>.instance.m_realTimer % Lifetime) / Lifetime);
            Color effectColor = new Color(circleColor.r, circleColor.g, circleColor.b, (Distance - effectRadius) * (1f / Distance));

            // Draw circle and effect at each position in list.
            foreach (Vector3 position in effectPositions)
            {
                renderManager.OverlayEffect.DrawCircle(cameraInfo, circleColor, position, 2f, -1f, 1280f, false, true);
                renderManager.OverlayEffect.DrawCircle(cameraInfo, effectColor, position, 2f + effectRadius, -1f, 1280f, false, true);
            }

            // All done - clear the list.
            effectPositions.Clear();
        }


        /// <summary>
        /// Add prop to the list to be highlighted.
        /// </summary>
        /// <param name="prop">Prop info</param>
        /// <param name="position">Prop position</param>
        public static void HighlightProp(PropInfo prop, Vector3 position)
        {
            if (InfoPanelManager.Panel != null)
            {
                if (CurrentProp != null && CurrentProp == prop)
                {
                    effectPositions.Add(position);
                }
            }
        }


        /// <summary>
        /// Add tree to the list to be highlighted.
        /// </summary>
        /// <param name="tree">Tree info</param>
        /// <param name="position">Tree position</param>
        public static void HighlightTree(TreeInfo tree, Vector3 position)
        {
            if (InfoPanelManager.Panel != null)
            {
                if (CurrentTree != null && CurrentTree == tree)
                {
                    effectPositions.Add(position);
                }
            }
        }


        /// <summary>
        /// Harmony transpiler for NetLane.RenderInstance, to insert calls to highlight selected props/tree.
        /// </summary>
        /// <param name="original">Original method</param>
        /// <param name="instructions">Original ILCode</param>
        /// <param name="generator">IL generator</param>
        /// <returns>Patched ILCode</returns>
        public static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // ILCode local variable indexes.
            const int surfaceMapping2Index = 9;
            const int iVarIndex = 11;
            const int propVarIndex = 16;
            const int propPositionVarIndex = 23;
            const int treeVarIndex = 31;
            const int treePositionVarIndex = 38;


            // Instruction parsing.
            IEnumerator<CodeInstruction> instructionsEnumerator = instructions.GetEnumerator();
            CodeInstruction instruction;
            bool foundPropCandidate = false, foundTreeCandidate = false;
            string candidateName, methodName;
            int prefabIndex, positionIndex;


            // Iterate through all instructions in original method.
            while (instructionsEnumerator.MoveNext())
            {
                // Get next instruction and add it to output.
                instruction = instructionsEnumerator.Current;
                yield return instruction;

                // Looking for possible precursor calls to "Void RenderInstance".
                if (instruction.opcode == OpCodes.Ldloc_S && instruction.operand is LocalBuilder localBuilder && localBuilder.LocalIndex == surfaceMapping2Index)
                {
                    // ldloc.s 9
                    foundPropCandidate = true;
                }
                else if (instruction.opcode == OpCodes.Ldc_I4_1)
                {
                    // ldc.i4.1.
                    foundPropCandidate = true;
                }
                else if (instruction.opcode == OpCodes.Ldsfld)
                {
                    // Tree candidate - ldsfld
                    foundTreeCandidate = true;
                }

                // Did we find a possible candidate?
                if (foundPropCandidate || foundTreeCandidate)
                {
                    // Yes - are there following instructions?
                    if (instructionsEnumerator.MoveNext())
                    {
                        // Yes - get the next instruction.
                        instruction = instructionsEnumerator.Current;
                        yield return instruction;

                        // Is this new instruction a call to Void RenderInstance?
                        if (instruction.opcode == OpCodes.Call && instruction.operand.ToString().StartsWith("Void RenderInstance"))
                        {
                            // Yes - prop or tree?
                            if (foundPropCandidate)
                            {
                                // Prop.
                                candidateName = "Prop";
                                prefabIndex = propVarIndex;
                                positionIndex = propPositionVarIndex;
                                methodName = nameof(BuildingOverlays.HighlightProp);
                            }
                            else
                            {
                                // Tree.
                                candidateName = "Tree";
                                prefabIndex = treeVarIndex;
                                positionIndex = treePositionVarIndex;
                                methodName = nameof(BuildingOverlays.HighlightTree);
                            }

                            // Insert call to PropOverlays.Highlight method after original call.
                            Logging.KeyMessage("adding network Highlight", candidateName, " call after RenderInstance");
                            yield return new CodeInstruction(OpCodes.Ldloc_S, prefabIndex);
                            yield return new CodeInstruction(OpCodes.Ldloc_S, positionIndex);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetOverlays), methodName));
                        }
                    }

                    // Reset flags for next pass.
                    foundPropCandidate = false;
                    foundTreeCandidate = false;
                }
            }
        }
    }
}