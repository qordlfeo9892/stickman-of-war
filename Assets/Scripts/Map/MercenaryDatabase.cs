using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StickmanOfWar.Map
{
    public static class MercenaryDatabase
    {
        private class NameInfo
        {
            public string Name;
            public string[] Personalities;
        }

        private class RoleTemplate
        {
            public string Role;
            public string Race;
            public float HpMult;
            public float DmgMult;
            public float Interval;
            public float Range;
            public float Speed;
            public float CostWeight; // 소환 비용 가중치 — 탱커는 싸고, 원거리 딜러는 비쌈
            public bool IsHealer;    // true면 공격 대신 아군 회복 (DmgMult는 회복량으로 재해석)
            public NameInfo[] Names;
        }

        private static readonly RoleTemplate[] Roles =
        {
            new RoleTemplate
            {
                Role = "전사", Race = "인간", HpMult = 1.0f, DmgMult = 1.0f, Interval = 1.0f, Range = 70f, Speed = 140f, CostWeight = 1.0f,
                Names = new[]
                {
                    new NameInfo { Name = "브람", Personalities = new[] { "정의", "충직" } },
                    new NameInfo { Name = "케일", Personalities = new[] { "냉혹", "고독" } },
                    new NameInfo { Name = "도란", Personalities = new[] { "충직" } },
                },
            },
            new RoleTemplate
            {
                Role = "수호자", Race = "드워프", HpMult = 1.6f, DmgMult = 0.7f, Interval = 1.2f, Range = 60f, Speed = 110f, CostWeight = 0.55f,
                Names = new[]
                {
                    new NameInfo { Name = "토린", Personalities = new[] { "충직", "정의" } },
                    new NameInfo { Name = "마그다", Personalities = new[] { "고독" } },
                    new NameInfo { Name = "우르사", Personalities = new[] { "파괴", "냉혹" } },
                },
            },
            new RoleTemplate
            {
                Role = "궁수", Race = "엘프", HpMult = 0.7f, DmgMult = 0.9f, Interval = 0.9f, Range = 220f, Speed = 130f, CostWeight = 1.4f,
                Names = new[]
                {
                    new NameInfo { Name = "핀", Personalities = new[] { "냉혹", "고독" } },
                    new NameInfo { Name = "세이블", Personalities = new[] { "탐욕" } },
                    new NameInfo { Name = "렌", Personalities = new[] { "정의", "심판자" } },
                },
            },
            new RoleTemplate
            {
                Role = "자객", Race = "언데드", HpMult = 0.6f, DmgMult = 1.3f, Interval = 0.7f, Range = 60f, Speed = 190f, CostWeight = 1.25f,
                Names = new[]
                {
                    new NameInfo { Name = "닉스", Personalities = new[] { "냉혹", "탐욕" } },
                    new NameInfo { Name = "벡스", Personalities = new[] { "탐욕" } },
                    new NameInfo { Name = "사일러스", Personalities = new[] { "고독", "파괴" } },
                },
            },
            new RoleTemplate
            {
                Role = "광전사", Race = "오크", HpMult = 0.8f, DmgMult = 1.5f, Interval = 1.1f, Range = 65f, Speed = 150f, CostWeight = 1.15f,
                Names = new[]
                {
                    new NameInfo { Name = "그롬", Personalities = new[] { "광기", "파괴" } },
                    new NameInfo { Name = "일사", Personalities = new[] { "광기", "냉혹" } },
                    new NameInfo { Name = "크라그", Personalities = new[] { "광기", "탐욕" } },
                },
            },
            new RoleTemplate
            {
                Role = "마도사", Race = "악마", HpMult = 1.1f, DmgMult = 0.6f, Interval = 1.0f, Range = 150f, Speed = 120f, CostWeight = 1.3f,
                Names = new[]
                {
                    new NameInfo { Name = "엘란드라", Personalities = new[] { "심판자", "정의" } },
                    new NameInfo { Name = "코린", Personalities = new[] { "심판자", "광기" } },
                    new NameInfo { Name = "테살리", Personalities = new[] { "심판자", "고독" } },
                },
            },
            new RoleTemplate
            {
                // 유일한 힐러 역할 — 이름 1개 × 5등급만 존재하는 희귀 라인업.
                // DmgMult는 Unit.cs에서 "회복량" 배율로 재해석된다 (공격력 계산식 그대로 재사용).
                Role = "성직자", Race = "인간", HpMult = 0.9f, DmgMult = 0.5f, Interval = 1.4f, Range = 150f, Speed = 120f, CostWeight = 1.2f,
                IsHealer = true,
                Names = new[]
                {
                    new NameInfo { Name = "셀레네", Personalities = new[] { "충직", "정의" } },
                },
            },
        };

        private static readonly string[] RankPrefix = { "신참", "베테랑", "정예", "챔피언", "전설" };

        private const float BaseHealth = 20f;
        private const float BaseDamage = 5f;

        // 소환 비용: 역할·등급이 큰 틀을 잡고, 실제 스탯과 "특성 조합"으로 유닛마다 값을 벌린다.
        // 마지막에 중복 제거 패스로 90종 전부 서로 다른 비용이 되도록 보장한다.
        private const int CostBase = 6;
        private const int CostPerTierStep = 4;

        // 특성 정체성 가중치 — 유닛마다 종족+성격 조합이 달라 합이 겹치지 않게 촘촘히 벌려 둔다.
        private static readonly Dictionary<string, float> TraitCostWeight = new Dictionary<string, float>
        {
            { "인간", 0.4f }, { "드워프", 1.1f }, { "엘프", 1.9f }, { "언데드", 2.6f }, { "오크", 3.3f }, { "악마", 4.1f },
            { "정의", 0.3f }, { "충직", 0.9f }, { "냉혹", 1.6f }, { "고독", 2.2f },
            { "탐욕", 2.9f }, { "광기", 3.6f }, { "심판자", 4.2f }, { "파괴", 4.9f },
        };

        private static float TraitW(string trait)
        {
            return !string.IsNullOrEmpty(trait) && TraitCostWeight.TryGetValue(trait, out float v) ? v : 0f;
        }

        private static int ComputeDeployCost(RoleTemplate role, int tier, NameInfo nameInfo)
        {
            float tierMult = 1f + (tier - 1) * 0.35f;
            float baseCost = (CostBase + tier * CostPerTierStep) * role.CostWeight;

            // 실제 스탯 기반 조정 (같은 역할이면 동일): DPS·원거리 프리미엄, 체력 할인
            float dps = (BaseDamage * role.DmgMult * tierMult) / Mathf.Max(0.1f, role.Interval);
            float hp = BaseHealth * role.HpMult * tierMult;
            float statAdj = dps * 0.15f + Mathf.Max(0f, role.Range - 90f) * 0.010f - hp * 0.02f;

            // 특성 조합 서명 (유닛마다 다름) — 미세 변별용이라 기여도는 작게
            string p1 = nameInfo.Personalities.Length > 0 ? nameInfo.Personalities[0] : "";
            string p2 = nameInfo.Personalities.Length > 1 ? nameInfo.Personalities[1] : "";
            float sig = (TraitW(role.Race) + TraitW(p1) + TraitW(p2)) * 0.5f;

            return Mathf.Max(4, Mathf.RoundToInt(baseCost + statAdj + sig));
        }

        private static readonly MercenaryDefinition[] All = BuildAll();

        private static MercenaryDefinition[] BuildAll()
        {
            var list = new List<MercenaryDefinition>();
            for (int tier = 1; tier <= 5; tier++)
            {
                float tierMult = 1f + (tier - 1) * 0.35f;
                string rank = RankPrefix[tier - 1];

                foreach (RoleTemplate role in Roles)
                {
                    foreach (NameInfo nameInfo in role.Names)
                    {
                        string id = $"{role.Role}_t{tier}_{nameInfo.Name}";
                        string displayName = $"{rank} {role.Role} {nameInfo.Name}";
                        float maxHealth = BaseHealth * role.HpMult * tierMult;
                        float attackDamage = BaseDamage * role.DmgMult * tierMult;
                        string personality1 = nameInfo.Personalities.Length > 0 ? nameInfo.Personalities[0] : "";
                        string personality2 = nameInfo.Personalities.Length > 1 ? nameInfo.Personalities[1] : "";
                        int deployCost = ComputeDeployCost(role, tier, nameInfo);

                        list.Add(new MercenaryDefinition(
                            id, displayName, tier, role.Race, personality1, personality2,
                            maxHealth, attackDamage, role.Interval, role.Range, role.Speed, deployCost,
                            role.IsHealer));
                    }
                }
            }

            // 90종 전부 소환 비용이 서로 다르도록 보장 — 값이 겹치면 +1씩 밀어낸다.
            // 현재 비용 오름차순으로 처리해 "쌀수록 저렴하다"는 상대 순위는 유지.
            var usedCosts = new HashSet<int>();
            foreach (MercenaryDefinition m in list.OrderBy(x => x.DeployCost))
            {
                while (usedCosts.Contains(m.DeployCost)) m.DeployCost++;
                usedCosts.Add(m.DeployCost);
            }

            return list.ToArray();
        }

        public static MercenaryDefinition[] GetByTier(int tier)
        {
            return All.Where(m => m.Tier == tier).ToArray();
        }

        public static MercenaryDefinition GetById(string id)
        {
            return All.FirstOrDefault(m => m.Id == id);
        }

        private static int RollWeightedTier(int act)
        {
            int low = Mathf.Clamp(act, 1, 5);
            int mid = Mathf.Clamp(act + 1, 1, 5);
            int high = Mathf.Clamp(act + 2, 1, 5);

            int roll = Random.Range(0, 100);
            if (roll < 50) return low;
            if (roll < 85) return mid;
            return high;
        }

        public static int RollGambleTier(int act)
        {
            int roll = Random.Range(0, 100);
            if (roll < 5) return 5;
            if (roll < 20) return Mathf.Clamp(act + 3, 1, 5);
            return RollWeightedTier(act);
        }

        public static MercenaryDefinition GetRandomForGamble(int act)
        {
            MercenaryDefinition[] pool = GetByTier(RollGambleTier(act));
            return pool[Random.Range(0, pool.Length)];
        }

        private static MercenaryDefinition PickDistinct(int tier, HashSet<string> usedIds)
        {
            MercenaryDefinition[] pool = GetByTier(tier);
            List<MercenaryDefinition> available = pool.Where(m => !usedIds.Contains(m.Id)).ToList();
            if (available.Count == 0) available = pool.ToList();

            MercenaryDefinition pick = available[Random.Range(0, available.Count)];
            usedIds.Add(pick.Id);
            return pick;
        }

        public static MercenaryDefinition[] RollOffersWeighted(int act, int count)
        {
            var usedIds = new HashSet<string>();
            var offers = new MercenaryDefinition[count];
            for (int i = 0; i < count; i++)
            {
                int tier = RollWeightedTier(act);
                offers[i] = PickDistinct(tier, usedIds);
            }
            return offers;
        }

        public static MercenaryDefinition[] RollOffersFixedTier(int tier, int count)
        {
            var usedIds = new HashSet<string>();
            var offers = new MercenaryDefinition[count];
            for (int i = 0; i < count; i++)
            {
                offers[i] = PickDistinct(tier, usedIds);
            }
            return offers;
        }

        public static int GetTavernCost(int tier)
        {
            return tier * 20;
        }

        // 선술집 영입 골드 비용 — 유닛별 소환 비용에 비례하게 (등급뿐 아니라 유닛 특색 반영)
        public static int GetTavernCost(MercenaryDefinition merc)
        {
            if (merc == null) return 20;
            return Mathf.RoundToInt(merc.DeployCost * 2.2f) + merc.Tier * 6;
        }
    }
}
