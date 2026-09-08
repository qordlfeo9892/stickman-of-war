using StickmanOfWar.Map;
using TMPro;
using UnityEngine;

namespace StickmanOfWar.UI
{
    public class UnitTooltipPanel : MonoBehaviour
    {
        public static UnitTooltipPanel Instance { get; private set; }

        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text traitText;
        [SerializeField] private TMP_Text statsText;

        private const float VerticalGap = 12f;

        private RectTransform rect;
        private RectTransform canvasRect;

        private void Awake()
        {
            Instance = this;
            rect = (RectTransform)transform;
            canvasRect = (RectTransform)transform.parent;
            gameObject.SetActive(false);
        }

        public void Show(RosterMember member, RectTransform anchor)
        {
            if (member == null || anchor == null) return;

            string starTag = member.IsEvolved ? " ★★" : "";
            nameText.text = member.DisplayName + starTag;
            traitText.text = string.Join(" · ", member.Traits);
            statsText.text = member.IsHealer
                ? $"소환 비용 {member.DeployCost}\nHP {member.MaxHealth:0}\n초당 회복량 {member.AttackDamage:0}\n회복 주기 {member.AttackInterval:0.0}\n사거리 {member.AttackRange:0}\n이동속도 {member.MoveSpeed:0}"
                : $"소환 비용 {member.DeployCost}\nHP {member.MaxHealth:0}\n공격력 {member.AttackDamage:0}\n공격주기 {member.AttackInterval:0.0}\n사거리 {member.AttackRange:0}\n이동속도 {member.MoveSpeed:0}";

            gameObject.SetActive(true);

            // 슬롯의 실제 화면상 윗변 중앙 지점을 구해서 그 바로 위에 붙인다 —
            // 슬롯이 BottomBar 하위라 좌표계가 달라 anchoredPosition을 그대로 쓸 수 없음.
            Vector3 topWorld = anchor.TransformPoint(new Vector3(0f, anchor.rect.yMax, 0f));
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, topWorld);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 local))
            {
                float halfWidth = canvasRect.rect.width * 0.5f;
                float clampedX = Mathf.Clamp(local.x, -halfWidth + rect.sizeDelta.x * 0.5f, halfWidth - rect.sizeDelta.x * 0.5f);
                rect.anchoredPosition = new Vector2(clampedX, local.y + VerticalGap);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
