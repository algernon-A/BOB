using System;
using UnityEngine;
using ColossalFramework.UI;


namespace BOB
{

	/// <summary>
	/// Static class to manage the BOB pack panel.
	/// </summary>
	internal static class PackPanelManager
	{
		// Instance references.
		private static GameObject uiGameObject;
		private static BOBPackPanel _panel;
		internal static BOBPackPanel Panel => _panel;

		// Recent state.
		internal static float lastX, lastY;


		/// <summary>
		/// Creates the panel object in-game and displays it.
		/// </summary>
		internal static void Create()
		{
			try
			{
				// If no instance already set, create one.
				if (uiGameObject == null)
				{
						uiGameObject = new GameObject("BOBPackPanel");
						uiGameObject.transform.parent = UIView.GetAView().transform;

						_panel = uiGameObject.AddComponent<BOBPackPanel>();
				}
			}
			catch (Exception e)
			{
				Logging.LogException(e, "exception creating PackPanel");
			}
		}


		/// <summary>
		/// Closes the panel by destroying the object (removing any ongoing UI overhead).
		/// </summary>
		internal static void Close()
		{
			// Store previous position.
			lastX = _panel.relativePosition.x;
			lastY = _panel.relativePosition.y;

			// Destroy game objects.
			GameObject.Destroy(_panel);
			GameObject.Destroy(uiGameObject);

			// Let the garbage collector do its work (and also let us know that we've closed the object).
			_panel = null;
			uiGameObject = null;
		}
	}


	/// <summary>
	/// Panel for replacement pack selection and application.
	/// </summary>
	public class BOBPackPanel : UIPanel
    {
        // Layout constants.
        public const float RowHeight = 30f;
        public const float ListWidth = 610f;
		private const float Margin = 5f;
		private const float PanelWidth = ListWidth + (Margin * 2f);
		private const float ListHeight = 300f;
		private const float TitleBarHeight = 40f;
        private const float FooterX = TitleBarHeight + ListHeight + Margin;
        private const float FooterHeight = 30f;
        private const float PanelHeight = FooterX + FooterHeight + Margin;


        // Reference variables.
        internal string selectedPack;


        /// <summary>
        /// Constructor.
        /// </summary>
        public BOBPackPanel()
        {
            // Basic behaviour.
            autoLayout = false;
            canFocus = true;
            isInteractive = true;

            // Appearance.
            backgroundSprite = "MenuPanel2";
            opacity = 1f;

            // Size.
            width = PanelWidth;
            height = PanelHeight;

            // Drag bar.
            UIDragHandle dragHandle = AddUIComponent<UIDragHandle>();
            dragHandle.width = this.width - 50f;
            dragHandle.height = this.height;
            dragHandle.relativePosition = Vector3.zero;
            dragHandle.target = this;

            // Title label.
            UILabel titleLabel = AddUIComponent<UILabel>();
            titleLabel.relativePosition = new Vector2(50f, 13f);
            titleLabel.text = Translations.Translate("BOB_NAM");

            // Close button.
            UIButton closeButton = AddUIComponent<UIButton>();
            closeButton.relativePosition = new Vector2(width - 35, 2);
            closeButton.normalBgSprite = "buttonclose";
            closeButton.hoveredBgSprite = "buttonclosehover";
            closeButton.pressedBgSprite = "buttonclosepressed";
            closeButton.eventClick += (component, clickEvent) =>
            {
                PackPanelManager.Close();
            };

            // Speed sign list panel.
            UIPanel packListPanel = AddUIComponent<UIPanel>();
            packListPanel.width = ListWidth;
            packListPanel.height = ListHeight;
            packListPanel.relativePosition = new Vector3(Margin, TitleBarHeight);

            // Speed sign pack selection list.
            UIFastList packSelection = UIFastList.Create<UIPackRow>(packListPanel);
            packSelection.backgroundSprite = "UnlockingPanel";
            packSelection.width = packListPanel.width;
            packSelection.height = packListPanel.height;
            packSelection.canSelect = true;
            packSelection.rowHeight = RowHeight;
            packSelection.autoHideScrollbar = true;
            packSelection.relativePosition = Vector3.zero;
            packSelection.rowsData = new FastList<object>();
            packSelection.selectedIndex = -1;

            // Apply and revert button.
            UIButton applyButton = UIControls.AddButton(this, Margin, FooterX, Translations.Translate("BOB_PCK_APP"));
            UIButton revertButton = UIControls.AddButton(this, (ListWidth / 2) + (Margin * 2), FooterX, Translations.Translate("BOB_PCK_RVT"));

            applyButton.eventClicked += (control, clickEvent) =>
            {
                PackReplacement.SetPackStatus(selectedPack, true);
                ConfigurationUtils.SaveConfig();
                packSelection.Refresh();
            };
            revertButton.eventClicked += (control, clickEvent) =>
            {
                PackReplacement.SetPackStatus(selectedPack, false);
                ConfigurationUtils.SaveConfig();
                packSelection.Refresh();
            };
            // Populate list.
            packSelection.rowsData = PackReplacement.GetPackFastList();

            // Focus.
            BringToFront();
            Focus();
        }
    }


    /// <summary>
    /// An individual row in the list of sign packs.
    /// </summary>
    public class UIPackRow : UIPanel, IUIFastListRow
    {
        // Panel components.
        private UIPanel panelBackground;
        private UILabel packLabel;
        private UISprite statusSprite;
        private string thisPack;


        // Background for each list item.
        public UIPanel Background
        {
            get
            {
                if (panelBackground == null)
                {
                    panelBackground = AddUIComponent<UIPanel>();
                    panelBackground.width = width;
                    panelBackground.height = BOBPackPanel.RowHeight;
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

            if (packLabel != null)
            {
                Background.width = width;
                packLabel.relativePosition = new Vector3(30f, 6f);
            }
        }


        /// <summary>
        /// Mouse click event handler - updates the selection to what was clicked.
        /// </summary>
        /// <param name="p">Mouse event parameter</param>
        protected override void OnClick(UIMouseEventParameter p)
        {
            base.OnClick(p);
            PackPanelManager.Panel.selectedPack = thisPack;
        }


        /// <summary>
        /// Generates and displays a building row.
        /// </summary>
        /// <param name="data">Object to list</param>
        /// <param name="isRowOdd">If the row is an odd-numbered row (for background banding)</param>
        public void Display(object data, bool isRowOdd)
        {
            // Perform initial setup for new rows.
            if (packLabel == null)
            {
                isVisible = true;
                canFocus = true;
                isInteractive = true;
                width = parent.width;
                height = BOBPackPanel.RowHeight;

                packLabel = AddUIComponent<UILabel>();
                packLabel.width = BOBPackPanel.ListWidth;
                packLabel.relativePosition = new Vector3(30f, 6f);
            }

            if (statusSprite == null)
            {
                statusSprite = AddUIComponent<UISprite>();
                statusSprite.size = new Vector2(20, 20);
                statusSprite.relativePosition = new Vector3(5f, 5f);
            }

            // Set selected pack.
            thisPack = data as string;
            packLabel.text = thisPack;
            statusSprite.spriteName = PackReplacement.GetPackStatus(thisPack) ? "AchievementCheckedTrue" : "AchievementCheckedFalse";

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