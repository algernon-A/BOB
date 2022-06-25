using UnityEngine;
using ColossalFramework.UI;


namespace BOB
{
    /// <summary>
    /// Options panel for setting basic mod options.
    /// </summary>
    internal class ConfigurationsPanel
    {
        // Layout constants - general.
        private const float Margin = 5f;

        // Layout constants - Y values.
        internal const float RowHeight = 30f;
        private const float ListHeight = 300f;
        private const float TitleBarHeight = 40f;
        private const float ToolBarHeight = 30f;
        private const float ListY = TitleBarHeight;
        private const float ToolBarY = ListY + ListHeight + Margin;
        private const float FooterY = ToolBarY + ToolBarHeight + Margin;

        // Layout constants - X values.
        internal const float ListWidth = 400f;
        private const float ControlPanelX = ListWidth + (Margin * 2f);


        // Panel components.
        private readonly UIFastList configList;
        private readonly UICheckBox customCheck;
        private readonly UITextField fileNameField;
        private readonly UIButton activeCopyButton, selectedCopyButton, newCleanButton, deleteButton;

        // Status flag.
        private readonly bool inGame;

        // Instance reference.
        private static ConfigurationsPanel instance;


        // Current selection.
        private static string selectedConfig;
        internal static string SelectedConfig
        {
            set
            {
                if (value != selectedConfig)
                {
                    selectedConfig = value;
                    instance?.UpdateButtonStates();
                }
            }
        }


