using UnityEngine;


namespace BOB
{
	/// <summary>
	/// Static class for replacment record management utilities.
	/// </summary>
	internal static class ReplacementUtils
	{
		/// <summary>
		/// Creates a clone of the given replacement record.
		/// </summary>
		/// <param name="replacement">Record to clone</param>
		/// <returns>Clone of the original record</returns>
		internal static Replacement Clone(Replacement replacement)
		{
			// Create new record instance.
			Replacement clone = new Replacement
			{
				// Copy original records to the clone.
				isTree = replacement.isTree,
				targetIndex = replacement.targetIndex,
				targetName = replacement.targetName,
				replaceName = replacement.replaceName,
				probability = replacement.probability,
				angle = replacement.angle,
				replacementInfo = replacement.replacementInfo,
				targetInfo = replacement.targetInfo
			};

			return clone;
		}
	}
}
