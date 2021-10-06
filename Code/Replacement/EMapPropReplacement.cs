using ColossalFramework;
using EManagersLib.API; // *EML*


namespace BOB
{
	/// <summary>
	/// Class to manage map prop replacements, using Extended Manager Library's EPropManager.
	/// </summary>
	internal class EMapPropReplacement : MapPropReplacement
	{
		/// <summary>
		/// Replaces a map prop.
		/// </summary>
		/// <param name="target">Prop to replace</param>
		/// <param name="replacement">Replacement prop</param>
		protected override void ReplaceProps(PropInfo target, PropInfo replacement)
		{
			// Check for valid parameters.
			if (target != null && replacement != null)
			{
				// Local reference.
				EPropInstance[] props = EPropManager.m_props.m_buffer;

				// Iterate through each prop in map.
				for (uint propIndex = 0; propIndex < props.Length; ++propIndex)
				{
					// Local reference.
					EPropInstance prop = props[propIndex];

					// Skip non-existent trees (those with no flags).
					if (prop.m_flags == (ushort)PropInstance.Flags.None)
					{
						continue;
					}

					// If tree matches, replace!
					if (prop.Info == target)
					{
						props[propIndex].Info = replacement;

						// Refresh prop render (to update LOD).
						EPropManager.UpdatePropRenderer(null, (ushort)propIndex, true);
					}
				}
			}
		}
	}

}