        /// <summary>
        /// Adds configurations panel tab to tabstrip.
        /// </summary>
        /// <param name="tabStrip">Tab strip to add to</param>
        /// <param name="tabIndex">Index number of tab</param>
        internal ConfigurationsPanel(UITabstrip tabStrip, int tabIndex)
        {
            // Set reference.
            instance = this;

            // Determine if we're in-game or not; use status of replacer managers to determine.
            inGame = BuildingReplacement.Instance != null && NetworkReplacement.Instance != null;

            // Add tab and helper.
            UIPanel panel = PanelUtils.AddTab(tabStrip, Translations.Translate("BOB_OPT_CFG"), tabIndex);
            UIHelper helper = new UIHelper(panel);
            panel.autoLayout = false;

            // Config list panel.
            UIPanel configListPanel = panel.AddUIComponent<UIPanel>();
            configListPanel.width = ListWidth;
            configListPanel.height = ListHeight;
            configListPanel.relativePosition = new Vector2(Margin, ListY);

            // Config selection list.
            configList = UIFastList.Create<UIConfigRow>(configListPanel);
            configList.backgroundSprite = "UnlockingPanel";
            configList.width = configListPanel.width;
            configList.height = configListPanel.height;
            configList.canSelect = true;
            configList.rowHeight = RowHeight;
            configList.autoHideScrollbar = true;
            configList.relativePosition = Vector2.zero;
            configList.rowsData = new FastList<object>();

            // File name textfield.
            UILabel fileTextLabel = UIControls.AddLabel(panel, ControlPanelX, ListY, "New configuration name:");
            fileNameField = UIControls.AddTextField(panel, ControlPanelX, ListY + fileTextLabel.height);
            fileNameField.eventTextChanged += (control, text) => UpdateButtonStates();

            // Buttons.
            activeCopyButton = UIControls.AddButton(panel, ControlPanelX, ListY + 70f, Translations.Translate("BOB_CFG_SAC"), 300f, scale: 0.8f);
            activeCopyButton.eventClicked += NewCurrent;
            selectedCopyButton = UIControls.AddButton(panel, ControlPanelX, ListY + 105f, Translations.Translate("BOB_CFG_SSC"), 300f, scale: 0.8f);
            selectedCopyButton.eventClicked += CopySelected;
            newCleanButton = UIControls.AddButton(panel, ControlPanelX, ListY + 140f, Translations.Translate("BOB_CFG_SEC"), 300f, scale: 0.8f);
            newCleanButton.eventClicked += NewClean;
            deleteButton = UIControls.AddButton(panel, ControlPanelX, ListY + 210f, Translations.Translate("BOB_CFG_DEL"), 300f, scale: 0.8f);
            deleteButton.eventClicked += Delete;

            // Ingame buttons - 'use custom' check and apply and nuke buttons.
            if (inGame)
            {
                // Use custom check box.
                customCheck = UIControls.LabelledCheckBox(panel, Margin, ToolBarY, Translations.Translate("BOB_CFG_UCS"));
                customCheck.eventCheckChanged += (control, isChecked) =>
                {
                    // If we've got a valid selection, set the current config name to this.
                    if (isChecked && !string.IsNullOrEmpty(selectedConfig))
                    {
                        ConfigurationUtils.CurrentSavedConfigName = selectedConfig;
                    }
                };

                // Apply button.
                UIButton applyButton = UIControls.AddButton(panel, Margin, FooterY, Translations.Translate("BOB_CFG_LAA"), 400f, scale: 0.8f);
                applyButton.eventClicked += Apply;

                // Use global configuration button.
                UIButton globalButton = UIControls.AddButton(panel, Margin, FooterY + 50f, Translations.Translate("BOB_CFG_LGL"), 400f, scale: 0.8f);
                globalButton.eventClicked += UseGlobal;

                // Clean up config button.
                UIButton cleanUpButton = UIControls.AddButton(panel, Margin + 50f, FooterY + 150f, Translations.Translate("BOB_CFG_CLE"), 300f);
                cleanUpButton.tooltip = Translations.Translate("BOB_CFG_CLE_TIP");
                cleanUpButton.eventClicked += (control, clickEvent) => ConfigurationUtils.Cleanup();

                // Nuke all settings button.
                UIButton nukeButton = UIControls.AddButton(panel, Margin + 50f, FooterY + 200f, Translations.Translate("BOB_NUKE"), 300f);
                nukeButton.eventClicked += (control, clickEvent) =>
                {
                    // Revert all-building and building settings.
                    ReplacementUtils.NukeSettings();
                };

                // Clean map data button.
                UIButton cleanSaveButton = UIControls.AddButton(panel, Margin + 50f, FooterY + 280f, Translations.Translate("BOB_CFG_CMD"), 300f, scale: 0.8f, tooltip: Translations.Translate("BOB_CFG_CMD_TIP"));
                cleanSaveButton.eventClicked += (control, clickEvent) =>
                {
                    // Clean all map data.
                    MapTreeReplacement.instance.Setup();
                    MapPropReplacement.instance.Setup();
                };
            }

            // Populate selection list and set initial button states.
            RefreshList();

            // Select current pack if we've got one.
            if (customCheck != null && customCheck.isChecked)
            {
                // Try to select current config name.
                selectedConfig = configList.FindItem(ConfigurationUtils.CurrentSavedConfigName);

                // Did we find it?
                if (selectedConfig == null)
                {
                    // Not found; uncheck the use custom check.
                    customCheck.isChecked = false;
                }
            }

            // Set initial button states.
            UpdateButtonStates();
        }


        /// <summary>
        /// Repopulates the config list and clears current selection.
        /// </summary>
        private void RefreshList()
        {
            configList.selectedIndex = -1;
            configList.rowsData = ConfigurationUtils.GetConfigFastList();
            selectedConfig = null;

            // Update button states.
            UpdateButtonStates();
        }


