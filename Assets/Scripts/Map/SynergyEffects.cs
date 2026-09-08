using System.Collections.Generic;
using UnityEngine;

namespace StickmanOfWar.Map
{
    // 전투에 실제로 적용되는 보정치 묶음. 시너지(SynergyEffects)와 유물(RelicEffects)이
    // 각각 이 형태로 값을 만든 뒤 SynergyCombatBonus.Combine 으로 합산된다.
    // TraitSynergyDatabase / RelicDatabase 의 "설명 문구"(UI 표시용)와 1:1로 대응하는 "수치"다.
    // 문구를 바꾸면 대응 표(Apply)도 같이 바꿀 것.
    public struct SynergyCombatBonus
    {
        public float DamageMult;        // 공격력 배수
        public float AttackSpeedMult;   // 공격 간격 /= 이 값 (>1 이면 더 빠름)
        public float RangeMult;         // 사거리 배수
        public float MoveSpeedMult;     // 이동속도 배수
        public float MaxHealthMult;     // 최대 체력 배수
        public float DamageTakenMult;   // 받는 피해 배수 (<1 이면 감소)
        public float FlatArmor;         // 피격 1회당 고정 차감
        public float LifestealFrac;     // 가한 피해 * 이 값 만큼 자가 회복
        public float DodgeFrac;         // 피격을 완전히 무효화할 확률
        public float ExecuteThreshold;  // 대상 체력 비율이 이 값 이하이면 즉시 처치
        public float ShieldFracOfMaxHp; // 소환 시 최대 체력 * 이 값 만큼 보호막 부여
        public float GoldGainMult;      // 전투 승리 보상 골드 배수
        public float CritChanceAdd;     // 치명타 확률 (가산, 0~1)
        public float CritDamageAdd;     // 치명타 피해 가산 (기본 1.5배에 더해짐)
        public float HealthRegenPerSec; // 초당 체력 재생
        public float EnemyMoveSpeedMult;// 적 유닛 이동속도 배수 (<1 이면 둔화)

        public static SynergyCombatBonus Identity => new SynergyCombatBonus
        {
            DamageMult = 1f,
            AttackSpeedMult = 1f,
            RangeMult = 1f,
            MoveSpeedMult = 1f,
            MaxHealthMult = 1f,
            DamageTakenMult = 1f,
            FlatArmor = 0f,
            LifestealFrac = 0f,
            DodgeFrac = 0f,
            ExecuteThreshold = 0f,
            ShieldFracOfMaxHp = 0f,
            GoldGainMult = 1f,
            CritChanceAdd = 0f,
            CritDamageAdd = 0f,
            HealthRegenPerSec = 0f,
            EnemyMoveSpeedMult = 1f,
        };

        // 배수 계열은 곱하고, 가산 계열은 더하고, 처형 문턱은 더 큰 쪽을 취한다.
        public static SynergyCombatBonus Combine(SynergyCombatBonus a, SynergyCombatBonus b)
        {
            return new SynergyCombatBonus
            {
                DamageMult = a.DamageMult * b.DamageMult,
                AttackSpeedMult = a.AttackSpeedMult * b.AttackSpeedMult,
                RangeMult = a.RangeMult * b.RangeMult,
                MoveSpeedMult = a.MoveSpeedMult * b.MoveSpeedMult,
                MaxHealthMult = a.MaxHealthMult * b.MaxHealthMult,
                DamageTakenMult = a.DamageTakenMult * b.DamageTakenMult,
                FlatArmor = a.FlatArmor + b.FlatArmor,
                LifestealFrac = a.LifestealFrac + b.LifestealFrac,
                DodgeFrac = a.DodgeFrac + b.DodgeFrac,
                ExecuteThreshold = Mathf.Max(a.ExecuteThreshold, b.ExecuteThreshold),
                ShieldFracOfMaxHp = a.ShieldFracOfMaxHp + b.ShieldFracOfMaxHp,
                GoldGainMult = a.GoldGainMult * b.GoldGainMult,
                CritChanceAdd = a.CritChanceAdd + b.CritChanceAdd,
                CritDamageAdd = a.CritDamageAdd + b.CritDamageAdd,
                HealthRegenPerSec = a.HealthRegenPerSec + b.HealthRegenPerSec,
                EnemyMoveSpeedMult = a.EnemyMoveSpeedMult * b.EnemyMoveSpeedMult,
            };
        }

        // 극단값 방지용 상한/하한.
        public SynergyCombatBonus Clamped()
        {
            SynergyCombatBonus c = this;
            if (c.DodgeFrac > 0.6f) c.DodgeFrac = 0.6f;
            if (c.LifestealFrac > 0.5f) c.LifestealFrac = 0.5f;
            if (c.CritChanceAdd > 1f) c.CritChanceAdd = 1f;
            if (c.DamageTakenMult < 0.1f) c.DamageTakenMult = 0.1f;
            if (c.EnemyMoveSpeedMult < 0.2f) c.EnemyMoveSpeedMult = 0.2f;
            return c;
        }
    }

    public static class SynergyEffects
    {
        private const float MaxDodge = 0.6f;
        private const float MaxLifesteal = 0.5f;

