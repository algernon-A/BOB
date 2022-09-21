// <copyright file="ReplacementUtils.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System;
    using AlgernonCommons;

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
                GroupedBuildingReplacement.Instance?.RevertAll();
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
                GroupedNetworkReplacement.Instance?.RevertAll();
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

            // Revert added building props.
            try
            {
                AddedBuildingProps.Instance?.RevertAll();
            }
            catch (Exception e)
            {
                // Don't let a single failure stop us.
                Logging.LogException(e, "exception reverting added building props");
            }

            // Revert added network props.
            try
            {
                AddedNetworkProps.Instance?.RevertAll();
            }
            catch (Exception e)
            {
                // Don't let a single failure stop us.
                Logging.LogException(e, "exception reverting added building props");
            }

            // Regenerate dirty renders.
            NetData.Update();
            BuildingData.Update();
        }
    }
}
