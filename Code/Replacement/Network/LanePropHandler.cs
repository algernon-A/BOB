// <copyright file="LanePropHandler.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using AlgernonCommons;
    using BOB.Skins;
    using UnityEngine;

    /// <summary>
    /// Handles an individual BuildingProp tree/prop record, including original values and any active replacements.
    /// </summary>
    public class LanePropHandler : PropHandler
    {
        // Parent network prefab.
        private readonly NetInfo _netInfo;

        // Parent lane prefab.
        private readonly NetInfo.Lane _originalLaneInfo;
        private readonly ushort _segmentID;
        private readonly int _laneIndex;

        // Original prop data.
        private readonly float _originalAngle;
        private readonly float _originalRepeatDistance;

        // Active replacement references.
        private BOBConfig.NetReplacement _addedReplacement;
        private BOBConfig.NetReplacement _individualReplacement;
        private BOBConfig.NetReplacement _groupedReplacement;
        private BOBConfig.NetReplacement _allReplacement;
        private BOBConfig.NetReplacement _packReplacement;

        /// <summary>
        /// Initializes a new instance of the <see cref="LanePropHandler"/> class for a generic info record.
        /// </summary>
        /// <param name="prefab"><see cref="NetInfo"/> prefab containing this prop.</param>
        /// <param name="segmentID">Segment ID (for a skin), default 0 (no skin).</param>
        /// <param name="laneInfo"><see cref="NetInfo.Lane"/> containing this prop (must not be null).</param>
        /// <param name="laneIndex">Lane index.</param>
        /// <param name="propIndex">Prop index.</param>
        /// <param name="laneProp">Lane prop instance (must not be <null></null>).</param>
        public LanePropHandler(NetInfo prefab, ushort segmentID, NetInfo.Lane laneInfo, int laneIndex, int propIndex, NetLaneProps.Prop laneProp)
            : base(
                  propIndex,
                  laneProp.m_prop,
                  laneProp.m_finalProp,
                  laneProp.m_tree,
                  laneProp.m_finalTree,
                  laneProp.m_position,
                  laneProp.m_probability)
        {
            // Set original data.
            _netInfo = prefab;
            _segmentID = segmentID;
            _laneIndex = laneIndex;
            _originalLaneInfo = laneInfo;
            _originalAngle = laneProp.m_angle;
            _originalRepeatDistance = laneProp.m_repeatDistance;

            // Clone NetLaneProp if required.
            NetData.CloneLanePropInstance(prefab, laneInfo);
        }

        /// <summary>
        /// Gets the network prefab for this prop.
        /// </summary>
        public NetInfo NetInfo => _netInfo;

        /// <summary>
        /// Gets the segment ID for this handler.
        /// </summary>
        public ushort SegmentID => _segmentID;

        /// <summary>
        /// Gets the lane info for this prop.
        /// </summary>
        public NetInfo.Lane LaneInfo => _originalLaneInfo;

        /// <summary>
        /// Gets the original rotation angle in degrees.
        /// </summary>
        public float OriginalAngle => _originalAngle;

        /// <summary>
        /// Gets the original repeat distance.
        /// </summary>
        public float OriginalRepeatDistance => _originalRepeatDistance;

        /// <summary>
        /// Gets the highest priority of any currently active replacements.
        /// </summary>
        public ReplacementPriority ActivePriority
        {
            get
            {
                // Check highest priorities first.
                if (_addedReplacement != null)
                {
                    return ReplacementPriority.AddedReplacement;
                }

                if (_individualReplacement != null)
                {
                    return ReplacementPriority.IndividualReplacement;
                }

                if (_groupedReplacement != null)
                {
                    return ReplacementPriority.GroupedReplacement;
                }

                if (_allReplacement != null)
                {
                    return ReplacementPriority.AllReplacement;
                }

                if (_packReplacement != null)
                {
                    return ReplacementPriority.PackReplacement;
                }

                // If we got here, there's no elgibile active replacement.
                return ReplacementPriority.NoReplacement;
            }
        }

        /// <summary>
        /// Gets the highest-priority currently active replacement (null if none).
        /// </summary>
        public BOBConfig.NetReplacement ActiveReplacement
        {
            get
            {
                // Check highest priorities first.
                // Check highest priorities first.
                if (_addedReplacement != null)
                {
                    return _addedReplacement;
                }

                if (_individualReplacement != null)
                {
                    return _individualReplacement;
                }

                if (_groupedReplacement != null)
                {
                    return _groupedReplacement;
                }

                if (_allReplacement != null)
                {
                    return _allReplacement;
                }

                if (_packReplacement != null)
                {
                    return _packReplacement;
                }

                // If we got here, there's no elgibile active replacement.
                return null;
            }
        }

        /// <summary>
        /// Gets the active replacement for the given priority for this prop (null if none).
        /// </summary>
        /// <param name="priority">Replacmeent priority to get.</param>
        /// <returns>Active bulding replacement for the given priority (null if none).</returns>
        public BOBConfig.NetReplacement GetReplacement(ReplacementPriority priority)
        {
            switch (priority)
            {
                case ReplacementPriority.AddedReplacement:
                    return _addedReplacement;
                case ReplacementPriority.IndividualReplacement:
                    return _individualReplacement;
                case ReplacementPriority.GroupedReplacement:
                    return _groupedReplacement;
                case ReplacementPriority.AllReplacement:
                    return _allReplacement;
                case ReplacementPriority.PackReplacement:
                    return _packReplacement;
                default:
                    Logging.Error("invalid priority ", priority, " passed to LanePropHandler.GetReplacement");
                    return null;
            }
        }

        /// <summary>
        /// Sets the active replacement for the current priority for this prop.
        /// Automatically updates the target prop when active replacements change as a result of this action.
        /// </summary>
        /// <param name="replacement">Replacement to set (must not be null; use ClearReplacement instead).</param>
        /// <param name="priority">Priority to apply to.</param>
        public void SetReplacement(BOBConfig.NetReplacement replacement, ReplacementPriority priority)
        {
            // Null check.
            if (replacement == null)
            {
                Logging.Error("null replacement passed to LanePropHandler.SetReplacement; use ClearReplacement instead");
                return;
            }

            // Record the current replacement priority before action.
            ReplacementPriority originalPriority = ActivePriority;

            switch (priority)
            {
                case ReplacementPriority.AddedReplacement:
                    _addedReplacement = replacement;
                    break;

                case ReplacementPriority.IndividualReplacement:
                    _individualReplacement = replacement;
                    break;

                case ReplacementPriority.GroupedReplacement:
                    _groupedReplacement = replacement;
                    break;

                case ReplacementPriority.AllReplacement:
                    _allReplacement = replacement;
                    break;

                case ReplacementPriority.PackReplacement:
                    _packReplacement = replacement;
                    break;

                default:
                    Logging.Error("invalid priority ", priority, " passed to LanePropHandler.SetReplacement");
                    return;
            }

            // Check to see if the replacement priority was the same (i.e. update existing replacement) or greater than the original priority.
            // Can't use ActivePriority here as it won't catch any updates of existing replacements.
            if (priority >= originalPriority)
            {
                // Priority has changed; update this prop.
                UpdateProp();
            }
        }

        /// <summary>
        /// Clears all references (if any) to the given replacement (for all priorities).
        /// Automatically updates the target prop when active replacements change as a result of this action.
        /// </summary>
        /// <param name="replacement">Replacement reference to clear.</param>
        public void ClearReplacement(BOBConfig.NetReplacement replacement)
        {
            // Null check.
            if (replacement == null)
            {
                Logging.Error("null replacement passed to LanePropHandler.ClearReplacement");
                return;
            }

            // Record the current replacement priority before action.
            ReplacementPriority originalPriority = ActivePriority;

            // Clear any references to this replacement.
            if (_individualReplacement == replacement)
            {
                _individualReplacement = null;
            }

            if (_groupedReplacement == replacement)
            {
                _groupedReplacement = null;
            }

            if (_allReplacement == replacement)
            {
                _allReplacement = null;
            }

            if (_packReplacement == replacement)
            {
                _packReplacement = null;
            }

            // Check to see if the currently active priority has changed as a result of this action.
            if (originalPriority != ActivePriority)
            {
                // Priority has changed; update this prop.
                UpdateProp();
            }
        }

        /// <summary>
        /// Reverts this prop to original values.
        /// Replacement references will NOT be removed.
        /// </summary>
        public void RevertToOriginal()
        {
            // Is this a skin?
            if (_segmentID != 0)
            {
                // Yes - remove skin change.
                NetworkSkins.SegmentSkins[_segmentID].RemoveChange(_laneIndex, PropIndex);
            }

            NetLaneProps.Prop thisProp = _originalLaneInfo?.m_laneProps?.m_props[PropIndex];
            if (thisProp != null)
            {
                // Added prop?
                if (_addedReplacement != null)
                {
                    // Added prop - revert to original means reverting to the original replacement values.
                    ReplaceProp(_addedReplacement);
                }
                else
                {
                    thisProp.m_prop = OriginalProp;
                    thisProp.m_finalProp = OriginalFinalProp;
                    thisProp.m_tree = OriginalTree;
                    thisProp.m_finalTree = OriginalFinalTree;
                    thisProp.m_angle = _originalAngle;
                    thisProp.m_position = OriginalPosition;
                    thisProp.m_probability = OriginalProbability;
                    thisProp.m_repeatDistance = _originalRepeatDistance;
                }

                // Update render.
                NetData.UpdateRender(_netInfo, _segmentID);
            }
        }

        /// <summary>
        /// Applies the specified replacement as a temporary preview.
        /// </summary>
        /// <param name="previewReplacement">Replacement to preview.</param>
        public override void PreviewReplacement(BOBConfig.Replacement previewReplacement) => ReplaceProp(previewReplacement);

        /// <summary>
        /// Clears any temporary preview, restoring the permanent underlying state.
        /// </summary>
        public override void ClearPreview()
        {
            // Previews may be for now removed added props - perform index check first.
            if (_originalLaneInfo?.m_laneProps?.m_props != null && PropIndex < _originalLaneInfo.m_laneProps.m_props.Length)
            {
                UpdateProp();
            }
        }

        /// <summary>
        /// Updates the target prop according current active replacement status.
        /// </summary>
        private void UpdateProp()
        {
            // Don't do anything further if this is a skin.
            if (_segmentID != 0)
            {
                return;
            }

            // Is there an active replacement for this reference?
            if (ActiveReplacement is BOBConfig.NetReplacement activeReplacement)
            {
                // Active replacement - replace prop accordingly.
                ReplaceProp(activeReplacement);
            }
            else
            {
                // No active replacement - restore original values.
                RevertToOriginal();
            }
        }

        /// <summary>
        /// Replaces the target prop using the specified building replacement.
        /// </summary>
        /// <param name="replacement">Network replacement element to apply.</param>
        private void ReplaceProp(BOBConfig.Replacement replacement)
        {
            if (replacement is BOBConfig.NetReplacement netReplacement)
            {
                NetLaneProps.Prop[] targetProps = _originalLaneInfo.m_laneProps.m_props;

                // Check for skin, and create custom lane reference if needed.
                if (_segmentID != 0)
                {
                    NetworkSkins.SegmentSkins[_segmentID].AddChange(_laneIndex, PropIndex);
                    targetProps = NetworkSkins.SegmentSkins[_segmentID].Lanes[_laneIndex].m_laneProps.m_props;
                }

                // Convert offset to Vector3.
                Vector3 offset = new Vector3
                {
                    x = netReplacement.OffsetX,
                    y = netReplacement.OffsetY,
                    z = netReplacement.OffsetZ,
                };

                // Apply replacement.
                targetProps[PropIndex].m_prop = netReplacement.ReplacementProp;
                targetProps[PropIndex].m_finalProp = netReplacement.ReplacementProp;
                targetProps[PropIndex].m_tree = netReplacement.ReplacementTree;
                targetProps[PropIndex].m_finalTree = netReplacement.ReplacementTree;

                // Invert x offset and angle to match original prop x position.
                float angleMult = 1f;
                if (_originalLaneInfo.m_position + OriginalPosition.x < 0)
                {
                    offset.x = 0 - offset.x;
                    angleMult = -1;
                }

                // Angle and offset.
                targetProps[PropIndex].m_angle = OriginalAngle + (netReplacement.Angle * angleMult);
                targetProps[PropIndex].m_position = OriginalPosition + offset;

                // Probability.
                targetProps[PropIndex].m_probability = netReplacement.Probability;

                // Repeat distance, if a valid value is set.
                if (netReplacement.RepeatDistance > 1)
                {
                    targetProps[PropIndex].m_repeatDistance = netReplacement.RepeatDistance;
                }

                // Update render.
                NetData.UpdateRender(_netInfo, _segmentID);
            }
        }
    }
}