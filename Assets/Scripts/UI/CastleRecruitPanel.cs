using System;
using StickmanOfWar.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickmanOfWar.UI
{
    public class CastleRecruitPanel : MonoBehaviour
    {
        [SerializeField] private MercenaryCardView[] cardViews = new MercenaryCardView[3];
        [SerializeField] private Button rerollButton;
        [SerializeField] private TMP_Text diceCountText;
        [SerializeField] private Button hireAllButton;
        [SerializeField] private Button leaveButton;

        private readonly MercenaryDefinition[] currentOffers = new MercenaryDefinition[3];
        private Action onDismiss;

        public void Show(Action dismissCallback)
        {
            onDismiss = dismissCallback;
            gameObject.SetActive(true);

            bool hireAll = RunState.CurrentAct == 1;
            hireAllButton.gameObject.SetActive(hireAll);
            // 1막은 3명을 무조건 전부 고용해야 전투로 넘어갈 수 있으므로 떠나기가 없다.
            // 2/3막은 파티가 이미 꽉 찼을 수 있어 떠나기가 필요하다.
            leaveButton.gameObject.SetActive(!hireAll);

            RollOffers();
            RefreshRerollButton();

            leaveButton.onClick.RemoveAllListeners();
            leaveButton.onClick.AddListener(HandleLeave);

            hireAllButton.onClick.RemoveAllListeners();
            hireAllButton.onClick.AddListener(HandleHireAll);

            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(HandleReroll);
        }

        private void RollOffers()
        {
            int act = RunState.CurrentAct;
            bool hireAll = act == 1;

            MercenaryDefinition[] offers = hireAll
                ? MercenaryDatabase.RollOffersWeighted(1, 3)
                : MercenaryDatabase.RollOffersFixedTier(Mathf.Clamp(act + 2, 1, 5), 3);

            for (int i = 0; i < 3; i++)
            {
                currentOffers[i] = offers[i];
                if (hireAll)
                {
                    cardViews[i].Setup(offers[i]);
                }
                else
                {
                    int idx = i;
                    // 이미 보유한 용병이면 진화라 슬롯이 필요 없다 — 정원이 가득 차 있어도 고를 수 있다.
                    int ownedStar = RunState.GetMercenaryStar(offers[i].Id);
                    bool canPick = RunState.CanHireMercenary() || ownedStar > 0;
                    string label = ownedStar >= RosterMember.MaxStar ? "환전" : (ownedStar > 0 ? "진화!" : "고용");
                    cardViews[i].Setup(offers[i], label, canPick, () => HandlePickOne(idx));
                }
            }
        }

        private void HandlePickOne(int idx)
        {
            if (!RunState.TryHireMercenaryFree(currentOffers[idx])) return;
            ClosePanel();
        }

        private void HandleHireAll()
        {
            for (int i = 0; i < 3; i++)
            {
                RunState.TryHireMercenaryFree(currentOffers[i]);
            }
            ClosePanel();
        }

        private void HandleReroll()
        {
            if (!RunState.TryUseDiceForReroll()) return;
            RollOffers();
            RefreshRerollButton();
        }

        private void HandleLeave()
        {
            ClosePanel();
        }

        private void RefreshRerollButton()
        {
            rerollButton.interactable = RunState.DiceCount > 0;
            if (diceCountText != null) diceCountText.text = $"주사위 {RunState.DiceCount}개";
        }

        private void ClosePanel()
        {
            gameObject.SetActive(false);
            onDismiss?.Invoke();
        }
    }
}
