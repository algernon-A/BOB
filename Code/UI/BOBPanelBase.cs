// <copyright file="BOBPanelBase.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Panel to setup random props/trees.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Protected fields")]
    internal abstract class BOBPanelBase : UIPanel
    {
        /// <summary>
        /// Layout margin.
        /// </summary>
        protected const float Margin = 5f;

        /// <summary>
        /// Toggle button size.
        /// </summary>
        protected const float ToggleSize = 32f;

        /// <summary>
        /// Arrow button width.
        /// </summary>
        protected const float ArrowButtonWidth = 32f;

        /// <summary>
        /// Arrow button height.
        /// </summary>
        protected const float ArrowButtonHeight = 20f;

        /// <summary>
        /// Titlebar height.
        /// </summary>
        protected const float TitleHeight = 40f;

        /// <summary>
        /// Toolbar height.
        /// </summary>
        protected const float ToolbarHeight = 42f;

        /// <summary>
        /// Toggle header height.
        /// </summary>
        protected const float ToggleHeaderHeight = 15f;

        /// <summary>
        /// Toggle header relative Y-position.
        /// </summary>
        protected const float ToggleHeaderY = TitleHeight + Margin;

        /// <summary>
        /// Toggle button relative Y-position.
        /// </summary>
        protected const float ToggleY = ToggleHeaderY + ToggleHeaderHeight;

        /// <summary>
        /// Filter components relative Y-position.
        /// </summary>
        protected const float FilterY = TitleHeight + ToggleHeaderHeight + ToolbarHeight;

        /// <summary>
        /// Filter components height.
        /// </summary>
        protected const float FilterHeight = 20f;

        /// <summary>
        /// Prop/tree checkboxes.
        /// </summary>
        protected readonly UICheckBox[] m_propTreeChecks = new UICheckBox[(int)PropTreeModes.NumModes];

        /// <summary>
        /// "Hide vanilla" checkbox.
        /// </summary>
        protected UICheckBox m_hideVanilla;

        /// <summary>
        /// Replacement prefab name sort button.
        /// </summary>
        protected UIButton m_replacementNameSortButton;

        /// <summary>
        /// Mode label.
        /// </summary>
        protected UILabel m_modeLabel;

        /// <summary>
        /// Replacement prefab list sorting setting.
        /// </summary>
        protected int m_replacementSortSetting;

        /// <summary>
        /// Indicates whether prop/tree check change events should be ignored.
        /// </summary>
        protected bool m_ignorePropTreeCheckChanged = false;

        /// <summary>
        /// Prop/tree check atlases.
        /// </summary>
        private readonly string[] propTreepAtlas = new string[(int)PropTreeModes.NumModes]
        {
            "BOB-PropsSmall",
            "BOB-TreesSmall",
            "BOB-PropTreeSmall",
        };

        /// <summary>
        /// Prop/tree check tooltip keys.
        /// </summary>
        private readonly string[] propTreeTipKeys = new string[(int)PropTreeModes.NumModes]
        {
            "BOB_PNL_PRP",
            "BOB_PNL_TRE",
            "BOB_PNL_BOT",
        };

        // Private components.
        private readonly UITextField _textSearchField;
        private UILabel _titleLabel;

        // Current replacement mode.
        private PropTreeModes _propTreeMode = PropTreeModes.Prop;

        /// <summary>
        /// Initializes a new instance of the <see cref="BOBPanelBase"/> class.
        /// Constructor.
        /// </summary>
        internal BOBPanelBase()
        {
            // Basic behaviour.
            autoLayout = false;
            canFocus = true;
            isInteractive = true;

            // Appearance.
            backgroundSprite = "MenuPanel2";
            opacity = PanelOpacity;

            // Size.
            size = new Vector2(PanelWidth, PanelHeight);

            // Drag bar.
            UIDragHandle dragHandle = AddUIComponent<UIDragHandle>();
            dragHandle.width = this.width - 50f;
            dragHandle.height = this.height;
            dragHandle.relativePosition = Vector3.zero;
            dragHandle.target = this;

            // Close button.
            UIButton closeButton = AddUIComponent<UIButton>();
            closeButton.relativePosition = new Vector2(width - 35, 2);
            closeButton.normalBgSprite = "buttonclose";
            closeButton.hoveredBgSprite = "buttonclosehover";
            closeButton.pressedBgSprite = "buttonclosepressed";
            closeButton.eventClick += (component, clickEvent) => CloseEvent();

            // Text search field.
            _textSearchField = UITextFields.AddSmallLabelledTextField(this, width - 200f - Margin, TitleHeight + Margin, Translations.Translate("BOB_FIL_NAME"));

            // Event handlers for text search field.
            _textSearchField.eventTextChanged += (control, text) => RegenerateReplacementList();
            _textSearchField.eventTextSubmitted += (control, text) => RegenerateReplacementList();

            // Vanilla filter.
            m_hideVanilla = UICheckBoxes.AddLabelledCheckBox((UIComponent)(object)this, _textSearchField.relativePosition.x, _textSearchField.relativePosition.y + _textSearchField.height + (Margin / 2f), Translations.Translate("BOB_PNL_HDV"), 12f, 0.7f);
            m_hideVanilla.isChecked = ModSettings.HideVanilla;
            m_hideVanilla.eventCheckChanged += VanillaCheckChanged;

            // Mode label.
            m_modeLabel = UILabels.AddLabel(this, Margin, ToggleHeaderY, Translations.Translate("BOB_PNL_MOD"), textScale: 0.8f);

            // Tree/Prop checkboxes.
            for (int i = 0; i < (int)PropTreeModes.NumModes; ++i)
            {
                m_propTreeChecks[i] = IconToggleCheck(this, Margin + (i * ToggleSize), ToggleY, propTreepAtlas[i], propTreeTipKeys[i]);
                m_propTreeChecks[i].objectUserData = i;
                m_propTreeChecks[i].eventCheckChanged += PropTreeCheckChanged;
            }

            // Set initial mode state.
            _propTreeMode = InitialPropTreeMode;
            m_ignorePropTreeCheckChanged = true;
            m_propTreeChecks[(int)PropTreeMode].isChecked = true;
            m_ignorePropTreeCheckChanged = false;
        }

        /// <summary>
        /// Prop/tree modes.
        /// </summary>
        internal enum PropTreeModes : int
        {
            /// <summary>
            /// Show props only.
            /// </summary>
            Prop = 0,

            /// <summary>
            /// Show trees only.
            /// </summary>
            Tree,

            /// <summary>
            /// Show both props and trees.
            /// </summary>
            Both,

            /// <summary>
            /// Number of modes.
            /// </summary>
            NumModes,
        }

        /// <summary>
        /// Display order states.
        /// </summary>
        protected enum OrderBy : int
        {
            /// <summary>
            /// Sort by prefab display name, ascending.
            /// </summary>
            NameAscending = 0,

            /// <summary>
            /// Sort by prefab display name, descending.
            /// </summary>
            NameDescending,

            /// <summary>
            /// Sort by creator name, ascending.
            /// </summary>
            CreatorAscending,

            /// <summary>
            /// Sort by creator name, descending.
            /// </summary>
            CreatorDescending,
        }

        /// <summary>
        /// Gets the panel width.
        /// </summary>
        protected abstract float PanelWidth { get; }

        /// <summary>
        /// Gets the panel height.
        /// </summary>
        protected abstract float PanelHeight { get; }

        /// <summary>
        /// Gets the panel opacity.
        /// </summary>
        protected abstract float PanelOpacity { get; }

        /// <summary>
        /// Gets the initial prop-tree mode for this panel.
        /// </summary>
        protected virtual PropTreeModes InitialPropTreeMode => PropTreeModes.Prop;

        /// <summary>
        /// Gets or sets the current prop/tree mode.
        /// </summary>
        protected PropTreeModes PropTreeMode
        {
            get => _propTreeMode;

            set
            {
                if (_propTreeMode != value)
                {
                    _propTreeMode = value;
                }
            }
        }

        /// <summary>
        /// Gets the current search text.
        /// </summary>
        protected string SearchText => _textSearchField.text;

        /// <summary>
        /// Close button event handler.
        /// </summary>
        protected abstract void CloseEvent();

        /// <summary>
        /// Populates the replacement UIList with a filtered list of eligible relacement trees or props.
        /// </summary>
        protected abstract void RegenerateReplacementList();

        /// <summary>
        /// Hide vanilla check event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="isChecked">New checked state.</param>
        protected virtual void VanillaCheckChanged(UIComponent c, bool isChecked) => RegenerateReplacementList();

        /// <summary>
        /// Performs actions required after a change to prop/tree mode.
        /// </summary>
        protected abstract void PropTreeChange();

        /// <summary>
        /// Event handler for prop/tree checkbox changes.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="isChecked">New checked state.</param>
        protected virtual void PropTreeCheckChanged(UIComponent c, bool isChecked)
        {
            // Don't do anything if we're ignoring events.
            if (m_ignorePropTreeCheckChanged)
            {
                return;
            }

            // Suspend event handling while processing.
            m_ignorePropTreeCheckChanged = true;

            if (c is UICheckBox thisCheck)
            {
                // If this checkbox is being enabled, uncheck all others:
                if (isChecked)
                {
                    // Don't do anything if the selected mode index isn't different to the current mode.
                    if (thisCheck.objectUserData is int index && index != (int)PropTreeMode)
                    {
                        // Iterate through all checkboxes, unchecking all those that aren't this one (checkbox index stored in objectUserData).
                        for (int i = 0; i < (int)PropTreeModes.NumModes; ++i)
                        {
                            if (i != index)
                            {
                                m_propTreeChecks[i].isChecked = false;
                            }
                        }

                        // Set current mode.
                        PropTreeMode = (PropTreeModes)index;

                        // Perform post-change actions.
                        PropTreeChange();
                    }
                }
                else
                {
                    // If no other check is checked, force this one to still be checked.
                    thisCheck.isChecked = true;
                }
            }

            // Resume event handling.
            m_ignorePropTreeCheckChanged = false;
        }

        /// <summary>
        /// Replacement list sort button event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="p">Mouse event parameter.</param>
        protected virtual void SortReplacements(UIComponent c, UIMouseEventParameter p)
        {
            // Toggle status (set to descending if we're currently ascending, otherwise set to ascending).
            if (m_replacementSortSetting == (int)OrderBy.NameAscending)
            {
                // Order by name descending.
                m_replacementSortSetting = (int)OrderBy.NameDescending;
            }
            else
            {
                // Order by name ascending.
                m_replacementSortSetting = (int)OrderBy.NameAscending;
            }

            // Reset name order buttons.
            SetSortButton(m_replacementNameSortButton, m_replacementSortSetting);

            // Regenerate replacement list.
            RegenerateReplacementList();
        }

        /// <summary>
        /// Sets the states of the given sort button to match the given search status.
        /// </summary>
        /// <param name="activeButton">Currently active sort button.</param>
        /// <param name="searchStatus">Search status to apply.</param>
        protected void SetSortButton(UIButton activeButton, int searchStatus)
        {
            // Null check.
            if (activeButton == null)
            {
                return;
            }

            bool ascending = searchStatus == (int)OrderBy.NameAscending;

            // Toggle status (set to descending if we're currently ascending, otherwise set to ascending).
            if (ascending)
            {
                // Order ascending.
                SetFgSprites(activeButton, "IconUpArrow2");
            }
            else
            {
                // Order descending.
                SetFgSprites(activeButton, "IconDownArrow2");
            }
        }

        /// <summary>
        /// Adds an icon-style button to the specified component at the specified coordinates.
        /// </summary>
        /// <param name="parent">Parent UIComponent.</param>
        /// <param name="xPos">Relative X position.</param>
        /// <param name="yPos">Relative Y position.</param>
        /// <param name="size">Button size (square).</param>
        /// <param name="tooltipKey">Tooltip translation key.</param>
        /// <param name="atlas">Icon atlas.</param>
        /// <returns>New UIButton.</returns>
        protected UIButton AddIconButton(UIComponent parent, float xPos, float yPos, float size, string tooltipKey, UITextureAtlas atlas)
        {
            UIButton newButton = parent.AddUIComponent<UIButton>();

            // Size and position.
            newButton.relativePosition = new Vector2(xPos, yPos);
            newButton.height = size;
            newButton.width = size;

            // Appearance.
            newButton.atlas = atlas;

            newButton.normalFgSprite = "normal";
            newButton.focusedFgSprite = "normal";
            newButton.hoveredFgSprite = "hovered";
            newButton.disabledFgSprite = "disabled";
            newButton.pressedFgSprite = "pressed";

            // Tooltip.
            newButton.tooltip = Translations.Translate(tooltipKey);

            return newButton;
        }

        /// <summary>
        /// Adds the title text label.
        /// </summary>
        /// <param name="title">Title text.</param>
        protected void SetTitle(string title)
        {
            // Create new title lablel if none already set.
            if (_titleLabel == null)
            {
                _titleLabel = AddUIComponent<UILabel>();
            }

            // Set text.
            _titleLabel.text = title;
            _titleLabel.relativePosition = new Vector2(50f, (TitleHeight - _titleLabel.height) / 2f);
        }

        /// <summary>
        /// Adds an arrow button.
        /// </summary>
        /// <param name="parent">Parent component.</param>
        /// <param name="posX">Relative X postion.</param>
        /// <param name="posY">Relative Y position.</param>
        /// <returns>New arrow button.</returns>
        protected UIButton ArrowButton(UIComponent parent, float posX, float posY)
        {
            UIButton button = parent.AddUIComponent<UIButton>();

            // Size and position.
            button.size = new Vector2(ArrowButtonWidth, ArrowButtonHeight);
            button.relativePosition = new Vector2(posX, posY);

            // Appearance.
            SetFgSprites(button, "IconUpArrow2");
            button.canFocus = false;

            return button;
        }

        /// <summary>
        /// Sets the foreground sprites for the given button to the specified sprite.
        /// </summary>
        /// <param name="button">Targeted button.</param>
        /// <param name="spriteName">Sprite name.</param>
        protected void SetFgSprites(UIButton button, string spriteName)
        {
            button.normalFgSprite = button.hoveredFgSprite = button.pressedFgSprite = button.focusedFgSprite = spriteName;
        }

        /// <summary>
        /// Adds a BOB slider to the specified component.
        /// </summary>
        /// <param name="parent">Parent component.</param>
        /// <param name="xPos">Relative X position.</param>
        /// <param name="yPos">Relative Y position.</param>
        /// <param name="width">Slider width.</param>
        /// <param name="labelKey">Text label translation key.</param>
        /// <param name="minValue">Minimum displayed value.</param>
        /// <param name="maxValue">Maximum displayed value.</param>
        /// <param name="stepSize">Minimum slider step size.</param>
        /// <param name="name">Slider name.</param>
        /// <returns>New BOBSlider.</returns>
        protected BOBSlider AddBOBSlider(UIComponent parent, float xPos, float yPos, float width, string labelKey, float minValue, float maxValue, float stepSize, string name)
        {
            const float SliderY = 18f;
            const float ValueY = 3f;
            const float LabelY = -13f;
            const float SliderHeight = 18f;
            const float FloatTextFieldWidth = 45f;
            const float IntTextFieldWidth = 38f;

            // Slider control.
            BOBSlider newSlider = parent.AddUIComponent<BOBSlider>();
            newSlider.size = new Vector2(width, SliderHeight);
            newSlider.relativePosition = new Vector2(xPos, yPos + SliderY);
            newSlider.name = name;

            // Value field - added to parent, not to slider, otherwise slider catches all input attempts.  Integer textfields (stepsize == 1) have shorter widths.
            float textFieldWidth = stepSize == 1 ? IntTextFieldWidth : FloatTextFieldWidth;
            UITextField valueField = UITextFields.AddTinyTextField(parent, xPos + Margin + newSlider.width - textFieldWidth, yPos + ValueY, textFieldWidth);

            // Title label.
            UILabel titleLabel = UILabels.AddLabel(newSlider, 0f, LabelY, Translations.Translate(labelKey), textScale: 0.7f);

            // Autoscale tile label text, with minimum size 0.35.
            while (titleLabel.width > newSlider.width - textFieldWidth && titleLabel.textScale > 0.35f)
            {
                titleLabel.textScale -= 0.05f;
            }

            // Slider track.
            UISlicedSprite sliderSprite = newSlider.AddUIComponent<UISlicedSprite>();
            sliderSprite.atlas = UITextures.InGameAtlas;
            sliderSprite.spriteName = "BudgetSlider";
            sliderSprite.size = new Vector2(newSlider.width, 9f);
            sliderSprite.relativePosition = new Vector2(0f, 4f);

            // Slider thumb.
            UISlicedSprite sliderThumb = newSlider.AddUIComponent<UISlicedSprite>();
            sliderThumb.atlas = UITextures.InGameAtlas;
            sliderThumb.spriteName = "SliderBudget";
            newSlider.thumbObject = sliderThumb;

            // Set references.
            newSlider.ValueField = valueField;

            // Set initial values.
            newSlider.StepSize = stepSize;
            newSlider.maxValue = maxValue;
            newSlider.minValue = minValue;
            newSlider.TrueValue = 0f;

            return newSlider;
        }

        /// <summary>
        /// Adds an icon toggle checkbox.
        /// </summary>
        /// <param name="parent">Parent component.</param>
        /// <param name="xPos">Relative X position.</param>
        /// <param name="yPos">Relative Y position.</param>
        /// <param name="atlasName">Atlas name (for loading from file).</param>
        /// <param name="tooltipKey">Tooltip translation key.</param>
        /// <returns>New checkbox.</returns>
        protected UICheckBox IconToggleCheck(UIComponent parent, float xPos, float yPos, string atlasName, string tooltipKey)
        {
            // Size and position.
            UICheckBox checkBox = parent.AddUIComponent<UICheckBox>();
            checkBox.width = ToggleSize;
            checkBox.height = ToggleSize;
            checkBox.clipChildren = true;
            checkBox.relativePosition = new Vector2(xPos, yPos);

            // Checkbox sprites.
            UISprite sprite = checkBox.AddUIComponent<UISprite>();
            sprite.name = "UncheckedSprite";
            sprite.atlas = UITextures.LoadQuadSpriteAtlas(atlasName);
            sprite.spriteName = "disabled";
            sprite.size = new Vector2(ToggleSize, ToggleSize);
            sprite.relativePosition = Vector3.zero;

            checkBox.checkedBoxObject = sprite.AddUIComponent<UISprite>();
            ((UISprite)checkBox.checkedBoxObject).atlas = UITextures.LoadQuadSpriteAtlas(atlasName);
            ((UISprite)checkBox.checkedBoxObject).spriteName = "pressed";
            checkBox.checkedBoxObject.size = new Vector2(ToggleSize, ToggleSize);
            checkBox.checkedBoxObject.relativePosition = Vector3.zero;

            checkBox.tooltip = Translations.Translate(tooltipKey);

            return checkBox;
        }
    }
}
