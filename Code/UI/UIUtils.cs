using UnityEngine;
using ColossalFramework.UI;


namespace BOB
{
	/// <summary>
	/// Static class for UI utilities.
	/// </summary>
    internal static class UIUtils
    {
        // Package icon texture atlas.
        private static UITextureAtlas packageSprites;

        internal static UITextureAtlas PackageSprites
        {
            get
            {
                if (packageSprites == null)
                {
                    packageSprites = FileUtils.LoadSpriteAtlas("bob_prop_pack_small");
                }

                return packageSprites;
            }
        }


        /// <summary>
        /// Sanitises a raw prefab name for display.
        /// Called by the settings panel fastlist.
        /// </summary>
        /// <param name="fullName">Original (raw) prefab name</param>
        /// <returns>Cleaned display name</returns>
        internal static string GetDisplayName(string fullName)
		{
			// Find any leading period (Steam package number).
			int num = fullName.IndexOf('.');

			// If no period, assume vanilla asset; return full name preceeded by vanilla flag.
			if (num < 0)
			{
				return "[v] " + fullName;
			}

			// Otherwise, omit the package number, and trim off any trailing _Data.
			return fullName.Substring(num + 1).Replace("_Data", "");
		}
	}
}