        /// <summary>
        /// 'Copy selected configuration' button event handler.
        /// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
        private void CopySelected(UIComponent control, UIMouseEventParameter mouseEvent)
        {
            // Ensure valid current selection and new filename before proceeding.
            if (!string.IsNullOrEmpty(selectedConfig) && !string.IsNullOrEmpty(fileNameField.text))
            {
                // Copy file, capturing any error message.
                string message = ConfigurationUtils.CopyCurrent(selectedConfig, fileNameField.text);
                if (message == null)
                {
                    // Successful copy - clear text field, clear selection, and refresh config list.
                    RefreshList();
                    fileNameField.text = "";
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
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
        private void NewClean(UIComponent control, UIMouseEventParameter mouseEvent)
        {
            // Ensure valid current selection and new filename before proceeding.
            if (!string.IsNullOrEmpty(fileNameField.text))
            {
                ConfigurationUtils.BlankConfig(fileNameField.text);
                RefreshList();
            }
        }


        /// <summary>
        /// 'Save current configuration' button event handler.
        /// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
        private void NewCurrent(UIComponent control, UIMouseEventParameter mouseEvent)
        {
            // Ensure valid current selection and new filename before proceeding.
            if (!string.IsNullOrEmpty(fileNameField.text))
            {
                ConfigurationUtils.SaveConfig(fileNameField.text);
                RefreshList();
            }
        }


        /// <summary>
        /// 'Delete selected configuration' button event handler.
        /// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
        private void Delete(UIComponent control, UIMouseEventParameter mouseEvent)
        {
            // Ensure valid selection before deletion.
            if (!string.IsNullOrEmpty(selectedConfig))
            {
                ConfigurationUtils.DeleteConfig(selectedConfig);
                RefreshList();
            }
        }


        /// <summary>
        /// 'Save and apply' button event handler.  Should only be called ingame.
        /// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
        private void Apply(UIComponent control, UIMouseEventParameter mouseEvent)
        {
            // Are we in-game, and do we have a valid current selection?
            if (inGame && !string.IsNullOrEmpty(selectedConfig))
            {
                // Yes - set current configuration file.
                ConfigurationUtils.CurrentSavedConfigName = selectedConfig;

                Logging.KeyMessage("current configuration set to ", ConfigurationUtils.CurrentSavedConfigName);

                // Load config.
                LoadConfig();
            }
        }


        /// <summary>
        /// 'Load global settings' button event handler.  Should only be called ingame.
        /// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
        private void UseGlobal(UIComponent control, UIMouseEventParameter mouseEvent)
        {
            // Are we in-game, and do we have a valid current selection?
            if (inGame)
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
            bool validFile = !string.IsNullOrEmpty(fileNameField.text);
            bool validSelection = !string.IsNullOrEmpty(selectedConfig);

            // Need a valid filename to enable creating new files, and 'current selction' is only valid when in-game.
            activeCopyButton.isEnabled = validFile & Loading.isLoaded;
            newCleanButton.isEnabled = validFile;

            // Selected copy button requires both a valid filename and valid current selection.
            selectedCopyButton.isEnabled = validFile && validSelection;

            // Delete button requires a valid current selection.
            deleteButton.isEnabled = validSelection;

            // Set 'use selected config as default' check.
            if (customCheck != null)
            {
                customCheck.isChecked = validSelection && selectedConfig == ConfigurationUtils.CurrentSavedConfigName;
            }
        }
    }


    /// <summary>
    /// An individual row in the list of config files.
    /// </summary>
    public class UIConfigRow : UIBasicRow
    {
        // Layout constants.
        protected override float TextX => 10f;


        // Panel components.
        private string thisConfigName;


        /// <summary>
        /// Mouse click event handler - updates the selection to what was clicked.
        /// </summary>
        /// <param name="p">Mouse event parameter</param>
        protected override void OnClick(UIMouseEventParameter p)
        {
            base.OnClick(p);

            ConfigurationsPanel.SelectedConfig = thisConfigName;
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
                rowLabel.width = ConfigurationsPanel.ListWidth;
                rowLabel.relativePosition = new Vector2(TextX, 6f);
            }

            // Set selected config.
            thisConfigName = data as string;
            rowLabel.text = thisConfigName ?? "Null";

            // Set initial background as deselected state.
            Deselect(isRowOdd);
        }
    }
}