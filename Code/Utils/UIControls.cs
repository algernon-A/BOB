using UnityEngine;
using ColossalFramework.UI;


namespace BOB
{
    /// <summary>
    /// Static utilities class for creating UI controls.
    /// </summary>
    public static class UIControls
    {
        /// <summary>
        /// Adds a simple pushbutton.
        /// </summary>
        /// <param name="parent">Parent component</param>
        /// <param name="posX">Relative X postion</param>
        /// <param name="posY">Relative Y position</param>
        /// <param name="text">Button text</param>
        /// <param name="width">Button width (default 200)</param>
        /// <param name="height">Button height (default 30)</param>
        /// <param name="scale">Text scale (default 0.9)</param>
        /// <returns></returns>
        public static UIButton AddButton(UIComponent parent, float posX, float posY, string text, float width = 200f, float height = 30f, float scale = 0.9f)
        {
            UIButton button = parent.AddUIComponent<UIButton>();

            // Size and position.
            button.size = new Vector2(width, height);
            button.relativePosition = new Vector2(posX, posY);

            // Appearance.
            button.textScale = scale;
            button.normalBgSprite = "ButtonMenu";
            button.hoveredBgSprite = "ButtonMenuHovered";
            button.pressedBgSprite = "ButtonMenuPressed";
            button.disabledBgSprite = "ButtonMenuDisabled";
            button.disabledTextColor = new Color32(128, 128, 128, 255);
            button.canFocus = false;

            // Text.
            button.text = text;

            return button;
        }


        /// <summary>
        /// Adds a large textfield with an attached label to the left.
        /// </summary>
        /// <param name="parent">Parent component</param>
        /// <param name="posX">Relative X postion</param>
        /// <param name="posY">Relative Y position</param>
        /// <param name="text">Label text</param>
        /// <param name="width">Textfield width (default 200)</param>
        /// <returns>New large textfield with attached label</returns>
        public static UITextField BigLabelledTextField(UIComponent parent, float posX, float posY, string text, float width = 200f) => LabelledTextField(parent, posX, posY, text, width, 30f, 1.2f, 6);


        /// <summary>
        /// Adds a textfield with an attached label to the left.
        /// </summary>
        /// <param name="parent">Parent component</param>
        /// <param name="posX">Relative X postion</param>
        /// <param name="posY">Relative Y position</param>
        /// <param name="text">Label text</param>
        /// <param name="width">Textfield width (default 200)</param>
        /// <param name="height">Textfield height (default 22)</param>
        /// <param name="scale">Text scale (default 1.0)</param>
        /// <param name="vertPad">Vertical text padding within textfield box (default 4)</param>
        /// <returns>New textfield with attached label</returns>
        public static UITextField LabelledTextField(UIComponent parent, float posX, float posY, string text, float width = 200f, float height = 22f, float scale = 1.0f, int vertPad = 4)
        {
            UITextField textField = AddTextField(parent, posX, posY, width, height, scale, vertPad);

            // Label.
            UILabel label = textField.AddUIComponent<UILabel>();
            label.textScale = scale;
            label.text = text;
            label.autoSize = true;
            label.verticalAlignment = UIVerticalAlignment.Middle;
            label.wordWrap = true;

            // Set position.
            label.relativePosition = new Vector2(-(label.width + 5f), (height - label.height) / 2);

            return textField;
        }


        /// <summary>
        /// Adds an input text field at the specified coordinates.
        /// </summary>
        /// <param name="textField">Textfield object</param>
        /// <param name="posX">Relative X postion</param>
        /// <param name="posY">Relative Y position</param>
        /// <param name="parent">component to add to</param>
        /// <param name="height">Textfield height (default 22)</param>
        /// <param name="scale">Text scale (default 1.0)</param>
        /// <param name="vertPad">Vertical text padding within textfield box (default 4)</param>
        /// <param name="tooltip">Tooltip, if any</param>
        /// <returns>New textfield *without* attached label</returns>
        public static UITextField AddTextField(UIComponent parent, float posX, float posY, float width = 200f, float height = 22f, float scale = 1f, int vertPad = 4, string tooltip = null)
        {
            UITextField textField = parent.AddUIComponent<UITextField>();

            // Size and position.
            textField.size = new Vector2(width, height);
            textField.relativePosition = new Vector2(posX, posY);

            // Text settings.
            textField.textScale = scale;
            textField.padding = new RectOffset(6, 6, vertPad, 3);
            textField.horizontalAlignment = UIHorizontalAlignment.Center;

            // Behaviour.
            textField.builtinKeyNavigation = true;
            textField.isInteractive = true;
            textField.readOnly = false;

            // Appearance.
            textField.color = new Color32(255, 255, 255, 255);
            textField.textColor = new Color32(0, 0, 0, 255);
            textField.disabledTextColor = new Color32(0, 0, 0, 128);
            textField.selectionSprite = "EmptySprite";
            textField.selectionBackgroundColor = new Color32(0, 172, 234, 255);
            textField.normalBgSprite = "TextFieldPanelHovered";
            textField.disabledBgSprite = "TextFieldPanel";

            // Add tooltip.
            if (tooltip != null)
            {
                textField.tooltip = tooltip;
            }

            return textField;
        }


