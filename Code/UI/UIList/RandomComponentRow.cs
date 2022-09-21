// <copyright file="RandomComponentRow.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// UIList row for random prop/tree variations.
    /// </summary>
    public class RandomComponentRow : UIListRow
    {
        // Layout constants.
        private const float LockSpriteSpace = 20f;

        // Display components.
        private UILabel _nameLabel;
        private UILabel _probLabel;
        private UISprite _lockSprite;

        // Data reference.
        private BOBRandomPrefab.Variation _thisVariation;

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
                // Add name labels.
                _nameLabel = AddLabel(5f, parent.width - LockSpriteSpace - 20f - Margin - Margin);
                _probLabel = AddLabel(Margin, 30f);
                _probLabel.textAlignment = UIHorizontalAlignment.Right;

                // Add sprite.
                _lockSprite = AddUIComponent<UISprite>();
                _lockSprite.size = new Vector2(17f, 17f);
                _lockSprite.atlas = UITextures.LoadQuadSpriteAtlas("BOB-Padlock");

                // Lock sprite event handler.
                _lockSprite.eventClicked += (c, p) =>
                {
                    if (_thisVariation != null)
                    {
                        _thisVariation.ProbLocked = !_thisVariation.ProbLocked;
                        SetLockSprite();
                    }
                };
            }

            if (data is BOBRandomPrefab.Variation variation)
            {
                // Record data.
                _thisVariation = variation;

                // Set label position, text and color.
                _nameLabel.text = variation?.DisplayName ?? "null";
                _nameLabel.textColor = variation.Prefab == null ? Color.gray : Color.white;

                _probLabel.text = (variation?.Probability.ToString() ?? "0") + "%";
                _probLabel.relativePosition = new Vector2(width - LockSpriteSpace - Margin - _probLabel.width, 0f);
            }

            // Set lock sprite.
            SetLockSprite();

            // Set initial background as deselected state.
            Deselect(rowIndex);
        }

        /// <summary>
        /// Sets the state of the probability locked sprite.
        /// </summary>
        private void SetLockSprite()
        {
            // Ensure valid data.
            if (_thisVariation != null & _lockSprite != null)
            {
                // Valid data - show sprite and set state according to data.
                _lockSprite.Show();
                _lockSprite.relativePosition = new Vector2(width - LockSpriteSpace, 3f);
                if (_thisVariation.ProbLocked)
                {
                    _lockSprite.spriteName = "disabled";
                }
                else
                {
                    _lockSprite.spriteName = "pressed";
                }
            }
            else
            {
                // No valid data - hide sprite.
                _lockSprite.Hide();
            }
        }
    }
}