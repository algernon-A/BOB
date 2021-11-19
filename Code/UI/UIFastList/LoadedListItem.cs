namespace BOB
{
	/// <summary>
	///  Data record for UI loaded prefab list item, showing creator name.
	/// </summary>
	public class LoadedListItem
	{
		// Prefab record.
		public PrefabInfo thisPrefab;

		// Display name.
		public string displayName;

		// Creator name.
		public string creatorName;


		/// <summary>
		/// Constructor - automatically sets values based on provided network prefab.
		/// </summary>
		/// <param name="network">Network prefab</param>
		public LoadedListItem(PrefabInfo prefab)
		{
			thisPrefab = prefab;
			displayName = PrefabLists.GetDisplayName(prefab);
			creatorName = PrefabLists.GetCreator(prefab);
		}
	}
}