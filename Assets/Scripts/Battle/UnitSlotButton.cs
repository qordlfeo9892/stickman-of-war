using UnityEngine;
using UnityEngine.UI;

namespace StickmanOfWar.Battle
{
    public class UnitSlotButton : MonoBehaviour
    {
        [SerializeField] private int slotIndex;
        [SerializeField] private Button button;
        [SerializeField] private bool locked;

        private void Start()
        {
            if (locked)
            {
                button.interactable = false;
                return;
            }
            button.onClick.AddListener(OnClick);
        }

        private void Update()
        {
            if (locked || BattleManager.Instance == null) return;
            button.interactable = BattleManager.Instance.Gold >= BattleManager.Instance.GetCost(slotIndex);
        }

        private void OnClick()
        {
            BattleManager.Instance.TrySpawnPlayerUnit(slotIndex);
        }
    }
}
