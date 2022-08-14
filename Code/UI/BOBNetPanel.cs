// <copyright file="BOBNetPanel.cs" company="algernon (K. Algernon A. Sheppard)">
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

    /// <summary>
    /// BOB network tree/prop replacement panel.
    /// </summary>
    internal sealed class BOBNetPanel : BOBInfoPanel
    {
        // Layout constants.
        private const float PackButtonX = RandomButtonX + ToggleSize;
        private const float LaneX = ModeX + (ToggleSize * 3f) + Margin;

        // Original selection values.
        private readonly List<LanePropHandler> _originalValues = new List<LanePropHandler>();

        // Panel components.
        private readonly UIDropDown _laneMenu;
        private readonly BOBSlider _repeatSlider;

        // Event suppression.
        private bool _ignoreIndexChange = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="BOBNetPanel"/> class.
        /// </summary>
        internal BOBNetPanel()
        {
            try
            {
                // Add lane menu
                _laneMenu = UIDropDowns.AddDropDown(this, LaneX, ToggleY + 3f, MiddleX - LaneX);
                UILabels.AddLabel(_laneMenu, 0f, -ToggleHeaderHeight - 3f, Translations.Translate("BOB_PNL_LAN"), textScale: 0.8f);
                _laneMenu.tooltipBox = UIToolTips.WordWrapToolTip;
                _laneMenu.tooltip = Translations.Translate("BOB_PNL_LAN_TIP");
                _laneMenu.eventSelectedIndexChanged += LaneIndexChanged;
                _laneMenu.isVisible = CurrentMode == ReplacementModes.Individual;

                // Add pack button.
                UIButton packButton = AddIconButton(this, PackButtonX, ToggleY, ToggleSize, "BOB_PNL_PKB", UITextures.LoadQuadSpriteAtlas("BOB-PropPack"));
                packButton.eventClicked += (component, clickEvent) => BOBPackPanel.Create();

                // Add repeat slider.
                UIPanel repeatPanel = Sliderpanel(this, MidControlX, RepeatSliderY + Margin, SliderHeight);
                _repeatSlider = AddBOBSlider(repeatPanel, Margin, 0f, MidControlWidth - (Margin * 2f), "BOB_PNL_REP", 1.1f, 50f, 0.1f, "Repeat");
                _repeatSlider.tooltip = Translations.Translate("BOB_PNL_REP_TIP");
                _repeatSlider.parent.isVisible = CurrentMode == ReplacementModes.Individual;
                _repeatSlider.EventTrueValueChanged += SliderChange;

                // Regenerate replacement list.
                RegenerateReplacementList();
            }
            catch (Exception e)
            {
                // Log and report any exception.
                Logging.LogException(e, "exception creating network panel");
            }
        }

        /// <summary>
        /// Sets the current target item and updates button states accordingly.
        /// </summary>
        internal override TargetListItem SelectedTargetItem
        {
            set
            {
                // First, undo any preview.
                RevertPreview();

                // Call base, while ignoring replacement prefab change live application.
                m_ignoreSelectedPrefabChange = true;
                base.SelectedTargetItem = value;
                m_ignoreSelectedPrefabChange = false;

                // Clear original data and record new references.
                RecordOriginal();

                if (value is TargetNetItem targetNetItem)
                {
                    // Ensure valid selection before proceeding.
                    if (SelectedNet != null)
                    {
                        // Set lane highlighting selection for individual items.
                        if (targetNetItem.LaneIndex > -1)
                        {
                            RenderOverlays.Lane = SelectedNet.m_lanes[targetNetItem.LaneIndex];
                        }

                        // Is this an added prop?
                        if (targetNetItem.IsAdded)
                        {
                            // Yes - set sliders from replacement record.
                            SetSliders(AddedNetworkProps.Instance.ReplacementRecord(SelectedNet, IndividualLane, IndividualIndex));

                            // All done here.
                            return;
                        }
                        else
                        {
                            // Set sliders according to highest active replacement (will be null if none).
                            SetSliders(targetNetItem.IndividualReplacement ?? targetNetItem.GroupedReplacement ?? targetNetItem.AllReplacement ?? targetNetItem.PackReplacement);
                            return;
                        }
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
            "BOB-RoadsSmall",
        };

        /// <summary>
        /// Gets the mode icon atlas names for tree modes.
        /// </summary>
        protected override string[] TreeModeAtlas => new string[(int)ReplacementModes.NumModes]
        {
            "BOB-ThisTreeSmall",
            "BOB-SameTreeSmall",
            "BOB-RoadsSmall",
        };

        /// <summary>
        /// Gets the mode icon tooltip keys for prop modes.
        /// </summary>
        protected override string[] PropModeTipKeys => new string[(int)ReplacementModes.NumModes]
        {
            "BOB_PNL_M_PIN",
            "BOB_PNL_M_PGN",
            "BOB_PNL_M_PAN",
        };

        /// <summary>
        /// Gets the mode icon tooltip keys for tree modes.
        /// </summary>
        protected override string[] TreeModeTipKeys => new string[(int)ReplacementModes.NumModes]
        {
            "BOB_PNL_M_TIN",
            "BOB_PNL_M_TGN",
            "BOB_PNL_M_TAN",
        };

        /// <summary>
        /// Sets the current replacement mode.
        /// </summary>
        protected override ReplacementModes CurrentMode
        {
            set
            {
                // Add and remove buttons, lane menu, and repeat distance slider are only valid in individual mode.
                bool isIndividual = value == ReplacementModes.Individual;
                _repeatSlider.parent.isVisible = isIndividual;
                m_addButton.isVisible = isIndividual;
                m_removeButton.isVisible = isIndividual;
                _laneMenu.isVisible = isIndividual;

                base.CurrentMode = value;
            }
        }

        /// <summary>
        /// Gets the current individual lane number of the current selection.  This could be either the direct lane or in the lane array, depending on situation.
        /// </summary>
        private int IndividualLane
        {
            get
            {
                if (SelectedTargetItem is TargetNetItem targetNetItem)
                {
                    return targetNetItem.LaneIndex < 0 ? targetNetItem.LaneIndexes[0] : targetNetItem.LaneIndex;
                }

                // If we got here, no valid lane was available; return -1 (should never happen, but you never know, plus we need to keep the compiler happy).
                return -1;
            }
        }

        /// <summary>
        /// Gets the currently selected lane index.
        /// </summary>
        private int SelectedLaneIndex => _laneMenu.selectedIndex - 1;

        /// <summary>
        /// Sets the target parent prefab.
        /// </summary>
        /// <param name="targetPrefabInfo">Target prefab to set.</param>
        internal override void SetTargetParent(PrefabInfo targetPrefabInfo)
        {
            // Don't do anything if target hasn't changed.
            if (SelectedNet == targetPrefabInfo)
            {
                return;
            }

            // Base setup.
            base.SetTargetParent(targetPrefabInfo);

            // Build lane menu selection list, with 'all lanes' at index 0, selected by default.
            _ignoreIndexChange = true;
            string[] laneMenuItems = new string[SelectedNet.m_lanes.Length + 1];
            laneMenuItems[0] = Translations.Translate("BOB_PNL_LAN_ALL");
            for (int i = 1; i < laneMenuItems.Length; ++i)
            {
                // Offset by one to allow for 'all' selection at index zero.
                laneMenuItems[i] = (i - 1).ToString();
            }

            _laneMenu.items = laneMenuItems;

            // Set selection to default 'all' and resume lane selection event handling.
            _laneMenu.selectedIndex = 0;
            _ignoreIndexChange = false;

            // Populate target list and select target item.
            RegenerateTargetList();

            // Record original stats for preview.
            RecordOriginal();

            // Apply Harmony rendering patches.
            RenderOverlays.Network = SelectedNet;
            Patcher.Instance.PatchNetworkOverlays(true);
        }

        /// <summary>
        /// Previews the current change.
        /// </summary>
        protected override void PreviewChange()
        {
            // Don't do anything if no current selection.
            if (SelectedTargetItem is TargetNetItem targetNetItem)
            {
                // Don't do anything if no changes.
                if (m_xSlider.TrueValue == 0f &&
                    m_ySlider.TrueValue == 0f &&
                    m_zSlider.TrueValue == 0f &&
                    m_rotationSlider.TrueValue == 0f &&
                    m_probabilitySlider.TrueValue.RoundToNearest(1) == targetNetItem.OriginalProbability &&
                    SelectedReplacementPrefab == targetNetItem.ActivePrefab &&
                    _repeatSlider.TrueValue == targetNetItem.OriginalRepeat)
                {
                    // Reset apply button icon.
                    UnappliedChanges = false;

                    return;
                }

                // Generate prevew record entry.
                BOBConfig.NetReplacement previewReplacement = new BOBConfig.NetReplacement
                {
                    ReplacementInfo = SelectedReplacementPrefab ?? targetNetItem.OriginalPrefab,
                    OffsetX = m_xSlider.TrueValue,
                    OffsetY = m_ySlider.TrueValue,
                    OffsetZ = m_zSlider.TrueValue,
                    Angle = m_rotationSlider.TrueValue,
                    Probability = (int)m_probabilitySlider.TrueValue.RoundToNearest(1),
                    RepeatDistance = _repeatSlider.TrueValue,
                };

                // Update preview for each handler.
                foreach (LanePropHandler handler in _originalValues)
                {
                    PreviewChange(handler, previewReplacement);
                }

                // Update renders.
                NetData.Update();

                // Update highlighting target.
                RenderOverlays.Prop = SelectedReplacementPrefab as PropInfo;
                RenderOverlays.Tree = SelectedReplacementPrefab as TreeInfo;

                // Update apply button icon to indicate change.
                UnappliedChanges = true;
            }
        }

        /// <summary>
        /// Reverts any previewed changes back to original prop/tree state.
        /// </summary>
        protected override void RevertPreview()
        {
            // Iterate through each original value.
            foreach (LanePropHandler handler in _originalValues)
            {
                // Restore original values.
                handler.ClearPreview();
            }

            // Update prefabs.
            NetData.Update();

            // Reset apply button icon
            UnappliedChanges = false;
        }

        /// <summary>
        /// Apply button event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="p">Mouse event parameter.</param>
        protected override void Apply(UIComponent c, UIMouseEventParameter p)
        {
            try
            {
                // First, undo any preview.
                RevertPreview();

                // Make sure we have valid a target and replacement.
                if (SelectedTargetItem is TargetNetItem targetNetItem && SelectedReplacementPrefab != null)
                {
                    // Check for added prop - instead of replacing, we update the original added prop reference.
                    if (targetNetItem.IsAdded)
                    {
                        AddedNetworkProps.Instance.Update(
                            SelectedNet,
                            targetNetItem.OriginalPrefab,
                            SelectedReplacementPrefab,
                            targetNetItem.LaneIndex,
                            targetNetItem.PropIndex,
                            m_rotationSlider.TrueValue,
                            m_xSlider.TrueValue,
                            m_ySlider.TrueValue,
                            m_zSlider.TrueValue,
                            (int)m_probabilitySlider.TrueValue,
                            _repeatSlider.TrueValue);

                        // Update target record to the new state.
                        targetNetItem.OriginalPrefab = SelectedReplacementPrefab;
                        targetNetItem.OriginalProbability = (int)m_probabilitySlider.TrueValue;
                        targetNetItem.OriginalRepeat = _repeatSlider.TrueValue;
                    }
                    else
                    {
                        // Not an added prop.
                        switch (CurrentMode)
                        {
                            case ReplacementModes.Individual:
                                IndividualNetworkReplacement.Instance.Replace(
                                    SelectedNet,
                                    targetNetItem.OriginalPrefab,
                                    SelectedReplacementPrefab,
                                    IndividualLane,
                                    IndividualIndex,
                                    m_rotationSlider.TrueValue,
                                    m_xSlider.TrueValue,
                                    m_ySlider.TrueValue,
                                    m_zSlider.TrueValue,
                                    (int)m_probabilitySlider.TrueValue,
                                    _repeatSlider.TrueValue,
                                    targetNetItem.IndividualReplacement);
                                break;

                            case ReplacementModes.Grouped:
                                // Grouped replacement.
                                GroupedNetworkReplacement.Instance.Replace(
                                    SelectedNet,
                                    targetNetItem.OriginalPrefab,
                                    SelectedReplacementPrefab,
                                    -1,
                                    -1,
                                    m_rotationSlider.TrueValue,
                                    m_xSlider.TrueValue,
                                    m_ySlider.TrueValue,
                                    m_zSlider.TrueValue,
                                    (int)m_probabilitySlider.TrueValue,
                                    -1,
                                    targetNetItem.GroupedReplacement);
                                break;

                            case ReplacementModes.All:
                                // All- replacement.
                                AllNetworkReplacement.Instance.Replace(
                                    null,
                                    targetNetItem.OriginalPrefab,
                                    SelectedReplacementPrefab,
                                    -1,
                                    -1,
                                    m_rotationSlider.TrueValue,
                                    m_xSlider.TrueValue,
                                    m_ySlider.TrueValue,
                                    m_zSlider.TrueValue,
                                    (int)m_probabilitySlider.TrueValue,
                                    -1,
                                    targetNetItem.AllReplacement);
                                break;

                            default:
                                Logging.Error("invalid replacement mode at NetInfoPanel.Apply");
                                return;
                        }
                    }

                    // Update any dirty network renders.
                    NetData.Update();

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
                Logging.LogException(e, "exception applying network replacement");
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
                if (SelectedTargetItem is TargetNetItem targetNetItem)
                {
                    // Individual prop reversion?
                    if (targetNetItem.IndividualReplacement != null)
                    {
                        // Individual reversion.
                        IndividualNetworkReplacement.Instance.RemoveReplacement(targetNetItem.IndividualReplacement);
                    }
                    else if (targetNetItem.GroupedReplacement != null)
                    {
                        // Grouped reversion.
                        GroupedNetworkReplacement.Instance.RemoveReplacement(targetNetItem.GroupedReplacement);
                    }
                    else if (targetNetItem.AllReplacement != null)
                    {
                        // All-network reversion - make sure we've got a currently active replacement before doing anything.
                        if (targetNetItem.OriginalPrefab)
                        {
                            // All-network reversion.
                            AllNetworkReplacement.Instance.RemoveReplacement(targetNetItem.AllReplacement);
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
                Logging.LogException(e, "exception perforiming network reversion");
            }
        }

        /// <summary>
        /// Updates the target item record for changes in replacement status (e.g. after applying or reverting changes).
        /// </summary>
        /// <param name="targetListItem">Target item.</param>
        protected override void UpdateTargetItem(TargetListItem targetListItem)
        {
            if (targetListItem is TargetNetItem targetNetItem)
            {
                // Determine index to test - if no individual index, just grab first one from list.
                int propIndex = targetNetItem.LaneIndex;
                if (propIndex < 0)
                {
                    propIndex = targetNetItem.LaneIndexes[0];
                }

                // Determine lane to test - if no individual lane, just grab first one from list.
                int lane = targetNetItem.LaneIndex;
                if (lane < 0)
                {
                    lane = targetNetItem.LaneIndexes[0];
                }

                // Is this an added prop?
                if (AddedNetworkProps.Instance.IsAdded(SelectedNet, lane, propIndex))
                {
                    targetNetItem.PropIndex = propIndex;
                    targetNetItem.IsAdded = true;
                }
                else
                {
                    // Non-added prop; update stored references.
                    LanePropHandler handler = NetHandlers.GetHandler(SelectedNet.m_lanes[lane], propIndex);
                    if (handler != null)
                    {
                        targetNetItem.IndividualReplacement = handler.GetReplacement(ReplacementPriority.IndividualReplacement);
                        targetNetItem.GroupedReplacement = handler.GetReplacement(ReplacementPriority.GroupedReplacement);
                        targetNetItem.AllReplacement = handler.GetReplacement(ReplacementPriority.AllReplacement);
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
            m_targetList.SelectedIndex = -1;

            // List of prefabs that have passed filtering.
            List<TargetListItem> itemList = new List<TargetListItem>();

            // Check to see if this building contains any lanes.
            if (SelectedNet?.m_lanes == null || SelectedNet.m_lanes.Length == 0)
            {
                // No lanes - show 'no props' label and return an empty list.
                m_noPropsLabel.Show();
                m_targetList.Data = new FastList<object>();

                // Force clearance of current target item.
                SelectedTargetItem = null;

                return;
            }

            // Local reference.
            NetInfo.Lane[] lanes = SelectedNet.m_lanes;

            // Iterate through each lane.
            for (int lane = 0; lane < lanes.Length; ++lane)
            {
                if (CurrentMode == (int)ReplacementModes.Individual)
                {
                    // If individual mode and a lane has been selected, skip any lanes not selected.
                    if (CurrentMode == (int)ReplacementModes.Individual && _laneMenu.selectedIndex > 0 && lane != SelectedLaneIndex)
                    {
                        continue;
                    }
                }

                // Local reference.
                NetLaneProps.Prop[] laneProps = lanes[lane].m_laneProps?.m_props;

                // If no props in this lane, skip it and go to the next one.
                if (laneProps == null)
                {
                    continue;
                }

                // Iterate through each prop in lane.
                for (int propIndex = 0; propIndex < laneProps.Length; ++propIndex)
                {
                    // Create new list item.
                    TargetNetItem targetNetItem = new TargetNetItem();

                    // Try to get relevant prefab (prop/tree), falling back to the other type if null (to allow for tree-prop changes), using finalProp.
                    PrefabInfo originalInfo = null;
                    if (PropTreeMode == PropTreeModes.Tree)
                    {
                        originalInfo = (PrefabInfo)laneProps[propIndex]?.m_tree ?? laneProps[propIndex]?.m_prop;
                    }
                    else
                    {
                        originalInfo = (PrefabInfo)laneProps[propIndex]?.m_prop ?? laneProps[propIndex]?.m_tree;
                    }

                    // Check to see if we were succesful - if not (e.g. we only want trees and this is a prop), continue on to next building prop.
                    if (originalInfo?.name == null)
                    {
                        continue;
                    }

                    // Get current tree/prop prefab and probability, as default original values.
                    targetNetItem.OriginalPrefab = originalInfo;
                    targetNetItem.OriginalProbability = laneProps[propIndex].m_probability;
                    targetNetItem.OriginalRepeat = laneProps[propIndex].m_repeatDistance;

                    // Is this an added prop?
                    if (AddedNetworkProps.Instance.IsAdded(lanes[lane], propIndex))
                    {
                        targetNetItem.PropIndex = propIndex;
                        targetNetItem.LaneIndex = lane;
                        targetNetItem.IsAdded = true;
                    }
                    else
                    {
                        // Non-added prop - see if we've got an existing reference.
                        LanePropHandler handler = NetHandlers.GetHandler(lanes[lane], propIndex);
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
                            targetNetItem.IndividualReplacement = handler.GetReplacement(ReplacementPriority.IndividualReplacement);
                            targetNetItem.GroupedReplacement = handler.GetReplacement(ReplacementPriority.GroupedReplacement);
                            targetNetItem.AllReplacement = handler.GetReplacement(ReplacementPriority.AllReplacement);
                            targetNetItem.PackReplacement = handler.GetReplacement(ReplacementPriority.PackReplacement);

                            // Record current values as replacement values.
                            targetNetItem.ReplacementPrefab = targetNetItem.OriginalPrefab;
                            targetNetItem.ReplacementProbability = targetNetItem.OriginalProbability;

                            // Update original values from the reference.
                            targetNetItem.OriginalPrefab = handler.OriginalPrefab;
                            targetNetItem.OriginalProbability = handler.OriginalProbability;
                            targetNetItem.OriginalRepeat = handler.OriginalRepeatDistance;
                        }

                        // Grouped or individual?
                        if (CurrentMode == (int)ReplacementModes.Individual)
                        {
                            // Individual - set index to the current prop indexes.
                            targetNetItem.PropIndex = propIndex;
                            targetNetItem.LaneIndex = lane;
                        }
                        else
                        {
                            // Grouped - set index to -1 and add to our list of indexes.
                            targetNetItem.PropIndex = -1;
                            targetNetItem.LaneIndex = -1;
                            targetNetItem.PropIndexes.Add(propIndex);
                            targetNetItem.LaneIndexes.Add(lane);
                        }
                    }

                    // Check for match with 'prop' mode - either original or replacement needs to be prop.
                    if (PropTreeMode == PropTreeModes.Prop && !(originalInfo is PropInfo) && !(targetNetItem.OriginalPrefab is PropInfo))
                    {
                        continue;
                    }

                    // Check for match with 'tree' mode - either original or replacement needs to be tree.
                    if (PropTreeMode == PropTreeModes.Tree && !(originalInfo is TreeInfo) && !(targetNetItem.OriginalPrefab is TreeInfo))
                    {
                        continue;
                    }

                    // Are we grouping?
                    if (targetNetItem.PropIndex == -1)
                    {
                        // Yes, grouping - initialise a flag to show if we've matched.
                        bool matched = false;

                        // Iterate through each item in our existing list of props.
                        foreach (TargetNetItem item in itemList)
                        {
                            // Check to see if we already have this in the list - matching original prefab, individual replacement prefab, network replacement prefab, all-network replacement prefab, and probability.
                            if (item.OriginalPrefab == targetNetItem.OriginalPrefab
                                && item.IndividualReplacement == targetNetItem.IndividualReplacement
                                && item.GroupedReplacement == targetNetItem.GroupedReplacement
                                && item.AllReplacement == targetNetItem.AllReplacement
                                && item.OriginalProbability == targetNetItem.OriginalProbability)
                            {
                                // We've already got an identical grouped instance of this item - add this index and lane to the lists of indexes and lanes under that item and set the flag to indicate that we've done so.
                                item.PropIndexes.Add(propIndex);
                                item.LaneIndexes.Add(lane);
                                matched = true;

                                // No point going any further through the list, since we've already found our match.
                                break;
                            }
                        }

                        // Did we get a match?
                        if (matched)
                        {
                            // Yes - continue on to next network prop (without adding this item separately to the list).
                            continue;
                        }
                    }

                    // Add this item to our list.
                    itemList.Add(targetNetItem);
                }
            }

            // Create return fastlist from our filtered list, ordering by name.
            m_targetList.Data = new FastList<object>()
            {
                m_buffer = m_targetSortSetting == (int)OrderBy.NameDescending ? itemList.OrderByDescending(item => item.DisplayName).ToArray() : itemList.OrderBy(item => item.DisplayName).ToArray(),
                m_size = itemList.Count,
            };

            m_targetList.Refresh();

            // If the list is empty, show the 'no props' label; otherwise, hide it.
            if (itemList.Count == 0)
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

            // Update any dirty net renders.
            NetData.Update();
        }

        /// <summary>
        /// Adds a new tree or prop.
        /// </summary>
        protected override void AddNew()
        {
            // Make sure a valid replacement prefab is set and we have a valid lane selection.
            if (SelectedReplacementPrefab != null && _laneMenu.selectedIndex > 0)
            {
                // Revert any preview.
                RevertPreview();

                // Add new prop.
                BOBConfig.NetReplacement newProp = new BOBConfig.NetReplacement
                {
                    LaneIndex = SelectedLaneIndex,
                    IsTree = SelectedReplacementPrefab is TreeInfo,
                    Replacement = SelectedReplacementPrefab.name,
                    Angle = m_rotationSlider.TrueValue,
                    OffsetX = m_xSlider.TrueValue,
                    OffsetY = m_ySlider.TrueValue,
                    OffsetZ = m_zSlider.TrueValue,
                    Probability = (int)m_probabilitySlider.TrueValue,
                    ParentInfo = SelectedNet,
                    ReplacementInfo = SelectedReplacementPrefab,
                    RepeatDistance = _repeatSlider.parent.isVisible ? _repeatSlider.TrueValue : 0,
                };
                AddedNetworkProps.Instance.AddNew(newProp);

                // Post-action cleanup.
                UpdateAddedPops();
            }
        }

        /// <summary>
        /// Removes an added tree or prop.
        /// </summary>
        protected override void RemoveProp()
        {
            // Safety first - need an individual index that's an added prop.
            if (SelectedTargetItem is TargetNetItem targetNetItem)
            {
                if (targetNetItem.PropIndex < 0 || targetNetItem.LaneIndex < 0 || !AddedNetworkProps.Instance.IsAdded(SelectedNet, targetNetItem.LaneIndex, targetNetItem.PropIndex))
                {
                    return;
                }

                // First, revert any preview (to prevent any clobbering when preview is reverted).
                RevertPreview();

                // Create new props array with one fewer entry, and copy the old props to it.
                // Remove prop reference and update other references as appropriate.
                AddedNetworkProps.Instance.RemoveNew(SelectedNet, targetNetItem.LaneIndex, targetNetItem.PropIndex);

                // Post-action cleanup.
                UpdateAddedPops();
            }
        }

        /// <summary>
        /// Record original prop values before previewing.
        /// </summary>
        protected override void RecordOriginal()
        {
            // Clear existing list.
            _originalValues.Clear();

            if (SelectedTargetItem is TargetNetItem targetNetItem)
            {
                // Don't do anything if no valid selection.
                if (targetNetItem.OriginalPrefab == null || SelectedNet == null)
                {
                    return;
                }

                // Check current mode.
                if (CurrentMode == ReplacementModes.All)
                {
                    // All-network replacement; iterate through all prefabs and find matching prop references.
                    for (uint i = 0; i < PrefabCollection<NetInfo>.LoadedCount(); ++i)
                    {
                        NetInfo prefab = PrefabCollection<NetInfo>.GetLoaded(i);
                        NetInfo.Lane[] lanes = prefab?.m_lanes;
                        if (lanes != null)
                        {
                            for (int j = 0; j < prefab.m_lanes.Length; ++j)
                            {
                                NetLaneProps.Prop[] laneProps = lanes[j]?.m_laneProps?.m_props;
                                if (laneProps != null)
                                {
                                    for (int k = 0; k < laneProps.Length; ++k)
                                    {
                                        if (laneProps[k].m_prop == targetNetItem.ReplacementPrefab || laneProps[k].m_tree == targetNetItem.ReplacementPrefab)
                                        {
                                            _originalValues.Add(GetOriginalData(prefab, j, k));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (targetNetItem.PropIndex < 0)
                {
                    // Grouped replacement - iterate through each instance and record values.
                    for (int i = 0; i < targetNetItem.PropIndexes.Count; ++i)
                    {
                        _originalValues.Add(GetOriginalData(SelectedNet, targetNetItem.LaneIndexes[i], targetNetItem.PropIndexes[i]));
                    }
                }
                else
                {
                    // Individual replacement - record original values.
                    _originalValues.Add(GetOriginalData(SelectedNet, targetNetItem.LaneIndex, targetNetItem.PropIndex));
                }
            }
        }

        /// <summary>
        /// Updates button states (enabled/disabled) according to current control states.
        /// </summary>
        protected override void UpdateButtonStates()
        {
            base.UpdateButtonStates();

            // Make sure add button is only enabled if the lane menu is visible and has a valid lane selection.
            if (m_addButton != null)
            {
                m_addButton.isVisible &= _laneMenu.isVisible;
                m_removeButton.isVisible &= _laneMenu.isVisible;
                m_addButton.isEnabled &= _laneMenu.selectedIndex > 0;
            }
        }

        /// <summary>
        /// Sets the sliders to the values specified in the given replacement record.
        /// </summary>
        /// <param name="replacement">Replacement record to use.</param>
        protected override void SetSliders(BOBConfig.ReplacementBase replacement)
        {
            // Disable events.
            m_ignoreSliderValueChange = true;

            if (SelectedTargetItem is TargetNetItem targetNetItem)
            {
                // Are we eligible for repeat distance (eligibile target and in individual mode).
                if (CurrentMode == ReplacementModes.Individual && targetNetItem != null && (targetNetItem.OriginalRepeat > 1f || targetNetItem.IsAdded))
                {
                    // Yes - do we have a replacement?
                    if (replacement is BOBConfig.NetReplacement netReplacement && netReplacement.RepeatDistance > 1f)
                    {
                        // Yes - set repeat distance slider value and show the slider.
                        _repeatSlider.TrueValue = netReplacement.RepeatDistance;
                    }
                    else
                    {
                        // No replacement; show original value.
                        _repeatSlider.TrueValue = targetNetItem.OriginalRepeat;
                    }

                    // Show slider.
                    _repeatSlider.parent.Show();
                }
                else
                {
                    // Hide repeat slider if no value to show.
                    _repeatSlider.TrueValue = 0f;
                    _repeatSlider.parent.Hide();
                }
            }

            base.SetSliders(replacement);
        }

        /// <summary>
        /// Called after any added prop manipulations (addition or removal) to perform cleanup.
        /// </summary>
        private void UpdateAddedPops()
        {
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
        private void PreviewChange(LanePropHandler handler, BOBConfig.NetReplacement previewReplacement)
        {
            handler.PreviewReplacement(previewReplacement);

            // Add network to dirty list.
            NetData.DirtyList.Add(handler.NetInfo);
        }

        /// <summary>
        /// Gets original (current) prop data.
        /// </summary>
        /// <param name="netInfo">Network prefab.</param>
        /// <param name="lane">Lane index.</param>
        /// <param name="propIndex">Prop index.</param>
        /// <returns>New prop handler containing original data.</returns>
        private LanePropHandler GetOriginalData(NetInfo netInfo, int lane, int propIndex)
        {
            // Ensure that the indexes are valid before proceeding.
            if (netInfo?.m_lanes == null || netInfo.m_lanes.Length <= lane)
            {
                Logging.Error("invalid lane index reference of ", lane, " for selected network ", SelectedNet?.name ?? "null");
                return null;
            }

            NetInfo.Lane thisLane = netInfo.m_lanes[lane];
            NetLaneProps.Prop[] propBuffer = thisLane?.m_laneProps?.m_props;
            if (propBuffer == null || propBuffer.Length <= propIndex)
            {
                Logging.Error("invalid prop index reference of ", propIndex, " for lane ", lane, " of selected network ", SelectedNet?.name ?? "null");
                return null;
            }

            // Create a new prop handler based on the current prop state (not the original).
            return NetHandlers.GetOrAddHandler(netInfo, thisLane, propIndex);
        }

        /// <summary>
        /// Lane menu index changed event handler.
        /// <param name="c">Calling componen.</param>
        /// <param name="index">New index.</param>
        /// </summary>
        private void LaneIndexChanged(UIComponent c, int index)
        {
            // Clear the tool's list of lanes to render.
            BOBTool tool = BOBTool.Instance;
            tool.LaneOverlays.Clear();

            // If the index is greater, there's a lane selection to highlight.
            if (index > 0)
            {
                // Local references.
                NetManager netManager = Singleton<NetManager>.instance;
                NetSegment[] segments = netManager.m_segments.m_buffer;
                NetLane[] lanes = netManager.m_lanes.m_buffer;

                // Lane index is offset for menu index by 1 to allow for the 'All' item at menu index 0.
                int laneIndex = index - 1;

                // Iterate through all segments on map.
                for (int i = 0; i < segments.Length; ++i)
                {
                    // Check for valid segments that match the selected NetInfo.
                    if ((segments[i].m_flags & NetSegment.Flags.Created) == 0 || segments[i].Info != SelectedNet)
                    {
                        continue;
                    }

                    // Iterate through segment lanes until we reach the one we need.
                    uint laneID = segments[i].m_lanes;
                    for (int j = 0; j < laneIndex; ++j)
                    {
                        // Safety check.
                        if (laneID == 0)
                        {
                            break;
                        }

                        // Get ID of next lane in segment.
                        laneID = lanes[laneID].m_nextLane;
                    }

                    // If we ended up with a valid lane ID, add the bezier to the list of lane overlays to be rendered.
                    if (laneID != 0)
                    {
                        tool.LaneOverlays.Add(lanes[laneID].m_bezier);
                    }
                }
            }

            // Regenerate target list and update controls if events aren't suspended.
            if (!_ignoreIndexChange)
            {
                RegenerateTargetList();
                UpdateButtonStates();
            }
        }
    }
}
