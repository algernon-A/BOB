// <copyright file="AddedNetworkProps.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System;
    using System.Collections.Generic;
    using AlgernonCommons;
    using UnityEngine;

    /// <summary>
    /// Class to manage added network props.
    /// </summary>
    internal class AddedNetworkProps
    {
        // Dictionary of active additional props.
        private readonly Dictionary<NetInfo.Lane, NetLaneProps.Prop[]> _changedNetLanes;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddedNetworkProps"/> class.
        /// </summary>
        internal AddedNetworkProps()
        {
            // Init dictionary.
            _changedNetLanes = new Dictionary<NetInfo.Lane, NetLaneProps.Prop[]>();

            // Set instance reference.
            Instance = this;
        }

        /// <summary>
        /// Gets the active instance.
        /// </summary>
        internal static AddedNetworkProps Instance { get; private set; }

        /// <summary>
        /// Gets the config file list of network elements relevant to the current replacement type.
        /// </summary>
        private List<BOBConfig.NetworkElement> NetworkElementList => ConfigurationUtils.CurrentConfig.AddedNetworkProps;

        /// <summary>
        /// Checks whether or not the prop with a given lane and prop index is an added prop.
        /// </summary>
        /// <param name="netInfo">Network prefab to check.</param>
        /// <param name="lane">Lane index to check.</param>
        /// <param name="index">Prop index to check.</param>
        /// <returns>True if the prop is added, false if not.</returns>
        internal bool IsAdded(NetInfo netInfo, int lane, int index) => IsAdded(netInfo.m_lanes[lane], index);

        /// <summary>
        /// Checks whether or not the prop with a given lane and prop index is an added prop.
        /// </summary>
        /// <param name="lane">Lane prefab to check.</param>
        /// <param name="index">Prop index to check.</param>
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
            if (_changedNetLanes.TryGetValue(lane, out NetLaneProps.Prop[] originalProps))
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
        /// Retrieves a currently-applied replacement entry for the given network, lane and prop index.
        /// </summary>
        /// <param name="netInfo">Network prefab.</param>
        /// <param name="laneIndex">Lane number.</param>
        /// <param name="propIndex">Prop index number.</param>
        /// <returns>Currently-applied individual network replacement (null if none).</returns>
        internal BOBConfig.NetReplacement ReplacementRecord(NetInfo netInfo, int laneIndex, int propIndex) => ReplacementEntry(netInfo)?.Find(x => x.LaneIndex == laneIndex && x.PropIndex == propIndex);

        /// <summary>
        /// Adds a new prop to a network after updating the config file with the new entry.
        /// </summary>
        /// <param name="data">Network replacement data record.</param>
        internal void AddNew(BOBConfig.NetReplacement data)
        {
            // Local reference.
            NetInfo.Lane lane = data.NetInfo.m_lanes[data.LaneIndex];

            // Make sure lane isn't null.
            if (lane == null)
            {
                return;
            }

            // Add entry to configuration file.
            List<BOBConfig.NetReplacement> replacementList = ReplacementEntry(data.NetInfo);
            BOBConfig.NetReplacement replacementEntry = replacementList.Find(x => x.PropIndex == data.PropIndex);
            if (replacementEntry != null)
            {
                replacementList.Remove(replacementEntry);
            }

            replacementList.Add(data);

            // Add prop to lane.
            AddProp(data);
        }

        /// <summary>
        /// Removes the specified added network prop.
        /// </summary>
        /// <param name="network">Network prefab.</param>
        /// <param name="laneIndex">Lane index.</param>
        /// <param name="index">Prop index.</param>
        internal void RemoveNew(NetInfo network, int laneIndex, int index)
        {
            // Get lane reference.
            NetInfo.Lane lane = network.m_lanes[laneIndex];

            // Don't do anything if no valid network entry.
            if (_changedNetLanes.TryGetValue(lane, out NetLaneProps.Prop[] originalProps))
            {
                // Restore original prop list.
                lane.m_laneProps.m_props = originalProps;

                // Remove entry from list.
                List<BOBConfig.NetReplacement> replacementList = ReplacementEntry(network);
                if (replacementList != null && replacementList.Count > 0)
                {
                    // Found an entry for this network - try to find the matching prop index.
                    BOBConfig.NetReplacement targetEntry = replacementList.Find(x => x.PropIndex == index);
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
                        foreach (BOBConfig.NetReplacement replacement in replacementList)
                        {
                            if (replacement.PropIndex >= 0)
                            {
                                replacement.PropIndex = AddProp(replacement);
                            }
                        }
                    }
                    else
                    {
                        // No remaining added props - remove lane entry entirely.
                        _changedNetLanes.Remove(lane);
                    }
                }
            }
        }

        /// <summary>
        /// Updates an existing replacement entry.
        /// </summary>
        /// <param name="netInfo">Targeted network prefab.</param>
        /// <param name="targetInfo">Targeted (original) prop prefab.</param>
        /// <param name="replacementInfo">Replacment prop prefab.</param>
        /// <param name="laneIndex">Lane index to apply replacement to.</param>
        /// <param name="propIndex">Prop index to apply replacement to.</param>
        /// <param name="angle">Replacment prop angle adjustment.</param>
        /// <param name="offsetX">Replacment X position offset.</param>
        /// <param name="offsetY">Replacment Y position offset.</param>
        /// <param name="offsetZ">Replacment Z position offset.</param>
        /// <param name="probability">Replacement probability.</param>
        /// <param name="repeatDistance">Replacement repeat distance.</param>
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
            if (!IsAdded(lane, propIndex) || !_changedNetLanes.ContainsKey(lane))
            {
                Logging.Error("unrecorded reference passed to AddedNetworkProps.Update");
                return;
            }

            // Get network element list.
            List<BOBConfig.NetReplacement> replacementList = ReplacementEntry(netInfo);

            // Try to find matching entry.
            BOBConfig.NetReplacement thisReplacement = replacementList.Find(x => x.LaneIndex == laneIndex && x.PropIndex == propIndex);

            if (thisReplacement != null)
            {
                // Update replacment entry.
                thisReplacement.LaneIndex = laneIndex;
                thisReplacement.PropIndex = propIndex;
                thisReplacement.IsTree = targetInfo is TreeInfo;
                thisReplacement.Angle = angle;
                thisReplacement.OffsetX = offsetX;  // Use unmirrored X to save.
                thisReplacement.OffsetY = offsetY;
                thisReplacement.OffsetZ = offsetZ;
                thisReplacement.Probability = probability;
                thisReplacement.RepeatDistance = repeatDistance;

                // Record replacement prop.
                thisReplacement.ReplacementInfo = replacementInfo;
                thisReplacement.ReplacementName = replacementInfo.name;

                // Update handler.
                LanePropHandler thisHandler = NetHandlers.GetOrAddHandler(netInfo, netInfo.m_lanes[laneIndex], propIndex);
                thisHandler.SetReplacement(thisReplacement, ReplacementPriority.AddedReplacement);
            }
        }

        /// <summary>
        /// Reverts all added network props.
        /// </summary>
        internal void RevertAll()
        {
            // Iterate through each building in dictionary of added props.
            foreach (KeyValuePair<NetInfo.Lane, NetLaneProps.Prop[]> entry in _changedNetLanes)
            {
                // Restore original props.
                entry.Key.m_laneProps.m_props = entry.Value;
            }

            // Mark all entries in element list as dierty
            foreach (BOBConfig.NetworkElement element in NetworkElementList)
            {
                NetData.DirtyList.Add(element.NetInfo);
            }

            // Clear element list.
            NetworkElementList.Clear();
        }

        /// <summary>
        /// Deserialises and applies added props.
        /// </summary>
        /// <param name="elementList">Element list to deserialise.</param>
        internal void Deserialize(List<BOBConfig.NetworkElement> elementList)
        {
            // Iterate through each element in the provided list.
            foreach (BOBConfig.NetworkElement element in elementList)
            {
                // Try to find network prefab.
                element.Prefab = PrefabCollection<NetInfo>.FindLoaded(element.Network);

                // Don't bother deserializing further if the netInfo wasn't found.
                if (element.NetInfo != null)
                {
                    Deserialize(element.NetInfo, element.Replacements);
                }
            }
        }

        /// <summary>
        /// Adds a new tree or prop to a network prefab.
        /// </summary>
        private int AddProp(BOBConfig.NetReplacement replacement)
        {
            // Make sure a valid replacement prefab is set.
            NetInfo netInfo = replacement?.NetInfo;
            if (netInfo != null)
            {
                // New prop index.
                int newIndex = 0;

                // Check for valid lane index.
                if (replacement.LaneIndex < 0 || replacement.NetInfo.m_lanes == null)
                {
                    return -1;
                }

                // Lane reference.
                NetInfo.Lane lane = replacement.NetInfo.m_lanes[replacement.LaneIndex];

                // Make sure lane isn't null.
                if (lane == null)
                {
                    return -1;
                }

                // Create laneprops array if we need to.
                if (lane.m_laneProps == null)
                {
                    Logging.KeyMessage("creating new NetLaneProps for network ", replacement.NetInfo.name);
                    lane.m_laneProps = ScriptableObject.CreateInstance<NetLaneProps>();
                }

                // Add lane to changed lanes list, if it's not already there, recording original values..
                if (!_changedNetLanes.ContainsKey(lane))
                {
                    _changedNetLanes.Add(lane, lane.m_laneProps.m_props);
                }

                // Check to see if we've got a current prop array.
                if (lane.m_laneProps.m_props != null)
                {
                    // Existing m_props array - check that we've got space for another entry.
                    newIndex = lane.m_laneProps.m_props.Length;
                    if (newIndex > 63)
                    {
                        // Props maxed out - exit.
                        return -1;
                    }

                    // If this is a vanilla network, then we've probably got shared NetLaneProp references, so need to copy to a new instance.
                    // If the name doesn't contain a period (c.f. 12345.MyNetwok_Data), then assume it's vanilla - may be a mod or not shared, but better safe than sorry.
                    if (!netInfo.name.Contains("."))
                    {
                        NetData.CloneLanePropInstance(netInfo, replacement.LaneIndex);
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
                replacement.PropIndex = newIndex;


                // Add new prop - position and angle are at zero to start with as the 'original' coordinates.
                Logging.Message("adding new prop for network ", netInfo.name, " at lane ", replacement.LaneIndex, " and index ", newIndex);

                lane.m_laneProps.m_props[newIndex] = new NetLaneProps.Prop
                {
                    m_flagsRequired = NetLane.Flags.None,
                    m_flagsForbidden = NetLane.Flags.None,
                    m_startFlagsRequired = NetNode.Flags.None,
                    m_startFlagsForbidden = NetNode.Flags.None,
                    m_endFlagsRequired = NetNode.Flags.None,
                    m_endFlagsForbidden = NetNode.Flags.None,
                    m_colorMode = NetLaneProps.ColorMode.Default,
                    m_angle = 0,
                    m_prop = replacement.ReplacementInfo as PropInfo,
                    m_tree = replacement.ReplacementInfo as TreeInfo,
                    m_finalProp = replacement.ReplacementInfo as PropInfo,
                    m_finalTree = replacement.ReplacementInfo as TreeInfo,
                    m_segmentOffset = 0f,
                    m_repeatDistance = replacement.RepeatDistance,
                    m_minLength = 0f,
                    m_cornerAngle = 0f,
                    m_upgradable = false,
                    m_position = Vector3.zero,
                    m_probability = replacement.Probability,
                };

                // Ensure a handler is generated and add the replacement to it (this will update the prop and the renderer)Apol.
                NetHandlers.GetOrAddHandler(netInfo, lane, newIndex).SetReplacement(replacement, ReplacementPriority.AddedReplacement);

                return newIndex;
            }

            // If we got here, it didn't work; return -1.
            return -1;
        }

        /// <summary>
        /// Returns the configuration file record for the specified network prefab.
        /// </summary>
        /// <param name="netInfo">Network prefab.</param>
        /// <returns>Replacement record for the specified network prefab (null if none).</returns>
        private BOBConfig.NetworkElement NetworkElement(NetInfo netInfo) => netInfo == null ? null : NetworkElementList?.Find(x => x.NetInfo == netInfo);

        /// <summary>
        /// Gets the relevant network list entry from the active configuration file, creating a new network entry if none already exists.
        /// </summary>
        /// <param name="netInfo">Network prefab.</param>
        /// <returns>Replacement list for the specified network prefab.</returns>
        private List<BOBConfig.NetReplacement> ReplacementEntry(NetInfo netInfo)
        {
            // Get existing entry for this network.
            BOBConfig.NetworkElement thisNetwork = NetworkElement(netInfo);

            // If no existing entry, create a new one.
            if (thisNetwork == null)
            {
                thisNetwork = new BOBConfig.NetworkElement(netInfo);
                NetworkElementList.Add(thisNetwork);
            }

            // Return replacement list from this entry.
            return thisNetwork.Replacements;
        }

        /// <summary>
        /// Deserialises a replacement list.
        /// </summary>
        /// <param name="netInfo">Network prefab.</param>
        /// <param name="replacementList">Replacement list to deserialise.</param>
        private void Deserialize(NetInfo netInfo, List<BOBConfig.NetReplacement> replacementList)
        {
            // Iterate through each element in the provided list.
            foreach (BOBConfig.NetReplacement replacement in replacementList)
            {
                replacement.PropIndex = -1;

                try
                {
                    // Assign network info.
                    replacement.ParentInfo = netInfo;

                    // Try to find target prefab.
                    replacement.TargetInfo = replacement.IsTree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacement.Target) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacement.Target);

                    // Try to find replacement prefab.
                    replacement.ReplacementInfo = ConfigurationUtils.FindReplacementPrefab(replacement.ReplacementName, replacement.IsTree);

                    // Try to apply the replacement.
                    replacement.PropIndex = AddProp(replacement);
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