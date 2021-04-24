using System;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework.UI;


namespace BOB
{
	/// <summary>
	/// Panel to setup random props/trees.
	/// </summary>
	public class BOBRandomPanel : UIPanel
	{
		// Layout constants - general.
		private const float Margin = 5f;

		// Layout constants - X.
		private const float RandomizerX = Margin;
		private const float RandomizerWidth = 300f;
		private const float SelectedX = RandomizerX + RandomizerWidth + Margin;
		private const float SelectedWidth = 300f;
		private const float MiddleX = SelectedX + SelectedWidth + Margin;
		private const float ArrowWidth = 32;
		private const float MidControlX = MiddleX + Margin;
		private const float MiddleWidth = ArrowWidth + (Margin * 2f);
		private const float LoadedX = MiddleX + MiddleWidth;
		private const float LoadedWidth = 320f;
		private const float PanelWidth = LoadedX + LoadedWidth + Margin;
		private const float RandomButtonWidth = 100f;

		// Layout constants - Y.
		private const float TitleHeight = 40f;
		private const float ToolY = TitleHeight + Margin;
		private const float ToolHeight = 30f;
		private const float ListY = ToolY + ToolHeight + Margin;
		private const float ListHeight = UIPropRow.RowHeight * 16f;
		private const float PanelHeight = ListY + ListHeight + Margin;
		private const float ArrowHeight = 64f;



		// Instance references.
		private static GameObject uiGameObject;
		private static BOBRandomPanel panel;
		internal static BOBRandomPanel Panel => panel;

		// Panel components.
		private readonly UIFastList randomList, variationsList, loadedList;
		private readonly UICheckBox treeCheck, propCheck;

		// Current selections.
		private PrefabInfo selectedRandomPrefab, selectedVariation, selectedLoadedPrefab;
		private readonly List<PrefabInfo> currentVariations;


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
					uiGameObject = new GameObject("BOBRandomPanel");
					uiGameObject.transform.parent = UIView.GetAView().transform;

