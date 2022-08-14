// <copyright file="BOBPackPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System;
    using AlgernonCommons;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Panel for replacement pack selection and application.
    /// </summary>
    internal sealed class BOBPackPanel : UIPanel
    {
        /// <summary>
        /// List row height.
        /// </summary>
        internal const float RowHeight = 30f;

        /// <summary>
        /// Listwidth.
        /// </summary>
        internal const float ListWidth = 710f;

        // Layout constants.
        private const float Margin = 5f;
        private const float PanelWidth = ListWidth + (Margin * 2f);
        private const float ListHeight = 300f;
        private const float TitleBarHeight = 40f;
        private const float FooterY = TitleBarHeight + ListHeight + Margin;
        private const float FooterHeight = 30f;
        private const float PanelHeight = FooterY + FooterHeight + Margin;

        // Instance references.
        private static GameObject s_gameObject;
        private static BOBPackPanel s_panel;

        // Panel components.
        private readonly UIButton _applyButton;
        private readonly UIButton _revertButton;
        private readonly UIList _packSelectionList;

        // Reference variables.
        private string selectedPack;

        /// <summary>
        /// Initializes a new instance of the <see cref="BOBPackPanel"/> class.
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
            closeButton.eventClick += (c, p) =>
            {
                Close();
            };

            // Pack list panel.
            UIPanel packListPanel = AddUIComponent<UIPanel>();
            packListPanel.width = ListWidth;
            packListPanel.height = ListHeight;
            packListPanel.relativePosition = new Vector3(Margin, TitleBarHeight);

            // Pack selection list.
            _packSelectionList = UIList.AddUIList<PackRow>(packListPanel, 0f, 0f, ListWidth, ListHeight);
            _packSelectionList.EventSelectionChanged += (c, data) => SelectedPack = data as string;

            // Apply and revert button.
            _applyButton = UIButtons.AddButton(this, Margin, FooterY, Translations.Translate("BOB_PCK_APP"));
            _revertButton = UIButtons.AddButton(this, (ListWidth / 2) + (Margin * 2), FooterY, Translations.Translate("BOB_PCK_RVT"));
            _applyButton.eventClicked += (c, p) => SetPackStatus(true);
            _revertButton.eventClicked += (c, p) => SetPackStatus(false);

            // Buttons are disabled to start with.
            _applyButton.Disable();
            _revertButton.Disable();

            // Populate list.
            _packSelectionList.Data = NetworkPackReplacement.Instance.GetPackFastList();

            // Focus.
            BringToFront();
            Focus();
        }

        /// <summary>
        /// Sets the currently selected pack, updating panel button states accordingly.
        /// </summary>
        internal string SelectedPack
        {
            set
            {
                selectedPack = value;

                // Update button states to reflect current selection.
                UpdateButtonStates();
            }
        }

        /// <summary>
        /// Creates the panel object in-game and displays it.
        /// </summary>
        internal static void Create()
        {
            try
            {
                // If no instance already set, create one.
                if (s_gameObject == null)
                {
                    s_gameObject = new GameObject("BOBPackPanel");
                    s_gameObject.transform.parent = UIView.GetAView().transform;

                    s_panel = s_gameObject.AddComponent<BOBPackPanel>();
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
            // Destroy game objects.
            GameObject.Destroy(s_panel);
            GameObject.Destroy(s_gameObject);

            // Let the garbage collector do its work (and also let us know that we've closed the object).
            s_panel = null;
            s_gameObject = null;
        }

        /// <summary>
        /// Sets the pack status of the currently selected pack.
        /// </summary>
        /// <param name="status">Status to set.</param>
        private void SetPackStatus(bool status)
        {
            // Set pack status,
            NetworkPackReplacement.Instance.SetPackStatus(selectedPack, status);
            ConfigurationUtils.SaveConfig();
            _packSelectionList.Refresh();

            // Update buttons.
            UpdateButtonStates();

            // Update parent panel, if it's open.
            if (BOBPanelManager.Panel != null)
            {
                BOBPanelManager.Panel.UpdateTargetList();
            }

            // Update renders.
            NetData.Update();
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
                _applyButton.tooltip = string.Empty;
                _revertButton.tooltip = string.Empty;
                _applyButton.Disable();
                _revertButton.Disable();

                return;
            }

            // Check status of current pack.
            if (NetworkPackReplacement.Instance.GetPackStatus(selectedPack))
            {
                // Pack is currently applied - enable revert button and disable apply button.
                _revertButton.tooltip = Translations.Translate("BOB_PCK_RVT_A");
                _revertButton.Enable();
                _applyButton.tooltip = Translations.Translate("BOB_PCK_APP_I");
                _applyButton.Disable();
            }
            else
            {
                // Pack is not currently applied - check for pack conflicts.
                if (NetworkPackReplacement.Instance.Conflicts(selectedPack))
                {
                    // Conflict detected - disable apply button and add explanatory tooltip.
                    _applyButton.tooltip = Translations.Translate("BOB_PCK_APP_C");
                    _applyButton.Disable();
                }
                else
                {
                    // No conflicts - enable apply button and update tooltip.
                    _applyButton.tooltip = Translations.Translate("BOB_PCK_APP_A");
                    _applyButton.Enable();
                }

                // Disable revert button and update tooltip.
                _revertButton.tooltip = Translations.Translate("BOB_PCK_RVT_I");
                _revertButton.Disable();
            }
        }

        /// <summary>
        /// An individual row in the list of replacement packs.
        /// </summary>
        private class PackRow : UIListRow
        {
            // Layout constants.
            private const float SpriteSize = 20f;

            // Panel components.
            private UILabel _nameLabel;
            private UISprite _statusSprite;
            private UISprite _notLoadedSprite;

            /// <summary>
            /// Gets the height for this row.
            /// </summary>
            public override float RowHeight => BOBPackPanel.RowHeight;

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
                    _nameLabel = AddLabel(Margin, parent.width - Margin - Margin);

                    _statusSprite = AddUIComponent<UISprite>();
                    _statusSprite.size = new Vector2(SpriteSize, SpriteSize);
                    _statusSprite.relativePosition = new Vector3(Margin, Margin);

                    _notLoadedSprite = AddUIComponent<UISprite>();
                    _notLoadedSprite.size = new Vector2(SpriteSize, SpriteSize);
                    _notLoadedSprite.relativePosition = new Vector3(SpriteSize + Margin + Margin, Margin);
                    _notLoadedSprite.spriteName = "NotificationIconNotHappy";
                }

                // Set selected pack.
                if (data is string thisPack)
                {
                    _nameLabel.text = thisPack;

                    // Set sprite status.
                    bool packStatus = NetworkPackReplacement.Instance.GetPackStatus(thisPack);
                    bool notAllLoaded = NetworkPackReplacement.Instance.PackNotAllLoaded(thisPack);
                    _statusSprite.spriteName = packStatus ? "AchievementCheckedTrue" : "AchievementCheckedFalse";
                    _statusSprite.tooltip = packStatus ? Translations.Translate("BOB_PCK_APP_I") : Translations.Translate("BOB_PCK_RVT_I");
                    if (notAllLoaded)
                    {
                        _notLoadedSprite.Show();
                        _notLoadedSprite.tooltip = Translations.Translate("BOB_PCK_NAL");
                    }
                    else
                    {
                        _notLoadedSprite.Hide();
                    }
                }
                else
                {
                    // Just in case - no valid data.
                    _nameLabel.text = string.Empty;
                    _statusSprite.Hide();
                    _notLoadedSprite.Hide();
                }

                // Set initial background as deselected state.
                Deselect(rowIndex);
            }
        }
    }
}