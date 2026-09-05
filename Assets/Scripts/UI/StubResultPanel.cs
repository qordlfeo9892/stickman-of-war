using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickmanOfWar.UI
{
    public class StubResultPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Button confirmButton;

        private Action onDismiss;

        public void Show(string title, string body, Action dismissCallback)
        {
            onDismiss = dismissCallback;

            if (titleText != null) titleText.text = title;
            if (bodyText != null) bodyText.text = body;

            gameObject.SetActive(true);

            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(HandleConfirm);
        }

        private void HandleConfirm()
        {
            gameObject.SetActive(false);
            onDismiss?.Invoke();
        }
    }
}
