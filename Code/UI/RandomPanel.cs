using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;


namespace BOB
{
	/// <summary>
	/// Panel to setup random props/trees.
	/// </summary>
	internal class BOBRandomPanel : BOBPanelBase
	{
		// Layout constants - X.
		private const float RandomizerX = Margin;
		private const float RandomizerWidth = 300f;
		private const float SelectedX = RandomizerX + RandomizerWidth + Margin;
		private const float SelectedWidth = 300f;
		private const float MiddleX = SelectedX + SelectedWidth + Margin;
		private const float ArrowWidth = ToggleSize;
		private const float MidControlX = MiddleX + Margin;
		private const float MiddleWidth = ArrowWidth + (Margin * 2f);
		private const float LoadedX = MiddleX + MiddleWidth;
		private const float LoadedWidth = 320f;
		private const float RandomButtonWidth = 100f;
		private const float RandomButtonX = Margin + RandomizerWidth - RandomButtonWidth;

		// Layout constants - Y.
		private const float ToolY = TitleHeight + Margin;
		private const float ListY = ToolY + ToolbarHeight + Margin;
		private const float ListHeight = UIPropRow.RowHeight * 16f;
		private const float LeftListY = ListY + 64f;
		private const float LeftListHeight = ListHeight - 64f;
		private const float RandomButtonY = LeftListY - ToggleSize - Margin;
		private const float NameFieldY = RandomButtonY - 22f - Margin;


		// Instance references.
		private static GameObject uiGameObject;
		private static BOBRandomPanel panel;
		internal static BOBRandomPanel Panel => panel;

		// Panel components.
		private readonly UIFastList randomList, variationsList, loadedList;
		private readonly UIButton removeRandomButton, renameButton;
		private readonly UITextField nameField;
		private readonly BOBSlider probSlider;
		private readonly PreviewPanel previewPanel;

		// Current selections.
		private BOBRandomPrefab selectedRandomPrefab;
		private PrefabInfo _selectedLoadedPrefab;
		private BOBVariation selectedVariation;
		private BOBVariation lastChangedVariant;
		private bool ignoreValueChange = false;


		// Panel width.
		protected override float PanelWidth => LoadedX + LoadedWidth + Margin;

		// Panel height.
		protected override float PanelHeight => ListY + ListHeight + Margin;

		// Panel opacity.
		protected override float PanelOpacity => 1f;


		/// <summary>
		/// Initial tree/prop checked state.
		/// </summary>
		protected override bool InitialTreeCheckedState => false;


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

					// Hide previous window, if any.
					InfoPanelManager.Panel?.Hide();
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

			// Show previous window, if any.
			InfoPanelManager.Panel?.Show();
		}


		// Trees or props?
		private bool IsTree => treeCheck?.isChecked ?? false;
		

		/// <summary>
		/// Sets the currently selected loaded prefab.
		/// </summary>
		internal PrefabInfo SelectedLoadedPrefab
		{
			private get => _selectedLoadedPrefab;

			set
			{
				_selectedLoadedPrefab = value;
				previewPanel.SetTarget(value);
			}
		}


		/// <summary>
		/// Sets the currently selected random component.
		/// </summary>
		internal BOBVariation SelectedVariation
		{
			set
			{
				selectedVariation = value;

				// Disable events while updating value.
				ignoreValueChange = true;
				probSlider.value = value.probability;
				ignoreValueChange = false;
			}
		}


		/// <summary>
		/// Sets the currently selected random prefab.
		/// </summary>
		internal BOBRandomPrefab SelectedRandomPrefab
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
				variationsList.selectedIndex = -1;

				// Regenerate variation UI fastlist.
				VariationsList();

				// Update name text.
				nameField.text = selectedRandomPrefab?.name ?? "";

