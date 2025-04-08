using OathFramework.EntitySystem;

namespace OathFramework.Achievements
{
    public static class AchievementUtil
    {
        public static void KilledEntity(Entity entity, float damageRatio)
        {
            if(damageRatio > 0.05f) {
                AchievementManager.IncrementStat("enemies_killed_total");
            }
        }
    }
}
