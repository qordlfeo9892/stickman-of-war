using System.Collections.Generic;
using System.Linq;

namespace StickmanOfWar.Map
{
    public class MercenaryDefinition
    {
        public string Id;
        public string DisplayName;
        public int Tier;
        public string Race;
        public string Personality1;
        public string Personality2;
        public float MaxHealth;
        public float AttackDamage;
        public float AttackInterval;
        public float AttackRange;
        public float MoveSpeed;

        // 전투 중 이 유닛을 소환하는 데 드는 재화. 같은 등급이라도 역할/특성에 따라 유닛마다 다르다.
        public int DeployCost;

        // 힐러 역할 여부 — true면 Unit이 공격 대신 아군 회복 행동을 한다 (AttackDamage는 회복량으로 재해석).
        public bool IsHealer;

        public float Dps => AttackInterval > 0.01f ? AttackDamage / AttackInterval : AttackDamage;

        public List<string> Traits => new[] { Race, Personality1, Personality2 }
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList();

        public MercenaryDefinition(string id, string displayName, int tier, string race, string personality1, string personality2,
            float maxHealth, float attackDamage, float attackInterval, float attackRange, float moveSpeed, int deployCost,
            bool isHealer = false)
        {
            Id = id;
            DisplayName = displayName;
            Tier = tier;
            Race = race;
            Personality1 = personality1;
            Personality2 = personality2;
            MaxHealth = maxHealth;
            AttackDamage = attackDamage;
            AttackInterval = attackInterval;
            AttackRange = attackRange;
            MoveSpeed = moveSpeed;
            DeployCost = deployCost;
            IsHealer = isHealer;
        }
    }
}
