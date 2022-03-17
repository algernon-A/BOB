using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;


namespace BOB
{
    /// <summary>
    /// Harmony patch to disable vanilla road tree replacement.
    /// Applied manually via patcher in response to setting selection.
    /// </summary>
    public static class TreeToolPatch
    {
        // <summary>
        /// Harmony transpiler for TreeTool.SimulationStep, to disable vanilla road tree replacement.
        /// </summary>
        /// <param name="instructions">Original ILCode</param>
        /// <returns>Patched ILCode</returns>
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Instruction parsing.
            IEnumerator<CodeInstruction> instructionsEnumerator = instructions.GetEnumerator();
            CodeInstruction instruction;

            // Iterate through all instructions in original method.
            while (instructionsEnumerator.MoveNext())
            {
                // Get next instruction.
                instruction = instructionsEnumerator.Current;

                // Looking for ldc.i4.S NetSegment.Flags.Untouchable - that's setting the tool's m_ignoreSegmentFlags.
                if (instruction.opcode == OpCodes.Ldc_I4_S && instruction.operand is sbyte operand && operand == (sbyte)NetSegment.Flags.Untouchable)
                {
                    // Replace with NetSegment.Flags.All (-1) to disable tool segment detection.
                    instruction.opcode = OpCodes.Ldc_I4_M1;
                    instruction.operand = null;
                }

                // Add instruction (original or modified) to output.
                yield return instruction;
            }
        }
    }
}