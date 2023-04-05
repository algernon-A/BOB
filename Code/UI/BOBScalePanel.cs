// <copyright file="BOBScalePanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System.Collections.Generic;
    using System.Linq;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Panel for prop scale selection.
    /// </summary>
    internal sealed class BOBScalePanel : BOBPanelBase
    {
        // Layout constants - X.
        private const float ControlX = Margin * 2f;
        private const float ControlWidth = 250f;
        private const float LoadedX = ControlX + ControlWidth + (Margin * 2f);
        private const float LoadedWidth = 440f;

        // Layout constants - Y.
        private const float SliderHeight = 38f;
        private const float ToolY = TitleHeight + Margin;
        private const float ListY = ToolY + ToolbarHeight + ArrowButtonHeight + Margin;
        private const float MinOffsetY = ListY;
        private const float MaxOffsetY = MinOffsetY + SliderHeight;
        private const float RevertY = MaxOffsetY + SliderHeight + 45f;
        private const float ListHeight = UIList.DefaultRowHeight * 16f;

        // Panel components.
        private UIList _loadedList;
        private BOBSlider _minScaleSlider;
        private BOBSlider _maxScaleSlider;
        private UIButton _revertButton;
        private UIButton _loadedCreatorButton;

        // Current selection.
        private PrefabInfo _selectedLoadedPrefab;

        // Status.
        private bool _disableEvents = false;
        private bool _panelReady = false;
        private PrefabInfo _initialPrefab = null;

        /// <summary>
        /// Gets the panel width.
        /// </summary>
        public override float PanelWidth => LoadedX + LoadedWidth + Margin;

        /// <summary>
        /// Gets the panel height.
        /// </summary>
        public override float PanelHeight => ListY + ListHeight + Margin;

        /// <summary>
        /// Gets the panel's title.
        /// </summary>
        protected override string PanelTitle => Translations.Translate("BOB_NAM") + " : " + Translations.Translate("BOB_SCA_TIT");

        /// <summary>
        /// Sets the currently selected loaded prefab.
        /// </summary>
        private PrefabInfo SelectedLoadedPrefab
        {
            set
            {
                AlgernonCommons.Logging.Message("setting scale prefab");

                // Disable events, otherwise slider value changes will mess things up.
                _disableEvents = true;

                // Set value.
                _selectedLoadedPrefab = value;

                // Clear highlighting by default (re-enable it later if needed).
                RenderOverlays.PropIndex = -1;
                RenderOverlays.Prop = null;
                RenderOverlays.Tree = null;

                // Prop or tree?  Set slider values accordingly.
                if (_selectedLoadedPrefab is PropInfo prop)
                {
                    _minScaleSlider.TrueValue = prop.m_minScale;
                    _maxScaleSlider.TrueValue = prop.m_maxScale;

                    // Enable revert button.
                    _revertButton.Enable();

                    // Set highlighting.
                    RenderOverlays.Prop = prop;
                }
                else if (_selectedLoadedPrefab is TreeInfo tree)
                {
                    _minScaleSlider.TrueValue = tree.m_minScale;
                    _maxScaleSlider.TrueValue = tree.m_maxScale;

                    // Enable revert button.
                    _revertButton.Enable();

                    // Set highlighting.
                    RenderOverlays.Tree = tree;
                }
                else
                {
                    // Neither prop nor tree, presumably null - set sliders to default values.
                    _minScaleSlider.TrueValue = 1f;
                    _maxScaleSlider.TrueValue = 1f;

                    // Disable revert button if no valid selection.
                    _revertButton.Disable();
                }

                // Restore events.
                _disableEvents = false;
            }
        }

        // Trees or props?
        private bool IsTree => PropTreeMode == PropTreeModes.Tree;

        /// <summary>
        /// Called by Unity before the first frame is displayed.
        /// Used to perform setup.
        /// </summary>
        public override void Start()
        {
            base.Start();

            // Default position - centre in screen.
            relativePosition = new Vector2(Mathf.Floor((GetUIView().fixedWidth - width) / 2), Mathf.Floor((GetUIView().fixedHeight - height) / 2));

            // Disable 'both' check.
            m_propTreeChecks[(int)PropTreeModes.Both].Disable();
            m_propTreeChecks[(int)PropTreeModes.Both].Hide();

            // Minimum scale slider.
            _minScaleSlider = AddBOBSlider(this, ControlX, MinOffsetY, ControlWidth - (Margin * 2f), "BOB_SCA_MIN", BOBScalingElement.MinimumScale, BOBScalingElement.MaximumScale, 0.1f, "MinScale");
            _minScaleSlider.EventTrueValueChanged += MinScaleValue;
            _minScaleSlider.value = 1f;
            _minScaleSlider.LimitToVisible = true;
            _maxScaleSlider = AddBOBSlider(this, ControlX, MaxOffsetY + 40f, ControlWidth - (Margin * 2f), "BOB_SCA_MAX", BOBScalingElement.MinimumScale, BOBScalingElement.MaximumScale, 0.1f, "MaxScale");
            _maxScaleSlider.EventTrueValueChanged += MaxScaleValue;
            _maxScaleSlider.value = 1f;
            _maxScaleSlider.LimitToVisible = true;

            // Revert button.
            _revertButton = UIButtons.AddSmallerButton(this, ControlX, RevertY, Translations.Translate("BOB_PNL_REV"), ControlWidth);
            _revertButton.eventClicked += Revert;
            _revertButton.Disable();

            // Loaded prop list.
            _loadedList = UIList.AddUIList<LoadedPrefabItem.DisplayRow>(this, LoadedX, ListY, LoadedWidth, ListHeight);
            _loadedList.EventSelectionChanged += (c, data) => SelectedLoadedPrefab = (data as LoadedPrefabItem)?.Prefab;

            // Order button.
            m_replacementNameSortButton = ArrowButton(this, LoadedX + 10f, ListY - 20f);
            m_replacementNameSortButton.eventClicked += SortReplacements;

            _loadedCreatorButton = ArrowButton(this, LoadedX + LoadedPrefabItem.DisplayRow.CreatorLabelX + 10f, ListY - 20f);
            _loadedCreatorButton.eventClicked += SortReplacements;

            // Default is name ascending.
            SetFgSprites(m_replacementNameSortButton, "IconUpArrow2");

            // Activate panel.
            _panelReady = true;

            // Regenerate replacement list.
            RegenerateReplacementList();

            // Set initial parent.
            SelectPrefab(_initialPrefab);

            // Bring to front.
            BringToFront();

            // Hide previous window, if any.
            BOBPanelManager.Panel?.Hide();
        }

        /// <summary>
        /// Sets the selected prefab to the one (if any) representing the given tree or prop prefab.
        /// </summary>
        /// <param name="prefab">Tree or prop prefab.</param>
        internal void SelectPrefab(PrefabInfo prefab)
        {
            // Don't proceed further if panel isn't ready.
            if (!_panelReady)
            {
                _initialPrefab = prefab;
                return;
            }

            _loadedList.FindItem<LoadedPrefabItem>(x => x.Prefab == prefab);
        }

        /// <summary>
        /// Populates the replacement UIList with a filtered list of eligible relacement trees or props.
        /// </summary>
        protected override void RegenerateReplacementList()
        {
            // List of prefabs that have passed filtering.
            List<LoadedPrefabItem> list = new List<LoadedPrefabItem>();

            bool nameFilterActive = !SearchText.IsNullOrWhiteSpace();

            if (IsTree)
            {
                // Tree - iterate through each prop in our list of loaded prefabs.
                foreach (LoadedPrefabItem loadedTree in PrefabLists.LoadedTreeItems)
                {
                    // Apply vanilla filtering if selected.
                    if (!m_hideVanilla.isChecked | !loadedTree.IsVanilla)
                    {
                        // Apply name filter.
                        if (!nameFilterActive || loadedTree.DisplayName.ToLower().Contains(SearchText.Trim().ToLower()))
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
                foreach (LoadedPrefabItem loadedProp in PrefabLists.LoadedPropItems)
                {
                    // Apply vanilla filtering if selected.
                    if (!m_hideVanilla.isChecked | !loadedProp.IsVanilla)
                    {
                        // Apply name filter.
                        if (!nameFilterActive || loadedProp.DisplayName.ToLower().Contains(SearchText.Trim().ToLower()))
                        {
                            // Filtering passed - add this prefab to our list.
                            list.Add(loadedProp);
                        }
                    }
                }
            }

            // Create new object list for fastlist, ordering as approprite.
            object[] objectArray;
            switch (m_replacementSortSetting)
            {
                case (int)OrderBy.NameDescending:
                    objectArray = list.OrderByDescending(item => item.DisplayName).ToArray();
                    break;
                case (int)OrderBy.CreatorAscending:
                    objectArray = list.OrderBy(item => item.CreatorName).ToArray();
                    break;
                case (int)OrderBy.CreatorDescending:
                    objectArray = list.OrderByDescending(item => item.CreatorName).ToArray();
                    break;
                default:
                    objectArray = list.OrderBy(item => item.DisplayName).ToArray();
                    break;
            }

            // Create return fastlist from our filtered list.
            _loadedList.Data = new FastList<object>
            {
                m_buffer = objectArray,
                m_size = list.Count,
            };

            // Select currently selected prefab, if any.
            if (_selectedLoadedPrefab != null)
            {
                _loadedList.FindItem<LoadedPrefabItem>(x => x.Prefab == _selectedLoadedPrefab);
            }
            else
            {
                // No current selection.
                _loadedList.SelectedIndex = -1;
            }
        }

        /// <summary>
        /// Performs actions required after a change to prop/tree mode.
        /// </summary>
        protected override void PropTreeChange()
        {
            // Reset current item.
            SelectedLoadedPrefab = null;

            // Regenerate replacement list.
            RegenerateReplacementList();
        }

        /// <summary>
        /// Replacement list sort button event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="p">Mouse event parameter.</param>
        protected override void SortReplacements(UIComponent c, UIMouseEventParameter p)
        {
            // Check if we are using the name or creator button.
            if (c == m_replacementNameSortButton)
            {
                // Name button.
                // Toggle status (set to descending if we're currently ascending, otherwise set to ascending).
                if (m_replacementSortSetting == (int)OrderBy.NameAscending)
                {
                    // Order by name descending.
                    m_replacementSortSetting = (int)OrderBy.NameDescending;
                }
                else
                {
                    // Order by name ascending.
                    m_replacementSortSetting = (int)OrderBy.NameAscending;
                }

                // Reset name order buttons.
                SetSortButton(m_replacementNameSortButton, _loadedCreatorButton, m_replacementSortSetting);
            }
            else if (c == _loadedCreatorButton)
            {
                // Creator button.
                // Toggle status (set to descending if we're currently ascending, otherwise set to ascending).
                if (m_replacementSortSetting == (int)OrderBy.CreatorAscending)
                {
                    // Order by creator descending.
                    m_replacementSortSetting = (int)OrderBy.CreatorDescending;
                }
                else
                {
                    // Order by name ascending.
                    m_replacementSortSetting = (int)OrderBy.CreatorAscending;
                }

                // Reset name order buttons.
                SetSortButton(_loadedCreatorButton, m_replacementNameSortButton, m_replacementSortSetting);
            }

            // Regenerate replacement list.
            RegenerateReplacementList();
        }

        /// <summary>
        /// Performs any actions required before closing the panel.
        /// </summary>
        /// <returns>True if the panel can close now, false otherwise.</returns>
        protected override bool PreClose()
        {
            // Show previous window, if any.
            BOBPanelManager.Panel?.Show();

            return true;
        }

        /// <summary>
        /// Sets the states of the given sort button to match the given search status.
        /// </summary>
        /// <param name="activeButton">Currently active sort button.</param>
        /// <param name="inactiveButton">Inactive button (other sort button for same list).</param>
        /// <param name="searchStatus">Search status to apply.</param>
        private void SetSortButton(UIButton activeButton, UIButton inactiveButton, int searchStatus)
        {
            // Null check.
            if (activeButton == null || inactiveButton == null)
            {
                return;
            }

            bool ascending = searchStatus == (int)OrderBy.CreatorAscending || searchStatus == (int)OrderBy.NameAscending;

            // Toggle status (set to descending if we're currently ascending, otherwise set to ascending).
            if (ascending)
            {
                // Order ascending.
                SetFgSprites(activeButton, "IconUpArrow2Focused");
            }
            else
            {
                // Order descending.
                SetFgSprites(activeButton, "IconDownArrow2Focused");
            }

            // Reset inactive button.
            SetFgSprites(inactiveButton, "IconUpArrow2");
        }

        /// <summary>
        /// Revert button event handler.
        /// <param name="control">Calling component (unused)</param>
        /// <param name="mouseEvent">Mouse event (unused)</param>
        /// </summary>
        private void Revert(UIComponent control, UIMouseEventParameter mouseEvent)
        {
            // Null check.
            if (_selectedLoadedPrefab?.name != null)
            {
                // Revert current selection.
                Scaling.Instance.Revert(_selectedLoadedPrefab, true);

                // Reset prefab record to reset slider valies.
                SelectedLoadedPrefab = _selectedLoadedPrefab;
            }
        }

        /// <summary>
        /// Minimum scale slider event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="value">New value.</param>
        private void MinScaleValue(UIComponent c, float value)
        {
            // Don't apply changes if events are disabled.
            if (!_disableEvents)
            {
                Scaling.Instance.ApplyMinScale(_selectedLoadedPrefab, value);
            }
        }

        /// <summary>
        /// Maximum scale slider event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="value">New value.</param>
        private void MaxScaleValue(UIComponent c, float value)
        {
            // Don't apply changes if events are disabled.
            if (!_disableEvents)
            {
                Scaling.Instance.ApplyMaxScale(_selectedLoadedPrefab, value);
            }
        }
    }
}