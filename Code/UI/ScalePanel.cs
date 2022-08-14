namespace BOB
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using AlgernonCommons;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Panel for prop scale selection.
    /// </summary>
    internal class BOBScalePanel : BOBPanelBase
    {
        // Layout constants - X.
        private const float ControlX = (Margin * 2f);
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
        private const float ListHeight = UIPropRow.RowHeight * 16f;


        // Instance references.
        private static GameObject uiGameObject;
        private static BOBScalePanel panel;
        internal static BOBScalePanel Panel => panel;

        // Panel components.
        private readonly UIFastList loadedList;
        private readonly BOBSlider minScaleSlider, maxScaleSlider;
        private readonly UIButton revertButton;
        private readonly UIButton loadedCreatorButton;

        // Current selection.
        private PrefabInfo selectedLoadedPrefab;

        // Status.
        private bool disableEvents = false;

        // Opening prop/tree mode.
        private static PropTreeModes openingMode = PropTreeModes.Prop;



        /// <summary>
        /// Initial prop-tree mode.
        /// </summary>
        protected override PropTreeModes InitialPropTreeMode => openingMode;


        // Panel width.
        protected override float PanelWidth => LoadedX + LoadedWidth + Margin;

        // Panel height.
        protected override float PanelHeight => ListY + ListHeight + Margin;

        // Panel opacity.
        protected override float PanelOpacity => 1f;


        /// <summary>
        /// Sets the currently selected loaded prefab.
        /// </summary>
        internal PrefabInfo SelectedLoadedPrefab
        {
            set
            {
                // Disable events, otherwise slider value changes will mess things up.
                disableEvents = true;

                // Set value.
                selectedLoadedPrefab = value;

                // Clear highlighting by default (re-enable it later if needed).
                RenderOverlays.PropIndex = -1;
                RenderOverlays.Prop = null;
                RenderOverlays.Tree = null;

                // Prop or tree?  Set slider values accordingly.
                if (selectedLoadedPrefab is PropInfo prop)
                {
                    minScaleSlider.TrueValue = prop.m_minScale;
                    maxScaleSlider.TrueValue = prop.m_maxScale;

                    // Enable revert button.
                    revertButton.Enable();

                    // Set highlighting.
                    RenderOverlays.Prop = prop;
                }
                else if (selectedLoadedPrefab is TreeInfo tree)
                {
                    minScaleSlider.TrueValue = tree.m_minScale;
                    maxScaleSlider.TrueValue = tree.m_maxScale;

                    // Enable revert button.
                    revertButton.Enable();

                    // Set highlighting.
                    RenderOverlays.Tree = tree;
                }
                else
                {
                    // Neither prop nor tree, presumably null - set sliders to default values.
                    minScaleSlider.TrueValue = 1f;
                    maxScaleSlider.TrueValue = 1f;

                    // Disable revert button if no valid selection.
                    revertButton.Disable();
                }

                // Restore events.
                disableEvents = false;
            }
        }


        /// <summary>
        /// Creates the panel object in-game and displays it.
        /// </summary>
        /// <param name="initialMode">Initial prop-tree opening mode</param>
        /// <param name="selectedPrefab">Already selected prefab (null if none)</param>
        internal static void Create(PropTreeModes initialMode, PrefabInfo selectedPrefab)
        {
            try
            {
                // If no GameObject instance already set, create one.
                if (uiGameObject == null)
                {
                    // Give it a unique name for easy finding with ModTools.
                    uiGameObject = new GameObject("BOBScalePanel");
                    uiGameObject.transform.parent = UIView.GetAView().transform;

                    // Set opening prop-tree mode.
                    if (initialMode == PropTreeModes.Tree)
                    {
                        openingMode = PropTreeModes.Tree;
                    }
                    else
                    {
                        openingMode = PropTreeModes.Prop;
                    }

                    // Create new panel instance and add it to GameObject.
                    panel = uiGameObject.AddComponent<BOBScalePanel>();

                    // Select previously selected prefab, if any.
                    if (selectedPrefab != null)
                    {
                        panel.SelectedLoadedPrefab = selectedPrefab;
                        panel.loadedList.FindItem(selectedPrefab);
                    }

                    // Hide previous window, if any.
                    InfoPanelManager.Panel?.Hide();
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
            if (panel == null)
            {
                return;
            }

            // Destroy game objects.
            GameObject.Destroy(panel);
            GameObject.Destroy(uiGameObject);

            // Let the garbage collector do its work (and also let us know that we've closed the object).
            panel = null;
            uiGameObject = null;

            // Show previous window, if any.
            InfoPanelManager.Panel?.Show();
        }


        /// <summary>
        /// Constructor - creates panel.
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
            minScaleSlider = AddBOBSlider(this, ControlX, MinOffsetY, ControlWidth - (Margin * 2f), "BOB_SCA_MIN", 0.5f, 2f, 0.5f, "MinScale");
            minScaleSlider.eventTrueValueChanged += MinScaleValue;
            minScaleSlider.value = 1f;
            maxScaleSlider = AddBOBSlider(this, ControlX, MaxOffsetY + 40f, ControlWidth - (Margin * 2f), "BOB_SCA_MAX", 0.5f, 2f, 0.5f, "MaxScale");
            maxScaleSlider.eventTrueValueChanged += MaxScaleValue;
            maxScaleSlider.value = 1f;

            // Revert button.
            revertButton = UIButtons.AddSmallerButton(this, ControlX, RevertY, Translations.Translate("BOB_PNL_REV"), ControlWidth);
            revertButton.eventClicked += Revert;
            revertButton.Disable();

            // Loaded prop list.
            UIPanel loadedPanel = AddUIComponent<UIPanel>();
            loadedPanel.width = LoadedWidth;
            loadedPanel.height = ListHeight;
            loadedPanel.relativePosition = new Vector2(LoadedX, ListY);
            loadedList = UIFastList.Create<UILoadedScalingPropRow>(loadedPanel);
            ListSetup(loadedList);

            // Order button.
            m_loadedNameSearchButton = ArrowButton(this, LoadedX + 10f, ListY - 20f);
            m_loadedNameSearchButton.eventClicked += SortLoaded;

            loadedCreatorButton = ArrowButton(this, LoadedX + UILoadedScalingPropRow.CreatorX + 10f, ListY - 20f);
            loadedCreatorButton.eventClicked += SortLoaded;

            // Default is name ascending.
            SetFgSprites(m_loadedNameSearchButton, "IconUpArrow2");

            // Populate loaded list.
            LoadedList();

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
            List<LoadedListItem> list = new List<LoadedListItem>();

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
                            list.Add(new LoadedListItem(loadedTree));
                        }
                    }
                }
            }
            else
            {
                // Prop - iterate through each prop in our list of loaded prefabs.
                foreach (PropInfo loadedProp in PrefabLists.LoadedProps)
                {
                    // Set display name.
                    string displayName = PrefabLists.GetDisplayName(loadedProp);

                    // Apply vanilla filtering if selected.
                    if (!m_hideVanilla.isChecked || !displayName.StartsWith("[v]"))
                    {
                        // Apply name filter.
                        if (!nameFilterActive || displayName.ToLower().Contains(SearchText.Trim().ToLower()))
                        {
                            // Filtering passed - add this prefab to our list.
                            list.Add(new LoadedListItem(loadedProp));
                        }
                    }
                }
            }


            // Create new object list for fastlist, ordering as approprite.
            object[] objectArray;
            switch (m_loadedSearchStatus)
            {
                case (int)OrderBy.NameDescending:
                    objectArray = list.OrderByDescending(item => item.displayName).ToArray();
                    break;
                case (int)OrderBy.CreatorAscending:
                    objectArray = list.OrderBy(item => item.creatorName).ToArray();
                    break;
                case (int)OrderBy.CreatorDescending:
                    objectArray = list.OrderByDescending(item => item.creatorName).ToArray();
                    break;
                default:
                    objectArray = list.OrderBy(item => item.displayName).ToArray();
                    break;
            }

            // Create return fastlist from our filtered list.
            loadedList.rowsData = new FastList<object>
            {
                m_buffer = objectArray,
                m_size = list.Count,
            };

            // Select currently selected prefab, if any.
            if (selectedLoadedPrefab != null)
            {
                loadedList.FindPrefabInItem(selectedLoadedPrefab);
            }
            else
            {
                // No current selection.
                loadedList.selectedIndex = -1;
            }
        }


        /// <summary>
        /// Performs actions required after a change to prop/tree mode.
        /// </summary>
        protected override void PropTreeChange()
        {
            // Reset current item.
            SelectedLoadedPrefab = null;

            // Regenerate lists.
            LoadedList();
        }


        /// <summary>
        /// Loaded list sort button event handler.
        /// <param name="control">Calling component (unused)</param>
        /// <param name="mouseEvent">Mouse event (unused)</param>
        /// </summary>
        protected override void SortLoaded(UIComponent control, UIMouseEventParameter mouseEvent)
        {
            // Check if we are using the name or creator button.
            if (control == m_loadedNameSearchButton)
            {
                // Name button.
                // Toggle status (set to descending if we're currently ascending, otherwise set to ascending).
                if (m_loadedSearchStatus == (int)OrderBy.NameAscending)
                {
                    // Order by name descending.
                    m_loadedSearchStatus = (int)OrderBy.NameDescending;
                }
                else
                {
                    // Order by name ascending.
                    m_loadedSearchStatus = (int)OrderBy.NameAscending;
                }

                // Reset name order buttons.
                SetSortButton(m_loadedNameSearchButton, loadedCreatorButton, m_loadedSearchStatus);
            }
            else if (control == loadedCreatorButton)
            {
                // Creator button.
                // Toggle status (set to descending if we're currently ascending, otherwise set to ascending).
                if (m_loadedSearchStatus == (int)OrderBy.CreatorAscending)
                {
                    // Order by creator descending.
                    m_loadedSearchStatus = (int)OrderBy.CreatorDescending;
                }
                else
                {
                    // Order by name ascending.
                    m_loadedSearchStatus = (int)OrderBy.CreatorAscending;
                }

                // Reset name order buttons.
                SetSortButton(loadedCreatorButton, m_loadedNameSearchButton, m_loadedSearchStatus);
            }


            // Regenerate loaded list.
            LoadedList();
        }


        /// <summary>
        /// Revert button event handler.
        /// <param name="control">Calling component (unused)</param>
        /// <param name="mouseEvent">Mouse event (unused)</param>
        /// </summary>
        private void Revert(UIComponent control, UIMouseEventParameter mouseEvent)
        {
            // Null check.
            if (selectedLoadedPrefab?.name != null)
            {
                // Revert current selection.
                Scaling.Instance.Revert(selectedLoadedPrefab, true);

                // Reset prefab record to reset slider valies.
                SelectedLoadedPrefab = selectedLoadedPrefab;
            }
        }


        /// <summary>
        /// Minimum scale slider event handler.
        /// </summary>
        /// <param name="control">Calling component (unused)</param>
        /// <param name="value">New value</param>
        private void MinScaleValue(UIComponent control, float value)
        {
            // Don't apply changes if events are disabled.
            if (!disableEvents)
            {
                Scaling.Instance.ApplyMinScale(selectedLoadedPrefab, value);
            }
        }


        /// <summary>
        /// Maximum scale slider event handler.
        /// </summary>
        /// <param name="control">Calling component (unused)</param>
        /// <param name="value">New value</param>
        private void MaxScaleValue(UIComponent control, float value)
        {
            // Don't apply changes if events are disabled.
            if (!disableEvents)
            {
                Scaling.Instance.ApplyMaxScale(selectedLoadedPrefab, value);
            }
        }


        /// <summary>
        /// Sets the states of the given sort button to match the given search status.
        /// </summary>
        /// <param name="activeButton">Currently active sort button</param>
        /// <param name="inactiveButton">Inactive button (other sort button for same list)</param>
        /// <param name="searchStatus">Search status to apply</param>
        protected void SetSortButton(UIButton activeButton, UIButton inactiveButton, int searchStatus)
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


        // Trees or props?
        private bool IsTree => PropTreeMode == PropTreeModes.Tree;
    }
}