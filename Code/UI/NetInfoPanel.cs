using System.Collections.Generic;
using UnityEngine;


namespace BOB
{
	/// <summary>
	/// BOB network tree/prop replacement panel.
	/// </summary>
	public class BOBNetInfoPanel : BOBInfoPanel
	{
		// Current selection reference.
		NetInfo currentNet;


		// Button labels.
		protected override string ReplaceLabel => Translations.Translate("BOB_PNL_RTN");

		protected override string ReplaceAllLabel => Translations.Translate("BOB_PNL_RAN");


		/// <summary>
		/// Handles changes to the currently selected target prefab.
		/// </summary>
		internal override PropListItem CurrentTargetItem
        {
            set
            {
				// Call base.
				base.CurrentTargetItem = value;

				if (value != null)
				{
					// If we've got a replacement, update the offset fields with the replacement vlues
					if (CurrentTargetItem.replacementPrefab != null)
					{
						angleField.text = NetworkReplacement.instance.replacements[currentNet][CurrentTargetItem.originalPrefab].angle.ToString();
						xField.text = NetworkReplacement.instance.replacements[currentNet][CurrentTargetItem.originalPrefab].offsetX.ToString();
						yField.text = NetworkReplacement.instance.replacements[currentNet][CurrentTargetItem.originalPrefab].offsetY.ToString();
						zField.text = NetworkReplacement.instance.replacements[currentNet][CurrentTargetItem.originalPrefab].offsetZ.ToString();
						probabilityField.text = NetworkReplacement.instance.replacements[currentNet][CurrentTargetItem.originalPrefab].probability.ToString();
					}
					// Ditto for any all-network replacement.
					else if (CurrentTargetItem.allPrefab != null)
					{
						angleField.text = AllNetworkReplacement.instance.replacements[CurrentTargetItem.originalPrefab].angle.ToString();
						xField.text = AllNetworkReplacement.instance.replacements[CurrentTargetItem.originalPrefab].offsetX.ToString();
						yField.text = AllNetworkReplacement.instance.replacements[CurrentTargetItem.originalPrefab].offsetY.ToString();
						zField.text = AllNetworkReplacement.instance.replacements[CurrentTargetItem.originalPrefab].offsetZ.ToString();
						probabilityField.text = AllNetworkReplacement.instance.replacements[CurrentTargetItem.originalPrefab].probability.ToString();
					}
					else
					{
						// No current replacement; set all offset fields to original prop.
						angleField.text = value.originalAngle.ToString();
						xField.text = "0";
						yField.text = "0";
						zField.text = "0";
						probabilityField.text = value.originalProb.ToString();
					}
				}
            }
		}


