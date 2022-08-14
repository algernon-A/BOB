namespace BOB
{
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Prop row fastlist item for loaded props/trees.
    /// </summary>
    public class UILoadedPropRow : UIPropRow
    {
        protected PrefabInfo thisPrefab;
        protected LoadedListItem thisListItem;
        private string displayName;


        /// <summary>
        /// Called when this item is selected.
        /// </summary>
        public override void UpdateSelection()
        {
            // Update currently selected replacement prefab.
            BOBPanelManager.Panel.SelectedReplacementPrefab = thisPrefab;
        }


        /// <summary>
        /// Generates and displays a list row.
        /// </summary>
        /// <param name="data">Object to list</param>
        /// <param name="isRowOdd">If the row is an odd-numbered row (for background banding)</param>
        public override void Display(object data, bool isRowOdd)
        {
            // Perform initial setup for new rows.
            if (nameLabel == null)
            {
                isVisible = true;
                canFocus = true;
                isInteractive = true;
                width = parent.width;
                height = RowHeight;

                // Add object name label.
                nameLabel = AddUIComponent<UILabel>();
                nameLabel.width = this.width - 10f;
                nameLabel.textScale = TextScale;

                labelX = LeftMargin;
            }

            // Set label position and default white colour.
            nameLabel.relativePosition = new Vector2(LeftMargin, PaddingY);
            nameLabel.textColor = Color.white;

            // Set data record and calculate name - depends if 'pure' prefab or BOB random prefab.
            if (data is BOBRandomPrefab randomPrefab)
            {
                // BOB random prefab.
                if (randomPrefab.Prop != null)
                {
                    thisPrefab = randomPrefab.Prop;
                }
                else
                {
                    thisPrefab = randomPrefab.Tree;
                }

                // Grey colour for random props with missing variants.
                if (randomPrefab.MissingVariant)
                {
                    nameLabel.textColor = Color.grey;
                }
            }
            else if (data is LoadedListItem loadedItem)
            {
                // Loaded list item.
                thisListItem = loadedItem;
                thisPrefab = loadedItem.thisPrefab;
            }
            else
            {
                // Standard PropInfo/TreeInfo prefab.
                thisPrefab = data as PrefabInfo;
            }

            displayName = PrefabLists.GetDisplayName(thisPrefab);
            nameLabel.text = displayName;

            // Set initial background as deselected state.
            Deselect(isRowOdd);
        }
    }


    /// <summary>
    /// Prop row fastlist item for building props/trees.
    /// </summary>
    public class UIPrefabPropRow : UIPropRow
    {
        /// <summary>
        /// Called when this item is selected.
        /// </summary>
        public override void UpdateSelection()
        {
            // Update currently selected target prefab.
            BOBPanelManager.Panel.SelectedTargetItem = thisItem;
        }
    }


    /// <summary>
    /// Prop row fastlist item for loaded props/trees for scaling.
    /// </summary>
    public class UILoadedScalingPropRow : UILoadedPropRow
    {
        // Layout constants.
        public const float CreatorX = 260f;

        // Creator name label.
        private UILabel creatorLabel;

        /// <summary>
        /// Called when this item is selected.
        /// </summary>
        public override void UpdateSelection()
        {
            // Update currently selected loaded prefab.
            BOBScalePanel.Panel.SelectedLoadedPrefab = thisPrefab;
        }

        /// <summary>
        /// Generates and displays a list row.
        /// </summary>
        /// <param name="data">Object to list</param>
        /// <param name="isRowOdd">If the row is an odd-numbered row (for background banding)</param>
        public override void Display(object data, bool isRowOdd)
        {
            base.Display(data, isRowOdd);

            // Add creator name label if it doesn't already exist.
            if (creatorLabel == null)
            {
                creatorLabel = AddUIComponent<UILabel>();
                creatorLabel.width = this.width - 10f;
                creatorLabel.textScale = TextScale;
                creatorLabel.relativePosition = new Vector2(CreatorX, PaddingY);
            }

            // Set creator name.
            creatorLabel.text = thisListItem?.creatorName ?? string.Empty;
        }

        /// <summary>
        /// Called when dimensions are changed, including as part of initial setup (required to set correct relative position of label).
        /// </summary>
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            if (creatorLabel != null)
            {
                creatorLabel.relativePosition = new Vector2(CreatorX, PaddingY);
            }
        }
    }


    /// <summary>
    /// Prop row fastlist item for loaded props/trees for random setup.
    /// </summary>
    public class UILoadedRandomPropRow : UILoadedPropRow
    {
        /// <summary>
        /// Called when this item is selected.
        /// </summary>
        public override void UpdateSelection()
        {
            // Update currently selected loaded prefab.
            BOBRandomPanel.Panel.SelectedLoadedPrefab = thisPrefab;
        }
    }


    /// <summary>
    /// Prop row fastlist item for random prop/tree variations.
    /// </summary>
    public class UIRandomComponentRow : UIPropRow
    {
        private BOBRandomPrefab.Variation thisVariant;
        private UILabel probLabel;
        private UISprite lockSprite;

        /// <summary>
        /// Called when this item is selected.
        /// </summary>
        public override void UpdateSelection()
        {
            // Update currently selected variation prefab.
            BOBRandomPanel.Panel.SelectedVariation = thisVariant;
        }


        /// <summary>
        /// Generates and displays a list row.
        /// </summary>
        /// <param name="data">Object to list</param>
        /// <param name="isRowOdd">If the row is an odd-numbered row (for background banding)</param>
        public override void Display(object data, bool isRowOdd)
        {
            thisVariant = data as BOBRandomPrefab.Variation;

            // Perform initial setup for new rows.
            if (nameLabel == null)
            {
                isVisible = true;
                canFocus = true;
                isInteractive = true;
                width = parent.width;
                height = RowHeight;

                // Add object name label.
                nameLabel = AddUIComponent<UILabel>();
                nameLabel.width = this.width - 10f;
                nameLabel.textScale = TextScale;

                // Add index text label.
                indexLabel = AddUIComponent<UILabel>();
                indexLabel.width = IndexWidth;
                indexLabel.textScale = TextScale;
                indexLabel.relativePosition = new Vector2(IndexLabelX, PaddingY);
            }

            // Set label position, text and color.
            nameLabel.relativePosition = new Vector2(labelX, PaddingY);
            nameLabel.text = thisVariant?.DisplayName ?? "null";
            nameLabel.textColor = thisVariant.Prefab == null ? Color.gray : Color.white;

            // Set initial background as deselected state.
            Deselect(isRowOdd);

            // Probability locked sprite.
            if (lockSprite == null)
            {
                lockSprite = AddUIComponent<UISprite>();

                lockSprite.size = new Vector2(17f, 17f);
                lockSprite.relativePosition = new Vector2(width - 20f, 3f);
                lockSprite.atlas = UITextures.LoadQuadSpriteAtlas("BOB-Padlock");
                SetLockSprite();

                lockSprite.eventClicked += (control, clickEvent) =>
                {
                    if (thisVariant != null)
                    {
                        thisVariant.ProbLocked = !thisVariant.ProbLocked;
                        SetLockSprite();
                    }
                };
            }

            // Probability label.
            if (probLabel == null)
            {
                probLabel = AddUIComponent<UILabel>();
            }
            probLabel.text = (thisVariant?.Probability.ToString() ?? "0") + "%";
            probLabel.relativePosition = new Vector2(width - 20f - 5f - probLabel.width, 3f);
        }


        /// <summary>
        /// Sets the state of the probability locked sprite.
        /// </summary>
        private void SetLockSprite()
        {
            if (thisVariant != null && lockSprite != null)
            {
                if (thisVariant.ProbLocked)
                {
                    lockSprite.spriteName = "disabled";
                }
                else
                {
                    lockSprite.spriteName = "pressed";
                }
            }
        }
    }
}
