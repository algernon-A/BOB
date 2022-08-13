namespace BOB
{
    using System.Collections.Generic;
	using AlgernonCommons;

	/// <summary>
	/// Central coordination and tracking of active building replacmenets.
	/// </summary>
    internal static class BuildingHandlers
    {
		// The master dicationary of all building props being handled.
		private static readonly Dictionary<BuildingInfo, Dictionary<int, BuildingPropHandler>> Handlers = new Dictionary<BuildingInfo, Dictionary<int, BuildingPropHandler>>();

		/// <summary>
		/// Gets a reference to the master dictionary (for debugging purposes).
		/// DO NOT USE to access the dictionary directly; this is simply for visibility via ModTools.
		/// </summary>
		internal static Dictionary<BuildingInfo, Dictionary<int, BuildingPropHandler>> GetDict => Handlers;

		/// <summary>
		/// Gets any existing prop handler for the given parameters, creating a new one if one doesn't already exist.
		/// </summary>
		/// <param name="buildingInfo">Building prefab for this prop.</param>
		/// <param name="propIndex">BuildingProp prop index.</param>
		/// <returns>Prop handler for this prop (creating a new handler if required).</returns>
		internal static BuildingPropHandler GetOrAddHandler(BuildingInfo buildingInfo, int propIndex)
		{
			// Check for an existing building entry.
			if (!Handlers.TryGetValue(buildingInfo, out Dictionary<int, BuildingPropHandler> buildingDict))
			{
				// No building entry exists; add one.
				buildingDict = new Dictionary<int, BuildingPropHandler>();
				Handlers.Add(buildingInfo, buildingDict);
			}

			// Check for existing prop index reference.
			if (!buildingDict.TryGetValue(propIndex, out BuildingPropHandler propHandler))
			{
				// No existing handler for this prop index - create a new one and add to the dictionary.
				propHandler = CreateHandler(buildingInfo, propIndex);
				buildingDict.Add(propIndex, propHandler);
			}

			return propHandler;
		}

		/// <summary>
		/// Gets any existing prop handler for the given parameters, returning null if one doesn't already exist.
		/// </summary>
		/// <param name="buildingInfo">Building prefab for this prop.</param>
		/// <param name="propIndex">BuildingProp prop index.</param>
		/// <returns>Prop handler (null if none).</returns>
		internal static BuildingPropHandler GetHandler(BuildingInfo buildingInfo, int propIndex)
		{
			// Check for an existing building entry.
			if (Handlers.TryGetValue(buildingInfo, out Dictionary<int, BuildingPropHandler> buildingDict))
			{
				// Entry found; check for and existing prop index reference.
				if (buildingDict.TryGetValue(propIndex, out BuildingPropHandler propHandler))
				{
					// Found a matching entry; return it.
					return propHandler;
				}
			}

			// If we got here, no matching entry was found; return null.
			return null;
		}

		/// <summary>
		/// Returns the replacement of the given priority for the given parameters, if any.
		/// </summary>
		/// <param name="buildingInfo">Building prefab for this prop.</param>
		/// <param name="propIndex">BuildingProp prop index.</param>
		/// <param name="priority">Replacement priority to return.</param>
		/// <returns>Specified building replacment entry (null if none).</returns>
		internal static BOBBuildingReplacement GetReplacement(BuildingInfo buildingInfo, int propIndex, ReplacementPriority priority) => GetHandler(buildingInfo, propIndex)?.GetReplacement(priority);

		/// <summary>
		/// Removes the given replacement from all existing handlers.
		/// Automatically updates any target props whose active replacements change as a result.
		/// </summary>
		/// <param name="replacement">Replacement to remove.</param>
		internal static void RemoveReplacement(BOBBuildingReplacement replacement)
		{
			// Iterate through all references in the dictionary.
			foreach (KeyValuePair<BuildingInfo, Dictionary<int, BuildingPropHandler>> buildingEntry in Handlers)
			{
				foreach (KeyValuePair<int, BuildingPropHandler> entry in buildingEntry.Value)
				{
					// Clear any of this replacement contained in this reference. 
					entry.Value.ClearReplacement(replacement);
				}
			}
		}

		/// <summary>
		/// Creates a new BuildingPropHandler from the provided building prefab and prop index.
		/// </summary>
		/// <param name="buildingInfo">Building prefab for this prop.</param>
		/// <param name="propIndex">BuildingProp prop index.</param>
		/// <returns>Newly-created reference (null if creation failed).</returns>
		internal static BuildingPropHandler CreateHandler(BuildingInfo buildingInfo, int propIndex)
		{
			// Safety checks to ensure prop reference is valid.
			BuildingInfo.Prop[] props = buildingInfo?.m_props;
			if (props != null && propIndex >= 0 && propIndex < props.Length)
			{
				// Check that actual prop value isn't null.
				BuildingInfo.Prop prop = props[propIndex];
				if (prop != null)
				{
					// Create and return new reference, recording original values.
					return new BuildingPropHandler(buildingInfo, propIndex, prop);
				}
			}

			// If we got here, something went wrong; return null.
			Logging.Error("invalid argument passed to BuildingReplacementBase.CreateReference");
			return null;
		}
	}
}
