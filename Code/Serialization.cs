using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using ICities;
using ColossalFramework.IO;


namespace BOB
{
    /// <summary>
    /// Handles savegame data saving and loading.
    /// </summary>
    public class Serializer : SerializableDataExtensionBase
    {
        // Unique data ID.
        private readonly string dataID = "BOB";
        private const uint DataVersion = 0;


        /// <summary>
        /// Serializes data to the savegame.
        /// Called by the game on save.
        /// </summary>
        public override void OnSaveData()
        {
            base.OnSaveData();


            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();

                // Serialise savegame settings.
                DataSerializer.Serialize(stream, DataSerializer.Mode.Memory, DataVersion, new BOBSerializer());

                // Write to savegame.
                serializableDataManager.SaveData(dataID, stream.ToArray());

                Debugging.Message("wrote " + stream.Length);
            }
        }


        /// <summary>
        /// Deserializes data from a savegame (or initialises new data structures when none available).
        /// Called by the game on load (including a new game).
        /// </summary>
        public override void OnLoadData()
        {
            base.OnLoadData();

            // Read data from savegame.
            byte[] data = serializableDataManager.LoadData(dataID);

            // Check to see if anything was read.
            if (data != null && data.Length != 0)
            {
                // Data was read - go ahead and deserialise.
                using (MemoryStream stream = new MemoryStream(data))
                {
                    BinaryFormatter formatter = new BinaryFormatter();

                    // Deserialise savegame settings.
                    DataSerializer.Deserialize<BOBSerializer>(stream, DataSerializer.Mode.Memory);
                    Debugging.Message("read " + stream.Length);
                }
            }
            else
            {
                // No data read.
                Debugging.Message("no data read");
            }
        }
    }


    /// <summary>
    ///  Savegame (de)serialisation for settings.
    /// </summary>
    public class BOBSerializer : IDataContainer
    {
        private const int CurrentDataVersion = 0;
        string[] treeNames;

        /// <summary>
        /// Serialise to savegame.
        /// </summary>
        /// <param name="serializer">Data serializer</param>
        public void Serialize(DataSerializer serializer)
        {
            Debugging.Message("writing data to save file");

            // Write data version.
            serializer.WriteInt32(CurrentDataVersion);

            // Serialize tree replacement dictionary.
            List<string> treeNames = new List<string>();
            foreach (KeyValuePair<TreeInfo, TreeInfo> replacement in MapTreeReplacement.replacements)
            {
                treeNames.Add(replacement.Key.name);
                treeNames.Add(replacement.Value.name);
            }

            // Write tree replacement lists to savegame.
            serializer.WriteUniqueStringArray(treeNames.ToArray());

            Debugging.Message("wrote trees length " + treeNames.Count);
        }


        /// <summary>
        /// Deseralise from savegame.
        /// </summary>
        /// <param name="serializer">Data serializer</param>
        public void Deserialize(DataSerializer serializer)
        {
            Debugging.Message("reading data from save file");

            try
            {
                // Read data version.
                int dataVersion = serializer.ReadInt32();
                Debugging.Message("read data version " + dataVersion);

                // Deserialize tree replacement dictionary keys and values.
                treeNames = serializer.ReadUniqueStringArray();

                Debugging.Message("read trees length " + treeNames.Length);
            }
            catch
            {
                // Don't really care much if nothing read; assume no settings.
                Debugging.Message("error deserializing data");
                return;
            }

            if (treeNames != null && treeNames.Length > 1)
            {
                // Iterate through each keyvalue pair read.
                for (int i = 0; i < treeNames.Length; ++i)
                {
                    // Attempt to load replacement tree prefab (key).
                    TreeInfo targetTree = PrefabCollection<TreeInfo>.FindLoaded(treeNames[i]);
                    if (targetTree == null)
                    {
                        // Failed to find matching tree prefab - skip this one.
                        Debugging.Message("couldn't find replacement tree " + treeNames[i]);
                        continue;
                    }

                    // Attempt to load original tree prefab (value).
                    TreeInfo replacementTree = PrefabCollection<TreeInfo>.FindLoaded(treeNames[++i]);
                    if (replacementTree == null)
                    {
                        // Failed to find matching tree prefab - skip this one.
                        Debugging.Message("couldn't find original tree " + treeNames[i]);
                        continue;
                    }

                    // If we got here, success!  Add to dictionary.
                    MapTreeReplacement.replacements.Add(targetTree, replacementTree);
                }
            }
        }


        /// <summary>
        /// Performs any post-serialization data management.  Called by game.
        /// </summary>
        /// <param name="serializer">Data serializer</param>
        public void AfterDeserialize(DataSerializer serializer)
        {
        }
    }
}