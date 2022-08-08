using System;
using System.Collections.Generic;
using UnityEngine;


namespace BOB
{
	/// <summary>
	/// Base class for network replacement.
	/// </summary>
	internal abstract class NetworkReplacementBase
	{
		/// <summary>
		/// Returns the config file list of network elements relevant to the current replacement type.
		/// </summary>
		protected abstract List<BOBNetworkElement> NetworkElementList { get; }


		/// <summary>
		/// Retrieves a currently-applied replacement entry for the given network, lane and prop index.
		/// </summary>
		/// <param name="netInfo">Targeted network prefab</param>
		/// <param name="targetInfo">Target prop/tree prefab</param>
		/// <param name="laneIndex">Lane number</param>
		/// <param name="propIndex">Prop index number</param>
		/// <returns>Currently-applied feplacement (null if none)</returns>
		internal abstract BOBNetReplacement ActiveReplacement(NetInfo netInfo, PrefabInfo targetInfo, int laneIndex, int propIndex);


		/// <summary>
		/// Retuns the list of active prop references for the given replacement value(s).
		/// </summary>
		/// <param name="netInfo">Targeted network prefab</param>
		/// <param name="targetInfo">Targeted (original) prop prefab</param>
		/// <param name="laneIndex">Targeted lane index (in parent network)</param>
		/// <param name="propIndex">Targeted prop index (in lanme)</param>
		/// <returns>List of active prop references for the given replacment values (null if none)</returns>
		internal virtual List<NetPropReference> ReferenceList(NetInfo netInfo, PrefabInfo targetInfo, int laneIndex, int propIndex) => ActiveReplacement(netInfo, targetInfo, laneIndex, propIndex)?.references;


		/// <summary>
		/// Reverts all active replacements.
		/// </summary>
		internal virtual void RevertAll()
		{
			// Iterate through each entry in the replacement list.
			foreach (BOBNetworkElement netElement in NetworkElementList)
			{
				foreach (BOBNetReplacement replacement in netElement.replacements)
				{
					// Revert all references for this replacement.
					RevertReferences(replacement.NetInfo, replacement.references);
				}
			}

			// Clear configuration file entry.
			NetworkElementList.Clear();
		}


		/// <summary>
		/// Checks if there's a currently active replacement applied to the given network, lane and prop index, and if so, returns the replacement record.
		/// </summary>
		/// <param name="netInfo">Net prefab to check</param>
		/// <param name="laneIndex">Lane index to check</param>
		/// <param name="propIndex">Prop index to check</param>
		/// <param name="propReference">Original prop reference record (null if none)</param>
		/// <returns>Replacement record if a replacement is currently applied, null if no replacement is currently applied</returns>
		internal virtual BOBNetReplacement ActiveReplacement(NetInfo netInfo, int laneIndex, int propIndex, out NetPropReference propReference)
		{
			// See if we've got a replacement entry for this building.
			List<BOBNetReplacement> replacementList = ReplacementList(netInfo);
			if (replacementList != null)
			{
				// Iterate through each replacement record for this network.
				foreach (BOBNetReplacement netReplacement in replacementList)
				{
					// Iterate through each prop reference in this record. 
					foreach (NetPropReference propRef in netReplacement.references)
					{
						// Check for a a network(due to all- replacement), lane and prop index match.
						if (propRef.netInfo == netInfo && propRef.laneIndex == laneIndex && propRef.PropIndex == propIndex)
						{
							// Match!  Return the replacement record.
							propReference = propRef;
							return netReplacement;
						}
					}
				}
			}

			// If we got here, no entry was found - return null to indicate no active replacement.
			propReference = null;
			return null;
		}


