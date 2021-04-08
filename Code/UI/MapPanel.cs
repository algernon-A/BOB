using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.UI;


namespace BOB
{
	/// <summary>
	/// BOB map tree/prop replacement panel.
	/// </summary>
	public class BOBMapInfoPanel : BOBInfoPanelBase
	{
		// Button labels.
		protected override string ReplaceTooltipKey => "BOB_PNL_RTT";

		// Trees or props?
		protected override bool IsTree => treeCheck?.isChecked ?? false;

		// Initial tree/prop checked state.
		protected override bool InitialTreeCheckedState => selectedPrefab is TreeInfo;


		// Replace button atlas.
		protected override UITextureAtlas ReplaceAtlas => TextureUtils.LoadSpriteAtlas("bob_trees");


		/// <summary>
		/// Performs initial setup 
		/// </summary>
		/// <param name="parentTransform">Parent transform</param>
		/// <param name="targetPrefabInfo">Currently selected target prefab</param>
		internal override void Setup(Transform parentTransform, PrefabInfo targetPrefabInfo)
		{
			// Base setup.
			base.Setup(parentTransform, targetPrefabInfo);

			// Populate target list and select target item.
			TargetList();
			targetList.FindTargetItem(targetPrefabInfo);

			// Update button states.
			UpdateButtonStates();

			// Apply Harmony rendering patches.
			Patcher.PatchMapOverlays(true);
		}


		/// <summary>
		/// Replace button event handler.
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
		/// </summary>
		protected override void Replace(UIComponent control, UIMouseEventParameter mouseEvent)
		{
			// Apply replacement.
			if (replacementPrefab is TreeInfo replacementTree)
			{
				MapTreeReplacement.instance.Apply((CurrentTargetItem.replacementPrefab ?? CurrentTargetItem.originalPrefab) as TreeInfo, replacementTree);
			}
			else if (replacementPrefab is PropInfo replacementProp)
			{
				MapPropReplacement.instance.Apply((CurrentTargetItem.replacementPrefab ?? CurrentTargetItem.originalPrefab) as PropInfo, replacementProp);
			}

			// Update current target.
			CurrentTargetItem.replacementPrefab = replacementPrefab;

			// Perform post-replacment updates.
			FinishUpdate();
		}


		/// <summary>
		/// Revert button event handler.
		/// <param name="control">Calling component (unused)</param>
		/// <param name="mouseEvent">Mouse event (unused)</param>
		/// </summary>
		protected override void Revert(UIComponent control, UIMouseEventParameter mouseEvent)
		{
			// Individual building prop reversion - ensuire that we've got a current selection before doing anything.
			if (CurrentTargetItem != null && CurrentTargetItem is PropListItem)
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


		/// <summary>
		/// Prop check event handler.
		/// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="isChecked">New checked state</param>
		protected override void PropCheckChanged(UIComponent control, bool isChecked)
		{
			base.PropCheckChanged(control, isChecked);

			// Toggle replace button atlas.
			replaceButton.atlas = isChecked ? TextureUtils.LoadSpriteAtlas("bob_props3_large") : TextureUtils.LoadSpriteAtlas("bob_trees");
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
			if (CurrentTargetItem != null)
			{
				// Replacement requires a valid replacement selection.
				if (replacementPrefab != null)
				{
					replaceButton.Enable();
				}

				// Reversion requires a currently active replacement.
				if (CurrentTargetItem.replacementPrefab != null)
				{
					revertButton.Enable();
				}
			}
		}


		/// <summary>
		/// Populates the target list with a list of map trees or props.
		/// </summary>
		protected override void TargetList()
		{
			// List of prefabs that have passed filtering.
			List<PropListItem> itemList = new List<PropListItem>();

			// Local references.
			TreeManager treeManager = Singleton<TreeManager>.instance;
			TreeInstance[] trees = treeManager.m_trees.m_buffer;
			PropManager propManager = Singleton<PropManager>.instance;
			PropInstance[] props = propManager.m_props.m_buffer;

			// Iterate through each tree instance map.
			for (int index = 0; index < (IsTree ? trees.Length : props.Length); ++index)
			{
				// Create new list item, hiding probabilities.
				PropListItem propListItem = new PropListItem { showProbs = false };

				if (IsTree)
				{
					// Local reference.
					TreeInstance tree = trees[index];

					// Skip non-existent trees (those with no flags).
					if (tree.m_flags == (ushort)TreeInstance.Flags.None)
					{
						continue;
					}

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
				}
				else
                {
					// Props.
					PropInstance prop = props[index];

					// Skip non-existent props (those with no flags).
					if (prop.m_flags == (ushort)PropInstance.Flags.None)
					{
						continue;
					}

					// Try to get any prop replacement.
					propListItem.originalPrefab = MapPropReplacement.instance.GetOriginal(prop.Info);

					// DId we find a current replacment?
					if (propListItem.originalPrefab == null)
					{
						// No - set current item as the original prop.
						propListItem.originalPrefab = prop.Info;
					}
					else
					{
						// Yes - record current item as replacement.
						propListItem.replacementPrefab = prop.Info;
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
					foreach (PropListItem item in itemList)
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
			targetList.m_rowsData = new FastList<object>
			{
				m_buffer = targetSearchStatus == (int)OrderBy.NameDescending ? itemList.OrderByDescending(item => item.DisplayName).ToArray() : itemList.OrderBy(item => item.DisplayName).ToArray(),
				m_size = itemList.Count
			};
			targetList.Refresh();

			// If the list is empty, show the 'no props' label; otherwise, hide it.
			if (targetList.m_rowsData.m_size == 0)
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