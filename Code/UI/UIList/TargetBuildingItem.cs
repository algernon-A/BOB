// <copyright file="TargetBuildingItem.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System.Text;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Data record for UIList line items for building props.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Internal data class")]
    internal class TargetBuildingItem : TargetListItem
    {
        /// <summary>
        /// The active individual replacement, if any, for this item.
        /// </summary>
        internal BOBConfig.BuildingReplacement IndividualReplacement;

        /// <summary>
        /// The active groped replacement, if any, for this item.
        /// </summary>
        internal BOBConfig.BuildingReplacement GroupedReplacement;

        /// <summary>
        /// The active all- replacement, if any, for this item.
        /// </summary>
        internal BOBConfig.BuildingReplacement AllReplacement;

        /// <summary>
        /// Gets a value indicating whether there's a currently active replacement for this item.
        /// </summary>
        internal override bool HasActiveReplacement => IndividualReplacement != null | GroupedReplacement != null | AllReplacement != null;

        /// <summary>
        /// Configures the given line sprite instance to match this item.
        /// </summary>
        /// <param name="lineSprite">Sprite to configure.</param>
        internal override void ConfigureLineSprite(UISprite lineSprite)
        {
            // Null check.
            if (lineSprite == null)
            {
                return;
            }

            // Check for replacement status to set sprite.
            if (IsAdded)
            {
                // Added prop.
                lineSprite.atlas = UITextures.LoadQuadSpriteAtlas("BOB-RoundPlus");
                lineSprite.tooltip = Translations.Translate("BOB_SPR_ADD");
                lineSprite.Show();
            }
            else if (IndividualReplacement != null)
            {
                // Individual replacement.
                lineSprite.atlas = UITextures.LoadQuadSpriteAtlas("BOB-BuildingSmall");
                lineSprite.tooltip = Translations.Translate("BOB_SPR_SBL");
                lineSprite.Show();
            }
            else if (GroupedReplacement != null)
            {
                // Grouped replacement.
                lineSprite.atlas = UITextures.LoadQuadSpriteAtlas("BOB-BuildingSmall");
                lineSprite.tooltip = Translations.Translate("BOB_SPR_SBL");
                lineSprite.Show();
            }
            else if (AllReplacement != null)
            {
                // All- replacement.
                lineSprite.atlas = UITextures.LoadQuadSpriteAtlas("BOB-BuildingsSmall");
                lineSprite.tooltip = Translations.Translate("BOB_SPR_ABL");
                lineSprite.Show();
            }
            else
            {
                // No valid replacement; hide the sprite.
                lineSprite.Hide();
            }
        }
    }
}