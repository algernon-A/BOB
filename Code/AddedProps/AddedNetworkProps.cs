using System;
using System.Collections.Generic;
using UnityEngine;


namespace BOB
{
    /// <summary>
    /// Class to manage added network props.
    /// </summary>
    internal class AddedNetworkProps
    {
        // Dictionary of active additional props.
        private Dictionary<NetInfo.Lane, NetLaneProps.Prop[]> changedNetLanes;


        /// <summary>
        /// Instance reference.
        /// </summary>
        internal static AddedNetworkProps Instance { get; private set; }


        /// <summary>
        /// Returns the config file list of network elements relevant to the current replacement type.
        /// </summary>
        private List<BOBNetworkElement> NetworkElementList => ConfigurationUtils.CurrentConfig.addedNetworkProps;


        /// <summary>
        /// Constructor.
        /// </summary>
        internal AddedNetworkProps()
        {
            // Init dictionary.
            changedNetLanes = new Dictionary<NetInfo.Lane, NetLaneProps.Prop[]>();

            // Set instance reference.
            Instance = this;
        }


        /// <summary>
        /// Checks whether or not the prop with a given lane and prop index is an added prop.
        /// </summary>
        /// <param name="netInfo">Network prefab to check</param>
        /// <param name="lane">Lane index to check</param>
        /// <param name="index">Prop index to check</param>
        /// <returns>True if the prop is added, false if not.</returns>
        internal bool IsAdded(NetInfo netInfo, int lane, int index) => IsAdded(netInfo.m_lanes[lane], index);


        /// <summary>
        /// Checks whether or not the prop with a given lane and prop index is an added prop.
        /// </summary>
        /// <param name="lane">Lane prefab to check</param>
        /// <param name="index">Prop index to check</param>
        /// <returns>True if the prop is added, false if not.</returns>
        internal bool IsAdded(NetInfo.Lane lane, int index)
        {
            // Null check.
            if (lane == null)
            {
                Logging.Error("null lane passed to AddedNetworkProps.IsAdded");
                return false;
            }

            // Return value depends on the dictionary containing an entry.
            if (changedNetLanes.TryGetValue(lane, out NetLaneProps.Prop[] originalProps))
            {
                // If there's no original props array,there were no original props; therefore anything here is added.
                if (originalProps == null)
                {
                    return true;
                }

                return index >= originalProps.Length;
            }

            // If we got here, there's no added prop entry.
            return false;
        }


        /// <summary>
        /// Adds a new prop to a network after updating the config file with the new entry.
        /// </summary>
        /// <param name="data">Network replacement data record</param>
        internal void AddNew(BOBNetReplacement data)
        {
            // Local reference.
            NetInfo.Lane lane = data.NetInfo.m_lanes[data.laneIndex];

            // Add entry to configuration file.
            List<BOBNetReplacement> replacementList = ReplacementEntry(data.NetInfo);
            BOBNetReplacement replacementEntry = replacementList.Find(x => x.propIndex == data.propIndex);
            if (replacementEntry != null)
            {
                replacementList.Remove(replacementEntry);
            }
            replacementList.Add(data);

            // Add lane to changed lanes list, if it's not already there.
            if (!changedNetLanes.ContainsKey(lane))
            {
                changedNetLanes.Add(lane, lane.m_laneProps.m_props);
            }

            // Add prop to lane.
            AddProp(data);
        }


        /// <summary>
        /// Removes the specified added network prop.
        /// </summary>
        /// <param name="network">Network prefab</param>
        /// <param name="laneIndex">Lane index</param>
        /// <param name="index">Prop index</param>
        internal void RemoveNew(NetInfo network, int laneIndex, int index)
        {
            // Get lane reference.
            NetInfo.Lane lane = network.m_lanes[laneIndex];

            // Don't do anything if no valid network entry.
            if (changedNetLanes.TryGetValue(lane, out NetLaneProps.Prop[] originalProps))
            {
                // Restore original prop list.
                lane.m_laneProps.m_props = originalProps;

                // Remove entry from list.
                List<BOBNetReplacement> replacementList = ReplacementEntry(network);
                if (replacementList != null && replacementList.Count > 0)
                {
                    // Found an entry for this network - try to find the matching prop index.
                    BOBNetReplacement targetEntry = replacementList.Find(x => x.propIndex == index);
                    if (targetEntry != null)
                    {
                        // Found a matching index - remove the prop from the list.
                        replacementList.Remove(targetEntry);
                    }

                    // Add network to dirty list.
                    NetData.DirtyList.Add(network);

                    // Check if any added props remaining for this lane.
                    if (replacementList.Count > 0)
                    {
                        // Yes - iterate through and re-add them (updating indexes).
                        foreach (BOBNetReplacement replacement in replacementList)
                        {
                            if (replacement.propIndex >= 0)
                            {
                                replacement.propIndex = AddProp(replacement);
                            }
                        }
                    }
                    else
                    {
                        // No remaining added props - remove lane entry entirely.
                        changedNetLanes.Remove(lane);
                    }
                }
            }
        }


