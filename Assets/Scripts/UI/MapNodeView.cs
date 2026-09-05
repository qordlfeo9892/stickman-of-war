using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StickmanOfWar.UI
{
    public class MapNodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private GameObject tooltipRoot;

        public int NodeId { get; private set; }

        public void Setup(int nodeId, string label, Sprite icon, Color fallbackColor, bool interactable, bool completed, Action<int> onClick)
        {
            NodeId = nodeId;

            if (labelText != null)
            {
                labelText.text = label;
            }

            if (tooltipRoot != null)
            {
                tooltipRoot.SetActive(false);
            }

            if (iconImage != null)
            {
                if (icon != null)
                {
                    iconImage.sprite = icon;
                    iconImage.type = Image.Type.Simple;
                    iconImage.preserveAspect = true;
                    iconImage.color = completed ? new Color(1f, 1f, 1f, 0.4f) : Color.white;
                }
                else
                {
                    iconImage.sprite = null;
                    iconImage.color = completed ? new Color(fallbackColor.r, fallbackColor.g, fallbackColor.b, 0.35f) : fallbackColor;
                }
            }

            if (button != null)
            {
                button.interactable = interactable;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClick?.Invoke(NodeId));
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltipRoot == null) return;
            transform.SetAsLastSibling();
            tooltipRoot.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltipRoot == null) return;
            tooltipRoot.SetActive(false);
        }
    }
}
