using System.Collections.Generic;
using UnityEngine;
using ColossalFramework.UI;


namespace BOB
{
	class BOBNetInfoPanel : BOBInfoPanel
	{
		// Current selection reference.
		NetInfo currentNet;

		// Panel components.
		internal UITextField angleField, xField, yField, zField;


		// Button labels.
		protected override string ReplaceLabel => Translations.Translate("BOB_PNL_RTN");

		protected override string ReplaceAllLabel => Translations.Translate("BOB_PNL_RAN");


		// Trees or props?
		protected override bool IsTree => false;


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

			// Angle label and textfield.
			UILabel angleLabel = AddUIComponent<UILabel>();
			angleLabel.relativePosition = new Vector2(LeftWidth + (Margin * 2), ProbabilityY);
			angleLabel.text = Translations.Translate("BOB_PNL_ANG");

			angleField = UIUtils.AddTextField(this, 190f, 30f);
			angleField.relativePosition = new Vector2(LeftWidth + (Margin * 2), ProbabilityY + angleLabel.height);

			// Offset X position.
			UILabel xLabel = AddUIComponent<UILabel>();
			xLabel.relativePosition = new Vector2(LeftWidth + (Margin * 2), 367f);
			xLabel.text = Translations.Translate("BOB_PNL_XOF");
			xField = UIUtils.AddTextField(this, 100f, 30f);
			xField.relativePosition = new Vector2(LeftWidth + (Margin * 2) + 90f, 360f);

			// Offset Y position.
			UILabel yLabel = AddUIComponent<UILabel>();
			yLabel.relativePosition = new Vector2(LeftWidth + (Margin * 2), 407f);
			yLabel.text = Translations.Translate("BOB_PNL_YOF");
			yField = UIUtils.AddTextField(this, 100f, 30f);
			yField.relativePosition = new Vector2(LeftWidth + (Margin * 2) + 90f, 400f);

			// Offset Z position.
			UILabel zLabel = AddUIComponent<UILabel>();
			zLabel.relativePosition = new Vector2(LeftWidth + (Margin * 2), 447f);
			zLabel.text = Translations.Translate("BOB_PNL_ZOF");
			zField = UIUtils.AddTextField(this, 100f, 30f);
			zField.relativePosition = new Vector2(LeftWidth + (Margin * 2) + 90f, 440f);

			// Replace button event handler.
			replaceButton.eventClicked += (control, clickEvent) =>
			{
				// Make sure we have valid a target and replacement.
				if (currentTargetItem != null && replacementPrefab != null)
				{
					// Create new replacement record with current info.
					NetReplacement replacement = new NetReplacement
					{
						isTree = false,
						probability = 100,
						originalProb = currentTargetItem.originalProb,
						targetIndex = currentTargetItem.index,
						replacementInfo = replacementPrefab,
						replaceName = replacementPrefab.name,
						lane = CurrentNetTargetItem.lane,

						// Original prefab is null if no active replacement; in which case, use the current prefab (which IS the original prefab).
						targetInfo = currentTargetItem.originalPrefab ?? currentTargetItem.currentPrefab
					};
					replacement.targetName = replacement.targetInfo.name;

					// Try to parse textfields.
					float.TryParse(angleField.text, out replacement.angle);
					float.TryParse(xField.text, out replacement.offsetX);
					float.TryParse(yField.text, out replacement.offsetY);
					float.TryParse(zField.text, out replacement.offsetZ);

					// Update text fields to match parsed values.
					angleField.text = replacement.angle.ToString();
					xField.text = replacement.offsetX.ToString();
					yField.text = replacement.offsetY.ToString();
					zField.text = replacement.offsetZ.ToString();

					// Network replacements are always grouped - iterate through each index in the list.
					for (int i = 0; i < currentTargetItem.indexes.Count; ++i)
					{
						// Add the replacement, providing an index override to the current index.
						NetworkReplacement.AddReplacement(currentNet, replacement, currentTargetItem.indexes[i], CurrentNetTargetItem.lanes[i]);
					}

					// Update current target.
					currentTargetItem.currentPrefab = replacementPrefab;

					// Save configuration file and refresh target list (to reflect our changes).
					ConfigurationUtils.SaveConfig();
					targetList.Refresh();

					// Update button states.
					UpdateButtonStates();
				}
			};

