using System.Collections.Generic;
using System.Linq;
using System.Text;
using StickmanOfWar.Map;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StickmanOfWar.UI
{
    // TFT식 특성 패널 — 한 시너지당 한 줄(아이콘 + 이름 + 2/3 형태의 유닛 수)만 표시하고,
    // 단계별 상세 효과(3오크 …, 6오크 …, 9오크 …)는 아이콘/행에 마우스 오버 시 툴팁으로 보여준다.
    // 이렇게 바꾼 이유: 기존에는 각 시너지 설명을 본문에 전부 이어붙여 패널 높이를 초과해
    // 아래쪽 시너지가 잘려 보였다.
    public class BattleSynergyPanel : MonoBehaviour
    {
        // 시너지가 하나도 없을 때 "시너지 없음"을 띄우는 용도로만 남겨둔 기존 텍스트.
        // 폰트(한글) 참조를 얻어 동적 생성 텍스트에 재사용하는 목적도 겸한다.
        [SerializeField] private TMP_Text synergyText;

        private RectTransform background;
        private readonly List<GameObject> rowObjects = new List<GameObject>();
        private SynergyTooltip tooltip;

        // ── 행 레이아웃 상수 (패널 로컬 좌표. 부모 스케일 0.75가 별도로 적용됨) ──
        private const float InnerWidth = 300f;
        private const float RowLeft = 10f;
        private const float HeaderOffset = 46f;   // "시너지" 타이틀 아래에서 첫 행이 시작되는 y
        private const float RowHeight = 30f;
        private const float RowGap = 6f;
        private const float IconSize = 24f;
        private const float LabelLeft = 32f;

        // ── 패널 높이 자동 조절 상수 (기존 로직 계승) ──
        private const float BottomPadding = 20f;
        private const float MinHeight = 90f;
        private const float TopOffset = 30f;
        private const float BottomBarHeight = 170f;
        private const float BottomMargin = 20f;

        private static readonly Dictionary<SynergyRank, string> RankColors = new Dictionary<SynergyRank, string>
        {
            { SynergyRank.None, "#888888" },
            { SynergyRank.Silver, "#C6C9CC" },
            { SynergyRank.Gold, "#FFD75A" },
            { SynergyRank.Prismatic, "#FF8FE0" },
        };

        private static readonly Dictionary<SynergyRank, Color> RankIconColors = new Dictionary<SynergyRank, Color>
        {
            { SynergyRank.None, new Color(0.30f, 0.30f, 0.33f) },
            { SynergyRank.Silver, new Color(0.78f, 0.79f, 0.80f) },
            { SynergyRank.Gold, new Color(1f, 0.84f, 0.35f) },
            { SynergyRank.Prismatic, new Color(1f, 0.56f, 0.88f) },
        };

        private void Start()
        {
            background = (RectTransform)transform;
            tooltip = SynergyTooltip.Create(this, synergyText != null ? synergyText.font : null);
            Refresh();
        }

        private void Refresh()
        {
            foreach (GameObject go in rowObjects) Destroy(go);
            rowObjects.Clear();

            List<(string trait, int count, int nextThreshold, string desc, SynergyRank rank)> progress =
                TraitSynergyDatabase.GetAllTraitProgress(RunState.Roster.Select(m => m.Definition).ToList());

            bool empty = progress.Count == 0;
            if (synergyText != null)
            {
                synergyText.gameObject.SetActive(empty);
                if (empty) synergyText.text = "시너지 없음";
            }

            for (int i = 0; i < progress.Count; i++)
                rowObjects.Add(BuildRow(progress[i], i));

            ResizeToContent(progress.Count);
        }

        private GameObject BuildRow(
            (string trait, int count, int nextThreshold, string desc, SynergyRank rank) p, int index)
        {
            var row = new GameObject($"SynergyRow_{p.trait}", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)row.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(InnerWidth, RowHeight);
            rt.anchoredPosition = new Vector2(RowLeft, -(HeaderOffset + index * (RowHeight + RowGap)));

            // 거의 투명한 배경 — 은은한 구분선 겸 마우스 오버 감지용 레이캐스트 타깃.
            var rowBg = row.GetComponent<Image>();
            rowBg.color = new Color(1f, 1f, 1f, 0.04f);
            rowBg.raycastTarget = true;

            // 아이콘: 전용 스프라이트가 아직 없어 랭크 색상 사각형으로 대체한다.
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var irt = (RectTransform)icon.transform;
            irt.SetParent(rt, false);
            irt.anchorMin = irt.anchorMax = new Vector2(0f, 0.5f);
            irt.pivot = new Vector2(0f, 0.5f);
            irt.sizeDelta = new Vector2(IconSize, IconSize);
            irt.anchoredPosition = new Vector2(2f, 0f);
            var iconImg = icon.GetComponent<Image>();
            iconImg.color = RankIconColors[p.rank];
            iconImg.raycastTarget = false;

            // 라벨: 이름 + "2/3" 형태의 유닛 수(TFT식).
            TMP_Text label = CreateText("Label", rt, 16f, TextAlignmentOptions.MidlineLeft);
            RectTransform lrt = label.rectTransform;
            lrt.anchorMin = new Vector2(0f, 0f);
            lrt.anchorMax = new Vector2(1f, 1f);
            lrt.offsetMin = new Vector2(LabelLeft, 0f);
            lrt.offsetMax = new Vector2(-4f, 0f);
            string color = RankColors[p.rank];
            label.text = $"<color={color}>{p.trait}  <size=88%>{p.count}/{p.nextThreshold}</size></color>";
            label.raycastTarget = false;

            var hover = row.AddComponent<SynergyRowHover>();
            hover.Init(tooltip, p.trait, rt);

            return row;
        }

        private TMP_Text CreateText(string objName, Transform parent, float size, TextAlignmentOptions align)
        {
            var go = new GameObject(objName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            if (synergyText != null) t.font = synergyText.font; // 한글 폰트 재사용
            t.fontSize = size;
            t.alignment = align;
            t.color = Color.white;
            return t;
        }

        private void ResizeToContent(int rowCount)
        {
            if (background == null) return;

            var canvasRect = transform.parent as RectTransform;
            float available = canvasRect != null
                ? canvasRect.rect.height - TopOffset - BottomBarHeight - BottomMargin
                : 620f;

            float content = rowCount > 0
                ? HeaderOffset + rowCount * RowHeight + (rowCount - 1) * RowGap + BottomPadding
                : HeaderOffset + 30f + BottomPadding;
            float height = Mathf.Clamp(content, MinHeight, Mathf.Max(MinHeight, available));

            background.sizeDelta = new Vector2(background.sizeDelta.x, height);
        }
    }

    // 행에 붙어 마우스 오버 시 시너지 툴팁을 띄운다. (UnitSlotButton의 툴팁 패턴과 동일)
    public class SynergyRowHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private SynergyTooltip tooltip;
        private string trait;
        private RectTransform anchor;

        public void Init(SynergyTooltip tooltip, string trait, RectTransform anchor)
        {
            this.tooltip = tooltip;
            this.trait = trait;
            this.anchor = anchor;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltip != null) tooltip.Show(trait, anchor);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltip != null) tooltip.Hide();
        }
    }

    // 시너지 단계별 상세 효과를 보여주는 팝업. 루트 캔버스 하위에 코드로 생성한다.
    public class SynergyTooltip : MonoBehaviour
    {
        private const float Width = 320f;
        private const float Pad = 14f;
        private const float TitleHeight = 26f;
        private const float TitleGap = 6f;
        private const float ScreenEdgeGap = 8f;

        private RectTransform rect;
        private RectTransform canvasRect;
        private TMP_Text titleText;
        private TMP_Text bodyText;

        public static SynergyTooltip Create(Component owner, TMP_FontAsset font)
        {
            Canvas canvas = owner.GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.rootCanvas.transform : owner.transform;

            var go = new GameObject("SynergyTooltip", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            var t = go.AddComponent<SynergyTooltip>();

            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(Width, 100f);

            // 커서 근처에 뜨는 팝업이라 레이캐스트를 막으면 hover가 깜빡이므로 통과시킨다.
            var cg = go.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            var bg = go.GetComponent<Image>();
            bg.color = new Color(0.04f, 0.04f, 0.07f, 0.96f);
            bg.raycastTarget = false;

            t.rect = rt;
            t.canvasRect = parent as RectTransform;
            t.titleText = MakeText(rt, font, 18f, FontStyles.Bold);
            t.bodyText = MakeText(rt, font, 15f, FontStyles.Normal);

            go.transform.SetAsLastSibling();
            go.SetActive(false);
            return t;
        }

        private static TMP_Text MakeText(Transform parent, TMP_FontAsset font, float size, FontStyles style)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = Color.white;
            t.raycastTarget = false;
            t.alignment = TextAlignmentOptions.TopLeft;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            return t;
        }

        public void Show(string trait, RectTransform anchor)
        {
            IReadOnlyList<(int count, string desc)> tiers = TraitSynergyDatabase.GetTiers(trait);
            if (tiers.Count == 0 || anchor == null)
            {
                Hide();
                return;
            }

            int have = TraitSynergyDatabase.CountTrait(RunState.Roster.Select(m => m.Definition).ToList(), trait);

            titleText.text = $"{trait}  <color=#FFD75A>{have}</color>";

            bool exact = TraitSynergyDatabase.IsExactActivation(trait);

            var sb = new StringBuilder();
            for (int i = 0; i < tiers.Count; i++)
            {
                bool active = TraitSynergyDatabase.IsTierActive(trait, i, have);
                string c = active ? RankHex(TraitSynergyDatabase.RankForTier(trait, i)) : "#6E6E73";
                string bullet = active ? "●" : "○";
                if (i > 0) sb.Append('\n');
                string prefix = exact ? $"정확히 {tiers[i].count}{trait}" : $"{tiers[i].count}{trait}";
                sb.Append($"<color={c}>{bullet} {prefix} : {tiers[i].desc}</color>");
            }
            bodyText.text = sb.ToString();

            float innerW = Width - Pad * 2f;
            titleText.rectTransform.sizeDelta = new Vector2(innerW, TitleHeight);
            titleText.rectTransform.anchoredPosition = new Vector2(Pad, -Pad);

            bodyText.rectTransform.sizeDelta = new Vector2(innerW, 0f);
            bodyText.rectTransform.anchoredPosition = new Vector2(Pad, -(Pad + TitleHeight + TitleGap));
            bodyText.ForceMeshUpdate();
            float bodyH = bodyText.preferredHeight;

            float totalH = Pad + TitleHeight + TitleGap + bodyH + Pad;
            rect.sizeDelta = new Vector2(Width, totalH);

            gameObject.SetActive(true);
            Reposition(anchor, totalH);
        }

        // 기본은 행의 오른쪽에 붙이고, 오른쪽 여백이 부족하면 왼쪽으로 뒤집는다.
        private void Reposition(RectTransform anchor, float totalH)
        {
            if (canvasRect == null) return;

            Vector2 rightLocal = LocalPointAt(anchor, anchor.rect.xMax);
            float halfW = canvasRect.rect.width * 0.5f;
            float halfH = canvasRect.rect.height * 0.5f;

            float x = rightLocal.x + ScreenEdgeGap;
            rect.pivot = new Vector2(0f, 0.5f);
            if (x + Width > halfW - ScreenEdgeGap)
            {
                x = LocalPointAt(anchor, anchor.rect.xMin).x - ScreenEdgeGap;
                rect.pivot = new Vector2(1f, 0.5f);
            }

            float y = Mathf.Clamp(rightLocal.y,
                -halfH + totalH * 0.5f + ScreenEdgeGap,
                halfH - totalH * 0.5f - ScreenEdgeGap);

            rect.anchoredPosition = new Vector2(x, y);
        }

        private Vector2 LocalPointAt(RectTransform anchor, float localX)
        {
            Vector3 world = anchor.TransformPoint(new Vector3(localX, anchor.rect.center.y, 0f));
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
            return local;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private static string RankHex(SynergyRank rank)
        {
            switch (rank)
            {
                case SynergyRank.Silver: return "#C6C9CC";
                case SynergyRank.Gold: return "#FFD75A";
                case SynergyRank.Prismatic: return "#FF8FE0";
                default: return "#888888";
            }
        }
    }
}