        /// <summary>
        /// Updates an existing replacement entry.
        /// </summary>
        /// <param name="netInfo">Targeted network prefab</param>
        /// <param name="targetInfo">Targeted (original) prop prefab</param>
        /// <param name="replacementInfo">Replacment prop prefab</param>
        /// <param name="laneIndex">Lane index to apply replacement to</param>
        /// <param name="propIndex">Prop index to apply replacement to</param>
        /// <param name="angle">Replacment prop angle adjustment</param>
        /// <param name="offsetX">Replacment X position offset</param>
        /// <param name="offsetY">Replacment Y position offset</param>
        /// <param name="offsetZ">Replacment Z position offset</param>
        /// <param name="probability">Replacement probability</param>
        /// <param name="repeatDistance">Replacement repeat distance</param>
        internal void Update(NetInfo netInfo, PrefabInfo targetInfo, PrefabInfo replacementInfo, int laneIndex, int propIndex, float angle, float offsetX, float offsetY, float offsetZ, int probability, float repeatDistance)
        {
            // Check for valid daa.
            if (replacementInfo?.name == null || laneIndex < 0 || netInfo?.m_lanes == null || laneIndex >= netInfo.m_lanes.Length || propIndex < 0)
            {
                Logging.Error("invalid data passed to AddedNetworkProps.Update");
                return;
            }

            // Existing reference checks.
            NetInfo.Lane lane = netInfo.m_lanes[laneIndex];
            if (!IsAdded(lane, propIndex) || !changedNetLanes.ContainsKey(lane))
            {
                Logging.Error("unrecorded reference passed to AddedNetworkProps.Update");
                return;
            }

            // Get network element list.
            List<BOBNetReplacement> replacementList = ReplacementEntry(netInfo);

            // Try to find matching entry.
            BOBNetReplacement thisReplacement = replacementList.Find(x => x.laneIndex == laneIndex && x.propIndex == propIndex);

            if (thisReplacement != null)
            {
                // Invert x offset and angle to match original prop x position.
                float angleMult = 1f;
                float xOffset = offsetX;
                if (lane.m_position < 0)
                {
                    xOffset = 0 - xOffset;
                    angleMult = -1;
                }

                // Update replacment entry.
                thisReplacement.laneIndex = laneIndex;
                thisReplacement.propIndex = propIndex;
                thisReplacement.isTree = targetInfo is TreeInfo;
                thisReplacement.angle = angle * angleMult;
                thisReplacement.offsetX = offsetX;  // Use unmirrored X to save.
                thisReplacement.offsetY = offsetY;
                thisReplacement.offsetZ = offsetZ;
                thisReplacement.probability = probability;
                thisReplacement.repeatDistance = repeatDistance;

                // Record replacement prop.
                thisReplacement.replacementInfo = replacementInfo;
                thisReplacement.Replacement = replacementInfo.name;

                // Apply update to targeted prop.
                NetLaneProps.Prop thisProp = lane.m_laneProps.m_props[propIndex];
                thisProp.m_prop = replacementInfo as PropInfo;
                thisProp.m_tree = replacementInfo as TreeInfo;
                thisProp.m_finalProp = replacementInfo as PropInfo;
                thisProp.m_finalTree = replacementInfo as TreeInfo;
                thisProp.m_angle = angle * angleMult;
                thisProp.m_position = new Vector3(xOffset, offsetY, offsetZ);   // Use mirrored X to apply.
                thisProp.m_probability = probability;
                thisReplacement.repeatDistance = repeatDistance;
            }
        }


        /// <summary>
        /// Deserialises and applies added props.
        /// </summary>
        /// <param name="elementList">Element list to deserialise</param>
        internal void Deserialize(List<BOBNetworkElement> elementList)
        {
            // Iterate through each element in the provided list.
            foreach (BOBNetworkElement element in elementList)
            {
                // Try to find network prefab.
                element.prefab = PrefabCollection<NetInfo>.FindLoaded(element.network);

                // Don't bother deserializing further if the netInfo wasn't found.
                if (element.NetInfo != null)
                {
                    Deserialize(element.NetInfo, element.replacements);
                }
            }
        }


