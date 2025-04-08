#if !UNITY_IOS && !UNITY_ANDROID
using Cysharp.Threading.Tasks;
using OathFramework.Achievements;
using Steamworks;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.Platform.Steam
{
    public class SteamAchievementPlatform : IAchievementPlatform
    {
        public async UniTask Initialize(Dictionary<string, bool> achievementStates, Dictionary<string, int> statStates)
        {
            foreach(KeyValuePair<string, bool> pair in achievementStates) {
                if(!pair.Value)
                    continue;
                
                new Steamworks.Data.Achievement(pair.Key).Trigger();
            }
            foreach(KeyValuePair<string, int> pair in statStates) {
                SteamUserStats.SetStat(pair.Key, pair.Value);
            }
        }

        public async UniTask ReadExisting(Dictionary<string, bool> achievementStates, Dictionary<string, int> statStates)
        {
            Dictionary<string, bool> newAchievementStates = new();
            Dictionary<string, int> newStatStates         = new();
            foreach(KeyValuePair<string, bool> pair in achievementStates) {
                newAchievementStates.Add(pair.Key, new Steamworks.Data.Achievement(pair.Key).State);
            }
            foreach(KeyValuePair<string, int> pair in statStates) {
                newStatStates.Add(pair.Key, SteamUserStats.GetStatInt(pair.Key));
            }
            foreach(KeyValuePair<string,bool> state in newAchievementStates) {
                achievementStates[state.Key] = state.Value;
            }
            foreach(KeyValuePair<string, int> state in newStatStates) {
                statStates[state.Key] = state.Value;
            }
        }

        public void OnStatChanged(Stat stat, int newValue)
        {
            SteamUserStats.SetStat(stat.ID, newValue);
        }

        public void OnAchievementUnlocked(Achievement achievement)
        {
            new Steamworks.Data.Achievement(achievement.ID).Trigger();
        }
    }
}
#endif
