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
        /// Creates a dropdown menu with an attached text label.
        /// </summary>
        /// <param name="parent">Parent component</param>
        /// <param name="text">Text label</param>
        /// <param name="xPos">Relative x position (default 20)</param>
        /// <param name="yPos">Relative y position (default 0)</param>
        /// <param name="width">Dropdown menu width, excluding label (default 220f)</param>
        /// <returns></returns>
        public static UIDropDown AddLabelledDropDown(UIComponent parent, string text, float xPos = 20f, float yPos = 0f, float width = 220f)
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
        /// <returns></returns>
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

            dropDown.relativePosition = new Vector2(xPos, yPos);

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
            button.relativePosition = new Vector3(0f, 0f);
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
        /// Creates a vertical scrollbar.
        /// </summary>
        /// <param name="parent">Parent component</param>
        /// <param name="scrollPanel">Panel to scroll</param>
        /// <returns></returns>
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