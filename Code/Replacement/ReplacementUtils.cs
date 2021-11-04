using System;


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
			Logging.KeyMessage("reverting all replacements");

			// Revert all-building replacements.
			try
			{
				AllBuildingReplacement.Instance?.RevertAll();
			}
			catch (Exception e)
			{
				// Don't let a single failure stop us.
				Logging.LogException(e, "exception reverting all-building replacements");
			}

			// Revert building replacements.
			try
			{
				BuildingReplacement.Instance?.RevertAll();
			}
			catch (Exception e)
			{
				// Don't let a single failure stop us.
				Logging.LogException(e, "exception reverting building replacements");
			}

			// Revert individual building replacements.
			try
			{
				IndividualBuildingReplacement.Instance?.RevertAll();
			}
			catch (Exception e)
			{
				// Don't let a single failure stop us.
				Logging.LogException(e, "exception reverting individual building replacements");
			}

			// Revert network pack replacements.
			try
			{
				NetworkPackReplacement.Instance?.RevertAll();
			}
			catch (Exception e)
			{
				// Don't let a single failure stop us.
				Logging.LogException(e, "exception reverting network pack replacements");
			}

			// Revert all-network replacements.
			try
			{
				AllNetworkReplacement.Instance?.RevertAll();
			}
			catch (Exception e)
			{
				// Don't let a single failure stop us.
				Logging.LogException(e, "exception reverting all-network replacements");
			}
			
			// Revert network replacements.
			try
			{
				NetworkReplacement.Instance?.RevertAll();
			}
			catch (Exception e)
			{
				// Don't let a single failure stop us.
				Logging.LogException(e, "exception reverting network replacements");
			}

			// Revert individual network replacements.
			try
			{
				IndividualNetworkReplacement.Instance?.RevertAll();
			}
			catch (Exception e)
			{
				// Don't let a single failure stop us.
				Logging.LogException(e, "exception reverting individual network replacements");
			}
			
			// Revert scaling.
			try
			{
				Scaling.Instance?.RevertAll();
			}
			catch (Exception e)
			{
				// Don't let a single failure stop us.
				Logging.LogException(e, "exception reverting scaling elemennts");
			}

			// Regenerate dirty renders.
			NetData.Update();
			BuildingData.Update();
		}
	}
}
