// <copyright file="BOBRandomPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AlgernonCommons;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Panel to setup random props/trees.
    /// </summary>
    internal sealed class BOBRandomPanel : BOBPanelBase
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
        private static GameObject s_gameObject;
        private static BOBRandomPanel s_panel;

        // Panel components.
        private readonly UIFastList _randomList;
        private readonly UIFastList _variationsList;
        private readonly UIFastList _loadedList;
        private readonly UIButton _removeRandomButton;
        private readonly UIButton _renameButton;
        private readonly UITextField _nameField;
        private readonly BOBSlider _probSlider;
        private readonly PreviewPanel _previewPanel;

        // Current selections.
        private BOBRandomPrefab _selectedRandomPrefab;
        private PrefabInfo _selectedLoadedPrefab;
        private BOBRandomPrefab.Variation _selectedVariation;
        private BOBRandomPrefab.Variation _lastChangedVariant;
        private bool _ignoreValueChange = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="BOBRandomPanel"/> class.
        /// </summary>
        internal BOBRandomPanel()
        {
            // Default position - centre in screen.
            relativePosition = new Vector2(Mathf.Floor((GetUIView().fixedWidth - width) / 2), Mathf.Floor((GetUIView().fixedHeight - height) / 2));

            // Title label.
            SetTitle(Translations.Translate("BOB_NAM") + " : " + Translations.Translate("BOB_RND_TIT"));

            // Disable 'both' check.
            m_propTreeChecks[(int)PropTreeModes.Both].Disable();
            m_propTreeChecks[(int)PropTreeModes.Both].Hide();

            // Selected random prop list.
            UIPanel randomizerPanel = AddUIComponent<UIPanel>();
            randomizerPanel.width = RandomizerWidth;
            randomizerPanel.height = LeftListHeight;
            randomizerPanel.relativePosition = new Vector2(RandomizerX, LeftListY);
            _randomList = UIFastList.Create<UIRandomRefabRow>(randomizerPanel);
            ListSetup(_randomList);

            // Variation selection list.
            UIPanel selectedPanel = AddUIComponent<UIPanel>();
            selectedPanel.width = SelectedWidth;
            selectedPanel.height = ListHeight;
            selectedPanel.relativePosition = new Vector2(SelectedX, ListY);
            _variationsList = UIFastList.Create<UIRandomComponentRow>(selectedPanel);
            ListSetup(_variationsList);

            // Loaded prop list.
            UIPanel loadedPanel = AddUIComponent<UIPanel>();
            loadedPanel.width = LoadedWidth;
            loadedPanel.height = ListHeight;
            loadedPanel.relativePosition = new Vector2(LoadedX, ListY);
            _loadedList = UIFastList.Create<UILoadedRandomPropRow>(loadedPanel);
            ListSetup(_loadedList);

            // Name change textfield.
            _nameField = UITextFields.AddTextField(this, Margin, NameFieldY, RandomizerWidth);

            // Add random prefab button.
            UIButton addRandomButton = AddIconButton(this, Margin, RandomButtonY, ToggleSize, "BOB_RND_NEW", UITextures.LoadQuadSpriteAtlas("BOB-RoundPlus"));
            addRandomButton.eventClicked += NewRandomPrefab;

            // Remove random prefab button.
            _removeRandomButton = AddIconButton(this, addRandomButton.relativePosition.x + ToggleSize, RandomButtonY, ToggleSize, "BOB_RND_DEL", UITextures.LoadQuadSpriteAtlas("BOB-RoundMinus"));
            _removeRandomButton.eventClicked += RemoveRandomPrefab;

            // Rename button.
            _renameButton = UIButtons.AddEvenSmallerButton(this, RandomButtonX, RandomButtonY, Translations.Translate("BOB_RND_REN"), RandomButtonWidth);
            _renameButton.eventClicked += RenameRandomPrefab;

            // Add variation button.
            UIButton addVariationButton = AddIconButton(this, MidControlX, ListY, ToggleSize, "BOB_RND_ADD", UITextures.LoadQuadSpriteAtlas("BOB-ArrowPlus"));
            addVariationButton.eventClicked += AddVariation;

            // Remove variation button.
            UIButton removeVariationButton = AddIconButton(this, MidControlX, ListY + ToggleSize, ToggleSize, "BOB_RND_SUB", UITextures.LoadQuadSpriteAtlas("BOB-ArrowMinus"));
            removeVariationButton.eventClicked += RemoveVariation;

            // Order button.
            m_replacementNameSortButton = ArrowButton(this, LoadedX + 10f, ListY - 20f);
            m_replacementNameSortButton.eventClicked += SortReplacements;

            // Probability slider.
            _probSlider = AddBOBSlider(this, SelectedX + Margin, ToolY + Margin, SelectedWidth - (Margin * 2f), "BOB_PNL_PRB", 0, 100, 1, "Probability");
            _probSlider.EventTrueValueChanged += (c, value) =>
            {
                if (_selectedVariation != null)
                {
                    _selectedVariation.Probability = (int)value;
                    _lastChangedVariant = _selectedVariation;

                    if (!_ignoreValueChange)
                    {
                        UpdateCurrentRandomPrefab();
                    }

                    _variationsList.Refresh();
                    ConfigurationUtils.SaveConfig();
                }
            };

            // Default is name ascending.
            SetFgSprites(m_replacementNameSortButton, "IconUpArrow2");

            // Preview image.
            _previewPanel = AddUIComponent<PreviewPanel>();
            _previewPanel.relativePosition = new Vector2(this.width + Margin, ListY);

            // Populate loaded lists.
            RegenerateRandomList();
            RegenerateReplacementList();

            // Update button states.
            UpdateButtonStates();

            // Bring to front.
            BringToFront();
        }

        /// <summary>
        /// Gets the active panel instance.
        /// </summary>
        internal static BOBRandomPanel Panel => s_panel;

        /// <summary>
        /// Sets the currently selected loaded prefab.
        /// </summary>
        internal PrefabInfo SelectedLoadedPrefab
        {
            private get => _selectedLoadedPrefab;

            set
            {
                _selectedLoadedPrefab = value;
                _previewPanel.SetTarget(value);
            }
        }

        /// <summary>
        /// Sets the currently selected random component.
        /// </summary>
        internal BOBRandomPrefab.Variation SelectedVariation
        {
            set
            {
                _selectedVariation = value;

                // Disable events while updating value.
                _ignoreValueChange = true;
                _probSlider.value = value.Probability;
                _ignoreValueChange = false;
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
                if (value == _selectedRandomPrefab)
                {
                    return;
                }

                // Set selection.
                _selectedRandomPrefab = value;

                // Reset variation lists.
                _selectedVariation = null;
                _variationsList.selectedIndex = -1;

                // Regenerate variation UI fastlist.
                VariationsList();

                // Update name text.
                _nameField.text = _selectedRandomPrefab?.Name ?? string.Empty;

                // Update button states.
                UpdateButtonStates();
            }
        }

        /// <summary>
        /// Gets the panel width.
        /// </summary>
        protected override float PanelWidth => LoadedX + LoadedWidth + Margin;

        /// <summary>
        /// Gets the panel height.
        /// </summary>
        protected override float PanelHeight => ListY + ListHeight + Margin;

        /// <summary>
        /// Gets the panel opacity.
        /// </summary>
        protected override float PanelOpacity => 1f;

        // Trees or props?
        private bool IsTree => PropTreeMode == PropTreeModes.Tree;

        /// <summary>
        /// Creates the panel object in-game and displays it.
        /// </summary>
        internal static void Create()
        {
            try
            {
                // If no GameObject instance already set, create one.
                if (s_gameObject == null)
                {
                    // Give it a unique name for easy finding with ModTools.
                    s_gameObject = new GameObject("BOBRandomPanel");
                    s_gameObject.transform.parent = UIView.GetAView().transform;

                    // Create new panel instance and add it to GameObject.
                    s_panel = s_gameObject.AddComponent<BOBRandomPanel>();

                    // Hide previous window, if any.
                    BOBPanelManager.Panel?.Hide();
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
            if (s_panel == null)
            {
                return;
            }

            // Save configuration file.
            ConfigurationUtils.SaveConfig();

            // Refresh random prop/tree lists.
            BOBPanelManager.RefreshRandom();

            // Destroy game objects.
            GameObject.Destroy(s_panel);
            GameObject.Destroy(s_gameObject);

            // Let the garbage collector do its work (and also let us know that we've closed the object).
            s_panel = null;
            s_gameObject = null;

            // Show previous window, if any.
            BOBPanelManager.Panel?.Show();
        }

        /// <summary>
        /// Close button event handler.
        /// </summary>
        protected override void CloseEvent() => Close();

        /// <summary>
        /// Populates the replacement UIList with a filtered list of eligible relacement trees or props.
        /// </summary>
        protected override void RegenerateReplacementList()
        {
            // List of prefabs that have passed filtering.
            List<PrefabInfo> list = new List<PrefabInfo>();

            bool nameFilterActive = !SearchText.IsNullOrWhiteSpace();

            if (IsTree)
            {
                // Tree - iterate through each prop in our list of loaded prefabs.
                foreach (TreeInfo loadedTree in PrefabLists.LoadedTrees)
                {
                    // Set display name.
                    string displayName = PrefabLists.GetDisplayName(loadedTree);

                    // Apply vanilla filtering if selected.
                    if (!m_hideVanilla.isChecked || !displayName.StartsWith("[v]"))
                    {
                        // Apply name filter.
                        if (!nameFilterActive || displayName.ToLower().Contains(SearchText.Trim().ToLower()))
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
                foreach (PropInfo loadedProp in PrefabLists.LoadedProps)
                {
                    // Skip any props that require height or water maps.
                    if (loadedProp.m_requireHeightMap || loadedProp.m_requireWaterMap)
                    {
                        continue;
                    }

                    // Set display name.
                    string displayName = PrefabLists.GetDisplayName(loadedProp);

                    // Apply vanilla filtering if selected.
                    if (!m_hideVanilla.isChecked || !displayName.StartsWith("[v]"))
                    {
                        // Apply name filter.
                        if (!nameFilterActive || displayName.ToLower().Contains(SearchText.Trim().ToLower()))
                        {
                            // Filtering passed - add this prefab to our list.
                            list.Add(loadedProp);
                        }
                    }
                }
            }

            // Master lists should already be sorted by display name so no need to sort again here.
            // Reverse order of filtered list if we're searching name descending.
            if (m_replacementSortSetting == (int)OrderBy.NameDescending)
            {
                list.Reverse();
            }

            // Create return fastlist from our filtered list.
            _loadedList.rowsData = new FastList<object>
            {
                m_buffer = list.ToArray(),
                m_size = list.Count,
            };
        }

        /// <summary>
        /// Performs actions required after a change to prop/tree mode.
        /// </summary>
        protected override void PropTreeChange()
        {
            // Reset current items.
            SelectedRandomPrefab = null;

            // Regenerate lists.
            RegenerateRandomList();
            RegenerateReplacementList();
        }

        /// <summary>
        /// Updates button states (enabled/disabled) according to current control states.
        /// </summary>
        private void UpdateButtonStates()
        {
            // Toggle enabled state based on whether or not there's a valid current selection.
            bool buttonState = _selectedRandomPrefab != null;
            _removeRandomButton.isEnabled = buttonState;
            _renameButton.isEnabled = buttonState;
        }

        /// <summary>
        /// Create new random prefab event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="p">Mouse event parameter.</param>
        private void NewRandomPrefab(UIComponent c, UIMouseEventParameter p)
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
                    existingTree = PrefabLists.RandomTrees.Find(x => x.Name.Equals(treeName));
                    if (existingTree != null)
                    {
                        treeName = treeNameBase + " " + (++existingCount).ToString();
                    }
                }
                while (existingTree != null);

                // Create new random tree prefab.
                Logging.Message("creating new random tree");
                newPrefab = RandomPrefabs.NewRandomTree(treeName);
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
                    existingProp = PrefabLists.RandomProps.Find(x => x.Name.Equals(propName));
                    if (existingProp != null)
                    {
                        propName = propNameBase + " " + (++existingCount).ToString();
                    }
                }
                while (existingProp != null);

                // Create new random prop prefab.
                Logging.Message("creating new random prop");
                newPrefab = RandomPrefabs.NewRandomProp(propName);
            }

            // Did we succesfully create a new prefab?
            if (newPrefab != null)
            {
                Logging.Message("Trying to find item");

                // Yes - regenerate random list to reflect the change, and select the new item.
                RegenerateRandomList();
                _randomList.FindItem(newPrefab);
                SelectedRandomPrefab = newPrefab;
            }
        }

        /// <summary>
        /// Remove random prefab event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="p">Mouse event parameter.</param>
        private void RemoveRandomPrefab(UIComponent c, UIMouseEventParameter p)
        {
            // Safety checks.
            if (_randomList.selectedIndex < 0 || _selectedRandomPrefab == null)
            {
                return;
            }

            // Remove tree or prop from relevant list of random prefabs.
            if (_selectedRandomPrefab.Tree != null)
            {
                RandomPrefabs.RemoveRandomTree(_selectedRandomPrefab.Tree);
            }
            else if (_selectedRandomPrefab.Prop != null)
            {
                RandomPrefabs.RemoveRandomProp(_selectedRandomPrefab.Prop);
            }

            // Reset selection and regenerate UI fastlist.
            SelectedRandomPrefab = null;
            _randomList.selectedIndex = -1;
            RegenerateRandomList();
        }

        /// <summary>
        /// Rename random prefab event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="p">Mouse event parameter.</param>
        private void RenameRandomPrefab(UIComponent c, UIMouseEventParameter p)
        {
            // Safety checks.
            if (_randomList.selectedIndex < 0 || _selectedRandomPrefab == null || _nameField.text.IsNullOrWhiteSpace())
            {
                return;
            }

            string trimmedName = _nameField.text.Trim();

            // Need unique name.
            if ((_selectedRandomPrefab.Prop != null & RandomPrefabs.DuplicatePropName(trimmedName)) || (_selectedRandomPrefab.Tree != null && RandomPrefabs.DuplicateTreeName(trimmedName)))
            {
                Logging.Error("duplicate name");
                return;
            }

            // If we got here, all good; rename random prop reference and PrefabInfo.
            _selectedRandomPrefab.Name = trimmedName;
            if (_selectedRandomPrefab.Prop != null)
            {
                _selectedRandomPrefab.Prop.name = _selectedRandomPrefab.Name;
            }

            if (_selectedRandomPrefab.Tree != null)
            {
                _selectedRandomPrefab.Tree.name = _selectedRandomPrefab.Name;
            }

            // Regenerate list.
            RegenerateRandomList();
        }

        /// <summary>
        /// Add variation event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="p">Mouse event parameter.</param>
        private void AddVariation(UIComponent c, UIMouseEventParameter p)
        {
            // Make sure we've got a valid selection first.
            if (SelectedLoadedPrefab == null || _selectedRandomPrefab == null)
            {
                return;
            }

            // Add selected prefab to list of variations and regenerate UI fastlist.
            BOBRandomPrefab.Variation newVariant = new BOBRandomPrefab.Variation { Name = SelectedLoadedPrefab.name, Prefab = SelectedLoadedPrefab, Probability = 0 };
            _selectedRandomPrefab.Variations.Add(newVariant);
            VariationsList();

            // Select variation.
            _variationsList.FindItem(newVariant);
            _selectedVariation = newVariant;

            // Update the random prefab with the new variation.
            UpdateCurrentRandomPrefab();

            // Update slider value.
            _probSlider.value = newVariant.Probability;
        }

        /// <summary>
        /// Remove variation event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="p">Mouse event parameter.</param>
        private void RemoveVariation(UIComponent c, UIMouseEventParameter p)
        {
            // Make sure we've got a valid selection first.
            if (_selectedVariation == null)
            {
                return;
            }

            // Remove selected prefab from list of variations and regenerate UI fastlist.
            _selectedRandomPrefab.Variations.Remove(_selectedVariation);
            VariationsList();

            // Update the random prefab to reflect the removal
            UpdateCurrentRandomPrefab();
        }

        /// <summary>
        /// Updates the variations for the current random prefab.
        /// </summary>
        private void UpdateCurrentRandomPrefab()
        {
            int variationCount = _selectedRandomPrefab.Variations.Count;

            // Recalculate probabilities.
            RecalculateProbabilities();

            // Trees or props?
            if (_selectedRandomPrefab.Tree != null)
            {
                // Trees - create new variations array.
                _selectedRandomPrefab.Tree.m_variations = new TreeInfo.Variation[variationCount];

                // Iterate through current variations list and add to prefab variation list.
                for (int i = 0; i < variationCount; ++i)
                {
                    _selectedRandomPrefab.Tree.m_variations[i] = new TreeInfo.Variation()
                    {
                        m_finalTree = _selectedRandomPrefab.Variations[i].Prefab as TreeInfo,
                        m_probability = 100 / variationCount,
                    };
                }
            }
            else if (_selectedRandomPrefab.Prop != null)
            {
                // Props - create new variations array.
                _selectedRandomPrefab.Prop.m_variations = new PropInfo.Variation[variationCount];

                // Iterate through current variations list and add to prefab variation list.
                for (int i = 0; i < variationCount; ++i)
                {
                    _selectedRandomPrefab.Prop.m_variations[i] = new PropInfo.Variation()
                    {
                        m_finalProp = _selectedRandomPrefab.Variations[i].Prefab as PropInfo,
                        m_probability = _selectedRandomPrefab.Variations[i].Probability,
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
            bool validLastChanged = _lastChangedVariant != null;

            // Number of locked and unlocked entries and their summative probabilities.
            int lockedProbs = 0, unlockedProbs = 0, lockedCount = 0, unlockedCount = 0;

            // Iterate through all current variations, identifying locked probabilties.
            for (int i = 0; i < _selectedRandomPrefab.Variations.Count; ++i)
            {
                // Ignore last changed variant.
                if (!validLastChanged || (validLastChanged && _selectedRandomPrefab.Variations[i] != _lastChangedVariant))
                {
                    // If this variation has a locked probability, add the probability to the total locked percentage and increment the locked counter - ignoring most recently changed item.
                    if (_selectedRandomPrefab.Variations[i].ProbLocked)
                    {
                        lockedProbs += _selectedRandomPrefab.Variations[i].Probability;
                        ++lockedCount;
                    }
                    else
                    {
                        // Unlocked.
                        unlockedProbs += _selectedRandomPrefab.Variations[i].Probability;
                        ++unlockedCount;
                    }
                }
            }

            // Get the probability of the most recently changed probability.
            int changedProb = validLastChanged ? _lastChangedVariant.Probability : 0;

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
                _lastChangedVariant.Probability = changedProb;
            }

            // Now, assign probabilities.
            int residualProb = Mathf.Max(0, 100 - lockedProbs - changedProb);
            for (int i = 0; i < _selectedRandomPrefab.Variations.Count; ++i)
            {
                // Ignore last changed variant.
                if (!validLastChanged || (validLastChanged && _selectedRandomPrefab.Variations[i] != _lastChangedVariant))
                {
                    if (!_selectedRandomPrefab.Variations[i].ProbLocked)
                    {
                        _selectedRandomPrefab.Variations[i].Probability = Mathf.Max(1, residualProb / unlockedCount--);
                        residualProb -= _selectedRandomPrefab.Variations[i].Probability;
                    }
                }
            }

            // Update probability slider.
            if (_selectedVariation != null)
            {
                _probSlider.value = _selectedVariation.Probability;
            }

            // Regenerate list.
            _variationsList.Refresh();
        }

        /// <summary>
        /// Regenerates the random prefab UI fastlist.
        /// </summary>
        private void RegenerateRandomList()
        {
            // Remove selection.
            _randomList.selectedIndex = -1;

            // Trees or props?
            if (IsTree)
            {
                // Trees.
                _randomList.rowsData = new FastList<object>
                {
                    m_buffer = PrefabLists.RandomTrees.OrderBy(x => x.Name.ToLower()).ToArray(),
                    m_size = PrefabLists.RandomTrees.Count,
                };
            }
            else
            {
                // Props.
                _randomList.rowsData = new FastList<object>
                {
                    m_buffer = PrefabLists.RandomProps.OrderBy(x => x.Name.ToLower()).ToArray(),
                    m_size = PrefabLists.RandomProps.Count,
                };
            }
        }

        /// <summary>
        /// Regenerates the variations UI fastlist.
        /// </summary>
        private void VariationsList()
        {
            // Remove selection.
            _variationsList.selectedIndex = -1;

            // Create return fastlist from our filtered list.
            _variationsList.rowsData = new FastList<object>
            {
                m_buffer = _selectedRandomPrefab?.Variations?.OrderBy(x => PrefabLists.GetDisplayName(x.Name).ToLower()).ToArray(),
                m_size = _selectedRandomPrefab?.Variations?.Count ?? 0,
            };
        }
    }
}
