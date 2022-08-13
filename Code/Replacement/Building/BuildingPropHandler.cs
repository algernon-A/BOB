namespace BOB
{
	using UnityEngine;

	/// <summary>
	/// Handles an individual BuildingProp tree/prop record, including original values and any active replacements.
	/// </summary>
	public class BuildingPropHandler : PropHandler
	{
		// Parent building prefab.
		private BuildingInfo _buildingInfo;

		// Original prop data.
		private float _originalRadAngle;
		private bool _originalFixedHeight;

		// Active replacement references.
		private BOBBuildingReplacement _individualReplacement;
		private BOBBuildingReplacement _groupedReplacement;
		private BOBBuildingReplacement _allReplacement;

		/// <summary>
		/// Initializes a new instance of the <see cref="BuildingPropHandler"/> class.
		/// </summary>
		/// <param name="buildingInfo">Building prefab containing this prop (must not be null).</param>
		/// <param name="propIndex">Prop index.</param>
		/// <param name="buildingProp">Building prop instance (must not be null).</param>
		public BuildingPropHandler(BuildingInfo buildingInfo, int propIndex, BuildingInfo.Prop buildingProp)
			: base(
				  propIndex,
				  buildingProp.m_prop,
				  buildingProp.m_finalProp,
				  buildingProp.m_tree,
				  buildingProp.m_finalTree,
				  buildingProp.m_position,
				  buildingProp.m_probability)
		{
			// Set original data.
			_buildingInfo = buildingInfo;
			_originalRadAngle = buildingProp.m_radAngle;
			_originalFixedHeight = buildingProp.m_fixedHeight;
		}

		/// <summary>
		/// Gets the building prefab for this prop.
		/// </summary>
		public BuildingInfo BuildingInfo => _buildingInfo;

		/// <summary>
		/// Gets the original rotation angle in radians.
		/// </summary>
		public float OriginalRadAngle => _originalRadAngle;

		/// <summary>
		/// Gets a value indicating the original fixed height status.
		/// </summary>
		public bool OriginalFixedHeight => _originalFixedHeight;

		/// <summary>
		/// Gets the highest priority of any currently active replacements.
		/// </summary>
		public ReplacementPriority ActivePriority
		{
			get
			{
				// Check highest priorities first.
				if (_individualReplacement != null)
				{
					return ReplacementPriority.IndividualReplacement;
				}
				if (_groupedReplacement != null)
				{
					return ReplacementPriority.GroupedReplacement;
				}
				if (_allReplacement != null)
				{
					return ReplacementPriority.AllReplacement;
				}
				
				// If we got here, there's no elgibile active replacement.
				return ReplacementPriority.NoReplacement;
			}
		}

		/// <summary>
		/// Gets the highest-priority currently active replacement (null if none).
		/// </summary>
		public BOBBuildingReplacement ActiveReplacement
		{
			get
			{
				// Check highest priorities first.
				if (_individualReplacement != null)
				{
					return _individualReplacement;
				}
				if (_groupedReplacement != null)
				{
					return _groupedReplacement;
				}
				if (_allReplacement != null)
				{
					return _allReplacement;
				}

				// If we got here, there's no elgibile active replacement.
				return null;
			}
		}

		/// <summary>
		/// Gets the active replacement for the given priority for this prop (null if none).
		/// </summary>
		/// <param name="priority">Replacmeent priority to get.</param>
		/// <returns>Active bulding replacement for the given priority (null if none).</returns>
		public BOBBuildingReplacement GetReplacement(ReplacementPriority priority)
		{
			switch (priority)
			{
				case ReplacementPriority.IndividualReplacement:
					return _individualReplacement;
				case ReplacementPriority.GroupedReplacement:
					return _groupedReplacement;
				case ReplacementPriority.AllReplacement:
					return _allReplacement;
				default:
					Logging.Error("invalid priority ", priority, " passed to BuildingPropHandler.GetReplacement");
					return null;
			}
		}

		/// <summary>
		/// Sets the active replacement for the current priority for this prop.
		/// Automatically updates the target prop when active replacements change as a result of this action.
		/// </summary>
		/// <param name="replacement">Replacement to set (must not be null; use ClearReplacement instead).</param>
		/// <param name="priority">Priority to apply to.</param>
		public void SetReplacement(BOBBuildingReplacement replacement, ReplacementPriority priority)
		{
			// Null check.
			if (replacement == null)
			{
				Logging.Error("null replacement passed to BuildingPropHandler.SetReplacement; use ClearReplacement instead");
				return;
			}

			// Record the current replacement priority before action.
			ReplacementPriority originalPriority = ActivePriority;

			switch (priority)
			{
				case ReplacementPriority.IndividualReplacement:
					_individualReplacement = replacement;
					break;

				case ReplacementPriority.GroupedReplacement:
					_groupedReplacement = replacement;
					break;

				case ReplacementPriority.AllReplacement:
					_allReplacement = replacement;
					break;

				default:
					Logging.Error("invalid priority ", priority, " passed to BuildingPropHandler.SetReplacement");
					return;
			}

			// Check to see if the replacement priority was the same (i.e. update existing replacement) or greater than the original priority.
			// Can't use ActivePriority here as it won't catch any updates of existing replacements.
			if (priority >= originalPriority)
			{
				// Priority has changed; update this prop.
				UpdateProp();
			}
		}

		/// <summary>
		/// Clears all references (if any) to the given replacement (for all priorities).
		/// Automatically updates the target prop when active replacements change as a result of this action.
		/// </summary>
		/// <param name="replacement">Replacement reference to clear.</param>
		public void ClearReplacement(BOBBuildingReplacement replacement)
		{
			// Null check.
			if (replacement == null)
			{
				Logging.Error("null replacement passed to BuildingPropHandler.ClearReplacement");
				return;
			}

			// Record the current replacement priority before action.
			ReplacementPriority originalPriority = ActivePriority;

			// Clear any references to this replacement.
			if (_individualReplacement == replacement)
			{
				_individualReplacement = null;
			}
			if (_groupedReplacement == replacement)
			{
				_groupedReplacement = null;
			}
			if (_allReplacement == replacement)
			{
				_allReplacement = null;
			}

			// Check to see if the currently active priority has changed as a result of this action.
			if (originalPriority != ActivePriority)
			{
				// Priority has changed; update this prop.
				UpdateProp();
			}
		}

		/// <summary>
		/// Reverts this prop to original values.
		/// Replacement references will NOT be removed.
		/// </summary>
		public void RevertToOriginal()
		{
			// Local reference.
			BuildingInfo.Prop thisProp = _buildingInfo?.m_props?[PropIndex];
			if (thisProp != null)
			{
				thisProp.m_prop = OriginalProp;
				thisProp.m_finalProp = OriginalFinalProp;
				thisProp.m_tree = OriginalTree;
				thisProp.m_finalTree = OriginalFinalTree;
				thisProp.m_radAngle = _originalRadAngle;
				thisProp.m_fixedHeight = _originalFixedHeight;
				thisProp.m_position = OriginalPosition;
				thisProp.m_probability = OriginalProbability;

				// Update building.
				_buildingInfo.CheckReferences();
				BuildingData.DirtyList.Add(_buildingInfo);
			}
		}

		/// <summary>
		/// Applies the specified replacement as a temporary preview.
		/// </summary>
		/// <param name="previewReplacement">Replacement to preview.</param>
		public void PreviewReplacement(BOBBuildingReplacement previewReplacement) => ReplaceProp(previewReplacement);

		/// <summary>
		/// Clears any temporary preview, restoring the permanent underlying state.
		/// </summary>
		public void ClearPreview() => UpdateProp();

		/// <summary>
		/// Updates the target prop according current active replacement status.
		/// </summary>
		private void UpdateProp()
		{
			// Is there an active replacement for this reference?
			if (ActiveReplacement is BOBBuildingReplacement activeReplacement)
			{
				// Active replacement - replace prop accordingly.
				ReplaceProp(activeReplacement);
			}
			else
			{
				// No active replacement - restore original values.
				RevertToOriginal();
			}
		}

		/// <summary>
		/// Replaces the target prop using the specified building replacement.
		/// </summary>
		/// <param name="replacement">Building replacement element to apply.</param>
		private void ReplaceProp(BOBBuildingReplacement replacement)
		{
			// Convert offset to Vector3.
			Vector3 offset = new Vector3
			{
				x = replacement.offsetX,
				y = replacement.offsetY,
				z = replacement.offsetZ
			};

			// Apply replacement.
			_buildingInfo.m_props[PropIndex].m_prop = replacement.ReplacementProp;
			_buildingInfo.m_props[PropIndex].m_finalProp = replacement.ReplacementProp;
			_buildingInfo.m_props[PropIndex].m_tree = replacement.ReplacementTree;
			_buildingInfo.m_props[PropIndex].m_finalTree = replacement.ReplacementTree;

			// Set m_fixedHeight.
			_buildingInfo.m_props[PropIndex].m_fixedHeight = replacement.customHeight;

			// Angle and offset.
			_buildingInfo.m_props[PropIndex].m_radAngle = OriginalRadAngle + (replacement.angle * Mathf.Deg2Rad);
			_buildingInfo.m_props[PropIndex].m_position = OriginalPosition + offset;

			// Probability.
			_buildingInfo.m_props[PropIndex].m_probability = replacement.probability;

			// Update building prop references.
			_buildingInfo.CheckReferences();

			// Add building to dirty list.
			BuildingData.DirtyList.Add(_buildingInfo);
		}
	}
}