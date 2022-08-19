// <copyright file="PackUtils.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Serialization;
    using AlgernonCommons;
    using ColossalFramework.Packaging;

    /// <summary>
    /// Static utility class for managing prop replacement pack files.
    /// </summary>
    public static class PackUtils
    {
        // Replacement pack directory.
        private static readonly string PackDirectory = Path.Combine(AssemblyUtils.AssemblyPath, "ReplacementPacks");

        /// <summary>
        /// Finds and loads XML replacement pack files.
        /// </summary>
        /// <returns>Loaded XML configuration file instance (null if failed).</returns>
        internal static List<BOBPackFile> LoadPackFiles()
        {
            try
            {
                // Return list.
                List<BOBPackFile> fileList = new List<BOBPackFile>();

                // Hashlist for parsed directories.
                HashSet<string> parsedDirectories = new HashSet<string>();

                // Iterate through each xml file in directory.
                string[] fileNames = Directory.GetFiles(PackDirectory, "*.xml", SearchOption.TopDirectoryOnly);

                foreach (string fileName in fileNames)
                {
                    ReadPackFile(fileName, fileList);
                }

                // Iterate through prop asset directories looking for BOB config files.
                for (uint i = 0; i < PrefabCollection<PropInfo>.LoadedCount(); ++i)
                {
                    // Get prefab.
                    PropInfo prop = PrefabCollection<PropInfo>.GetLoaded(i);

                    // Skip invalid prefabs.
                    if (prop == null)
                    {
                        continue;
                    }

                    // Try to find filesystem path for this asset.
                    Package.Asset asset = PackageManager.FindAssetByName(prop.name);

                    // Local reference.
                    string directory = asset?.package?.packagePath;

                    // Don't do anything if invalid directory.
                    if (directory != null)
                    {
                        // Skip directories we've already parsed.
                        if (!parsedDirectories.Contains(directory))
                        {
                            // Look for BOB pack file attached to asset.
                            ReadPackFile(Path.Combine(Path.GetDirectoryName(asset.package.packagePath), "BOBPack.xml"), fileList);

                            // Record package path in Hashset.
                            parsedDirectories.Add(directory);
                        }
                    }
                }

                return fileList;
            }
            catch (Exception e)
            {
                // If an exception occured, we'll return a new empty list rather than null.
                Logging.LogException(e, "exception reading replacement pack files");
                return new List<BOBPackFile>();
            }
        }

        /// <summary>
        /// Attempts to read a BOB pack file.
        /// </summary>
        /// <param name="filename">Filename to read.</param>
        /// <param name="fileList">List to add sucessfully parsed files to.</param>
        private static void ReadPackFile(string filename, List<BOBPackFile> fileList)
        {
            // Make sure file exists.
            if (File.Exists(filename))
            {
                Logging.Message("found pack file ", filename);

                // Parse the file.
                try
                {
                    // Read it.
                    using (StreamReader reader = new StreamReader(filename))
                    {
                        XmlSerializer xmlSerializer = new XmlSerializer(typeof(BOBPackFile));
                        if (!(xmlSerializer.Deserialize(reader) is BOBPackFile packFile))
                        {
                            Logging.Error("couldn't deserialize pack file");
                        }
                        else
                        {
                            // Success - add file to list.
                            fileList.Add(packFile);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logging.LogException(e, "exception reading XML pack file ", filename);
                }
            }
        }
    }
}