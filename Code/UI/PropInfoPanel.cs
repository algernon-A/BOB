using UnityEngine;
using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.UI;


namespace BOB
{
	/// <summary>
	/// BOB map tree replacement panel.
	/// </summary>
	public class BOBPropInfoPanel : BOBInfoPanelBase
	{
		// Button labels.
		protected override string ReplaceTooltipKey => "BOB_PNL_RTT";


		// Never, never, trees.
		protected override bool IsTree => false;


		// Replace button atlas.
		protected override UITextureAtlas ReplaceAtlas => TextureUtils.LoadSpriteAtlas("bob_props3");


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
				MapPropReplacement.instance.Apply((CurrentTargetItem.replacementPrefab ?? CurrentTargetItem.originalPrefab) as PropInfo, replacementPrefab as PropInfo);

				// Update current target.
				CurrentTargetItem.replacementPrefab = replacementPrefab;

				// Refresh target list (to reflect our changes).
				targetList.Refresh();

				// Update button states.
				UpdateButtonStates();
			};

			// Revert button event handler.
			revertButton.eventClicked += (control, clickEvent) =>
			{
				// Individual building prop reversion - ensuire that we've got a current selection before doing anything.
				if (CurrentTargetItem != null && CurrentTargetItem is PropListItem currentItem)
				{
					// Individual reversion.
					MapPropReplacement.instance.Revert(CurrentTargetItem.replacementPrefab as PropInfo);

					// Clear current target replacement prefab.
					CurrentTargetItem.replacementPrefab = null;
				}

				// Refresh target list (to reflect our changes).
				targetList.Refresh();

				// Update button states.
				UpdateButtonStates();
			};

			// Set loaded lists to 'props'.
			loadedList.rowsData = LoadedList(isTree: false);
			targetList.rowsData = TargetList(isTree: false);

			// Select target item.
			targetList.FindTargetItem(targetPrefabInfo);

			// Update button states.
			UpdateButtonStates();

			// Apply Harmony rendering patches.
			//Patcher.PatchTreeOverlays(true);
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
		/// Populates a fastlist with a list of map trees.
		/// </summary>
		/// <param name="isTree">True for a list of trees, false for props</param>
		/// <returns>Populated fastlist of loaded prefabs</returns>
		protected override FastList<object> TargetList(bool isTree)
		{
			// List of prefabs that have passed filtering.
			List<PropListItem> propList = new List<PropListItem>();

			// Local references.
			PropManager propManager = Singleton<PropManager>.instance;
			PropInstance[] props = propManager.m_props.m_buffer;

			// Iterate through each map prop instance.
			for (uint propIndex = 0; propIndex < props.Length; ++propIndex)
			{
				// Local reference.
				PropInstance prop = props[propIndex];

				// Skip non-existent props (those with no flags).
				if (prop.m_flags == (ushort)PropInstance.Flags.None)
				{
					continue;
				}

				// Create new list item, hiding probabilities.
				PropListItem propListItem = new PropListItem { showProbs = false };

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
					foreach (PropListItem item in propList)
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
				propList.Add(propListItem);
			}

			// Create return fastlist from our filtered list, ordering by name.
			FastList<object> fastList = new FastList<object>
			{
				m_buffer = propList.ToArray(),
				m_size = propList.Count
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