        // 현재 로스터로 활성화된 시너지 단계를 모아 전투 보정치로 환산한다.
        // 단계 판정/유닛 수 집계는 TraitSynergyDatabase 를 그대로 재사용한다.
        public static SynergyCombatBonus GetActiveBonus(List<MercenaryDefinition> roster)
        {
            SynergyCombatBonus b = SynergyCombatBonus.Identity;
            if (roster == null || roster.Count == 0) return b;

            foreach (var p in TraitSynergyDatabase.GetAllTraitProgress(roster))
            {
                if (p.rank == SynergyRank.None) continue;
                // 랭크가 아니라 "달성한 단계 인덱스"로 효과를 적용한다
                // (특성마다 임계값·랭크 오버라이드가 달라 rank→tier 환산은 부정확).
                int tier = TraitSynergyDatabase.GetActiveTierIndex(p.trait, p.count);
                if (tier < 0) continue;
                Apply(ref b, p.trait, tier);
            }

            if (b.DodgeFrac > MaxDodge) b.DodgeFrac = MaxDodge;
            if (b.LifestealFrac > MaxLifesteal) b.LifestealFrac = MaxLifesteal;
            return b;
        }

        // tier: 달성한 단계 인덱스(0-base). 특성마다 임계값이 다르다
        //  (종족 3/6/9, 광기·냉혹·탐욕·충직 2/4/6, 정의·파괴 3/6/9, 고독=정확히1, 심판자=정확히4).
        // 설명 문구가 각 단계에서 절대값이므로 하위 단계를 누적하지 않고 달성 단계 하나만 적용한다.
        private static void Apply(ref SynergyCombatBonus b, string trait, int tier)
        {
            switch (trait)
            {
                // ───────── 종족 ─────────
                case "인간": // 방어력 (+ 이동속도)
                    b.FlatArmor += tier == 0 ? 10f : tier == 1 ? 20f : 35f;
                    if (tier == 1) b.MoveSpeedMult *= 1.05f;
                    else if (tier == 2) b.MoveSpeedMult *= 1.10f;
                    break;

                case "드워프": // 최대 체력 / 받는 피해
                    if (tier == 0) b.MaxHealthMult *= 1.15f;
                    else if (tier == 1) b.DamageTakenMult *= 0.90f;
                    else { b.DamageTakenMult *= 0.80f; b.MaxHealthMult *= 1.30f; }
                    break;

                case "엘프": // 사거리 / 공격속도
                    if (tier == 0) b.RangeMult *= 1.20f;
                    else if (tier == 1) b.AttackSpeedMult *= 1.15f;
                    else { b.RangeMult *= 1.40f; b.AttackSpeedMult *= 1.30f; }
                    break;

                case "언데드": // 공격력 / 생명력 흡수
                    b.DamageMult *= tier == 0 ? 1.10f : tier == 1 ? 1.20f : 1.35f;
                    if (tier == 1) b.LifestealFrac += 0.10f;
                    else if (tier == 2) b.LifestealFrac += 0.20f;
                    break;

                case "오크": // 공격력 / 공격속도
                    if (tier == 0) b.DamageMult *= 1.15f;
                    else if (tier == 1) b.AttackSpeedMult *= 1.20f;
                    else { b.DamageMult *= 1.30f; b.AttackSpeedMult *= 1.35f; }
                    break;

                case "악마": // 생명력 흡수
                    b.LifestealFrac += tier == 0 ? 0.05f : tier == 1 ? 0.10f : 0.20f;
                    break;

                // ───────── 성격 : 2의 배수(2/4/6) ─────────
                case "광기": // 공격속도
                    b.AttackSpeedMult *= tier == 0 ? 1.12f : tier == 1 ? 1.25f : 1.42f;
                    break;

                case "냉혹": // 치명타 피해 (치명타 확률은 유물 등 다른 곳에서 확보)
                    b.CritDamageAdd += tier == 0 ? 0.18f : tier == 1 ? 0.35f : 0.60f;
                    break;

                case "탐욕": // 골드 획득
                    b.GoldGainMult *= tier == 0 ? 1.10f : tier == 1 ? 1.18f : 1.30f;
                    break;

                case "충직": // 아군 보호막
                    b.ShieldFracOfMaxHp += tier == 0 ? 0.08f : tier == 1 ? 0.16f : 0.28f;
                    break;

                // ───────── 성격 : 3의 배수(3/6/9) ─────────
                case "정의": // 받는 피해 감소
                    b.DamageTakenMult *= tier == 0 ? 0.90f : tier == 1 ? 0.80f : 0.65f;
                    break;

                case "파괴": // 공격력
                    b.DamageMult *= tier == 0 ? 1.10f : tier == 1 ? 1.20f : 1.35f;
                    break;

                // ───────── 특수 : 정확히 N명일 때만 ─────────
                case "고독": // 정확히 1명 — 홀로 싸울 때 큰 보정 (2명 이상이면 비활성)
                    b.DodgeFrac += 0.30f;
                    b.MoveSpeedMult *= 1.15f;
                    break;

                case "심판자": // 정확히 4명 — 강력한 처형
                    if (0.20f > b.ExecuteThreshold) b.ExecuteThreshold = 0.20f;
                    break;
            }
        }
    }
}