		/// <summary>
		/// Applies a new (or updated) replacement.
		/// </summary>
		/// <param name="netInfo">Targeted network prefab</param>
		/// <param name="targetInfo">Targeted (original) prop prefab</param>
		/// <param name="replacementInfo">Replacment prop prefab</param>
		/// <param name="laneIndex">Targeted lane index (in parent network)</param>
		/// <param name="propIndex">Prop index to apply replacement to</param>
		/// <param name="angle">Replacment prop angle adjustment</param>
		/// <param name="offsetX">Replacment X position offset</param>
		/// <param name="offsetY">Replacment Y position offset</param>
		/// <param name="offsetZ">Replacment Z position offset</param>
		/// <param name="probability">Replacement probability</param>
		/// <param name="repeatDistance">Replacement repeat distance</param>
		internal void Replace(NetInfo netInfo, PrefabInfo targetInfo, PrefabInfo replacementInfo, int laneIndex, int propIndex, float angle, float offsetX, float offsetY, float offsetZ, int probability, float repeatDistance)
		{
			// Null checks.
			if (targetInfo?.name == null || replacementInfo?.name == null)
			{
				return;
			}

			// Revert any current replacement entry for this prop.
			Revert(netInfo, targetInfo, laneIndex, propIndex, true);

			// Get configuration file network list entry.
			List<BOBNetReplacement> replacementsList = ReplacementEntry(netInfo);

			// Get current replacement after reversion above.
			BOBNetReplacement thisReplacement = ActiveReplacement(netInfo, targetInfo, laneIndex, propIndex);

			// Create new replacement list entry if none already exists.
			if (thisReplacement == null)
			{
				thisReplacement = new BOBNetReplacement
				{
					parentInfo = netInfo,
					target = targetInfo.name,
					targetInfo = targetInfo,
					laneIndex = laneIndex,
					propIndex = propIndex
				};
				replacementsList.Add(thisReplacement);
			}

			// Add/replace replacement data.
			thisReplacement.isTree = targetInfo is TreeInfo;
			thisReplacement.angle = angle;
			thisReplacement.offsetX = offsetX;
			thisReplacement.offsetY = offsetY;
			thisReplacement.offsetZ = offsetZ;
			thisReplacement.probability = probability;
			thisReplacement.repeatDistance = repeatDistance;

			// Record replacement prop.
			thisReplacement.replacementInfo = replacementInfo;
			thisReplacement.Replacement = replacementInfo.name;

			// Apply replacement.
			ApplyReplacement(thisReplacement);
		}


		/// <summary>
		/// Reverts a replacement.
		/// </summary>
		/// <param name="netInfo">Targeted network prefab</param>
		/// <param name="targetInfo">Targeted (original) tree/prop prefab</param>
		/// <param name="laneIndex">Targeted (original) tree/prop lane index</param>
		/// <param name="propIndex">Targeted (original) tree/prop prop index</param>
		/// <param name="removeEntries">True to remove the reverted entries from the list of replacements, false to leave the list unchanged</param>
		internal void Revert(NetInfo netInfo, PrefabInfo targetInfo, int laneIndex, int propIndex, bool removeEntries) => Revert(ActiveReplacement(netInfo, targetInfo, laneIndex, propIndex), removeEntries);


		/// <summary>
		/// Restores a replacement, if any (e.g. after a higher-priority replacement has been reverted).
		/// </summary>
		/// <param name="netInfo">Network prefab</param>
		/// <param name="targetInfo">Target prop info</param>
		/// <param name="laneIndex">Lane index</param>
		/// <param name="propIndex">Prop index</param>
		/// <returns>True if a restoration was made, false otherwise</returns>
		internal bool Restore(NetInfo netInfo, PrefabInfo targetInfo, int laneIndex, int propIndex)
		{
			// See if we have a relevant replacement record.
			BOBNetReplacement thisReplacement = ActiveReplacement(netInfo, targetInfo, laneIndex, propIndex);
			if (thisReplacement != null)
			{
				// Yes - add reference data to the list.
				NetPropReference newReference = CreateReference(netInfo, laneIndex, propIndex, thisReplacement.isTree);
				AddReference(thisReplacement, newReference);

				// Apply replacement and return true to indicate restoration.
				ReplaceProp(thisReplacement, newReference);

				return true;
			}

			// If we got here, no restoration was made.
			return false;
		}


