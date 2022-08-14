// <copyright file="LoadedPrefabItem.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Data record for UIList line items for loaded prefabs.
    /// </summary>
    internal class LoadedPrefabItem
    {
        // Private fields.
        private readonly PrefabInfo _prefabInfo;
        private readonly string _displayName;
        private readonly string _creatorName;
        private readonly bool _isVanilla = false;
        private readonly bool _greyed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadedPrefabItem"/> class.
        /// </summary>
        /// <param name="prefabInfo">Prefab for this item.</param>
        internal LoadedPrefabItem(PrefabInfo prefabInfo)
        {
            // Initialise fields.
            _prefabInfo = prefabInfo;
            _displayName = PrefabLists.GetDisplayName(prefabInfo.name);
            _creatorName = PrefabLists.GetCreator(prefabInfo);
            _isVanilla = !prefabInfo.m_isCustomContent;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadedPrefabItem"/> class.
        /// </summary>
        /// <param name="randomPrefabInfo">BOB random prefab for this item.</param>
        internal LoadedPrefabItem(BOBRandomPrefab randomPrefabInfo)
        {
            // Initialise fields.
            _prefabInfo = (PrefabInfo)randomPrefabInfo.Prop ?? randomPrefabInfo.Tree;

            _displayName = PrefabLists.GetDisplayName(_prefabInfo.name);
            _creatorName = string.Empty;

            // Grey this item if not all variants are loaded.
            _greyed = randomPrefabInfo.MissingVariant;
        }

        /// <summary>
        /// Gets the prefab info for this item.
        /// </summary>
        internal PrefabInfo Prefab => _prefabInfo;

        /// <summary>
        /// Gets the prefab info for this item as PropInfo.
        /// </summary>
        internal PropInfo Prop => _prefabInfo as PropInfo;

        /// <summary>
        /// Gets the display name of this item.
        /// </summary>
        internal string DisplayName => _displayName;

        /// <summary>
        /// Gets the creator's name of this item (empty string if none).
        /// </summary>
        internal string CreatorName => _creatorName;

        /// <summary>
        /// Gets a value indicating whether this is a vanilla prefab.
        /// </summary>
        internal bool IsVanilla => _isVanilla;

        /// <summary>
        /// UIListRow for prop/tree prefabs.
        /// </summary>
        internal class DisplayRow : UIListRow
        {
            /// <summary>
            /// Creator label relative X position.
            /// </summary>
            internal const float CreatorLabelX = 260f;

            // Display labels.
            private UILabel _nameLabel;
            private UILabel _creatorLabel;

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
                    _creatorLabel = AddLabel(CreatorLabelX, parent.width - CreatorLabelX);
                    _nameLabel = AddLabel(Margin, parent.width - Margin - Margin);
                }

                // Set label default white colour.
                _nameLabel.textColor = Color.white;

                if (data is LoadedPrefabItem loadedPrefabItem)
                {
                    _nameLabel.text = loadedPrefabItem._displayName;
                    _creatorLabel.text = loadedPrefabItem._creatorName;
                    _nameLabel.textColor = loadedPrefabItem._greyed ? Color.gray : Color.white;
                }
                else
                {
                    // Just in case (no valid data).
                    _nameLabel.text = string.Empty;
                    _creatorLabel.text = string.Empty;
                }

                // Set initial background as deselected state.
                Deselect(rowIndex);
            }
        }
    }
}
