using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;


namespace BOB
{
    /// <summary>
    /// Harmony transpilers for registering overlays to render.
    /// </summary>
    public static class OverlayTranspilers
    {
        /// <summary>
        /// Harmony transpiler for BuildingAI.RenderProps, to insert calls to highlight selected props/trees
        /// </summary>
        /// <param name="original">Original method</param>
        /// <param name="instructions">Original ILCode</param>
        /// <param name="generator">IL generator</param>
        /// <returns>Patched ILCode</returns>
        public static IEnumerable<CodeInstruction> BuildingTranspiler(MethodBase original, IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
                                methodName = nameof(RenderOverlays.HighlightBuildingProp);
                            }
                            else
                            {
                                // Tree.
                                candidateName = "Tree";
                                prefabIndex = treeVarIndex;
                                positionIndex = treePositionVarIndex;
                                methodName = nameof(RenderOverlays.HighlightBuildingTree);
                            }

                            // Insert call to PropOverlays.Highlight method after original call.
                            Logging.KeyMessage("adding building Highlight", candidateName, " call after RenderInstance");
                            yield return new CodeInstruction(OpCodes.Ldloc_S, iVarIndex);
                            yield return new CodeInstruction(OpCodes.Ldloc_S, prefabIndex);
                            yield return new CodeInstruction(OpCodes.Ldarg_S, buildingArg);
                            yield return new CodeInstruction(OpCodes.Ldloc_S, positionIndex);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RenderOverlays), methodName));
                        }
                    }

                    // Reset flags for next pass.
                    foundPropCandidate = false;
                    foundTreeCandidate = false;
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
        public static IEnumerable<CodeInstruction> NetTranspiler(MethodBase original, IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
                                methodName = nameof(RenderOverlays.HighlightProp);
                            }
                            else
                            {
                                // Tree.
                                candidateName = "Tree";
                                prefabIndex = treeVarIndex;
                                positionIndex = treePositionVarIndex;
                                methodName = nameof(RenderOverlays.HighlightTree);
                            }

                            // Insert call to PropOverlays.Highlight method after original call.
                            Logging.KeyMessage("adding network Highlight", candidateName, " call after RenderInstance");
                            yield return new CodeInstruction(OpCodes.Ldloc_S, prefabIndex);
                            yield return new CodeInstruction(OpCodes.Ldloc_S, positionIndex);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RenderOverlays), methodName));
                        }
                    }

                    // Reset flags for next pass.
                    foundPropCandidate = false;
                    foundTreeCandidate = false;
                }
            }
        }


        /// <summary>
        /// Harmony transpiler for TreeInstance.RenderInstance, to insert calls to highlight selected props/tree.
        /// </summary>
        /// <param name="original">Original method</param>
        /// <param name="instructions">Original ILCode</param>
        /// <param name="generator">IL generator</param>
        /// <returns>Patched ILCode</returns>
        public static IEnumerable<CodeInstruction> TreeTranspiler(MethodBase original, IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // ILCode local variable indexes.
            const int treeVarIndex = 0;
            const int treePositionVarIndex = 1;
            const int defaultColorLocationVarIndex = 5;


            // Instruction parsing.
            IEnumerator<CodeInstruction> instructionsEnumerator = instructions.GetEnumerator();
            CodeInstruction instruction;

            // Iterate through all instructions in original method.
            while (instructionsEnumerator.MoveNext())
            {
                // Get next instruction and add it to output.
                instruction = instructionsEnumerator.Current;
                yield return instruction;

                // Looking for possible precursor calls to "Void DrawMesh".
                if (instruction.opcode == OpCodes.Ldloc_S && instruction.operand is LocalBuilder localBuilder && localBuilder.LocalIndex == defaultColorLocationVarIndex)
                {
                    // Found ldloc.1 - are there following instructions?
                    if (instructionsEnumerator.MoveNext())
                    {
                        // Yes - get the next instruction.
                        instruction = instructionsEnumerator.Current;
                        yield return instruction;

                        // Is this new instruction a call to Void RenderInstance?

                        if (instruction.opcode == OpCodes.Call && instruction.operand.ToString().StartsWith("Void RenderInstance"))
                        {
                            // Yes - insert call to PropOverlays.Highlight method after original call.
                            Logging.KeyMessage("adding tree Highlight call after RenderInstance");
                            yield return new CodeInstruction(OpCodes.Ldloc_S, treeVarIndex);
                            yield return new CodeInstruction(OpCodes.Ldloc_S, treePositionVarIndex);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RenderOverlays), nameof(RenderOverlays.HighlightTree)));
                        }
                    }
                }
            }
        }
    }
}