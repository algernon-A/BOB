using System.Collections.Generic;
using ColossalFramework;


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
            // Need to do this for each segment instance, so iterate through all segments.
            NetSegment[] segments = NetManager.instance.m_segments.m_buffer;
            for (ushort i = 0; i < segments.Length; ++i)
            {
                // Local reference.
                NetSegment segment = segments[i];

                // Check that this is a valid network in the dirty list.
                if (segment.m_flags != NetSegment.Flags.None && DirtyList.Contains(segment.Info))
                {
                    // Match - update segment render via simulation thread, creating local buildingID reference to avoid race condition.
                    ushort segmentID = i;
                    Singleton<SimulationManager>.instance.AddAction(delegate { Singleton<NetManager>.instance.UpdateSegmentRenderer(segmentID, true); });
                }
            }

            // Recalculate any Network Skins 2 applied skins.
            ModUtils.NS2Recalculate();

            // Clear dirty prefabs list.
            DirtyList.Clear();
        }
    }
}
