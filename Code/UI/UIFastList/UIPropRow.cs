using System.Text;
using UnityEngine;
using ColossalFramework.UI;


namespace BOB
{
    /// <summary>
    /// An individual prop row.
    /// </summary>
    public class UIPropRow : UIPanel, UIFastListRow
    {
        // Height of each row.
        private const int rowHeight = 30;

        // Panel components.
        private UIPanel panelBackground;
        private UILabel objectName;

        // ObjectData
		protected PrefabInfo thisPrefab;
		protected PropListItem thisItem;
		protected int index;
		protected float angle;
		protected int probability;
		protected int originalProb;


        // Background for each list item.
        public UIPanel Background
        {
            get
            {
                if (panelBackground == null)
                {
                    panelBackground = AddUIComponent<UIPanel>();
                    panelBackground.width = width;
                    panelBackground.height = rowHeight;
                    panelBackground.relativePosition = Vector2.zero;

                    panelBackground.zOrder = 0;
                }

                return panelBackground;
            }
        }


        /// <summary>
        /// Called when dimensions are changed, including as part of initial setup (required to set correct relative position of label).
        /// </summary>
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            if (objectName != null)
            {
                Background.width = width;
                objectName.relativePosition = new Vector3(10f, 5f);
            }
        }


        /// <summary>
        /// Mouse click event handler - updates the selected building to what was clicked.
        /// </summary>
        /// <param name="p">Mouse event parameter</param>
        protected override void OnClick(UIMouseEventParameter p)
        {
            base.OnClick(p);
            UpdateSelection();
        }


        /// <summary>
        /// Updates current replacement selection when this item is selected.
        /// </summary>
        public virtual void UpdateSelection()
        {
        }


        /// <summary>
        /// Generates and displays a building row.
        /// </summary>
        /// <param name="data">Object to list</param>
        /// <param name="isRowOdd">If the row is an odd-numbered row (for background banding)</param>
        public void Display(object data, bool isRowOdd)
        {
            // Perform initial setup for new rows.
            if (objectName == null)
            {
                isVisible = true;
                canFocus = true;
                isInteractive = true;
                width = parent.width;
                height = rowHeight;

                // Add object name label.
                objectName = AddUIComponent<UILabel>();
                objectName.relativePosition = new Vector3(10f, 5f);
                objectName.width = this.width - 10f;
            }

            // See if our attached data is a raw PropInfo (e.g an available prop item as opposed to a PropListItem replacment record).
            thisPrefab = data as PrefabInfo;
            if (thisPrefab == null)
            {
                // Text to display - StringBuilder due to the amount of manipulation we're doing.
                StringBuilder displayText = new StringBuilder();

                // Not a raw PropInfo, so it should be a PropListItem replacement record.
                // Set local references.
                thisItem = data as PropListItem;
                thisPrefab = thisItem.originalPrefab;
                index = thisItem.index;
                angle = thisItem.angle;
                probability = thisItem.probability;
                originalProb = thisItem.originalProb;

                // See if this is a network prop.
                NetPropListItem thisNetItem = data as NetPropListItem;

                // Display index number if this is an individual reference.
                if (thisItem.index < 0)
                {
                    displayText.Append(thisItem.index);
                    displayText.Append(" ");
                }

                // Prefab display name.
                displayText.Append(UIUtils.GetDisplayName(thisPrefab.name));

                // Show original probability in brackets immediately afterwards, if this isn't a network item.
                if (thisNetItem == null)
                {
                    displayText.Append(" (");
                    displayText.Append(originalProb);
                    displayText.Append("%)");
                }

                // Check to see if there's a currently active replacement (currentPrefab isn't null).
                if (thisItem.currentPrefab != null)
                {
                    // A replacement is currently active - include it in the text.
                    displayText.Append(" (");
                    displayText.Append(Translations.Translate("BOB_ROW_NOW"));
                    displayText.Append(UIUtils.GetDisplayName(thisItem.currentPrefab.name));

                    // Append replacement name and probability to the label, if this isn't a network item.
                    if (thisNetItem == null)
                    {
                        displayText.Append(" ");
                        displayText.Append(probability);
                        displayText.Append("%");
                    }

                    // Append closing bracket.
                    displayText.Append(")");
                }
                // If no current building replacement, check to see if any all- replacement is currently active.
                else if (thisItem.allPrefab != null)
                {
                    // An all- replacement is currently active; append name to the label.
                    displayText.Append(" (");
                    displayText.Append(thisNetItem == null ? Translations.Translate("BOB_PNL_RAB") : Translations.Translate("BOB_PNL_RAN"));
                    displayText.Append(" ");
                    displayText.Append(UIUtils.GetDisplayName(thisItem.allPrefab.name));

                    // Append probability if this is not a network item.
                    if (thisNetItem == null)
                    {
                        displayText.Append(" ");
                        displayText.Append(probability);
                        displayText.Append("%");
                    }

                    // Closing bracket.
                    displayText.Append(")");
                }

                // Set display text.
                objectName.text = displayText.ToString();
            }
            else
            {
                // Attached data is a raw PropInfo; just display its (cleaned-up) name.
                objectName.text = UIUtils.GetDisplayName(thisPrefab.name);
            }


            // Set initial background as deselected state.
            Deselect(isRowOdd);
        }


        /// <summary>
        /// Highlights the selected row.
        /// </summary>
        /// <param name="isRowOdd">If the row is an odd-numbered row (for background banding)</param>
        public void Select(bool isRowOdd)
        {
            Background.backgroundSprite = "ListItemHighlight";
            Background.color = new Color32(255, 255, 255, 255);
        }


        /// <summary>
        /// Unhighlights the (un)selected row.
        /// </summary>
        /// <param name="isRowOdd">If the row is an odd-numbered row (for background banding)</param>
        public void Deselect(bool isRowOdd)
        {
            if (isRowOdd)
            {
                // Lighter background for odd rows.
                Background.backgroundSprite = "UnlockingItemBackground";
                Background.color = new Color32(0, 0, 0, 128);
            }
            else
            {
                // Darker background for even rows.
                Background.backgroundSprite = null;
            }
        }
    }
}

