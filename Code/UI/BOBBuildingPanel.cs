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
    using AlgernonCommons.Patching;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// BOB building tree/prop replacement panel.
    /// </summary>
    internal sealed class BOBBuildingPanel : BOBReplacementPanel
    {
        // Panel components.
        private readonly UICheckBox _customHeightCheck;
        private UIPanel _subBuildingPanel;
        private UIList _subBuildingList;

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
                base.SelectedTargetItem = value;

                if (value is TargetBuildingItem targetBuildingItem)
                {
                    // Ensure valid selection before proceeding.
                    if (_selectedSubBuilding != null)
                    {
                        // Set custom height checkbox.
                        _customHeightCheck.isChecked = _selectedSubBuilding.m_props[IndividualIndex].m_fixedHeight;

                        // Set sliders according to highest active replacement (will be null if none).
                        SetSliders(targetBuildingItem.AddedProp ?? targetBuildingItem.IndividualReplacement ?? targetBuildingItem.GroupedReplacement ?? targetBuildingItem.AllReplacement);

                        // All done here.
                        return;
                    }
                }

                // If we got here, there's no valid current selection; set all offset fields to defaults by passing null to SetSliders().
                SetSliders(null);
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
                    _subBuildingList = UIList.AddUIList<SubBuildingRow>(subBuildingListPanel, 0f, 0f, subBuildingListPanel.width, subBuildingListPanel.height);
                    _subBuildingList.EventSelectionChanged += (c, data) => SetSubBuilding((int)data);

                    // Create return fastlist from our filtered list.
                    _subBuildingList.Data = new FastList<object>
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

            // Regenerate target list.
            RegenerateTargetList();

            // Apply Harmony rendering patches.
            RenderOverlays.Building = _selectedSubBuilding;
            PatcherManager<Patcher>.Instance.PatchBuildingOverlays(true);
        }

        /// <summary>
        /// Generates a new replacement record from current control settings.
        /// </summary>
        /// <returns>New replacement record.</returns>
        protected override BOBConfig.Replacement GetReplacementFromControls()
        {
            // Generate prevew record entry.
            return new BOBConfig.BuildingReplacement
            {
                ReplacementInfo = SelectedReplacementPrefab ?? SelectedTargetItem.OriginalPrefab,
                OffsetX = m_xSlider.TrueValue,
                OffsetY = m_ySlider.TrueValue,
                OffsetZ = m_zSlider.TrueValue,
                Angle = m_rotationSlider.TrueValue,
                Probability = (int)m_probabilitySlider.TrueValue.RoundToNearest(1),
                CustomHeight = _customHeightCheck.isChecked,
            };
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
                if (SelectedTargetItem is TargetBuildingItem targetBuildingItem)
                {
                    // Determine applicable replacement prefab.
                    PrefabInfo replacementPrefab = SelectedReplacementPrefab ?? targetBuildingItem.ActivePrefab;

                    // Check for added prop - instead of replacing, we update the original added prop reference.
                    if (targetBuildingItem.AddedProp != null)
                    {
                        Logging.Message("updating added prop");

                        AddedBuildingProps.Instance.Update(
                            _selectedSubBuilding,
                            targetBuildingItem.OriginalPrefab,
                            replacementPrefab,
                            targetBuildingItem.PropIndex,
                            m_rotationSlider.TrueValue,
                            m_xSlider.TrueValue,
                            m_ySlider.TrueValue,
                            m_zSlider.TrueValue,
                            (int)m_probabilitySlider.TrueValue,
                            _customHeightCheck.isChecked);

                        // Update target record to the new state.
                        targetBuildingItem.OriginalPrefab = SelectedReplacementPrefab;
                        targetBuildingItem.OriginalProbability = (int)m_probabilitySlider.TrueValue;
                    }
                    else
                    {
                        // Not an added prop.
                        switch (CurrentMode)
                        {
                            case ReplacementModes.Individual:
                                // Individual replacement.
                                IndividualBuildingReplacement.Instance.Replace(
                                    _selectedSubBuilding,
                                    targetBuildingItem.OriginalPrefab,
                                    replacementPrefab,
                                    IndividualIndex,
                                    m_rotationSlider.TrueValue,
                                    m_xSlider.TrueValue,
                                    m_ySlider.TrueValue,
                                    m_zSlider.TrueValue,
                                    (int)m_probabilitySlider.TrueValue,
                                    _customHeightCheck.isChecked,
                                    targetBuildingItem.IndividualReplacement);
                                break;

                            case ReplacementModes.Grouped:
                                // Grouped replacement.
                                GroupedBuildingReplacement.Instance.Replace(
                                    _selectedSubBuilding,
                                    targetBuildingItem.OriginalPrefab,
                                    replacementPrefab,
                                    -1,
                                    m_rotationSlider.TrueValue,
                                    m_xSlider.TrueValue,
                                    m_ySlider.TrueValue,
                                    m_zSlider.TrueValue,
                                    (int)m_probabilitySlider.TrueValue,
                                    _customHeightCheck.isChecked,
                                    targetBuildingItem.GroupedReplacement);
                                break;

                            case ReplacementModes.All:
                                // All- replacement.
                                AllBuildingReplacement.Instance.Replace(
                                    null,
                                    targetBuildingItem.OriginalPrefab,
                                    replacementPrefab,
                                    -1,
                                    m_rotationSlider.TrueValue,
                                    m_xSlider.TrueValue,
                                    m_ySlider.TrueValue,
                                    m_zSlider.TrueValue,
                                    (int)m_probabilitySlider.TrueValue,
                                    _customHeightCheck.isChecked,
                                    targetBuildingItem.AllReplacement);
                                break;

                            default:
                                Logging.Error("invalid replacement mode at BuildingInfoPanel.Apply");
                                return;
                        }
                    }

                    // Update highlighting target.
                    RenderOverlays.Prop = replacementPrefab as PropInfo;
                    RenderOverlays.Tree = replacementPrefab as TreeInfo;

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
                if (SelectedTargetItem is TargetBuildingItem targetBuildingItem)
                {
                    // Individual building prop reversion?
                    if (targetBuildingItem.IndividualReplacement != null)
                    {
                        // Individual reversion.
                        IndividualBuildingReplacement.Instance.RemoveReplacement(targetBuildingItem.IndividualReplacement);
                    }
                    else if (targetBuildingItem.GroupedReplacement != null)
                    {
                        // Grouped reversion.
                        GroupedBuildingReplacement.Instance.RemoveReplacement(targetBuildingItem.GroupedReplacement);
                    }
                    else if (targetBuildingItem.AllReplacement != null)
                    {
                        // All-building reversion - make sure we've got a currently active replacement before doing anything.
                        if (targetBuildingItem.OriginalPrefab)
                        {
                            // All-building reversion.
                            AllBuildingReplacement.Instance.RemoveReplacement(targetBuildingItem.AllReplacement);
                        }
                    }

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
        /// Regenerates the target fastlist with a list of target-specific trees or props.
        /// </summary>
        protected override void RegenerateTargetList()
        {
            // Clear current selection.
            m_targetList.SelectedIndex = -1;

            // List of prefabs that have passed filtering.
            List<TargetListItem> itemList = new List<TargetListItem>();

            // Check to see if this building contains any props.
            if (_selectedSubBuilding.m_props == null || _selectedSubBuilding.m_props.Length == 0)
            {
                // No props - show 'no props' label and return an empty list.
                m_noPropsLabel.Show();
                m_targetList.Data = new FastList<object>();

                return;
            }

            // Iterate through each prop in building.
            for (int propIndex = 0; propIndex < _selectedSubBuilding.m_props.Length; ++propIndex)
            {
                // Create new list item.
                TargetBuildingItem targetBuildingItem = new TargetBuildingItem();

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

                // Get current tree/prop prefab and probability, as default original values.
                targetBuildingItem.OriginalPrefab = originalInfo;
                targetBuildingItem.OriginalProbability = _selectedSubBuilding.m_props[propIndex].m_probability;

                // Is this an added prop?
                if (AddedBuildingProps.Instance.ReplacementRecord(_selectedSubBuilding, propIndex) is BOBConfig.BuildingReplacement addedItem)
                {
                    targetBuildingItem.PropIndex = propIndex;
                    targetBuildingItem.AddedProp = addedItem;
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
                        targetBuildingItem.IndividualReplacement = handler.GetReplacement(ReplacementPriority.IndividualReplacement);
                        targetBuildingItem.GroupedReplacement = handler.GetReplacement(ReplacementPriority.GroupedReplacement);
                        targetBuildingItem.AllReplacement = handler.GetReplacement(ReplacementPriority.AllReplacement);

                        // Record current values as replacement values.
                        targetBuildingItem.ReplacementPrefab = targetBuildingItem.OriginalPrefab;
                        targetBuildingItem.ReplacementProbability = targetBuildingItem.OriginalProbability;

                        // Update original values from the reference.
                        targetBuildingItem.OriginalPrefab = handler.OriginalPrefab;
                        targetBuildingItem.OriginalProbability = handler.OriginalProbability;
                    }

                    // Grouped or individual?  Check is here (non-added prop section) as added props are always individual.
                    if (CurrentMode == ReplacementModes.Individual)
                    {
                        // Individual - set index to the current building prop indexes.
                        targetBuildingItem.PropIndex = propIndex;
                    }
                    else
                    {
                        // Grouped - set index to -1 and add to our list of indexes.
                        targetBuildingItem.PropIndex = -1;
                        targetBuildingItem.PropIndexes.Add(propIndex);
                    }
                }

                // Check for match with 'prop' mode - either original or replacement needs to be prop.
                if (PropTreeMode == PropTreeModes.Prop && !(originalInfo is PropInfo) && !(targetBuildingItem.OriginalPrefab is PropInfo))
                {
                    continue;
                }

                // Check for match with 'tree' mode - either original or replacement needs to be tree.
                if (PropTreeMode == PropTreeModes.Tree && !(originalInfo is TreeInfo) && !(targetBuildingItem.OriginalPrefab is TreeInfo))
                {
                    continue;
                }

                // Are we grouping?
                if (targetBuildingItem.PropIndex == -1)
                {
                    // Yes, grouping - initialise a flag to show if we've matched.
                    bool matched = false;

                    // Iterate through each item in our existing list of props.
                    foreach (TargetListItem item in itemList)
                    {
                        if (item is TargetBuildingItem buildingItem)
                        {
                            // Check to see if we already have this in the list - matching original prefab, replacements, and probability.
                            if (buildingItem.OriginalPrefab == targetBuildingItem.OriginalPrefab &&
                                buildingItem.IndividualReplacement == targetBuildingItem.IndividualReplacement &&
                                buildingItem.GroupedReplacement == targetBuildingItem.GroupedReplacement &&
                                buildingItem.AllReplacement == targetBuildingItem.AllReplacement &&
                                buildingItem.OriginalProbability == targetBuildingItem.OriginalProbability)
                            {
                                // We've already got an identical grouped instance of this item - add this index and lane to the lists of indexes and lanes under that item and set the flag to indicate that we've done so.
                                buildingItem.PropIndexes.Add(propIndex);
                                matched = true;

                                // No point going any further through the list, since we've already found our match.
                                break;
                            }
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
                itemList.Add(targetBuildingItem);
            }

            // Create return fastlist from our filtered list, ordering by name.
            m_targetList.Data = new FastList<object>
            {
                m_buffer = m_targetSortSetting == (int)OrderBy.NameDescending ? itemList.OrderByDescending(item => item.DisplayName).ToArray() : itemList.OrderBy(item => item.DisplayName).ToArray(),
                m_size = itemList.Count,
            };

            // If the list is empty, show the 'no props' label; otherwise, hide it.
            if (m_targetList.Data.m_size == 0)
            {
                m_noPropsLabel.Show();
            }
            else
            {
                m_noPropsLabel.Hide();
            }
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
                    ReplacementName = SelectedReplacementPrefab.name,
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
        /// Removes an added prop.
        /// </summary>
        protected override void RemoveAddedProp() => AddedBuildingProps.Instance.RemoveNew(_selectedSubBuilding, SelectedTargetItem.PropIndex);

        /// <summary>
        /// Removes an added tree or prop.
        /// </summary>
        protected override void RemoveProp()
        {
            // Safety first - need an individual index that's an added prop.
            if (SelectedTargetItem == null || SelectedTargetItem.PropIndex < 0 || !AddedBuildingProps.Instance.IsAdded(_selectedSubBuilding, SelectedTargetItem.PropIndex))
            {
                return;
            }

            base.RemoveProp();
        }

        /// <summary>
        /// Record original prop values before previewing.
        /// </summary>
        protected override void RecordOriginals()
        {
            // Clear existing list.
            m_originalValues.Clear();

            // Don't do anything if no valid selection.
            if (SelectedTargetItem?.OriginalPrefab == null || _selectedSubBuilding == null)
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
                            if (prefab.m_props[j].m_prop == SelectedTargetItem.ReplacementPrefab || prefab.m_props[j].m_tree == SelectedTargetItem.ReplacementPrefab)
                            {
                                m_originalValues.Add(GetOriginalData(prefab, j));
                            }
                        }
                    }
                }
            }
            else if (SelectedTargetItem.PropIndex < 0)
            {
                // Grouped replacement - iterate through each instance and record values.
                for (int i = 0; i < SelectedTargetItem.PropIndexes.Count; ++i)
                {
                    m_originalValues.Add(GetOriginalData(_selectedSubBuilding, SelectedTargetItem.PropIndexes[i]));
                }
            }
            else
            {
                // Individual replacement - record original values.
                m_originalValues.Add(GetOriginalData(_selectedSubBuilding, SelectedTargetItem.PropIndex));
            }
        }

        /// <summary>
        /// Regenerate render and prefab data.
        /// </summary>
        protected override void UpdateData() => BuildingData.Update();

        /// <summary>
        /// Called after any added prop manipulations (addition or removal) to perform cleanup.
        /// </summary>
        protected override void UpdateAddedProps()
        {
            // Update building prop references.
            _selectedSubBuilding.CheckReferences();

            base.UpdateAddedProps();
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
        /// UIList row item for sub-buildings.
        /// </summary>
        private class SubBuildingRow : UIListRow
        {
            // Display label.
            private UILabel _nameLabel;

            /// <summary>
            /// Generates and displays a list row.
            /// </summary>
            /// <param name="data">Object data to display.</param>
            /// <param name="rowIndex">Row index number (for background banding).</param>
            public override void Display(object data, int rowIndex)
            {
                // Perform initial setup for new rows.
                if (_nameLabel == null)
                {
                    // Add name labels.
                    _nameLabel = AddLabel(Margin, parent.width - Margin - Margin);
                }

                // Get sub-building index number.
                if (data is int subBuildingIndex)
                {
                    // Set display text.
                    _nameLabel.text = (BOBPanelManager.Panel as BOBBuildingPanel)?._subBuildingNames[subBuildingIndex] ?? string.Empty;
                }
                else
                {
                    // Just in case.
                    _nameLabel.text = string.Empty;
                }

                // Set initial background as deselected state.
                Deselect(rowIndex);
            }
        }
    }
}
