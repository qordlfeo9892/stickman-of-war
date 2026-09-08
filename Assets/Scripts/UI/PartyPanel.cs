using System.Collections.Generic;
using System.Linq;
using StickmanOfWar.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickmanOfWar.UI
{
    public class PartyPanel : MonoBehaviour
    {
        [SerializeField] private MercenaryCardView[] cardViews = new MercenaryCardView[10];
        [SerializeField] private TMP_Text synergyText;
        [SerializeField] private Button closeButton;

        private void Awake()
        {
            closeButton.onClick.AddListener(Hide);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            RefreshCards();
            RefreshSynergies();
        }

        private void RefreshCards()
        {
            for (int i = 0; i < cardViews.Length; i++)
            {
                bool hasMerc = i < RunState.Roster.Count;
                cardViews[i].gameObject.SetActive(hasMerc);
                if (hasMerc) cardViews[i].SetupCompact(RunState.Roster[i]);
            }
        }

        private static readonly Dictionary<SynergyRank, string> RankColors = new Dictionary<SynergyRank, string>
        {
            { SynergyRank.None, "#888888" },
            { SynergyRank.Silver, "#C6C9CC" },
            { SynergyRank.Gold, "#FFD75A" },
            { SynergyRank.Prismatic, "#FF8FE0" },
        };

        private void RefreshSynergies()
        {
            List<(string trait, int count, int nextThreshold, string desc, SynergyRank rank)> progress =
                TraitSynergyDatabase.GetAllTraitProgress(RunState.Roster.Select(m => m.Definition).ToList());

            synergyText.text = progress.Count == 0
                ? "시너지 없음"
                : string.Join("\n", progress.Select(p => $"<color={RankColors[p.rank]}>◆ {p.trait} {p.count}/{p.nextThreshold} - {p.desc}</color>"));
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
