// <copyright file="BOBBuildingPanel.cs" company="algernon (K. Algernon A. Sheppard)">
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
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// BOB building tree/prop replacement panel.
    /// </summary>
    internal sealed class BOBBuildingPanel : BOBInfoPanel
    {
        // Original selection values.
        private readonly List<BuildingPropHandler> _originalValues = new List<BuildingPropHandler>();

        // Panel components.
        private readonly UICheckBox _customHeightCheck;
        private UIPanel _subBuildingPanel;
        private UIFastList _subBuildingList;

        // Sub-buildings.
        private BuildingInfo _selectedSubBuilding;
        private BuildingInfo[] _subBuildings;
        private string[] _subBuildingNames;

        /// <summary>
        /// Initializes a new instance of the <see cref="BOBBuildingPanel"/> class.
        /// </summary>
        internal BOBBuildingPanel()
        {
            try
            {
                // Fixed height checkbox.
                _customHeightCheck = UICheckBoxes.AddLabelledCheckBox(m_heightPanel, Margin, FixedHeightY, Translations.Translate("BOB_PNL_CUH"), tooltip: Translations.Translate("BOB_PNL_CUH_TIP"));
                _customHeightCheck.eventCheckChanged += CustomHeightChange;

                // Adjust y-slider position and panel height.
                m_ySlider.relativePosition += new Vector3(0f, 20f);
                m_ySlider.ValueField.relativePosition += new Vector3(0f, 20f);
                m_heightPanel.height = HeightPanelBottomY;

                // Regenerate replacement list.
                RegenerateReplacementList();

                // Update button states.
                UpdateButtonStates();
            }
            catch (Exception e)
            {
                // Log and report any exception.
                Logging.LogException(e, "exception creating building panel");
            }
        }

        /// <summary>
        /// Sets the current target item and updates button states accordingly.
        /// </summary>
        internal override TargetListItem SelectedTargetItem
        {
            set
            {
                if (value is BuildingTargetListItem buildingTargetListItem)
                {
                    // First, undo any preview.
                    RevertPreview();

                    // Call base, while ignoring replacement prefab change live application.
                    m_ignoreSelectedPrefabChange = true;
                    base.SelectedTargetItem = value;
                    m_ignoreSelectedPrefabChange = false;

                    // Ensure valid selection before proceeding.
                    if (_selectedSubBuilding != null)
                    {
                        // Set custom height checkbox.
                        _customHeightCheck.isChecked = _selectedSubBuilding.m_props[IndividualIndex].m_fixedHeight;

                        // Is this an added prop?
                        if (buildingTargetListItem.isAdded)
                        {
                            Logging.Message("setting sliders for added prop at index ", IndividualIndex);

                            // Yes - set sliders directly.
                            // Disable events.
                            m_ignoreSliderValueChange = true;

                            // Set slider values.
                            BuildingInfo.Prop buildingProp = _selectedSubBuilding.m_props[IndividualIndex];
                            m_rotationSlider.TrueValue = buildingProp.m_radAngle * Mathf.Rad2Deg;
                            m_xSlider.TrueValue = buildingProp.m_position.x;
                            m_ySlider.TrueValue = buildingProp.m_position.y;
                            m_zSlider.TrueValue = buildingProp.m_position.z;
                            m_probabilitySlider.TrueValue = buildingProp.m_probability;

                            // Re-enable events.
                            m_ignoreSliderValueChange = false;

                            // All done here.
                            return;
                        }
                        else
                        {
                            // Set sliders according to highest active replacement (will be null if none).
                            SetSliders(buildingTargetListItem.IndividualReplacement ?? buildingTargetListItem.GroupedReplacement ?? buildingTargetListItem.AllReplacement);
                            return;
                        }
                    }

                    // If we got here, there's no valid current selection; set all offset fields to defaults by passing null to SetSliders().
                    SetSliders(null);
                }
            }
        }

        /// <summary>
        /// Gets the mode icon atlas names for prop modes.
        /// </summary>
        protected override string[] PropModeAtlas => new string[(int)ReplacementModes.NumModes]
        {
            "BOB-ThisPropSmall",
            "BOB-SamePropSmall",
            "BOB-BuildingsSmall",
        };

        /// <summary>
        /// Gets the mode icon atlas names for tree modes.
        /// </summary>
        protected override string[] TreeModeAtlas => new string[(int)ReplacementModes.NumModes]
        {
            "BOB-ThisTreeSmall",
            "BOB-SameTreeSmall",
            "BOB-BuildingsSmall",
        };

        /// <summary>
        /// Gets the mode icon tooltip keys for prop modes.
        /// </summary>
        protected override string[] PropModeTipKeys => new string[(int)ReplacementModes.NumModes]
        {
            "BOB_PNL_M_PIB",
            "BOB_PNL_M_PGB",
            "BOB_PNL_M_PAB",
        };

        /// <summary>
        /// Gets the mode icon tooltip keys for tree modes.
        /// </summary>
        protected override string[] TreeModeTipKeys => new string[(int)ReplacementModes.NumModes]
        {
            "BOB_PNL_M_TIB",
            "BOB_PNL_M_TGB",
            "BOB_PNL_M_TAB",
        };

        /// <summary>
        /// Sets the current replacement mode.
        /// </summary>
        protected override ReplacementModes CurrentMode
        {
            set
            {
                base.CurrentMode = value;

                // Show/hide add new prop button based on mode.
                bool eligibleMode = CurrentMode == ReplacementModes.Individual | CurrentMode == ReplacementModes.Grouped;
                m_addButton.isVisible = eligibleMode;
                m_removeButton.isVisible = eligibleMode;
            }
        }

        /// <summary>
        /// Sets the target parent prefab.
        /// </summary>
        /// <param name="targetPrefabInfo">Target prefab to set.</param>
        internal override void SetTargetParent(PrefabInfo targetPrefabInfo)
        {
            // Don't do anything if invalid target, or target hasn't changed.
            if (!(targetPrefabInfo is BuildingInfo) || SelectedBuilding == targetPrefabInfo)
            {
                return;
            }

            // Base setup.
            base.SetTargetParent(targetPrefabInfo);

            // Set target reference.
            _selectedSubBuilding = targetPrefabInfo as BuildingInfo;

            // Does this building have sub-buildings?
            if (_selectedSubBuilding.m_subBuildings != null && _selectedSubBuilding.m_subBuildings.Length > 0)
            {
                // Yes - create lists of sub-buildings (names and infos).
                int numSubs = _selectedSubBuilding.m_subBuildings.Length;
                int numChoices = numSubs + 1;
                _subBuildingNames = new string[numChoices];
                _subBuildings = new BuildingInfo[numChoices];
                _subBuildingNames[0] = PrefabLists.GetDisplayName(_selectedSubBuilding);
                _subBuildings[0] = _selectedSubBuilding;

                object[] subBuildingIndexes = new object[numChoices];
                subBuildingIndexes[0] = 0;

                for (int i = 0; i < numSubs; ++i)
                {
                    _subBuildingNames[i + 1] = PrefabLists.GetDisplayName(_selectedSubBuilding.m_subBuildings[i].m_buildingInfo);
                    _subBuildings[i + 1] = _selectedSubBuilding.m_subBuildings[i].m_buildingInfo;
                    subBuildingIndexes[i + 1] = i + 1;
                }

                // Add sub-building menu, if it doesn't already exist.
                if (_subBuildingPanel == null)
                {
                    _subBuildingPanel = this.AddUIComponent<UIPanel>();

                    // Basic behaviour.
                    _subBuildingPanel.autoLayout = false;
                    _subBuildingPanel.canFocus = true;
                    _subBuildingPanel.isInteractive = true;

                    // Appearance.
                    _subBuildingPanel.backgroundSprite = "MenuPanel2";
                    _subBuildingPanel.opacity = PanelOpacity;

                    // Size and position.
                    _subBuildingPanel.size = new Vector2(200f, PanelHeight - TitleHeight);
                    _subBuildingPanel.relativePosition = new Vector2(-205f, TitleHeight);

                    // Heading.
                    UILabel subTitleLabel = UILabels.AddLabel(_subBuildingPanel, 5f, 5f, Translations.Translate("BOB_PNL_SUB"), 190f);
                    subTitleLabel.textAlignment = UIHorizontalAlignment.Center;
                    subTitleLabel.relativePosition = new Vector2(5f, (TitleHeight - subTitleLabel.height) / 2f);

                    // List panel.
                    UIPanel subBuildingListPanel = _subBuildingPanel.AddUIComponent<UIPanel>();
                    subBuildingListPanel.relativePosition = new Vector2(Margin, TitleHeight);
                    subBuildingListPanel.width = _subBuildingPanel.width - (Margin * 2f);
                    subBuildingListPanel.height = _subBuildingPanel.height - TitleHeight - (Margin * 2f);
                    _subBuildingList = UIFastList.Create<UISubBuildingRow>(subBuildingListPanel);
                    ListSetup(_subBuildingList);

                    // Create return fastlist from our filtered list.
                    _subBuildingList.rowsData = new FastList<object>
                    {
                        m_buffer = subBuildingIndexes,
                        m_size = subBuildingIndexes.Length,
                    };
                }
                else
                {
                    // If the sub-building panel has already been created. just make sure it's visible.
                    _subBuildingPanel.Show();
                }
            }
            else
            {
                // Otherwise, hide the sub-building panel (if it exists).
                _subBuildingPanel?.Hide();
            }

            // Regenerate target list and select target item.
            RegenerateTargetList();

            // Record original stats for preview.
            RecordOriginal();

            // Apply Harmony rendering patches.
            RenderOverlays.Building = _selectedSubBuilding;
            Patcher.Instance.PatchBuildingOverlays(true);
        }

        /// <summary>
        /// Previews the current change.
        /// </summary>
        protected override void PreviewChange()
        {
            // Don't do anything if no current selection.
            if (SelectedTargetItem == null)
            {
                return;
            }

            // Don't do anything if no changes.
            if (m_xSlider.value == 0f &&
                m_ySlider.value == 0f &&
                m_zSlider.value == 0f &&
                m_rotationSlider.value == 0f &&
                m_probabilitySlider.value.RoundToNearest(1) == SelectedTargetItem.originalProb &&
                SelectedReplacementPrefab == SelectedTargetItem.CurrentPrefab)
            {
                // Reset apply button icon.
                UnappliedChanges = false;

                return;
            }

            // Generate prevew record entry.
            BOBConfig.BuildingReplacement previewReplacement = new BOBConfig.BuildingReplacement
            {
                ReplacementInfo = SelectedReplacementPrefab ?? SelectedTargetItem.originalPrefab,
                OffsetX = m_xSlider.TrueValue,
                OffsetY = m_ySlider.TrueValue,
                OffsetZ = m_zSlider.TrueValue,
                Angle = m_rotationSlider.TrueValue,
                Probability = (int)m_probabilitySlider.TrueValue.RoundToNearest(1),
                CustomHeight = _customHeightCheck.isChecked,
            };

            // Update preview for each handler.
            foreach (BuildingPropHandler handler in _originalValues)
            {
                PreviewChange(handler, previewReplacement);
            }

            // Update renders.
            BuildingData.Update();

            // Update highlighting target.
            RenderOverlays.Prop = SelectedReplacementPrefab as PropInfo;
            RenderOverlays.Tree = SelectedReplacementPrefab as TreeInfo;

            // Update apply button icon to indicate change.
            UnappliedChanges = true;
        }

        /// <summary>
        /// Reverts any previewed changes back to original prop/tree state.
        /// </summary>
        protected override void RevertPreview()
        {
            // Iterate through each original value.
            foreach (BuildingPropHandler handler in _originalValues)
            {
                // Restore original values.
                handler.ClearPreview();
            }

            // Update prefabs.
            BuildingData.Update();

            // Reset apply button icon.
            UnappliedChanges = false;
        }

        /// <summary>
        /// Apply button event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="p">Mouse event parameter.</param>
        protected override void Apply(UIComponent c, UIMouseEventParameter p)
        {
            // First, undo any preview.
            RevertPreview();

            try
            {
                // Make sure we have valid a target and replacement.
                if (SelectedTargetItem is BuildingTargetListItem buildingTargetListItem && SelectedReplacementPrefab != null)
                {
                    // Check for added prop - instead of replacing, we update the original added prop reference.
                    if (SelectedTargetItem.isAdded)
                    {
                        AddedBuildingProps.Instance.Update(_selectedSubBuilding, SelectedTargetItem.originalPrefab, SelectedReplacementPrefab, SelectedTargetItem.index, m_rotationSlider.TrueValue, m_xSlider.TrueValue, m_ySlider.TrueValue, m_zSlider.TrueValue, (int)m_probabilitySlider.TrueValue, _customHeightCheck.isChecked);

                        // Update current target.
                        SelectedTargetItem.originalPrefab = SelectedReplacementPrefab;
                        SelectedTargetItem.originalProb = (int)m_probabilitySlider.TrueValue;
                    }
                    else
                    {
                        // Not an added prop.
                        switch (CurrentMode)
                        {
                            case ReplacementModes.Individual:
                                // Individual replacement.
                                IndividualBuildingReplacement.Instance.Replace(_selectedSubBuilding, SelectedTargetItem.originalPrefab, SelectedReplacementPrefab, IndividualIndex, m_rotationSlider.TrueValue, m_xSlider.TrueValue, m_ySlider.TrueValue, m_zSlider.TrueValue, (int)m_probabilitySlider.TrueValue, _customHeightCheck.isChecked, buildingTargetListItem.IndividualReplacement);
                                break;

                            case ReplacementModes.Grouped:
                                // Grouped replacement.
                                GroupedBuildingReplacement.Instance.Replace(_selectedSubBuilding, SelectedTargetItem.originalPrefab, SelectedReplacementPrefab, -1, m_rotationSlider.TrueValue, m_xSlider.TrueValue, m_ySlider.TrueValue, m_zSlider.TrueValue, (int)m_probabilitySlider.TrueValue, _customHeightCheck.isChecked, buildingTargetListItem.GroupedReplacement);
                                break;

                            case ReplacementModes.All:
                                // All- replacement.
                                AllBuildingReplacement.Instance.Replace(null, SelectedTargetItem.originalPrefab, SelectedReplacementPrefab, -1, m_rotationSlider.TrueValue, m_xSlider.TrueValue, m_ySlider.TrueValue, m_zSlider.TrueValue, (int)m_probabilitySlider.TrueValue, _customHeightCheck.isChecked, buildingTargetListItem.AllReplacement);
                                break;

                            default:
                                Logging.Error("invalid replacement mode at BuildingInfoPanel.Apply");
                                return;
                        }
                    }

                    // Update any dirty building renders.
                    BuildingData.Update();

                    // Record updated original data.
                    RecordOriginal();

                    // Update target list and buttons.
                    RegenerateTargetList();
                    UpdateButtonStates();

                    // Update highlighting target.
                    RenderOverlays.Prop = SelectedReplacementPrefab as PropInfo;
                    RenderOverlays.Tree = SelectedReplacementPrefab as TreeInfo;

                    // Perform post-replacement processing.
                    FinishUpdate();
                }
            }
            catch (Exception e)
            {
                // Log and report any exception.
                Logging.LogException(e, "exception applying building replacement");
            }
        }

        /// <summary>
        /// Revert button event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="p">Mouse event parameter.</param>
        protected override void Revert(UIComponent c, UIMouseEventParameter p)
        {
            // Revert any unapplied changes first.
            if (UnappliedChanges)
            {
                // Reset slider values by reassigning the current target item - this will also revert any preview.
                SelectedTargetItem = SelectedTargetItem;
                return;
            }

            try
            {
                // Make sure we've got a valid selection.
                if (SelectedTargetItem is BuildingTargetListItem buildingTargetListItem)
                {
                    // Individual building prop reversion?
                    if (buildingTargetListItem.IndividualReplacement != null)
                    {
                        // Individual reversion.
                        IndividualBuildingReplacement.Instance.RemoveReplacement(buildingTargetListItem.IndividualReplacement);
                    }
                    else if (buildingTargetListItem.GroupedReplacement != null)
                    {
                        // Grouped reversion.
                        GroupedBuildingReplacement.Instance.RemoveReplacement(buildingTargetListItem.GroupedReplacement);
                    }
                    else if (buildingTargetListItem.AllReplacement != null)
                    {
                        // All-building reversion - make sure we've got a currently active replacement before doing anything.
                        if (buildingTargetListItem.originalPrefab)
                        {
                            // All-building reversion.
                            AllBuildingReplacement.Instance.RemoveReplacement(buildingTargetListItem.AllReplacement);
                        }
                    }

                    // Re-record originals (need to do this before updating controls).
                    RecordOriginal();

                    // Update target list.
                    RegenerateTargetList();

                    // Perform post-replacement processing.
                    FinishUpdate();
                }
            }
            catch (Exception e)
            {
                // Log and report any exception.
                Logging.LogException(e, "exception performing building reversion");
            }
        }

        /// <summary>
        /// Updates the target item record for changes in replacement status (e.g. after applying or reverting changes).
        /// </summary>
        /// <param name="targetListItem">Target item.</param>
        protected override void UpdateTargetItem(TargetListItem targetListItem)
        {
            if (targetListItem is BuildingTargetListItem buildingItem)
            {
                // Determine index to test - if no individual index, just grab first one from list.
                int propIndex = targetListItem.index;
                if (propIndex < 0)
                {
                    propIndex = targetListItem.indexes[0];
                }

                // Is this an added prop?
                if (AddedBuildingProps.Instance.IsAdded(_selectedSubBuilding, propIndex))
                {
                    targetListItem.index = propIndex;
                    targetListItem.isAdded = true;
                }
                else
                {
                    // Non-added prop; update stored references.
                    BuildingPropHandler handler = BuildingHandlers.GetHandler(_selectedSubBuilding, propIndex);
                    if (handler != null)
                    {
                        buildingItem.IndividualReplacement = handler.GetReplacement(ReplacementPriority.IndividualReplacement);
                        buildingItem.GroupedReplacement = handler.GetReplacement(ReplacementPriority.GroupedReplacement);
                        buildingItem.AllReplacement = handler.GetReplacement(ReplacementPriority.AllReplacement);
                    }
                }
            }
        }

        /// <summary>
        /// Regenerates the target fastlist with a list of target-specific trees or props.
        /// </summary>
        protected override void RegenerateTargetList()
        {
            // Clear current selection.
            m_targetList.selectedIndex = -1;

            // List of prefabs that have passed filtering.
            List<TargetListItem> itemList = new List<TargetListItem>();

            // Check to see if this building contains any props.
            if (_selectedSubBuilding.m_props == null || _selectedSubBuilding.m_props.Length == 0)
            {
                // No props - show 'no props' label and return an empty list.
                m_noPropsLabel.Show();
                m_targetList.rowsData = new FastList<object>();

                // Force clearance of current target item.
                SelectedTargetItem = null;

                return;
            }

            // Iterate through each prop in building.
            for (int propIndex = 0; propIndex < _selectedSubBuilding.m_props.Length; ++propIndex)
            {
                // Create new list item.
                BuildingTargetListItem targetListItem = new BuildingTargetListItem();

                // Try to get relevant prefab (prop/tree), falling back to the other type if null (to allow for tree-prop changes), using finalProp.
                PrefabInfo originalInfo = null;
                if (PropTreeMode == PropTreeModes.Tree)
                {
                    originalInfo = (PrefabInfo)_selectedSubBuilding.m_props[propIndex]?.m_tree ?? _selectedSubBuilding.m_props[propIndex]?.m_prop;
                }
                else
                {
                    originalInfo = (PrefabInfo)_selectedSubBuilding.m_props[propIndex]?.m_prop ?? _selectedSubBuilding.m_props[propIndex]?.m_tree;
                }

                // Check to see if we were succesful - if not (e.g. we only want trees and this is a prop), continue on to next building prop.
                if (originalInfo?.name == null)
                {
                    continue;
                }

                // Get original (pre-replacement) tree/prop prefab and current probability (as default original probability).
                targetListItem.originalPrefab = originalInfo;
                targetListItem.originalProb = _selectedSubBuilding.m_props[propIndex].m_probability;
                targetListItem.originalAngle = _selectedSubBuilding.m_props[propIndex].m_radAngle * Mathf.Rad2Deg;

                // Is this an added prop?
                if (AddedBuildingProps.Instance.IsAdded(_selectedSubBuilding, propIndex))
                {
                    targetListItem.index = propIndex;
                    targetListItem.isAdded = true;
                }
                else
                {
                    // Non-added prop - see if we've got an existing reference.
                    BuildingPropHandler handler = BuildingHandlers.GetHandler(_selectedSubBuilding, propIndex);
                    if (handler != null)
                    {
                        // Existing reference found - get the relevant original prefab name.
                        originalInfo = (PrefabInfo)handler.OriginalProp ?? handler.OriginalTree;

                        // Check to see if we were succesful - if not (e.g. we only want trees and this is a prop), continue on to next building prop.
                        if (originalInfo?.name == null)
                        {
                            continue;
                        }

                        // Record active replacements.
                        targetListItem.IndividualReplacement = handler.GetReplacement(ReplacementPriority.IndividualReplacement);
                        targetListItem.GroupedReplacement = handler.GetReplacement(ReplacementPriority.GroupedReplacement);
                        targetListItem.AllReplacement = handler.GetReplacement(ReplacementPriority.AllReplacement);

                        // Update original values from the reference.
                        targetListItem.originalPrefab = handler.OriginalPrefab;
                        targetListItem.originalAngle = handler.OriginalRadAngle * Mathf.Rad2Deg;
                        targetListItem.originalProb = handler.OriginalProbability;
                    }

                    // Grouped or individual?  Check is here (non-added prop section) as added props are always individual.
                    if (CurrentMode == ReplacementModes.Individual)
                    {
                        // Individual - set index to the current building prop indexes.
                        targetListItem.index = propIndex;
                    }
                    else
                    {
                        // Grouped - set index to -1 and add to our list of indexes.
                        targetListItem.index = -1;
                        targetListItem.indexes.Add(propIndex);
                    }
                }

                // Check for match with 'prop' mode - either original or replacement needs to be prop.
                if (PropTreeMode == PropTreeModes.Prop && !(originalInfo is PropInfo) && !(targetListItem.originalPrefab is PropInfo))
                {
                    continue;
                }

                // Check for match with 'tree' mode - either original or replacement needs to be tree.
                if (PropTreeMode == PropTreeModes.Tree && !(originalInfo is TreeInfo) && !(targetListItem.originalPrefab is TreeInfo))
                {
                    continue;
                }

                // Are we grouping?
                if (targetListItem.index == -1)
                {
                    // Yes, grouping - initialise a flag to show if we've matched.
                    bool matched = false;

                    // Iterate through each item in our existing list of props.
                    foreach (BuildingTargetListItem item in itemList)
                    {
                        // Check to see if we already have this in the list - matching original prefab, replacements, and probability.
                        if (item.originalPrefab == targetListItem.originalPrefab &&
                            item.IndividualReplacement == targetListItem.IndividualReplacement &&
                            item.GroupedReplacement == targetListItem.GroupedReplacement &&
                            item.AllReplacement == targetListItem.AllReplacement &&
                            item.originalProb == targetListItem.originalProb)
                        {
                            // We've already got an identical grouped instance of this item - add this index and lane to the lists of indexes and lanes under that item and set the flag to indicate that we've done so.
                            item.indexes.Add(propIndex);
                            matched = true;

                            // No point going any further through the list, since we've already found our match.
                            break;
                        }
                    }

                    // Did we get a match?
                    if (matched)
                    {
                        // Yes - continue on to next building prop (without adding this item separately to the list).
                        continue;
                    }
                }

                // Add this item to our list.
                itemList.Add(targetListItem);
            }

            // Create return fastlist from our filtered list, ordering by name.
            m_targetList.rowsData = new FastList<object>
            {
                m_buffer = m_targetSortSetting == (int)OrderBy.NameDescending ? itemList.OrderByDescending(item => item.DisplayName).ToArray() : itemList.OrderBy(item => item.DisplayName).ToArray(),
                m_size = itemList.Count,
            };

            // If the list is empty, show the 'no props' label; otherwise, hide it.
            if (m_targetList.rowsData.m_size == 0)
            {
                m_noPropsLabel.Show();
            }
            else
            {
                m_noPropsLabel.Hide();
            }
        }

        /// <summary>
        /// Performs actions to be taken once an update (application or reversion) has been applied, including saving data, updating button states, and refreshing renders.
        /// </summary>
        protected override void FinishUpdate()
        {
            base.FinishUpdate();

            // Update any dirty building renders.
            BuildingData.Update();
        }

        /// <summary>
        /// Adds a new tree or prop.
        /// </summary>
        protected override void AddNew()
        {
            // Make sure a valid replacement prefab is set.
            if (SelectedReplacementPrefab != null)
            {
                // Revert any preview.
                RevertPreview();

                // Add new prop.
                BOBConfig.BuildingReplacement newProp = new BOBConfig.BuildingReplacement
                {
                    IsTree = SelectedReplacementPrefab is TreeInfo,
                    Replacement = SelectedReplacementPrefab.name,
                    Angle = m_rotationSlider.TrueValue,
                    OffsetX = m_xSlider.TrueValue,
                    OffsetY = m_ySlider.TrueValue,
                    OffsetZ = m_zSlider.TrueValue,
                    Probability = (int)m_probabilitySlider.TrueValue,
                    ParentInfo = _selectedSubBuilding,
                    ReplacementInfo = SelectedReplacementPrefab,
                    CustomHeight = _customHeightCheck.isChecked,
                };
                AddedBuildingProps.Instance.AddNew(newProp);

                // Post-action cleanup.
                UpdateAddedProps();
            }
        }

        /// <summary>
        /// Removes an added tree or prop.
        /// </summary>
        protected override void RemoveProp()
        {
            // Safety first - need an individual index that's an added prop.
            if (SelectedTargetItem == null || SelectedTargetItem.index < 0 || !AddedBuildingProps.Instance.IsAdded(_selectedSubBuilding, SelectedTargetItem.index))
            {
                return;
            }

            // First, revert any preview (to prevent any clobbering when preview is reverted).
            RevertPreview();

            // Create new props array with one fewer entry, and copy the old props to it.
            // Remove prop reference and update other references as appropriate.
            AddedBuildingProps.Instance.RemoveNew(_selectedSubBuilding, SelectedTargetItem.index);

            // Post-action cleanup.
            UpdateAddedProps();
        }

        /// <summary>
        /// Record original prop values before previewing.
        /// </summary>
        protected override void RecordOriginal()
        {
            // Clear existing list.
            _originalValues.Clear();

            // Don't do anything if no valid selection.
            if (SelectedTargetItem?.originalPrefab == null || _selectedSubBuilding == null)
            {
                return;
            }

            // Check current mode.
            if (CurrentMode == ReplacementModes.All)
            {
                // All-building replacement; iterate through all prefabs and find matching prop references.
                for (uint i = 0; i < PrefabCollection<BuildingInfo>.LoadedCount(); ++i)
                {
                    BuildingInfo prefab = PrefabCollection<BuildingInfo>.GetLoaded(i);
                    if (prefab?.m_props != null)
                    {
                        for (int j = 0; j < prefab.m_props.Length; ++j)
                        {
                            if (prefab.m_props[j].m_prop == SelectedTargetItem.CurrentPrefab | prefab.m_props[j].m_tree == SelectedTargetItem.CurrentPrefab)
                            {
                                _originalValues.Add(GetOriginalData(prefab, j));
                            }
                        }
                    }
                }
            }
            else if (SelectedTargetItem.index < 0)
            {
                // Grouped replacement - iterate through each instance and record values.
                for (int i = 0; i < SelectedTargetItem.indexes.Count; ++i)
                {
                    _originalValues.Add(GetOriginalData(_selectedSubBuilding, SelectedTargetItem.indexes[i]));
                }
            }
            else
            {
                // Individual replacement - record original values.
                _originalValues.Add(GetOriginalData(_selectedSubBuilding, SelectedTargetItem.index));
            }
        }

        /// <summary>
        /// Sets the current sub-building selection to the specified index.
        /// </summary>
        /// <param name="index">Index number of specified sub-building.</param>
        private void SetSubBuilding(int index)
        {
            // Revert any preview.
            RevertPreview();

            // Set current building.
            _selectedSubBuilding = _subBuildings[index];

            // Reset current items.
            SelectedTargetItem = null;
            SelectedReplacementPrefab = null;

            // Reset lists.
            RegenerateReplacementList();
            RegenerateTargetList();

            // Update overlay.
            RenderOverlays.Building = _selectedSubBuilding;
        }

        /// <summary>
        /// Custom height checkbox event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="isChecked">New checked state.</param>
        private void CustomHeightChange(UIComponent c, bool isChecked)
        {
            // Show/hide Y position slider based on value.
            m_ySlider.isVisible = isChecked;
            m_ySlider.ValueField.isVisible = isChecked;
        }

        /// <summary>
        /// Called after any added prop manipulations (addition or removal) to perform cleanup.
        /// </summary>
        private void UpdateAddedProps()
        {
            // Update building prop references.
            _selectedSubBuilding.CheckReferences();

            // Clear current selection.
            SelectedTargetItem = null;

            // Perform regular post-processing.
            FinishUpdate();
            RegenerateTargetList();

            // Rebuild recorded originals list.
            RecordOriginal();
        }

        /// <summary>
        /// Previews the change for the current target item.
        /// </summary>
        /// <param name="handler">Prop handler.</param>
        /// <param name="previewReplacement">Replacement to preview.</param>
        private void PreviewChange(BuildingPropHandler handler, BOBConfig.BuildingReplacement previewReplacement)
        {
            handler.PreviewReplacement(previewReplacement);

            // Add building to dirty list.
            BuildingData.DirtyList.Add(handler.BuildingInfo);
        }

        /// <summary>
        /// Gets original (current) prop data.
        /// </summary>
        /// <param name="buildingInfo">Building prefab.</param>
        /// <param name="propIndex">Prop index.</param>
        /// <returns>New prop handler containing original data.</returns>
        private BuildingPropHandler GetOriginalData(BuildingInfo buildingInfo, int propIndex)
        {
            // Ensure that the index is valid before proceeding.
            if (buildingInfo?.m_props == null || buildingInfo.m_props.Length <= propIndex)
            {
                Logging.Error("invalid prop index reference of ", propIndex, " for selected building ", buildingInfo?.name ?? "null");
                return null;
            }

            // Create a new prop handler based on the current prop state (not the original).
            return BuildingHandlers.GetOrAddHandler(buildingInfo, propIndex);
        }

        /// <summary>
        /// Prop row fastlist item for sub-buildings.
        /// </summary>
        private class UISubBuildingRow : UIPropRow
        {
            // Sub-building reference index.
            private int subBuildingIndex;

            /// <summary>
            /// Called when this item is selected.
            /// </summary>
            public override void UpdateSelection()
            {
                // Update currently selected target prefab.
                if (BOBPanelManager.Panel is BOBBuildingPanel buildingPanel)
                {
                    buildingPanel.SetSubBuilding(subBuildingIndex);
                }
            }

            /// <summary>
            /// Called when list item is displayed.
            /// </summary>
            public override void Display(object data, bool isRowOdd)
            {
                // Perform initial setup for new rows.
                if (nameLabel == null)
                {
                    isVisible = true;
                    canFocus = true;
                    isInteractive = true;
                    width = parent.width;
                    height = RowHeight;

                    // Add object name label.
                    nameLabel = AddUIComponent<UILabel>();
                    nameLabel.width = this.width - 10f;
                    nameLabel.textScale = TextScale;
                }

                // Get sub-building index number.
                subBuildingIndex = (int)data;

                // Set display text.
                nameLabel.text = (BOBPanelManager.Panel as BOBBuildingPanel)._subBuildingNames[subBuildingIndex] ?? string.Empty;

                // Set label position
                nameLabel.relativePosition = new Vector2(5f, PaddingY);

                // Set initial background as deselected state.
                Deselect(isRowOdd);
            }
        }
    }
}
