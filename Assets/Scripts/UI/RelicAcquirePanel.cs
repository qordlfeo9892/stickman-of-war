using System;
using StickmanOfWar.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickmanOfWar.UI
{
    public class RelicAcquirePanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text infoText;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button declineButton;
        [SerializeField] private BagPanel bagPanel;

        private Action onResolved;
        private RelicDefinition currentDef;

        public void Show(RelicDefinition def, Action onResolvedCallback)
        {
            currentDef = def;
            onResolved = onResolvedCallback;
            gameObject.SetActive(true);

            if (infoText != null)
            {
                infoText.text = $"새 유물을 발견했다: {def.DisplayName}\n{def.EffectDescription}\n획득하시겠습니까?";
            }

            acceptButton.onClick.RemoveAllListeners();
            acceptButton.onClick.AddListener(HandleAccept);

            declineButton.onClick.RemoveAllListeners();
            declineButton.onClick.AddListener(HandleDecline);
        }

        private void HandleAccept()
        {
            gameObject.SetActive(false);
            bagPanel.ShowForNewRelic(currentDef, placed => onResolved?.Invoke());
        }

        private void HandleDecline()
        {
            gameObject.SetActive(false);
            onResolved?.Invoke();
        }
    }
}
