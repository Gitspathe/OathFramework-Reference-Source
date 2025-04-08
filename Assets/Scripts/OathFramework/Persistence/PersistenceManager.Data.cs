using Cysharp.Threading.Tasks;
using OathFramework.Core;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Serialization.Json;

namespace OathFramework.Persistence
{
    public partial class PersistenceManager
    {
        public struct Data
        {
            public Dictionary<string, PersistentScene.Data> Scenes;

            public async UniTask SaveToFile(string snapshotName)
            {
                bool? compressOverride = null;
                if(INISettings.GetString("FileIO/PersistenceFormat", out string val)) {
                    if(val == "debug") {
                        compressOverride = false;
                    } else if(val == "performance") {
                        compressOverride = true;
                    }
                }
                
                string path = $"{FileIO.SnapshotPath}{snapshotName}.sav";
                string bak  = $"{FileIO.SnapshotPath}{snapshotName}.bak";
                string json = JsonSerialization.ToJson(this, serializerParams);
                await UniTask.WhenAll(
                    FileIO.SaveFile(path, json, compressOverride ?? compressSnapshots),
                    FileIO.SaveFile(bak, json, compressOverride ?? compressSnapshots)
                );
            }

            public static async UniTask<(LoadResult, Data)> LoadFromFile(string snapshotName)
            {
                string path   = $"{FileIO.SnapshotPath}{snapshotName}.sav";
                string bak    = $"{FileIO.SnapshotPath}{snapshotName}.bak";
                bool isBackup = false;
                Data data;
                string json;
                try {
                    json = await FileIO.LoadFile(path);
                    data = JsonSerialization.FromJson<Data>(json, serializerParams);
                } catch(Exception) {
                    try {
                        isBackup = true;
                        json     = await FileIO.LoadFile(bak);
                        data     = JsonSerialization.FromJson<Data>(json, serializerParams);
                    } catch(Exception) { return (LoadResult.Fail, default); }
                }
                
                if(isBackup) {
                    await FileIO.SaveFile(path, json, compressSnapshots);
                }
                LoadResult loadResult = isBackup ? LoadResult.Backup : LoadResult.Success;
                return (loadResult, data);
            }

            public static Data Construct()
            {
                Dictionary<string, PersistentScene.Data> dict = new();
                Data data = new() { Scenes = dict };
                dict.Add(GlobalScene.ID, new PersistentScene.Data(GlobalScene));
                foreach(KeyValuePair<string, PersistentScene> pair in persistentScenes) {
                    dict.Add(pair.Key, new PersistentScene.Data(pair.Value));
                }
                return data;
            }
        }

        public class DataAdapter : IJsonAdapter<Data>
        {
            public void Serialize(in JsonSerializationContext<Data> context, Data value)
            {
                using JsonWriter.ObjectScope objScope = context.Writer.WriteObjectScope();
                context.Writer.WriteKey("scenes");
                using JsonWriter.ArrayScope arrScope = context.Writer.WriteArrayScope();

                foreach(KeyValuePair<string, PersistentScene.Data> pair in value.Scenes) {
                    context.SerializeValue(pair.Value);
                }
            }

            public Data Deserialize(in JsonDeserializationContext<Data> context)
            {
                Data data                    = new() { Scenes = new Dictionary<string, PersistentScene.Data>() };
                SerializedValueView value    = context.SerializedValue;
                SerializedArrayView sceneArr = value.GetValue("scenes").AsArrayView();
                foreach(SerializedValueView scene in sceneArr) {
                    data.Scenes.Add(scene.GetValue("id").ToString(), context.DeserializeValue<PersistentScene.Data>(scene));
                }
                return data;
            }
        }
    }
}