			// All network button event handler.
			replaceAllButton.eventClicked += (control, clickEvent) =>
			{
				// Try to parse text fields.
				float angle, xOffset, yOffset, zOffset;
				float.TryParse(angleField.text, out angle);
				float.TryParse(xField.text, out xOffset);
				float.TryParse(yField.text, out yOffset);
				float.TryParse(zField.text, out zOffset);

				// Update text fields to match parsed values.
				angleField.text = angle.ToString();
				xField.text = xOffset.ToString();
				yField.text = yOffset.ToString();
				zField.text = zOffset.ToString();

				// Apply replacement.
				AllNetworkReplacement.Apply(currentTargetItem.originalPrefab ?? currentTargetItem.currentPrefab, replacementPrefab, angle, xOffset, yOffset, zOffset);

				// Update current target.
				currentTargetItem.allPrefab = replacementPrefab;

				// Save configuration file and refresh building list (to reflect our changes).
				ConfigurationUtils.SaveConfig();
				targetList.Refresh();

				// Update button states.
				UpdateButtonStates();
			};

			// Revert button event handler.
			revertButton.eventClicked += (control, clickEvent) =>
			{
				// Network or all-network reversion?
				if (currentTargetItem.allPrefab != null)
				{
					// All-network reversion - make sure we've got a currently active replacement before doing anything.
					if (currentTargetItem.originalPrefab)
					{
						// Apply all-network reversion.
						AllNetworkReplacement.Revert(currentTargetItem.originalPrefab, currentTargetItem.allPrefab);

						// Clear current target 'all' prefab.
						currentTargetItem.allPrefab = null;

						// Save configuration file and refresh target list (to reflect our changes).
						ConfigurationUtils.SaveConfig();
						targetList.Refresh();

						// Update button states.
						UpdateButtonStates();
					}
				}
				else
				{
					// Individual network reversion - ensuire that we've got a current selection before doing anything.
					if (currentTargetItem != null)
					{
						// Network replacements are always grouped -iterate through each instance in the list and revert.
							for (int i = 0; i < currentTargetItem.indexes.Count; ++i)
						{
							// Add the replacement, providing an index override to the current index.
							NetworkReplacement.Revert(currentNet, CurrentNetTargetItem.lanes[i], currentTargetItem.indexes[i]);
						}

						// Clear current target 'all' prefab.
						currentTargetItem.currentPrefab = null;

						// Save configuration file and refresh building list (to reflect our changes).
						ConfigurationUtils.SaveConfig();
						targetList.Refresh();

						// Update button states.
						UpdateButtonStates();
					}
				}
			};

			// Populate initial lists.
			loadedList.rowsData = LoadedList(false);
			targetList.rowsData = TargetList(false);
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

					// Try to get original (pre-replacement) tree/prop prefab.
					propListItem.originalPrefab = NetworkReplacement.GetOriginal(currentNet, lane, propIndex);

					// If the above returned null, there's no currently active building replacement.
					if (propListItem.originalPrefab == null)
					{
						// Check for currently active all-network replacement.
						propListItem.originalPrefab = AllNetworkReplacement.ActiveReplacement(currentNet, lane, propIndex);
						if (propListItem.originalPrefab == null)
						{
							// No currently active all-network replacement - therefore, the current prefab IS the original, so set original prefab record accordingly.
							propListItem.originalPrefab = finalInfo;
						}
						else
						{
							// There's a currently active all-network replacement - add that to our record.
							propListItem.allPrefab = finalInfo;
						}
					}
					else
					{
						// There's a currently active net replacement - add that to our record.
						propListItem.currentPrefab = finalInfo;
					}

					// Probability.
					propListItem.probability = laneProps[propIndex].m_probability;

					// Are we grouping?
					if (propListItem.index == -1)
					{
						// Yes, grouping - initialise a flag to show if we've matched.
						bool matched = false;

						// Iterate through each item in our existing list of props.
						foreach (NetPropListItem item in propList)
						{
							// Check to see if we already have this in the list - matching original prefab, building replacement prefab, all-building replacement prefab, and probability.
							if (item.originalPrefab == propListItem.originalPrefab && item.currentPrefab == propListItem.currentPrefab && propListItem.allPrefab == item.allPrefab && item.probability == propListItem.probability)
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
			FastList<object> fastList = new FastList<object>();
			fastList.m_buffer = propList.ToArray();
			fastList.m_size = propList.Count;

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
		/// Gets the current target item as a NetPropListItem.
		/// </summary>
		private NetPropListItem CurrentNetTargetItem => currentTargetItem as NetPropListItem;
	}
}
