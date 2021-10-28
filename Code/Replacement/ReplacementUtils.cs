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
			AllBuildingReplacement.Instance?.RevertAll();
			BuildingReplacement.Instance?.RevertAll();
			IndividualBuildingReplacement.Instance?.RevertAll();
			NetworkPackReplacement.Instance?.RevertAll();
			AllNetworkReplacement.Instance?.RevertAll();
			NetworkReplacement.Instance?.RevertAll();
			Scaling.instance?.RevertAll();
		}
	}
}
