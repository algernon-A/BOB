// <copyright file="IndividualBuildingReplacement.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System.Collections.Generic;
    using AlgernonCommons;

    /// <summary>
    /// Class to manage individual building prop and tree replacements.
    /// </summary>
    internal class IndividualBuildingReplacement : BuildingReplacementBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IndividualBuildingReplacement"/> class.
        /// </summary>
        internal IndividualBuildingReplacement()
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the active instance.
        /// </summary>
        internal static IndividualBuildingReplacement Instance { get; private set; }

        /// <summary>
        /// Gets the config file list of building elements relevant to the current replacement type.
        /// </summary>
        protected override List<BOBConfig.BuildingElement> BuildingElementList => ConfigurationUtils.CurrentConfig.IndBuildings;

        /// <summary>
        /// Gets the priority level of this replacmeent type.
        /// </summary>
        protected override ReplacementPriority ThisPriority => ReplacementPriority.IndividualReplacement;

        /// <summary>
        /// Finds any existing replacement relevant to the provided arguments.
        /// </summary>
        /// <param name="buildingInfo">Building prefab.</param>
        /// <param name="propIndex">Prop index.</param>
        /// <param name="targetInfo">Target prop/tree prefab (ignored).</param>
        /// <returns>Existing replacement entry, if one was found, otherwise null.</returns>
        protected override BOBConfig.BuildingReplacement FindReplacement(BuildingInfo buildingInfo, int propIndex, PrefabInfo targetInfo) =>
            ReplacementList(buildingInfo)?.Find(x => x.PropIndex == propIndex);

        /// <summary>
        /// Applies a replacement.
        /// </summary>
        /// <param name="replacement">Replacement record to apply.</param>
        protected override void ApplyReplacement(BOBConfig.BuildingReplacement replacement)
        {
            // Don't do anything if prefabs can't be found.
            if (replacement?.TargetInfo == null || replacement.ReplacementInfo == null || replacement.BuildingInfo == null)
            {
                return;
            }

            // Check index bounds.
            if (replacement.BuildingInfo.m_props == null || replacement.PropIndex < 0 || replacement.PropIndex >= replacement.BuildingInfo.m_props.Length)
            {
                Logging.Message("ignoring invalid individual building replacement index ", replacement.PropIndex, " for building ", replacement.BuildingInfo.name);
                return;
            }

            // Don't apply replacement if this is an added prop.
            if (AddedBuildingProps.Instance.IsAdded(replacement.BuildingInfo, replacement.PropIndex))
            {
                return;
            }

            // Check prop for null.
            BuildingInfo.Prop thisProp = replacement.BuildingInfo.m_props[replacement.PropIndex];
            if (thisProp == null)
            {
                return;
            }

            // Set the new replacement.
            BuildingHandlers.GetOrAddHandler(replacement.BuildingInfo, replacement.PropIndex).SetReplacement(replacement, ThisPriority);
        }
    }
}
