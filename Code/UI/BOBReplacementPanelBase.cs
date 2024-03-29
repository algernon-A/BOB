﻿// <copyright file="BOBReplacementPanelBase.cs" company="algernon (K. Algernon A. Sheppard)">
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
    internal abstract class BOBReplacementPanelBase : BOBPanelBase
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
        protected UIButton m_applyButton;

        /// <summary>
        /// Target prefab selection list.
        /// </summary>
        protected UIList m_targetList;

        /// <summary>
        /// Replacement prefab selection list.
        /// </summary>
        protected UIList m_replacementList;

        /// <summary>
        /// Label displayed when no props are eligible for current selection.
        /// </summary>
        protected UILabel m_noPropsLabel;

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
        private const float ListHeight = UIList.DefaultRowHeight * 20f;
        private const float ActionHeaderY = ActionsY - 15f;

        // Private components.
        private UIButton _targetNameSortButton;
        private PreviewPanel _previewPanel;

        // Current selections.
        private PrefabInfo _selectedParentPrefab;
        private TargetListItem _selectedTargetItem;
        private PrefabInfo _selectedReplacementPrefab;

        /// <summary>
        /// Gets the panel width.
        /// </summary>
        public override float PanelWidth => RightX + RightWidth + Margin;

        /// <summary>
        /// Gets the panel height.
        /// </summary>
        public override float PanelHeight => ListY + ListHeight + (Margin * 2f);

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
                    PrefabInfo effectivePrefab = _selectedTargetItem.ActivePrefab;

                    // Select current replacement prefab.
                    m_replacementList.FindItem<LoadedPrefabItem>(x => x.Prefab == effectivePrefab);

                    // Set highlighting.
                    RenderOverlays.PropIndex = _selectedTargetItem.PropIndex;
                    RenderOverlays.Prop = effectivePrefab as PropInfo;
                    RenderOverlays.Tree = effectivePrefab as TreeInfo;
                }
                else
                {
                    // No valid current selection - clear selection.
                    m_targetList.SelectedIndex = -1;

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
        /// Gets the panel opacity.
        /// </summary>
        protected override float PanelOpacity => 0.8f;

        /// <summary>
        /// Gets the current individual index number of the current selection.  This could be either the direct index or in the index array, depending on situation.
        /// </summary>
        protected int IndividualIndex => _selectedTargetItem.PropIndex < 0 ? _selectedTargetItem.PropIndexes[0] : _selectedTargetItem.PropIndex;

        /// <summary>
        /// Gets the currently selected building (null if none).
        /// </summary>
        protected BuildingInfo SelectedBuilding => _selectedParentPrefab as BuildingInfo;

        /// <summary>
        /// Gets the currently selected network (null if none).
        /// </summary>
        protected NetInfo SelectedNet => _selectedParentPrefab as NetInfo;

        /// <summary>
        /// Called by Unity before the first frame is displayed.
        /// Used to perform setup.
        /// </summary>
        public override void Start()
        {
            base.Start();
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
                m_targetList = UIList.AddUIList<TargetListItem.DisplayRow>(this, Margin, ListY, LeftWidth, ListHeight);
                m_targetList.EventSelectionChanged += (c, data) => SelectedTargetItem = data as TargetListItem;

                // Replacement prop list.
                m_replacementList = UIList.AddUIList<LoadedPrefabItem.DisplayRow>(this, RightX, ListY, RightWidth, ListHeight);
                m_replacementList.EventSelectionChanged += (c, data) =>
                {
                    if (data is BOBRandomPrefab randomPrefab)
                    {
                        SelectedReplacementPrefab = (PrefabInfo)randomPrefab.Prop ?? randomPrefab.Tree;
                    }
                    else
                    {
                        SelectedReplacementPrefab = (data as LoadedPrefabItem)?.Prefab;
                    }
                };

                // 'No props' label (starts hidden).
                m_noPropsLabel = m_targetList.AddUIComponent<UILabel>();
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
                scaleButton.eventClicked += (c, clickEvent) =>
                {
                    StandalonePanelManager<BOBScalePanel>.Create();
                    StandalonePanelManager<BOBScalePanel>.Panel.SelectPrefab(SelectedReplacementPrefab);
                };

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
                if (m_targetList != null)
                {
                    m_targetList.CurrentPosition = 0;
                    m_targetList.SelectedIndex = -1;
                }

                // Update button states.
                UpdateButtonStates();
            }
        }

        /// <summary>
        /// Refreshes the panel's target list (called when external factors change, e.g. pack replacements).
        /// </summary>
        internal virtual void RefreshTargetList()
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
        /// Performs actions required after a change to prop/tree mode.
        /// </summary>
        protected override void PropTreeChange()
        {
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
            // List of prefabs that have passed filtering.
            List<LoadedPrefabItem> list = new List<LoadedPrefabItem>();

            bool nameFilterActive = !SearchText.IsNullOrWhiteSpace();

            // Add trees, if applicable.
            if (PropTreeMode == PropTreeModes.Tree || PropTreeMode == PropTreeModes.Both)
            {
                // Tree - iterate through each tree in our list of loaded prefabs.
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

            // Add props, if applicable.
            if (PropTreeMode == PropTreeModes.Prop || PropTreeMode == PropTreeModes.Both)
            {
                // Iterate through each prop in our list of loaded prefabs.
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

            // If we're combining trees and props, sort by name to combine them.
            if (PropTreeMode == PropTreeModes.Both)
            {
                Logging.Message("ordering lists");
                list = list.OrderBy(x => x.DisplayName.ToLower()).ToList();
            }

            // Master lists should already be sorted by display name so no need to sort again here.
            // Reverse order of filtered list if we're searching name descending.
            if (m_replacementSortSetting == (int)OrderBy.NameDescending)
            {
                list.Reverse();
            }

            // Create return fastlist from our filtered list.
            m_replacementList.Data = new FastList<object>
            {
                m_buffer = list.ToArray(),
                m_size = list.Count,
            };

            // Select current replacement prefab, if any.
            if (_selectedReplacementPrefab != null)
            {
                m_replacementList.FindItem<LoadedPrefabItem>(x => x.Prefab == _selectedReplacementPrefab);
            }
        }

        /// <summary>
        /// Performs actions to be taken once an update (application or reversion) has been applied, including saving data, updating button states, and refreshing renders.
        /// </summary>
        protected virtual void FinishUpdate()
        {
            // Save configuration file and refresh target list (to reflect our changes).
            ConfigurationUtils.SaveConfig();

            // Regenerate target list.
            RegenerateTargetList();

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
