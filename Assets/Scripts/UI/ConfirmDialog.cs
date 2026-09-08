using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickmanOfWar.UI
{
    public class ConfirmDialog : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;

        public void Show(string message, Action onYes, Action onNo = null)
        {
            gameObject.SetActive(true);
            if (messageText != null) messageText.text = message;

            yesButton.onClick.RemoveAllListeners();
            yesButton.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                onYes?.Invoke();
            });

            noButton.onClick.RemoveAllListeners();
            noButton.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                onNo?.Invoke();
            });
        }
    }
}
