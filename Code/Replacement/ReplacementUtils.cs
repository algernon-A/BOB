using UnityEngine;


namespace BOB
{
	/// <summary>
	/// Static class for replacment record management utilities.
	/// </summary>
	internal static class ReplacementUtils
	{
		/// <summary>
		/// Reverts all current settings and clears replacement dictionaries.
		/// </summary>
		internal static void NukeSettings()
        {
			// Revert all-building and building settings (assuming instances exist).
			AllBuildingReplacement.instance?.RevertAll();
			BuildingReplacement.instance?.RevertAll();
			IndividualReplacement.instance?.RevertAll();
			PackReplacement.instance?.RevertAll();
			AllNetworkReplacement.instance?.RevertAll();
			NetworkReplacement.instance?.RevertAll();
			Scaling.instance?.RevertAll();
		}
	}
}
