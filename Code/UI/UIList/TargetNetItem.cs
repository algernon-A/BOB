// <copyright file="TargetNetItem.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System.Collections.Generic;
    using System.Text;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Data record for UIList line items for building props.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Internal data class")]
    internal class TargetNetItem : TargetListItem
    {
        /// <summary>
        /// The single lane index for this item (-1 for multiple).
        /// </summary>
        internal int LaneIndex;

        /// <summary>
        /// The list of multiple lane indexes for this item.
        /// </summary>
        internal List<int> LaneIndexes = new List<int>();

        /// <summary>
        /// The active individual replacement, if any, for this item.
        /// </summary>
        internal BOBConfig.NetReplacement IndividualReplacement;

        /// <summary>
        /// The active groped replacement, if any, for this item.
        /// </summary>
        internal BOBConfig.NetReplacement GroupedReplacement;

        /// <summary>
        /// The active all- replacement, if any, for this item.
        /// </summary>
        internal BOBConfig.NetReplacement AllReplacement;

        /// <summary>
        /// The active pack replacement, if any, for this item.
        /// </summary>
        internal BOBConfig.NetReplacement PackReplacement;

        /// <summary>
        /// The original repeat distance for this item (before replacement).
        /// </summary>
        internal float OriginalRepeat;

        /// <summary>
        /// Gets a value indicating whether there's a currently active replacement for this item.
        /// </summary>
        internal override bool HasActiveReplacement => IndividualReplacement != null | GroupedReplacement != null | AllReplacement != null | PackReplacement != null;

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
                lineSprite.atlas = UITextures.LoadQuadSpriteAtlas("BOB-BOB-RoadSmall");
                lineSprite.tooltip = Translations.Translate("BOB_SPR_SNT");
                lineSprite.Show();
            }
            else if (GroupedReplacement != null)
            {
                // Grouped replacement.
                lineSprite.atlas = UITextures.LoadQuadSpriteAtlas("BOB-RoadSmall");
                lineSprite.tooltip = Translations.Translate("BOB_SPR_SNT");
                lineSprite.Show();
            }
            else if (AllReplacement != null)
            {
                // All- replacement.
                lineSprite.atlas = UITextures.LoadQuadSpriteAtlas("BOB-RoadsSmall");
                lineSprite.tooltip = Translations.Translate("BOB_SPR_ANT");
                lineSprite.Show();
            }
            else if (PackReplacement != null)
            {
                // All- replacement.
                lineSprite.atlas = UITextures.LoadQuadSpriteAtlas("BOB-RoadsSmall");
                lineSprite.tooltip = Translations.Translate("BOB_SPR_ANT");
                lineSprite.Show();
            }
            else
            {
                // No valid replacement; hide the sprite.
                lineSprite.Hide();
            }
        }

        /// <summary>
        /// Sets the provided UILabel to display the relevant index number for this item (empty string if this item isn't an individual replacement).
        /// </summary>
        /// <param name="displayLabel">Label to display on.</param>
        internal override void DisplayIndexNumber(UILabel displayLabel) => displayLabel.text = PropIndex >= 0 ? PropIndex.ToString() + ' ' + LaneIndex.ToString() : string.Empty;
    }
}