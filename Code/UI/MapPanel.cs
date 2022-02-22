using System;
using System.Linq;
using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.UI;
using EManagersLib.API;


namespace BOB
{
	/// <summary>
	/// BOB map tree/prop replacement panel.
	/// </summary>
	internal class BOBMapInfoPanel : BOBInfoPanelBase
	{
		/// <summary>
		/// Initial prop-tree mode.
		/// </summary>
		protected override PropTreeModes InitialPropTreeMode
        {
			get
            {
				// Set initial mode based on initial selection.
				if (selectedPrefab is TreeInfo)
                {
					return PropTreeModes.Tree;
                }

				return PropTreeModes.Prop;
            }
        }


		/// <summary>
		/// Sets the target prefab.
		/// </summary>
		/// <param name="targetPrefabInfo">Target prefab to set</param>
		internal override void SetTarget(PrefabInfo targetPrefabInfo)
		{
			// Base setup.
			base.SetTarget(targetPrefabInfo);

			// Title label.
			SetTitle(Translations.Translate("BOB_NAM"));

			// Set intial prop/tree mode, deselecting previous selection and disabling events throughout.
			ignorePropTreeCheckChanged = true;
			propTreeChecks[(int)PropTreeMode].isChecked = false;
			PropTreeMode = targetPrefabInfo is TreeInfo ? PropTreeModes.Tree : PropTreeModes.Prop;
			propTreeChecks[(int)PropTreeMode].isChecked = true;
			ignorePropTreeCheckChanged = false;

			// Populate target list and select target item.
			TargetList();
			targetList.FindTargetItem(targetPrefabInfo);

			// Apply Harmony rendering patches.
			Patcher.PatchMapOverlays(true);
		}


		/// <summary>
		/// Constructor.
		/// </summary>
		internal BOBMapInfoPanel()
        {
			try
			{
				// Disable 'both' check.
				propTreeChecks[(int)PropTreeModes.Both].Disable();
				propTreeChecks[(int)PropTreeModes.Both].Hide();

				// Populate loaded list.
				LoadedList();

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
		/// Apply button event handler.
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
		/// </summary>
		protected override void Apply(UIComponent control, UIMouseEventParameter mouseEvent)
		{
			try
			{
				// Apply replacement.
				if (ReplacementPrefab is TreeInfo replacementTree)
				{
					MapTreeReplacement.instance.Apply((CurrentTargetItem.replacementPrefab ?? CurrentTargetItem.originalPrefab) as TreeInfo, replacementTree);
				}
				else if (ReplacementPrefab is PropInfo replacementProp)
				{
					MapPropReplacement.instance.Apply((CurrentTargetItem.replacementPrefab ?? CurrentTargetItem.originalPrefab) as PropInfo, replacementProp);
				}

				// Update current target.
				CurrentTargetItem.replacementPrefab = ReplacementPrefab;

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
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
		/// </summary>
		protected override void Revert(UIComponent control, UIMouseEventParameter mouseEvent)
		{
			try
			{
				// Individual building prop reversion - ensuire that we've got a current selection before doing anything.
				if (CurrentTargetItem != null && CurrentTargetItem is TargetListItem)
				{
					// Individual reversion.
					if (CurrentTargetItem.replacementPrefab is TreeInfo tree)
					{
						MapTreeReplacement.instance.Revert(tree);
					}
					else if (CurrentTargetItem.replacementPrefab is PropInfo prop)
					{
						MapPropReplacement.instance.Revert(prop);
					}

					// Clear current target replacement prefab.
					CurrentTargetItem.replacementPrefab = null;
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
			applyButton.Disable();
			revertButton.Disable();

			// Buttons are only enabled if a current target item is selected.
			if (CurrentTargetItem != null)
			{
				// Replacement requires a valid replacement selection.
				if (ReplacementPrefab != null)
				{
					applyButton.Enable();
				}

				// Reversion requires a currently active replacement.
				if (CurrentTargetItem.replacementPrefab != null)
				{
					revertButton.Enable();
					revertButton.tooltip = Translations.Translate("BOB_PNL_REV_UND");
				}
				else
				{
					revertButton.tooltip = Translations.Translate("BOB_PNL_REV_TIP");
				}
			}
		}


		/// <summary>
		/// Populates the target list with a list of map trees or props.
		/// </summary>
		protected override void TargetList()
		{
			Logging.Message("starting TargetList");

			// List of prefabs that have passed filtering.
			List<TargetListItem> itemList = new List<TargetListItem>();

			// Local references.
			TreeInstance[] trees = Singleton<TreeManager>.instance.m_trees.m_buffer;

			// Iterate through each prop or tree instance on map.
			for (uint i = 0; i < (PropTreeMode == PropTreeModes.Tree ? trees.Length : PropAPI.PropBufferLen); ++i)
			{
				// Create new list item, hiding probabilities.
				TargetListItem propListItem = new TargetListItem { showProbs = false };

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
					propListItem.originalPrefab = MapTreeReplacement.instance.GetOriginal(tree.Info);

					// Did we find a current replacment?
					if (propListItem.originalPrefab == null)
					{
						// No - set current item as the original tree.
						propListItem.originalPrefab = tree.Info;
					}
					else
					{
						// Yes - record current item as replacement.
						propListItem.replacementPrefab = tree.Info;
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
					propListItem.originalPrefab = MapPropReplacement.instance.GetOriginal(prop);

					// Did we find a current replacment?
					if (propListItem.originalPrefab == null)
					{
						// No - set current item as the original prop.
						propListItem.originalPrefab = prop;
					}
					else
					{
						// Yes - record current item as replacement.
						propListItem.replacementPrefab = prop;
					}
				}

				// Check to see if we were succesful - if not (e.g. we only want trees and this is a prop), continue on to next instance.
				if (propListItem.originalPrefab?.name == null)
				{
					continue;
				}

				// Map instances are always grouped, and we don't have lists of indexes - too many trees!
				propListItem.index = -1;

				// Are we grouping?
				if (propListItem.index == -1)
				{
					// Yes, grouping - initialise a flag to show if we've matched.
					bool matched = false;

					// Iterate through each item in our existing list of props.
					foreach (TargetListItem item in itemList)
					{
						// Check to see if we already have this in the list - matching original prefab.
						if (item.originalPrefab == propListItem.originalPrefab)
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
				}

				// Add this item to our list.
				itemList.Add(propListItem);
			}

			// Create return fastlist from our filtered list, ordering by name.
			targetList.rowsData = new FastList<object>
			{
				m_buffer = targetSearchStatus == (int)OrderBy.NameDescending ? itemList.OrderByDescending(item => item.DisplayName).ToArray() : itemList.OrderBy(item => item.DisplayName).ToArray(),
				m_size = itemList.Count
			};

			// If the list is empty, show the 'no props' label; otherwise, hide it.
			if (itemList.Count == 0)
			{
				noPropsLabel.Show();
			}
			else
			{
				noPropsLabel.Hide();
			}
		}
	}
}