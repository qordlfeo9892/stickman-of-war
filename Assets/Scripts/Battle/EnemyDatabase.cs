using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StickmanOfWar.Battle
{
    // 막(Act)별 적 풀. 요구 규격: 막당 일반 8 / 엘리트 3 / 보스 3.
    // 보스의 Id·이름은 Map.BossDatabase 와 1:1로 일치시켜, 맵에서 뽑힌 보스가 그대로 전투에 등장하도록 한다.
    public static class EnemyDatabase
    {
        // 막별 기준치(일반 "grunt" 환산): 체력 / 공격력 / 이동속도
        // 밸런스 2차 조정: "너무 어려워졌다" 피드백으로 직전 상향분을 절반쯤 되돌림 (체력/공격력 -12%, 이동속도는 유지).
        private static readonly (float hp, float dmg, float spd)[] ActBaseline =
        {
            (30f, 5.3f, 142f),  // Act 1
            (58f, 10.5f, 144f), // Act 2
            (109f, 16f, 146f),  // Act 3
        };

        // 등급 배수: (체력, 공격력)  ─ 밸런스 1차 조정: 엘리트를 살짝 낮춤
        private static (float hp, float dmg) RankMult(EnemyRank r) =>
            r == EnemyRank.Elite ? (3.0f, 1.55f) :
            r == EnemyRank.Boss ? (14f, 3.0f) :
            (1f, 1f);

        private static float RankSize(EnemyRank r) =>
            r == EnemyRank.Elite ? 1.28f : r == EnemyRank.Boss ? 1.45f : 1f;

        // 아키타입 프로필: 모양, 체력배수, 공격배수, 공격간격(초), 사거리, 이동속도배수
        // 밸런스 1차 조정: runner 감속, swarm 생존력 소폭↑, brute/tank/caster 미세 조정
        private static (EnemyShape shape, float hpM, float dmgM, float intr, float rng, float spdM) Archetype(string a)
        {
            switch (a)
            {
                case "runner":     return (EnemyShape.Runner,     0.55f, 0.70f, 0.82f, 60f,  1.45f);
                case "skirmisher": return (EnemyShape.Skirmisher, 0.80f, 0.90f, 1.00f, 150f, 1.12f);
                case "bruiser":    return (EnemyShape.Bruiser,    1.50f, 1.20f, 1.05f, 72f,  1.00f);
                case "caster":     return (EnemyShape.Caster,     0.70f, 1.15f, 1.35f, 190f, 0.90f);
                case "swarm":      return (EnemyShape.Swarm,      0.48f, 0.55f, 0.72f, 55f,  1.25f);
                case "brute":      return (EnemyShape.Brute,      2.20f, 1.60f, 1.40f, 78f,  0.70f);
                case "tank":       return (EnemyShape.Tank,       3.00f, 1.00f, 1.50f, 65f,  0.55f);
                case "horned":     return (EnemyShape.Horned,     1.30f, 1.35f, 1.00f, 82f,  1.05f);
                case "colossus":   return (EnemyShape.Colossus,   1.00f, 1.00f, 1.40f, 95f,  0.60f);
                default:           return (EnemyShape.Grunt,      1.00f, 1.00f, 1.00f, 70f,  1.00f);
            }
        }

        private static Vector2 BaseSize(EnemyShape s)
        {
            switch (s)
            {
                case EnemyShape.Runner:     return new Vector2(34f, 74f);
                case EnemyShape.Skirmisher: return new Vector2(44f, 66f);
                case EnemyShape.Bruiser:    return new Vector2(58f, 74f);
                case EnemyShape.Caster:     return new Vector2(50f, 64f);
                case EnemyShape.Swarm:      return new Vector2(30f, 40f);
                case EnemyShape.Brute:      return new Vector2(76f, 92f);
                case EnemyShape.Tank:       return new Vector2(80f, 72f);
                case EnemyShape.Horned:     return new Vector2(64f, 86f);
                case EnemyShape.Colossus:   return new Vector2(108f, 134f);
                default:                    return new Vector2(46f, 66f);
            }
        }

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;
        }

        private static EnemyDefinition Make(string id, string name, int act, EnemyRank rank,
            string archetype, string bodyHex, string accentHex)
        {
            (float hp, float dmg, float spd) baseStat = ActBaseline[Mathf.Clamp(act, 1, 3) - 1];
            var arch = Archetype(archetype);
            (float hpR, float dmgR) = RankMult(rank);

            float hp = baseStat.hp * arch.hpM * hpR;
            float dmg = baseStat.dmg * arch.dmgM * dmgR;
            float spd = baseStat.spd * arch.spdM;
            Vector2 size = BaseSize(arch.shape) * RankSize(rank);

            return new EnemyDefinition(id, name, act, rank,
                Mathf.Round(hp), Mathf.Round(dmg), arch.intr, arch.rng, Mathf.Round(spd),
                arch.shape, Hex(bodyHex), Hex(accentHex), size);
        }

        // 보스: 패턴별로 체력/공격 배수와 승리 조건이 크게 달라지므로 전용 빌더를 쓴다.
        private static EnemyDefinition MakeBoss(string id, string name, int act,
            string archetype, string bodyHex, string accentHex, BossPattern pattern)
        {
            (float hp, float dmg, float spd) baseStat = ActBaseline[Mathf.Clamp(act, 1, 3) - 1];
            var arch = Archetype(archetype);

            // 패턴별 (체력배수, 공격배수, 처치필요횟수, 재등장딜레이, 간격배수, 이동속도배수)
            float hpM, dmgM, intrM, spdM, respawn;
            int defeatsToWin;
            switch (pattern)
            {
                case BossPattern.Juggernaut: // 거대 단일체 — 두껍고 느리고 아프다. 처치 즉시 승리
                    hpM = 15f; dmgM = 2.6f; intrM = 1.6f; spdM = 0.5f; defeatsToWin = 1; respawn = 0f;
                    break;
                case BossPattern.Relentless: // 저체력·고공격 단일 유닛이 연속으로 되살아남 — 4회 처치 시 승리
                    hpM = 1.0f; dmgM = 1.6f; intrM = 1.0f; spdM = 0.75f; defeatsToWin = 4; respawn = 4.5f;
                    break;
                case BossPattern.Summoner: // 표준 체급 + 잡몹 폭풍 소환. 기지 파괴로 승리
                    hpM = 6.5f; dmgM = 1.9f; intrM = 1.4f; spdM = 0.62f; defeatsToWin = 0; respawn = 0f;
                    break;
                default: // Vanguard — 강력한 선봉. 기지 파괴로 승리
                    hpM = 7.5f; dmgM = 2.1f; intrM = 1.35f; spdM = 0.6f; defeatsToWin = 0; respawn = 0f;
                    break;
            }

            var def = new EnemyDefinition(id, name, act, EnemyRank.Boss,
                Mathf.Round(baseStat.hp * arch.hpM * hpM),
                Mathf.Round(baseStat.dmg * arch.dmgM * dmgM),
                arch.intr * intrM, arch.rng,
                Mathf.Round(baseStat.spd * spdM),
                arch.shape, Hex(bodyHex), Hex(accentHex), BaseSize(arch.shape) * RankSize(EnemyRank.Boss));
            def.Pattern = pattern;
            def.DefeatsToWin = defeatsToWin;
            def.BossRespawnDelay = respawn;
            return def;
        }

        // ─────────────────────────── 일반 (막당 8) ───────────────────────────
        private static readonly EnemyDefinition[][] Normals =
        {
            // Act 1 — 숲 · 야수 · 도적
            new[]
            {
                Make("a1_n_wilddog",   "들개 병졸",   1, EnemyRank.Normal, "grunt",      "#8A6A44", "#5A4530"),
                Make("a1_n_scout",     "숲 정찰병",   1, EnemyRank.Normal, "runner",     "#6F9D55", "#C9E6A0"),
                Make("a1_n_thrower",   "투척 도적",   1, EnemyRank.Normal, "skirmisher", "#9A7F4A", "#D8C187"),
                Make("a1_n_boar",      "가시멧돼지",  1, EnemyRank.Normal, "bruiser",    "#7A5A3C", "#3F2D1E"),
                Make("a1_n_shaman",    "늪 주술사",   1, EnemyRank.Normal, "caster",     "#4F7D6A", "#9FE0C8"),
                Make("a1_n_spiders",   "독거미 무리", 1, EnemyRank.Normal, "swarm",      "#4A5540", "#8FAE6A"),
                Make("a1_n_bearman",   "곰 사냥꾼",   1, EnemyRank.Normal, "brute",      "#6B4A30", "#2F2016"),
                Make("a1_n_mossgolem", "이끼 골렘",   1, EnemyRank.Normal, "tank",       "#5E7048", "#38452B"),
            },
            // Act 2 — 강철 · 기계 · 그림자
            new[]
            {
                Make("a2_n_trooper",   "강철 보병",   2, EnemyRank.Normal, "grunt",      "#7C8894", "#4A535C"),
                Make("a2_n_drone",     "추적 드론",   2, EnemyRank.Normal, "runner",     "#6AA0C4", "#D0EEFF"),
                Make("a2_n_crossbow",  "쇠뇌 사수",   2, EnemyRank.Normal, "skirmisher", "#8A94A0", "#CFD8E0"),
                Make("a2_n_shieldman", "충격 방패병", 2, EnemyRank.Normal, "bruiser",    "#5F6B78", "#2E3640"),
                Make("a2_n_sparkmage", "전류 술사",   2, EnemyRank.Normal, "caster",     "#5F7FB0", "#BCD6FF"),
                Make("a2_n_nanobugs",  "나노 벌레떼", 2, EnemyRank.Normal, "swarm",      "#69737D", "#A9C4D6"),
                Make("a2_n_crusher",   "파쇄 골렘",   2, EnemyRank.Normal, "brute",      "#6B7075", "#33373B"),
                Make("a2_n_turret",    "공성 자동포", 2, EnemyRank.Normal, "tank",       "#586069", "#2B3138"),
            },
            // Act 3 — 심연 · 악마 · 종말
            new[]
            {
                Make("a3_n_fallen",    "타락한 병졸",   3, EnemyRank.Normal, "grunt",      "#6A4A72", "#3A2740"),
                Make("a3_n_stalker",   "그림자 추격자", 3, EnemyRank.Normal, "runner",     "#7A5AA0", "#C9A8FF"),
                Make("a3_n_hexbow",    "저주 궁수",     3, EnemyRank.Normal, "skirmisher", "#8A4A6A", "#E0A8C4"),
                Make("a3_n_hound",     "지옥견",        3, EnemyRank.Normal, "bruiser",    "#7A3A3A", "#2F1414"),
                Make("a3_n_priest",    "심연 사제",     3, EnemyRank.Normal, "caster",     "#5A3A8A", "#B394FF"),
                Make("a3_n_maggots",   "구더기 떼",     3, EnemyRank.Normal, "swarm",      "#5F4A5A", "#9A7F95"),
                Make("a3_n_giant",     "석화 거인",     3, EnemyRank.Normal, "brute",      "#5A4A5F", "#2A222E"),
                Make("a3_n_altar",     "종말 제단병",   3, EnemyRank.Normal, "tank",       "#4A3A55", "#241C2B"),
            },
        };

        // ─────────────────────────── 엘리트 (막당 3) ───────────────────────────
        private static readonly EnemyDefinition[][] Elites =
        {
            new[]
            {
                Make("a1_e_predator", "숲의 포식자",     1, EnemyRank.Elite, "horned", "#5A7A3A", "#D8FFA0"),
                Make("a1_e_ward",     "돌가죽 수호물",   1, EnemyRank.Elite, "tank",   "#6A6048", "#39331F"),
                Make("a1_e_archmage", "늪의 대주술사",   1, EnemyRank.Elite, "caster", "#3F8F77", "#B6FFE6"),
            },
            new[]
            {
                Make("a2_e_ripper",   "강철 파쇄기",     2, EnemyRank.Elite, "horned", "#6A7684", "#C0CCD8"),
                Make("a2_e_fortress", "이동 요새",       2, EnemyRank.Elite, "tank",   "#4F575F", "#262B30"),
                Make("a2_e_tesla",    "폭풍 방전탑",     2, EnemyRank.Elite, "caster", "#5A86C0", "#D6E8FF"),
            },
            new[]
            {
                Make("a3_e_butcher",  "심연의 도살자",   3, EnemyRank.Elite, "horned", "#7A3A5A", "#FFB0D0"),
                Make("a3_e_sarco",    "봉인된 석관",     3, EnemyRank.Elite, "tank",   "#4A3F55", "#221C2A"),
                Make("a3_e_prophet",  "공허의 예언자",   3, EnemyRank.Elite, "caster", "#6A3AA0", "#C9A8FF"),
            },
        };

        // ─────────────────────────── 보스 (막당 3, BossDatabase 와 Id/이름 일치) ───────────────────────────
        // 패턴이 골고루 섞이도록 배치: Juggernaut x2, Relentless x2, Summoner x3, Vanguard x2
        private static readonly EnemyDefinition[][] Bosses =
        {
            new[]
            {
                MakeBoss("act1_boss_a", "가시멧돼지 두목", 1, "colossus", "#7A5A3C", "#2F2016", BossPattern.Juggernaut),
                MakeBoss("act1_boss_b", "해골 궁수장",     1, "horned",   "#C8C0A8", "#5A5040", BossPattern.Summoner),
                MakeBoss("act1_boss_c", "늪지 거대괴수",   1, "colossus", "#3F7D6A", "#9FE0C8", BossPattern.Vanguard),
            },
            new[]
            {
                MakeBoss("act2_boss_a", "강철 골렘",       2, "colossus", "#6B7075", "#33373B", BossPattern.Juggernaut),
                MakeBoss("act2_boss_b", "그림자 암살자",   2, "horned",   "#3A3A4A", "#B0B0FF", BossPattern.Relentless),
                MakeBoss("act2_boss_c", "불타는 야수",     2, "colossus", "#B04A2A", "#FFD08A", BossPattern.Summoner),
            },
            new[]
            {
                MakeBoss("act3_boss_a", "타락한 황제",     3, "colossus", "#7A3A5A", "#FFD0E0", BossPattern.Summoner),
                MakeBoss("act3_boss_b", "심연의 군주",     3, "colossus", "#5A3A8A", "#B394FF", BossPattern.Vanguard),
                MakeBoss("act3_boss_c", "종말의 파수꾼",   3, "colossus", "#4A4A52", "#E0E0FF", BossPattern.Relentless),
            },
        };

        private static int ActIndex(int act) => Mathf.Clamp(act, 1, 3) - 1;

        public static EnemyDefinition[] GetNormalPool(int act) => Normals[ActIndex(act)];
        public static EnemyDefinition[] GetElitePool(int act) => Elites[ActIndex(act)];
        public static EnemyDefinition[] GetBossPool(int act) => Bosses[ActIndex(act)];

        public static EnemyDefinition GetRandomNormal(int act)
        {
            EnemyDefinition[] pool = GetNormalPool(act);
            return pool[Random.Range(0, pool.Length)];
        }

        public static EnemyDefinition GetRandomElite(int act)
        {
            EnemyDefinition[] pool = GetElitePool(act);
            return pool[Random.Range(0, pool.Length)];
        }

        public static EnemyDefinition GetById(string id)
        {
            foreach (EnemyDefinition[][] group in new[] { Normals, Elites, Bosses })
            {
                foreach (EnemyDefinition[] actPool in group)
                {
                    EnemyDefinition found = actPool.FirstOrDefault(e => e.Id == id);
                    if (found != null) return found;
                }
            }
            return null;
        }

        // 이번 전투가 뽑아 쓸 일반 적 "종류" 부분집합(다양성 제한). null/0 이면 전체 풀 사용.
        public static EnemyDefinition[] RollBattleSelection(int act, int distinctTypes)
        {
            EnemyDefinition[] pool = GetNormalPool(act);
            if (distinctTypes <= 0 || distinctTypes >= pool.Length) return pool;
            return pool.OrderBy(_ => Random.value).Take(distinctTypes).ToArray();
        }
    }
}
