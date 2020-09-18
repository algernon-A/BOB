using System.Collections.Generic;
using System.IO;
using System.Linq;
using ColossalFramework;
using ColossalFramework.Plugins;
using ICities;
using UnityEngine;


namespace BOB
{
	/// <summary>
	/// Static class for file management utilities.
	/// </summary>
	internal static class FileUtils
	{
		/// <summary>
		/// Returns the filepath of the mod assembly.
		/// </summary>
		/// <returns>Mod assembly filepath</returns>
		internal static string GetAssemblyPath()
		{
			// Get list of currently active plugins.
			IEnumerable<PluginManager.PluginInfo> plugins = PluginManager.instance.GetPluginsInfo();

			// Iterate through list.
			foreach (PluginManager.PluginInfo plugin in plugins)
			{
				try
				{
					// Get all (if any) mod instances from this plugin.
					IUserMod[] mods = plugin.GetInstances<IUserMod>();

					// Check to see if the primary instance is this mod.
					if (mods.FirstOrDefault() is BOBMod)
					{
						// Found it! Return path.
						return plugin.modPath;
					}
				}
				catch
				{
					// Don't care.
				}
			}

			// If we got here, then we didn't find the assembly.
			Debugging.Message("assembly path not found");
			throw new FileNotFoundException(BOBMod.ModName + ": assembly path not found!");
		}


		/// <summary>
		/// Loads a cursor texture.
		/// </summary>
		/// <param name="cursorName">Cursor texture file name</param>
		/// <returns>New cursor</returns>
		internal static CursorInfo LoadCursor(string cursorName)
		{
			CursorInfo cursor = ScriptableObject.CreateInstance<CursorInfo>();


			cursor.m_texture = LoadTexture(cursorName);
			cursor.m_hotspot = new Vector2(5f, 0f);

			return cursor;
		}


		/// <summary>
		/// Loads a 2D texture from file.
		/// </summary>
		/// <param name="fileName">Texture file to load</param>
		/// <returns>New 2D texture</returns>
		private static Texture2D LoadTexture(string fileName)
		{
			using (Stream stream = OpenResourceFile(fileName))
			{
				// New texture.
				Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
				texture.filterMode = FilterMode.Bilinear;
				texture.wrapMode = TextureWrapMode.Clamp;

				// Read texture as byte stream from file.
				byte[] array = new byte[stream.Length];
				stream.Read(array, 0, array.Length);
				texture.LoadImage(array);
				texture.Apply();

				return texture;
			}
		}


		/// <summary>
		/// Opens the named resource file for reading.
		/// </summary>
		/// <param name="fileName">File to open</param>
		/// <returns>Read-only file stream</returns>
		private static Stream OpenResourceFile(string fileName)
		{
			string path = Path.Combine(GetAssemblyPath(), "Resources");
			return File.OpenRead(Path.Combine(path, fileName));
		}
	}
}
