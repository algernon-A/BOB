// <copyright file="TargetListItem.cs" company="algernon (K. Algernon A. Sheppard)">
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
    /// Data record for UIList line items for building and network target props.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Internal data class")]
    internal class TargetListItem
    {
        /// <summary>
        /// The single prop index for this item (-1 for multiple).
        /// </summary>
        internal int PropIndex;

        /// <summary>
        /// The list of multiple prop indexes for this item.
        /// </summary>
        internal List<int> PropIndexes = new List<int>();

        /// <summary>
        /// Whether or not to show probabilities.
        /// </summary>
        internal bool ShowProbs = false;

        /// <summary>
        /// The replacent prefab for this item (null if none).
        /// </summary>
        internal PrefabInfo ReplacementPrefab;

        /// <summary>
        /// The replacent probability for this item (-1 if none).
        /// </summary>
        internal int ReplacementProbability = -1;

        /// <summary>
        /// The original prefab for this item (before replacement).
        /// </summary>
        internal PrefabInfo OriginalPrefab;

        /// <summary>
        /// The original probability for this item (before replacement).
        /// </summary>
        internal int OriginalProbability;

        /// <summary>
        /// Gets the current prefab for this item (replacement, or original if no replacment).
        /// </summary>
        internal PrefabInfo ActivePrefab => ReplacementPrefab ?? OriginalPrefab;

        /// <summary>
        /// Gets the current active probability for this item (replacement, or original if no replacment).
        /// </summary>
        internal int ActiveProbability => ReplacementProbability < 0 ? OriginalProbability : ReplacementProbability;

        /// <summary>
        /// Gets the display name for this item.
        /// </summary>
        internal string DisplayName => PrefabLists.GetDisplayName(ActivePrefab);

        /// <summary>
        /// Gets a value indicating whether there's a currently active replacement for this item.
        /// </summary>
        internal virtual bool HasActiveReplacement => false;

        /// <summary>
        /// Gets a value indicating whether or not this is an added prop.
        /// </summary>
        internal virtual bool IsAdded => false;

        /// <summary>
        /// Configures the given line sprite instance to match this item.
        /// </summary>
        /// <param name="lineSprite">Sprite to configure.</param>
        internal virtual void ConfigureLineSprite(UISprite lineSprite)
        {
        }

        /// <summary>
        /// Sets the provided UILabel to display the relevant index number for this item (empty string if this item isn't an individual replacement).
        /// </summary>
        /// <param name="displayLabel">Label to display on.</param>
        internal virtual void DisplayIndexNumber(UILabel displayLabel) => displayLabel.text = PropIndex >= 0 ? PropIndex.ToString() : string.Empty;

        /// <summary>
        /// Generates the list display text for this item.
        /// This includes original prefab name (if applicable), and any probabilities (if option is set).
        /// </summary>
        /// <returns>Generated list display text.</returns>
        internal string GenerateDisplayText()
        {
            // Text to display - StringBuilder due to the amount of manipulation we're doing.
            StringBuilder displayText = new StringBuilder(DisplayName);

            // Show current probability in brackets immediately afterwards.
            if (ShowProbs)
            {
                displayText.Append(" (");
                displayText.Append(ActiveProbability);
                displayText.Append("%)");
            }

            // Was the prefab replaced?
            if (ReplacementPrefab != null & ReplacementPrefab != OriginalPrefab)
            {
                // Yes; append "was " and original prefab name to the display name.
                displayText.Append("; ");
                displayText.Append(Translations.Translate("BOB_ROW_WAS"));
                displayText.Append(" ");
                displayText.Append(PrefabLists.GetDisplayName(OriginalPrefab));

                // Show original probability in brackets immediately afterwards.
                if (ShowProbs)
                {
                    displayText.Append(" (");
                    displayText.Append(OriginalProbability);
                    displayText.Append("%)");
                }
            }

            return displayText.ToString();
        }

        /// <summary>
        /// UIListRow for building target items.
        /// </summary>
        internal class DisplayRow : UIListRow
        {
            // Layout constants.
            private const float IndexWidth = 30f;
            private const float SpriteMargin = 20f;
            private const float IndexLabelX = Margin + SpriteMargin;

            // Row components.
            private UILabel _nameLabel;
            private UILabel _indexLabel;
            private UISprite _lineSprite;

            /// <summary>
            /// Generates and displays a list row.
            /// </summary>
            /// <param name="data">Object data to display.</param>
            /// <param name="rowIndex">Row index number (for background banding).</param>
            public override void Display(object data, int rowIndex)
            {
                // Perform initial setup for new rows.
                if (_nameLabel == null)
                {
                    // Add name label.
                    _nameLabel = AddLabel(Margin, parent.width - Margin - Margin);

                    // Add index text label.
                    _indexLabel = AddLabel(Margin, IndexWidth);
                    _indexLabel.relativePosition = new Vector2(IndexLabelX, 0f);
                }

                // Add line sprite if we need to (initially hidden).
                if (_lineSprite == null)
                {
                    _lineSprite = AddUIComponent<UISprite>();
                    _lineSprite.size = new Vector2(17f, 17f);
                    _lineSprite.relativePosition = new Vector2(3f, 1.5f);
                    _lineSprite.Hide();
                    _lineSprite.spriteName = "normal";
                }

                // Clear label text.
                _indexLabel.text = string.Empty;

                // Set name label.
                if (data is TargetListItem targetItem && targetItem.OriginalPrefab != null)
                {
                    // Configure line sprite.
                    targetItem.ConfigureLineSprite(_lineSprite);

                    // Configure index number display.
                    targetItem.DisplayIndexNumber(_indexLabel);

                    // Set display text.
                    _nameLabel.text = targetItem.GenerateDisplayText();

                    // Adjust name label to accomodate index label width.
                    _nameLabel.relativePosition = new Vector2((targetItem.PropIndex >= 0 ? IndexWidth : 0) + IndexLabelX + Margin, 0f);
                }
                else
                {
                    // Just in case (no valid prefab).
                    _indexLabel.text = string.Empty;
                    _nameLabel.text = string.Empty;
                    _lineSprite.Hide();
                }

                // Set initial background as deselected state.
                Deselect(rowIndex);
            }
        }
    }
}
