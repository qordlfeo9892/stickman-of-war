using StickmanOfWar.Map;
using StickmanOfWar.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StickmanOfWar.Battle
{
    public class UnitSlotButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private int slotIndex;
        [SerializeField] private Button button;
        [SerializeField] private bool locked;
        [SerializeField] private TMP_Text costLabel;

        private bool effectivelyLocked;

        private void Start()
        {
            effectivelyLocked = locked || slotIndex >= RunState.Roster.Count;
            if (effectivelyLocked)
            {
                button.interactable = false;
                return;
            }

            if (costLabel != null) costLabel.text = BattleManager.Instance.GetCost(slotIndex).ToString();
            button.onClick.AddListener(OnClick);
        }

        private void Update()
        {
            if (effectivelyLocked || BattleManager.Instance == null) return;
            button.interactable = BattleManager.Instance.Energy >= BattleManager.Instance.GetCost(slotIndex);
        }

        private void OnClick()
        {
            BattleManager.Instance.TrySpawnPlayerUnit(slotIndex);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (slotIndex < 0 || slotIndex >= RunState.Roster.Count) return;
            if (UnitTooltipPanel.Instance == null) return;
            UnitTooltipPanel.Instance.Show(RunState.Roster[slotIndex], (RectTransform)transform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (UnitTooltipPanel.Instance != null) UnitTooltipPanel.Instance.Hide();
        }
    }
}