				// Update button states.
				UpdateButtonStates();
			}
        }


		/// <summary>
		/// Constructor.
		/// </summary>
		internal BOBRandomPanel()
		{
			// Default position - centre in screen.
			relativePosition = new Vector2(Mathf.Floor((GetUIView().fixedWidth - width) / 2), Mathf.Floor((GetUIView().fixedHeight - height) / 2));

			// Title label.
			SetTitle(Translations.Translate("BOB_NAM") + " : " + Translations.Translate("BOB_RND_TIT"));

			// Selected random prop list.
			UIPanel randomizerPanel = AddUIComponent<UIPanel>();
			randomizerPanel.width = RandomizerWidth;
			randomizerPanel.height = LeftListHeight;
			randomizerPanel.relativePosition = new Vector2(RandomizerX, LeftListY);
			randomList = UIFastList.Create<UIRandomRefabRow>(randomizerPanel);
			ListSetup(randomList);

			// Variation selection list.
			UIPanel selectedPanel = AddUIComponent<UIPanel>();
			selectedPanel.width = SelectedWidth;
			selectedPanel.height = ListHeight;
			selectedPanel.relativePosition = new Vector2(SelectedX, ListY);
			variationsList = UIFastList.Create<UIRandomComponentRow>(selectedPanel);
			ListSetup(variationsList);

			// Loaded prop list.
			UIPanel loadedPanel = AddUIComponent<UIPanel>();
			loadedPanel.width = LoadedWidth;
			loadedPanel.height = ListHeight;
			loadedPanel.relativePosition = new Vector2(LoadedX, ListY);
			loadedList = UIFastList.Create<UILoadedRandomPropRow>(loadedPanel);
			ListSetup(loadedList);

			// Name change textfield.
			nameField = UIControls.AddTextField(this, Margin, NameFieldY, RandomizerWidth);

			// Add random prefab button.
			UIButton addRandomButton = AddIconButton(this, Margin, RandomButtonY, ToggleSize, "BOB_RND_NEW", TextureUtils.LoadSpriteAtlas("bob_buttons_plus_round_small"));
			addRandomButton.eventClicked += NewRandomPrefab;

			// Remove random prefab button.
			removeRandomButton = AddIconButton(this, addRandomButton.relativePosition.x + ToggleSize, RandomButtonY, ToggleSize, "BOB_RND_DEL", TextureUtils.LoadSpriteAtlas("bob_buttons_minus_round_small"));
			removeRandomButton.eventClicked += RemoveRandomPrefab;

			// Rename button.
			renameButton = UIControls.EvenSmallerButton(this, RandomButtonX, RandomButtonY, Translations.Translate("BOB_RND_REN"), RandomButtonWidth);
			renameButton.eventClicked += RenameRandomPrefab;

			// Add variation button.
			UIButton addVariationButton = AddIconButton(this, MidControlX, ListY, ToggleSize, "BOB_RND_ADD", TextureUtils.LoadSpriteAtlas("bob_plus"));
			addVariationButton.eventClicked += AddVariation;

			// Remove variation button.
			UIButton removeVariationButton = AddIconButton(this, MidControlX, ListY + ToggleSize, ToggleSize, "BOB_RND_SUB", TextureUtils.LoadSpriteAtlas("bob_minus"));
			removeVariationButton.eventClicked += RemoveVariation;

			// Order button.
			loadedNameButton = ArrowButton(this, LoadedX + 10f, ListY - 20f);
			loadedNameButton.eventClicked += SortLoaded;
	
			// Probability slider.
			probSlider = AddBOBSlider(this, SelectedX + Margin, ToolY + Margin, SelectedWidth - (Margin * 2f), "BOB_PNL_PRB", 0, 100, 1, "Probability");
			probSlider.eventValueChanged += (control, value) =>
			{
				if (selectedVariation != null)
                {
					selectedVariation.probability = (int)value;
					lastChangedVariant = selectedVariation;

					if (!ignoreValueChange)
					{
						UpdateCurrentRandomPrefab();
					}

					variationsList.Refresh();
					ConfigurationUtils.SaveConfig();
                }
			};

			// Default is name ascending.
			SetFgSprites(loadedNameButton, "IconUpArrow2");

			// Preview image.
			previewPanel = AddUIComponent<PreviewPanel>();
			previewPanel.relativePosition = new Vector2(this.width + Margin, ListY);

			// Populate loaded lists.
			RandomList();
			LoadedList();

			// Update button states.
			UpdateButtonStates();

			// Bring to front.
			BringToFront();
		}


		/// <summary>
		/// Close button event handler.
		/// </summary>
		protected override void CloseEvent() => Close();


		/// <summary>
		/// Populates a fastlist with a filtered list of loaded trees or props.
		/// </summary>
		protected override void LoadedList()
		{
			// List of prefabs that have passed filtering.
			List<PrefabInfo> list = new List<PrefabInfo>();

			bool nameFilterActive = !nameFilter.text.IsNullOrWhiteSpace();

			if (IsTree)
			{
				// Tree - iterate through each prop in our list of loaded prefabs.
				for (int i = 0; i < PrefabLists.loadedTrees.Length; ++i)
				{
					TreeInfo loadedTree = PrefabLists.loadedTrees[i];

					// Set display name.
					string displayName = PrefabLists.GetDisplayName(loadedTree);

					// Apply vanilla filtering if selected.
					if (!hideVanilla.isChecked || !displayName.StartsWith("[v]"))
					{
						// Apply name filter.
						if (!nameFilterActive || displayName.ToLower().Contains(nameFilter.text.Trim().ToLower()))
						{
							// Filtering passed - add this prefab to our list.
							list.Add(loadedTree);
						}
					}
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

					// Set display name.
					string displayName = PrefabLists.GetDisplayName(loadedProp);

					// Apply vanilla filtering if selected.
					if (!hideVanilla.isChecked || !displayName.StartsWith("[v]"))
					{
						// Apply name filter.
						if (!nameFilterActive || displayName.ToLower().Contains(nameFilter.text.Trim().ToLower()))
						{
							// Filtering passed - add this prefab to our list.
							list.Add(loadedProp);
						}
					}
				}
			}

			// Master lists should already be sorted by display name so no need to sort again here.
			// Reverse order of filtered list if we're searching name descending.
			if (loadedSearchStatus == (int)OrderBy.NameDescending)
			{
				list.Reverse();
			}

			// Create return fastlist from our filtered list.
			loadedList.rowsData = new FastList<object>
			{
				m_buffer = list.ToArray(),
				m_size = list.Count
			};
		}


		/// <summary>
		/// Updates button states (enabled/disabled) according to current control states.
		/// </summary>
		private void UpdateButtonStates()
		{
			// Toggle enabled state based on whether or not there's a valid current selection.
			bool buttonState = selectedRandomPrefab != null;
			removeRandomButton.isEnabled = buttonState;
			renameButton.isEnabled = buttonState;
		}


		/// <summary>
		/// Prop check event handler.
		/// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="isChecked">New checked state</param>
		protected override void PropCheckChanged(UIComponent control, bool isChecked)
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
		protected override void TreeCheckChanged(UIComponent control, bool isChecked)
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
			BOBRandomPrefab newPrefab;

			// Name conflict deteciton.
			int existingCount = 1;

			// Trees or props?
			if (IsTree)
			{
				// Trees - generate unique name.
				string treeNameBase = "BOB random tree";
				string treeName = treeNameBase + " 1";

				// Interate through existing names, incrementing post numeral until we've got a unique name.
				BOBRandomPrefab existingTree = null;
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
				Logging.Message("creating new random tree");
				newPrefab = PrefabLists.NewRandomTree(treeName);
			}
			else
			{
				Logging.Message("creating new prop");

				// Props - generate unique name.
				string propNameBase = "BOB random prop";
				string propName = propNameBase + " 1";

				// Interate through existing names, incrementing post numeral until we've got a unique name.10
				Logging.Message("checking new prop name");
				BOBRandomPrefab existingProp = null;
				do
				{
					existingProp = PrefabLists.randomProps.Find(x => x.name.Equals(propName));
					if (existingProp != null)
					{
						propName = propNameBase + " " + (++existingCount).ToString();
					}
				}
				while (existingProp != null);

				// Create new random prop prefab.
				Logging.Message("creating new random prop");
				newPrefab = PrefabLists.NewRandomProp(propName);
			}

			// Did we succesfully create a new prefab?
			if (newPrefab != null)
			{
				Logging.Message("Trying to find item");

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
			if (selectedRandomPrefab.tree != null)
			{
				PrefabLists.RemoveRandomTree(selectedRandomPrefab.tree);
			}
			else if (selectedRandomPrefab.prop != null)
			{
				PrefabLists.RemoveRandomProp(selectedRandomPrefab.prop);
			}

			// Reset selection and regenerate UI fastlist.
			SelectedRandomPrefab = null;
			randomList.selectedIndex = -1;
			RandomList();
		}


		/// <summary>
		/// Rename random prefab event handler.
		/// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="clickEvent">Mouse event parameter (unused)</param>
		private void RenameRandomPrefab(UIComponent control, UIMouseEventParameter clickEvent)
		{
			// Safety checks.
			if (randomList.selectedIndex < 0 || selectedRandomPrefab == null || nameField.text.IsNullOrWhiteSpace())
			{
				return;
			}

			string trimmedName = nameField.text.Trim();

			// Need unique name.
			if ((selectedRandomPrefab.prop != null & PrefabLists.DuplicatePropName(trimmedName)) || (selectedRandomPrefab.tree != null && PrefabLists.DuplicateTreeName(trimmedName)))
			{
				Logging.Error("duplicate name");
				return;
			}

			// If we got here, all good; rename random prop reference and PrefabInfo.
			selectedRandomPrefab.name = trimmedName;
			if (selectedRandomPrefab.prop != null)
            {
				selectedRandomPrefab.prop.name = selectedRandomPrefab.name;
			}
			if (selectedRandomPrefab.tree != null)
			{
				selectedRandomPrefab.tree.name = selectedRandomPrefab.name;
			}

			// Regenerate list.
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
			if (SelectedLoadedPrefab == null || selectedRandomPrefab == null)
			{
				return;
			}

			// Add selected prefab to list of variations and regenerate UI fastlist.
			BOBVariation newVariant = new BOBVariation { name = SelectedLoadedPrefab.name, prefab = SelectedLoadedPrefab, probability = 0 };
			selectedRandomPrefab.variations.Add(newVariant);
			VariationsList();

			// Select variation.
			variationsList.FindItem(newVariant);
			selectedVariation = newVariant;

			// Update the random prefab with the new variation.
			UpdateCurrentRandomPrefab();

			// Update slider value.
			probSlider.value = newVariant.probability;
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
			selectedRandomPrefab.variations.Remove(selectedVariation);
			VariationsList();

			// Update the random prefab to reflect the removal
			UpdateCurrentRandomPrefab();
		}


		/// <summary>
		/// Updates the variations for the current random prefab.
		/// </summary>
		private void UpdateCurrentRandomPrefab()
		{
			int variationCount = selectedRandomPrefab.variations.Count;

			// Recalculate probabilities.
			RecalculateProbabilities();

			// Trees or props?
			if (selectedRandomPrefab.tree != null)
			{
				// Trees - create new variations array.
				selectedRandomPrefab.tree.m_variations = new TreeInfo.Variation[variationCount];

				// Iterate through current variations list and add to prefab variation list.
				for (int i = 0; i < variationCount; ++i)
				{
					selectedRandomPrefab.tree.m_variations[i] = new TreeInfo.Variation()
					{
						m_finalTree = selectedRandomPrefab.variations[i].prefab as TreeInfo,
						m_probability = 100 / variationCount
					};
				}

			}
			else if (selectedRandomPrefab.prop != null)
			{
				// Props - create new variations array.
				selectedRandomPrefab.prop.m_variations = new PropInfo.Variation[variationCount];

				// Iterate through current variations list and add to prefab variation list.
				for (int i = 0; i < variationCount; ++i)
				{
					selectedRandomPrefab.prop.m_variations[i] = new PropInfo.Variation()
					{
						m_finalProp = selectedRandomPrefab.variations[i].prefab as PropInfo,
						m_probability = selectedRandomPrefab.variations[i].probability
					};
				}
			}
		}


		/// <summary>
		/// Recalculates the current component probabilities.
		/// </summary>
		private void RecalculateProbabilities()
		{
			Logging.Message("recalculating probabilities");

			// Get last changed variant - ignore if locked.
			bool validLastChanged = lastChangedVariant != null;

			// Number of locked and unlocked entries and their summative probabilities.
			int lockedProbs = 0, unlockedProbs = 0, lockedCount = 0, unlockedCount = 0;

			// Iterate through all current variations, identifying locked probabilties.
			for (int i = 0; i < selectedRandomPrefab.variations.Count; ++i)
			{
				// Ignore last changed variant.
				if (!validLastChanged || (validLastChanged && selectedRandomPrefab.variations[i] != lastChangedVariant))
				{
					// If this variation has a locked probability, add the probability to the total locked percentage and increment the locked counter - ignoring most recently changed item.
					if (selectedRandomPrefab.variations[i].probLocked)
					{
						lockedProbs += selectedRandomPrefab.variations[i].probability;
						++lockedCount;
					}
					else
					{
						// Unlocked.
						unlockedProbs += selectedRandomPrefab.variations[i].probability;
						++unlockedCount;
					}
				}
			}

			// Get the probability of the most recently changed probability.
			int changedProb = validLastChanged ? lastChangedVariant.probability : 0;

			// If total probs are more than 100, reduce unlocked probs to fit.
			/*if (lockedProbs + unlockedProbs + changedProb > 100)
			{
				// Assign unlocked probabilities, except to most recently changed item.
				int remainderProb = 100 - lockedProbs - changedProb;
				for (int i = 0; i < selectedRandomPrefab.variations.Count; ++i)
				{
					if (!selectedRandomPrefab.variations[i].probLocked && (!validLastChanged || selectedRandomPrefab.variations[i] != lastChangedVariant))
					{
						// Minimum probability of one; decrement remaining count and recalculate remaining probabilities as we go, to avoid rounding errors.
						int thisProb = Math.Max(1, remainderProb / unlockedCount--);
						selectedRandomPrefab.variations[i].probability = thisProb;
						remainderProb -= thisProb;

						// Abort if for some reason unlockedCount is zero.
						if (unlockedCount == 0)
						{
							break;
						}
					}
				}

				// Now, review probabilities that we've assigned.
				int residualProb = 100;
				for (int i = 0; i < selectedRandomPrefab.variations.Count; ++i)
				{
					residualProb -= selectedRandomPrefab.variations[i].probability;
				}

				// Change the 'last changed' variant if we need to to keep total probability to 100.
				if (residualProb < 1 && lastChangedVariant != null)
				{
					lastChangedVariant.probability += (residualProb - 1);
				}
				
				// Update probability slider.
				if (selectedVariation != null)
				{
					probSlider.value = selectedVariation.probability;
				}
			}*/

			// Cap changed probability slider amount.
			changedProb = Mathf.Clamp(changedProb, 1, 100 - lockedProbs - unlockedCount);
			if (validLastChanged)
			{
				lastChangedVariant.probability = changedProb;
			}

			// Now, assign probabilities.
			int residualProb = Mathf.Max(0, 100 - lockedProbs - changedProb);
			for (int i = 0; i < selectedRandomPrefab.variations.Count; ++i)
			{
				// Ignore last changed variant.
				if (!validLastChanged || (validLastChanged && selectedRandomPrefab.variations[i] != lastChangedVariant))
				{
					if (!selectedRandomPrefab.variations[i].probLocked)
					{
						selectedRandomPrefab.variations[i].probability = Mathf.Max(1, residualProb / unlockedCount--);
						residualProb -= selectedRandomPrefab.variations[i].probability;
					}
				}
			}

			// Update probability slider.
			if (selectedVariation != null)
			{
				probSlider.value = selectedVariation.probability;
			}

			// Regenerate list.
			variationsList.Refresh();
		}
		


		/// <summary>
		/// Regenerates the random prefab UI fastlist.
		/// </summary>
		private void RandomList()
		{
			// Remove selection.
			randomList.selectedIndex = -1;

			// Trees or props?
			if (IsTree)
			{
				// Trees.
				randomList.rowsData = new FastList<object>
				{
					m_buffer = PrefabLists.randomTrees.OrderBy(x => x.name.ToLower()).ToArray(),
					m_size = PrefabLists.randomTrees.Count
				};
			}
			else
			{
				// Props.
				randomList.rowsData = new FastList<object>
				{
					m_buffer = PrefabLists.randomProps.OrderBy(x => x.name.ToLower()).ToArray(),
					m_size = PrefabLists.randomProps.Count
				};
			}
		}


		/// <summary>
		/// Regenerates the variations UI fastlist.
		/// </summary>
		private void VariationsList()
		{
			// Remove selection.
			variationsList.selectedIndex = -1;

			// Create return fastlist from our filtered list.
			variationsList.rowsData = new FastList<object>
			{
				m_buffer = selectedRandomPrefab?.variations?.OrderBy(x => PrefabLists.GetDisplayName(x.name).ToLower()).ToArray(),
				m_size = selectedRandomPrefab?.variations?.Count ?? 0
			};
		}
	}
}
