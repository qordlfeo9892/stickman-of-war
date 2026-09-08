using System;
using StickmanOfWar.Map;
using UnityEngine;
using UnityEngine.UI;

namespace StickmanOfWar.UI
{
    public class ReleaseMercenaryPanel : MonoBehaviour
    {
        [SerializeField] private MercenaryCardView[] cardViews = new MercenaryCardView[10];
        [SerializeField] private Button cancelButton;

        private Action<bool> onDone;

        public void Show(Action<bool> onDoneCallback)
        {
            onDone = onDoneCallback;
            gameObject.SetActive(true);
            Refresh();

            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(HandleCancel);
        }

        private void Refresh()
        {
            for (int i = 0; i < cardViews.Length; i++)
            {
                bool hasMerc = i < RunState.Roster.Count;
                cardViews[i].gameObject.SetActive(hasMerc);
                if (!hasMerc) continue;

                RosterMember merc = RunState.Roster[i];
                cardViews[i].SetupCompactWithAction(merc, "방출", true, () => HandleRelease(merc));
            }
        }

        private void HandleRelease(RosterMember merc)
        {
            RunState.ReleaseMercenary(merc);
            gameObject.SetActive(false);
            onDone?.Invoke(true);
        }

        private void HandleCancel()
        {
            gameObject.SetActive(false);
            onDone?.Invoke(false);
        }
    }
}