        /// <summary>
        /// Adds a checkbox with a descriptive text label immediately to the right.
        /// </summary>
        /// <param name="parent">Parent component</param>
        /// <param name="text">Descriptive label text</param>
        /// <param name="xPos">Relative x position</param>
        /// <param name="yPos">Relative y position</param>
        /// <param name="textScale">Text scale of label (default 0.8)</param>
        /// <returns>New UI checkbox with attached labels</returns>
        public static UICheckBox AddCheckBox(UIComponent parent, float xPos, float yPos, string text, float textScale = 0.8f)
        {
            // Create base checkbox.
            UICheckBox checkBox = AddCheckBox(parent, xPos, yPos);

            // Label.
            checkBox.label = checkBox.AddUIComponent<UILabel>();
            checkBox.label.relativePosition = new Vector3(21f, checkBox.height / 2f);
            checkBox.label.anchor = UIAnchorStyle.Left | UIAnchorStyle.CenterVertical;
            checkBox.label.textScale = textScale;
            checkBox.label.autoSize = true;
            checkBox.label.text = text;

            // Dynamic width to accomodate label.
            checkBox.width = checkBox.label.width + 21f;

            return checkBox;
        }


        /// <summary>
        /// Adds a checkbox without a label.
        /// </summary>
        /// <param name="parent">Parent component</param>
        /// <param name="xPos">Relative x position</param>
        /// <param name="yPos">Relative y position</param>
        /// <returns>New UI checkbox *without* attached labels</returns>
        public static UICheckBox AddCheckBox(UIComponent parent, float xPos, float yPos)
        {
            UICheckBox checkBox = parent.AddUIComponent<UICheckBox>();

            // Size and position.
            checkBox.height = 16f;
            checkBox.width = 16f;
            checkBox.clipChildren = false;
            checkBox.relativePosition = new Vector3(xPos, yPos);

            // Sprites.
            UISprite sprite = checkBox.AddUIComponent<UISprite>();
            sprite.spriteName = "check-unchecked";
            sprite.size = new Vector2(16f, 16f);
            sprite.relativePosition = Vector3.zero;

            checkBox.checkedBoxObject = sprite.AddUIComponent<UISprite>();
            ((UISprite)checkBox.checkedBoxObject).spriteName = "check-checked";
            checkBox.checkedBoxObject.size = new Vector2(16f, 16f);
            checkBox.checkedBoxObject.relativePosition = Vector3.zero;

            return checkBox;
        }


        /// <summary>
        /// Creates a plain checkbox using the game's option panel checkbox template.
        /// </summary>
        /// <param name="parent">Parent component</param>
        /// <param name="text">Descriptive label text</param>
        /// <returns>New checkbox using the game's option panel template</returns>
        public static UICheckBox AddPlainCheckBox(UIComponent parent, string text)
        {
            UICheckBox checkBox = parent.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsCheckBoxTemplate")) as UICheckBox;

            // Set text.
            checkBox.text = text;

            return checkBox;
        }


