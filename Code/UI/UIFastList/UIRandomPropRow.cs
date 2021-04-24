using UnityEngine;
using ColossalFramework.UI;


namespace BOB
{
    /// <summary>
    /// An individual row in the list of random props/trees.
    /// </summary>
    public class UIRandomRefabRow : UIBasicRow
    {
        // Layout constants.
        public const float RowHeight = 23f;
        private const float TextScale = 0.8f;

        // Object references.
        PrefabInfo thisPrefab;


        // Background for each list item.
        public override UIPanel Background
        {
            get
            {
                if (panelBackground == null)
                {
                    panelBackground = AddUIComponent<UIPanel>();
                    panelBackground.width = width;
                    panelBackground.height = RowHeight;
                    panelBackground.relativePosition = Vector2.zero;

                    panelBackground.zOrder = 0;
                }

                return panelBackground;
            }
        }


        /// <summary>
        /// Mouse click event handler - updates the selection to what was clicked.
        /// </summary>
        /// <param name="p">Mouse event parameter</param>
        protected override void OnClick(UIMouseEventParameter p)
        {
            base.OnClick(p);
            BOBRandomPanel.Panel.SelectedRandomPrefab = thisPrefab;
        }


        /// <summary>
        /// Generates and displays a pack row.
        /// </summary>
        /// <param name="data">Object to list</param>
        /// <param name="isRowOdd">If the row is an odd-numbered row (for background banding)</param>
        public override void Display(object data, bool isRowOdd)
        {
            // Perform initial setup for new rows.
            if (rowLabel == null)
            {
                isVisible = true;
                canFocus = true;
                isInteractive = true;
                width = parent.width;
                height = BOBPackPanel.RowHeight;

                rowLabel = AddUIComponent<UILabel>();
                rowLabel.width = BOBPackPanel.ListWidth;
                rowLabel.relativePosition = new Vector2(TextX, 6f);
                rowLabel.textScale = TextScale;
            }

            // Set selected prop.
            thisPrefab = data as PrefabInfo;
            rowLabel.text = thisPrefab?.name ?? "null";

            // Set initial background as deselected state.
            Deselect(isRowOdd);
        }
    }
}