// <copyright file="ConfigurationsPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using AlgernonCommons;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Options panel for setting basic mod options.
    /// </summary>
    internal class ConfigurationsPanel
    {
        // Layout constants - general.
        private const float Margin = 5f;

        // Layout constants - Y values.
        private const float RowHeight = 30f;
        private const float ListHeight = 300f;
        private const float TitleBarHeight = 40f;
        private const float ToolBarHeight = 30f;
        private const float ListY = TitleBarHeight;
        private const float ToolBarY = ListY + ListHeight + Margin;
        private const float FooterY = ToolBarY + ToolBarHeight + Margin;

        // Layout constants - X values.
        private const float ListWidth = 400f;
        private const float ControlPanelX = ListWidth + (Margin * 2f);

        // Instance reference.
        private static ConfigurationsPanel s_instance;

        // Current configuration file name.
        private static string s_selectedConfig;

        // Panel components.
        private readonly UIFastList _configList;
        private readonly UICheckBox _customCheck;
        private readonly UITextField _fileNameField;
        private readonly UIButton _activeCopyButton;
        private readonly UIButton _selectedCopyButton;
        private readonly UIButton _newCleanButton;
        private readonly UIButton _deleteButton;

        // Status flag.
        private readonly bool _inGame;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationsPanel"/> class.
        /// </summary>
        /// <param name="tabStrip">Tab strip to add to.</param>
        /// <param name="tabIndex">Index number of tab.</param>
        internal ConfigurationsPanel(UITabstrip tabStrip, int tabIndex)
        {
            // Set reference.
            s_instance = this;

            // Determine if we're in-game or not; use status of replacer managers to determine.
            _inGame = GroupedBuildingReplacement.Instance != null && GroupedNetworkReplacement.Instance != null;

            // Add tab and helper.
            UIPanel panel = UITabstrips.AddTextTab(tabStrip, Translations.Translate("BOB_OPT_CFG"), tabIndex, out _);
            UIHelper helper = new UIHelper(panel);
            panel.autoLayout = false;

            // Config list panel.
            UIPanel configListPanel = panel.AddUIComponent<UIPanel>();
            configListPanel.width = ListWidth;
            configListPanel.height = ListHeight;
            configListPanel.relativePosition = new Vector2(Margin, ListY);

            // Config selection list.
            _configList = UIFastList.Create<UIConfigRow>(configListPanel);
            _configList.backgroundSprite = "UnlockingPanel";
            _configList.width = configListPanel.width;
            _configList.height = configListPanel.height;
            _configList.canSelect = true;
            _configList.rowHeight = RowHeight;
            _configList.autoHideScrollbar = true;
            _configList.relativePosition = Vector2.zero;
            _configList.rowsData = new FastList<object>();

            // File name textfield.
            UILabel fileTextLabel = UILabels.AddLabel(panel, ControlPanelX, ListY, "New configuration name:");
            _fileNameField = UITextFields.AddTextField(panel, ControlPanelX, ListY + fileTextLabel.height);
            _fileNameField.eventTextChanged += (control, text) => UpdateButtonStates();

            // Buttons.
            _activeCopyButton = UIButtons.AddButton(panel, ControlPanelX, ListY + 70f, Translations.Translate("BOB_CFG_SAC"), 300f, scale: 0.8f);
            _activeCopyButton.eventClicked += NewCurrent;
            _selectedCopyButton = UIButtons.AddButton(panel, ControlPanelX, ListY + 105f, Translations.Translate("BOB_CFG_SSC"), 300f, scale: 0.8f);
            _selectedCopyButton.eventClicked += CopySelected;
            _newCleanButton = UIButtons.AddButton(panel, ControlPanelX, ListY + 140f, Translations.Translate("BOB_CFG_SEC"), 300f, scale: 0.8f);
            _newCleanButton.eventClicked += NewClean;
            _deleteButton = UIButtons.AddButton(panel, ControlPanelX, ListY + 210f, Translations.Translate("BOB_CFG_DEL"), 300f, scale: 0.8f);
            _deleteButton.eventClicked += Delete;

            // Ingame buttons - 'use custom' check and apply and nuke buttons.
            if (_inGame)
            {
                // Use custom check box.
                _customCheck = UICheckBoxes.AddLabelledCheckBox(panel, Margin, ToolBarY, Translations.Translate("BOB_CFG_UCS"));
                _customCheck.eventCheckChanged += (control, isChecked) =>
                {
                    // If we've got a valid selection, set the current config name to this.
                    if (isChecked && !string.IsNullOrEmpty(s_selectedConfig))
                    {
                        ConfigurationUtils.CurrentSavedConfigName = s_selectedConfig;
                    }
                };

                // Apply button.
                UIButton applyButton = UIButtons.AddButton(panel, Margin, FooterY, Translations.Translate("BOB_CFG_LAA"), 400f, scale: 0.8f);
                applyButton.eventClicked += Apply;

                // Use global configuration button.
                UIButton globalButton = UIButtons.AddButton(panel, Margin, FooterY + 50f, Translations.Translate("BOB_CFG_LGL"), 400f, scale: 0.8f);
                globalButton.eventClicked += UseGlobal;

                // Clean up config button.
                UIButton cleanUpButton = UIButtons.AddButton(panel, Margin + 50f, FooterY + 150f, Translations.Translate("BOB_CFG_CLE"), 300f);
                cleanUpButton.tooltip = Translations.Translate("BOB_CFG_CLE_TIP");
                cleanUpButton.eventClicked += (control, clickEvent) => ConfigurationUtils.Cleanup();

                // Nuke all settings button.
                UIButton nukeButton = UIButtons.AddButton(panel, Margin + 50f, FooterY + 200f, Translations.Translate("BOB_NUKE"), 300f);
                nukeButton.eventClicked += (control, clickEvent) =>
                {
                    // Revert all-building and building settings.
                    ReplacementUtils.NukeSettings();
                };

                // Clean map data button.
                UIButton cleanSaveButton = UIButtons.AddButton(panel, Margin + 50f, FooterY + 280f, Translations.Translate("BOB_CFG_CMD"), 300f, scale: 0.8f, tooltip: Translations.Translate("BOB_CFG_CMD_TIP"));
                cleanSaveButton.eventClicked += (control, clickEvent) =>
                {
                    // Clean all map data.
                    MapTreeReplacement.Instance.Replacements.Clear();
                    MapPropReplacement.Instance.Replacements.Clear();
                };
            }

            // Populate selection list and set initial button states.
            RefreshList();

            // Select current pack if we've got one.
            if (_customCheck != null && _customCheck.isChecked)
            {
                // Try to select current config name.
                s_selectedConfig = _configList.FindItem(ConfigurationUtils.CurrentSavedConfigName);

                // Did we find it?
                if (s_selectedConfig == null)
                {
                    // Not found; uncheck the use custom check.
                    _customCheck.isChecked = false;
                }
            }

            // Set initial button states.
            UpdateButtonStates();
        }

        /// <summary>
        /// Sets the selected configuration file.
        /// </summary>
        internal static string SelectedConfig
        {
            set
            {
                if (value != s_selectedConfig)
                {
                    s_selectedConfig = value;
                    s_instance?.UpdateButtonStates();
                }
            }
        }

        /// <summary>
        /// Repopulates the config list and clears current selection.
        /// </summary>
        private void RefreshList()
        {
            _configList.selectedIndex = -1;
            _configList.rowsData = ConfigurationUtils.GetConfigFastList();
            s_selectedConfig = null;

            // Update button states.
            UpdateButtonStates();
        }

        /// <summary>
        /// 'Copy selected configuration' button event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="p">Mouse event parameter.</param>
        private void CopySelected(UIComponent c, UIMouseEventParameter p)
        {
            // Ensure valid current selection and new filename before proceeding.
            if (!string.IsNullOrEmpty(s_selectedConfig) && !string.IsNullOrEmpty(_fileNameField.text))
            {
                // Copy file, capturing any error message.
                string message = ConfigurationUtils.CopyCurrent(s_selectedConfig, _fileNameField.text);
                if (message == null)
                {
                    // Successful copy - clear text field, clear selection, and refresh config list.
                    RefreshList();
                    _fileNameField.text = string.Empty;
                }
                else
                {
                    Logging.Error(message);
                }
            }
        }

        /// <summary>
        /// 'Create new configuration' button event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="p">Mouse event parameter.</param>
        private void NewClean(UIComponent c, UIMouseEventParameter p)
        {
            // Ensure valid current selection and new filename before proceeding.
            if (!string.IsNullOrEmpty(_fileNameField.text))
            {
                ConfigurationUtils.BlankConfig(_fileNameField.text);
                RefreshList();
            }
        }

        /// <summary>
        /// 'Save current configuration' button event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="p">Mouse event parameter.</param>
        private void NewCurrent(UIComponent c, UIMouseEventParameter p)
        {
            // Ensure valid current selection and new filename before proceeding.
            if (!string.IsNullOrEmpty(_fileNameField.text))
            {
                ConfigurationUtils.SaveConfig(_fileNameField.text);
                RefreshList();
            }
        }

        /// <summary>
        /// 'Delete selected configuration' button event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="p">Mouse event parameter.</param>
        private void Delete(UIComponent c, UIMouseEventParameter p)
        {
            // Ensure valid selection before deletion.
            if (!string.IsNullOrEmpty(s_selectedConfig))
            {
                ConfigurationUtils.DeleteConfig(s_selectedConfig);
                RefreshList();
            }
        }

        /// <summary>
        /// 'Save and apply' button event handler.  Should only be called ingame.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="p">Mouse event parameter.</param>
        private void Apply(UIComponent c, UIMouseEventParameter p)
        {
            // Are we in-game, and do we have a valid current selection?
            if (_inGame && !string.IsNullOrEmpty(s_selectedConfig))
            {
                // Yes - set current configuration file.
                ConfigurationUtils.CurrentSavedConfigName = s_selectedConfig;

                Logging.KeyMessage("current configuration set to ", ConfigurationUtils.CurrentSavedConfigName);

                // Load config.
                LoadConfig();
            }
        }

        /// <summary>
        /// 'Load global settings' button event handler.  Should only be called ingame.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="p">Mouse event parameter.</param>
        private void UseGlobal(UIComponent c, UIMouseEventParameter p)
        {
            // Are we in-game, and do we have a valid current selection?
            if (_inGame)
            {
                // Yes - clear currently selected configuration file.
                ConfigurationUtils.CurrentSavedConfigName = null;

                Logging.KeyMessage("reloading global configuration");

                // Load config.
                LoadConfig();
            }
        }

        /// <summary>
        /// (Re-)loads the current config (per ConfigurationUtils.CurrentSavedConfigName).
        /// </summary>
        private void LoadConfig()
        {
            // Clear current replacements.
            ReplacementUtils.NukeSettings();

            // Load config file.
            ConfigurationUtils.LoadConfig();

            Logging.KeyMessage("reloaded global configuration");

            // Update button states accordingly.
            UpdateButtonStates();

            // Update dirty renders.
            BuildingData.Update();
            NetData.Update();
        }

        /// <summary>
        /// Updates the control button states based on current panel state.
        /// </summary>
        private void UpdateButtonStates()
        {
            // See if there's a valid filename string.
            bool validFile = !string.IsNullOrEmpty(_fileNameField.text);
            bool validSelection = !string.IsNullOrEmpty(s_selectedConfig);

            // Need a valid filename to enable creating new files, and 'current selction' is only valid when in-game.
            _activeCopyButton.isEnabled = validFile & Loading.IsLoaded;
            _newCleanButton.isEnabled = validFile;

            // Selected copy button requires both a valid filename and valid current selection.
            _selectedCopyButton.isEnabled = validFile && validSelection;

            // Delete button requires a valid current selection.
            _deleteButton.isEnabled = validSelection;

            // Set 'use selected config as default' check.
            if (_customCheck != null)
            {
                _customCheck.isChecked = validSelection && s_selectedConfig == ConfigurationUtils.CurrentSavedConfigName;
            }
        }

        /// <summary>
        /// An individual row in the list of config files.
        /// </summary>
        private class UIConfigRow : UIBasicRow
        {
            // Panel components.
            private string _thisConfigName;

            // Layout constants.
            protected override float TextX => 10f;

            /// <summary>
            /// Generates and displays a pack row.
            /// </summary>
            /// <param name="data">Object to list.</param>
            /// <param name="isRowOdd">If the row is an odd-numbered row (for background banding).</param>
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
                    rowLabel.width = ConfigurationsPanel.ListWidth;
                    rowLabel.relativePosition = new Vector2(TextX, 6f);
                }

                // Set selected config.
                _thisConfigName = data as string;
                rowLabel.text = _thisConfigName ?? "Null";

                // Set initial background as deselected state.
                Deselect(isRowOdd);
            }

            /// <summary>
            /// Mouse click event handler - updates the selection to what was clicked.
            /// </summary>
            /// <param name="p">Mouse event parameter.</param>
            protected override void OnClick(UIMouseEventParameter p)
            {
                base.OnClick(p);

                SelectedConfig = _thisConfigName;
            }
        }
    }
}