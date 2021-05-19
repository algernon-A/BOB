using UnityEngine;
using ColossalFramework.UI;


namespace BOB
{
	/// <summary>
	/// Panel to setup random props/trees.
	/// </summary>
	internal abstract class BOBPanelBase : UIPanel
	{
		// Layout constants - general.
		protected const float Margin = 5f;
		protected const float ToggleSize = 32f;

		// Layout constants - Y.
		protected const float TitleHeight = 40f;
		protected const float ToolbarHeight = 42f;
		protected const float FilterY = TitleHeight + ToolbarHeight;
		protected const float FilterHeight = 20f;


		// Panel components.
		protected UITextField nameFilter;
		protected UICheckBox hideVanilla;
		protected UICheckBox treeCheck, propCheck;


		/// <summary>
		/// Panel width.
		/// </summary>
		protected abstract float PanelWidth { get; }


		/// <summary>
		/// Panel height.
		/// </summary>
		protected abstract float PanelHeight { get; }

		/// <summary>
		/// Panel opacity.
		/// </summary>
		protected abstract float PanelOpacity { get; }


		/// <summary>
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

			// Name filter.
			nameFilter = UIControls.SmallLabelledTextField(this, width - 200f - Margin, TitleHeight + Margin, Translations.Translate("BOB_FIL_NAME"));
			// Event handlers for name filter textbox.
			nameFilter.eventTextChanged += (control, text) => LoadedList();
			nameFilter.eventTextSubmitted += (control, text) => LoadedList();

			// Vanilla filter.
			hideVanilla = UIControls.LabelledCheckBox((UIComponent)(object)this, nameFilter.relativePosition.x, nameFilter.relativePosition.y + nameFilter.height + (Margin / 2f), Translations.Translate("BOB_PNL_HDV"), 12f, 0.7f);
			hideVanilla.isChecked = ModSettings.hideVanilla;
			hideVanilla.eventCheckChanged += VanillaCheckChanged;

			// Tree/Prop checkboxes.
			propCheck = IconToggleCheck(this, Margin, TitleHeight + Margin, "bob_props3", "BOB_PNL_PRP");
			treeCheck = IconToggleCheck(this, Margin + ToggleSize, TitleHeight + Margin, "bob_trees_small", "BOB_PNL_TRE");
			propCheck.isChecked = !InitialTreeCheckedState;
			treeCheck.isChecked = InitialTreeCheckedState;
			propCheck.eventCheckChanged += PropCheckChanged;
			treeCheck.eventCheckChanged += TreeCheckChanged;
		}


		/// <summary>
		/// Initial tree/prop checked state.
		/// </summary>
		protected abstract bool InitialTreeCheckedState { get; }


		/// <summary>
		/// Close button event handler.
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
		/// </summary>
		protected abstract void CloseEvent();


		/// <summary>
		/// Populates a fastlist with a filtered list of loaded trees or props.
		/// </summary>
		protected abstract void LoadedList();


		/// <summary>
		/// Hide vanilla check event handler.
		/// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="isChecked">New checked state</param>
		protected virtual void VanillaCheckChanged(UIComponent control, bool isChecked) => LoadedList();


		/// <summary>
		/// Prop check event handler.
		/// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="isChecked">New checked state</param>
		protected abstract void PropCheckChanged(UIComponent control, bool isChecked);


		/// <summary>
		/// Tree check event handler.
		/// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="isChecked">New checked state</param>
		protected abstract void TreeCheckChanged(UIComponent control, bool isChecked);


		/// <summary>
		/// Adds an icon-style button to the specified component at the specified coordinates.
		/// </summary>
		/// <param name="parent">Parent UIComponent</param>
		/// <param name="xPos">Relative X position</param>
		/// <param name="yPos">Relative Y position</param>
		/// <param name="size">Button size (square)</param>
		/// <param name="tooltipKey">Tooltip translation key</param>
		/// <param name="atlas">Icon atlas</param>
		/// <returns>New UIButton</returns>
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
		/// <param name="title">Title text</param>
		protected void AddTitle(string title)
        {
			// Title label.
			UILabel titleLabel = AddUIComponent<UILabel>();
			titleLabel.text = title;
			titleLabel.relativePosition = new Vector2(50f, (TitleHeight - titleLabel.height) / 2f);
		}


		/// <summary>
		/// Adds an icon toggle checkbox.
		/// </summary>
		/// <param name="parent">Parent component</param>
		/// <param name="xPos">Relative X position</param>
		/// <param name="yPos">Relative Y position</param>
		/// <param name="atlasName">Atlas name (for loading from file)</param>
		/// <param name="tooltipKey">Tooltip translation key</param>
		/// <returns>New checkbox</returns>
		private UICheckBox IconToggleCheck(UIComponent parent, float xPos, float yPos, string atlasName, string tooltipKey)
		{
			// Size and position.
			UICheckBox checkBox = parent.AddUIComponent<UICheckBox>();
			checkBox.width = ToggleSize;
			checkBox.height = ToggleSize;
			checkBox.clipChildren = true;
			checkBox.relativePosition = new Vector2(xPos, yPos);

			// Checkbox sprites.
			UISprite sprite = checkBox.AddUIComponent<UISprite>();
			sprite.atlas = TextureUtils.LoadSpriteAtlas(atlasName);
			sprite.spriteName = "disabled";
			sprite.size = new Vector2(ToggleSize, ToggleSize);
			sprite.relativePosition = Vector3.zero;

			checkBox.checkedBoxObject = sprite.AddUIComponent<UISprite>();
			((UISprite)checkBox.checkedBoxObject).atlas = TextureUtils.LoadSpriteAtlas(atlasName);
			((UISprite)checkBox.checkedBoxObject).spriteName = "pressed";
			checkBox.checkedBoxObject.size = new Vector2(ToggleSize, ToggleSize);
			checkBox.checkedBoxObject.relativePosition = Vector3.zero;

			checkBox.tooltip = Translations.Translate(tooltipKey);

			return checkBox;
		}
	}
}
