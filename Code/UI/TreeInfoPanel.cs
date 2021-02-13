using UnityEngine;
using System.Collections.Generic;
using ColossalFramework;


namespace BOB
{
	/// <summary>
	/// BOB map tree replacement panel.
	/// </summary>
	public class BOBTreeInfoPanel : BOBInfoPanelBase
	{
		// Button labels.
		protected override string ReplaceLabel => Translations.Translate("BOB_PNL_RTT");


		// Always, always, trees.
		protected override bool IsTree => true;


		/// <summary>
		/// Performs initial setup 
		/// </summary>
		/// <param name="parentTransform">Parent transform</param>
		/// <param name="targetPrefabInfo">Currently selected target prefab</param>
		internal override void Setup(Transform parentTransform, PrefabInfo targetPrefabInfo)
		{
			// Base setup.
			base.Setup(parentTransform, targetPrefabInfo);

			// Replace button event handler.
			replaceButton.eventClicked += (control, clickEvent) =>
			{
				// Apply replacement.
				MapTreeReplacement.instance.Apply((currentTargetItem.replacementPrefab ?? currentTargetItem.originalPrefab) as TreeInfo, replacementPrefab as TreeInfo);

				// Update current target.
				currentTargetItem.replacementPrefab = replacementPrefab;

				// Refresh target list (to reflect our changes).
				targetList.Refresh();

				// Update button states.
				UpdateButtonStates();
			};

			// Revert button event handler.
			revertButton.eventClicked += (control, clickEvent) =>
			{
				// Individual building prop reversion - ensuire that we've got a current selection before doing anything.
				if (currentTargetItem != null && currentTargetItem is PropListItem currentItem)
				{
					// Individual reversion.
					MapTreeReplacement.instance.Revert(currentTargetItem.replacementPrefab as TreeInfo);

					// Clear current target replacement prefab.
					currentTargetItem.replacementPrefab = null;
				}

				// Refresh target list (to reflect our changes).
				targetList.Refresh();

				// Update button states.
				UpdateButtonStates();
			};

			// Set loaded lists to 'trees'.
			loadedList.rowsData = LoadedList(isTree: true);
			targetList.rowsData = TargetList(isTree: true);

			// Select target item.
			targetList.FindTargetItem(targetPrefabInfo);

			// Update button states.
			UpdateButtonStates();
		}


		/// <summary>
		/// Updates button states (enabled/disabled) according to current control states.
		/// </summary>
		protected override void UpdateButtonStates()
		{
			// Disable by default (selectively (re)-enable if eligible).
			replaceButton.Disable();
			revertButton.Disable();

			// Buttons are only enabled if a current target item is selected.
			if (currentTargetItem != null)
			{
				// Replacement requires a valid replacement selection.
				if (replacementPrefab != null)
				{
					replaceButton.Enable();
				}

				// Reversion requires a currently active replacement.
				if (currentTargetItem.replacementPrefab != null)
				{
					revertButton.Enable();
				}
			}
		}


		/// <summary>
		/// Populates a fastlist with a list of map trees.
		/// </summary>
		/// <param name="isTree">True for a list of trees, false for props</param>
		/// <returns>Populated fastlist of loaded prefabs</returns>
		protected override FastList<object> TargetList(bool isTree)
		{
			// List of prefabs that have passed filtering.
			List<PropListItem> treeList = new List<PropListItem>();

			// Iterate through each tree instance map.
			for (int treeIndex = 0; treeIndex < Singleton<TreeManager>.instance.m_trees.m_buffer.Length; ++treeIndex)
			{
				// Local reference.
				TreeInstance tree = Singleton<TreeManager>.instance.m_trees.m_buffer[treeIndex];

				// Skip non-existent trees (those with no flags).
				if (tree.m_flags == (ushort)TreeInstance.Flags.None)
				{
					continue;
				}

				// Create new list item, hiding probabilities.
				PropListItem propListItem = new PropListItem { showProbs = false };

				// Try to get any tree replacement.
				propListItem.originalPrefab = MapTreeReplacement.instance.GetOriginal(tree.Info);

				// DId we find a current replacment?
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

				// Check to see if we were succesful - if not (e.g. we only want trees and this is a prop), continue on to next building prop.
				if (propListItem.originalPrefab?.name == null)
				{
					continue;
				}

				// Trees are always grouped, and we don't have lists of indexes - too many!
				propListItem.index = -1;

				// Are we grouping?
				if (propListItem.index == -1)
				{
					// Yes, grouping - initialise a flag to show if we've matched.
					bool matched = false;

					// Iterate through each item in our existing list of props.
					foreach (PropListItem item in treeList)
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
				treeList.Add(propListItem);
			}

			// Create return fastlist from our filtered list, ordering by name.
			FastList<object> fastList = new FastList<object>
			{
				m_buffer = treeList.ToArray(),
				m_size = treeList.Count
			};

			// If the list is empty, show the 'no props' label; otherwise, hide it.
			if (fastList.m_size == 0)
			{
				noPropsLabel.Show();
			}
			else
			{
				noPropsLabel.Hide();
			}

			return fastList;
		}
	}
}