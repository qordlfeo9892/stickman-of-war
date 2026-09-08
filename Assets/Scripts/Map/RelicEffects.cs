using UnityEngine;

namespace StickmanOfWar.Map
{
    // 가방에 "배치된" 유물들의 효과를 전투 보정치로 환산한다.
    // RelicDatabase 의 EffectDescription(표시용 문구)과 1:1로 대응 — 문구를 바꾸면 여기도 바꿀 것.
    public static class RelicEffects
    {
        public static SynergyCombatBonus GetActiveBonus(RelicBag bag)
        {
            SynergyCombatBonus b = SynergyCombatBonus.Identity;
            if (bag?.Placed == null) return b;

            foreach (PlacedRelic placed in bag.Placed)
            {
                RelicDefinition def = RelicDatabase.GetById(placed.RelicId);
                if (def == null) continue;
                Apply(ref b, def.Id);
            }
            return b;
        }

        private static void Apply(ref SynergyCombatBonus b, string relicId)
        {
            switch (relicId)
            {
                case "ring_flame":          b.DamageMult *= 1.10f; break;        // 공격력 +10%
                case "giant_gauntlet":      b.DamageMult *= 1.20f; break;        // 공격력 +20%
                case "twin_daggers":        b.AttackSpeedMult *= 1.08f; break;   // 공격속도 +8%
                case "silver_belt":         b.MoveSpeedMult *= 1.10f; break;     // 이동속도 +10%
                case "long_spear":          b.RangeMult *= 1.15f; break;         // 사거리 +15%
                case "chain_armor":         b.MaxHealthMult *= 1.15f; break;     // 최대 체력 +15%
                case "dragon_heart":        b.MaxHealthMult *= 1.25f; break;     // 최대 체력 +25%
                case "amulet_frost":        b.DamageTakenMult *= 0.95f; break;   // 받는 피해 -5%
                case "ancient_shield":      b.DamageTakenMult *= 0.85f; break;   // 받는 피해 -15%
                case "raven_claw":          b.CritChanceAdd += 0.10f; break;     // 치명타 확률 +10%
                case "ancient_rune":        b.GoldGainMult *= 1.15f; break;      // 골드 획득 +15%
                case "tangled_chains":      b.EnemyMoveSpeedMult *= 0.85f; break;// 적 이동속도 감소
                case "shadow_cloak":        b.DodgeFrac += 0.10f; break;         // 회피율 +10%
                case "tree_of_life":        b.HealthRegenPerSec += 2f; break;    // 초당 체력 재생 +2

                case "scales_of_judgement": // 체력 10% 이하 적 처형 확률 증가
                    if (b.ExecuteThreshold < 0.10f) b.ExecuteThreshold = 0.10f;
                    break;

                case "chaos_orb":           ApplyChaos(ref b); break;            // 무작위 효과 대폭 상승

                // "뒤틀린 지팡이"(스킬 피해 +10%)·"뿌리내린 지팡이"(마나 회복 +10%)는
                // 아직 스킬/마나 시스템이 전투에 없어 미적용. 해당 시스템 추가 시 여기서 반영할 것.
            }
        }

        private static void ApplyChaos(ref SynergyCombatBonus b)
        {
            switch (Random.Range(0, 5))
            {
                case 0:  b.DamageMult *= 1.5f; break;
                case 1:  b.AttackSpeedMult *= 1.5f; break;
                case 2:  b.MaxHealthMult *= 1.5f; break;
                case 3:  b.DamageTakenMult *= 0.6f; break;
                default: b.MoveSpeedMult *= 1.4f; break;
            }
        }
    }
}
