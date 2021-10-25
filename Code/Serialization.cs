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
        public const int DataVersion = 2;


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

                Logging.Message("wrote ", stream.Length.ToString());
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
                    Logging.Message("read ", stream.Length.ToString());
                }
            }
            else
            {
                // No data read.
                Logging.Message("no data read");
            }
        }
    }


    /// <summary>
    ///  Savegame (de)serialisation for settings.
    /// </summary>
    public class BOBSerializer : IDataContainer
    {
        private const int CurrentDataVersion = Serializer.DataVersion;

        /// <summary>
        /// Serialise to savegame.
        /// </summary>
        /// <param name="serializer">Data serializer</param>
        public void Serialize(DataSerializer serializer)
        {
            Logging.Message("writing data to save file");

            // Write data version.
            serializer.WriteInt32(CurrentDataVersion);

            // Serialize tree replacement dictionary.
            List<string> treeNames = new List<string>();
            foreach (KeyValuePair<TreeInfo, TreeInfo> replacement in MapTreeReplacement.instance.replacements)
            {
                treeNames.Add(replacement.Key.name);
                treeNames.Add(replacement.Value.name);
            }

            // Serialize prop replacement dictionary.
            List<string> propNames = new List<string>();
            foreach (KeyValuePair<PropInfo, PropInfo> replacement in MapPropReplacement.instance.replacements)
            {
                propNames.Add(replacement.Key.name);
                propNames.Add(replacement.Value.name);
            }

            // Write tree replacement lists to savegame.
            serializer.WriteUniqueStringArray(treeNames.ToArray());
            serializer.WriteUniqueStringArray(propNames.ToArray());
            Logging.Message("wrote trees length ", treeNames.Count.ToString());
            Logging.Message("wrote props length ", propNames.Count.ToString());

            // Write current configuration name.
            serializer.WriteSharedString(ConfigurationUtils.CurrentConfigName);
            Logging.Message("wrote current configuration name ", ConfigurationUtils.CurrentConfigName ?? "null");
        }


        /// <summary>
        /// Deseralise from savegame.
        /// </summary>
        /// <param name="serializer">Data serializer</param>
        public void Deserialize(DataSerializer serializer)
        {
            // Map replacement arrays.
            string[] treeNames, propNames = null;

            Logging.Message("reading data from save file");

            try
            {
                // Read data version.
                int dataVersion = serializer.ReadInt32();
                Logging.Message("read data version ", dataVersion.ToString());

                // Deserialize tree replacement dictionary keys and values.
                treeNames = serializer.ReadUniqueStringArray();
                Logging.Message("read trees length ", treeNames.Length.ToString());

                // Deserialize prop replacement dictionary keys and values if we're using version 2 or greater.
                if (dataVersion > 1)
                {
                    propNames = serializer.ReadUniqueStringArray();
                    Logging.Message("read props length ", propNames.Length.ToString());
                }

                // Read custom config name if we're using version 1 or greater.
                if (dataVersion > 0)
                {
                    ConfigurationUtils.CurrentConfigName = serializer.ReadSharedString();
                    Logging.Message("read current configuration name ", (ConfigurationUtils.CurrentConfigName ?? "null"));
                }
            }
            catch
            {
                // Don't really care much if nothing read; assume no settings.
                Logging.Message("error deserializing data");
                return;
            }

            // Populate map tree replacement dictionary.
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
                        Logging.Message("couldn't find replacement tree ", treeNames[i]);
                        continue;
                    }

                    // Attempt to load original tree prefab (value).
                    TreeInfo replacementTree = PrefabCollection<TreeInfo>.FindLoaded(treeNames[++i]);
                    if (replacementTree == null)
                    {
                        // Failed to find matching tree prefab - skip this one.
                        Logging.Message("couldn't find original tree ", treeNames[i]);
                        continue;
                    }

                    // If we got here, success!  Add to dictionary.
                    MapTreeReplacement.instance.replacements.Add(targetTree, replacementTree);
                }
            }

            // Populate map prop replacement dictionary.
            if (propNames != null && propNames.Length > 1)
            {
                // Iterate through each keyvalue pair read.
                for (int i = 0; i < propNames.Length; ++i)
                {
                    // Attempt to load replacement tree prefab (key).
                    PropInfo targetProp = PrefabCollection<PropInfo>.FindLoaded(propNames[i]);
                    if (targetProp == null)
                    {
                        // Failed to find matching tree prefab - skip this one.
                        Logging.Message("couldn't find replacement tree ", treeNames[i]);
                        continue;
                    }

                    // Attempt to load original tree prefab (value).
                    PropInfo replacementProp = PrefabCollection<PropInfo>.FindLoaded(propNames[++i]);
                    if (replacementProp == null)
                    {
                        // Failed to find matching tree prefab - skip this one.
                        Logging.Message("couldn't find original tree ", treeNames[i]);
                        continue;
                    }

                    // If we got here, success!  Add to dictionary.
                    MapPropReplacement.instance.replacements.Add(targetProp, replacementProp);
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