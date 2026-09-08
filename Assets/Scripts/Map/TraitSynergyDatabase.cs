using System.Collections.Generic;
using System.Linq;

namespace StickmanOfWar.Map
{
    public enum SynergyRank
    {
        None,
        Silver,
        Gold,
        Prismatic,
    }

    public static class TraitSynergyDatabase
    {
        // 활성화 임계값은 특성마다 다르다:
        //  - 종족(특성1): 3/6/9 (3의 배수)
        //  - 일부 성격: 2/4/6 (2의 배수)
        //  - 일부 성격: 3/6/9 유지 (정의/파괴 — 완만한 스케일링)
        //  - 특수 성격: "정확히 N명"일 때만 활성 (고독 = 1, 심판자 = 4) — ExactActivation 참고
        private static readonly Dictionary<string, (int count, string desc)[]> Tiers = new Dictionary<string, (int, string)[]>
        {
            // ── 종족(특성1) : 3의 배수 ──
            { "인간", new[] { (3, "방어력 +10"), (6, "방어력 +20, 이동속도 +5%"), (9, "방어력 +35, 이동속도 +10%") } },
            { "드워프", new[] { (3, "최대 체력 +15%"), (6, "받는 피해 -10%"), (9, "받는 피해 -20%, 최대 체력 +30%") } },
            { "엘프", new[] { (3, "사거리 +20%"), (6, "공격속도 +15%"), (9, "사거리 +40%, 공격속도 +30%") } },
            { "언데드", new[] { (3, "공격력 +10%"), (6, "공격력 +20%, 생명력 흡수 +10%"), (9, "공격력 +35%, 생명력 흡수 +20%") } },
            { "오크", new[] { (3, "공격력 +15%"), (6, "공격속도 +20%"), (9, "공격력 +30%, 공격속도 +35%") } },
            { "악마", new[] { (3, "생명력 흡수 +5%"), (6, "생명력 흡수 +10%"), (9, "생명력 흡수 +20%") } },

            // ── 성격 : 2의 배수 ──
            { "광기", new[] { (2, "공격속도 +12%"), (4, "공격속도 +25%"), (6, "공격속도 +42%") } },
            { "냉혹", new[] { (2, "치명타 피해 +18%"), (4, "치명타 피해 +35%"), (6, "치명타 피해 +60%") } },
            { "탐욕", new[] { (2, "골드 획득 +10%"), (4, "골드 획득 +18%"), (6, "골드 획득 +30%") } },
            { "충직", new[] { (2, "아군 보호막 +8%"), (4, "아군 보호막 +16%"), (6, "아군 보호막 +28%") } },

            // ── 성격 : 3의 배수 유지(완만) ──
            { "정의", new[] { (3, "받는 피해 -10%"), (6, "받는 피해 -20%"), (9, "받는 피해 -35%") } },
            { "파괴", new[] { (3, "공격력 +10%"), (6, "공격력 +20%"), (9, "공격력 +35%") } },

            // ── 특수 : 정확히 N명일 때만 (ExactActivation) ──
            { "고독", new[] { (1, "홀로 싸울 때(정확히 1명) : 회피율 +30%, 이동속도 +15%") } },
            { "심판자", new[] { (4, "심판단 결성(정확히 4명) : 체력 20% 이하 적 처형") } },
        };

        // 이 특성들은 count == 임계값 일 때만 활성화된다 (초과하면 비활성).
        private static readonly HashSet<string> ExactActivation = new HashSet<string> { "고독", "심판자" };

        // 단계 인덱스와 무관하게 특정 특성의 랭크(색)를 강제 지정.
        private static readonly Dictionary<string, SynergyRank> RankOverride = new Dictionary<string, SynergyRank>
        {
            { "고독", SynergyRank.Gold },
            { "심판자", SynergyRank.Prismatic },
        };

        private static readonly SynergyRank[] RankByTierIndex = { SynergyRank.Silver, SynergyRank.Gold, SynergyRank.Prismatic };