        /// <summary>
        /// Adds a plain text label to the specified UI panel.
        /// </summary>
        /// <param name="parent">Parent component</param>
        /// <param name="xPos">Relative x position)</param>
        /// <param name="yPos">Relative y position</param>
        /// <param name="text">Label text</param>
        /// <param name="width">Label width (default 700)</param>
        /// <param name="width">Text scale (default 1.0)</param>
        /// <returns>New text label</returns>
        public static UILabel AddLabel(UIComponent parent, float xPos, float yPos, string text, float width = 700f, float textScale = 1.0f)
        {
            // Add label.
            UILabel label = (UILabel)parent.AddUIComponent<UILabel>();
            label.autoSize = false;
            label.autoHeight = true;
            label.wordWrap = true;
            label.width = width;
            label.textScale = textScale;
            label.text = text;
            label.relativePosition = new Vector2(xPos, yPos);

            return label;
        }


        /// <summary>
        /// Creates a dropdown menu with an attached text label.
        /// </summary>
        /// <param name="parent">Parent component</param>
        /// <param name="xPos">Relative x position</param>
        /// <param name="yPos">Relative y position</param>
        /// <param name="text">Text label</param>
        /// <param name="width">Dropdown menu width, excluding label (default 220f)</param>
        /// <returns>New dropdown menu with an attached text label and enclosing panel</returns>
        public static UIDropDown AddLabelledDropDown(UIComponent parent, float xPos, float yPos, string text, float width = 220f)
        {
            // Create dropdown.
            UIDropDown dropDown = AddDropDown(parent, xPos, yPos, width);

            // Add label.
            UILabel label = dropDown.AddUIComponent<UILabel>();
            label.textScale = 0.8f;
            label.text = text;

            // Get width and position.
            float labelWidth = label.width + 10f;

            label.relativePosition = new Vector3(-labelWidth, 6f);

            // Move dropdown to accomodate label.
            dropDown.relativePosition += new Vector3(labelWidth, 0f);

            return dropDown;
        }


        /// <summary>
        /// Creates a dropdown menu without text label or enclosing panel.
        /// </summary>
        /// <param name="parent">Parent component</param>
        /// <param name="xPos">Relative x position (default 20)</param>
        /// <param name="yPos">Relative y position (default 0)</param>
        /// <param name="width">Dropdown menu width, excluding label (default 220f)</param>
        /// <returns>New dropdown menu *without* an attached text label or enclosing panel</returns>
        public static UIDropDown AddDropDown(UIComponent parent, float xPos, float yPos, float width = 220f)
        {
            // Constants.
            const float Height = 25f;
            const int ItemHeight = 20;

            // Create dropdown menu.
            UIDropDown dropDown = parent.AddUIComponent<UIDropDown>();
            dropDown.listBackground = "GenericPanelLight";
            dropDown.itemHover = "ListItemHover";
            dropDown.itemHighlight = "ListItemHighlight";
            dropDown.normalBgSprite = "ButtonMenu";
            dropDown.disabledBgSprite = "ButtonMenuDisabled";
            dropDown.hoveredBgSprite = "ButtonMenuHovered";
            dropDown.focusedBgSprite = "ButtonMenu";
            dropDown.foregroundSpriteMode = UIForegroundSpriteMode.Stretch;
            dropDown.popupColor = new Color32(45, 52, 61, 255);
            dropDown.popupTextColor = new Color32(170, 170, 170, 255);
            dropDown.zOrder = 1;
            dropDown.verticalAlignment = UIVerticalAlignment.Middle;
            dropDown.horizontalAlignment = UIHorizontalAlignment.Left;
            dropDown.textFieldPadding = new RectOffset(8, 0, 8, 0);
            dropDown.itemPadding = new RectOffset(14, 0, 8, 0);

            dropDown.relativePosition = new Vector3(xPos, yPos);

            // Dropdown size parameters.
            dropDown.size = new Vector2(width, Height);
            dropDown.listWidth = (int)width;
            dropDown.listHeight = 500;
            dropDown.itemHeight = ItemHeight;
            dropDown.textScale = 0.7f;

            // Create dropdown button.
            UIButton button = dropDown.AddUIComponent<UIButton>();
            dropDown.triggerButton = button;
            button.size = dropDown.size;
            button.text = "";
            button.relativePosition = new Vector2(0f, 0f);
            button.textVerticalAlignment = UIVerticalAlignment.Middle;
            button.textHorizontalAlignment = UIHorizontalAlignment.Left;
            button.normalFgSprite = "IconDownArrow";
            button.hoveredFgSprite = "IconDownArrowHovered";
            button.pressedFgSprite = "IconDownArrowPressed";
            button.focusedFgSprite = "IconDownArrowFocused";
            button.disabledFgSprite = "IconDownArrowDisabled";
            button.spritePadding = new RectOffset(3, 3, 3, 3);
            button.foregroundSpriteMode = UIForegroundSpriteMode.Fill;
            button.horizontalAlignment = UIHorizontalAlignment.Right;
            button.verticalAlignment = UIVerticalAlignment.Middle;
            button.zOrder = 0;

            return dropDown;
        }


