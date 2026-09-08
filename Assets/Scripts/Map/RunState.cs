using System.Collections.Generic;

namespace StickmanOfWar.Map
{
    public static class RunState
    {
        public const int MaxPartySizeCap = 10;
        public const int MaxBagDimension = 7;
        public const int StartingDiceCount = 3;

        public static int CurrentAct { get; private set; }
        public static MapGraph CurrentMap { get; private set; }
        public static int? CurrentNodeId { get; set; }

        public static int PartySizeCap { get; private set; } = 3;
        public static int PartySizeCurrent => Roster.Count;

        public static int DiceCount { get; set; }
        public static int Gold { get; set; }
        public static List<RosterMember> Roster { get; private set; } = new List<RosterMember>();

        public static RelicBag Bag { get; private set; } = new RelicBag();
        public static List<string> PendingRelicIds { get; private set; } = new List<string>();
        public static float NextBattleBaseDamage { get; set; }

        // 일반 전투 승리 시 확률로 제안되는 용병 (맵 복귀 시 영입/넘기기 선택). 없으면 null.
        public static string PendingMercOfferId { get; set; }

        public static bool HasActiveRun => CurrentMap != null;

        public static void StartNewRun()
        {
            CurrentAct = 1;
            PartySizeCap = 3;
            CurrentNodeId = null;
            CurrentMap = MapGenerator.Generate(CurrentAct);
            DiceCount = StartingDiceCount;
            Gold = 0;
            Roster = new List<RosterMember>();
            Bag = new RelicBag { Width = 3, Height = 3 };
            PendingRelicIds = new List<string>();
            NextBattleBaseDamage = 0f;
            PendingMercOfferId = null;
            SaveSystem.Save();
        }

        public static void LoadState(int act, MapGraph map, List<RosterMember> roster, int dice, int gold,
            int partySizeCap, RelicBag bag, List<string> pendingRelicIds, float nextBattleBaseDamage)
        {
            CurrentAct = act;
            CurrentMap = map;
            CurrentNodeId = null;
            Roster = roster;
            DiceCount = dice;
            Gold = gold;
            PartySizeCap = partySizeCap;
            Bag = bag;
            PendingRelicIds = pendingRelicIds;
            NextBattleBaseDamage = nextBattleBaseDamage;
        }

        public static void CompleteCurrentNode()
        {
            if (CurrentNodeId.HasValue)
            {
                CompleteNode(CurrentNodeId.Value);
            }
            CurrentNodeId = null;
            SaveSystem.Save();
        }

        public static void CompleteNode(int nodeId)
        {
            if (CurrentMap != null && CurrentMap.Nodes.TryGetValue(nodeId, out MapNode node))
            {
                node.IsCompleted = true;
                CurrentMap.CurrentPositionNodeId = nodeId;
            }
        }

        public static bool IsBossNodeCompleted()
        {
            return CurrentMap != null && CurrentMap.Nodes[CurrentMap.BossNodeId].IsCompleted;
        }

        public static void AdvanceToNextAct()
        {
            CurrentAct++;
            CurrentNodeId = null;
            CurrentMap = MapGenerator.Generate(CurrentAct);
            SaveSystem.Save();
        }

        public static bool IsFinalActBossCleared()
        {
            return CurrentAct >= 3 && IsBossNodeCompleted();
        }

        public static void IncreasePartySizeCap()
        {
            if (PartySizeCap < MaxPartySizeCap) PartySizeCap++;
            SaveSystem.Save();
        }

        public static void IncreaseBagWidth()
        {
            if (Bag.Width < MaxBagDimension) Bag.Width++;
            SaveSystem.Save();
        }

        public static void IncreaseBagHeight()
        {
            if (Bag.Height < MaxBagDimension) Bag.Height++;
            SaveSystem.Save();
        }

        public static bool CanHireMercenary()
        {
            return PartySizeCurrent < PartySizeCap;
        }

        // 이미 보유한 용병인지 (진화 판단용).
        public static bool HasMercenary(string mercId)
        {
            return Roster.Exists(m => m.Definition.Id == mercId);
        }

        // 보유 중인 별 등급. 0이면 미보유 — UI에서 "영입/진화/환전" 버튼 문구를 정할 때 쓴다.
        public static int GetMercenaryStar(string mercId)
        {
            RosterMember m = Roster.Find(r => r.Definition.Id == mercId);
            return m?.Star ?? 0;
        }

        public enum HireResult
        {
            Added,      // 새 슬롯에 영입
            Evolved,    // 이미 있던 용병이라 슬롯 소모 없이 2성으로 진화
            Overflowed, // 이미 2성 상한이라 골드로 환전됨
            NoRoom,     // 새 용병인데 파티 정원이 가득 참
        }