		/// <summary>
		/// Performs initial setup 
		/// </summary>
		/// <param name="parentTransform">Parent transform</param>
		/// <param name="targetPrefabInfo">Currently selected target prefab</param>
		internal override void Setup(Transform parentTransform, PrefabInfo targetPrefabInfo)
		{
			// Set target reference.
			currentNet = targetPrefabInfo as NetInfo;

			// Base setup.
			base.Setup(parentTransform, targetPrefabInfo);

			// Add pack button.
			ColossalFramework.UI.UIButton packButton = UIControls.AddButton(this, 250f, 50f, Translations.Translate("BOB_PNL_PKB"));
			packButton.eventClicked += (component, clickEvent) => PackPanelManager.Create();

			// Event handler for prop checkbox.
			propCheck.eventCheckChanged += (control, isChecked) =>
			{
				if (isChecked)
				{
					// Props are now selected - unset tree check.
					treeCheck.isChecked = false;

					// Reset current items.
					CurrentTargetItem = null;
					replacementPrefab = null;

					// Set loaded lists to 'props'.
					loadedList.rowsData = LoadedList(isTree: false);
					targetList.rowsData = TargetList(isTree: false);

					// Set 'no props' label text.
					noPropsLabel.text = Translations.Translate("BOB_PNL_NOP");
				}
				else
				{
					// Props are now unselected - set tree check if it isn't already (letting tree check event handler do the work required).
					if (!treeCheck.isChecked)
					{
						treeCheck.isChecked = true;
					}
				}

				// Save state.
				ModSettings.treeSelected = !isChecked;
			};

			// Event handler for tree checkbox.
			treeCheck.eventCheckChanged += (control, isChecked) =>
			{
				if (isChecked)
				{
					// Trees are now selected - unset prop check.
					propCheck.isChecked = false;

					// Reset current items.
					CurrentTargetItem = null;
					replacementPrefab = null;

					// Set loaded lists to 'trees'.
					loadedList.rowsData = LoadedList(isTree: true);
					targetList.rowsData = TargetList(isTree: true);

					// Set 'no props' label text.
					noPropsLabel.text = Translations.Translate("BOB_PNL_NOT");
				}
				else
				{
					// Trees are now unselected - set prop check if it isn't already (letting prop check event handler do the work required).
					if (!propCheck.isChecked)
					{
						propCheck.isChecked = true;
					}
				}

				// Save state.
				ModSettings.treeSelected = isChecked;
			};

			// Replace button event handler.
			replaceButton.eventClicked += (control, clickEvent) =>
			{
				// Make sure we have valid a target and replacement.
				if (CurrentTargetItem != null && replacementPrefab != null)
				{
					// Try to parse textfields.
					float.TryParse(angleField.text, out float angle);
					float.TryParse(xField.text, out float xOffset);
					float.TryParse(yField.text, out float yOffset);
					float.TryParse(zField.text, out float zOffset);
					int.TryParse(probabilityField.text, out int probability);

					// Update text fields to match parsed values.
					angleField.text = angle.ToString();
					xField.text = xOffset.ToString();
					yField.text = yOffset.ToString();
					zField.text = zOffset.ToString();
					probabilityField.text = probability.ToString();

					// Network replacements are always grouped.
					NetworkReplacement.instance.Apply(currentNet, CurrentTargetItem.originalPrefab ?? CurrentTargetItem.replacementPrefab, replacementPrefab, angle, xOffset, yOffset, zOffset, probability);

					// Perform post-replacment updates.
					FinishUpdate();
				}
			};

			// All network button event handler.
			replaceAllButton.eventClicked += (control, clickEvent) =>
			{
				// Try to parse text fields.
				float.TryParse(angleField.text, out float angle);
				float.TryParse(xField.text, out float xOffset);
				float.TryParse(yField.text, out float yOffset);
				float.TryParse(zField.text, out float zOffset);
				int.TryParse(probabilityField.text, out int probability);

				// Update text fields to match parsed values.
				angleField.text = angle.ToString();
				xField.text = xOffset.ToString();
				yField.text = yOffset.ToString();
				zField.text = zOffset.ToString();
				probabilityField.text = probability.ToString();

				// Apply replacement.
				AllNetworkReplacement.instance.Apply(CurrentTargetItem.originalPrefab ?? CurrentTargetItem.replacementPrefab, replacementPrefab, angle, xOffset, yOffset, zOffset, probability);

				// Perform post-replacment updates.
				FinishUpdate();
			};

			// Revert button event handler.
			revertButton.eventClicked += (control, clickEvent) =>
			{
				// Network or all-network reversion?
				if (CurrentTargetItem.replacementPrefab != null)
				{
					// Individual network reversion - ensuire that we've got a current selection before doing anything.
					if (CurrentTargetItem != null && CurrentTargetItem is NetPropListItem currentNetItem)
					{
						// Network replacements are always grouped.
						NetworkReplacement.instance.Revert(currentNet, CurrentTargetItem.originalPrefab, true);

						// Perform post-reversion updates.
						FinishUpdate();
					}
				}
				else if (CurrentTargetItem.allPrefab != null)
				{
					// All-network reversion - make sure we've got a currently active replacement before doing anything.
					if (CurrentTargetItem.originalPrefab)
					{
						// Apply all-network reversion.
						AllNetworkReplacement.instance.Revert(CurrentTargetItem.originalPrefab, true);

						// Save configuration file and refresh target list (to reflect our changes).
						ConfigurationUtils.SaveConfig();

						// Perform post-reversion updates.
						FinishUpdate();
					}
				}
			};

			// Set remaining check states from previous (OR default) settings and update button states.
			propCheck.isChecked = !ModSettings.treeSelected;
			treeCheck.isChecked = ModSettings.treeSelected;
			UpdateButtonStates();

			// Apply Harmony rendering patches.
			Patcher.PatchNetworkOverlays(true);
		}


		/// <summary>
		/// Updates the target item record for changes in replacement status (e.g. after applying or reverting changes).
		/// </summary>
		/// <param name="propListItem">Target item</param>
		protected override void UpdateTargetItem(PropListItem propListItem)
		{
			if (propListItem is NetPropListItem netItem)
			{
				// Determine lane and index to test - just grab first one from list.
				int lane = netItem.lanes[0];
				int propIndex = netItem.indexes[0];

				// All-network replacement and original probability (if any).
				BOBNetReplacement allNetReplacement = AllNetworkReplacement.instance.ActiveReplacement(currentNet, lane, propIndex);
				if (allNetReplacement != null)
				{
					propListItem.allPrefab = allNetReplacement.replacementInfo;
					propListItem.allProb = allNetReplacement.probability;
				}
				else
				{
					// If no active current record, ensure that it's reset to null.
					propListItem.allPrefab = null;
				}

				// Individual network replacement and original probability (if any).
				BOBNetReplacement netReplacement = NetworkReplacement.instance.ActiveReplacement(currentNet, lane, propIndex);
				if (netReplacement != null)
				{
					propListItem.replacementPrefab = netReplacement.replacementInfo;
					propListItem.replacementProb = netReplacement.probability;
				}
				else
				{
					// If no active current record, ensure that it's reset to null.
					propListItem.replacementPrefab = null;
				}

				// Replacement pack replacement and original probability (if any).
				BOBNetReplacement packReplacement = PackReplacement.instance.ActiveReplacement(currentNet, lane, propIndex);
				if (packReplacement != null)
				{
					propListItem.packagePrefab = packReplacement.replacementInfo;
				}
				else
				{
					// If no active current record, ensure that it's reset to null.
					propListItem.packagePrefab = null;
				}
			}
		}


