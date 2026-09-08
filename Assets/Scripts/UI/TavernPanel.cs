using System;
using StickmanOfWar.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickmanOfWar.UI
{
    public class TavernPanel : MonoBehaviour
    {
        [SerializeField] private MercenaryCardView[] cardViews = new MercenaryCardView[3];
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private Button leaveButton;
        [SerializeField] private Button partySlotButton;
        [SerializeField] private TMP_Text partySlotLabel;
        [SerializeField] private ConfirmDialog confirmDialog;

        private readonly MercenaryDefinition[] currentOffers = new MercenaryDefinition[3];
        private readonly bool[] purchased = new bool[3];
        private Action onDismiss;

        public void Show(Action dismissCallback)
        {
            onDismiss = dismissCallback;
            gameObject.SetActive(true);

            MercenaryDefinition[] offers = MercenaryDatabase.RollOffersWeighted(RunState.CurrentAct, 3);
            for (int i = 0; i < 3; i++)
            {
                currentOffers[i] = offers[i];
                purchased[i] = false;
            }
            RefreshCards();
            RefreshGoldText();
            RefreshPartySlotButton();

            leaveButton.onClick.RemoveAllListeners();
            leaveButton.onClick.AddListener(HandleLeave);

            partySlotButton.onClick.RemoveAllListeners();
            partySlotButton.onClick.AddListener(HandleBuyPartySlot);
        }

        private void RefreshPartySlotButton()
        {
            bool available = RunState.PartySizeCap < RunState.MaxPartySizeCap;
            partySlotButton.gameObject.SetActive(available);
            if (!available) return;

            int cost = RunState.PartySizeCap * 30;
            if (partySlotLabel != null) partySlotLabel.text = $"파티 정원 +1 ({cost}G)";
            partySlotButton.interactable = RunState.Gold >= cost;
        }

        private void HandleBuyPartySlot()
        {
            int cost = RunState.PartySizeCap * 30;
            confirmDialog.Show($"파티 정원을 {cost}G에 늘리시겠습니까?", () =>
            {
                if (!RunState.TryPurchasePartySlot(cost)) return;
                RefreshCards();
                RefreshGoldText();
                RefreshPartySlotButton();
            });
        }

        private void RefreshCards()
        {
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                MercenaryDefinition def = currentOffers[idx];
                int cost = MercenaryDatabase.GetTavernCost(def);
                bool bought = purchased[idx];
                // 이미 보유한 용병이면 진화라 슬롯이 필요 없다 — 스쿼드가 가득 차 있어도 구매 가능.
                int ownedStar = RunState.GetMercenaryStar(def.Id);
                bool alreadyOwned = ownedStar > 0;
                bool maxedOut = ownedStar >= RosterMember.MaxStar;
                string label = bought ? "구매됨" : (maxedOut ? $"환전 ({cost}G→+{def.DeployCost * 2}G)" : (alreadyOwned ? $"진화! ({cost}G)" : $"구매 ({cost}G)"));
                bool interactable = !bought && RunState.Gold >= cost && (alreadyOwned || RunState.CanHireMercenary());
                cardViews[idx].Setup(def, label, interactable, () => HandleBuy(idx));
            }
        }

        private void HandleBuy(int idx)
        {
            MercenaryDefinition def = currentOffers[idx];
            int cost = MercenaryDatabase.GetTavernCost(def);
            if (!RunState.TryPurchaseMercenary(def, cost)) return;

            purchased[idx] = true;
            RefreshCards();
            RefreshGoldText();
        }

        private void HandleLeave()
        {
            gameObject.SetActive(false);
            onDismiss?.Invoke();
        }

        private void RefreshGoldText()
        {
            if (goldText != null) goldText.text = $"보유 골드: {RunState.Gold}G";
        }
    }
}
