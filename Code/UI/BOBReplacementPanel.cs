// <copyright file="BOBReplacementPanel.cs" company="algernon (K. Algernon A. Sheppard)">
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
    /// Abstract class for building and network BOB tree/prop replacement panels.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Protected fields")]
    internal abstract class BOBReplacementPanel : BOBReplacementPanelBase
    {
        /// <summary>
        /// Mode button strip relative X-position.
        /// </summary>
        protected const float ModeX = Margin + (ToggleSize * 4f);

        /// <summary>
        /// Slider height.
        /// </summary>
        protected const float SliderHeight = 38f;

        /// <summary>
        /// Repeat distance slider relative Y-position.
        /// </summary>
        protected const float RepeatSliderY = ListBottom - FieldOffset;

        /// <summary>
        /// 'Fixed height' checkbock relative Y position.
        /// </summary>
        protected const float FixedHeightY = OffsetLabelY + 20f;

        /// <summary>
        /// Relatative Y-position of the bottom of the height slider panel.
        /// </summary>
        protected const float HeightPanelBottomY = YOffsetY + SliderHeight;

        /// <summary>
        /// Relative X-position of the random panel button.
        /// </summary>
        protected const float RandomButtonX = MiddleX + ToggleSize;

        /// <summary>
        /// Original prefab values (for reference when previewing).
        /// </summary>
        protected readonly List<PropHandler> m_originalValues = new List<PropHandler>();

        /// <summary>
        /// Probability slider.
        /// </summary>
        protected BOBSlider m_probabilitySlider;

        /// <summary>
        /// X-position offset slider.
        /// </summary>
        protected BOBSlider m_xSlider;

        /// <summary>
        /// X-position offset slider.
        /// </summary>
        protected BOBSlider m_ySlider;

        /// <summary>
        /// X-position offset slider.
        /// </summary>
        protected BOBSlider m_zSlider;

        /// <summary>
        /// Rotation adjustment slider.
        /// </summary>
        protected BOBSlider m_rotationSlider;

        /// <summary>
        /// 'Add new prop' button.
        /// </summary>
        protected UIButton m_addButton;

        /// <summary>
        /// 'Remove added prop' button.
        /// </summary>
        protected UIButton m_removeButton;

        /// <summary>
        /// Height adjustement panel.
        /// </summary>
        protected UIPanel m_heightPanel;

        /// <summary>
        /// Ignore slider value change events.
        /// </summary>
        protected bool m_ignoreSliderValueChange = true;

        // Mode button strip relative Y-position.
        private const float ModeY = ToggleY;
        private const float FieldOffset = SliderHeight + Margin;
        private const float OffsetLabelY = Margin;
        private const float XOffsetY = OffsetLabelY + 20f;
        private const float ZOffsetY = XOffsetY + SliderHeight;
        private const float OffsetPanelHeight = ZOffsetY + SliderHeight;
        private const float HeightPanelShortHeight = FixedHeightY + SliderHeight;

        // Layout constants - detail control locations - align bottom with bottom of lists, and work up.
        private const float HeightPanelY = RepeatSliderY - HeightPanelShortHeight;
        private const float OffsetPanelY = HeightPanelY - OffsetPanelHeight - Margin;
        private const float YOffsetY = FixedHeightY + 20f;
        private const float AngleY = OffsetPanelY - FieldOffset;
        private const float ProbabilityY = AngleY - FieldOffset;

        // Layout constants - other controls.
        private const float ActionsY2 = ActionsY + ActionSize;

        private readonly UICheckBox _randomCheck;
        private readonly UICheckBox[] _modeChecks;
        private readonly UIPanel _rotationPanel;
        private readonly UIButton _hideButton;

        // Status flags.
        private bool _ignoreModeCheckChanged = false;
        private bool _unappliedChanges = false;

        // Current replacement mode.
        private ReplacementModes _currentMode = ReplacementModes.Grouped;

        /// <summary>
        /// Initializes a new instance of the <see cref="BOBReplacementPanel"/> class.
        /// </summary>
        internal BOBReplacementPanel()
        {
            try
            {
                // Replacement mode buttons.
                _modeChecks = new UICheckBox[(int)ReplacementModes.NumModes];
                for (int i = 0; i < (int)ReplacementModes.NumModes; ++i)
                {
                    bool useTreeLabels = PropTreeMode == PropTreeModes.Tree;
                    _modeChecks[i] = IconToggleCheck(this, ModeX + (i * ToggleSize), ModeY, useTreeLabels ? TreeModeAtlas[i] : PropModeAtlas[i], useTreeLabels ? TreeModeTipKeys[i] : PropModeTipKeys[i]);
                    _modeChecks[i].objectUserData = i;
                    _modeChecks[i].eventCheckChanged += ModeCheckChanged;
                }

                // Set initial mode state.
                _modeChecks[(int)CurrentMode].isChecked = true;

                // Adjust mode label position to be centred over all mode toggles.
                float modeRight = ModeX + ((float)ReplacementModes.NumModes * ToggleSize);
                float modeOffset = (modeRight - Margin - m_modeLabel.width) / 2f;
                m_modeLabel.relativePosition += new Vector3(modeOffset, 0f);

                // Hide button.
                _hideButton = AddIconButton(this, MidControlX + ActionSize + ActionSize, ActionsY, ActionSize, "BOB_PNL_HID", UITextures.LoadQuadSpriteAtlas("BOB-InvisibleProp"));
                _hideButton.eventClicked += HideProp;

                // Probability.
                UIPanel probabilityPanel = Sliderpanel(this, MidControlX, ProbabilityY, SliderHeight);
                m_probabilitySlider = AddBOBSlider(probabilityPanel, Margin, 0f, MidControlWidth - (Margin * 2f), "BOB_PNL_PRB", 0, 100, 1, "Probability");
                m_probabilitySlider.TrueValue = 100f;
                m_probabilitySlider.LimitToVisible = true;

                // Angle.
                _rotationPanel = Sliderpanel(this, MidControlX, AngleY, SliderHeight);
                m_rotationSlider = AddBOBSlider(_rotationPanel, Margin, 0f, MidControlWidth - (Margin * 2f), "BOB_PNL_ANG", -180, 180, 1, "Angle");

                // Offset panel.
                UIPanel offsetPanel = Sliderpanel(this, MidControlX, OffsetPanelY, OffsetPanelHeight);
                UILabel offsetLabel = UILabels.AddLabel(offsetPanel, 0f, OffsetLabelY, Translations.Translate("BOB_PNL_OFF"));
                offsetLabel.textAlignment = UIHorizontalAlignment.Center;
                while (offsetLabel.width > MidControlWidth)
                {
                    offsetLabel.textScale -= 0.05f;
                    offsetLabel.PerformLayout();
                }

                offsetLabel.relativePosition = new Vector2((offsetPanel.width - offsetLabel.width) / 2f, OffsetLabelY);

                // Offset sliders.
                m_xSlider = AddBOBSlider(offsetPanel, Margin, XOffsetY, MidControlWidth - (Margin * 2f), "BOB_PNL_XOF", -32f, 32f, 0.01f, "X offset");
                m_zSlider = AddBOBSlider(offsetPanel, Margin, ZOffsetY, MidControlWidth - (Margin * 2f), "BOB_PNL_ZOF", -32f, 32f, 0.01f, "Z offset");

                // Height panel.
                m_heightPanel = Sliderpanel(this, MidControlX, HeightPanelY, HeightPanelShortHeight);
                UILabel heightLabel = UILabels.AddLabel(m_heightPanel, 0f, OffsetLabelY, Translations.Translate("BOB_PNL_HEI"));
                m_ySlider = AddBOBSlider(m_heightPanel, Margin, YOffsetY - 20f, MidControlWidth - (Margin * 2f), "BOB_PNL_YOF", -32f, 32f, 0.01f, "Y offset");
                while (heightLabel.width > MidControlWidth)
                {
                    heightLabel.textScale -= 0.05f;
                    heightLabel.PerformLayout();
                }

                heightLabel.relativePosition = new Vector2((m_heightPanel.width - heightLabel.width) / 2f, OffsetLabelY);

                // Live application of position changes.
                m_xSlider.EventTrueValueChanged += SliderChange;
                m_ySlider.EventTrueValueChanged += SliderChange;
                m_zSlider.EventTrueValueChanged += SliderChange;
                m_rotationSlider.EventTrueValueChanged += SliderChange;
                m_probabilitySlider.EventTrueValueChanged += SliderChange;

                // Normal/random toggle.
                _randomCheck = UICheckBoxes.AddLabelledCheckBox((UIComponent)(object)this, m_hideVanilla.relativePosition.x, m_hideVanilla.relativePosition.y + m_hideVanilla.height + (Margin / 2f), Translations.Translate("BOB_PNL_RSW"), 12f, 0.7f);
                _randomCheck.eventCheckChanged += RandomCheckChanged;

                // Random settings button.
                UIButton randomButton = AddIconButton(this, RandomButtonX, ToggleY, ToggleSize, "BOB_PNL_RST", UITextures.LoadQuadSpriteAtlas("BOB-Random"));
                randomButton.eventClicked += (c, clickEvent) => StandalonePanelManager<BOBRandomPanel>.Create();

                // Set initial button states.
                UpdateButtonStates();
                UpdateModeIcons();

                // Add button.
                m_addButton = AddIconButton(this, MidControlX, ActionsY2, ActionSize, "BOB_PNL_ADD", UITextures.LoadQuadSpriteAtlas("BOB-RoundPlus"));
                m_addButton.eventClicked += (c, clickEvent) => AddNew();

                // Remove button.
                m_removeButton = AddIconButton(this, MidControlX + ActionSize, ActionsY2, ActionSize, "BOB_PNL_REM", UITextures.LoadQuadSpriteAtlas("BOB-RoundMinus"));
                m_removeButton.eventClicked += (c, clickEvent) => RemoveProp();

                // Add/remove button initial visibility.
                bool eligibleMode = CurrentMode == ReplacementModes.Individual | CurrentMode == ReplacementModes.Grouped;
                m_addButton.isVisible = eligibleMode;
                m_removeButton.isVisible = eligibleMode;
            }
            catch (Exception e)
            {
                // Log and report any exception.
                Logging.LogException(e, "exception creating info panel");
            }
        }

        /// <summary>
        /// Replacement modes.
        /// </summary>
        protected enum ReplacementModes : int
        {
            /// <summary>
            /// Individual replacement mode.
            /// </summary>
            Individual = 0,

            /// <summary>
            /// Grouped replacement mode.
            /// </summary>
            Grouped,

            /// <summary>
            /// All- replacement mode.
            /// </summary>
            All,

            /// <summary>
            /// Number of replacement modes.
            /// </summary>
            NumModes,
        }

        /// <summary>
        /// Sets the current replacement prefab and updates button states accordingly.
        /// </summary>
        internal override PrefabInfo SelectedReplacementPrefab
        {
            set
            {
                base.SelectedReplacementPrefab = value;
                PreviewChange();
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

                // Don't show rotation slider for trees.
                _rotationPanel.isVisible = !(value?.ActivePrefab is TreeInfo);

                // Record original stats for preview.
                RecordOriginals();
            }
        }

        /// <summary>
        /// Gets the mode icon atlas names for prop modes.
        /// </summary>
        protected abstract string[] PropModeAtlas { get; }

        /// <summary>
        /// Gets the mode icon atlas names for tree modes.
        /// </summary>
        protected abstract string[] TreeModeAtlas { get; }

        /// <summary>
        /// Gets the mode icon tooltip keys for prop modes.
        /// </summary>
        protected abstract string[] PropModeTipKeys { get; }

        /// <summary>
        /// Gets the mode icon tooltip keys for tree modes.
        /// </summary>
        protected abstract string[] TreeModeTipKeys { get; }

        /// <summary>
        /// Gets a value indicating whether there are currrently unapplied changes.
        /// </summary>
        protected virtual bool AreUnappliedChanges
        {
            get
            {
                return
                SelectedTargetItem != null
                &&
                (m_xSlider.value != 0f
                || m_ySlider.value != 0f
                || m_zSlider.value != 0f
                || m_rotationSlider.value != 0f
                || m_probabilitySlider.value.RoundToNearest(1) != SelectedTargetItem.OriginalProbability
                || SelectedReplacementPrefab != SelectedTargetItem.ActivePrefab);
            }
        }

        /// <summary>
        /// Gets the panel's title.
        /// </summary>
        protected override string PanelTitle => Translations.Translate("BOB_NAM");

        /// <summary>
        /// Gets the initial tree/prop checked state for this panel.
        /// </summary>
        protected override PropTreeModes InitialPropTreeMode => ModSettings.LastPropTreeMode;

        /// <summary>
        /// Gets or sets a value indicating whether there are currently any unapplied changes.
        /// </summary>
        protected bool UnappliedChanges
        {
            get => _unappliedChanges;

            set
            {
                // Don't do anything if no changes.
                if (_unappliedChanges != value)
                {
                    _unappliedChanges = value;

                    // Update apply button atlas to show/hide alert mark as appropriate.
                    m_applyButton.atlas = UITextures.LoadQuadSpriteAtlas(value ? "BOB-OkSmallWarn" : "BOB-OkSmall");

                    // Set button states for new state.
                    UpdateButtonStates();
                }
            }
        }

        /// <summary>
        /// Gets or sets the current replacement mode.
        /// </summary>
        protected virtual ReplacementModes CurrentMode
        {
            get => _currentMode;

            set
            {
                if (_currentMode != value)
                {
                    _currentMode = value;

                    // Update render overlays.
                    if (_currentMode == ReplacementModes.Individual || _currentMode == ReplacementModes.Grouped)
                    {
                        // Update render target to specific building/net.
                        RenderOverlays.Building = SelectedBuilding;
                        RenderOverlays.Network = SelectedNet;
                    }
                    else
                    {
                        // Clear render prefab references to render overlays for all prefabs.
                        RenderOverlays.Building = null;
                        RenderOverlays.Network = null;
                    }

                    // Regenerate target list.
                    RegenerateTargetList();
                }
            }
        }

        /// <summary>
        /// Sets the target parent prefab.
        /// </summary>
        /// <param name="targetPrefabInfo">Target prefab to set.</param>
        internal override void SetTargetParent(PrefabInfo targetPrefabInfo)
        {
            // First, undo any preview.
            RevertPreview();

            base.SetTargetParent(targetPrefabInfo);

            // Update title label.
            TitleText = Translations.Translate("BOB_NAM") + ": " + GetDisplayName(targetPrefabInfo.name);
        }

        /// <summary>
        /// Refreshes the panel's target list (called when external factors change, e.g. pack replacements).
        /// </summary>
        internal override void RefreshTargetList() => RegenerateTargetList();

        /// <summary>
        /// Refreshes the random prop/tree list.
        /// </summary>
        internal void RefreshRandom()
        {
            if (_randomCheck.isChecked)
            {
                // Regenerate replacement list.
                RegenerateReplacementList();
            }
        }

        /// <summary>
        /// Adds a new tree or prop.
        /// </summary>
        protected abstract void AddNew();

        /// <summary>
        /// Record original prop values before previewing.
        /// </summary>
        protected abstract void RecordOriginals();

        /// <summary>
        /// Regenerate render and prefab data.
        /// </summary>
        protected abstract void UpdateData();

        /// <summary>
        /// Removes the currently selected added prop.
        /// </summary>
        protected abstract void RemoveAddedProp();

        /// <summary>
        /// Generates a new replacement record from current control settings.
        /// </summary>
        /// <returns>New replacement record.</returns>
        protected abstract BOBConfig.Replacement GetReplacementFromControls();

        /// <summary>
        /// Previews the current change.
        /// </summary>
        protected virtual void PreviewChange()
        {
            // Don't do anything if no current selection.
            if (SelectedTargetItem == null)
            {
                return;
            }

            // Don't do anything if no changes.
            if (!AreUnappliedChanges)
            {
                // Reset apply button icon.
                UnappliedChanges = false;

                return;
            }

            // Update preview for each handler.
            BOBConfig.Replacement previewReplacement = GetReplacementFromControls();

            foreach (PropHandler handler in m_originalValues)
            {
                handler.PreviewReplacement(previewReplacement);
            }

            // Update renders.
            UpdateData();

            // Update highlighting target.
            RenderOverlays.Prop = SelectedReplacementPrefab as PropInfo;
            RenderOverlays.Tree = SelectedReplacementPrefab as TreeInfo;

            // Update apply button icon to indicate change.
            UnappliedChanges = true;
        }

        /// <summary>
        /// Removes an added tree or prop.
        /// </summary>
        protected virtual void RemoveProp()
        {
            // First, revert any preview (to prevent any clobbering when preview is reverted).
            RevertPreview();

            // Create new props array with one fewer entry, and copy the old props to it.
            // Remove prop reference and update other references as appropriate.
            RemoveAddedProp();

            // Regenerate original list to reflect new state.
           // RecordOriginals();

            // Post-action cleanup.
            UpdateAddedProps();
        }

        /// <summary>
        /// Event handler for ptop/tree checkbox changes.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="isChecked">New checked state.</param>
        protected override void PropTreeCheckChanged(UIComponent c, bool isChecked)
        {
            base.PropTreeCheckChanged(c, isChecked);

            // Save last used mode.
            ModSettings.LastPropTreeMode = PropTreeMode;

            // Update mode icons.
            UpdateModeIcons();
        }

        /// <summary>
        /// Updates button states (enabled/disabled) according to current control states.
        /// </summary>
        protected override void UpdateButtonStates()
        {
            // Disable by default (selectively (re)-enable if eligible).
            m_applyButton.Disable();
            m_revertButton.Disable();
            _hideButton.Disable();

            // Buttons are only enabled if a current target item is selected.
            if (SelectedTargetItem != null)
            {
                // Replacement requires a valid replacement selection.
                if (SelectedReplacementPrefab != null)
                {
                    m_applyButton.Enable();
                }

                // Reversion requires a currently active replacement.
                if (SelectedTargetItem.HasActiveReplacement)
                {
                    m_revertButton.Enable();
                    m_revertButton.tooltip = Translations.Translate("BOB_PNL_REV_UND");
                }
                else
                {
                    m_revertButton.tooltip = Translations.Translate("BOB_PNL_REV_TIP");
                }

                // Hide button is enabled whenever there's a valid target item.
                _hideButton.Enable();
            }

            // Show revert button if unapplied changes.
            if (UnappliedChanges)
            {
                m_revertButton.Enable();
                m_revertButton.tooltip = Translations.Translate("BOB_PNL_REV_UND");
            }

            // Don't do anything to add/remove buttons if buttons haven't been created yet.
            if (m_addButton == null || m_removeButton == null)
            {
                return;
            }

            // Disable/enable add new prop button.
            m_addButton.isEnabled = SelectedReplacementPrefab != null;

            // Disable/enable remove new prop button.
            m_removeButton.isEnabled = SelectedTargetItem != null && SelectedTargetItem.IsAdded;
        }

        /// <summary>
        /// Populates the replacement UIList with a filtered list of eligible relacement trees or props.
        /// </summary>
        protected override void RegenerateReplacementList()
        {
            // Are we using random props?
            if (_randomCheck.isChecked)
            {
                // Yes - show only random trees/props.
                if (PropTreeMode == PropTreeModes.Tree)
                {
                    // Trees.
                    m_replacementList.Data = new FastList<object>
                    {
                        m_buffer = PrefabLists.RandomTrees.OrderBy(x => x.Name.ToLower()).ToArray(),
                        m_size = PrefabLists.RandomTrees.Count,
                    };
                }
                else if (PropTreeMode == PropTreeModes.Prop)
                {
                    // Props.
                    m_replacementList.Data = new FastList<object>
                    {
                        m_buffer = PrefabLists.RandomProps.OrderBy(x => x.Name.ToLower()).ToArray(),
                        m_size = PrefabLists.RandomProps.Count,
                    };
                }
                else if (PropTreeMode == PropTreeModes.Both)
                {
                    // Trees and props - combine both.
                    List<BOBRandomPrefab> randomList = new List<BOBRandomPrefab>(PrefabLists.RandomProps.Count + PrefabLists.RandomTrees.Count);
                    randomList.AddRange(PrefabLists.RandomProps);
                    randomList.AddRange(PrefabLists.RandomTrees);

                    m_replacementList.Data = new FastList<object>
                    {
                        m_buffer = randomList.OrderBy(x => x.Name.ToLower()).ToArray(),
                        m_size = randomList.Count,
                    };
                }

                // Reverse order of filtered list if we're searching name descending.
                if (m_replacementSortSetting == (int)OrderBy.NameDescending)
                {
                    Array.Reverse(m_replacementList.Data.m_buffer);
                    m_replacementList.Refresh();
                }
            }
            else
            {
                // No - show normal loaded prefab list.
                base.RegenerateReplacementList();
            }
        }

        /// <summary>
        /// Performs any actions required before closing the panel.
        /// </summary>
        /// <returns>True if the panel can close now, false otherwise.</returns>
        protected override bool PreClose()
        {
            // Revert any preview.
            RevertPreview();

            // Perform post-update tasks, such as saving the config file and refreshing renders.
            FinishUpdate();

            return true;
        }

        /// <summary>
        /// Performs actions to be taken once an update (application or reversion) has been applied, including saving data, updating button states, and refreshing renders.
        /// </summary>
        protected override void FinishUpdate()
        {
            base.FinishUpdate();

            // Update any dirty net renders.
            UpdateData();
        }

        /// <summary>
        /// Called after any added prop manipulations (addition or removal) to perform cleanup.
        /// </summary>
        protected virtual void UpdateAddedProps()
        {
            // Perform regular post-processing.
            FinishUpdate();
        }

        /// <summary>
        /// Sets the sliders to the values specified in the given replacement record.
        /// </summary>
        /// <param name="replacement">Replacement record to use.</param>
        protected virtual void SetSliders(BOBConfig.Replacement replacement)
        {
            // Disable events.
            m_ignoreSliderValueChange = true;

            // Null check first.
            if (replacement == null)
            {
                // In the absense of valid data, set all offset fields to defaults.
                m_rotationSlider.TrueValue = 0f;
                m_xSlider.TrueValue = 0;
                m_ySlider.TrueValue = 0;
                m_zSlider.TrueValue = 0;
                m_probabilitySlider.TrueValue = SelectedTargetItem != null ? SelectedTargetItem.OriginalProbability : 100;
            }
            else
            {
                // Valid replacement - set slider values.
                m_rotationSlider.TrueValue = replacement.Angle;
                m_xSlider.TrueValue = replacement.OffsetX;
                m_ySlider.TrueValue = replacement.OffsetY;
                m_zSlider.TrueValue = replacement.OffsetZ;
                m_probabilitySlider.TrueValue = replacement.Probability;
            }

            // Re-enable events.
            m_ignoreSliderValueChange = false;
        }

        /// <summary>
        /// Reverts any previewed changes back to original prop/tree state.
        /// </summary>
        protected void RevertPreview()
        {
            // Iterate through each original value.
            foreach (PropHandler handler in m_originalValues)
            {
                // Restore original values.
                handler.ClearPreview();
            }

            // Update prefabs.
            UpdateData();

            // Reset apply button icon
            UnappliedChanges = false;
        }

        /// <summary>
        /// Adds a slider panel to the specified component.
        /// </summary>
        /// <param name="parent">Parent component.</param>
        /// <param name="xPos">Relative X position.</param>
        /// <param name="yPos">Relative Y position.</param>
        /// <param name="height">Panel height.</param>
        /// <returns>New UIPanel.</returns>
        protected UIPanel Sliderpanel(UIComponent parent, float xPos, float yPos, float height)
        {
            // Slider panel.
            UIPanel sliderPanel = parent.AddUIComponent<UIPanel>();
            sliderPanel.atlas = UITextures.InGameAtlas;
            sliderPanel.backgroundSprite = "GenericPanel";
            sliderPanel.color = new Color32(206, 206, 206, 255);
            sliderPanel.size = new Vector2(MidControlWidth, height);
            sliderPanel.relativePosition = new Vector2(xPos, yPos);

            return sliderPanel;
        }

        /// <summary>
        /// Event handler for applying live changes on slider value change.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="isChecked">New checked state.</param>
        protected void SliderChange(UIComponent c, float isChecked)
        {
            // Don't do anything if already ignoring events.
            if (!m_ignoreSliderValueChange)
            {
                // Disable events while applying changes.
                m_ignoreSliderValueChange = true;
                PreviewChange();
                m_ignoreSliderValueChange = false;
            }
        }

        /// <summary>
        /// Event handler for mode checkbox changes.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="isChecked">New checked state.</param>
        private void ModeCheckChanged(UIComponent c, bool isChecked)
        {
            // Don't do anything if we're ignoring events.
            if (_ignoreModeCheckChanged)
            {
                return;
            }

            // Suspend event handling while processing.
            _ignoreModeCheckChanged = true;

            if (c is UICheckBox thisCheck)
            {
                // If this checkbox is being enabled, uncheck all others:
                if (isChecked)
                {
                    // Don't do anything if the selected mode index isn't different to the current mode.
                    if (thisCheck.objectUserData is int index && index != (int)CurrentMode)
                    {
                        // Iterate through all checkboxes, unchecking all those that aren't this one (checkbox index stored in objectUserData).
                        for (int i = 0; i < (int)ReplacementModes.NumModes; ++i)
                        {
                            if (i != index)
                            {
                                _modeChecks[i].isChecked = false;
                            }
                        }

                        // Set current replacement mode, while saving old value.
                        ReplacementModes oldMode = CurrentMode;
                        CurrentMode = (ReplacementModes)index;

                        // Update target list if we've changed between individual and grouped modes (we've already filtered out non-changes, so checking for any individual mode will do).
                        if (oldMode == ReplacementModes.Individual || CurrentMode == ReplacementModes.Individual)
                        {
                            // Rebuild target list.
                            RegenerateTargetList();
                        }
                    }
                }
                else
                {
                    // If no other check is checked, force this one to still be checked.
                    thisCheck.isChecked = true;
                }
            }

            // Resume event handling.
            _ignoreModeCheckChanged = false;
        }

        /// <summary>
        /// Updates mode icons and tooltips (when switching between trees and props).
        /// </summary>
        private void UpdateModeIcons()
        {
            string[] atlasNames, tipKeys;

            // Null check to avoid race condition with base constructor.
            if (_modeChecks == null)
            {
                return;
            }

            // Get releveant atlases and tooltips for current mode.
            if (PropTreeMode == PropTreeModes.Tree)
            {
                atlasNames = TreeModeAtlas;
                tipKeys = TreeModeTipKeys;
            }
            else
            {
                atlasNames = PropModeAtlas;
                tipKeys = PropModeTipKeys;
            }

            // Iterate through all mode checks.
            for (int i = 0; i < (int)ReplacementModes.NumModes; ++i)
            {
                // Load atlas.
                UITextureAtlas checkAtlas = UITextures.LoadQuadSpriteAtlas(atlasNames[i]);

                // Update unchecked sprite.
                UISprite uncheckedSprite = _modeChecks[i].Find<UISprite>("UncheckedSprite");
                uncheckedSprite.atlas = checkAtlas;

                // Update checked sprite.
                ((UISprite)_modeChecks[i].checkedBoxObject).atlas = checkAtlas;

                // Update tooltip.
                _modeChecks[i].tooltip = Translations.Translate(tipKeys[i]);
            }
        }

        /// <summary>
        /// Random check event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="isChecked">New checked state.</param>
        private void RandomCheckChanged(UIComponent c, bool isChecked)
        {
            // Regenerate replacement list.
            RegenerateReplacementList();
        }

        /// <summary>
        /// Hides the selected prop/tree.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="p">Mouse event parameter.</param>
        private void HideProp(UIComponent c, UIMouseEventParameter p) => m_probabilitySlider.TrueValue = 0f;

        /// <summary>
        /// Returns a cleaned-up display name for the given prefab.
        /// </summary>
        /// <param name="prefabName">Raw prefab name.</param>
        /// <returns>Cleaned display name.</returns>
        private string GetDisplayName(string prefabName) => prefabName.Substring(prefabName.IndexOf('.') + 1).Replace("_Data", string.Empty);
    }
}