        // 영입/진화 공용 처리 — 슬롯을 쓰는지 여부와 무관하게 여기서만 Roster를 변경한다.
        // 같은 id를 이미 보유했으면 정원과 무관하게 진화(또는 상한 초과 시 골드 환전)하고,
        // 완전히 새 용병일 때만 정원을 확인한다 (requireRoom=false는 호출부에서 이미 확인한 경우).
        private static HireResult AddOrEvolve(MercenaryDefinition merc, bool requireRoom)
        {
            if (merc == null) return HireResult.NoRoom;

            RosterMember existing = Roster.Find(m => m.Definition.Id == merc.Id);
            if (existing != null)
            {
                if (existing.Star < RosterMember.MaxStar)
                {
                    existing.Star++;
                    SaveSystem.Save();
                    return HireResult.Evolved;
                }

                // 이미 2성 상한 — 3장째부터는 로스터에 넣지 않고 소환 비용의 2배를 골드로 환전.
                Gold += merc.DeployCost * 2;
                SaveSystem.Save();
                return HireResult.Overflowed;
            }

            if (requireRoom && !CanHireMercenary()) return HireResult.NoRoom;

            Roster.Add(new RosterMember(merc));
            SaveSystem.Save();
            return HireResult.Added;
        }

        public static bool TryHireMercenaryFree(MercenaryDefinition merc)
        {
            return AddOrEvolve(merc, requireRoom: true) != HireResult.NoRoom;
        }

        // 정원 확인 없이 로스터에 추가/진화 (전투 후 용병 제안에서 "교체" 선택 시 등 — 이미 자리를 비워둔 경우).
        public static void AddMercenaryUnchecked(MercenaryDefinition merc)
        {
            AddOrEvolve(merc, requireRoom: false);
        }

        public static void SetPendingMercOffer(string mercId)
        {
            PendingMercOfferId = mercId;
            SaveSystem.Save();
        }

        public static void ClearPendingMercOffer()
        {
            PendingMercOfferId = null;
            SaveSystem.Save();
        }

        public static bool TryPurchaseMercenary(MercenaryDefinition merc, int cost)
        {
            if (merc == null || Gold < cost) return false;
            if (!HasMercenary(merc.Id) && !CanHireMercenary()) return false;

            Gold -= cost;
            AddOrEvolve(merc, requireRoom: false);
            return true;
        }

        public static bool TryUseDiceForReroll()
        {
            if (DiceCount <= 0) return false;
            DiceCount--;
            SaveSystem.Save();
            return true;
        }

        public static bool TrySpendGold(int amount)
        {
            if (Gold < amount) return false;
            Gold -= amount;
            SaveSystem.Save();
            return true;
        }

        public static void GrantPendingRelic(string relicId)
        {
            PendingRelicIds.Add(relicId);
            SaveSystem.Save();
        }

        public static void ResolvePendingRelic(string relicId)
        {
            PendingRelicIds.Remove(relicId);
            SaveSystem.Save();
        }

        public static bool TryPurchasePartySlot(int cost)
        {
            if (PartySizeCap >= MaxPartySizeCap || Gold < cost) return false;
            Gold -= cost;
            PartySizeCap++;
            SaveSystem.Save();
            return true;
        }

        public static void ReleaseMercenary(RosterMember merc)
        {
            Roster.Remove(merc);
            SaveSystem.Save();
        }

        public static void SpendAllGold()
        {
            Gold = 0;
            SaveSystem.Save();
        }

        // --- 맵 이벤트 효과용 헬퍼 (음수 amount로 차감도 가능, 0 미만은 클램프) ---
        public static void AddGold(int amount)
        {
            Gold += amount;
            if (Gold < 0) Gold = 0;
            SaveSystem.Save();
        }

        public static void DoubleGold()
        {
            Gold *= 2;
            SaveSystem.Save();
        }

        public static void HalveGold()
        {
            Gold /= 2;
            SaveSystem.Save();
        }

        public static void AddDice(int amount)
        {
            DiceCount += amount;
            if (DiceCount < 0) DiceCount = 0;
            SaveSystem.Save();
        }

        // NextBattleBaseDamage를 음수 방향으로 낮춰 다음 전투 기지에 추가 체력을 부여한다.
        // (BattleManager: playerBaseHealth = Max(1, maxHealth - NextBattleBaseDamage))
        public static void FortifyNextBattleBase(float amount)
        {
            NextBattleBaseDamage -= amount;
            SaveSystem.Save();
        }

        public static RosterMember RemoveRandomMercenary()
        {
            if (Roster.Count == 0) return null;
            RosterMember merc = Roster[UnityEngine.Random.Range(0, Roster.Count)];
            Roster.Remove(merc);
            SaveSystem.Save();
            return merc;
        }

        public static void SwapRandomMercenary(out RosterMember removed, out MercenaryDefinition added)
        {
            removed = null;
            if (Roster.Count > 0)
            {
                removed = Roster[UnityEngine.Random.Range(0, Roster.Count)];
                Roster.Remove(removed);
            }
            added = CanHireMercenary() ? MercenaryDatabase.GetRandomForGamble(CurrentAct) : null;
            // 로스터에서 방금 지운 뒤라 requireRoom 없이 넣되, 뽑힌 용병이 이미 로스터에 있으면 진화로 처리.
            if (added != null) AddOrEvolve(added, requireRoom: false);
            SaveSystem.Save();
        }

        public static float ConsumeNextBattleBaseDamage()
        {
            float dmg = NextBattleBaseDamage;
            NextBattleBaseDamage = 0f;
            SaveSystem.Save();
            return dmg;
        }
    }
}