        /// <summary>
        /// Creates a plain dropdown using the game's option panel dropdown template.
        /// </summary>
        /// <param name="parent">Parent component</param>
        /// <param name="text">Descriptive label text</param>
        /// <param name="items">Dropdown menu item list</param>
        /// <param name="selectedIndex">Initially selected index (default 0)</param>
        /// <param name="width">Width of dropdown (default 60)</param>
        /// <returns>New dropdown menu using game's option panel template</returns>
        public static UIDropDown AddPlainDropDown(UIComponent parent, string text, string[] items, int selectedIndex = 0, float width = 270f)
        {
            UIPanel panel = parent.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsDropdownTemplate")) as UIPanel;
            UIDropDown dropDown = panel.Find<UIDropDown>("Dropdown");

            // Set text.
            panel.Find<UILabel>("Label").text = text;

            // Slightly increase width.
            dropDown.autoSize = false;
            dropDown.width = width;

            // Add items.
            dropDown.items = items;
            dropDown.selectedIndex = selectedIndex;

            return dropDown;
        }


        /// <summary>
        /// Creates a vertical scrollbar.
        /// </summary>
        /// <param name="parent">Parent component</param>
        /// <param name="scrollPanel">Panel to scroll</param>
        /// <returns>New vertical scrollbar linked to the specified scrollable panel</returns>
        public static UIScrollbar AddScrollbar(UIComponent parent, UIScrollablePanel scrollPanel)
        {
            // Basic setup.
            UIScrollbar newScrollbar = parent.AddUIComponent<UIScrollbar>();
            newScrollbar.orientation = UIOrientation.Vertical;
            newScrollbar.pivot = UIPivotPoint.TopLeft;
            newScrollbar.minValue = 0;
            newScrollbar.value = 0;
            newScrollbar.incrementAmount = 50f;
            newScrollbar.autoHide = true;
            newScrollbar.width = 10f;

            // Tracking sprite.
            UISlicedSprite trackSprite = newScrollbar.AddUIComponent<UISlicedSprite>();
            trackSprite.relativePosition = Vector2.zero;
            trackSprite.autoSize = true;
            trackSprite.anchor = UIAnchorStyle.All;
            trackSprite.size = trackSprite.parent.size;
            trackSprite.fillDirection = UIFillDirection.Vertical;
            trackSprite.spriteName = "ScrollbarTrack";
            newScrollbar.trackObject = trackSprite;

            // Thumb sprite.
            UISlicedSprite thumbSprite = trackSprite.AddUIComponent<UISlicedSprite>();
            thumbSprite.relativePosition = Vector2.zero;
            thumbSprite.fillDirection = UIFillDirection.Vertical;
            thumbSprite.autoSize = true;
            thumbSprite.width = thumbSprite.parent.width;
            thumbSprite.spriteName = "ScrollbarThumb";
            newScrollbar.thumbObject = thumbSprite;

            // Event handler - scroll panel.
            newScrollbar.eventValueChanged += (component, value) => scrollPanel.scrollPosition = new Vector2(0, value);

            // Event handler - mouse wheel (scrollbar and panel).
            parent.eventMouseWheel += (component, mouseEvent) => newScrollbar.value -= mouseEvent.wheelDelta * newScrollbar.incrementAmount;
            scrollPanel.eventMouseWheel += (component, mouseEvent) => newScrollbar.value -= mouseEvent.wheelDelta * newScrollbar.incrementAmount;

            // Event handler to handle resize of scroll panel.
            scrollPanel.eventSizeChanged += (component, newSize) =>
            {
                newScrollbar.relativePosition += new Vector3(scrollPanel.width, 0);
                newScrollbar.height = scrollPanel.height;
            };

            // Attach to scroll panel.
            scrollPanel.verticalScrollbar = newScrollbar;

            return newScrollbar;
        }
    }
}