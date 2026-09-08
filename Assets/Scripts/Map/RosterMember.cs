using System.Collections.Generic;

namespace StickmanOfWar.Map
{
    // 로스터 한 칸의 상태를 담는 래퍼.
    // MercenaryDefinition은 MercenaryDatabase의 static 배열에 있는 "공유 인스턴스"라서
    // (같은 id면 항상 같은 객체) 진화(2성) 스탯을 원본에 직접 적용하면 이후 다른 슬롯/오퍼에
    // 등장할 때도 강화된 채로 나오는 버그가 생긴다. 그래서 배율은 항상 이 래퍼에서만 계산한다.
    public class RosterMember
    {
        public const int MaxStar = 2;
        public const float EvolvedStatMult = 1.7f; // 2성 체력/공격력 배율 (사거리·속도·주기는 유지)

        public MercenaryDefinition Definition;
        public int Star;

        public RosterMember(MercenaryDefinition definition, int star = 1)
        {
            Definition = definition;
            Star = star;
        }

        public bool IsEvolved => Star >= MaxStar;

        // --- 표시/전투에 필요한 파생 프로퍼티 (기존에 MercenaryDefinition을 직접 읽던 코드 호환용) ---
        public string Id => Definition.Id;
        public string DisplayName => Definition.DisplayName;
        public int Tier => Definition.Tier;
        public string Race => Definition.Race;
        public string Personality1 => Definition.Personality1;
        public string Personality2 => Definition.Personality2;
        public bool IsHealer => Definition.IsHealer;
        public int DeployCost => Definition.DeployCost;
        public List<string> Traits => Definition.Traits;

        public float MaxHealth => Definition.MaxHealth * (IsEvolved ? EvolvedStatMult : 1f);
        public float AttackDamage => Definition.AttackDamage * (IsEvolved ? EvolvedStatMult : 1f);
        public float AttackInterval => Definition.AttackInterval;
        public float AttackRange => Definition.AttackRange;
        public float MoveSpeed => Definition.MoveSpeed;
    }
}