		/// <summary>
		/// Unapplies a particular replacement instance to defer to a higher-priority replacement.
		/// </summary>
		/// <param name="netInfo">Network prefab</param>
		/// <param name="targetInfo">Target prefab</param>
		/// <param name="laneIndex">Lane index</param>
		/// <param name="propIndex">Prop index</param>
		internal void RemoveEntry(NetInfo netInfo, PrefabInfo targetInfo, int laneIndex, int propIndex)
		{
			// Check to see if we have an entry for this prefab and target.
			List<NetPropReference> referenceList = ReferenceList(netInfo, targetInfo, laneIndex, propIndex);
			if (referenceList != null)
			{
				// Got an active reference list; create a variable to store any matching reference for later removal.
				NetPropReference thisPropReference = null;

				// Iterate through each recorded prop reference.
				foreach (NetPropReference propReference in referenceList)
				{
					// Look for a network, lane and index match.
					if (propReference.netInfo == netInfo && propReference.laneIndex == laneIndex && propReference.PropIndex == propIndex)
					{
						// Got a match!  Revert instance.
						RevertReference(propReference);

						// Record the matching reference and stop iterating - we're done here.
						thisPropReference = propReference;
						break;
					}
				}

				// Remove replacement if one was found.
				if (thisPropReference != null)
				{
					referenceList.Remove(thisPropReference);
				}
			}
		}


		/// <summary>
		/// Deserialises a network element list.
		/// </summary>
		/// <param name="elementList">Element list to deserialise</param>
		internal void Deserialize(List<BOBNetworkElement> elementList)
		{
			// Iterate through each element in the provided list.
			foreach (BOBNetworkElement element in elementList)
			{
				// Try to find network prefab.
				element.prefab = PrefabCollection<NetInfo>.FindLoaded(element.network);

				// Don't bother deserializing further if the network info wasn't found.
				if (element.NetInfo != null)
				{
					Deserialize(element.NetInfo, element.replacements);
				}
			}
		}


		/// <summary>
		/// Applies a replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to apply</param>
		protected abstract void ApplyReplacement(BOBNetReplacement replacement);


		/// <summary>
		/// Restores any replacements from lower-priority replacements after a reversion.
		/// </summary>
		/// <param name="netInfo">Network prefab</param>
		/// <param name="targetInfo">Target prop info</param>
		/// <param name="laneIndex">Lane index</param>
		/// <param name="propIndex">Prop index</param>
		protected abstract void RestoreLower(NetInfo netInfo, PrefabInfo targetInfo, int laneIndex, int propIndex);


		/// <summary>
		/// Gets the relevant replacement list entry from the active configuration file, if any.
		/// </summary>
		/// <param name="netInfo">Network prefab</param>
		/// <returns>Replacement list for the specified network prefab (null if none)</returns>
		protected virtual List<BOBNetReplacement> ReplacementList(NetInfo netInfo) => NetworkElement(netInfo)?.replacements;


		/// <summary>
		/// Gets the relevant network replacement list entry from the active configuration file, creating a new network entry if none already exists.
		/// </summary>
		/// <param name="netInfo">Network prefab</param>
		/// <returns>Replacement list for the specified network prefab</returns>
		protected virtual List<BOBNetReplacement> ReplacementEntry(NetInfo netInfo)
		{
			// Get existing entry for this network.
			BOBNetworkElement thisNetwork = NetworkElement(netInfo);

			// If no existing entry, create a new one.
			if (thisNetwork == null)
			{
				thisNetwork = new BOBNetworkElement(netInfo);
				NetworkElementList.Add(thisNetwork);
			}

			// Return replacement list from this entry.
			return thisNetwork.replacements;
		}


