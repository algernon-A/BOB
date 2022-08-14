﻿// <copyright file="BOBMapPanel.cs" company="algernon (K. Algernon A. Sheppard)">
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
    using ColossalFramework;
    using ColossalFramework.UI;
    using EManagersLib.API;

    /// <summary>
    /// BOB map tree/prop replacement panel.
    /// </summary>
    internal sealed class BOBMapPanel : BOBInfoPanelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BOBMapPanel"/> class.
        /// </summary>
        internal BOBMapPanel()
        {
            try
            {
                // Disable 'both' check.
                m_propTreeChecks[(int)PropTreeModes.Both].Disable();
                m_propTreeChecks[(int)PropTreeModes.Both].Hide();

                // Set initial button states.
                UpdateButtonStates();
            }
            catch (Exception e)
            {
                // Log and report any exception.
                Logging.LogException(e, "exception creating map panel");
            }
        }

        /// <summary>
        /// Sets the target parent prefab.
        /// </summary>
        /// <param name="targetPrefabInfo">Target prefab to set.</param>
        internal override void SetTargetParent(PrefabInfo targetPrefabInfo)
        {
            // Base setup.
            base.SetTargetParent(targetPrefabInfo);

            // Title label.
            SetTitle(Translations.Translate("BOB_NAM"));

            // Set intial prop/tree mode, deselecting previous selection and disabling events throughout.
            m_ignorePropTreeCheckChanged = true;
            m_propTreeChecks[(int)PropTreeMode].isChecked = false;
            PropTreeMode = targetPrefabInfo is TreeInfo ? PropTreeModes.Tree : PropTreeModes.Prop;
            m_propTreeChecks[(int)PropTreeMode].isChecked = true;
            m_ignorePropTreeCheckChanged = false;

            // Regenerate target list and select target item.
            RegenerateTargetList();
            m_targetList.FindItem<LoadedPrefabItem>(x => x.Prefab == targetPrefabInfo);

            // Regenerate replacement list.
            RegenerateReplacementList();

            // Apply Harmony rendering patches.
            Patcher.Instance.PatchMapOverlays(true);
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
                // Apply replacement.
                if (!m_propTreeChecks[(int)PropTreeModes.Prop].isChecked && SelectedReplacementPrefab is TreeInfo replacementTree)
                {
                    MapTreeReplacement.Instance.Apply(SelectedTargetItem.ActivePrefab as TreeInfo, replacementTree);
                }
                else if (!m_propTreeChecks[(int)PropTreeModes.Tree].isChecked && SelectedReplacementPrefab is PropInfo replacementProp)
                {
                    MapPropReplacement.Instance.Apply(SelectedTargetItem.ActivePrefab as PropInfo, replacementProp);
                }

                // Update current target.
                SelectedTargetItem.ReplacementPrefab = SelectedReplacementPrefab;

                // Perform post-replacment updates.
                FinishUpdate();
            }
            catch (Exception e)
            {
                // Log and report any exception.
                Logging.LogException(e, "exception perforiming map replacement");
            }
        }

        /// <summary>
        /// Revert button event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="p">Mouse event parameter.</param>
        protected override void Revert(UIComponent c, UIMouseEventParameter p)
        {
            try
            {
                // Individual building prop reversion - ensuire that we've got a current selection before doing anything.
                if (SelectedTargetItem != null && SelectedTargetItem is TargetListItem)
                {
                    // Individual reversion.
                    if (SelectedTargetItem.ReplacementPrefab is TreeInfo tree)
                    {
                        MapTreeReplacement.Instance.Revert(tree);
                    }
                    else if (SelectedTargetItem.ReplacementPrefab is PropInfo prop)
                    {
                        MapPropReplacement.Instance.Revert(prop);
                    }

                    // Clear current target replacement prefab.
                    SelectedTargetItem.ReplacementPrefab = null;
                }

                // Perform post-replacment updates.
                FinishUpdate();
            }
            catch (Exception e)
            {
                // Log and report any exception.
                Logging.LogException(e, "exception perforiming map reversion");
            }
        }

        /// <summary>
        /// Updates button states (enabled/disabled) according to current control states.
        /// </summary>
        protected override void UpdateButtonStates()
        {
            // Disable by default (selectively (re)-enable if eligible).
            m_applyButton.Disable();
            m_revertButton.Disable();

            // Buttons are only enabled if a current target item is selected.
            if (SelectedTargetItem != null)
            {
                // Replacement requires a valid replacement selection.
                if (SelectedReplacementPrefab != null)
                {
                    m_applyButton.Enable();
                }

                // Reversion requires a currently active replacement.
                if (SelectedTargetItem.ReplacementPrefab != null)
                {
                    m_revertButton.Enable();
                    m_revertButton.tooltip = Translations.Translate("BOB_PNL_REV_UND");
                }
                else
                {
                    m_revertButton.tooltip = Translations.Translate("BOB_PNL_REV_TIP");
                }
            }
        }

        /// <summary>
        /// Regenerates the target fastlist with a list of target-specific trees or props.
        /// </summary>
        protected override void RegenerateTargetList()
        {
            // List of prefabs that have passed filtering.
            List<TargetListItem> itemList = new List<TargetListItem>();

            // Local references.
            TreeInstance[] trees = Singleton<TreeManager>.instance.m_trees.m_buffer;

            // Iterate through each prop or tree instance on map.
            for (uint i = 0; i < (PropTreeMode == PropTreeModes.Tree ? trees.Length : PropAPI.PropBufferLen); ++i)
            {
                // Create new list item, hiding probabilities.
                TargetListItem propListItem = new TargetListItem { ShowProbs = false };

                if (PropTreeMode == PropTreeModes.Tree)
                {
                    // Local reference.
                    TreeInstance tree = trees[i];

                    // Skip non-existent trees (those with no flags).
                    if (tree.m_flags == (ushort)TreeInstance.Flags.None)
                    {
                        continue;
                    }

                    // Try to get any tree replacement.
                    propListItem.OriginalPrefab = MapTreeReplacement.Instance.GetOriginal(tree.Info);

                    // Did we find a current replacment?
                    if (propListItem.OriginalPrefab == null)
                    {
                        // No - set current item as the original tree.
                        propListItem.OriginalPrefab = tree.Info;
                    }
                    else
                    {
                        // Yes - record current item as replacement.
                        propListItem.ReplacementPrefab = tree.Info;
                    }
                }
                else
                {
                    // Props - skip non-existent props (those with no flags).
                    if (PropAPI.Wrapper.GetFlags(i) == (ushort)PropInstance.Flags.None)
                    {
                        continue;
                    }

                    // Get prop info.
                    PropInfo prop = PropAPI.Wrapper.GetInfo(i);
                    if (prop == null)
                    {
                        continue;
                    }

                    // Try to get any prop replacement.
                    propListItem.OriginalPrefab = MapPropReplacement.Instance.GetOriginal(prop);

                    // Did we find a current replacment?
                    if (propListItem.OriginalPrefab == null)
                    {
                        // No - set current item as the original prop.
                        propListItem.OriginalPrefab = prop;
                    }
                    else
                    {
                        // Yes - record current item as replacement.
                        propListItem.ReplacementPrefab = prop;
                    }
                }

                // Check to see if we were succesful - if not (e.g. we only want trees and this is a prop), continue on to next instance.
                if (propListItem.OriginalPrefab?.name == null)
                {
                    continue;
                }

                // Map instances are always grouped, and we don't have lists of indexes - too many trees!
                propListItem.PropIndex = -1;

                // Initialise a flag to show if we've matched.
                bool matched = false;

                // Iterate through each item in our existing list of props.
                foreach (TargetListItem item in itemList)
                {
                    // Check to see if we already have this in the list - matching original prefab.
                    if (item.OriginalPrefab == propListItem.OriginalPrefab)
                    {
                        // We've already got an identical grouped instance of this item - set the flag to indicate that we've match it.
                        matched = true;

                        // No point going any further through the list, since we've already found our match.
                        break;
                    }
                }

                // Did we get a match?
                if (matched)
                {
                    // Yes - continue on to next tree (without adding this item separately to the list).
                    continue;
                }

                // Add this item to our list.
                itemList.Add(propListItem);
            }

            // Create return fastlist from our filtered list, ordering by name.
            m_targetList.Data = new FastList<object>
            {
                m_buffer = m_targetSortSetting == (int)OrderBy.NameDescending ? itemList.OrderByDescending(item => item.DisplayName).ToArray() : itemList.OrderBy(item => item.DisplayName).ToArray(),
                m_size = itemList.Count,
            };

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
    }
}