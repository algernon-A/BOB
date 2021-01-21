namespace BOB
{
    /// <summary>
    /// Static class to manage replacement packs.
    /// </summary>
    internal static class ReplacementPacks
	{
		/// <summary>
		/// Checks if there's a currently active individual package replacement applied to the given prop, and if so, returns the *original* prefab.
		/// </summary>
		/// <param name="currentProp">Prop/tree to check</param>
		/// <returns>Original prefab if package replacement is currently applied, null if no package replacement is currently applied</returns>
		internal static PrefabInfo GetOriginal(PrefabInfo currentPrefab)
		{
			// TODO: nothing here yet!

			// If we got here, no entry was found - return null to indicate no active replacement.
			return null;
		}
	}
}