					// Create new panel instance and add it to GameObject.
					panel = uiGameObject.AddComponent<BOBRandomPanel>();
					panel.transform.parent = uiGameObject.transform.parent;
				}
			}
			catch (Exception e)
			{
				Logging.LogException(e, "exception creating random panel");
			}
		}


		/// <summary>
		/// Closes the panel by destroying the object (removing any ongoing UI overhead).
		/// </summary>
		internal static void Close()
		{
			// Don't do anything if no panel.
			if (panel == null)
			{
				return;
			}

			// Save configuration file.
			ConfigurationUtils.SaveConfig();

			/*
			// Need to do this for each building instance, so iterate through all buildings.
			Building[] buildings = BuildingManager.instance.m_buildings.m_buffer;
			for (ushort i = 0; i < buildings.Length; ++i)
			{
				// Local reference.
				Building building = buildings[i];

				// Check that this is a valid building in the dirty list.
				if (building.m_flags != Building.Flags.None)
				{
					// Match - update building render.
					BuildingManager.instance.UpdateBuildingRenderer(i, true);
				}
			}*/


			// Destroy game objects.
			GameObject.Destroy(panel);
			GameObject.Destroy(uiGameObject);

			// Let the garbage collector do its work (and also let us know that we've closed the object).
			panel = null;
			uiGameObject = null;
		}


		// Trees or props?
		private bool IsTree => treeCheck?.isChecked ?? false;
		

		/// <summary>
		/// Sets the currently selected loaded prefab.
		/// </summary>
		internal PrefabInfo SelectedLoadedPrefab { set => selectedLoadedPrefab = value; }


		/// <summary>
		/// Sets the currently selected random component.
		/// </summary>
		internal PrefabInfo SelectedVariation { set => selectedVariation = value; }


		/// <summary>
		/// Sets the currently selected random prefab.
		/// </summary>
		internal PrefabInfo SelectedRandomPrefab
        {
			set
            {
				// Don't do anything if no change.
				if (value == selectedRandomPrefab)
                {
					return;
                }
				
				// Set selection.
				selectedRandomPrefab = value;

				// Reset variation lists.
				selectedVariation = null;
				currentVariations.Clear();
				variationsList.selectedIndex = -1;

				// Generate component list to reflect new selection.
				if (selectedRandomPrefab is PropInfo randomProp && randomProp.m_variations != null)
                {
					// Prop- iterate through variations and add to list.
					for (int i = 0; i < randomProp.m_variations.Length; ++i)
                    {
						currentVariations.Add(randomProp.m_variations[i].m_finalProp);
                    }
				}
				else if (selectedRandomPrefab is TreeInfo randomTree && randomTree.m_variations != null)
				{
					// Tree - iterate through variations and add to list.
					for (int i = 0; i < randomTree.m_variations.Length; ++i)
					{
						currentVariations.Add(randomTree.m_variations[i].m_finalTree);
					}
				}

				// Regenerate variation UI fastlist.
				VariationsList();
			}
        }


		/// <summary>
		/// Constructor.
		/// </summary>
		internal BOBRandomPanel()
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
			titleLabel.text = Translations.Translate("BOB_NAM");
			titleLabel.relativePosition = new Vector2(50f, (TitleHeight - titleLabel.height) / 2f);

			// Close button.
			UIButton closeButton = AddUIComponent<UIButton>();
			closeButton.relativePosition = new Vector2(width - 35, 2);
			closeButton.normalBgSprite = "buttonclose";
			closeButton.hoveredBgSprite = "buttonclosehover";
			closeButton.pressedBgSprite = "buttonclosepressed";
			closeButton.eventClick += (component, clickEvent) => Close();

			// Selected random prop list.
			UIPanel randomizerPanel = AddUIComponent<UIPanel>();
			randomizerPanel.width = RandomizerWidth;
			randomizerPanel.height = ListHeight;
			randomizerPanel.relativePosition = new Vector2(RandomizerX, ListY);
			randomList = UIFastList.Create<UIRandomRefabRow>(randomizerPanel);
			ListSetup(randomList);

			// Variation selection list.
			UIPanel selectedPanel = AddUIComponent<UIPanel>();
			selectedPanel.width = SelectedWidth;
			selectedPanel.height = ListHeight;
			selectedPanel.relativePosition = new Vector2(SelectedX, ListY);
			variationsList = UIFastList.Create<UIRandomComponentRow>(selectedPanel);
			ListSetup(variationsList);

			// Initialize curren variations list.
			currentVariations = new List<PrefabInfo>();

			// Loaded prop list.
			UIPanel loadedPanel = AddUIComponent<UIPanel>();
			loadedPanel.width = LoadedWidth;
			loadedPanel.height = ListHeight;
			loadedPanel.relativePosition = new Vector2(LoadedX, ListY);
			loadedList = UIFastList.Create<UILoadedRandomPropRow>(loadedPanel);
			ListSetup(loadedList);

			// Add variation button.
			UIButton addVariationButton = ArrowButton(this, MidControlX, ListY, "ArrowLeft");
			addVariationButton.eventClicked += AddVariation;

			// Remove variation button.
			UIButton removeVariationButton = ArrowButton(this, MidControlX, ListY + ArrowHeight + Margin, "ArrowRight");
			removeVariationButton.eventClicked += RemoveVariation;

			// Add random prefab button.
			UIButton addRandomButton = UIControls.AddButton(this, Margin, ToolY, "Add", RandomButtonWidth);
			addRandomButton.eventClicked += NewRandomPrefab;

			// Remove random prefab button.
			UIButton removeRandomButton = UIControls.AddButton(this, RandomButtonWidth + (Margin * 2f), ToolY, "Remove", RandomButtonWidth);
			removeRandomButton.eventClicked += RemoveRandomPrefab;

			// Tree/prop checks.
			propCheck = IconToggleCheck(this, (RandomButtonWidth * 2f) + (Margin * 3f), ToolY, "bob_props3", "BOB_PNL_PRP");
			treeCheck = IconToggleCheck(this, propCheck.relativePosition.x + propCheck.width, ToolY, "bob_trees_small", "BOB_PNL_TRE");
			propCheck.isChecked = true;
			propCheck.eventCheckChanged += PropCheckChanged;
			treeCheck.eventCheckChanged += TreeCheckChanged;

			// Populate loaded lists.
			RandomList();
			LoadedList();

			// Bring to front.
			BringToFront();
		}


		/// <summary>
		/// Prop check event handler.
		/// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="isChecked">New checked state</param>
		private void PropCheckChanged(UIComponent control, bool isChecked)
		{
			if (isChecked)
			{
				// Props are now selected - unset tree check.
				treeCheck.isChecked = false;

				// Reset current items.
				SelectedRandomPrefab = null;

				// Set loaded lists to 'props'.
				RandomList();
				LoadedList();
			}
			else
			{
				// Props are now unselected - set tree check if it isn't already (letting tree check event handler do the work required).
				if (!treeCheck.isChecked)
				{
					treeCheck.isChecked = true;
				}
			}
		}


		/// <summary>
		/// Tree check event handler.
		/// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="isChecked">New checked state</param>
		private void TreeCheckChanged(UIComponent control, bool isChecked)
		{
			if (isChecked)
			{
				// Trees are now selected - unset prop check.
				propCheck.isChecked = false;

				// Reset current items.
				SelectedRandomPrefab = null;

				// Set loaded lists to 'trees'.
				RandomList();
				LoadedList();
			}
			else
			{
				// Trees are now unselected - set prop check if it isn't already (letting prop check event handler do the work required).
				if (!propCheck.isChecked)
				{
					propCheck.isChecked = true;
				}
			}
		}


		/// <summary>
		/// Create new random prefab event handler.
		/// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="clickEvent">Mouse event parameter (unused)</param>
		private void NewRandomPrefab(UIComponent control, UIMouseEventParameter clickEvent)
        {
			// New prefab record.
			PrefabInfo newPrefab;

			// Name conflict deteciton.
			int existingCount = 1;

			// Trees or props?
			if (IsTree)
			{
				// Trees - generate unique name.
				string treeNameBase = "BOB random tree";
				string treeName = treeNameBase + " 1";

				// Interate through existing names, incrementing post numeral until we've got a unique name.
				TreeInfo existingTree = null;
				do
				{
					existingTree = PrefabLists.randomTrees.Find(x => x.name.Equals(treeName));
					if (existingTree != null)
					{
						treeName = treeNameBase + " " + (++existingCount).ToString();
					}
				}
				while (existingTree != null);
				
				// Create new random tree prefab.
				newPrefab = PrefabLists.NewRandomTree(treeName);
			}
			else
			{
				// Props - generate unique name.
				string propNameBase = "BOB random prop";
				string propName = propNameBase + " 1";

				// Interate through existing names, incrementing post numeral until we've got a unique name.
				PropInfo existingProp = null;
				do
				{
					existingProp = PrefabLists.randomProps.Find(x => x.name.Equals(propName));
					if (existingProp != null)
					{
						propName = propNameBase + " " + (++existingCount).ToString();
					}
				}
				while (existingProp != null);

				// Create new random tree prefab.
				newPrefab = PrefabLists.NewRandomProp(propName);
			}

			// Did we succesfully create a new prefab?
			if (newPrefab != null)
			{
				// Yes - regenerate random list to reflect the change, and select the new item.
				RandomList();
				randomList.FindItem(newPrefab);
				SelectedRandomPrefab = newPrefab;
			}
		}


		/// <summary>
		/// Remove random prefab event handler.
		/// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="clickEvent">Mouse event parameter (unused)</param>
		private void RemoveRandomPrefab(UIComponent control, UIMouseEventParameter clickEvent)
		{
			// Safety checks.
			if (randomList.selectedIndex < 0 || selectedRandomPrefab == null)
            {
				return;
            }

			// Remove tree or prop from relevant list of random prefabs.
			if (selectedRandomPrefab is TreeInfo randomTree)
			{
				PrefabLists.randomTrees.Remove(randomTree);
			}
			else if (selectedRandomPrefab is PropInfo randomProp)
			{
				PrefabLists.randomProps.Remove(randomProp);
			}

			// Reset selection and regenerate UI fastlist.
			selectedRandomPrefab = null;
			randomList.selectedIndex = -1;
			RandomList();
		}


		/// <summary>
		/// Add variation event handler.
		/// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="clickEvent">Mouse event parameter (unused)</param>
		private void AddVariation(UIComponent control, UIMouseEventParameter clickEvent)
		{
			// Make sure we've got a valid selection first.
			if (selectedLoadedPrefab == null || selectedRandomPrefab == null)
			{
				return;
			}

			// Add selected prefab to list of variations and regenerate UI fastlist.
			currentVariations.Add(selectedLoadedPrefab);
			VariationsList();

			// Select variation.
			variationsList.FindItem(selectedLoadedPrefab);
			selectedVariation = selectedLoadedPrefab;

			// Update the random prefab with the new variation.
			UpdateCurrentRandomPrefab();
		}


		/// <summary>
		/// Remove variation event handler.
		/// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="clickEvent">Mouse event parameter (unused)</param>
		private void RemoveVariation(UIComponent control, UIMouseEventParameter clickEvent)
		{
			// Make sure we've got a valid selection first.
			if (selectedVariation == null)
			{
				return;
			}

			// Remove selected prefab from list of variations and regenerate UI fastlist.
			currentVariations.Remove(selectedVariation);
			VariationsList();

			// Update the random prefab to reflect the removal
			UpdateCurrentRandomPrefab();
		}


		/// <summary>
		/// Updates the variations for the current random prefab.
		/// </summary>
		private void UpdateCurrentRandomPrefab()
		{
			int variationCount = currentVariations.Count;

			// Trees or props?
			if (selectedRandomPrefab is TreeInfo randomTree)
			{
				// Trees - create new variations array.
				randomTree.m_variations = new TreeInfo.Variation[variationCount];

				// Iterate through current variations list and add to prefab variation list.
				for (int i = 0; i < variationCount; ++i)
				{
					randomTree.m_variations[i] = new TreeInfo.Variation()
					{
						m_finalTree = currentVariations[i] as TreeInfo,
						m_probability = 100 / variationCount
					};
				}

			}
			else if (selectedRandomPrefab is PropInfo randomProp)
			{
				// Props - create new variations array.
				randomProp.m_variations = new PropInfo.Variation[variationCount];

				// Iterate through current variations list and add to prefab variation list.
				for (int i = 0; i < variationCount; ++i)
				{
					randomProp.m_variations[i] = new PropInfo.Variation()
					{
						m_finalProp = currentVariations[i] as PropInfo,
						m_probability = 100 / variationCount
					};
				}
			}
		}


		/// <summary>
		/// Regenerates the random prefab UI fastlist.
		/// </summary>
		private void RandomList()
        {
			// Trees or props?
			if (IsTree)
			{
				// Trees.
				randomList.m_rowsData = new FastList<object>
				{
					m_buffer = PrefabLists.randomTrees.ToArray(),
					m_size = PrefabLists.randomTrees.Count
				};
			}
			else
			{
				// Props.
				randomList.m_rowsData = new FastList<object>
				{
					m_buffer = PrefabLists.randomProps.ToArray(),
					m_size = PrefabLists.randomProps.Count
				};
			}

			randomList.selectedIndex = -1;

			randomList.Refresh();
        }


		/// <summary>
		/// Regenerates the variations UI fastlist.
		/// </summary>
		private void VariationsList()
		{
			// Create return fastlist from our filtered list.
			variationsList.rowsData = new FastList<object>
			{
				m_buffer = currentVariations.ToArray(),
				m_size = currentVariations.Count
			};
		}


		/// <summary>
		/// Populates a fastlist with a filtered list of loaded trees or props.
		/// </summary>
		private void LoadedList()
		{
			// List of prefabs that have passed filtering.
			List<PrefabInfo> list = new List<PrefabInfo>();

			if (IsTree)
			{
				// Tree - iterate through each prop in our list of loaded prefabs.
				for (int i = 0; i < PrefabLists.loadedTrees.Length; ++i)
				{
					TreeInfo loadedTree = PrefabLists.loadedTrees[i];
					// Add this prefab to our list.
					list.Add(loadedTree);
				}
			}
			else
			{
				// Prop - iterate through each prop in our list of loaded prefabs.
				for (int i = 0; i < PrefabLists.loadedProps.Length; ++i)
				{
					PropInfo loadedProp = PrefabLists.loadedProps[i];

					// Skip any props that require height or water maps.
					if (loadedProp.m_requireHeightMap || loadedProp.m_requireWaterMap)
					{
						continue;
					}

					// Add this prefab to our list.
					list.Add(loadedProp);
				}
			}

			// Create return fastlist from our filtered list.
			loadedList.rowsData = new FastList<object>
			{
				m_buffer = list.ToArray(),
				m_size = list.Count
			};
		}


		/// <summary>
		/// Performs initial fastlist setup.
		/// </summary>
		/// <param name="fastList">Fastlist to set up</param>
		private void ListSetup(UIFastList fastList)
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
		private UICheckBox IconToggleCheck(UIComponent parent, float xPos, float yPos, string atlasName, string tooltipKey)
		{
			const float ToggleSpriteSize = 32f;

			// Size and position.
			UICheckBox checkBox = parent.AddUIComponent<UICheckBox>();
			checkBox.width = ToggleSpriteSize;
			checkBox.height = ToggleSpriteSize;
			checkBox.clipChildren = true;
			checkBox.relativePosition = new Vector2(xPos, yPos);

			// Checkbox sprites.
			UISprite sprite = checkBox.AddUIComponent<UISprite>();
			sprite.atlas = TextureUtils.LoadSpriteAtlas(atlasName);
			sprite.spriteName = "disabled";
			sprite.size = new Vector2(ToggleSpriteSize, ToggleSpriteSize);
			sprite.relativePosition = Vector3.zero;

			checkBox.checkedBoxObject = sprite.AddUIComponent<UISprite>();
			((UISprite)checkBox.checkedBoxObject).atlas = TextureUtils.LoadSpriteAtlas(atlasName);
			((UISprite)checkBox.checkedBoxObject).spriteName = "pressed";
			checkBox.checkedBoxObject.size = new Vector2(ToggleSpriteSize, ToggleSpriteSize);
			checkBox.checkedBoxObject.relativePosition = Vector3.zero;

			checkBox.tooltip = Translations.Translate(tooltipKey);

			return checkBox;
		}


		/// <summary>
		/// Adds an arrow button.
		/// </summary>
		/// <param name="parent">Parent component</param>
		/// <param name="posX">Relative X postion</param>
		/// <param name="posY">Relative Y position</param>
		/// <param name="spriteName">Sprite name</param>
		/// <returns>New arrow button</returns>
		private UIButton ArrowButton(UIComponent parent, float posX, float posY, string spriteName)
		{
			UIButton arrowButton = parent.AddUIComponent<UIButton>();

			// Size and position.
			arrowButton.size = new Vector2(ArrowWidth, ArrowHeight);
			arrowButton.relativePosition = new Vector2(posX, posY);

			// Appearance.
			arrowButton.atlas = TextureUtils.InGameAtlas;
			arrowButton.normalFgSprite = spriteName;
			arrowButton.focusedFgSprite = spriteName + "Focused";
			arrowButton.disabledFgSprite = spriteName + "Disabled";
			arrowButton.hoveredFgSprite = spriteName + "Hovered";
			arrowButton.pressedFgSprite = spriteName + "Pressed";

			return arrowButton;
		}
	}
}
