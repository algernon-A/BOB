// <copyright file="ReplacementPriority.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    /// <summary>
    /// Replacement priority.
    /// Higher values have higher priority.
    /// </summary>
    public enum ReplacementPriority : byte
    {
        /// <summary>
        /// No replacement is currently applied.
        /// </summary>
        NoReplacement = 0,

        /// <summary>
        /// Replacement packs.
        /// </summary>
        PackReplacement,

        /// <summary>
        /// All- replacements.
        /// </summary>
        AllReplacement,

        /// <summary>
        /// Grouped replacements.
        /// </summary>
        GroupedReplacement,

        /// <summary>
        /// Individual replacements.
        /// </summary>
        IndividualReplacement,

        InstanceGroupedReplacement,
        InstanceIndividualReplacement,

        /// <summary>
        /// Added props
        /// </summary>
        AddedReplacement,
    }
}