        private static Dictionary<string, int> CountTraits(List<MercenaryDefinition> roster)
        {
            var counts = new Dictionary<string, int>();
            foreach (MercenaryDefinition merc in roster)
            {
                foreach (string trait in merc.Traits)
                {
                    counts[trait] = counts.TryGetValue(trait, out int c) ? c + 1 : 1;
                }
            }
            return counts;
        }

        public static bool IsExactActivation(string trait) => ExactActivation.Contains(trait);

        // 주어진 특성/단계가 현재 보유 수(count)로 활성 상태인지.
        // Exact 특성은 정확히 일치할 때만, 그 외는 임계값 이상이면 활성.
        public static bool IsTierActive(string trait, int tierIndex, int count)
        {
            if (!Tiers.TryGetValue(trait, out (int count, string desc)[] tiers)) return false;
            if (tierIndex < 0 || tierIndex >= tiers.Length) return false;
            return ExactActivation.Contains(trait) ? count == tiers[tierIndex].count : count >= tiers[tierIndex].count;
        }

        // 현재 보유 수로 달성한 최고 단계 인덱스(0-base). 미달성이면 -1.
        public static int GetActiveTierIndex(string trait, int count)
        {
            if (!Tiers.TryGetValue(trait, out (int count, string desc)[] tiers)) return -1;
            bool exact = ExactActivation.Contains(trait);
            int idx = -1;
            for (int i = 0; i < tiers.Length; i++)
            {
                if (exact ? count == tiers[i].count : count >= tiers[i].count) idx = i;
            }
            return idx;
        }

        // 툴팁용 — 특성의 전체 단계 목록을 그대로 노출한다.
        public static IReadOnlyList<(int count, string desc)> GetTiers(string trait)
        {
            return Tiers.TryGetValue(trait, out (int count, string desc)[] tiers)
                ? tiers
                : System.Array.Empty<(int count, string desc)>();
        }

        // 단계 인덱스 기준 기본 랭크(실버/골드/프리즘).
        public static SynergyRank RankForTier(int tierIndex)
        {
            return tierIndex >= 0 && tierIndex < RankByTierIndex.Length
                ? RankByTierIndex[tierIndex]
                : SynergyRank.None;
        }

        // 특성별 랭크 오버라이드를 반영한 단계 랭크.
        public static SynergyRank RankForTier(string trait, int tierIndex)
        {
            if (RankOverride.TryGetValue(trait, out SynergyRank r)) return r;
            return RankForTier(tierIndex);
        }

        // 특정 특성을 가진 로스터 유닛 수.
        public static int CountTrait(List<MercenaryDefinition> roster, string trait)
        {
            return CountTraits(roster).TryGetValue(trait, out int c) ? c : 0;
        }

        // TFT식 특성 패널용 — 활성화 안 된 특성도 "다음 단계까지 남은 칸"과 함께 전부 보여준다.
        public static List<(string trait, int count, int nextThreshold, string desc, SynergyRank rank)> GetAllTraitProgress(List<MercenaryDefinition> roster)
        {
            Dictionary<string, int> counts = CountTraits(roster);

            var result = new List<(string trait, int count, int nextThreshold, string desc, SynergyRank rank)>();
            foreach (KeyValuePair<string, int> kv in counts)
            {
                if (!Tiers.TryGetValue(kv.Key, out (int count, string desc)[] tiers)) continue;

                int achievedIndex = GetActiveTierIndex(kv.Key, kv.Value);

                SynergyRank rank = achievedIndex >= 0 ? RankForTier(kv.Key, achievedIndex) : SynergyRank.None;
                string desc = achievedIndex >= 0 ? tiers[achievedIndex].desc : tiers[0].desc;
                int nextThreshold = achievedIndex + 1 < tiers.Length ? tiers[achievedIndex + 1].count : tiers[tiers.Length - 1].count;

                result.Add((kv.Key, kv.Value, nextThreshold, desc, rank));
            }

            return result.OrderByDescending(r => r.rank).ThenByDescending(r => r.count).ThenBy(r => r.trait).ToList();
        }
    }
}
