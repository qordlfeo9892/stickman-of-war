using System;
using System.Text;
using StickmanOfWar.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickmanOfWar.UI
{
    // 일반 전투 승리 후 뜨는 "용병 합류 제안" 팝업.
    // 루트 캔버스 하위에 코드로 생성되어 재사용된다(씬 수정 없음). 씬 전환 시 캔버스와 함께 파괴됨.
    public class MercenaryOfferPopup : MonoBehaviour
    {
        private const float Width = 500f;
        private const float Pad = 22f;
        private const float TitleH = 34f;
        private const float RowH = 46f;
        private const float RowGap = 10f;

        private static MercenaryOfferPopup shared;

        private RectTransform panelRect;
        private TMP_Text titleText;
        private TMP_Text bodyText;
        private RectTransform rowColumn;
        private TMP_FontAsset font;

        private MercenaryDefinition offered;
        private Action onResolved;

        public static void Show(Component owner, MercenaryDefinition merc, Action onResolved)
        {
            if (merc == null) { onResolved?.Invoke(); return; }
            if (shared == null) shared = Create(owner);
            shared.offered = merc;
            shared.onResolved = onResolved;
            shared.Populate();
            shared.gameObject.SetActive(true);
            shared.transform.SetAsLastSibling();
        }

        private void Resolve()
        {
            gameObject.SetActive(false);
            Action cb = onResolved;
            onResolved = null;
            cb?.Invoke();
        }

        private static MercenaryOfferPopup Create(Component owner)
        {
            Canvas canvas = owner.GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.rootCanvas.transform : owner.transform;

            TMP_FontAsset f = null;
            var anyText = owner.GetComponentInChildren<TMP_Text>();
            if (anyText != null) f = anyText.font;

            var go = new GameObject("MercenaryOfferPopup", typeof(RectTransform), typeof(Image));
            var backdrop = (RectTransform)go.transform;
            backdrop.SetParent(parent, false);
            backdrop.anchorMin = Vector2.zero;
            backdrop.anchorMax = Vector2.one;
            backdrop.offsetMin = Vector2.zero;
            backdrop.offsetMax = Vector2.zero;
            var bg = go.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.7f);
            bg.raycastTarget = true; // 뒤쪽 클릭 차단 (전투 후 강제 선택)
            var popup = go.AddComponent<MercenaryOfferPopup>();
            popup.font = f;

            var panelGO = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            var prt = (RectTransform)panelGO.transform;
            prt.SetParent(backdrop, false);
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(Width, 360f);
            panelGO.GetComponent<Image>().color = new Color(0.11f, 0.11f, 0.14f, 0.99f);
            popup.panelRect = prt;

            popup.titleText = MakeText(prt, f, 24f, FontStyles.Bold);
            popup.bodyText = MakeText(prt, f, 18f, FontStyles.Normal);

            var colGO = new GameObject("Rows", typeof(RectTransform));
            var col = (RectTransform)colGO.transform;
            col.SetParent(prt, false);
            col.anchorMin = col.anchorMax = new Vector2(0f, 1f);
            col.pivot = new Vector2(0f, 1f);
            popup.rowColumn = col;

            go.SetActive(false);
            return popup;
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

        private Button MakeRow(int index, string label, Color color, Action onClick)
        {
            var go = new GameObject("Row" + index, typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = (RectTransform)go.transform;
            rt.SetParent(rowColumn, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(Width - Pad * 2f, RowH);
            rt.anchoredPosition = new Vector2(0f, -index * (RowH + RowGap));

            go.GetComponent<Image>().color = color;
            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick());

            var txt = MakeText(rt, font, 18f, FontStyles.Bold);
            txt.alignment = TextAlignmentOptions.Center;
            var trt = txt.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            txt.text = label;
            return btn;
        }

        private void Populate()
        {
            for (int i = rowColumn.childCount - 1; i >= 0; i--)
            {
                GameObject stale = rowColumn.GetChild(i).gameObject;
                stale.SetActive(false); // Destroy는 프레임 끝에 처리되므로 즉시 비활성화
                Destroy(stale);
            }

            titleText.text = "새 용병 합류 제안";

            string pers = !string.IsNullOrEmpty(offered.Personality2)
                ? offered.Personality1 + " · " + offered.Personality2
                : (string.IsNullOrEmpty(offered.Personality1) ? "-" : offered.Personality1);

            var sb = new StringBuilder();
            sb.AppendLine($"<b>{offered.DisplayName}</b>");
            sb.AppendLine($"<color=#FFD75A>{offered.Tier}성 · {offered.Race} · {pers}</color>");
            sb.AppendLine($"소환 비용 {offered.DeployCost}   체력 {offered.MaxHealth:0}   공격력 {offered.AttackDamage:0}   사거리 {offered.AttackRange:0}");

            bool hasRoom = RunState.CanHireMercenary();
            // 이미 보유한 용병이면 진화라서 슬롯이 필요 없다 — 스쿼드가 가득 차 있어도 받을 수 있다.
            int ownedStar = RunState.GetMercenaryStar(offered.Id);
            bool alreadyOwned = ownedStar > 0;
            bool maxedOut = ownedStar >= RosterMember.MaxStar;
            int rowIndex = 0;
            var acceptColor = new Color(0.20f, 0.55f, 0.30f, 1f);
            var neutralColor = new Color(0.30f, 0.30f, 0.35f, 1f);
            var swapColor = new Color(0.35f, 0.30f, 0.20f, 1f);

            if (hasRoom || alreadyOwned)
            {
                string label = maxedOut ? $"환전 (+{offered.DeployCost * 2}G)" : (alreadyOwned ? "진화! (2성)" : "영입");
                MakeRow(rowIndex++, label, acceptColor, () =>
                {
                    RunState.TryHireMercenaryFree(offered);
                    Resolve();
                });
            }
            else
            {
                sb.AppendLine();
                sb.AppendLine($"<color=#FF8F8F>스쿼드가 가득 찼습니다 ({RunState.PartySizeCurrent}/{RunState.PartySizeCap}). 교체할 용병을 고르세요.</color>");
                foreach (RosterMember m in new System.Collections.Generic.List<RosterMember>(RunState.Roster))
                {
                    RosterMember drop = m;
                    MakeRow(rowIndex++, $"{drop.DisplayName} 방출하고 영입", swapColor, () =>
                    {
                        RunState.ReleaseMercenary(drop);
                        RunState.AddMercenaryUnchecked(offered);
                        Resolve();
                    });
                }
            }

            MakeRow(rowIndex++, "넘기기", neutralColor, Resolve);

            bodyText.text = sb.ToString();

            float innerW = Width - Pad * 2f;
            titleText.rectTransform.sizeDelta = new Vector2(innerW, TitleH);
            titleText.rectTransform.anchoredPosition = new Vector2(Pad, -Pad);

            bodyText.rectTransform.sizeDelta = new Vector2(innerW, 0f);
            bodyText.rectTransform.anchoredPosition = new Vector2(Pad, -(Pad + TitleH + 8f));
            bodyText.ForceMeshUpdate();
            float bodyH = bodyText.preferredHeight;

            float colTop = Pad + TitleH + 8f + bodyH + 16f;
            rowColumn.anchoredPosition = new Vector2(Pad, -colTop);
            float rowsH = rowIndex * RowH + (rowIndex - 1) * RowGap;

            panelRect.sizeDelta = new Vector2(Width, colTop + rowsH + Pad);
        }
    }
}
