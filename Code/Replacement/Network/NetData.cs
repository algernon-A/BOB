using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;


namespace BOB
{
    /// <summary>
    /// Class to handle centralised network data.
    /// </summary>
    internal static class NetData
    {
        // List of dirty net prefabs.
        private static HashSet<NetInfo> dirtyList;


        /// <summary>
        /// Dirty prefabs list.
        /// </summary>
        internal static HashSet<NetInfo> DirtyList
        {
            get
            {
                // Initialise list if it isn't already.
                if (dirtyList == null)
                {
                    dirtyList = new HashSet<NetInfo>();
                }

                return dirtyList;
            }
        }


        /// <summary>
        /// Refreshes network prefab renders for all 'dirty' networks and calls a recalculation of any Network Skins 2 skins.
        /// </summary>
        internal static void Update()
        {
            HashSet<KeyValuePair<int, int>> groupHash = new HashSet<KeyValuePair<int, int>>();

            // Local references.
            NetManager netManager = Singleton<NetManager>.instance;
            NetSegment[] segments = netManager.m_segments.m_buffer;
            NetNode[] nodes = netManager.m_nodes.m_buffer;

            // Need to do this for each segment instance, so iterate through all segments.
            for (ushort i = 0; i < segments.Length; ++i)
            {
				// Check that this is a valid network in the dirty list.
				if (segments[i].m_flags != NetSegment.Flags.None && DirtyList.Contains(segments[i].Info))
				{
                    // Caclulate segment render group.
                    ushort startNode = segments[i].m_startNode;
                    ushort endNode = segments[i].m_endNode;
                    Vector3 position = nodes[startNode].m_position;
                    Vector3 position2 = nodes[endNode].m_position;
                    Vector3 vector = (position + position2) * 0.5f;
                    int num = Mathf.Clamp((int)(vector.x / 64f + 135f), 0, 269);
                    int num2 = Mathf.Clamp((int)(vector.z / 64f + 135f), 0, 269);
                    int x = num * 45 / 270;
                    int z = num2 * 45 / 270;

                    // Add render group coordinates to hashlist (ignore if already there).
                    groupHash.Add(new KeyValuePair<int, int>(x, z));
                }
            }

            // Update render groups via simulation thread.
            Singleton<SimulationManager>.instance.AddAction(delegate
            {
                // Iterate through each key in group.
                foreach (KeyValuePair<int, int> keyPair in groupHash)
                {
                    // Update group render (all 32 layers, since we've got all kinds of mismatches with replacements).
                    for (int i = 0; i < 32; ++i)
                    {
                        Singleton<RenderManager>.instance.UpdateGroup(keyPair.Key, keyPair.Value, i);
                    }
                }
            });

            // Recalculate any Network Skins 2 applied skins.
            ModUtils.NS2Recalculate();

            // Clear dirty prefabs list.
            DirtyList.Clear();
        }
	}
}
