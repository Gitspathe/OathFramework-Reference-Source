using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace OathFramework.Achievements
{
    [Serializable]
    public class Achievement
    {
        [field: SerializeField] public string ID            { get; private set; }
        [field: SerializeField] public string Name          { get; private set; }
        [field: SerializeField] public string Description   { get; private set; }
        [field: SerializeField] public Sprite LockedIcon    { get; private set; }
        [field: SerializeField] public Sprite UnlockedIcon  { get; private set; }

        [field: SerializeField] public bool BasedOnStat     { get; private set; }
        
        [field: SerializeField, ShowIf("@BasedOnStat")]
        public string StatID                                { get; private set; }
        
        [field: SerializeField, ShowIf("@BasedOnStat")]
        public int UnlockedAt                               { get; private set; }
        
        [field: SerializeField, ShowIf("@BasedOnStat")]
        public AchievementStatRequirementType UnlockReqType { get; private set; }
    }

    public enum AchievementStatRequirementType
    {
        AtOrAbove,
        AtOrBelow
    }
}
