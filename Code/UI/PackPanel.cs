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
	internal class BOBPackPanel : UIPanel
    {
        // Layout constants.
        internal const float RowHeight = 30f;
        internal const float ListWidth = 710f;
		private const float Margin = 5f;
		private const float PanelWidth = ListWidth + (Margin * 2f);
		private const float ListHeight = 300f;
		private const float TitleBarHeight = 40f;
        private const float FooterX = TitleBarHeight + ListHeight + Margin;
        private const float FooterHeight = 30f;
        private const float PanelHeight = FooterX + FooterHeight + Margin;


        // Panel components.
        private readonly UIButton applyButton, revertButton;
        private readonly UIFastList packSelection;

        // Reference variables.
        private string selectedPack;


        /// <summary>
        /// Handles changes to currently selected pack, updating panel button states accordingly.
        /// </summary>
        internal string SelectedPack
        {
            get => selectedPack;

            set
            {
                selectedPack = value;

                // Update button states to reflect current selection.
                UpdateButtonStates();
            }
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        internal BOBPackPanel()
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

            // Pack list panel.
            UIPanel packListPanel = AddUIComponent<UIPanel>();
            packListPanel.width = ListWidth;
            packListPanel.height = ListHeight;
            packListPanel.relativePosition = new Vector3(Margin, TitleBarHeight);

            // Pack selection list.
            packSelection = UIFastList.Create<UIPackRow>(packListPanel);
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
            applyButton = UIControls.AddButton(this, Margin, FooterX, Translations.Translate("BOB_PCK_APP"));
            revertButton = UIControls.AddButton(this, (ListWidth / 2) + (Margin * 2), FooterX, Translations.Translate("BOB_PCK_RVT"));
            applyButton.eventClicked += (control, clickEvent) => SetPackStatus(true);
            revertButton.eventClicked += (control, clickEvent) => SetPackStatus(false);

            // Buttons are disabled to start with.
            applyButton.Disable();
            revertButton.Disable();

            // Populate list.
            packSelection.rowsData = PackReplacement.instance.GetPackFastList();

            // Focus.
            BringToFront();
            Focus();
        }


        /// <summary>
        /// Sets the pack status of the currently selected pack.
        /// </summary>
        /// <param name="status">Status to set</param>
        private void SetPackStatus(bool status)
        {
            // Set pack status,
            PackReplacement.instance.SetPackStatus(selectedPack, status);
            ConfigurationUtils.SaveConfig();
            packSelection.Refresh();

            // Update buttons.
            UpdateButtonStates();

            // Update parent panel, if it's open.
            if (InfoPanelManager.Panel != null)
            {
                InfoPanelManager.Panel.UpdateTargetList();
            }
        }



        /// <summary>
        /// Updating panel button states according ti the status of the current selection.
        /// </summary>
        private void UpdateButtonStates()
        {
            // No pack selection.
            if (selectedPack == null)
            {
                // Disable all buttons and cancel tooltips.
                applyButton.tooltip = "";
                revertButton.tooltip = "";
                applyButton.Disable();
                revertButton.Disable();

                return;
            }    

            // Check status of current pack.
            if (PackReplacement.instance.GetPackStatus(selectedPack))
            {
                // Pack is currently applied - enable revert button and disable apply button.
                revertButton.tooltip = Translations.Translate("BOB_PCK_RVT_A");
                revertButton.Enable();
                applyButton.tooltip = Translations.Translate("BOB_PCK_APP_I");
                applyButton.Disable();
            }
            else
            {
                // Pack is not currently applied - check for pack conflicts.
                if (PackReplacement.instance.Conflicts(selectedPack))
                {
                    // Conflict detected - disable apply button and add explanatory tooltip.
                    applyButton.tooltip = Translations.Translate("BOB_PCK_APP_C");
                    applyButton.Disable();
                }
                else
                {
                    // No conflicts - enable apply button and update tooltip.
                    applyButton.tooltip = Translations.Translate("BOB_PCK_APP_A");
                    applyButton.Enable();
                }

                // Disable revert button and update tooltip.
                revertButton.tooltip = Translations.Translate("BOB_PCK_RVT_I");
                revertButton.Disable();
            }
        }
    }


    /// <summary>
    /// An individual row in the list of sign packs.
    /// </summary>
    public class UIPackRow : UIPanel, IUIFastListRow
    {
        // Layout constants.
        private const float TextX = 60f;

        // Panel components.
        private UIPanel panelBackground;
        private UILabel packLabel;
        private UISprite statusSprite, notLoadedSprite;
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
                packLabel.relativePosition = new Vector3(TextX, 6f);
            }
        }


        /// <summary>
        /// Mouse click event handler - updates the selection to what was clicked.
        /// </summary>
        /// <param name="p">Mouse event parameter</param>
        protected override void OnClick(UIMouseEventParameter p)
        {
            base.OnClick(p);
            PackPanelManager.Panel.SelectedPack = thisPack;
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
                packLabel.relativePosition = new Vector3(TextX, 6f);
            }

            if (statusSprite == null)
            {
                statusSprite = AddUIComponent<UISprite>();
                statusSprite.size = new Vector2(20f, 20f);
                statusSprite.relativePosition = new Vector3(5f, 5f);
            }

            if (notLoadedSprite == null)
            {
                notLoadedSprite = AddUIComponent<UISprite>();
                notLoadedSprite.size = new Vector2(20f, 20f);
                notLoadedSprite.relativePosition = new Vector3(30f, 5f);
                notLoadedSprite.spriteName = "NotificationIconNotHappy";
            }

            // Set selected pack.
            thisPack = data as string;
            packLabel.text = thisPack;

            // Set sprite status.
            bool packStatus = PackReplacement.instance.GetPackStatus(thisPack);
            bool notAllLoaded = PackReplacement.instance.PackNotAllLoaded(thisPack);
            statusSprite.spriteName = packStatus ? "AchievementCheckedTrue" : "AchievementCheckedFalse";
            statusSprite.tooltip = packStatus ? Translations.Translate("BOB_PCK_APP_I") : Translations.Translate("BOB_PCK_RVT_I");
            if (notAllLoaded)
            {
                notLoadedSprite.Show();
                notLoadedSprite.tooltip = Translations.Translate("BOB_PCK_NAL");
            }
            else
            {
                notLoadedSprite.Hide();
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