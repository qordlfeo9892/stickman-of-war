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
        [SerializeField] private Button bagWidthButton;
        [SerializeField] private Button bagHeightButton;

        private Action onDismiss;

        public void Show(Action dismissCallback)
        {
            onDismiss = dismissCallback;
            gameObject.SetActive(true);

            restButton.gameObject.SetActive(true);
            partySlotButton.gameObject.SetActive(RunState.PartySizeCap < RunState.MaxPartySizeCap);
            bagWidthButton.gameObject.SetActive(RunState.Bag.Width < RunState.MaxBagDimension);
            bagHeightButton.gameObject.SetActive(RunState.Bag.Height < RunState.MaxBagDimension);

            restButton.onClick.RemoveAllListeners();
            restButton.onClick.AddListener(() => Choose(null));

            partySlotButton.onClick.RemoveAllListeners();
            partySlotButton.onClick.AddListener(() => Choose(RunState.IncreasePartySizeCap));

            bagWidthButton.onClick.RemoveAllListeners();
            bagWidthButton.onClick.AddListener(() => Choose(RunState.IncreaseBagWidth));

            bagHeightButton.onClick.RemoveAllListeners();
            bagHeightButton.onClick.AddListener(() => Choose(RunState.IncreaseBagHeight));
        }

        private void Choose(Action effect)
        {
            effect?.Invoke();
            gameObject.SetActive(false);
            onDismiss?.Invoke();
        }
    }
}
