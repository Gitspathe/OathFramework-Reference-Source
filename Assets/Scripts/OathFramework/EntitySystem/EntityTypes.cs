using OathFramework.Extensions;
using System;
using UnityEngine;

namespace OathFramework.EntitySystem
{

    public static class EntityTypes
    {
        private static readonly EntityTeams[] allEntityTypes = { EntityTeams.Human, EntityTeams.Monster };
        private static readonly EntityTeams[] humanEnemies   = { EntityTeams.Monster };
        private static readonly EntityTeams[] monsterEnemies = { EntityTeams.Human };

        public static EntityTeams[] AllTypes       => allEntityTypes;
        public static EntityTeams[] HumanEnemies   => humanEnemies;
        public static EntityTeams[] MonsterEnemies => monsterEnemies;

        public static EntityTeams[] GetEnemies(EntityTeams type)
        {
            switch(type) {
                case EntityTeams.Monster:
                    return MonsterEnemies;
                case EntityTeams.Human:
                    return HumanEnemies;

                default:
                    return null;
            }
        }

        public static bool AreEnemies(EntityTeams a, EntityTeams b)
        {
            switch(a) {
                case EntityTeams.Human:
                    return HumanEnemies.Contains(b);
                case EntityTeams.Monster:
                    return MonsterEnemies.Contains(b);
                
                case EntityTeams.None:
                default:
                    return false;
            }
        }
        
        public static bool AreFriends(EntityTeams a, EntityTeams b)
        {
            switch(a) {
                case EntityTeams.Human:
                    return !HumanEnemies.Contains(b);
                case EntityTeams.Monster:
                    return !MonsterEnemies.Contains(b);
                
                case EntityTeams.None:
                default:
                    return false;
            }
        }
    }

    public enum EntityTeams : int
    {
        None    = 0,
        Human   = 1,
        Monster = 2
    }
}
