namespace BOB
{
	/// <summary>
	/// Static class to hold global mod settings.
	/// </summary>
	internal static class ModSettings
	{
		// Default behaviour of the group setting.
		internal static int groupDefault;

		// Last selected group.
		internal static bool lastGroup;

		// Last selected vanilla filter state.
		internal static bool hideVanilla = false;

		// Last selected tree-or-prop state.
		internal static bool treeSelected = false;
	}
}