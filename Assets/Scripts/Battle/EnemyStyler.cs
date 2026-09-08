using UnityEngine;
using UnityEngine.UI;

namespace StickmanOfWar.Battle
{
    // 스폰된 적 Unit 프리팹 인스턴스를 EnemyDefinition 에 맞춰 절차적으로 다시 꾸민다.
    // (전용 스프라이트 없이 색·크기·Image 도형 조합만으로 등급/아키타입을 구분)
    public static class EnemyStyler
    {
        private const string PiecePrefix = "Sil_";

        public static void Apply(Unit unit, EnemyDefinition def)
        {
            if (unit == null || def == null) return;

            var root = (RectTransform)unit.transform;
            root.sizeDelta = def.BodySize;

            Image bodyImg = unit.GetComponent<Image>();
            if (bodyImg != null)
            {
                bodyImg.sprite = null;
                bodyImg.color = def.BodyColor;
            }

            // 재사용/풀링 대비 이전 실루엣 조각 제거
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child.name.StartsWith(PiecePrefix)) Object.Destroy(child.gameObject);
            }

            float w = def.BodySize.x;
            float h = def.BodySize.y;
            Color accent = def.AccentColor;
            Color deep = Darken(def.BodyColor, 0.75f);

            switch (def.Shape)
            {
                case EnemyShape.Grunt:
                    Piece(root, "core", w * 0.5f, h * 0.5f, 0f, 0f, 0f, deep);
                    break;

                case EnemyShape.Runner:
                    Piece(root, "head", w * 0.95f, w * 0.95f, 0f, h * 0.5f - w * 0.5f, 0f, accent);
                    break;

                case EnemyShape.Skirmisher:
                    Piece(root, "bow", w * 0.28f, h * 0.9f, w * 0.6f, 0f, 0f, accent);
                    Piece(root, "core", w * 0.45f, h * 0.4f, 0f, 0f, 0f, deep);
                    break;

                case EnemyShape.Bruiser:
                    Piece(root, "shoulderL", w * 0.4f, h * 0.3f, -w * 0.55f, h * 0.22f, 0f, accent);
                    Piece(root, "shoulderR", w * 0.4f, h * 0.3f, w * 0.55f, h * 0.22f, 0f, accent);
                    break;

                case EnemyShape.Brute:
                    Piece(root, "head", w * 0.45f, w * 0.45f, 0f, h * 0.5f - w * 0.22f, 0f, accent);
                    Piece(root, "fistL", w * 0.32f, w * 0.32f, -w * 0.62f, -h * 0.25f, 0f, deep);
                    Piece(root, "fistR", w * 0.32f, w * 0.32f, w * 0.62f, -h * 0.25f, 0f, deep);
                    break;

                case EnemyShape.Caster:
                    Piece(root, "gem", w * 0.55f, w * 0.55f, 0f, 0f, 45f, accent);
                    break;

                case EnemyShape.Swarm:
                    Piece(root, "dotA", w * 0.5f, w * 0.5f, -w * 0.4f, h * 0.25f, 0f, accent);
                    Piece(root, "dotB", w * 0.5f, w * 0.5f, w * 0.4f, -h * 0.25f, 0f, accent);
                    break;

                case EnemyShape.Tank:
                    Piece(root, "plate", w * 0.85f, h * 0.32f, 0f, h * 0.3f, 0f, accent);
                    Piece(root, "core", w * 0.5f, h * 0.4f, 0f, -h * 0.05f, 0f, deep);
                    break;

                case EnemyShape.Horned:
                    Piece(root, "hornL", w * 0.3f, w * 0.3f, -w * 0.42f, h * 0.55f, 45f, accent);
                    Piece(root, "hornR", w * 0.3f, w * 0.3f, w * 0.42f, h * 0.55f, 45f, accent);
                    Piece(root, "belt", w * 0.9f, h * 0.14f, 0f, -h * 0.12f, 0f, accent);
                    Piece(root, "core", w * 0.4f, h * 0.35f, 0f, h * 0.05f, 0f, deep);
                    break;

                case EnemyShape.Colossus:
                    Piece(root, "crownL", w * 0.2f, w * 0.2f, -w * 0.3f, h * 0.52f, 45f, accent);
                    Piece(root, "crownC", w * 0.24f, w * 0.24f, 0f, h * 0.56f, 45f, accent);
                    Piece(root, "crownR", w * 0.2f, w * 0.2f, w * 0.3f, h * 0.52f, 45f, accent);
                    Piece(root, "sideL", w * 0.22f, h * 0.62f, -w * 0.58f, 0f, 0f, deep);
                    Piece(root, "sideR", w * 0.22f, h * 0.62f, w * 0.58f, 0f, 0f, deep);
                    Piece(root, "core", w * 0.42f, h * 0.42f, 0f, 0f, 0f, accent);
                    break;
            }

            // HP 바를 몸통 위로 올리고 폭을 맞춘 뒤 항상 최상단에 오도록.
            if (root.Find("HpBarBg") is RectTransform bar)
            {
                bar.anchoredPosition = new Vector2(0f, h * 0.5f + 10f);
                bar.sizeDelta = new Vector2(Mathf.Max(50f, w + 8f), 14f);
                bar.SetAsLastSibling();
            }
        }

        private static void Piece(RectTransform parent, string tag, float w, float h,
            float x, float y, float rotZ, Color color)
        {
            var go = new GameObject(PiecePrefix + tag, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
            rt.localEulerAngles = new Vector3(0f, 0f, rotZ);

            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        private static Color Darken(Color c, float factor)
        {
            return new Color(c.r * factor, c.g * factor, c.b * factor, c.a);
        }
    }
}
