// <copyright file="BOBScalePanel.cs" company="algernon (K. Algernon A. Sheppard)">
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
        private const float ListHeight = UIListRow.DefaultRowHeight * 16f;

        // Instance references.
        private static GameObject s_gameObject;
        private static BOBScalePanel s_panel;

        // Opening prop/tree mode.
        private static PropTreeModes s_openingMode = PropTreeModes.Prop;

        // Panel components.
        private readonly UIList _loadedList;
        private readonly BOBSlider _minScaleSlider;
        private readonly BOBSlider _maxScaleSlider;
        private readonly UIButton _revertButton;
        private readonly UIButton _loadedCreatorButton;

        // Current selection.
        private PrefabInfo _selectedLoadedPrefab;

        // Status.
        private bool _disableEvents = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="BOBScalePanel"/> class.
        /// </summary>
        internal BOBScalePanel()
        {
            // Default position - centre in screen.
            relativePosition = new Vector2(Mathf.Floor((GetUIView().fixedWidth - width) / 2), Mathf.Floor((GetUIView().fixedHeight - height) / 2));

            // Disable 'both' check.
            m_propTreeChecks[(int)PropTreeModes.Both].Disable();
            m_propTreeChecks[(int)PropTreeModes.Both].Hide();

            // Title label.
            SetTitle(Translations.Translate("BOB_NAM") + " : " + Translations.Translate("BOB_SCA_TIT"));

            // Minimum scale slider.
            _minScaleSlider = AddBOBSlider(this, ControlX, MinOffsetY, ControlWidth - (Margin * 2f), "BOB_SCA_MIN", 0.5f, 2f, 0.5f, "MinScale");
            _minScaleSlider.EventTrueValueChanged += MinScaleValue;
            _minScaleSlider.value = 1f;
            _maxScaleSlider = AddBOBSlider(this, ControlX, MaxOffsetY + 40f, ControlWidth - (Margin * 2f), "BOB_SCA_MAX", 0.5f, 2f, 0.5f, "MaxScale");
            _maxScaleSlider.EventTrueValueChanged += MaxScaleValue;
            _maxScaleSlider.value = 1f;

            // Revert button.
            _revertButton = UIButtons.AddSmallerButton(this, ControlX, RevertY, Translations.Translate("BOB_PNL_REV"), ControlWidth);
            _revertButton.eventClicked += Revert;
            _revertButton.Disable();

            // Loaded prop list.
            UIPanel loadedPanel = AddUIComponent<UIPanel>();
            loadedPanel.width = LoadedWidth;
            loadedPanel.height = ListHeight;
            loadedPanel.relativePosition = new Vector2(LoadedX, ListY);
            _loadedList = UIList.AddUIList<LoadedPrefabItem.DisplayRow>(loadedPanel, 0f, 0f, LoadedWidth, ListHeight);
            _loadedList.EventSelectionChanged += (c, data) => _selectedLoadedPrefab = (data as LoadedPrefabItem)?.Prefab;

            // Order button.
            m_replacementNameSortButton = ArrowButton(this, LoadedX + 10f, ListY - 20f);
            m_replacementNameSortButton.eventClicked += SortReplacements;

            _loadedCreatorButton = ArrowButton(this, LoadedX + LoadedPrefabItem.DisplayRow.CreatorLabelX + 10f, ListY - 20f);
            _loadedCreatorButton.eventClicked += SortReplacements;

            // Default is name ascending.
            SetFgSprites(m_replacementNameSortButton, "IconUpArrow2");

            // Regenerate replacement list.
            RegenerateReplacementList();

            // Bring to front.
            BringToFront();
        }

        /// <summary>
        /// Gets the active panel instance.
        /// </summary>
        internal static BOBScalePanel Panel => s_panel;

        /// <summary>
        /// Sets the currently selected loaded prefab.
        /// </summary>
        internal PrefabInfo SelectedLoadedPrefab
        {
            set
            {
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

        /// <summary>
        /// Gets the initial prop-tree mode for this panel.
        /// </summary>
        protected override PropTreeModes InitialPropTreeMode => s_openingMode;

        // Trees or props?
        private bool IsTree => PropTreeMode == PropTreeModes.Tree;

        /// <summary>
        /// Creates the panel object in-game and displays it.
        /// </summary>
        /// <param name="initialMode">Initial prop-tree opening mode.</param>
        /// <param name="selectedPrefab">Already selected prefab (null if none).</param>
        internal static void Create(PropTreeModes initialMode, PrefabInfo selectedPrefab)
        {
            try
            {
                // If no GameObject instance already set, create one.
                if (s_gameObject == null)
                {
                    // Give it a unique name for easy finding with ModTools.
                    s_gameObject = new GameObject("BOBScalePanel");
                    s_gameObject.transform.parent = UIView.GetAView().transform;

                    // Set opening prop-tree mode.
                    if (initialMode == PropTreeModes.Tree)
                    {
                        s_openingMode = PropTreeModes.Tree;
                    }
                    else
                    {
                        s_openingMode = PropTreeModes.Prop;
                    }

                    // Create new panel instance and add it to GameObject.
                    s_panel = s_gameObject.AddComponent<BOBScalePanel>();

                    // Select previously selected prefab, if any.
                    if (selectedPrefab != null)
                    {
                        s_panel.SelectedLoadedPrefab = selectedPrefab;
                        s_panel._loadedList.FindItem<LoadedPrefabItem>(x => x.Prefab == selectedPrefab);
                    }

                    // Hide previous window, if any.
                    BOBPanelManager.Panel?.Hide();
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception creating scale panel");
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