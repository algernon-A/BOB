using UnityEngine;


namespace BOB
{
	// Records original prop/tree data.
	public class PropReference
    {
		public PropInfo originalProp, originalFinalProp;
		public TreeInfo originalTree, originalFinalTree;
		public bool isTree;
		public int propIndex;
		public Vector3 position, adjustment;
		public int probability;


		/// <summary>
		/// Returns original final prefab info (tree or prop).
		/// </summary>
		public PrefabInfo OriginalInfo => isTree ? (PrefabInfo)originalTree : originalProp;
    }


	/// <summary>
	/// Records original building prop data.
	/// </summary>
	public class BuildingPropReference : PropReference
	{
		public BuildingInfo buildingInfo;
		public float radAngle, radAngleAdjustment;
		public bool fixedHeight;
	}


	/// <summary>
	/// Records original network prop data.
	/// </summary>
	public class NetPropReference : PropReference
	{
		public NetInfo netInfo;
		public int laneIndex;
		public float angle, angleAdjustment;
		public float repeatDistance;
	}
}