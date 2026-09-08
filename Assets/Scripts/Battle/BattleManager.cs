using System.Collections.Generic;
using System.Linq;
using StickmanOfWar.Map;
using StickmanOfWar.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StickmanOfWar.Battle
{
    public class BattleManager : MonoBehaviour
    {
        private const string MapSelectSceneName = "MapSelect";
        private const string GameplaySceneName = "Gameplay";

        public static BattleManager Instance { get; private set; }

        [SerializeField] private RectTransform lane;
        [SerializeField] private RectTransform playerBaseAnchor;
        [SerializeField] private RectTransform enemyBaseAnchor;

        [SerializeField] private float playerBaseMaxHealth = 100f;
        [SerializeField] private float enemyBaseMaxHealth = 100f;
        [SerializeField] private Image playerBaseHealthFill;
        [SerializeField] private Image enemyBaseHealthFill;
        [SerializeField] private TMP_Text playerBaseHealthText;
        [SerializeField] private TMP_Text enemyBaseHealthText;

        [SerializeField] private float energyPerSecond = 1f;
        [SerializeField] private TMP_Text energyText;

        [SerializeField] private float energyPerEnemyMaxHealth = 0.25f;
        [SerializeField] private float energyPerEnemyAttackDamage = 1f;

        [SerializeField] private Unit[] unitPrefabs = new Unit[3];
        [SerializeField] private int[] unitCosts = { 10, 20, 30 };

        [SerializeField] private Unit meleeUnitTemplate;
        [SerializeField] private Unit rangedUnitTemplate;
        private const float RangedThreshold = 100f;

        [SerializeField] private Unit enemyUnitPrefab;
        // 소환 페이스 기준값. 12 = 기본 배속(×1). 값을 키우면 전체적으로 느긋해진다.
        [SerializeField] private float enemySpawnInterval = 12f;

        // ── 적 웨이브 편성 상수 ──
        // 밸런스 2차 조정: "너무 어려워졌다" 피드백으로, 직전 상향분(초반 압박 강화)을 절반쯤 되돌림.
        private const float FirstSpawnDelay = 2.2f;
        private const float BossFirstDelay = 2.4f;
        private const float SpawnIntervalStart = 7.5f;
        private const float SpawnIntervalMin = 4.6f;
        private const int SpawnRampCount = 18;      // 이 횟수에 걸쳐 소환 간격이 최소치까지 좁혀짐
        private const int EliteEvery = 5;           // 엘리트 노드에서 N번째 소환마다 엘리트 1기
        private const int BattleDistinctTypes = 5;  // 한 전투가 뽑아 쓰는 일반 적 종류 수
        private const float SummonerPaceMult = 0.85f;  // Summoner 보스전: 잡몹 소환 간격 배수(짧게)
        private const float VanguardAddPaceMult = 1.5f; // Vanguard 보스전: 잡몹 소환 간격 배수(길게)
        private const float JuggernautAddInterval = 15f; // Juggernaut 보스전: 잡몹은 아주 드물게

        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;
        [SerializeField] private Button returnToMapButton;
        [SerializeField] private Button retryButton;

        [SerializeField] private Button bagButton;
        [SerializeField] private BagPanel bagPanel;

        [SerializeField] private Button partyButton;
        [SerializeField] private PartyPanel partyPanel;

        private const int BaseGoldReward = 20;
        private const int EliteGoldReward = 40;
        private const int BossGoldReward = 60;

        private float energy;
        private float playerBaseHealth;
        private float enemyBaseHealth;
        private float enemySpawnTimer;
        private bool battleOver;

        // 로스터 시너지 + 배치된 유물 효과를 합산한 전투 보정치 (Awake에서 1회 계산)
        private SynergyCombatBonus combatBonus = SynergyCombatBonus.Identity;

        // 현재 노드 종류/막에 맞춰 Awake에서 구성되는 적 편성
        private EnemyDefinition[] enemyWavePool;   // 이번 전투가 뽑아 쓰는 일반 적 종류
        private EnemyDefinition[] eliteInjects;    // 중간중간 섞을 엘리트 (엘리트 노드 전용)
        private EnemyDefinition pendingBoss;       // 보스 노드에서 등장할 보스 정의
        private bool bossSpawned;
        private int enemiesSpawned;

        // ── 보스 패턴 상태 ──
        private BossPattern bossPattern;
        private int bossDefeatsNeeded;   // >=1: 보스를 이만큼 처치하면 승리 / 0: 기지 파괴로 승리
        private int bossDefeatsDone;
        private bool bossAlive;
        private float bossRespawnTimer;

        private readonly List<Unit> playerUnits = new List<Unit>();
        private readonly List<Unit> enemyUnits = new List<Unit>();

        public float Energy => energy;

        private void Awake()
        {
            Instance = this;
            playerBaseHealth = Mathf.Max(1f, playerBaseMaxHealth - RunState.ConsumeNextBattleBaseDamage());
            enemyBaseHealth = enemyBaseMaxHealth;

            var prefabs = new List<Unit>();
            var costs = new List<int>();
            foreach (RosterMember merc in RunState.Roster)
            {
                bool ranged = merc.AttackRange >= RangedThreshold;
                prefabs.Add(ranged ? rangedUnitTemplate : meleeUnitTemplate);
                costs.Add(merc.DeployCost);
            }
            unitPrefabs = prefabs.ToArray();
            unitCosts = costs.ToArray();

            combatBonus = SynergyCombatBonus.Combine(
                SynergyEffects.GetActiveBonus(RunState.Roster.Select(m => m.Definition).ToList()),
                RelicEffects.GetActiveBonus(RunState.Bag)).Clamped();

            // 재화 수급은 막이 올라갈수록 빨라진다 (적 스탯이 막마다 커지므로).
            // 씬의 energyPerSecond는 1막 기준값. 여기서 막 배수를 곱해 확정한다.
            int act = RunState.CurrentAct > 0 ? Mathf.Clamp(RunState.CurrentAct, 1, 3) : 1;
            energyPerSecond *= EnergyActScale(act);
            // 개전 직후 얇은 방어선을 세울 초기 재화 (이후엔 처치 보상 위주로 굴러감)
            energy = energyPerSecond * StartEnergySeconds;

            BuildEnemyPlan();
        }

        // 밸런스 2차 조정: 초반 방어선을 세울 여유를 조금 더 준다 (기존 8 → 10).
        private const float StartEnergySeconds = 10f;

        private static float EnergyActScale(int act)
        {
            switch (act)
            {
                case 2: return 1.5f;
                case 3: return 2.0f;
                default: return 1f;
            }
        }

        // 일반 전투 승리 후 용병 제안이 뜰 확률 (후반일수록 빌드가 굳어 낮춤)
        private static float MercOfferChanceForAct(int act)
        {
            switch (act)
            {
                case 2: return 0.45f;
                case 3: return 0.35f;
                default: return 0.55f;
            }
        }

        // 현재 밟은 노드가 전투/정예전투/보스 중 무엇이냐에 따라 적 풀을 편성한다.
        private void BuildEnemyPlan()
        {
            NodeType nodeType = NodeType.Battle;
            string bossId = null;
            if (RunState.CurrentNodeId.HasValue && RunState.CurrentMap != null
                && RunState.CurrentMap.Nodes.TryGetValue(RunState.CurrentNodeId.Value, out MapNode node))
            {
                nodeType = node.Type;
                bossId = node.BossId;
            }

            int act = RunState.CurrentAct > 0 ? Mathf.Clamp(RunState.CurrentAct, 1, 3) : 1;
            enemyWavePool = EnemyDatabase.RollBattleSelection(act, BattleDistinctTypes);

            switch (nodeType)
            {
                case NodeType.Boss:
                    pendingBoss = EnemyDatabase.GetById(bossId)
                                  ?? EnemyDatabase.GetBossPool(act)[Random.Range(0, EnemyDatabase.GetBossPool(act).Length)];
                    bossPattern = pendingBoss.Pattern;
                    bossDefeatsNeeded = pendingBoss.DefeatsToWin;
                    break;

                case NodeType.Elite:
                    // 초반 난이도 완화: 1막은 엘리트 1종, 2막부터 2종을 섞는다
                    eliteInjects = EnemyDatabase.GetElitePool(act)
                        .OrderBy(_ => Random.value).Take(act <= 1 ? 1 : 2).ToArray();
                    break;
            }
        }

        private void Start()
        {
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);
            if (returnToMapButton != null) returnToMapButton.onClick.AddListener(OnClickReturnToMap);
            if (retryButton != null) retryButton.onClick.AddListener(OnClickRetry);
            if (bagButton != null) bagButton.onClick.AddListener(() => bagPanel.Show());
            if (partyButton != null) partyButton.onClick.AddListener(() => partyPanel.Show());

            StartCoroutine(RepositionEnemyBaseNextFrame());

            UpdateHealthBars();
            UpdateEnergyText();
        }

        private System.Collections.IEnumerator RepositionEnemyBaseNextFrame()
        {
            // Lane stretches to fill the actual screen width, which the CanvasScaler
            // only resolves after the first layout pass — reading lane.rect.width in
            // Start() (before that pass) reads a stale size. Wait a frame so the enemy
            // base ends up the same margin from the true right edge as the player base
            // is from the left, regardless of the real aspect ratio.
            yield return null;

            float margin = playerBaseAnchor.anchoredPosition.x;
            Vector2 enemyPos = enemyBaseAnchor.anchoredPosition;
            enemyPos.x = lane.rect.width - margin;
            enemyBaseAnchor.anchoredPosition = enemyPos;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public bool BattleOver => battleOver;

        public void Tick(float deltaTime)
        {
            if (battleOver) return;

            energy += energyPerSecond * deltaTime;
            UpdateEnergyText();

            TickBossRespawn(deltaTime);

            enemySpawnTimer += deltaTime;
            if (enemySpawnTimer >= NextSpawnDelay())
            {
                enemySpawnTimer = 0f;
                SpawnNextScheduledEnemy();
            }
        }

        // Relentless 보스: 쓰러진 뒤 일정 시간이 지나면 다시 등장 (필요 처치 횟수를 채울 때까지).
        private void TickBossRespawn(float deltaTime)
        {
            if (pendingBoss == null || bossPattern != BossPattern.Relentless) return;
            if (!bossSpawned || bossAlive || bossDefeatsDone >= bossDefeatsNeeded) return;

            bossRespawnTimer += deltaTime;
            if (bossRespawnTimer >= Mathf.Max(0.5f, pendingBoss.BossRespawnDelay))
            {
                bossRespawnTimer = 0f;
                SpawnEnemy(pendingBoss);
            }
        }

        // 소환이 진행될수록 간격이 좁아진다(전투가 후반으로 갈수록 압박 증가).
        // 보스는 최우선으로 빠르게 등장시킨다.
        private float NextSpawnDelay()
        {
            float paceScale = enemySpawnInterval > 0f ? enemySpawnInterval / 12f : 1f;

            if (pendingBoss != null && !bossSpawned)
                // Relentless는 초반에 방어선을 세울 시간을 준다 (이후 재등장은 BossRespawnDelay 사용)
                return (bossPattern == BossPattern.Relentless ? 6f : BossFirstDelay) * paceScale;
            if (enemiesSpawned == 0)
                return FirstSpawnDelay * paceScale;

            // 보스 패턴별 잡몹 소환 페이스
            if (pendingBoss != null && bossSpawned)
            {
                // Juggernaut / Relentless 는 "보스 한 명과의 승부"에 집중 — 잡몹은 거의 없음
                if (bossPattern == BossPattern.Juggernaut || bossPattern == BossPattern.Relentless)
                    return JuggernautAddInterval * paceScale;

                float ts = Mathf.Clamp01((float)enemiesSpawned / SpawnRampCount);
                float baseDelay = Mathf.Lerp(SpawnIntervalStart, SpawnIntervalMin, ts) * paceScale;
                if (bossPattern == BossPattern.Summoner) return baseDelay * SummonerPaceMult;
                if (bossPattern == BossPattern.Vanguard) return baseDelay * VanguardAddPaceMult;
                return baseDelay;
            }

            float t = Mathf.Clamp01((float)enemiesSpawned / SpawnRampCount);
            return Mathf.Lerp(SpawnIntervalStart, SpawnIntervalMin, t) * paceScale;
        }

        public int GetCost(int slotIndex)
        {
            return (slotIndex >= 0 && slotIndex < unitCosts.Length) ? unitCosts[slotIndex] : 0;
        }

        public bool TrySpawnPlayerUnit(int slotIndex)
        {
            if (battleOver) return false;
            if (slotIndex < 0 || slotIndex >= unitPrefabs.Length) return false;

            Unit prefab = unitPrefabs[slotIndex];
            if (prefab == null) return false;

            int cost = GetCost(slotIndex);
            if (energy < cost) return false;

            energy -= cost;
            UpdateEnergyText();

            Unit instance = Instantiate(prefab, lane);
            RectTransform rect = (RectTransform)instance.transform;
            rect.anchoredPosition = new Vector2(playerBaseAnchor.anchoredPosition.x, 0f);

            if (slotIndex < RunState.Roster.Count)
            {
                RosterMember merc = RunState.Roster[slotIndex];

                // 용병 기본 스펙(진화 배율 포함, RosterMember가 계산)에 공격 계열 시너지 보정을 곱하고,
                // 방어/유틸 계열 보정은 Unit이 런타임에 쓰도록 그대로 넘긴다.
                float atkSpeed = Mathf.Max(0.01f, combatBonus.AttackSpeedMult);
                instance.Configure(
                    merc.MaxHealth * combatBonus.MaxHealthMult,
                    merc.AttackDamage * combatBonus.DamageMult,
                    merc.AttackInterval / atkSpeed,
                    merc.AttackRange * combatBonus.RangeMult,
                    merc.MoveSpeed * combatBonus.MoveSpeedMult,
                    combatBonus,
                    merc.IsHealer,
                    merc.IsEvolved);
            }

            return true;
        }

        private void SpawnNextScheduledEnemy()
        {
            if (enemyUnitPrefab == null) return;

            EnemyDefinition def;
            if (pendingBoss != null && !bossSpawned)
            {
                def = pendingBoss;
                bossSpawned = true;
            }
            else if (eliteInjects != null && eliteInjects.Length > 0
                     && enemiesSpawned > 0 && enemiesSpawned % EliteEvery == 0)
            {
                def = eliteInjects[Random.Range(0, eliteInjects.Length)];
            }
            else if (enemyWavePool != null && enemyWavePool.Length > 0)
            {
                def = enemyWavePool[Random.Range(0, enemyWavePool.Length)];
            }
            else
            {
                def = EnemyDatabase.GetRandomNormal(1);
            }

            enemiesSpawned++;
            SpawnEnemy(def);
        }

        private void SpawnEnemy(EnemyDefinition def)
        {
            Unit instance = Instantiate(enemyUnitPrefab, lane);
            RectTransform rect = (RectTransform)instance.transform;
            rect.anchoredPosition = new Vector2(enemyBaseAnchor.anchoredPosition.x, 0f);

            EnemyStyler.Apply(instance, def);
            instance.Configure(def.MaxHealth, def.AttackDamage, def.AttackInterval, def.AttackRange, def.MoveSpeed);

            // 유물 "뒤엉킨 사슬" 등으로 적 이동속도를 둔화시킨다.
            if (combatBonus.EnemyMoveSpeedMult != 1f)
            {
                instance.SetMoveSpeedMultiplier(combatBonus.EnemyMoveSpeedMult);
            }

            // 이 보스를 쓰러뜨리면 승리하는 패턴이면 표식을 남긴다.
            if (def.Rank == EnemyRank.Boss && def.DefeatsToWin >= 1)
            {
                instance.IsBossWinTarget = true;
                bossAlive = true;
            }
        }

        // Juggernaut / Relentless 보스가 쓰러졌을 때 Unit이 호출.
        public void NotifyBossTargetDefeated()
        {
            if (battleOver) return;

            bossAlive = false;
            bossDefeatsDone++;

            if (bossDefeatsNeeded >= 1 && bossDefeatsDone >= bossDefeatsNeeded)
            {
                EndBattle(true);
            }
            else
            {
                bossRespawnTimer = 0f; // Relentless: 다음 등장 카운트다운 시작
            }
        }

        public void OnEnemyKilled(Unit enemy)
        {
            float reward = enemy.MaxHealth * energyPerEnemyMaxHealth + enemy.AttackDamage * energyPerEnemyAttackDamage;
            energy += reward;
            UpdateEnergyText();
        }

        public void RegisterUnit(Unit unit)
        {
            (unit.Faction == Faction.Player ? playerUnits : enemyUnits).Add(unit);
        }

        public void UnregisterUnit(Unit unit)
        {
            (unit.Faction == Faction.Player ? playerUnits : enemyUnits).Remove(unit);
        }

        public Unit FindNearestEnemy(Unit from)
        {
            List<Unit> pool = from.Faction == Faction.Player ? enemyUnits : playerUnits;
            Unit nearest = null;
            float nearestDist = float.MaxValue;
            float myX = from.Rect.anchoredPosition.x;

            foreach (Unit candidate in pool)
            {
                if (candidate == null) continue;
                float dist = Mathf.Abs(candidate.Rect.anchoredPosition.x - myX);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = candidate;
                }
            }
            return nearest;
        }

        // 힐러 유닛 전용 — 같은 진영에서 체력 비율이 가장 낮은(단, 부상 상태인) 유닛을 찾는다.
        // FindNearestEnemy와 동일하게 거리 제한 없이 우선순위 대상만 고르고, 사거리 판정은 Unit 쪽에서 한다.
        public Unit FindMostInjuredAlly(Unit from)
        {
            List<Unit> pool = from.Faction == Faction.Player ? playerUnits : enemyUnits;
            Unit best = null;
            float bestFraction = 1f;

            foreach (Unit candidate in pool)
            {
                if (candidate == null || !candidate.IsInjured) continue;
                if (best == null || candidate.HealthFraction < bestFraction)
                {
                    best = candidate;
                    bestFraction = candidate.HealthFraction;
                }
            }
            return best;
        }

        public float GetBaseX(Faction faction)
        {
            return faction == Faction.Player ? playerBaseAnchor.anchoredPosition.x : enemyBaseAnchor.anchoredPosition.x;
        }

        public void DamageBase(Faction faction, float amount)
        {
            if (battleOver) return;

            if (faction == Faction.Player)
            {
                playerBaseHealth = Mathf.Max(0f, playerBaseHealth - amount);
            }
            else
            {
                enemyBaseHealth = Mathf.Max(0f, enemyBaseHealth - amount);
            }
            UpdateHealthBars();

            if (enemyBaseHealth <= 0f)
            {
                EndBattle(true);
            }
            else if (playerBaseHealth <= 0f)
            {
                EndBattle(false);
            }
        }

        private void UpdateHealthBars()
        {
            if (playerBaseHealthFill != null) playerBaseHealthFill.fillAmount = playerBaseHealth / playerBaseMaxHealth;
            if (enemyBaseHealthFill != null) enemyBaseHealthFill.fillAmount = enemyBaseHealth / enemyBaseMaxHealth;
            if (playerBaseHealthText != null) playerBaseHealthText.text = $"{Mathf.CeilToInt(playerBaseHealth)}/{Mathf.CeilToInt(playerBaseMaxHealth)}";
            if (enemyBaseHealthText != null) enemyBaseHealthText.text = $"{Mathf.CeilToInt(enemyBaseHealth)}/{Mathf.CeilToInt(enemyBaseMaxHealth)}";
        }

        private void UpdateEnergyText()
        {
            if (energyText != null) energyText.text = Mathf.FloorToInt(energy).ToString();
        }

        private void EndBattle(bool playerWon)
        {
            if (battleOver) return;   // 기지 파괴와 보스 처치가 동시에 들어와도 한 번만 처리
            battleOver = true;

            foreach (Unit u in new List<Unit>(playerUnits)) if (u != null) Destroy(u.gameObject);
            foreach (Unit u in new List<Unit>(enemyUnits)) if (u != null) Destroy(u.gameObject);

            if (playerWon)
            {
                if (victoryPanel != null) victoryPanel.SetActive(true);

                MapNode currentNode = null;
                if (RunState.CurrentNodeId.HasValue)
                {
                    RunState.CurrentMap.Nodes.TryGetValue(RunState.CurrentNodeId.Value, out currentNode);
                }

                RunState.Gold += Mathf.RoundToInt(GetGoldReward(currentNode) * combatBonus.GoldGainMult);

                if (currentNode != null && currentNode.Type == NodeType.Elite)
                {
                    RunState.GrantPendingRelic(RelicDatabase.GetRandom().Id);
                }

                // 일반 전투 승리 시 챕터별 확률로 용병 1명 제안 (맵 복귀 시 영입/넘기기).
                // 정예는 유물, 보스는 큰 보상이라 중복하지 않는다.
                if (currentNode != null && currentNode.Type == NodeType.Battle
                    && string.IsNullOrEmpty(RunState.PendingMercOfferId)
                    && Random.value < MercOfferChanceForAct(RunState.CurrentAct))
                {
                    MercenaryDefinition[] rolled = MercenaryDatabase.RollOffersWeighted(RunState.CurrentAct, 1);
                    if (rolled.Length > 0 && rolled[0] != null)
                    {
                        RunState.PendingMercOfferId = rolled[0].Id;
                    }
                }

                RunState.CompleteCurrentNode();
            }
            else
            {
                if (defeatPanel != null) defeatPanel.SetActive(true);
            }
        }

        private static int GetGoldReward(MapNode node)
        {
            if (node == null) return BaseGoldReward;
            switch (node.Type)
            {
                case NodeType.Elite: return EliteGoldReward;
                case NodeType.Boss: return BossGoldReward;
                default: return BaseGoldReward;
            }
        }

        private void OnClickReturnToMap()
        {
            SceneManager.LoadScene(MapSelectSceneName);
        }

        // 패배 시 "다시하기" — 로그라이트답게 이 런은 완전히 끝난 것으로 보고,
        // 진 전투를 그대로 재시도하는 게 아니라 런 자체를 초기화해 1막 성(시작 노드)부터 다시 시작한다.
        private void OnClickRetry()
        {
            RunState.StartNewRun();
            SceneManager.LoadScene(MapSelectSceneName);
        }
    }
}
