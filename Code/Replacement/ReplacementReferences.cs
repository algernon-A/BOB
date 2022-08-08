using UnityEngine;


namespace BOB
{

	/// <summary>
	/// Records original network prop data.
	/// </summary>
	public class NetPropReference : PropHandler
	{
		public NetInfo netInfo;
		public int laneIndex;
		public float angle, angleAdjustment;
		public float repeatDistance;

		/// <param name="propIndex">Prop index.</param>
		/// <param name="prop">Original m_prop value.</param>
		/// <param name="finalProp">Original m_finalProp value.</param>
		/// <param name="tree">Original m_tree value.</param>
		/// <param name="finalTree">Original m_finalTree value.</param>
		/// <param name="position">Original prop position</param>
		/// <param name="probability">Original prop probability.</param>
		public NetPropReference(int propIndex, PropInfo prop, PropInfo finalProp, TreeInfo tree, TreeInfo finalTree, Vector3 position, int probability)
			: base(propIndex, prop, finalProp, tree, finalTree, position, probability)
		{
		}
	}
}