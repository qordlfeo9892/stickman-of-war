using System;
using System.Collections.Generic;
using StickmanOfWar.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickmanOfWar.UI
{
    public class MercenaryCardView : MonoBehaviour
    {
        [SerializeField] private Image portraitFrame;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private RectTransform traitChipContainer;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private Button actionButton;
        [SerializeField] private TMP_Text actionButtonLabel;

        private const float ChipGap = 8f;
        private static readonly Color InfoTextColor = new Color(0.12f, 0.12f, 0.14f);
        private static readonly Color ChipColor = new Color(0.15f, 0.15f, 0.18f, 0.85f);

        private readonly List<GameObject> spawnedChips = new List<GameObject>();

        // 모든 카드가 공유하는 시너지 설명 툴팁 (전투 시너지 패널의 것과 동일 구현 재사용).
        // 루트 캔버스 하위에 붙으므로 카드가 재구성돼도 살아남는다. 씬 전환 시 캔버스와 함께 파괴됨.
        private static SynergyTooltip sharedSynergyTooltip;

        public void Setup(MercenaryDefinition def)
        {
            ApplyInfo(def);
            if (statsText != null) statsText.gameObject.SetActive(true);
            if (actionButton != null) actionButton.gameObject.SetActive(false);
        }

        public void Setup(MercenaryDefinition def, string actionLabel, bool actionInteractable, Action onAction)
        {
            ApplyInfo(def);
            if (statsText != null) statsText.gameObject.SetActive(true);

            if (actionButton != null)
            {
                actionButton.gameObject.SetActive(true);
                actionButton.interactable = actionInteractable;
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(() => onAction?.Invoke());
            }
            if (actionButtonLabel != null) actionButtonLabel.text = actionLabel;
        }

        public void SetupCompact(MercenaryDefinition def)
        {
            SetupCompact(def, 1);
        }

        // 로스터에 이미 들어간 용병 표시용 — 진화 별 배지를 이름 옆에 붙인다.
        public void SetupCompact(RosterMember member)
        {
            SetupCompact(member.Definition, member.Star);
        }

        private void SetupCompact(MercenaryDefinition def, int star)
        {
            ApplyInfo(def, star);
            if (statsText != null) statsText.gameObject.SetActive(false);

            // 액션 버튼을 "자세히 보기" 돋보기로 재사용 — 누르면 상세 스펙 팝업
            if (actionButton != null)
            {
                MercenaryDefinition captured = def;
                actionButton.gameObject.SetActive(true);
                actionButton.interactable = true;
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(() => MercenaryDetailPopup.ShowFor(this, captured));
            }
            if (actionButtonLabel != null) actionButtonLabel.text = "＋ 자세히 보기";
        }

        public void SetupCompactWithAction(MercenaryDefinition def, string actionLabel, bool actionInteractable, Action onAction)
        {
            ApplyInfo(def, 1);
            if (statsText != null) statsText.gameObject.SetActive(false);

            if (actionButton != null)
            {
                actionButton.gameObject.SetActive(true);
                actionButton.interactable = actionInteractable;
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(() => onAction?.Invoke());
            }
            if (actionButtonLabel != null) actionButtonLabel.text = actionLabel;
        }

        // 로스터 방출 패널 등 — RosterMember 기반, 별 배지 포함.
        public void SetupCompactWithAction(RosterMember member, string actionLabel, bool actionInteractable, Action onAction)
        {
            ApplyInfo(member.Definition, member.Star);
            if (statsText != null) statsText.gameObject.SetActive(false);

            if (actionButton != null)
            {
                actionButton.gameObject.SetActive(true);
                actionButton.interactable = actionInteractable;
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(() => onAction?.Invoke());
            }
            if (actionButtonLabel != null) actionButtonLabel.text = actionLabel;
        }

        private void ApplyInfo(MercenaryDefinition def, int star = 1)
        {
            if (nameText != null)
            {
                nameText.text = star >= RosterMember.MaxStar ? $"{def.DisplayName} <color=#FFD75A>★★</color>" : def.DisplayName;
                nameText.color = InfoTextColor;
            }
            if (statsText != null)
            {
                statsText.text = def.IsHealer
                    ? $"소환 비용 {def.DeployCost}\nHP {def.MaxHealth:0}\n초당 회복량 {def.AttackDamage:0}\n회복 주기 {def.AttackInterval:0.0}\n사거리 {def.AttackRange:0}\n이동속도 {def.MoveSpeed:0}"
                    : $"소환 비용 {def.DeployCost}\nHP {def.MaxHealth:0}\n공격력 {def.AttackDamage:0}\n공격주기 {def.AttackInterval:0.0}\n사거리 {def.AttackRange:0}\n이동속도 {def.MoveSpeed:0}";
                statsText.color = InfoTextColor;
            }
            if (portraitFrame != null) portraitFrame.color = MercenaryVisuals.GetTierColor(def.Tier);
            SetTraitChips(def.Traits);
        }

        private void SetTraitChips(List<string> traits)
        {
            foreach (GameObject chip in spawnedChips) Destroy(chip);
            spawnedChips.Clear();
            if (traitChipContainer == null || traits.Count == 0) return;

            if (sharedSynergyTooltip == null)
            {
                TMP_FontAsset font = nameText != null ? nameText.font
                    : (statsText != null ? statsText.font : null);
                sharedSynergyTooltip = SynergyTooltip.Create(this, font);
            }

            float totalWidth = traitChipContainer.rect.width;
            float chipHeight = traitChipContainer.rect.height;
            int count = traits.Count;
            float chipWidth = (totalWidth - ChipGap * (count - 1)) / count;
            float startX = -totalWidth / 2f + chipWidth / 2f;

            for (int i = 0; i < count; i++)
            {
                GameObject chip = new GameObject("TraitChip", typeof(RectTransform));
                var rt = (RectTransform)chip.transform;
                rt.SetParent(traitChipContainer, false);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(chipWidth, chipHeight);
                rt.anchoredPosition = new Vector2(startX + i * (chipWidth + ChipGap), 0f);

                Image bg = chip.AddComponent<Image>();
                bg.color = ChipColor;
                bg.raycastTarget = true; // 마우스 오버 감지 대상

                // 이 칩(특성)에 마우스를 올리면 시너지 단계별 효과 툴팁을 띄운다.
                SynergyRowHover hover = chip.AddComponent<SynergyRowHover>();
                hover.Init(sharedSynergyTooltip, traits[i], rt);

                var textGO = new GameObject("Text", typeof(RectTransform));
                var textRT = (RectTransform)textGO.transform;
                textRT.SetParent(rt, false);
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = Vector2.zero;
                textRT.offsetMax = Vector2.zero;

                TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
                tmp.text = traits[i];
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 10f;
                tmp.fontSizeMax = 22f;
                tmp.color = Color.white;
                tmp.raycastTarget = false; // 오버 감지는 칩 배경이 담당

                spawnedChips.Add(chip);
            }
        }
    }
}