        /// <summary>
        /// Adds a new tree or prop to a network prefab.
        /// </summary>
        private int AddProp(BOBNetReplacement replacement)
        {
            // Make sure a valid replacement prefab is set.
            NetInfo netInfo = replacement?.NetInfo;
            if (netInfo != null)
            {
                // New prop index.
                int newIndex = 0;

                // Check for valid lane index.
                if (replacement.laneIndex < 0 || replacement.NetInfo.m_lanes == null)
                {
                    return -1;
                }

                // Lane reference.
                NetInfo.Lane lane = replacement.NetInfo.m_lanes[replacement.laneIndex];

                // Make sure lane and landeProps isn't null.
                if (lane?.m_laneProps == null)
                {
                    return -1;
                }

                // Check to see if we've got a current prop array.
                if (lane?.m_laneProps?.m_props != null)
                {
                    // Existing m_props array - check that we've got space for another entry.
                    newIndex = lane.m_laneProps.m_props.Length;
                    if (newIndex > 63)
                    {
                        // Props maxed out - exit.
                        return -1;
                    }

                    // Get old props reference.
                    NetLaneProps.Prop[] oldNetLaneProps = lane.m_laneProps.m_props;

                    // Create new props array with one extra entry, and copy the old props to it.
                    lane.m_laneProps.m_props = new NetLaneProps.Prop[newIndex + 1];
                    for (int i = 0; i < newIndex; ++i)
                    {
                        lane.m_laneProps.m_props[i] = oldNetLaneProps[i];
                    }
                }
                else
                {
                    // No m_props array already; create one.
                    lane.m_laneProps.m_props = new NetLaneProps.Prop[1];
                }

                // Update reference with new index.
                replacement.propIndex = newIndex;

                // Add new prop.
                Logging.Message("adding new prop for network ", netInfo.name, " at lane ", replacement.laneIndex, " and index ", newIndex);

                // Invert x offset and angle to match original prop x position.
                float angleMult = 1f;
                float xOffset = replacement.offsetX;
                if (lane.m_position < 0)
                {
                    xOffset = 0 - xOffset;
                    angleMult = -1;
                }

                lane.m_laneProps.m_props[newIndex] = new NetLaneProps.Prop
                {
                    m_flagsRequired = NetLane.Flags.None,
                    m_flagsForbidden = NetLane.Flags.None,
                    m_startFlagsRequired = NetNode.Flags.None,
                    m_startFlagsForbidden = NetNode.Flags.None,
                    m_endFlagsRequired = NetNode.Flags.None,
                    m_endFlagsForbidden = NetNode.Flags.None,
                    m_colorMode = NetLaneProps.ColorMode.Default,
                    m_angle = replacement.angle * angleMult,
                    m_prop = replacement.replacementInfo as PropInfo,
                    m_tree = replacement.replacementInfo as TreeInfo,
                    m_finalProp = replacement.replacementInfo as PropInfo,
                    m_finalTree = replacement.replacementInfo as TreeInfo,
                    m_segmentOffset = 0f,
                    m_repeatDistance = replacement.repeatDistance,
                    m_minLength = 0f,
                    m_cornerAngle = 0f,
                    m_upgradable = false,
                    m_position = new Vector3(xOffset, replacement.offsetY, replacement.offsetZ),
                    m_probability = replacement.probability
                };

                // Add network to dirty list.
                NetData.DirtyList.Add(netInfo);

                return newIndex;
            }

            return -1;
        }


        /// <summary>
        /// Returns the configuration file record for the specified network prefab.
        /// </summary>
        /// <param name="netInfo">Network prefab</param>
        /// <returns>Replacement record for the specified network prefab (null if none)</returns>
        private BOBNetworkElement NetworkElement(NetInfo netInfo) => netInfo == null ? null : NetworkElementList?.Find(x => x.NetInfo == netInfo);


        /// <summary>
        /// Gets the relevant network list entry from the active configuration file, creating a new network entry if none already exists.
        /// </summary>
        /// <param name="netInfo">Network prefab</param>
        /// <returns>Replacement list for the specified network prefab</returns>
        private List<BOBNetReplacement> ReplacementEntry(NetInfo netInfo)
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
        /// Deserialises a replacement list.
        /// </summary>
        /// <param name="thisNetwork">Network prefab</param>
        /// <param name="elementList">Replacement list to deserialise</param>
        private void Deserialize(NetInfo netInfo, List<BOBNetReplacement> replacementList)
        {
            // Iterate through each element in the provided list.
            foreach (BOBNetReplacement replacement in replacementList)
            {
                replacement.propIndex = -1;

                try
                {
                    // Creeate record for lane info.
                    NetInfo.Lane lane = netInfo.m_lanes[replacement.laneIndex];
                    if (!changedNetLanes.ContainsKey(lane))
                    {
                        changedNetLanes.Add(lane, lane.m_laneProps.m_props);
                    }

                    // Assign network info.
                    replacement.parentInfo = netInfo;

                    // Try to find target prefab.
                    replacement.targetInfo = replacement.isTree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacement.target) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacement.target);

                    // Try to find replacement prefab.
                    replacement.replacementInfo = ConfigurationUtils.FindReplacementPrefab(replacement.Replacement, replacement.isTree);

                    // Try to apply the replacement.
                    replacement.propIndex = AddProp(replacement);
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