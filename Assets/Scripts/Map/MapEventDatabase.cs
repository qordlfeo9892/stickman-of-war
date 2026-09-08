using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StickmanOfWar.Map
{
    public static class MapEventDatabase
    {
        private static readonly MapEventDefinition[] All =
        {
            // ================= 유물 발견 =================
            new MapEventDefinition("old_chest", "낡은 상자",
                "먼지 쌓인 낡은 상자 안에서 무언가 반짝인다.",
                EventEffectKind.GrantRandomRelic),

            new MapEventDefinition("shining_altar", "빛나는 제단",
                "숲 속 작은 제단이 은은한 빛을 낸다. 손을 대자 유물이 나타났다.",
                EventEffectKind.GrantRandomRelic),

            // ============ 전 재산 헌납 → 유물 ============
            new MapEventDefinition("passing_fanatic", "지나가던 광신도",
                "당신은 지나가던 광신도를 만났다.",
                "같이 예배한다", EventEffectKind.SpendAllGoldThenGrantRelic,
                "무시하고 지나간다", EventEffectKind.None),

            new MapEventDefinition("wandering_priest", "떠돌이 사제",
                "떠돌이 사제가 다가와 기부를 요청한다.",
                "가진 돈을 전부 기부한다", EventEffectKind.SpendAllGoldThenGrantRelic,
                "거절한다", EventEffectKind.None),

            new MapEventDefinition("shady_merchant", "수상한 상인",
                "수상한 상인이 목소리를 낮추고 특별한 물건이 있다며 속삭인다.",
                "전 재산을 걸고 산다", EventEffectKind.SpendAllGoldThenGrantRelic,
                "자리를 뜬다", EventEffectKind.None),

            // ================ 용병 교체 ================
            new MapEventDefinition("wandering_mercenary", "지나가던 용병의 제안",
                "지나가던 용병이 자신이 큰 도움이 될 거라며 파티에 가입하고 싶어한다.",
                EventEffectKind.SwapRandomMercenary),

            new MapEventDefinition("deserter", "지친 탈영병",
                "지친 기색의 탈영병이 파티에 자리를 요청한다.",
                EventEffectKind.SwapRandomMercenary),

            // ============ 다음 전투 페널티 ============
            new MapEventDefinition("flood", "홍수",
                "갑작스런 홍수가 들이닥쳤다! 기지가 침수되어 다음 전투는 손상된 채로 시작한다.",
                EventEffectKind.DamageNextBattleBase, 20f),

            new MapEventDefinition("landslide", "산사태",
                "산사태가 진영을 덮쳤다. 다음 전투에서 기지가 약해진 채로 시작한다.",
                EventEffectKind.DamageNextBattleBase, 25f),

            new MapEventDefinition("plague", "정체불명의 역병",
                "정체불명의 역병이 퍼져 병사들의 사기가 떨어졌다. 다음 전투에서 기지 방어가 약해진다.",
                EventEffectKind.DamageNextBattleBase, 15f),

            // ================ 재화 획득 ================
            new MapEventDefinition("supply_wagon", "버려진 보급 마차",
                "길가에 부서진 보급 마차가 뒹군다. 짐칸을 뒤지자 군자금 주머니가 나왔다.",
                EventEffectKind.GainGold, 45f, "군자금 45를 챙겼다."),

            new MapEventDefinition("lost_caravan", "길 잃은 상단",
                "길을 잃은 상단이 호위를 부탁한다. 안전한 길목까지 데려다주자 병력 수만큼 사례한다.",
                EventEffectKind.GainGoldPerMerc, 15f),

            new MapEventDefinition("gamblers_bones", "도박꾼의 유해",
                "말라붙은 도박꾼의 유해 곁에 상아 주사위 한 쌍이 굴러다닌다.",
                EventEffectKind.GainDice, 2f, "주사위 2개를 챙겼다."),

            new MapEventDefinition("conscription_banner", "낡은 징집 깃발",
                "전장에 반쯤 파묻힌 징집 깃발이 있다. 다시 세워 흔들자 떠돌이 무사들이 모여든다.",
                EventEffectKind.GainPartySlot, 0f, "스쿼드 슬롯이 하나 늘었다."),

            new MapEventDefinition("divine_spring", "신성한 샘",
                "맑은 샘물을 마신 병사들의 눈빛이 달라졌다.",
                EventEffectKind.FortifyNextBattleBase, 40f,
                "사기가 치솟는다. 다음 전투를 강화된 기지로 시작한다. (+40)"),

            // ================== 도박 ==================
            new MapEventDefinition("wheel_of_fate", "운명의 수레바퀴",
                "낡은 도박대 위, 녹슨 수레바퀴가 돌아가길 기다린다. 전 재산을 걸어야 한다.",
                "수레바퀴를 돌린다", EventEffectKind.GambleGold, 0f, null,
                "돌리지 않는다", EventEffectKind.None, 0f, null),

            new MapEventDefinition("golden_idol", "황금 우상",
                "받침대 위에 놓인 황금 우상이 눈부시게 빛난다. 왠지 함정 냄새가 난다.",
                "우상을 챙긴다", EventEffectKind.GambleRelicOrCurse, 25f, null,
                "제자리에 둔다", EventEffectKind.None, 0f, null),

            new MapEventDefinition("living_wall", "살아있는 벽",
                "복도 끝의 벽이 미묘하게 숨 쉬듯 움직인다.",
                "벽을 더듬어 본다", EventEffectKind.GambleRelicOrCurse, 20f, null,
                "조용히 물러난다", EventEffectKind.None, 0f, null),

            new MapEventDefinition("old_mausoleum", "잊혀진 지하묘소",
                "먼지 쌓인 석관 하나가 안치되어 있다. 부장품이 있을지도 모른다.",
                "석관을 연다", EventEffectKind.GambleRelicOrCurse, 25f, null,
                "예를 갖추고 떠난다", EventEffectKind.GainDice, 1f, "떠나기 전 제단에서 주사위 1개를 주웠다."),

            new MapEventDefinition("mushroom_hollow", "버섯 골짜기",
                "골짜기 가득 형형색색의 버섯이 자라 있다. 먹음직스러운 것도, 수상한 것도 섞여 있다.",
                "버섯을 먹는다", EventEffectKind.GambleFortifyOrDamage, 30f, null,
                "그냥 지나친다", EventEffectKind.None, 0f, null),

            // ================== 거래 ==================
            new MapEventDefinition("field_smith", "야전 대장장이",
                "야전 대장장이가 놀음빚을 갚아야 한다며 당신의 주사위를 사고 싶어한다.",
                "주사위 하나를 넘긴다", EventEffectKind.TradeDiceForGold, 45f, null,
                "거절한다", EventEffectKind.None, 0f, null),

            new MapEventDefinition("money_changer", "환전상",
                "떠돌이 환전상이 금화 35를 행운의 주사위 2개로 바꿔주겠다고 한다.",
                "금화 35를 바꾼다", EventEffectKind.TradeGoldForDice, 35f, null,
                "거절한다", EventEffectKind.None, 0f, null),

            new MapEventDefinition("wandering_sapper", "떠돌이 공병",
                "떠돌이 공병이 삯만 주면 다음 전장의 방벽을 손봐주겠다고 한다.",
                "전 재산을 지불한다", EventEffectKind.SpendAllGoldThenFortify, 60f, null,
                "사양한다", EventEffectKind.None, 0f, null),

            new MapEventDefinition("smuggler_offer", "밀수꾼의 제안",
                "밀수꾼이 재산의 절반이면 창고의 귀한 물건 하나를 넘기겠다고 한다.",
                "절반을 지불한다", EventEffectKind.LoseHalfGoldThenGrantRelic, 0f, null,
                "거절한다", EventEffectKind.None, 0f, null),

            // ============== 용병 처분 ==============
            new MapEventDefinition("deserter_ransom", "탈영병의 흥정",
                "붙잡힌 탈영병이 몸값을 낼 테니 조용히 보내달라고 애원한다.",
                "몸값을 받고 보내준다", EventEffectKind.ReleaseRandomMercForGold, 25f, null,
                "대열에 다시 세운다", EventEffectKind.None, 0f, "탈영병은 마지못해 대열로 돌아갔다."),

            new MapEventDefinition("bonfire_spirit", "모닥불의 정령",
                "모닥불 속에서 정령이 속삭인다. 동료 하나를 불길에 바치면 유물을 주겠다고.",
                "동료 하나를 제물로 바친다", EventEffectKind.SacrificeRandomMercForRelic, 0f, null,
                "불을 끈다", EventEffectKind.None, 0f, null),
        };

        // 직전에 등장한 이벤트가 곧바로 다시 나오지 않도록 최근 몇 개를 기억한다.
        private static readonly List<string> recentIds = new List<string>();
        private const int RecentMemory = 5;

        public static MapEventDefinition GetRandom()
        {
            MapEventDefinition[] pool = All.Where(e => !recentIds.Contains(e.Id)).ToArray();
            if (pool.Length == 0) pool = All;

            MapEventDefinition pick = pool[Random.Range(0, pool.Length)];

            recentIds.Add(pick.Id);
            if (recentIds.Count > RecentMemory) recentIds.RemoveAt(0);
            return pick;
        }
    }
}
