using System;
using UnityEngine;
using ColossalFramework.UI;


namespace BOB
{
    /// <summary>
    /// Panel for configuration presets.
    /// </summary>
    internal class BOBConfigPanel : UIPanel
    {
        // Layout constants - general.
        private const float Margin = 5f;

        // Layout constants - Y values.
        internal const float RowHeight = 30f;
        private const float ListHeight = 300f;
        private const float TitleBarHeight = 40f;
        private const float ToolBarHeight = 30f;
        private const float FooterHeight = 35f;
        private const float ListY = TitleBarHeight;
        private const float ToolBarY = ListY + ListHeight + Margin;
        private const float FooterY = ToolBarY + ToolBarHeight + Margin;
        private const float PanelHeight = FooterY + FooterHeight + Margin;

        // Layout constants - X values.
        internal const float ListWidth = 400f;
        private const float ControlPanelWidth = 310f;
        private const float ControlPanelX = ListWidth + (Margin * 2f);
        private const float PanelWidth = ControlPanelX + ControlPanelWidth + Margin;


        // Instance references.
        private static GameObject uiGameObject;
        internal static BOBConfigPanel Panel { get; private set; }

        // Panel components.
        private readonly UIFastList configList;
        private readonly UICheckBox customCheck;
        private readonly UITextField fileNameField;
        private readonly UIButton activeCopyButton, selectedCopyButton, newCleanButton, deleteButton;

        // Current selection.
        private string selectedConfig;


        /// <summary>
        /// Accessor for currently selected configuration name.
        /// </summary>
        internal string SelectedConfig
        {
            get => selectedConfig;

            set
            {
                // Don't do anything if value hasn't changed.
                if (selectedConfig != value)
                {
                    // The value has changed; update it and refresh button states.
                    selectedConfig = value;
                    UpdateButtonStates();
                }
            }
        }



