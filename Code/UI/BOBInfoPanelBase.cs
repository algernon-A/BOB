// <copyright file="BOBInfoPanelBase.cs" company="algernon (K. Algernon A. Sheppard)">
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
    /// Abstract base class for all BOB tree/prop replacement panels.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Protected fields")]
    internal abstract class BOBInfoPanelBase : BOBPanelBase
    {
        /// <summary>
        /// Middle layout column relative X position.
        /// </summary>
        protected const float MiddleX = LeftWidth + Margin;

        /// <summary>
        /// Middle layout column width.
        /// </summary>
        protected const float MidControlX = MiddleX + Margin;

        /// <summary>
        /// Middle layout column control width.
        /// </summary>
        protected const float MidControlWidth = ActionSize * 3f;

        /// <summary>
        /// Selection list bottom relative Y position.
        /// </summary>
        protected const float ListBottom = ListY + ListHeight;

        /// <summary>
        /// Action controls relative Y position.
        /// </summary>
        protected const float ActionsY = ListY;

        /// <summary>
        /// Action button size.
        /// </summary>
        protected const float ActionSize = 48f;

        /// <summary>
        /// Apply changes button.
        /// </summary>
        protected readonly UIButton m_applyButton;

        /// <summary>
        /// Target prefab selection list.
        /// </summary>
        protected readonly UIFastList m_targetList;

        /// <summary>
        /// Replacement prefab selection list.
        /// </summary>
        protected readonly UIFastList m_replacementList;

        /// <summary>
        /// Label displayed when no props are eligible for current selection.
        /// </summary>
        protected readonly UILabel m_noPropsLabel;

        /// <summary>
        /// Target list sorting setting.
        /// </summary>
        protected int m_targetSortSetting;

        /// <summary>
        /// Revert changes button.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Protected fields")]
        protected UIButton m_revertButton;

        // Layout constants - X.
        private const float LeftWidth = 400f;
        private const float MiddleWidth = MidControlWidth + (Margin * 2f);
        private const float RightX = MiddleX + MiddleWidth;
        private const float RightWidth = 320f;

        // Layout constants - Y.
        private const float ListY = FilterY + FilterHeight;
        private const float ListHeight = UIPropRow.RowHeight * 18f;
        private const float ActionHeaderY = ActionsY - 15f;

        // Private components.
        private readonly UIButton _targetNameSortButton;
        private readonly PreviewPanel _previewPanel;
        private readonly UIPanel _rightPanel;

        // Current selections.
        private PrefabInfo _selectedParentPrefab;
        private TargetListItem _selectedTargetItem;
        private PrefabInfo _selectedReplacementPrefab;

        /// <summary>
        /// Initializes a new instance of the <see cref="BOBInfoPanelBase"/> class.
        /// </summary>
        internal BOBInfoPanelBase()
        {
            try
            {
                // Position - are we restoring the previous position?.
                if (ModSettings.RememberPosition && (BOBPanelManager.PreviousX != 0f || BOBPanelManager.PreviousY != 0f))
                {
                    // 'Remember default position' is active and at least one of X and Y positions is non-zero.
                    relativePosition = new Vector2(BOBPanelManager.PreviousX, BOBPanelManager.PreviousY);
                }
                else
                {
                    // Default position - centre in screen.
                    relativePosition = new Vector2(Mathf.Floor((GetUIView().fixedWidth - width) / 2), Mathf.Floor((GetUIView().fixedHeight - height) / 2));
                }

                // Order buttons.
                _targetNameSortButton = ArrowButton(this, 30f, FilterY);
                m_replacementNameSortButton = ArrowButton(this, RightX + 10f, FilterY);

                _targetNameSortButton.eventClicked += SortTargets;
                m_replacementNameSortButton.eventClicked += SortReplacements;

                // Default is name ascending.
                SetFgSprites(_targetNameSortButton, "IconUpArrow2");
                SetFgSprites(m_replacementNameSortButton, "IconUpArrow2");

                // Target prop list.
                UIPanel leftPanel = AddUIComponent<UIPanel>();
                leftPanel.width = LeftWidth;
                leftPanel.height = ListHeight;
                leftPanel.relativePosition = new Vector2(Margin, ListY);
                m_targetList = UIFastList.Create<UIPrefabPropRow>(leftPanel);
                ListSetup(m_targetList);

                // Replacement prop list.
                _rightPanel = AddUIComponent<UIPanel>();
                _rightPanel.width = RightWidth;
                _rightPanel.height = ListHeight;
                _rightPanel.relativePosition = new Vector2(RightX, ListY);
                m_replacementList = UIFastList.Create<UILoadedPropRow>(_rightPanel);
                ListSetup(m_replacementList);

                // 'No props' label (starts hidden).
                m_noPropsLabel = leftPanel.AddUIComponent<UILabel>();
                m_noPropsLabel.relativePosition = new Vector2(Margin, Margin);
                m_noPropsLabel.text = Translations.Translate(PropTreeMode == PropTreeModes.Tree ? "BOB_PNL_NOT" : "BOB_PNL_NOP");
                m_noPropsLabel.Hide();

                // Actions text label.
                UILabel actionsLabel = UILabels.AddLabel(this, MidControlX, ActionHeaderY, Translations.Translate("BOB_PNL_ACT"), textScale: 0.8f);

                // Apply button.
                m_applyButton = AddIconButton(this, MidControlX, ActionsY, ActionSize, "BOB_PNL_APP", UITextures.LoadQuadSpriteAtlas("BOB-OkSmall"));
                m_applyButton.eventClicked += Apply;

                // Revert button.
                m_revertButton = AddIconButton(this, MidControlX + ActionSize, ActionsY, ActionSize, "BOB_PNL_REV", UITextures.LoadQuadSpriteAtlas("BOB-Revert"));
                m_revertButton.eventClicked += Revert;

                // Extra functions label.
                UILabel functionsLabel = UILabels.AddLabel(this, MiddleX, ToggleHeaderY, Translations.Translate("BOB_PNL_FUN"), textScale: 0.8f);

                // Scale button.
                UIButton scaleButton = AddIconButton(this, MiddleX, ToggleY, ToggleSize, "BOB_PNL_SCA", UITextures.LoadQuadSpriteAtlas("BOB-Scale"));
                scaleButton.eventClicked += (control, clickEvent) => BOBScalePanel.Create(PropTreeMode, _selectedReplacementPrefab);

                // Preview image.
                _previewPanel = AddUIComponent<PreviewPanel>();
                _previewPanel.relativePosition = new Vector2(this.width + Margin, ListY);

                Logging.Message("InfoPanelBase constructor complete");
            }
            catch (Exception e)
            {
                // Log and report any exception.
                Logging.LogException(e, "exception creating base info panel");
            }
        }

        /// <summary>
        /// Gets or sets the current target item and updates button states accordingly.
        /// </summary>
        internal virtual TargetListItem SelectedTargetItem
        {
            get => _selectedTargetItem;

            set
            {
                _selectedTargetItem = value;

                // Check if actual item has been set.
                if (_selectedTargetItem != null)
                {
                    PrefabInfo effectivePrefab = _selectedTargetItem.individualPrefab ?? _selectedTargetItem.replacementPrefab ?? _selectedTargetItem.allPrefab ?? _selectedTargetItem.originalPrefab;

                    // Select current replacement prefab.
                    m_replacementList.FindItem(effectivePrefab);

                    // Set current panel selection.
                    SelectedReplacementPrefab = effectivePrefab;

                    // Set highlighting.
                    RenderOverlays.PropIndex = _selectedTargetItem.index;
                    RenderOverlays.Prop = effectivePrefab as PropInfo;
                    RenderOverlays.Tree = effectivePrefab as TreeInfo;
                }
                else
                {
                    // No valid current selection - clear selection.
                    m_targetList.selectedIndex = -1;

                    // Clear highlighting.
                    RenderOverlays.PropIndex = -1;
                    RenderOverlays.Lane = null;
                    RenderOverlays.Prop = null;
                    RenderOverlays.Tree = null;
                }

                UpdateButtonStates();
            }
        }

        /// <summary>
        /// Gets or sets the current replacement prefab and updates button states accordingly.
        /// </summary>
        internal virtual PrefabInfo SelectedReplacementPrefab
        {
            get => _selectedReplacementPrefab;

            set
            {
                _selectedReplacementPrefab = value;
                UpdateButtonStates();

                // Update preview.
                _previewPanel.SetTarget(value);
            }
        }

        /// <summary>
        /// Gets the panel width.
        /// </summary>
        protected override float PanelWidth => RightX + RightWidth + Margin;

        /// <summary>
        /// Gets the panel height.
        /// </summary>
        protected override float PanelHeight => ListY + ListHeight + (Margin * 2f);

        /// <summary>
        /// Gets the panel opacity.
        /// </summary>
        protected override float PanelOpacity => 0.8f;

        /// <summary>
        /// Gets the current individual index number of the current selection.  This could be either the direct index or in the index array, depending on situation.
        /// </summary>
        protected int IndividualIndex => _selectedTargetItem.index < 0 ? _selectedTargetItem.indexes[0] : _selectedTargetItem.index;

        /// <summary>
        /// Gets the currently selected building (null if none).
        /// </summary>
        protected BuildingInfo SelectedBuilding => _selectedParentPrefab as BuildingInfo;

        /// <summary>
        /// Gets the currently selected network (null if none).
        /// </summary>
        protected NetInfo SelectedNet => _selectedParentPrefab as NetInfo;

        /// <summary>
        /// Called by the game every update cycle.
        /// Handles overlay intensity modifier keys, and checks for and displays any exception message.
        /// </summary>
        public override void Update()
        {
            base.Update();

            // Check for exceptions.
            BOBPanelManager.CheckException();

            // Check for overlay intensity modifier keys.
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr))
            {
                // Alt - disable overlays.
                RenderOverlays.Intensity = 0f;
            }
            else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                // Shift - half-intensity.
                RenderOverlays.Intensity = 0.5f;
            }
            else
            {
                // Default - full intensity.
                RenderOverlays.Intensity = 1f;
            }
        }

        /// <summary>
        /// Updates all items in the target list.
        /// </summary>
        internal void UpdateTargetList()
        {
            // Iterate through each item in list.
            foreach (object item in m_targetList.rowsData)
            {
                if (item is TargetListItem targetListItem)
                {
                    // Update status.
                    UpdateTargetItem(targetListItem);
                }
            }

            // Refresh list display.
            m_targetList.Refresh();
        }

        /// <summary>
        /// Sets the target parent prefab.
        /// </summary>
        /// <param name="targetPrefabInfo">Target prefab to set.</param>
        internal virtual void SetTargetParent(PrefabInfo targetPrefabInfo)
        {
            // Don't do anything if we're already selected.
            if (_selectedParentPrefab != targetPrefabInfo)
            {
                // Set target reference.
                _selectedParentPrefab = targetPrefabInfo;

                // Clear selection.
                SelectedTargetItem = null;
                m_targetList.listPosition = 0;
                m_targetList.selectedIndex = -1;

                // Update button states.
                UpdateButtonStates();
            }
        }

        /// <summary>
        /// Performs any actions-on-close for the panel.
        /// </summary>
        internal virtual void Close()
        {
        }

        /// <summary>
        /// Regenerates the target fastlist with a list of target-specific trees or props.
        /// </summary>
        protected abstract void RegenerateTargetList();

        /// <summary>
        /// Updates button states (enabled/disabled) according to current control states.
        /// </summary>
        protected abstract void UpdateButtonStates();

        /// <summary>
        /// Apply button event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="p">Mouse event parameter.</param>
        protected abstract void Apply(UIComponent c, UIMouseEventParameter p);

        /// <summary>
        /// Revert button event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="p">Mouse event parameter.</param>
        protected abstract void Revert(UIComponent c, UIMouseEventParameter p);

        /// <summary>
        /// Close button event handler.
        /// </summary>
        protected override void CloseEvent() => BOBPanelManager.Close();

        /// <summary>
        /// Updates the target item record for changes in replacement status (e.g. after applying or reverting changes).
        /// </summary>
        /// <param name="targetListItem">Target item.</param>
        protected virtual void UpdateTargetItem(TargetListItem targetListItem)
        {
        }

        /// <summary>
        /// Performs actions required after a change to prop/tree mode.
        /// </summary>
        protected override void PropTreeChange()
        {
            // Reset current items.
            SelectedTargetItem = null;
            SelectedReplacementPrefab = null;

            // Set 'no props' label text.
            m_noPropsLabel.text = Translations.Translate(PropTreeMode == PropTreeModes.Tree ? "BOB_PNL_NOT" : "BOB_PNL_NOP");

            // Regenerate lists.
            RegenerateReplacementList();
            RegenerateTargetList();

            // Update button states.
            UpdateButtonStates();
        }

        /// <summary>
        /// Populates the replacement UIList with a filtered list of eligible relacement trees or props.
        /// </summary>
        protected override void RegenerateReplacementList()
        {
            // Clear current selection.
            m_replacementList.selectedIndex = -1;

            // List of prefabs that have passed filtering.
            List<PrefabInfo> list = new List<PrefabInfo>();

            bool nameFilterActive = !SearchText.IsNullOrWhiteSpace();

            // Add trees, if applicable.
            if (PropTreeMode == PropTreeModes.Tree || PropTreeMode == PropTreeModes.Both)
            {
                // Tree - iterate through each tree in our list of loaded prefabs.
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

            // Add props, if applicable.
            if (PropTreeMode == PropTreeModes.Prop || PropTreeMode == PropTreeModes.Both)
            {
                // Iterate through each prop in our list of loaded prefabs.
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
                            list.Add(loadedProp);
                        }
                    }
                }
            }

            // If we're combining trees and props, sort by name to combine them.
            if (PropTreeMode == PropTreeModes.Both)
            {
                Logging.Message("ordering lists");
                list = list.OrderBy(x => PrefabLists.GetDisplayName(x.name).ToLower()).ToList();
            }

            // Master lists should already be sorted by display name so no need to sort again here.
            // Reverse order of filtered list if we're searching name descending.
            if (m_replacementSortSetting == (int)OrderBy.NameDescending)
            {
                list.Reverse();
            }

            // Create return fastlist from our filtered list.
            m_replacementList.rowsData = new FastList<object>
            {
                m_buffer = list.ToArray(),
                m_size = list.Count,
            };

            // Select current replacement prefab, if any.
            if (_selectedReplacementPrefab != null)
            {
                m_replacementList.FindItem(_selectedReplacementPrefab);
            }
            else
            {
                // No current selection.
                m_replacementList.selectedIndex = -1;
            }
        }

        /// <summary>
        /// Performs actions to be taken once an update (application or reversion) has been applied, including saving data, updating button states, and refreshing renders.
        /// </summary>
        protected virtual void FinishUpdate()
        {
            // Save configuration file and refresh target list (to reflect our changes).
            ConfigurationUtils.SaveConfig();
            UpdateTargetList();

            // Update button states.
            UpdateButtonStates();

            // Refresh current target item to update highlighting.
            SelectedTargetItem = SelectedTargetItem;
        }

        /// <summary>
        /// Hide vanilla check event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="isChecked">New checked state.</param>
        protected override void VanillaCheckChanged(UIComponent c, bool isChecked)
        {
            // Filter list.
            base.VanillaCheckChanged(c, isChecked);

            // Store state.
            ModSettings.HideVanilla = isChecked;
        }

        /// <summary>
        /// Target list sort button event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="p">Mouse event parameter.</param>
        private void SortTargets(UIComponent c, UIMouseEventParameter p)
        {
            // Toggle status (set to descending if we're currently ascending, otherwise set to ascending).
            if (m_targetSortSetting == (int)OrderBy.NameAscending)
            {
                // Order by name descending.
                m_targetSortSetting = (int)OrderBy.NameDescending;
            }
            else
            {
                // Order by name ascending.
                m_targetSortSetting = (int)OrderBy.NameAscending;
            }

            // Reset name order buttons.
            SetSortButton(c as UIButton, m_targetSortSetting);

            // Regenerate target list.
            RegenerateTargetList();
        }
    }
}
