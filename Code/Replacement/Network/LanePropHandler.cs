namespace BOB
{
	using AlgernonCommons;
	using UnityEngine;

	/// <summary>
	/// Handles an individual BuildingProp tree/prop record, including original values and any active replacements.
	/// </summary>
	public class LanePropHandler : PropHandler
	{
		// Parent network prefab.
		private NetInfo _netInfo;

		// Parent lane prefab.
		private NetInfo.Lane _laneInfo;

		// Original prop data.
		private float _originalAngle;
		private float _originalRepeatDistance;

		// Active replacement references.
		private BOBNetReplacement _individualReplacement;
		private BOBNetReplacement _groupedReplacement;
		private BOBNetReplacement _allReplacement;
		private BOBNetReplacement _packReplacement;

		/// <summary>
		/// Initializes a new instance of the <see cref="NetowrkPropHandler"/> class.
		/// </summary>
		/// <param name="prefab">Netwok prefab containing this prop (must not be null).</param>
		/// <param name="laneInfo">Network lane containing this prop (must not be null).</param>
		/// <param name="propIndex">Prop index.</param>
		/// <param name="buildingProp">Building prop instance (must not be null).</param>
		public LanePropHandler(NetInfo prefab, NetInfo.Lane laneInfo, int propIndex, NetLaneProps.Prop laneProp)
			: base(
				  propIndex,
				  laneProp.m_prop,
				  laneProp.m_finalProp,
				  laneProp.m_tree,
				  laneProp.m_finalTree,
				  laneProp.m_position,
				  laneProp.m_probability)
		{
			// Set original data.
			_netInfo = prefab;
			_laneInfo = laneInfo;
			_originalAngle = laneProp.m_angle;
			_originalRepeatDistance = laneProp.m_repeatDistance;
		}

		/// <summary>
		/// Gets the network prefab for this prop.
		/// </summary>
		public NetInfo NetInfo => _netInfo;

		/// <summary>
		/// Gets the lane info for this prop.
		/// </summary>
		public NetInfo.Lane LaneInfo => _laneInfo;

		/// <summary>
		/// Gets the original rotation angle in degrees.
		/// </summary>
		public float OriginalAngle => _originalAngle;

		/// <summary>
		/// Gets the original repeat distance.
		/// </summary>
		public float OriginalRepeatDistance => _originalRepeatDistance;

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
				if (_packReplacement != null)
				{
					return ReplacementPriority.PackReplacement;
				}

				// If we got here, there's no elgibile active replacement.
				return ReplacementPriority.NoReplacement;
			}
		}

		/// <summary>
		/// Gets the highest-priority currently active replacement (null if none).
		/// </summary>
		public BOBNetReplacement ActiveReplacement
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
				if (_packReplacement != null)
				{
					return _packReplacement;
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
		public BOBNetReplacement GetReplacement(ReplacementPriority priority)
		{
			switch (priority)
			{
				case ReplacementPriority.IndividualReplacement:
					return _individualReplacement;
				case ReplacementPriority.GroupedReplacement:
					return _groupedReplacement;
				case ReplacementPriority.AllReplacement:
					return _allReplacement;
				case ReplacementPriority.PackReplacement:
					return _packReplacement;
				default:
					Logging.Error("invalid priority ", priority, " passed to LanePropHandler.GetReplacement");
					return null;
			}
		}

		/// <summary>
		/// Sets the active replacement for the current priority for this prop.
		/// Automatically updates the target prop when active replacements change as a result of this action.
		/// </summary>
		/// <param name="replacement">Replacement to set (must not be null; use ClearReplacement instead).</param>
		/// <param name="priority">Priority to apply to.</param>
		public void SetReplacement(BOBNetReplacement replacement, ReplacementPriority priority)
		{
			// Null check.
			if (replacement == null)
			{
				Logging.Error("null replacement passed to LanePropHandler.SetReplacement; use ClearReplacement instead");
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

				case ReplacementPriority.PackReplacement:
					_packReplacement = replacement;
					break;

				default:
					Logging.Error("invalid priority ", priority, " passed to LanePropHandler.SetReplacement");
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
		public void ClearReplacement(BOBNetReplacement replacement)
		{
			// Null check.
			if (replacement == null)
			{
				Logging.Error("null replacement passed to LanePropHandler.ClearReplacement");
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
			if (_packReplacement == replacement)
			{
				_packReplacement = null;
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
			NetLaneProps.Prop thisProp = _laneInfo?.m_laneProps?.m_props[PropIndex];
			if (thisProp != null)
			{
				thisProp.m_prop = OriginalProp;
				thisProp.m_finalProp = OriginalFinalProp;
				thisProp.m_tree = OriginalTree;
				thisProp.m_finalTree = OriginalFinalTree;
				thisProp.m_angle = _originalAngle;
				thisProp.m_position = OriginalPosition;
				thisProp.m_probability = OriginalProbability;
				thisProp.m_repeatDistance = _originalRepeatDistance;

				// Update network.
				_netInfo.CheckReferences();
				NetData.DirtyList.Add(_netInfo);
			}
		}

		/// <summary>
		/// Applies the specified replacement as a temporary preview.
		/// </summary>
		/// <param name="previewReplacement">Replacement to preview.</param>
		public void PreviewReplacement(BOBNetReplacement previewReplacement) => ReplaceProp(previewReplacement);

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
			if (ActiveReplacement is BOBNetReplacement activeReplacement)
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
		/// <param name="replacement">Network replacement element to apply.</param>
		private void ReplaceProp(BOBNetReplacement replacement)
		{
			// Convert offset to Vector3.
			Vector3 offset = new Vector3
			{
				x = replacement.offsetX,
				y = replacement.offsetY,
				z = replacement.offsetZ
			};

			// Apply replacement.
			_laneInfo.m_laneProps.m_props[PropIndex].m_prop = replacement.ReplacementProp;
			_laneInfo.m_laneProps.m_props[PropIndex].m_finalProp = replacement.ReplacementProp;
			_laneInfo.m_laneProps.m_props[PropIndex].m_tree = replacement.ReplacementTree;
			_laneInfo.m_laneProps.m_props[PropIndex].m_finalTree = replacement.ReplacementTree;

			// Invert x offset and angle to match original prop x position.
			float angleMult = 1f;
			if (_laneInfo.m_position + OriginalPosition.x < 0)
			{
				offset.x = 0 - offset.x;
				angleMult = -1;
			}

			// Angle and offset.
			_laneInfo.m_laneProps.m_props[PropIndex].m_angle = OriginalAngle + (replacement.angle * angleMult);
			_laneInfo.m_laneProps.m_props[PropIndex].m_position = OriginalPosition + offset;

			// Probability.
			_laneInfo.m_laneProps.m_props[PropIndex].m_probability = replacement.probability;

			// Repeat distance, if a valid value is set.
			if (replacement.repeatDistance > 1)
			{
				_laneInfo.m_laneProps.m_props[PropIndex].m_repeatDistance = replacement.repeatDistance;
			}

			// Update network prop references.
			_netInfo.CheckReferences();

			// Add building to dirty list.
			NetData.DirtyList.Add(_netInfo);
		}
	}
}