using UnityEngine;
using ColossalFramework.UI;


namespace BOB
{
	/// <summary>
	/// Static class for UI utilities.
	/// </summary>
    internal static class UIUtils
    {
        // Package icon texture atlas.
        private static UITextureAtlas packageSprites;

        internal static UITextureAtlas PackageSprites
        {
            get
            {
                if (packageSprites == null)
                {
                    packageSprites = FileUtils.LoadSpriteAtlas("bob_prop_pack_small");
                }

                return packageSprites;
            }
        }


        /// <summary>
        /// Adds a checkbox with a descriptive text label immediately to the right.
        /// </summary>
        /// <param name="parent">Parent component</param>
        /// <param name="text">Descriptive label text</param>
        /// <param name="xPos">Relative x position (default 0)</param>
        /// <param name="yPos">Relative y position (default 0)</param>
        /// <returns>New UI checkbox with attached labels</returns>
        internal static UICheckBox AddCheckBox(UIComponent parent, string text, float xPos = 20f, float yPos = 0f)
        {
            UICheckBox checkBox = parent.AddUIComponent<UICheckBox>();

            // Size and position.
            checkBox.height = 20f;
            checkBox.clipChildren = true;
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

            // Label.
            checkBox.label = checkBox.AddUIComponent<UILabel>();
            checkBox.label.relativePosition = new Vector3(21f, 2f);
            checkBox.label.height = 20f;
            checkBox.label.textScale = 0.8f;
            checkBox.label.autoSize = true;
            checkBox.label.text = text;

            // Dynamic width to accomodate label.
            checkBox.width = checkBox.label.width + 21f;

            return checkBox;
        }


        /// <summary>
        /// Creates a pushbutton.
        /// </summary>
        /// <param name="parent">Parent component</param>
        /// <param name="text">Text label</param>
        /// <param name="width">Width (default 100)</param>
        /// <param name="xPos">Relative x position (default 0)</param>
        /// <param name="yPos">Relative y position (default 0)</param>
        /// <returns></returns>
        internal static UIButton CreateButton(UIComponent parent, string text, float width = 100f, float xPos = 0f, float yPos = 0f)
        {
            // Constants.
            const float Height = 30f;


            // Create button.
            UIButton button = parent.AddUIComponent<UIButton>();
            button.normalBgSprite = "ButtonMenu";
            button.hoveredBgSprite = "ButtonMenuHovered";
            button.pressedBgSprite = "ButtonMenuPressed";
            button.disabledBgSprite = "ButtonMenuDisabled";
            button.disabledTextColor = new Color32(128, 128, 128, 255);
            button.canFocus = false;

            // Button size parameters.
            button.relativePosition = new Vector3(xPos, yPos);
            button.size = new Vector2(width, Height);
            button.textScale = 0.9f;

            // Label.
            button.text = text;

            return button;
        }


        /// <summary>
        /// Adds a textfield with an attached label to the left.
        /// </summary>
        /// <param name="parent">Parent component</param>
        /// <param name="text">Label text</param>
        /// <param name="width">Textfield width (default 200)</param>
        /// <param name="height">Textfield height (default 30)</param>
        /// <param name="scale">Text scale (default 1.0)</param>
        /// <returns>New textfield with attached label</returns>
        internal static UITextField LabelledTextField(UIComponent parent, string text, float width = 200f, float height = 30f, float scale = 1.0f)
        {
            UITextField textField = AddTextField(parent, width, height, scale);

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
        /// Creates a basic textfield.
        /// </summary>
        /// <param name="parent">Parent component</param>
        /// <param name="width">Textfield width</param>
        /// <param name="height">Textfield height (default 30)</param>
        /// <param name="scale">Text scale (default 1.0)</param>
        /// <returns>New textfield</returns>
        public static UITextField AddTextField(UIComponent parent, float width, float height = 30f, float scale = 1.0f)
        {
            UITextField textField = parent.AddUIComponent<UITextField>();

            // Size and position.
            textField.size = new Vector2(width, height);
            textField.textScale = scale;

            // Appearance.
            textField.selectionSprite = "EmptySprite";
            textField.selectionBackgroundColor = new Color32(0, 172, 234, 255);
            textField.normalBgSprite = "TextFieldPanelHovered";
            textField.disabledBgSprite = "TextFieldPanel";
            textField.textColor = new Color32(0, 0, 0, 255);
            textField.disabledTextColor = new Color32(0, 0, 0, 128);
            textField.color = new Color32(255, 255, 255, 255);

            // Text layout.
            textField.padding = new RectOffset(6, 6, 3, 3);
            textField.horizontalAlignment = UIHorizontalAlignment.Center;

            // Behaviour.
            textField.builtinKeyNavigation = true;
            textField.isInteractive = true;
            textField.readOnly = false;

            return textField;
        }


        /// <summary>
        /// Sanitises a raw prefab name for display.
        /// Called by the settings panel fastlist.
        /// </summary>
        /// <param name="fullName">Original (raw) prefab name</param>
        /// <returns>Cleaned display name</returns>
        internal static string GetDisplayName(string fullName)
		{
			// Find any leading period (Steam package number).
			int num = fullName.IndexOf('.');

			// If no period, assume vanilla asset; return full name preceeded by vanilla flag.
			if (num < 0)
			{
				return "[v] " + fullName;
			}

			// Otherwise, omit the package number, and trim off any trailing _Data.
			return fullName.Substring(num + 1).Replace("_Data", "");
		}
	}
}