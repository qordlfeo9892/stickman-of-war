using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StickmanOfWar.Map
{
    public static class RelicDatabase
    {
        private static readonly Vector2Int[] Single = { new Vector2Int(0, 0) };
        private static readonly Vector2Int[] Domino = { new Vector2Int(0, 0), new Vector2Int(1, 0) };
        private static readonly Vector2Int[] TrominoI = { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
        private static readonly Vector2Int[] Square = { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) };
        private static readonly Vector2Int[] LTromino = { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) };
        private static readonly Vector2Int[] LTetromino = { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 2) };
        private static readonly Vector2Int[] TTetromino = { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, 1) };
        private static readonly Vector2Int[] STetromino = { new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) };
        private static readonly Vector2Int[] Plus = { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(1, 2) };

        private static readonly RelicDefinition[] All =
        {
            new RelicDefinition("ring_flame", "화염의 반지", "공격력 +10%", Single.ToList(), new Color(0.85f, 0.25f, 0.2f)),
            new RelicDefinition("amulet_frost", "얼음 부적", "받는 피해 -5%", Single.ToList(), new Color(0.3f, 0.65f, 0.9f)),

            new RelicDefinition("twin_daggers", "쌍둥이 단검", "공격속도 +8%", Domino.ToList(), new Color(0.6f, 0.6f, 0.65f)),
            new RelicDefinition("silver_belt", "은빛 벨트", "이동속도 +10%", Domino.ToList(), new Color(0.75f, 0.75f, 0.8f)),

            new RelicDefinition("long_spear", "긴 창", "사거리 +15%", TrominoI.ToList(), new Color(0.55f, 0.4f, 0.25f)),
            new RelicDefinition("chain_armor", "사슬 갑옷", "최대 체력 +15%", TrominoI.ToList(), new Color(0.5f, 0.5f, 0.55f)),

            new RelicDefinition("ancient_shield", "고대의 방패", "받는 피해 -15%", Square.ToList(), new Color(0.6f, 0.5f, 0.2f)),
            new RelicDefinition("dragon_heart", "용의 심장", "최대 체력 +25%", Square.ToList(), new Color(0.8f, 0.15f, 0.15f)),

            new RelicDefinition("raven_claw", "까마귀 발톱", "치명타 확률 +10%", LTromino.ToList(), new Color(0.2f, 0.2f, 0.25f)),
            new RelicDefinition("twisted_staff", "뒤틀린 지팡이", "스킬 피해 +10%", LTromino.ToList(), new Color(0.4f, 0.2f, 0.55f)),

            new RelicDefinition("giant_gauntlet", "거인의 건틀릿", "공격력 +20%", LTetromino.ToList(), new Color(0.65f, 0.45f, 0.2f)),
            new RelicDefinition("rooted_staff", "뿌리내린 지팡이", "마나 회복 +10%", LTetromino.ToList(), new Color(0.3f, 0.55f, 0.3f)),

            new RelicDefinition("scales_of_judgement", "심판의 저울", "체력 10% 이하 적 처형 확률 증가", TTetromino.ToList(), new Color(0.9f, 0.8f, 0.3f)),
            new RelicDefinition("ancient_rune", "고대 룬", "골드 획득 +15%", TTetromino.ToList(), new Color(0.85f, 0.7f, 0.2f)),

            new RelicDefinition("tangled_chains", "뒤엉킨 사슬", "적 이동속도 감소", STetromino.ToList(), new Color(0.35f, 0.35f, 0.4f)),
            new RelicDefinition("shadow_cloak", "그림자 망토", "회피율 +10%", STetromino.ToList(), new Color(0.2f, 0.15f, 0.3f)),

            new RelicDefinition("tree_of_life", "생명의 나무", "초당 체력 재생 +2", Plus.ToList(), new Color(0.3f, 0.7f, 0.35f)),
            new RelicDefinition("chaos_orb", "혼돈의 구슬", "무작위 효과 대폭 상승", Plus.ToList(), new Color(0.7f, 0.25f, 0.75f)),
        };

        public static RelicDefinition[] GetAll()
        {
            return All;
        }

        public static RelicDefinition GetById(string id)
        {
            return All.FirstOrDefault(r => r.Id == id);
        }

        public static RelicDefinition GetRandom()
        {
            return All[Random.Range(0, All.Length)];
        }

        public static int GetCost(RelicDefinition def)
        {
            return def.Shape.Count * 15;
        }
    }
}
