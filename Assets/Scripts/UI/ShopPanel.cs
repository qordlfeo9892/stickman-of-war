using System;
using System.Linq;
using StickmanOfWar.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickmanOfWar.UI
{
    public class ShopPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text[] nameTexts = new TMP_Text[3];
        [SerializeField] private TMP_Text[] effectTexts = new TMP_Text[3];
        [SerializeField] private Button[] buyButtons = new Button[3];
        [SerializeField] private TMP_Text[] buyLabels = new TMP_Text[3];
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private Button leaveButton;
        [SerializeField] private BagPanel bagPanel;
        [SerializeField] private ConfirmDialog confirmDialog;
        [SerializeField] private Button contractButton;
        [SerializeField] private TMP_Text contractLabel;
        [SerializeField] private ReleaseMercenaryPanel releaseMercenaryPanel;

        private const int ContractCost = 50;

        private readonly RelicDefinition[] currentOffers = new RelicDefinition[3];
        private readonly bool[] purchased = new bool[3];
        private Action onDismiss;

        public void Show(Action dismissCallback)
        {
            onDismiss = dismissCallback;
            gameObject.SetActive(true);

            RelicDefinition[] offers = RelicDatabase.GetAll().OrderBy(_ => UnityEngine.Random.value).Take(3).ToArray();
            for (int i = 0; i < 3; i++)
            {
                currentOffers[i] = offers[i];
                purchased[i] = false;
            }
            RefreshCards();
            RefreshGoldText();
            RefreshContractButton();

            leaveButton.onClick.RemoveAllListeners();
            leaveButton.onClick.AddListener(HandleLeave);

            contractButton.onClick.RemoveAllListeners();
            contractButton.onClick.AddListener(HandleBuyContract);
        }

        private void RefreshContractButton()
        {
            if (contractLabel != null) contractLabel.text = $"계약 해지서 ({ContractCost}G)";
            contractButton.interactable = RunState.Gold >= ContractCost && RunState.Roster.Count > 0;
        }

        private void HandleBuyContract()
        {
            confirmDialog.Show($"계약 해지서를 {ContractCost}G에 구매하시겠습니까?", () =>
            {
                if (!RunState.TrySpendGold(ContractCost)) return;
                RefreshGoldText();
                RefreshContractButton();

                releaseMercenaryPanel.Show(released =>
                {
                    if (!released)
                    {
                        RunState.Gold += ContractCost;
                    }
                    RefreshGoldText();
                    RefreshContractButton();
                });
            });
        }

        private void RefreshCards()
        {
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                RelicDefinition def = currentOffers[idx];
                int cost = RelicDatabase.GetCost(def);
                bool bought = purchased[idx];

                if (nameTexts[idx] != null) nameTexts[idx].text = def.DisplayName;
                if (effectTexts[idx] != null) effectTexts[idx].text = def.EffectDescription;
                if (buyLabels[idx] != null) buyLabels[idx].text = bought ? "구매됨" : $"구매 ({cost}G)";

                buyButtons[idx].interactable = !bought && RunState.Gold >= cost;
                buyButtons[idx].onClick.RemoveAllListeners();
                buyButtons[idx].onClick.AddListener(() => HandleBuy(idx));
            }
        }

        private void HandleBuy(int idx)
        {
            RelicDefinition def = currentOffers[idx];
            int cost = RelicDatabase.GetCost(def);

            confirmDialog.Show($"{def.DisplayName}을(를) {cost}G에 정말 구매하시겠습니까?", () =>
            {
                if (!RunState.TrySpendGold(cost)) return;

                purchased[idx] = true;
                RefreshCards();
                RefreshGoldText();

                bagPanel.ShowForNewRelic(def, placed =>
                {
                    if (!placed)
                    {
                        RunState.Gold += cost;
                        purchased[idx] = false;
                        RefreshCards();
                        RefreshGoldText();
                    }
                });
            });
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
