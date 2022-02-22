using UnityEngine;
using ColossalFramework.UI;


namespace BOB
{
	/// <summary>
	/// Panel to setup random props/trees.
	/// </summary>
	internal abstract class BOBPanelBase : UIPanel
	{
		/// <summary>
		/// Display order states.
		/// </summary>
		protected enum OrderBy : int
		{
			NameAscending = 0,
			NameDescending,
			CreatorAscending,
			CreatorDescending
		}


		/// <summary>
		/// Prop/tree modes.
		/// </summary>
		internal enum PropTreeModes : int
		{
			Prop = 0,
			Tree,
			Both,
			NumModes
		}


		// Layout constants - general.
		protected const float Margin = 5f;
		protected const float ToggleSize = 32f;

		// Layout constants - components.
		protected const float ArrowButtonWidth = 32f;
		protected const float ArrowButtonHeight = 20f;

		// Layout constants - Y.
		protected const float TitleHeight = 40f;
		protected const float ToolbarHeight = 42f;
		protected const float ToggleHeaderHeight = 15f;
		protected const float ToggleHeaderY = TitleHeight + Margin;
		protected const float ToggleY = ToggleHeaderY + ToggleHeaderHeight;
		protected const float FilterY = TitleHeight + ToggleHeaderHeight + ToolbarHeight;
		protected const float FilterHeight = 20f;


		// Panel components.
		protected UITextField nameFilter;
		protected UICheckBox hideVanilla;
		protected UIButton loadedNameButton;
		protected UILabel modeLabel;
		private UILabel titleLabel;
		protected readonly UICheckBox[] propTreeChecks = new UICheckBox[(int)PropTreeModes.NumModes];

		// Search settings.
		protected int loadedSearchStatus;

		// Status flag.
		protected bool ignorePropTreeCheckChanged = false;


		/// <summary>
		/// Prop/tree check atlases.
		/// </summary>
		private readonly string[] propTreepAtlas = new string[(int)PropTreeModes.NumModes]
		{
			"BOB-PropsSmall",
			"BOB-TreesSmall",
			"BOB-PropTreeSmall"
		};


		/// <summary>
		/// Prop/tree check tooltip keys.
		/// </summary>
		private string[] propTreeTipKeys = new string[(int)PropTreeModes.NumModes]
		{
			"BOB_PNL_PRP",
			"BOB_PNL_TRE",
			"BOB_PNL_BOT"
		};


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
		/// Current prop/tree mode.
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
		// Current replacement mode.
		private PropTreeModes _propTreeMode = PropTreeModes.Prop;


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

			// Mode label.
			modeLabel = UIControls.AddLabel(this, Margin, ToggleHeaderY, Translations.Translate("BOB_PNL_MOD"), textScale: 0.8f);

			// Tree/Prop checkboxes.
			for (int i = 0; i < (int)PropTreeModes.NumModes; ++i)
			{
				propTreeChecks[i] = IconToggleCheck(this, Margin + (i * ToggleSize), ToggleY, propTreepAtlas[i], propTreeTipKeys[i]);
				propTreeChecks[i].objectUserData = i;
				propTreeChecks[i].eventCheckChanged += PropTreeCheckChanged;
			}

			// Set initial mode state.
			_propTreeMode = InitialPropTreeMode;
			ignorePropTreeCheckChanged = true;
			propTreeChecks[(int)PropTreeMode].isChecked = true;
			ignorePropTreeCheckChanged = false;
		}


		/// <summary>
		/// Initial prop-tree mode.
		/// </summary>
		protected abstract PropTreeModes InitialPropTreeMode { get; }


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
		/// Performs actions required after a change to prop/tree mode.
		/// </summary>
		protected abstract void PropTreeChange();


		/// <summary>
		/// Event handler for ptop/tree checkbox changes.
		/// </summary>
		/// <param name="control">Calling component</param>
		/// <param name="isChecked">New checked state</param>
		protected virtual void PropTreeCheckChanged(UIComponent control, bool isChecked)
		{
			// Don't do anything if we're ignoring events.
			if (ignorePropTreeCheckChanged)
			{
				return;
			}

			// Suspend event handling while processing.
			ignorePropTreeCheckChanged = true;

			if (control is UICheckBox thisCheck)
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
								propTreeChecks[i].isChecked = false;
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
			ignorePropTreeCheckChanged = false;
		}


		/// <summary>
		/// Loaded list sort button event handler.
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
		/// </summary>
		protected virtual void SortLoaded(UIComponent control, UIMouseEventParameter mouseEvent)
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
		/// Sets the states of the given sort button to match the given search status.
		/// </summary>
		/// <param name="activeButton">Currently active sort button</param>
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
		protected void SetTitle(string title)
        {
			// Create new title lablel if none already set.
			if (titleLabel == null)
			{
				titleLabel = AddUIComponent<UILabel>();
			}

			// Set text.
			titleLabel.text = title;
			titleLabel.relativePosition = new Vector2(50f, (TitleHeight - titleLabel.height) / 2f);
		}


		/// <summary>
		/// Adds an arrow button.
		/// </summary>
		/// <param name="parent">Parent component</param>
		/// <param name="posX">Relative X postion</param>
		/// <param name="posY">Relative Y position</param>
		/// <returns>New arrow button</returns>
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
		/// Performs initial fastlist setup.
		/// </summary>
		/// <param name="fastList">Fastlist to set up</param>
		protected void ListSetup(UIFastList fastList)
		{
			// Apperance, size and position.
			fastList.backgroundSprite = "UnlockingPanel";
			fastList.width = fastList.parent.width;
			fastList.height = fastList.parent.height;
			fastList.relativePosition = Vector2.zero;
			fastList.rowHeight = UIPropRow.RowHeight;

			// Behaviour.
			fastList.canSelect = true;
			fastList.autoHideScrollbar = true;

			// Data.
			fastList.rowsData = new FastList<object>();
			fastList.selectedIndex = -1;
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