        /// <summary>
        /// Creates the panel object in-game and displays it.
        /// </summary>
        internal static void Create()
        {
            try
            {
                // If no GameObject instance already set, create one.
                if (uiGameObject == null)
                {
                    // Give it a unique name for easy finding with ModTools.
                    uiGameObject = new GameObject("BOBConfigPanel");
                    uiGameObject.transform.parent = UIView.GetAView().transform;

                    // Create new panel instance and add it to GameObject.
                    Panel = uiGameObject.AddComponent<BOBConfigPanel>();
                    Panel.transform.parent = uiGameObject.transform.parent;
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception creating ConfigPanel");
            }
        }


        /// <summary>
        /// Closes the panel by destroying the object (removing any ongoing UI overhead).
        /// </summary>
        internal static void Close()
        {
            // Don't do anything if no panel.
            if (Panel == null)
            {
                return;
            }

            // Destroy game objects.
            GameObject.Destroy(Panel);
            GameObject.Destroy(uiGameObject);

            // Let the garbage collector do its work (and also let us know that we've closed the object).
            Panel = null;
            uiGameObject = null;
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        internal BOBConfigPanel()
        {
            // Basic behaviour.
            autoLayout = false;
            canFocus = true;
            isInteractive = true;

            // Appearance.
            backgroundSprite = "MenuPanel2";
            opacity = 1f;

            // Size.
            size = new Vector2(PanelWidth, PanelHeight);

            // Default position - centre in screen.
            relativePosition = new Vector2(Mathf.Floor((GetUIView().fixedWidth - width) / 2), Mathf.Floor((GetUIView().fixedHeight - height) / 2));

            // Drag bar.
            UIDragHandle dragHandle = AddUIComponent<UIDragHandle>();
            dragHandle.width = this.width - 50f;
            dragHandle.height = this.height;
            dragHandle.relativePosition = Vector3.zero;
            dragHandle.target = this;

            // Title label.
            UILabel titleLabel = AddUIComponent<UILabel>();
            titleLabel.relativePosition = new Vector2(50f, 13f);
            titleLabel.text = Translations.Translate("BOB_NAM") + " " + Translations.Translate("BOB_PNL_CFB");

            // Close button.
            UIButton closeButton = AddUIComponent<UIButton>();
            closeButton.relativePosition = new Vector2(width - 35, 2);
            closeButton.normalBgSprite = "buttonclose";
            closeButton.hoveredBgSprite = "buttonclosehover";
            closeButton.pressedBgSprite = "buttonclosepressed";
            closeButton.eventClick += (component, clickEvent) => Close();

            // Use custom check box.
            customCheck = UIControls.AddCheckBox(this, Margin, ToolBarY, Translations.Translate("BOB_CFG_UCS"));
            customCheck.isChecked = !string.IsNullOrEmpty(ConfigurationUtils.currentConfig);

            // Config list panel.
            UIPanel configListPanel = AddUIComponent<UIPanel>();
            configListPanel.width = ListWidth;
            configListPanel.height = ListHeight;
            configListPanel.relativePosition = new Vector3(Margin, ListY);

            // Config selection list.
            configList = UIFastList.Create<UIConfigRow>(configListPanel);
            configList.backgroundSprite = "UnlockingPanel";
            configList.width = configListPanel.width;
            configList.height = configListPanel.height;
            configList.canSelect = true;
            configList.rowHeight = RowHeight;
            configList.autoHideScrollbar = true;
            configList.relativePosition = Vector3.zero;
            configList.rowsData = new FastList<object>();

            // File name textfield.
            UILabel fileTextLabel = UIControls.AddLabel(this, ControlPanelX, ListY, "New configuration name:");
            fileNameField = UIControls.AddTextField(this, ControlPanelX, ListY + fileTextLabel.height);
            fileNameField.eventTextChanged += (control, text) => UpdateButtonStates();

            // Buttons.
            activeCopyButton = UIControls.AddButton(this, ControlPanelX, ListY + 70f, Translations.Translate("BOB_CFG_SAC"), 300f, scale: 0.8f);
            activeCopyButton.eventClicked += NewCurrent;
            selectedCopyButton = UIControls.AddButton(this, ControlPanelX, ListY + 105f, Translations.Translate("BOB_CFG_SSC"), 300f, scale: 0.8f);
            selectedCopyButton.eventClicked += CopySelected;
            newCleanButton = UIControls.AddButton(this, ControlPanelX, ListY + 140f, Translations.Translate("BOB_CFG_SEC"), 300f, scale: 0.8f);
            newCleanButton.eventClicked += NewClean;
            deleteButton = UIControls.AddButton(this, ControlPanelX, ListY + 210f, Translations.Translate("BOB_CFG_DEL"), 300f, scale: 0.8f);
            deleteButton.eventClicked += Delete;

            UIButton cancelButton = UIControls.AddButton(this, Margin, FooterY, "Cancel", 300f, scale: 0.8f);
            cancelButton.eventClicked += (control, clickEvent) => Close();
            UIButton applyButton = UIControls.AddButton(this, (PanelWidth / 2) + (Margin * 2), FooterY, "Save and apply", 300f, scale: 0.8f);
            applyButton.eventClicked += Apply;

            // Populate selection list and set initial button states.
            RefreshList();

            // Select current pack if we've got one.
            if (customCheck.isChecked)
            {
                // Try to select current config name.
                selectedConfig = configList.FindItem(ConfigurationUtils.currentConfig);

                // Did we find it?
                if (selectedConfig == null)
                {
                    // Not found; uncheck the use custom check.
                    customCheck.isChecked = false;
                }
            }

            // Set initial button states.
            UpdateButtonStates();

            // Focus.
            BringToFront();
            Focus();
        }


        /// <summary>
        /// Repopulates the config list and clears current selection.
        /// </summary>
        private void RefreshList()
        {
            configList.m_rowsData = ConfigurationUtils.GetConfigFastList();
            configList.Refresh();
            configList.selectedIndex = -1;
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
            if (!string.IsNullOrEmpty(selectedConfig) && !string.IsNullOrEmpty(fileNameField.text))
            {
                ConfigurationUtils.SaveConfig(fileNameField.text, true);
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
            if (!string.IsNullOrEmpty(selectedConfig) && !string.IsNullOrEmpty(fileNameField.text))
            {
                ConfigurationUtils.SaveConfig(fileNameField.text, false);
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
        /// 'Save and apply' button event handler.
        /// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
        private void Apply(UIComponent control, UIMouseEventParameter mouseEvent)
        {
            // Are we using custom settings for this savegame, and have a valid current selection?
            if (customCheck.isChecked && !string.IsNullOrEmpty(selectedConfig))
            {
                // Yes - set current configuration file.
                ConfigurationUtils.currentConfig = selectedConfig;

                // Clear current replacements.
                ReplacementUtils.NukeSettings();

                // Load config file.
                ConfigurationUtils.LoadConfig();
            }
            else
            {
                // No custom settings - did we have any previously?
                if (ConfigurationUtils.currentConfig != null)
                {
                    // We had previous settings - reset.
                    ConfigurationUtils.currentConfig = null;

                    // Clear current replacements.
                    ReplacementUtils.NukeSettings();

                    // Load config file.
                    ConfigurationUtils.LoadConfig();
                }
            }

            // Close and exit.
            Close();
        }


        /// <summary>
        /// Updates the control button states based on current panel state.
        /// </summary>
        private void UpdateButtonStates()
        {
            // See if there's a valid filename string.
            bool validFile = !string.IsNullOrEmpty(fileNameField.text);
            bool validSelection = !string.IsNullOrEmpty(selectedConfig);

            // Need a valid filename to enable creating new files.
            activeCopyButton.isEnabled = validFile;
            newCleanButton.isEnabled = validFile;

            // Selected copy button requires both a valid filename and valid current selection.
            selectedCopyButton.isEnabled = validFile && validSelection;

            // Delete button requires a valid current selection.
            deleteButton.isEnabled = validSelection;
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

            BOBConfigPanel.Panel.SelectedConfig = thisConfigName;
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
                rowLabel.width = BOBConfigPanel.ListWidth;
                rowLabel.relativePosition = new Vector3(TextX, 6f);
            }

            // Set selected config.
            thisConfigName = data as string;
            rowLabel.text = thisConfigName ?? "Null";

            // Set initial background as deselected state.
            Deselect(isRowOdd);
        }
    }
}