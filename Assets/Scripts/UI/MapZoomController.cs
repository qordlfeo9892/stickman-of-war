using UnityEngine;
using UnityEngine.EventSystems;

namespace StickmanOfWar.UI
{
    // 마우스 휠로 지도를 확대/축소한다.
    // ScrollRect의 Viewport에 런타임으로 부착되어(= 이벤트 계층상 ScrollRect보다 먼저 잡힘)
    // 휠 이벤트를 가로채 소비하므로, 휠은 세로 스크롤 대신 줌으로 동작한다. (드래그 이동은 그대로)
    public class MapZoomController : MonoBehaviour, IScrollHandler
    {
        private const float MinZoom = 0.55f;
        private const float MaxZoom = 1.9f;
        private const float ZoomStep = 0.12f;

        private RectTransform content;
        private float zoom = 1f;

        public void Init(RectTransform mapContent)
        {
            content = mapContent;
            zoom = (content != null && content.localScale.x > 0.01f) ? content.localScale.x : 1f;
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (content == null) return;

            float delta = eventData.scrollDelta.y;
            if (Mathf.Abs(delta) < 0.01f) return;

            float prev = zoom;
            zoom = Mathf.Clamp(zoom + Mathf.Sign(delta) * ZoomStep, MinZoom, MaxZoom);
            if (Mathf.Approximately(zoom, prev)) return;

            // 커서 밑 지점이 화면에서 크게 튀지 않도록 세로축 기준으로 보정한다.
            // (가로 스크롤은 비활성이라 X는 ScrollRect가 자동 중앙 정렬)
            Camera cam = eventData.pressEventCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(content, eventData.position, cam, out Vector2 localBefore);

            content.localScale = new Vector3(zoom, zoom, 1f);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(content, eventData.position, cam, out Vector2 localAfter);
            float dy = (localBefore.y - localAfter.y) * zoom;
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, content.anchoredPosition.y + dy);
        }
    }
}
