using System;
using StickmanOfWar.Map;
using UnityEngine;
using UnityEngine.UI;

namespace StickmanOfWar.UI
{
    public class CampfirePanel : MonoBehaviour
    {
        [SerializeField] private Button restButton;
        [SerializeField] private Button partySlotButton;
        [SerializeField] private Button bagSlotButton;

        private Action onDismiss;

        public void Show(Action dismissCallback)
        {
            onDismiss = dismissCallback;
            gameObject.SetActive(true);

            restButton.gameObject.SetActive(true);
            partySlotButton.gameObject.SetActive(RunState.PartySizeCap < RunState.MaxPartySizeCap);
            bagSlotButton.gameObject.SetActive(RunState.BagSizeCap < RunState.MaxBagSizeCap);

            restButton.onClick.RemoveAllListeners();
            restButton.onClick.AddListener(() => Choose(null));

            partySlotButton.onClick.RemoveAllListeners();
            partySlotButton.onClick.AddListener(() => Choose(RunState.IncreasePartySizeCap));

            bagSlotButton.onClick.RemoveAllListeners();
            bagSlotButton.onClick.AddListener(() => Choose(RunState.IncreaseBagSizeCap));
        }

        private void Choose(Action effect)
        {
            effect?.Invoke();
            gameObject.SetActive(false);
            onDismiss?.Invoke();
        }
    }
}
