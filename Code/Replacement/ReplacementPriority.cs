namespace BOB
{
	/// <summary>
	/// Replacement priority.
	/// Higher values have higher priority.
	/// </summary>
	public enum ReplacementPriority : byte
	{
		/// <summary>
		/// No replacement is currently applied.
		/// </summary>
		NoReplacement = 0,

		/// <summary>
		/// Replacement packs.
		/// </summary>
		PackReplacement,

		/// <summary>
		/// All- replacements.
		/// </summary>
		AllReplacement,

		/// <summary>
		/// Grouped replacements.
		/// </summary>
		GroupedReplacement,

		/// <summary>
		/// Individual replacements.
		/// </summary>
		IndividualReplacement,
	}
}