		/// <summary>
		/// Reverts a replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to revert</param>
		/// <param name="removeEntries">True to remove the reverted entries from the list of replacements, false to leave the list unchanged</param>
		/// <returns>True if the entire network record was removed from the list (due to no remaining replacements for that prefab), false if the prefab remains in the list (has other active replacements)</returns>
		protected virtual bool Revert(BOBNetReplacement replacement, bool removeEntries)
		{
			// Safety check for calls without any current replacement.
			if (replacement?.targetInfo == null || replacement.references == null)
			{
				return false;
			}

			if (replacement.references != null)
			{
				// Revert all entries in list.
				RevertReferences(replacement.targetInfo, replacement.references);

				// Remove replacement entry from list of replacements, if we're doing so.
				if (removeEntries)
				{
					// Remove from replacement list.
					ReplacementList(replacement.NetInfo).Remove(replacement);

					// See if we've got a parent network element record, and if so, if it has any remaining replacement entries.
					BOBNetworkElement thisElement = NetworkElement(replacement.NetInfo);
					if (thisElement != null && (thisElement.replacements == null || thisElement.replacements.Count == 0))
					{
						// No replacement entries left - delete entire network entry and return true to indicate that we've done so.
						NetworkElementList.Remove(thisElement);
						return true;
					}
				}
			}

			// If we got here, we didn't remove any network entries from the list; return false.
			return false;
		}


		/// <summary>
		/// Adds the given prop reference to the record for the given replacement.
		/// </summary>
		/// <param name="replacement">Replacement reference</param>
		/// <param name="propReference">Pop reference to store</param>
		protected virtual void AddReference(BOBNetReplacement replacement, NetPropReference propReference) => replacement.references.Add(propReference);


		/// <summary>
		/// Reverts all prop references in the provided list using the given replacement list and original target prefab.
		/// </summary>
		/// <param name="originalPrefab">Original prop/tree prefab</param>
		/// <param name="references">List of prop references to revert</param>
		protected void RevertReferences(PrefabInfo originalPrefab, List<NetPropReference> references)
		{
			// Iterate through each entry in our list.
			foreach (NetPropReference propReference in references)
			{
				// Revert entry.
				RevertReference(propReference);

				// Restore any lower-priority replacements.
				RestoreLower(propReference.netInfo, originalPrefab, propReference.laneIndex, propReference.PropIndex);

				// Add network to dirty list.
				NetData.DirtyList.Add(propReference.netInfo);
			}
		}


		/// <summary>
		/// Creates a new PropReference from the provided network prefab, lane and prop index.
		/// </summary>
		/// <param name="netInfo">Network prefab</param>
		/// <param name="laneIndex">Lane index</param>
		/// <param name="propIndex">Prop index</param>
		/// <param name="isTree">True if this is a tree reference, false if this is a prop reference</param>
		/// <returns>Newly-created reference (null if creation failed)</returns>
		protected NetPropReference CreateReference(NetInfo netInfo, int laneIndex, int propIndex, bool isTree)
		{
			// Safety checks.
			if (netInfo != null && laneIndex >= 0 && propIndex >= 0)
			{
				// Local reference.
				NetLaneProps.Prop thisProp = netInfo.m_lanes[laneIndex].m_laneProps.m_props[propIndex];

				// Create and return new reference.
				return new NetPropReference(
					propIndex,
					thisProp.m_prop,
					thisProp.m_finalProp,
					thisProp.m_tree,
					thisProp.m_finalTree,
					thisProp.m_position,
					thisProp.m_probability)
				{
					netInfo = netInfo,
					laneIndex = laneIndex,
					angle = thisProp.m_angle,
					repeatDistance = thisProp.m_repeatDistance,
				};
			}

			// If we got here, something went wrong; return null.
			Logging.Error("invalid argument passed to NetworkReplacementBase.CreateReference");
			return null;
		}


		/// <summary>
		/// Creates a new PropReference from the provided building prefab and prop index.
		/// </summary>
		/// <param name="reference">Referene to revert</param>
		protected void RevertReference(NetPropReference reference)
		{
			// Local reference.
			NetLaneProps.Prop thisProp = reference.netInfo.m_lanes[reference.laneIndex].m_laneProps.m_props?[reference.PropIndex];
			if (thisProp != null)
			{
				thisProp.m_prop = reference.OriginalProp;
				thisProp.m_finalProp = reference.OriginalFinalProp;
				thisProp.m_tree = reference.OriginalTree;
				thisProp.m_finalTree = reference.OriginalFinalTree;
				thisProp.m_angle = reference.angle;
				thisProp.m_position = reference.OriginalPosition;
				thisProp.m_probability = reference.OriginalProbability;
				thisProp.m_repeatDistance = reference.repeatDistance;

				// Update network.
				reference.netInfo.CheckReferences();
				NetData.DirtyList.Add(reference.netInfo);
			}
		}


