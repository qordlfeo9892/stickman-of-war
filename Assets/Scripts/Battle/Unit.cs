using StickmanOfWar.Map;
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
        [SerializeField] private Image shieldFill; // HP바 뒤에 깔리는 회색 보호막 게이지 (없으면 생략)
        [SerializeField] private GameObject evolvedHitEffectPrefab; // 2성 명중 이펙트 (CFXR). 없으면 절차적 사각 버스트로 대체.
        [SerializeField] private float evolvedHitEffectScale = 1f;
        // 캔버스가 Screen Space - Camera라 RectTransform.position이 실제 월드 좌표다.
        // 이 값만큼 카메라 쪽으로 당겨서 UI 평면보다 앞에 그려지게 한다 (겹쳐서 안 가려지도록).
        [SerializeField] private float evolvedHitEffectCameraOffset = 8f;

        // 2성 진화 유닛 전용 연출 상수 — 그래픽 에셋 없으면 절차적으로 대체 재생한다.
        private const float EvolvedLungeDistance = 18f;
        private const float EvolvedLungeDuration = 0.12f;
        private const float EvolvedFlashDuration = 0.06f;
        private const float HitBurstSize = 26f;
        private const float HitBurstDuration = 0.22f;
        private static readonly Color HitBurstColor = new Color(1f, 0.55f, 0.15f, 0.9f);

        private float currentHealth;
        private float attackTimer;
        private RectTransform rect;
        private Image bodyImage; // 피격 플래시용 — 유닛 본체 사각형의 Image (루트에 있음)
        private Coroutine lungeCoroutine;

        // 어그로 고정(sticky targeting) — 한 번 물면 상대가 죽거나 사거리를 벗어나기 전까지 표적을 안 바꾼다.
        private Unit currentTargetUnit;
        private bool isAttackingBase;

        // 시너지 + 유물 전투 보정치 (플레이어 유닛만 Configure 로 주입, 적 유닛은 기본값 유지)
        private float damageTakenMult = 1f;
        private float flatArmor;
        private float lifestealFrac;
        private float dodgeFrac;
        private float executeThreshold;
        private float currentShield;
        private float critChanceAdd;
        private float critDamageAdd;
        private float healthRegenPerSec;

        // 힐러 역할 여부 — true면 공격 대신 사거리 내 가장 체력이 낮은 아군을 회복한다.
        private bool isHealer;
        // 2성 진화 여부 — true면 공격 시 펀치 모션 + 명중 시 새 피격 이펙트를 재생한다.
        private bool isEvolved;

        // 이 유닛이 쓰러지면 전투가 승리로 끝나는 보스인지 (Juggernaut / Relentless)
        public bool IsBossWinTarget;

        public Faction Faction => faction;
        public RectTransform Rect => rect;
        public float MaxHealth => maxHealth;
        public float AttackDamage => attackDamage;
        public bool IsInjured => currentHealth < maxHealth;
        public float HealthFraction => maxHealth > 0f ? currentHealth / maxHealth : 0f;

        private void Awake()
        {
            rect = (RectTransform)transform;
            bodyImage = GetComponent<Image>();
            currentHealth = maxHealth;
        }

        public void Configure(float configuredMaxHealth, float configuredAttackDamage, float configuredAttackInterval,
            float configuredAttackRange, float configuredMoveSpeed, SynergyCombatBonus? synergy = null,
            bool configuredIsHealer = false, bool configuredIsEvolved = false)
        {
            maxHealth = configuredMaxHealth;
            attackDamage = configuredAttackDamage;
            attackInterval = configuredAttackInterval;
            attackRange = configuredAttackRange;
            moveSpeed = configuredMoveSpeed;
            currentHealth = maxHealth;
            isHealer = configuredIsHealer;
            isEvolved = configuredIsEvolved;

            SynergyCombatBonus s = synergy ?? SynergyCombatBonus.Identity;
            damageTakenMult = s.DamageTakenMult;
            flatArmor = s.FlatArmor;
            lifestealFrac = s.LifestealFrac;
            dodgeFrac = s.DodgeFrac;
            executeThreshold = s.ExecuteThreshold;
            currentShield = maxHealth * s.ShieldFracOfMaxHp;
            critChanceAdd = s.CritChanceAdd;
            critDamageAdd = s.CritDamageAdd;
            healthRegenPerSec = s.HealthRegenPerSec;

            UpdateHealthBarVisuals();
        }

        // 유물 "뒤엉킨 사슬" 등 — 소환 시점에 적 유닛 이동속도를 조정한다.
        public void SetMoveSpeedMultiplier(float multiplier)
        {
            moveSpeed *= multiplier;
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
            if (healthRegenPerSec > 0f && currentHealth > 0f && currentHealth < maxHealth)
            {
                Heal(healthRegenPerSec * deltaTime);
            }

            if (isHealer)
            {
                TickHealer(deltaTime);
                return;
            }

            float myX = rect.anchoredPosition.x;
            Faction enemyFaction = faction == Faction.Player ? Faction.Enemy : Faction.Player;

            // 어그로 우선순위: 이미 기지를 공격 중이면, 나중에 적(아군 입장에서 새로 나타난 상대) 유닛이
            // 등장해도 정신 팔리지 않고 계속 기지만 때린다. 기지는 움직이지 않으므로 사거리를 벗어날 일은 없다.
            if (isAttackingBase)
            {
                float baseXStick = BattleManager.Instance.GetBaseX(enemyFaction);
                if (Mathf.Abs(baseXStick - myX) <= attackRange)
                {
                    TickAttack(deltaTime, () => AttackBase(enemyFaction));
                    return;
                }
                isAttackingBase = false;
            }

            // 마찬가지로 이미 싸우고 있는 유닛이 있으면(살아있고 사거리 내) 그 상대를 계속 공격한다 —
            // 더 가까운 새 상대가 나타났다고 매 프레임 표적을 바꾸지 않는다.
            if (currentTargetUnit != null)
            {
                if (currentTargetUnit.gameObject.activeInHierarchy)
                {
                    float stickyDistance = Mathf.Abs(currentTargetUnit.Rect.anchoredPosition.x - myX);
                    if (stickyDistance <= attackRange)
                    {
                        Unit stickyTarget = currentTargetUnit;
                        TickAttack(deltaTime, () => AttackUnit(stickyTarget));
                        return;
                    }
                }
                currentTargetUnit = null;
            }

            Unit target = BattleManager.Instance.FindNearestEnemy(this);

            if (target != null)
            {
                float distance = Mathf.Abs(target.Rect.anchoredPosition.x - myX);
                if (distance <= attackRange)
                {
                    currentTargetUnit = target;
                    Unit targetRef = target;
                    TickAttack(deltaTime, () => AttackUnit(targetRef));
                    return;
                }
            }
            else
            {
                float baseX = BattleManager.Instance.GetBaseX(enemyFaction);
                if (Mathf.Abs(baseX - myX) <= attackRange)
                {
                    isAttackingBase = true;
                    TickAttack(deltaTime, () => AttackBase(enemyFaction));
                    return;
                }
            }

            Advance(deltaTime);
        }

        // 힐러 전용 루프 — 적을 공격하는 대신 사거리 내 가장 체력이 낮은 아군을 찾아 회복한다.
        // 회복할 대상이 없으면(전원 만피) 다른 유닛처럼 그냥 전열을 따라 전진만 한다.
        private void TickHealer(float deltaTime)
        {
            Unit ally = BattleManager.Instance.FindMostInjuredAlly(this);
            if (ally != null)
            {
                float distance = Mathf.Abs(ally.Rect.anchoredPosition.x - rect.anchoredPosition.x);
                if (distance <= attackRange)
                {
                    Unit allyRef = ally;
                    TickAttack(deltaTime, () => HealAlly(allyRef));
                    return;
                }
            }

            Advance(deltaTime);
        }

        private void Advance(float deltaTime)
        {
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

        private void AttackUnit(Unit target)
        {
            if (target == null) return;

            float dmg = RollAttackDamage();
            target.TakeDamage(dmg);
            if (lifestealFrac > 0f) Heal(dmg * lifestealFrac);
            // 처형: 피해를 준 뒤에도 살아있고 문턱 이하로 남았으면 즉시 처치
            if (executeThreshold > 0f && target != null) target.ExecuteIfBelow(executeThreshold);

            if (isEvolved)
            {
                float direction = Mathf.Sign(target.Rect.anchoredPosition.x - rect.anchoredPosition.x);
                PlayEvolvedAttackMotion(direction == 0f ? DefaultDirection() : direction);

                // 방금 그 공격에 target이 죽어 비활성화됐다면 이펙트를 재생하지 않는다 (죽은 오브젝트에 코루틴 시작 방지).
                if (target != null && target.gameObject.activeInHierarchy) target.PlayEvolvedHitEffect();
            }
        }

        private void AttackBase(Faction enemyFaction)
        {
            float dmg = RollAttackDamage();
            BattleManager.Instance.DamageBase(enemyFaction, dmg);
            if (lifestealFrac > 0f) Heal(dmg * lifestealFrac);

            if (isEvolved) PlayEvolvedAttackMotion(DefaultDirection());
        }

        // 힐러의 "공격" — attackDamage를 회복량으로 재해석해 대상 아군의 체력을 채운다.
        private void HealAlly(Unit ally)
        {
            if (ally == null) return;
            ally.ReceiveHeal(attackDamage);
        }

        public void ReceiveHeal(float amount)
        {
            Heal(amount);
        }

        private float DefaultDirection()
        {
            return faction == Faction.Player ? 1f : -1f;
        }

        // 치명타 확률이 있으면 굴려서 (기본 1.5배 + 추가 치명타 피해)를 적용한다.
        private float RollAttackDamage()
        {
            if (critChanceAdd > 0f && Random.value < critChanceAdd)
            {
                return attackDamage * (1.5f + critDamageAdd);
            }
            return attackDamage;
        }

        private void Heal(float amount)
        {
            if (amount <= 0f) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            UpdateHealthBarVisuals();
        }

        // 다른 유닛의 공격에서 호출 — 남은 체력 비율이 문턱 이하이면 확정 처치.
        // 방어력/피해감소에 상쇄되지 않도록 남은 체력보다 확실히 큰 값을 넣는다.
        public void ExecuteIfBelow(float threshold)
        {
            if (currentHealth <= 0f) return;
            if (currentHealth / maxHealth <= threshold) TakeDamage(currentHealth + maxHealth + flatArmor + 1f);
        }

        public void TakeDamage(float amount)
        {
            // 회피: 피해 자체를 무효화
            if (dodgeFrac > 0f && Random.value < dodgeFrac) return;

            // 받는 피해 감소(%) → 고정 방어력(flat) 순으로 적용
            amount *= damageTakenMult;
            amount = Mathf.Max(0f, amount - flatArmor);

            // 보호막이 먼저 흡수
            if (currentShield > 0f)
            {
                float absorbed = Mathf.Min(currentShield, amount);
                currentShield -= absorbed;
                amount -= absorbed;
            }

            currentHealth -= amount;
            UpdateHealthBarVisuals();
            if (currentHealth <= 0f)
            {
                if (faction == Faction.Enemy)
                {
                    BattleManager.Instance.OnEnemyKilled(this);
                    if (IsBossWinTarget) BattleManager.Instance.NotifyBossTargetDefeated();
                }
                BattleManager.Instance.UnregisterUnit(this);
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
        }

        // HP바(빨강/초록 등)는 currentHealth 비율만, 보호막바(회색)는 (체력+보호막) 비율만큼 채워서
        // 보호막바가 HP바 뒤에서 오른쪽으로 삐져나온 것처럼 보이게 한다.
        private void UpdateHealthBarVisuals()
        {
            if (healthFill != null) healthFill.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
            if (shieldFill != null) shieldFill.fillAmount = Mathf.Clamp01((currentHealth + currentShield) / maxHealth);
        }

        // ── 2성 진화 연출 (그래픽 에셋 없이 코드로만 구현) ──

        private void PlayEvolvedAttackMotion(float direction)
        {
            if (rect == null) return;
            if (lungeCoroutine != null) StopCoroutine(lungeCoroutine);
            lungeCoroutine = StartCoroutine(LungeRoutine(direction));
        }

        private System.Collections.IEnumerator LungeRoutine(float direction)
        {
            Vector2 basePos = rect.anchoredPosition;
            Vector2 lungePos = basePos + new Vector2(direction * EvolvedLungeDistance, 0f);
            float half = EvolvedLungeDuration * 0.5f;

            float t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                rect.anchoredPosition = Vector2.Lerp(basePos, lungePos, t / half);
                yield return null;
            }
            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                rect.anchoredPosition = Vector2.Lerp(lungePos, basePos, t / half);
                yield return null;
            }
            rect.anchoredPosition = basePos;
            lungeCoroutine = null;
        }

        // 2성 유닛에게 맞았을 때 표적 쪽에서 재생 — 본체 색 플래시 + (있으면) CFXR 이펙트 / 없으면 절차적 사각 버스트.
        public void PlayEvolvedHitEffect()
        {
            if (bodyImage != null) StartCoroutine(FlashRoutine());

            if (evolvedHitEffectPrefab != null) SpawnCfxrHitEffect();
            else SpawnHitBurst();
        }

        // 캔버스가 Screen Space - Camera이므로 RectTransform.position이 그대로 실제 월드 좌표다.
        // 카메라 쪽으로 살짝 당겨서 UI 평면(Canvas Plane Distance)보다 앞에서 그려지게 한다.
        private void SpawnCfxrHitEffect()
        {
            Camera cam = Camera.main;
            Vector3 worldPos = rect.position;
            if (cam != null)
            {
                Vector3 towardCamera = (cam.transform.position - worldPos).normalized;
                worldPos += towardCamera * evolvedHitEffectCameraOffset;
            }

            GameObject fx = Instantiate(evolvedHitEffectPrefab, worldPos, evolvedHitEffectPrefab.transform.rotation);
            fx.transform.localScale = evolvedHitEffectPrefab.transform.localScale * evolvedHitEffectScale;
            Destroy(fx, 3f); // CFXR 자체에도 소멸 로직이 있지만, 씬에 남지 않도록 안전망으로 정리
        }

        private System.Collections.IEnumerator FlashRoutine()
        {
            Color original = bodyImage.color;
            bodyImage.color = Color.white;
            yield return new WaitForSeconds(EvolvedFlashDuration);
            if (bodyImage != null) bodyImage.color = original;
        }

        private void SpawnHitBurst()
        {
            if (rect == null) return;

            var burstGO = new GameObject("EvolvedHitBurst", typeof(RectTransform), typeof(Image));
            var burstRect = (RectTransform)burstGO.transform;
            burstRect.SetParent(rect, false);
            burstRect.anchorMin = burstRect.anchorMax = new Vector2(0.5f, 0.5f);
            burstRect.pivot = new Vector2(0.5f, 0.5f);
            burstRect.anchoredPosition = Vector2.zero;
            burstRect.sizeDelta = new Vector2(HitBurstSize, HitBurstSize);
            burstRect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 45f));

            Image burstImage = burstGO.GetComponent<Image>();
            burstImage.color = HitBurstColor;
            burstImage.raycastTarget = false;

            StartCoroutine(BurstRoutine(burstRect, burstImage));
        }

        private System.Collections.IEnumerator BurstRoutine(RectTransform burstRect, Image burstImage)
        {
            float t = 0f;
            Vector3 startScale = Vector3.one * 0.4f;
            Vector3 endScale = Vector3.one * 1.6f;
            float startAlpha = burstImage.color.a;

            while (t < HitBurstDuration)
            {
                t += Time.deltaTime;
                float f = t / HitBurstDuration;
                if (burstRect == null) yield break;
                burstRect.localScale = Vector3.Lerp(startScale, endScale, f);
                Color c = burstImage.color;
                c.a = Mathf.Lerp(startAlpha, 0f, f);
                burstImage.color = c;
                yield return null;
            }
            if (burstRect != null) Destroy(burstRect.gameObject);
        }
    }
}
