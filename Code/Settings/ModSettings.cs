﻿namespace BOB
{
	/// <summary>
	/// Static class to hold global mod settings.
	/// </summary>
	internal static class ModSettings
	{
		// Default behaviour of the show individual props setting.
		internal static int indDefault;

		// Last selected indivisual setting.
		internal static bool lastInd;

		// Last selected vanilla filter state.
		internal static bool hideVanilla = false;

		// Last selected tree-or-prop state.
		internal static bool treeSelected = false;

		// Remember last panel position.
		internal static bool rememberPosition = false;

		// What's new notification version.
		internal static string whatsNewVersion = "0.0";
	}
}