		/// <summary>
		/// Replaces a prop, using a network replacement.
		/// </summary>
		/// <param name="replacement">Network replacement to apply</param>
		/// <param name="propReference">Individual prop reference to apply to</param>
		protected void ReplaceProp(BOBNetReplacement replacement, NetPropReference propReference)
		{
			// If this is a vanilla network, then we've probably got shared NetLaneProp references, so need to copy to a new instance.
			// If the name doesn't contain a period (c.f. 12345.MyNetwok_Data), then assume it's vanilla - may be a mod or not shared, but better safe than sorry.
			if (!propReference.netInfo.name.Contains("."))
			{
				NetData.CloneLanePropInstance(propReference.netInfo, propReference.laneIndex);
			}

			// Convert offset to Vector3.
			Vector3 offset = new Vector3
			{
				x = replacement.offsetX,
				y = replacement.offsetY,
				z = replacement.offsetZ
			};

			NetInfo.Lane thisLane = propReference.netInfo.m_lanes[propReference.laneIndex];

			// Apply replacement.
			thisLane.m_laneProps.m_props[propReference.PropIndex].m_prop = replacement.ReplacementProp;
			thisLane.m_laneProps.m_props[propReference.PropIndex].m_finalProp = replacement.ReplacementProp;
			thisLane.m_laneProps.m_props[propReference.PropIndex].m_tree = replacement.ReplacementTree;
			thisLane.m_laneProps.m_props[propReference.PropIndex].m_finalTree = replacement.ReplacementTree;

			// Invert x offset and angle to match original prop x position.
			float angleMult = 1f;
			if (thisLane.m_position + propReference.OriginalPosition.x < 0)
			{
				offset.x = 0 - offset.x;
				angleMult = -1;
			}

			// Angle and offset.
			thisLane.m_laneProps.m_props[propReference.PropIndex].m_angle = propReference.angle + (replacement.angle * angleMult);
			thisLane.m_laneProps.m_props[propReference.PropIndex].m_position = propReference.OriginalPosition + offset;

			// Probability.
			thisLane.m_laneProps.m_props[propReference.PropIndex].m_probability = replacement.probability;

			// Repeat distance, if a valid value is set.
			if (replacement.repeatDistance > 1)
			{
				thisLane.m_laneProps.m_props[propReference.PropIndex].m_repeatDistance = replacement.repeatDistance;
			}

			// Update network prop references.
			propReference.netInfo.CheckReferences();

			// Add network to dirty list.
			NetData.DirtyList.Add(propReference.netInfo);
		}


		/// <summary>
		/// Returns the configuration file record for the specified network prefab.
		/// </summary>
		/// <param name="netInfo">Network prefab</param>
		/// <returns>Replacement record for the specified network prefab (null if none)</returns>
		protected BOBNetworkElement NetworkElement(NetInfo netInfo) => netInfo == null ? null : NetworkElementList?.Find(x => x.NetInfo == netInfo);


		/// <summary>
		/// Deserialises a replacement list.
		/// </summary>
		/// <param name="netInfo">Network prefab</param>
		/// <param name="elementList">Replacement list to deserialise</param>
		protected void Deserialize(NetInfo NetInfo, List<BOBNetReplacement> replacementList)
		{
			// Iterate through each element in the provided list.
			foreach (BOBNetReplacement replacement in replacementList)
			{
				try
				{
					// Assign network info.
					replacement.parentInfo = NetInfo;

					// Try to find target prefab.
					replacement.targetInfo = replacement.isTree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacement.target) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacement.target);

					// Try to find replacement prefab.
					replacement.replacementInfo = ConfigurationUtils.FindReplacementPrefab(replacement.Replacement, replacement.isTree);

					// Try to apply the replacement.
					ApplyReplacement(replacement);
				}
				catch (Exception e)
				{
					// Don't let a single failure stop us.
					Logging.LogException(e, "exception deserializing network replacement");
				}
			}
		}
	}
}