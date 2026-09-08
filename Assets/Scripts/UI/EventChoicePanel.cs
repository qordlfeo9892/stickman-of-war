using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickmanOfWar.UI
{
    public class EventChoicePanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Button choice1Button;
        [SerializeField] private TMP_Text choice1Label;
        [SerializeField] private Button choice2Button;
        [SerializeField] private TMP_Text choice2Label;

        public void Show(string title, string body, string choice1, string choice2, Action onChoice1, Action onChoice2)
        {
            gameObject.SetActive(true);
            if (titleText != null) titleText.text = title;
            if (bodyText != null) bodyText.text = body;
            if (choice1Label != null) choice1Label.text = choice1;
            if (choice2Label != null) choice2Label.text = choice2;

            choice1Button.onClick.RemoveAllListeners();
            choice1Button.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                onChoice1?.Invoke();
            });

            choice2Button.onClick.RemoveAllListeners();
            choice2Button.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                onChoice2?.Invoke();
            });
        }
    }
}
