#if !UNITY_IOS && !UNITY_ANDROID
using OathFramework.Platform.Steam;
#endif
using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Utility;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Serialization.Json;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace OathFramework.Achievements
{
    public class AchievementManager : Subsystem, IResetGameStateCallback, IInitialized
    {
        [SerializeField] private List<AchievementCollection> achievementCollections;
        [SerializeField] private List<StatCollection> statCollections;

        public override string Name    => "Achievement Manager";
        public override uint LoadOrder => SubsystemLoadOrders.AchievementManager;
        public uint Order              => 100;

        private static bool isSaving;
        private static SaveData saveData;
        private static IAchievementPlatform achievementPlatform;
        private static Dictionary<string, Achievement> achievements            = new();
        private static Dictionary<string, Stat> stats                          = new();
        private static Dictionary<string, bool> achievementStates              = new();
        private static Dictionary<string, int> statStates                      = new();
        private static Dictionary<Stat, QList<Achievement>> linkedAchievements = new();

        public static AchievementManager Instance { get; private set; }

        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(AchievementManager)} singleton.");
                Destroy(this);
                return UniTask.CompletedTask;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
            foreach(AchievementCollection collection in achievementCollections) {
                foreach(Achievement a in collection.Achievements) {
                    achievements.Add(a.ID, a);
                    achievementStates.Add(a.ID, false);
                }
            }
            foreach(StatCollection collection in statCollections) {
                foreach(Stat s in collection.Stats) {
                    stats.Add(s.ID, s);
                    statStates.Add(s.ID, s.DefaultVal);
                }
            }
            foreach(KeyValuePair<string, Achievement> a in achievements) {
                if(!a.Value.BasedOnStat || !TryGetStat(a.Value.StatID, out Stat stat, out int _))
                    continue;

                if(!linkedAchievements.TryGetValue(stat, out QList<Achievement> collection)) {
                    collection = new QList<Achievement>();
                    linkedAchievements.Add(stat, collection);
                }
                collection.Add(a.Value);
            }
            saveData = new SaveData(achievementStates, statStates);
            GameCallbacks.Register((IInitialized)this);
            GameCallbacks.Register((IResetGameStateCallback)this);
            return UniTask.CompletedTask;
        }

        async UniTask IInitialized.OnGameInitialized()
        {
#if !UNITY_IOS && !UNITY_ANDROID
            if(Game.Instance.supportSteam) {
                SetPlatform(new SteamAchievementPlatform());
            }
#endif
            await Load();
            achievementPlatform?.Initialize(achievementStates, statStates);
        }
        
        void IResetGameStateCallback.OnResetGameState()
        {
            if(isSaving)
                return;
            
            _ = Save();
        }

        public static void SetPlatform(IAchievementPlatform platform)
        {
            achievementPlatform = platform;
        }

        public static bool TryGetAchievement(string id, out Achievement achievement, out bool state)
        {
            achievement = null;
            state       = false;
            return achievements.TryGetValue(id, out achievement) && achievementStates.TryGetValue(id, out state);
        }

        public static bool TryGetStat(string id, out Stat stat, out int state)
        {
            stat  = null;
            state = 0;
            return stats.TryGetValue(id, out stat) && statStates.TryGetValue(id, out state);
        }

        public static void IncrementStat(string id, int value = 1)
        {
            if(!TryGetStat(id, out Stat stat, out int current)) {
                Debug.LogError($"No stat with ID '{id}' found");
                return;
            }
            int newVal = current + value;
            statStates[id] = Mathf.Clamp(newVal, stat.MinVal, stat.MaxVal);
            UpdateLinkedStatAchievements(stat, newVal);
            achievementPlatform?.OnStatChanged(stat, newVal);
        }

        public static void DecrementStat(string id, int value = 1)
        {
            if(!TryGetStat(id, out Stat stat, out int current)) {
                Debug.LogError($"No stat with ID '{id}' found");
                return;
            }
            int newVal = current - value;
            statStates[id] = Mathf.Clamp(newVal, stat.MinVal, stat.MaxVal);
            UpdateLinkedStatAchievements(stat, newVal);
            achievementPlatform?.OnStatChanged(stat, newVal);
        }

        public static void SetStat(string id, int value)
        {
            if(!TryGetStat(id, out Stat stat, out int _)) {
                Debug.LogError($"No stat with ID '{id}' found");
                return;
            }
            statStates[id] = Mathf.Clamp(value, stat.MinVal, stat.MaxVal);
            UpdateLinkedStatAchievements(stat, value);
            achievementPlatform?.OnStatChanged(stat, value);
        }

        private static void UpdateLinkedStatAchievements(Stat stat, int newValue)
        {
            if(!linkedAchievements.TryGetValue(stat, out QList<Achievement> collection))
                return;

            for(int i = 0; i < collection.Count; i++) {
                Achievement a = collection.Array[i];
                if(achievementStates[a.ID])
                    continue; // Already unlocked.

                switch(a.UnlockReqType) {
                    case AchievementStatRequirementType.AtOrAbove: {
                        if(newValue >= a.UnlockedAt) {
                            UnlockAchievement(a.ID);
                        }
                    } break;
                    case AchievementStatRequirementType.AtOrBelow: {
                        if(newValue <= a.UnlockedAt) {
                            UnlockAchievement(a.ID);
                        }
                    } break;
                    default:
                        return;
                }
            }
        }

        public static bool UnlockAchievement(string id)
        {
            if(!TryGetAchievement(id, out Achievement achievement, out bool state)) {
                Debug.LogError($"No achievement with ID '{id}' found");
                return false;
            }
            if(state)
                return false; // Already unlocked.

            achievementStates[id] = true;
            achievementPlatform?.OnAchievementUnlocked(achievement);
            _ = Save();
            return true;
        }

        public static async UniTask Save()
        {
            if(isSaving)
                return;

            isSaving = true;
            await FileIO.SaveFile($"{FileIO.SavePath}achievements.sav", JsonSerialization.ToJson(saveData), noHeader: true);
            isSaving = false;
        }

        public static async UniTask Load()
        {
            if(!FileIO.FileExists($"{FileIO.SavePath}achievements.sav")) {
                await LoadExisting();
                await Save();
                return;
            }
            string json   = await FileIO.LoadFile($"{FileIO.SavePath}achievements.sav", noHeader: true);
            SaveData copy = JsonSerialization.FromJson<SaveData>(json);
            copy.CopyTo(saveData);
        }

        private static async UniTask LoadExisting()
        {
            if(achievementPlatform != null) {
                await achievementPlatform.ReadExisting(achievementStates, statStates);
            }
        }
    }

    public class SaveData
    {
        private Dictionary<string, bool> achievementStates = new();
        private Dictionary<string, int> statStates = new();

        public SaveData() { }

        public SaveData(Dictionary<string, bool> achievementStates, Dictionary<string, int> statStates)
        {
            this.achievementStates = achievementStates;
            this.statStates        = statStates;
        }

        public void CopyTo(SaveData other)
        {
            other.achievementStates.Clear();
            other.statStates.Clear();
            foreach(KeyValuePair<string, bool> pair in achievementStates) {
                other.achievementStates.Add(pair.Key, pair.Value);
            }
            foreach(KeyValuePair<string, int> pair in statStates) {
                other.statStates.Add(pair.Key, pair.Value);
            }
        }

        public class JsonAdapter : IJsonAdapter<SaveData>
        {
            public void Serialize(in JsonSerializationContext<SaveData> context, SaveData value)
            {
                using JsonWriter.ObjectScope objScope = context.Writer.WriteObjectScope();
                context.Writer.WriteKey("achievements");
                using(context.Writer.WriteArrayScope()) {
                    foreach(KeyValuePair<string, bool> pair in value.achievementStates) {
                        using JsonWriter.ObjectScope obj2Scope = context.Writer.WriteObjectScope();
                        context.Writer.WriteKeyValue("id", pair.Key);
                        context.Writer.WriteKeyValue("state", pair.Value);
                    }
                }
                context.Writer.WriteKey("stats");
                using(context.Writer.WriteArrayScope()) {
                    foreach(KeyValuePair<string, int> pair in value.statStates) {
                        using JsonWriter.ObjectScope obj2Scope = context.Writer.WriteObjectScope();
                        context.Writer.WriteKeyValue("id", pair.Key);
                        context.Writer.WriteKeyValue("state", pair.Value);
                    }
                }
            }

            public SaveData Deserialize(in JsonDeserializationContext<SaveData> context)
            {
                Dictionary<string, bool> foundAchievementStates = new();
                Dictionary<string, int> foundStatStates         = new();
                SerializedValueView value                       = context.SerializedValue;
                SerializedArrayView arr1                        = value.GetValue("achievements").AsArrayView();
                SerializedArrayView arr2                        = value.GetValue("stats").AsArrayView();
                foreach(SerializedValueView arrVal in arr1) {
                    string id  = arrVal.GetValue("id").ToString();
                    bool state = arrVal.GetValue("state").AsBoolean();
                    foundAchievementStates.Add(id, state);
                }
                foreach(SerializedValueView statVal in arr2) {
                    string id = statVal.GetValue("id").ToString();
                    int state = statVal.GetValue("state").AsInt32();
                    foundStatStates.Add(id, state);
                }
                return new SaveData(foundAchievementStates, foundStatStates);
            }
        }
    }
}
