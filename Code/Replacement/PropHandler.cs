﻿namespace BOB
{
	using UnityEngine;

	/// <summary>
	/// Handles an individual tree/prop record, including original values and any active replacements.
	/// </summary>
	public abstract class PropHandler
	{
		/// <summary>
		/// Stores any position adjustment required for previewing.
		/// </summary>
		public Vector3 Adjustment;

		// The prop index number.
		private int _propIndex;

		// Original prop data.
		private PropInfo _originalProp;
		private PropInfo _originalFinalProp;
		private TreeInfo _originalTree;
		private TreeInfo _originalFinalTree;
		private Vector3 _originalPosition;
		private int _originalProbability;

		/// <summary>
		/// Initializes a new instance of the <see cref="PropHandler"/> class.
		/// </summary>
		/// <param name="propIndex">Prop index.</param>
		/// <param name="prop">Original m_prop value.</param>
		/// <param name="finalProp">Original m_finalProp value.</param>
		/// <param name="tree">Original m_tree value.</param>
		/// <param name="finalTree">Original m_finalTree value.</param>
		/// <param name="position">Original prop position</param>
		/// <param name="probability">Original prop probability.</param>
		public PropHandler(int propIndex, PropInfo prop, PropInfo finalProp, TreeInfo tree, TreeInfo finalTree, Vector3 position, int probability)
		{
			// Set original data.
			_propIndex = propIndex;
			_originalProp = prop;
			_originalFinalProp = finalProp;
			_originalTree = tree;
			_originalFinalTree = finalTree;
			_originalPosition = position;
			_originalProbability = probability;
		}

		/// <summary>
		/// Gets the prop index for this reference.
		/// </summary>
		public int PropIndex => _propIndex;

		/// <summary>
		/// Gets the original target prefab info (either prop or tree).
		/// </summary>
		public PrefabInfo OriginalPrefab => (PrefabInfo)_originalProp ?? _originalTree;

		/// <summary>
		/// Gets the original m_prop prop prefab.
		/// </summary>
		public PropInfo OriginalProp => _originalProp;

		/// <summary>
		/// Gets the original m_finalProp prop prefab.
		/// </summary>
		public PropInfo OriginalFinalProp => _originalFinalProp;

		/// <summary>
		/// Gets the original m_tree tree prefab.
		/// </summary>
		public TreeInfo OriginalTree => _originalTree;

		/// <summary>
		/// Gets the original m_finalTree tree prefab.
		/// </summary>
		public TreeInfo OriginalFinalTree => _originalFinalTree;

		/// <summary>
		/// Gets the original position.
		/// </summary>
		public Vector3 OriginalPosition => _originalPosition;

		/// <summary>
		/// Gets the original probability.
		/// </summary>
		public int OriginalProbability => _originalProbability;
	}
}