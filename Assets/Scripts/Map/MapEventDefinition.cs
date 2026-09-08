namespace StickmanOfWar.Map
{
    public enum EventEffectKind
    {
        None,
        GrantRandomRelic,
        SpendAllGoldThenGrantRelic,
        SwapRandomMercenary,
        DamageNextBattleBase,

        // --- 확장 효과 ---
        GainGold,                    // 금화 +EffectAmount
        GainGoldPerMerc,             // 금화 +(EffectAmount * 현재 병력 수)
        GainDice,                    // 주사위 +EffectAmount
        GainPartySlot,               // 스쿼드 슬롯 +1
        FortifyNextBattleBase,       // 다음 전투 기지를 EffectAmount만큼 강화(추가 체력)
        SpendAllGoldThenFortify,     // 전 재산 지불 후 다음 전투 기지 강화
        LoseHalfGoldThenGrantRelic,  // 금화 절반 지불 후 유물 획득
        ReleaseRandomMercForGold,    // 무작위 병력 방출, 대가로 금화(등급 * EffectAmount)
        SacrificeRandomMercForRelic, // 무작위 병력 희생, 대가로 유물
        TradeDiceForGold,            // 주사위 1개 지불, 금화 +EffectAmount
        TradeGoldForDice,            // 금화 EffectAmount 지불, 주사위 +2
        GambleGold,                  // 50%: 금화 2배 / 50%: 전액 손실
        GambleRelicOrCurse,          // 60%: 유물 획득 / 40%: 다음 전투 기지 손상(EffectAmount)
        GambleFortifyOrDamage,       // 50%: 기지 강화(EffectAmount) / 50%: 기지 손상(EffectAmount)
    }

    public class MapEventDefinition
    {
        public string Id;
        public string Title;
        public string Body;
        public bool IsBinaryChoice;

        public string Choice1Label;
        public EventEffectKind Choice1Effect;
        public float Choice1Amount;
        public string Choice1ResultBody;

        public string Choice2Label;
        public EventEffectKind Choice2Effect;
        public float Choice2Amount;
        public string Choice2ResultBody;

        // 단일 효과 이벤트용
        public float EffectAmount;
        public string ResultBody;

        // 단일 효과 이벤트 (resultBody는 선택 - 없으면 효과별 기본 문구 사용)
        public MapEventDefinition(string id, string title, string body,
            EventEffectKind effect, float effectAmount = 0f, string resultBody = null)
        {
            Id = id;
            Title = title;
            Body = body;
            IsBinaryChoice = false;
            Choice1Effect = effect;
            EffectAmount = effectAmount;
            ResultBody = resultBody;
        }

        // 2지선다 이벤트 - 기존 시그니처 유지 (선택지별 수치/결과문구 없음)
        public MapEventDefinition(string id, string title, string body,
            string choice1Label, EventEffectKind choice1Effect,
            string choice2Label, EventEffectKind choice2Effect)
            : this(id, title, body,
                   choice1Label, choice1Effect, 0f, null,
                   choice2Label, choice2Effect, 0f, null)
        {
        }

        // 2지선다 이벤트 - 선택지별 수치 + 결과 문구 지정
        public MapEventDefinition(string id, string title, string body,
            string choice1Label, EventEffectKind choice1Effect, float choice1Amount, string choice1ResultBody,
            string choice2Label, EventEffectKind choice2Effect, float choice2Amount, string choice2ResultBody)
        {
            Id = id;
            Title = title;
            Body = body;
            IsBinaryChoice = true;
            Choice1Label = choice1Label;
            Choice1Effect = choice1Effect;
            Choice1Amount = choice1Amount;
            Choice1ResultBody = choice1ResultBody;
            Choice2Label = choice2Label;
            Choice2Effect = choice2Effect;
            Choice2Amount = choice2Amount;
            Choice2ResultBody = choice2ResultBody;
        }
    }
}
