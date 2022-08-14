// <copyright file="Escape.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using HarmonyLib;

    /// <summary>
    /// Harmony patch to implement escape key handling.
    /// </summary>
    [HarmonyPatch(typeof(GameKeyShortcuts), "Escape")]
    public static class Escape
    {
        /// <summary>
        /// Harmony prefix patch to cancel the BOB tool when it's active and the escape key is pressed.
        /// </summary>
        /// <returns>True (continue on to game method) if the BOB tool isn't already active, false (pre-empt game method) otherwise.</returns>
        public static bool Prefix()
        {
            // Is the BOB tool active?
            if (BOBTool.IsActiveTool)
            {
                // Yes; toggle tool status and return false (pre-empt original method).
                BOBTool.ToggleTool();
                return false;
            }

            // Tool not active - don't do anything, just go on to game code.
            return true;
        }
    }
}