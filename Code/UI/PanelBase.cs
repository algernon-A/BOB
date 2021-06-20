using UnityEngine;
using ColossalFramework.UI;


namespace BOB
{
	/// <summary>
	/// Panel to setup random props/trees.
	/// </summary>
	internal abstract class BOBPanelBase : UIPanel
	{
		// Display order state.
		internal enum OrderBy
		{
			NameAscending = 0,
			NameDescending
		}


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
		protected UIButton loadedNameButton;

		// Search settings.
		protected int loadedSearchStatus;


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
		/// Loaded list sort button event handler.
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
		/// </summary>
		protected void SortLoaded(UIComponent control, UIMouseEventParameter mouseEvent)
		{
			// Toggle status (set to descending if we're currently ascending, otherwise set to ascending).
			if (loadedSearchStatus == (int)OrderBy.NameAscending)
			{
				// Order by name descending.
				loadedSearchStatus = (int)OrderBy.NameDescending;
			}
			else
			{
				// Order by name ascending.
				loadedSearchStatus = (int)OrderBy.NameAscending;
			}

			// Reset name order buttons.
			SetSortButton(loadedNameButton, loadedSearchStatus);

			// Regenerate loaded list.
			LoadedList();
		}


		/// <summary>
		/// Sets the states of the two given sort buttons to match the given search status.
		/// </summary>
		/// <param name="activeButton">Currently active sort button</param>
		/// <param name="inactiveButton">Inactive button (other sort button for same list)</param>
		/// <param name="searchStatus">Search status to apply</param>
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
		/// Adds an arrow button.
		/// </summary>
		/// <param name="parent">Parent component</param>
		/// <param name="posX">Relative X postion</param>
		/// <param name="posY">Relative Y position</param>
		/// <param name="width">Button width (default 32)</param>
		/// <param name="height">Button height (default 20)</param>
		/// <returns>New arrow button</returns>
		protected UIButton ArrowButton(UIComponent parent, float posX, float posY, float width = 32f, float height = 20f)
		{
			UIButton button = parent.AddUIComponent<UIButton>();

			// Size and position.
			button.size = new Vector2(width, height);
			button.relativePosition = new Vector2(posX, posY);

			// Appearance.
			SetFgSprites(button, "IconUpArrow2");
			button.canFocus = false;

			return button;
		}


		/// <summary>
		/// Sets the foreground sprites for the given button to the specified sprite.
		/// </summary>
		/// <param name="button">Targeted button</param>
		/// <param name="spriteName">Sprite name</param>
		protected void SetFgSprites(UIButton button, string spriteName)
		{
			button.normalFgSprite = button.hoveredFgSprite = button.pressedFgSprite = button.focusedFgSprite = spriteName;
		}


		/// <summary>
		/// Adds a BOB slider to the specified component.
		/// </summary>
		/// <param name="parent">Parent component</param>
		/// <param name="xPos">Relative X position</param
		/// <param name="yPos">Relative Y position</param
		/// <param name="width">Slider width</param>
		/// <param name="labelKey">Text label translation key</param>
		/// <param name="minValue">Minimum displayed value</param
		/// <param name="maxValue">Maximum displayed value</param>
		/// <param name="stepSize">Minimum slider step size</param>
		/// <param name="name">Slider name</param>
		/// <returns>New BOBSlider</returns>
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
			UITextField valueField = UIControls.TinyTextField(parent, xPos + Margin + newSlider.width - textFieldWidth, yPos + ValueY, textFieldWidth);

			// Title label.
			UILabel titleLabel = UIControls.AddLabel(newSlider, 0f, LabelY, Translations.Translate(labelKey), textScale: 0.7f);

			// Autoscale tile label text, with minimum size 0.35.
			while (titleLabel.width > newSlider.width - textFieldWidth && titleLabel.textScale > 0.35f)
			{
				titleLabel.textScale -= 0.05f;
			}

			// Slider track.
			UISlicedSprite sliderSprite = newSlider.AddUIComponent<UISlicedSprite>();
			sliderSprite.atlas = TextureUtils.InGameAtlas;
			sliderSprite.spriteName = "BudgetSlider";
			sliderSprite.size = new Vector2(newSlider.width, 9f);
			sliderSprite.relativePosition = new Vector2(0f, 4f);

			// Slider thumb.
			UISlicedSprite sliderThumb = newSlider.AddUIComponent<UISlicedSprite>();
			sliderThumb.atlas = TextureUtils.InGameAtlas;
			sliderThumb.spriteName = "SliderBudget";
			newSlider.thumbObject = sliderThumb;

			// Set references.
			newSlider.ValueField = valueField;

			// Event handler for textfield.
			newSlider.ValueField.eventTextSubmitted += newSlider.OnTextSubmitted;

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
