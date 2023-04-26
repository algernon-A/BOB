// <copyright file="NetSegmentPatches.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB.Skins
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using AlgernonCommons;
    using HarmonyLib;

    /// <summary>
    /// Harmony patches to implement network skinning.
    /// </summary>
    [HarmonyPatch(typeof(NetSegment))]
    internal static class NetSegmentPatches
    {
        /// <summary>
        /// Harmony transpiler to NetSegment.RenderInstance to implement network skinning.
        /// </summary>
        /// <param name="instructions">Original ILCode.</param>
        /// <returns>Modified ILCode.</returns>
        [HarmonyPatch(
            nameof(NetSegment.RenderInstance),
            new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(int), typeof(NetInfo), typeof(RenderManager.Instance) },
            new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Ref })]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> AfterTerrainUpdatedTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            // NetInfo segment and lane fields.
            FieldInfo m_segments = AccessTools.Field(typeof(NetInfo), nameof(NetInfo.m_segments));
            FieldInfo m_lanes = AccessTools.Field(typeof(NetInfo), nameof(NetInfo.m_lanes));

            // Custom segment and lane fields.
            FieldInfo segmentSkins = AccessTools.Field(typeof(NetworkSkin), nameof(NetworkSkin.Segments));
            FieldInfo laneSkins = AccessTools.Field(typeof(NetworkSkin), nameof(NetworkSkin.Lanes));

            // Iterate through each instruction.
            foreach (CodeInstruction instruction in instructions)
            {
                // Looking for loading the target fields.
                if (instruction.LoadsField(m_segments))
                {
                    // Loads m_segments - replace with call to our custom method.
                    Logging.KeyMessage("found load m_segments");

                    // Arg 2 is segmentID.
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetSegmentPatches), nameof(GetSegments)));
                    continue;
                }
                else if (instruction.LoadsField(m_lanes))
                {
                    // Loads m_lanes - replace with call to our custom method.
                    Logging.KeyMessage("found load m_lanes");

                    // Arg 2 is segmentID.
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetSegmentPatches), nameof(GetLanes)));
                    continue;
                }

                yield return instruction;
            }
        }

        /// <summary>
        /// Gets the active <see cref="NetInfo.Segment"/> array for the given segment.
        /// This will be a segment skin if any is active, or the default <see cref="NetInfo.m_segments"/> array otherwise.
        /// </summary>
        /// <param name="info"><see cref="NetInfo"/> prefab for this segment.</param>
        /// <param name="segmentID">Segment ID.</param>
        /// <returns><see cref="NetInfo.Segment"/> array to use for this segment.</returns>
        private static NetInfo.Segment[] GetSegments(NetInfo info, uint segmentID)
        {
            // Try to retrieve an active skin.
            NetworkSkin skin = NetworkSkins.SegmentSkins[segmentID];

            // If no active skin, then return the info default (m_segments).
            if (skin == null)
            {
                return info.m_segments;
            }

            return skin.Segments;
        }

        /// <summary>
        /// Gets the active <see cref="NetInfo.Lane"/> array for the given segment.
        /// This will be a segment skin if any is active, or the default <see cref="NetInfo.m_lanes"/> array otherwise.
        /// </summary>
        /// <param name="info"><see cref="NetInfo"/> prefab for this segment.</param>
        /// <param name="segmentID">Segment ID.</param>
        /// <returns><see cref="NetInfo.Segment"/> array to use for this segment.</returns>
        private static NetInfo.Lane[] GetLanes(NetInfo info, uint segmentID)
        {
            // Try to retrieve an active skin.
            NetworkSkin skin = NetworkSkins.SegmentSkins[segmentID];

            // If no active skin, then return the info default (m_lanes).
            if (skin == null)
            {
                return info.m_lanes;
            }

            return skin.Lanes;
        }
    }
}