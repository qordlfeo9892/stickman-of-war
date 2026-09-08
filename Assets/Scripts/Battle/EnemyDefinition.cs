using UnityEngine;

namespace StickmanOfWar.Battle
{
    public enum EnemyRank
    {
        Normal,
        Elite,
        Boss,
    }

    // 스프라이트 없이 Image 도형 조합으로 실루엣을 만들기 위한 종류 구분.
    public enum EnemyShape
    {
        Grunt,       // 평범한 몸통
        Runner,      // 얇고 길쭉 + 작은 머리
        Skirmisher,  // 몸통 옆 세로 막대(원거리)
        Bruiser,     // 어깨 견장 2개
        Brute,       // 큰 몸통 + 머리 + 주먹
        Caster,      // 45도 회전한 보석(원거리)
        Swarm,       // 아주 작은 몸 + 점 2개
        Tank,        // 상판 + 내부 코어
        Horned,      // 뿔 2개 + 벨트 (엘리트 느낌)
        Colossus,    // 왕관 + 측면 블록 + 코어 (보스 느낌)
    }

    // 보스 전투 패턴 (Rank == Boss 일 때만 의미).
    public enum BossPattern
    {
        Vanguard,    // 표준: 강력한 선봉 + 잡몹. 적 기지 파괴로 승리
        Juggernaut,  // 거대 단일체: 이 유닛을 처치하면 즉시 승리. 잡몹은 드묾
        Relentless,  // 저체력·초고공격 보스가 연속으로 여러 번 등장. 정해진 횟수만큼 처치하면 승리
        Summoner,    // 표준 보스 + 잡몹을 매우 자주 소환. 적 기지 파괴로 승리
    }

    public class EnemyDefinition
    {
        public string Id;
        public string DisplayName;
        public int Act;
        public EnemyRank Rank;

        public float MaxHealth;
        public float AttackDamage;
        public float AttackInterval;
        public float AttackRange;
        public float MoveSpeed;

        public EnemyShape Shape;
        public Color BodyColor;
        public Color AccentColor;
        public Vector2 BodySize;

        // ── 보스 전용 ──
        public BossPattern Pattern = BossPattern.Vanguard;
        public int DefeatsToWin;        // >=1: 보스를 이만큼 처치하면 승리. 0: 기지 파괴로 승리
        public float BossRespawnDelay;  // Relentless: 재등장까지 대기 시간

        public EnemyDefinition(string id, string displayName, int act, EnemyRank rank,
            float maxHealth, float attackDamage, float attackInterval, float attackRange, float moveSpeed,
            EnemyShape shape, Color bodyColor, Color accentColor, Vector2 bodySize)
        {
            Id = id;
            DisplayName = displayName;
            Act = act;
            Rank = rank;
            MaxHealth = maxHealth;
            AttackDamage = attackDamage;
            AttackInterval = attackInterval;
            AttackRange = attackRange;
            MoveSpeed = moveSpeed;
            Shape = shape;
            BodyColor = bodyColor;
            AccentColor = accentColor;
            BodySize = bodySize;
        }
    }
}