		/// <summary>
		/// Populates a fastlist with a list of network-specific trees or props.
		/// </summary>
		/// <param name="isTree">True for a list of trees, false for props</param>
		/// <returns>Populated fastlist of loaded prefabs</returns>
		protected override FastList<object> TargetList(bool isTree)
		{
			// List of prefabs that have passed filtering.
			List<NetPropListItem> propList = new List<NetPropListItem>();

			// Check to see if this building contains any props.
			if (currentNet.m_lanes == null || currentNet.m_lanes.Length == 0)
			{
				// No props - show 'no props' label and return an empty list.
				noPropsLabel.Show();
				return new FastList<object>();
			}

			// Local reference.
			NetInfo.Lane[] lanes = currentNet.m_lanes;

			// Iterate through each lane.
			for (int lane = 0; lane < lanes.Length; ++lane)
			{
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
					NetPropListItem propListItem = new NetPropListItem();

					// Try to get relevant prefab (prop/tree), using finalProp.
					PrefabInfo finalInfo = isTree ? (PrefabInfo)laneProps[propIndex]?.m_finalTree : (PrefabInfo)laneProps[propIndex]?.m_finalProp;

					// Check to see if we were succesful - if not (e.g. we only want trees and this is a prop), continue on to next building prop.
					if (finalInfo?.name == null)
					{
						continue;
					}

					// Networks are always grouped - set index and lane to -1 and add to our lists of indexes and lanes.
					propListItem.index = -1;
					propListItem.lane = -1;
					propListItem.indexes.Add(propIndex);
					propListItem.lanes.Add(lane);

					// Get original (pre-replacement) tree/prop prefab and current probability (as default original probability).
					propListItem.originalPrefab = NetworkReplacement.instance.GetOriginal(currentNet, lane, propIndex) ?? AllNetworkReplacement.instance.GetOriginal(currentNet, lane, propIndex) ?? PackReplacement.instance.GetOriginal(currentNet, lane, propIndex) ?? finalInfo;
					propListItem.originalProb = laneProps[propIndex].m_probability;
					propListItem.originalAngle = laneProps[propIndex].m_angle;

					// All-network replacement and original probability (if any).
					BOBNetReplacement allNetReplacement = AllNetworkReplacement.instance.ActiveReplacement(currentNet, lane, propIndex);
					if (allNetReplacement != null)
					{
						propListItem.allPrefab = allNetReplacement.replacementInfo;
						propListItem.allProb = allNetReplacement.probability;
					}

					// Individual network replacement and original probability (if any).
					BOBNetReplacement netReplacement = NetworkReplacement.instance.ActiveReplacement(currentNet, lane, propIndex);
					if (netReplacement != null)
					{
						propListItem.replacementPrefab = netReplacement.replacementInfo;
						propListItem.replacementProb = netReplacement.probability;
					}

					// Replacement pack replacement and original probability (if any).
					BOBNetReplacement packReplacement = PackReplacement.instance.ActiveReplacement(currentNet, lane, propIndex);
					if (packReplacement != null)
					{
						propListItem.packagePrefab = packReplacement.replacementInfo;
					}

					// Are we grouping?
					if (propListItem.index == -1)
					{
						// Yes, grouping - initialise a flag to show if we've matched.
						bool matched = false;

						// Iterate through each item in our existing list of props.
						foreach (NetPropListItem item in propList)
						{
							// Check to see if we already have this in the list - matching original prefab, building replacement prefab, all-building replacement prefab, and probability.
							if (item.originalPrefab == propListItem.originalPrefab && item.replacementPrefab == propListItem.replacementPrefab && propListItem.allPrefab == item.allPrefab)
							{
								// We've already got an identical grouped instance of this item - add this index and lane to the lists of indexes and lanes under that item and set the flag to indicate that we've done so.
								item.indexes.Add(propIndex);
								item.lanes.Add(lane);
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
					propList.Add(propListItem);
				}
			}

			// Create return fastlist from our filtered list, ordering by name.
			FastList<object> fastList = new FastList<object>()
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


		/// <summary>
		/// Performs actions to be taken once an update (application or reversion) has been applied, including saving data, updating button states, and refreshing renders.
		/// </summary>
		private void FinishUpdate()
        {
			// Save configuration file and refresh target list (to reflect our changes).
			ConfigurationUtils.SaveConfig();
			UpdateTargetList();

			// Update button states.
			UpdateButtonStates();

			// Update any dirty net renders.
			NetData.Update();
		}
	}
}
