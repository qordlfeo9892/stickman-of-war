using System.Collections.Generic;
using StickmanOfWar.Map;
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

        [SerializeField] private float goldPerSecond = 1f;
        [SerializeField] private TMP_Text goldText;

        [SerializeField] private Unit[] unitPrefabs = new Unit[3];
        [SerializeField] private int[] unitCosts = { 10, 20, 30 };

        [SerializeField] private Unit enemyUnitPrefab;
        [SerializeField] private float enemySpawnInterval = 12f;

        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;
        [SerializeField] private Button returnToMapButton;
        [SerializeField] private Button retryButton;

        private float gold;
        private float playerBaseHealth;
        private float enemyBaseHealth;
        private float enemySpawnTimer;
        private bool battleOver;

        private readonly List<Unit> playerUnits = new List<Unit>();
        private readonly List<Unit> enemyUnits = new List<Unit>();

        public float Gold => gold;

        private void Awake()
        {
            Instance = this;
            playerBaseHealth = playerBaseMaxHealth;
            enemyBaseHealth = enemyBaseMaxHealth;
        }

        private void Start()
        {
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);
            if (returnToMapButton != null) returnToMapButton.onClick.AddListener(OnClickReturnToMap);
            if (retryButton != null) retryButton.onClick.AddListener(OnClickRetry);

            StartCoroutine(RepositionEnemyBaseNextFrame());

            UpdateHealthBars();
            UpdateGoldText();
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

            gold += goldPerSecond * deltaTime;
            UpdateGoldText();

            enemySpawnTimer += deltaTime;
            if (enemySpawnTimer >= enemySpawnInterval)
            {
                enemySpawnTimer -= enemySpawnInterval;
                SpawnEnemyUnit();
            }
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
            if (gold < cost) return false;

            gold -= cost;
            UpdateGoldText();

            Unit instance = Instantiate(prefab, lane);
            RectTransform rect = (RectTransform)instance.transform;
            rect.anchoredPosition = new Vector2(playerBaseAnchor.anchoredPosition.x, 0f);
            return true;
        }

        private void SpawnEnemyUnit()
        {
            if (enemyUnitPrefab == null) return;

            Unit instance = Instantiate(enemyUnitPrefab, lane);
            RectTransform rect = (RectTransform)instance.transform;
            rect.anchoredPosition = new Vector2(enemyBaseAnchor.anchoredPosition.x, 0f);
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
        }

        private void UpdateGoldText()
        {
            if (goldText != null) goldText.text = Mathf.FloorToInt(gold).ToString();
        }

        private void EndBattle(bool playerWon)
        {
            battleOver = true;

            foreach (Unit u in new List<Unit>(playerUnits)) if (u != null) Destroy(u.gameObject);
            foreach (Unit u in new List<Unit>(enemyUnits)) if (u != null) Destroy(u.gameObject);

            if (playerWon)
            {
                if (victoryPanel != null) victoryPanel.SetActive(true);
                RunState.CompleteCurrentNode();
            }
            else
            {
                if (defeatPanel != null) defeatPanel.SetActive(true);
            }
        }

        private void OnClickReturnToMap()
        {
            SceneManager.LoadScene(MapSelectSceneName);
        }

        private void OnClickRetry()
        {
            SceneManager.LoadScene(GameplaySceneName);
        }
    }
}
