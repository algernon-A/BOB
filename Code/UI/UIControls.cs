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
        /// Creates a vertical scrollbar
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