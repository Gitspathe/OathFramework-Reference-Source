using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace OathFramework.Achievements
{
    public interface IAchievementPlatform
    {
        UniTask Initialize(Dictionary<string, bool> achievementStates, Dictionary<string, int> statStates);
        UniTask ReadExisting(Dictionary<string, bool> achievementStates, Dictionary<string, int> statStates);
        void OnStatChanged(Stat stat, int newValue);
        void OnAchievementUnlocked(Achievement achievement);
    }
}
