using System.Text;
using StickmanOfWar.Map;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StickmanOfWar.UI
{
    // "자세히 보기" — 유닛 상세 스펙 팝업. 루트 캔버스 하위에 코드로 생성되며 모든 카드가 공유한다.
    // 어두운 전체 배경을 클릭하면 닫힌다. 씬 전환 시 캔버스와 함께 파괴됨.
    public class MercenaryDetailPopup : MonoBehaviour, IPointerClickHandler
    {
        private const float Width = 440f;
        private const float Pad = 20f;
        private const float TitleHeight = 32f;
        private const float TitleGap = 10f;

        private static MercenaryDetailPopup shared;

        private RectTransform panelRect;
        private TMP_Text titleText;
        private TMP_Text bodyText;

        public static void ShowFor(Component owner, MercenaryDefinition def)
        {
            if (def == null) return;
            if (shared == null) shared = Create(owner);
            shared.Populate(def);
            shared.gameObject.SetActive(true);
            shared.transform.SetAsLastSibling();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 배경(이 GameObject) 자체를 클릭했을 때만 닫는다. 패널 위 클릭은 무시.
            if (eventData.pointerCurrentRaycast.gameObject == gameObject)
            {
                gameObject.SetActive(false);
            }
        }

        private static MercenaryDetailPopup Create(Component owner)
        {
            Canvas canvas = owner.GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.rootCanvas.transform : owner.transform;

            TMP_FontAsset font = null;
            var anyText = owner.GetComponentInChildren<TMP_Text>();
            if (anyText != null) font = anyText.font;

            var go = new GameObject("MercenaryDetailPopup", typeof(RectTransform), typeof(Image));
            var backdrop = (RectTransform)go.transform;
            backdrop.SetParent(parent, false);
            backdrop.anchorMin = Vector2.zero;
            backdrop.anchorMax = Vector2.one;
            backdrop.offsetMin = Vector2.zero;
            backdrop.offsetMax = Vector2.zero;
            var bg = go.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.6f);
            bg.raycastTarget = true;
            var popup = go.AddComponent<MercenaryDetailPopup>();

            var panelGO = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            var prt = (RectTransform)panelGO.transform;
            prt.SetParent(backdrop, false);
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(Width, 300f);
            var pimg = panelGO.GetComponent<Image>();
            pimg.color = new Color(0.10f, 0.10f, 0.13f, 0.98f);
            pimg.raycastTarget = true; // 패널 클릭은 닫기에서 제외

            popup.panelRect = prt;
            popup.titleText = MakeText(prt, font, 23f, FontStyles.Bold);
            popup.bodyText = MakeText(prt, font, 18f, FontStyles.Normal);

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

        private void Populate(MercenaryDefinition def)
        {
            titleText.text = def.DisplayName;

            string pers = !string.IsNullOrEmpty(def.Personality2)
                ? def.Personality1 + " · " + def.Personality2
                : (string.IsNullOrEmpty(def.Personality1) ? "-" : def.Personality1);

            var sb = new StringBuilder();
            sb.AppendLine($"<color=#FFD75A>등급</color>  {def.Tier}성       <color=#FFD75A>종족</color>  {def.Race}");
            sb.AppendLine($"<color=#FFD75A>성격</color>  {pers}");
            sb.AppendLine();
            sb.AppendLine($"<color=#8FE0C8>소환 비용</color>   {def.DeployCost}");
            sb.AppendLine($"<color=#8FE0C8>체력</color>        {def.MaxHealth:0}");
            if (def.IsHealer)
            {
                sb.AppendLine($"<color=#8FE0C8>회복량</color>      {def.AttackDamage:0}");
                sb.AppendLine($"<color=#8FE0C8>회복 주기</color>   {def.AttackInterval:0.0}초  (초당 회복 {def.Dps:0.0})");
            }
            else
            {
                sb.AppendLine($"<color=#8FE0C8>공격력</color>      {def.AttackDamage:0}");
                sb.AppendLine($"<color=#8FE0C8>공격 주기</color>   {def.AttackInterval:0.0}초  (초당 피해 {def.Dps:0.0})");
            }
            sb.AppendLine($"<color=#8FE0C8>사거리</color>      {def.AttackRange:0}");
            sb.Append($"<color=#8FE0C8>이동 속도</color>   {def.MoveSpeed:0}");
            bodyText.text = sb.ToString();

            float innerW = Width - Pad * 2f;
            titleText.rectTransform.sizeDelta = new Vector2(innerW, TitleHeight);
            titleText.rectTransform.anchoredPosition = new Vector2(Pad, -Pad);
            bodyText.rectTransform.sizeDelta = new Vector2(innerW, 0f);
            bodyText.rectTransform.anchoredPosition = new Vector2(Pad, -(Pad + TitleHeight + TitleGap));
            bodyText.ForceMeshUpdate();
            panelRect.sizeDelta = new Vector2(Width, Pad + TitleHeight + TitleGap + bodyText.preferredHeight + Pad);
        }
    }
}
