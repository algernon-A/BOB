using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using ColossalFramework;
using HarmonyLib;


namespace BOB
{
    public struct OverlayData
    {
        public Vector3 position;
        public float radius;
    }

    /// <summary>
    /// Harmony patches and associated methods for rendering selection overlays for buildings.
    /// </summary>
    public static class BuildingOverlays
    {
        // List of positions to highlight.
        private readonly static List<OverlayData> overlays = new List<OverlayData>();


        // Props and trees to highlight.
        internal static int CurrentIndex { get; set; }
        internal static PropInfo CurrentProp { get; set; }
        internal static TreeInfo CurrentTree { get; set; }


        /// <summary>
        /// Render highlight for each selected prop or tree.
        /// Should be called as a Harmony Postfix to BuildingManager.EndOverlay.
        /// </summary>
        /// <param name="cameraInfo">Current camera</param>
        public static void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            // 'Contracting circle' effect constants.
            const float Lifetime = 0.5f;
            const float EffectDistance = 4f;
            Color circleColor = new Color(1f, 0f, 1f, 1f);

            // Instance reference.
            RenderManager renderManager = Singleton<RenderManager>.instance;

            // 'Contracting circle' outside actual highlighted circle.  Alpha increases as it moves inwards.
            float effectRadius = Mathf.Lerp(EffectDistance, 0f, (Singleton<SimulationManager>.instance.m_realTimer % Lifetime) / Lifetime);
            Color effectColor = new Color(circleColor.r, circleColor.g, circleColor.b, (EffectDistance - effectRadius) * (1f / EffectDistance));

            // Draw circle and effect at each position in list.
            foreach (OverlayData data in overlays)
            {
                renderManager.OverlayEffect.DrawCircle(cameraInfo, circleColor, data.position, data.radius, -1f, 1280f, false, true);
                renderManager.OverlayEffect.DrawCircle(cameraInfo, effectColor, data.position, data.radius + effectRadius, -1f, 1280f, false, true);
            }

            // All done - clear the list.
            overlays.Clear();
        }



        /// <summary>
        /// Add prop to the list to be highlighted.
        /// </summary>
        /// <param name="index">Prop index</param>
        /// <param name="prop">Prop info</param>
        /// <param name="building">Building data</param>
        /// <param name="position">Prop position</param>
        public static void HighlightProp(int index, PropInfo prop, ref Building building, Vector3 position)
        {
            if (InfoPanelManager.Panel != null)
            {
                if (CurrentProp != null && CurrentProp == prop && (CurrentIndex < 0 || CurrentIndex == index))
                {
                    // Calculate radius of effect - largest of x and z size of props (minimum of 1 in any case).
                    Vector3 size = prop.m_mesh.bounds.size;
                    overlays.Add(new OverlayData { position = position, radius = Mathf.Max(2f, size.x, size.z) });
                }
            }
        }


        /// <summary>
        /// Add tree to the list to be highlighted.
        /// </summary>
        /// <param name="index">Tree index</param>
        /// <param name="tree">Tree info</param>
        /// <param name="building">Building data</param>
        /// <param name="position">Tree position</param>
        public static void HighlightTree(int index, TreeInfo tree, ref Building building, Vector3 position)
        {
            if (InfoPanelManager.Panel != null)
            {
                if (CurrentTree != null && CurrentTree == tree && (CurrentIndex < 0 || CurrentIndex == index))
                {
                    // Calculate radius of effect - largest of x and z size of props (minimum of 1 in any case).
                    Vector3 size = tree.m_mesh.bounds.size;
                    overlays.Add(new OverlayData { position = position, radius = Mathf.Max(1f, size.x, size.z) });
                }
            }
        }


        /// <summary>
        /// Harmony transpiler for BuildingAI.RenderProps, to insert calls to highlight selected props/trees
        /// </summary>
        /// <param name="original">Original method</param>
        /// <param name="instructions">Original ILCode</param>
        /// <param name="generator">IL generator</param>
        /// <returns>Patched ILCode</returns>
        public static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // ILCode local variable indexes.
            const int iVarIndex = 11;
            const int propVarIndex = 14;
            const int treeVarIndex = 15;
            const int propPositionVarIndex = 18;
            const int treePositionVarIndex = 29;
            const int dataVector2VarIndex = 30;

            // ILCode argument indexes.
            const int buildingArg = 3;
            const int isActiveArg = 8;

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
                if (instruction.opcode == OpCodes.Ldarg_S)
                {
                    // ldarg.s isActive.
                    if (instruction.operand is byte arg && arg == isActiveArg)
                    {
                        foundPropCandidate = true;
                    }

                }
                else if (instruction.opcode == OpCodes.Ldloc_3)
                {
                    // ldloc.3
                    foundPropCandidate = true;
                }
                else if (instruction.opcode == OpCodes.Ldfld)
                {
                    // ldfld - a lot of false positives here.
                    foundPropCandidate = true;
                }
                else if (instruction.opcode == OpCodes.Ldloc_S && instruction.operand is LocalBuilder localBuilder && localBuilder.LocalIndex == dataVector2VarIndex)
                {
                    // Tree candidate - ldloc.s 2
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
                            Logging.KeyMessage("adding building Highlight", candidateName, " call after RenderInstance");
                            yield return new CodeInstruction(OpCodes.Ldloc_S, iVarIndex);
                            yield return new CodeInstruction(OpCodes.Ldloc_S, prefabIndex);
                            yield return new CodeInstruction(OpCodes.Ldarg_S, buildingArg);
                            yield return new CodeInstruction(OpCodes.Ldloc_S, positionIndex);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BuildingOverlays), methodName));
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