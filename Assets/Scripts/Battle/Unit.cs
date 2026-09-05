using UnityEngine;
using UnityEngine.UI;

namespace StickmanOfWar.Battle
{
    public class Unit : MonoBehaviour
    {
        [SerializeField] private Faction faction;
        [SerializeField] private float maxHealth = 20f;
        [SerializeField] private float moveSpeed = 140f;
        [SerializeField] private float attackDamage = 5f;
        [SerializeField] private float attackInterval = 1f;
        [SerializeField] private float attackRange = 70f;
        [SerializeField] private Image healthFill;

        private float currentHealth;
        private float attackTimer;
        private RectTransform rect;

        public Faction Faction => faction;
        public RectTransform Rect => rect;

        private void Awake()
        {
            rect = (RectTransform)transform;
            currentHealth = maxHealth;
        }

        private void OnEnable()
        {
            BattleManager.Instance.RegisterUnit(this);
        }

        private void OnDisable()
        {
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.UnregisterUnit(this);
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            Unit target = BattleManager.Instance.FindNearestEnemy(this);
            float myX = rect.anchoredPosition.x;

            if (target != null)
            {
                float distance = Mathf.Abs(target.Rect.anchoredPosition.x - myX);
                if (distance <= attackRange)
                {
                    Unit targetRef = target;
                    TickAttack(deltaTime, () => targetRef.TakeDamage(attackDamage));
                    return;
                }
            }
            else
            {
                Faction enemyFaction = faction == Faction.Player ? Faction.Enemy : Faction.Player;
                float baseX = BattleManager.Instance.GetBaseX(enemyFaction);
                if (Mathf.Abs(baseX - myX) <= attackRange)
                {
                    TickAttack(deltaTime, () => BattleManager.Instance.DamageBase(enemyFaction, attackDamage));
                    return;
                }
            }

            float direction = faction == Faction.Player ? 1f : -1f;
            Vector2 pos = rect.anchoredPosition;
            pos.x += direction * moveSpeed * deltaTime;
            rect.anchoredPosition = pos;
        }

        private void TickAttack(float deltaTime, System.Action onAttack)
        {
            attackTimer += deltaTime;
            if (attackTimer >= attackInterval)
            {
                attackTimer -= attackInterval;
                onAttack();
            }
        }

        public void TakeDamage(float amount)
        {
            currentHealth -= amount;
            if (healthFill != null)
            {
                healthFill.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
            }
            if (currentHealth <= 0f)
            {
                BattleManager.Instance.UnregisterUnit(this);
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
        }
    }
}
