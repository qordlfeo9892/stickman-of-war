using System;
using StickmanOfWar.Map;
using UnityEngine;

namespace StickmanOfWar.UI
{
    public class EventPanel : MonoBehaviour
    {
        [SerializeField] private RelicAcquirePanel relicAcquirePanel;
        [SerializeField] private EventChoicePanel eventChoicePanel;
        [SerializeField] private StubResultPanel stubResultPanel;
        [SerializeField] private BagPanel bagPanel;

        public void Show(Action dismissCallback)
        {
            MapEventDefinition ev = MapEventDatabase.GetRandom();

            if (ev.IsBinaryChoice)
            {
                eventChoicePanel.Show(ev.Title, ev.Body, ev.Choice1Label, ev.Choice2Label,
                    () => ApplyEffect(ev, ev.Choice1Effect, ev.Choice1Amount, ev.Choice1ResultBody, dismissCallback),
                    () => ApplyEffect(ev, ev.Choice2Effect, ev.Choice2Amount, ev.Choice2ResultBody, dismissCallback));
            }
            else
            {
                ApplyEffect(ev, ev.Choice1Effect, ev.EffectAmount, ev.ResultBody, dismissCallback);
            }
        }

        private void ApplyEffect(MapEventDefinition ev, EventEffectKind kind, float amount, string resultBody, Action dismissCallback)
        {
            Action done = () => dismissCallback?.Invoke();
            int amt = Mathf.RoundToInt(amount);

            switch (kind)
            {
                case EventEffectKind.GrantRandomRelic:
                    relicAcquirePanel.Show(RelicDatabase.GetRandom(), done);
                    break;

                case EventEffectKind.SpendAllGoldThenGrantRelic:
                    RunState.SpendAllGold();
                    bagPanel.ShowForNewRelic(RelicDatabase.GetRandom(), _ => done());
                    break;

                case EventEffectKind.LoseHalfGoldThenGrantRelic:
                    RunState.HalveGold();
                    bagPanel.ShowForNewRelic(RelicDatabase.GetRandom(), _ => done());
                    break;

                case EventEffectKind.SwapRandomMercenary:
                {
                    RunState.SwapRandomMercenary(out RosterMember removed, out MercenaryDefinition added);
                    string removedName = removed != null ? removed.DisplayName : "아무도";
                    string addedName = added != null ? added.DisplayName : "아무도";
                    ShowResult(ev, $"{removedName}이(가) 떠나고, {addedName}이(가) 합류했다.", done);
                    break;
                }

                case EventEffectKind.DamageNextBattleBase:
                    RunState.NextBattleBaseDamage += amount;
                    ShowResult(ev, resultBody ?? ev.Body, done);
                    break;

                case EventEffectKind.FortifyNextBattleBase:
                    RunState.FortifyNextBattleBase(amount);
                    ShowResult(ev, resultBody ?? $"다음 전투를 강화된 기지로 시작한다. (+{amt})", done);
                    break;

                case EventEffectKind.SpendAllGoldThenFortify:
                {
                    bool paid = RunState.Gold > 0;
                    RunState.SpendAllGold();
                    RunState.FortifyNextBattleBase(amount);
                    ShowResult(ev, paid
                        ? $"가진 금화를 모두 넘겼다. 다음 전투를 튼튼한 방벽과 함께 시작한다. (+{amt})"
                        : "가진 금화가 없었지만, 공병은 그래도 방벽을 손봐주었다.", done);
                    break;
                }

                case EventEffectKind.GainGold:
                    RunState.AddGold(amt);
                    ShowResult(ev, resultBody ?? $"금화 {amt}을(를) 얻었다.", done);
                    break;

                case EventEffectKind.GainGoldPerMerc:
                {
                    int total = amt * RunState.PartySizeCurrent;
                    RunState.AddGold(total);
                    ShowResult(ev, total > 0
                        ? $"병력 수에 비례해 금화 {total}을(를) 사례받았다."
                        : "호위할 병력이 없어 사례는 없었다.", done);
                    break;
                }

                case EventEffectKind.GainDice:
                    RunState.AddDice(amt);
                    ShowResult(ev, resultBody ?? $"주사위 {amt}개를 얻었다.", done);
                    break;

                case EventEffectKind.GainPartySlot:
                {
                    bool room = RunState.PartySizeCap < RunState.MaxPartySizeCap;
                    RunState.IncreasePartySizeCap();
                    ShowResult(ev, room
                        ? (resultBody ?? "스쿼드 슬롯이 하나 늘었다.")
                        : "하지만 스쿼드 슬롯은 이미 최대치다.", done);
                    break;
                }

                case EventEffectKind.TradeDiceForGold:
                {
                    if (RunState.DiceCount <= 0)
                    {
                        ShowResult(ev, "넘길 주사위가 없어 거래는 무산됐다.", done);
                        break;
                    }
                    RunState.AddDice(-1);
                    RunState.AddGold(amt);
                    ShowResult(ev, $"주사위 하나를 넘기고 금화 {amt}을(를) 받았다.", done);
                    break;
                }

                case EventEffectKind.TradeGoldForDice:
                {
                    if (!RunState.TrySpendGold(amt))
                    {
                        ShowResult(ev, $"금화 {amt}이(가) 없어 거래는 무산됐다.", done);
                        break;
                    }
                    RunState.AddDice(2);
                    ShowResult(ev, $"금화 {amt}을(를) 치르고 주사위 2개를 받았다.", done);
                    break;
                }

                case EventEffectKind.ReleaseRandomMercForGold:
                {
                    RosterMember m = RunState.RemoveRandomMercenary();
                    if (m == null)
                    {
                        ShowResult(ev, "보내줄 병사가 없었다.", done);
                        break;
                    }
                    int gold = Mathf.Max(1, m.Tier) * amt;
                    RunState.AddGold(gold);
                    ShowResult(ev, $"{m.DisplayName}이(가) 몸값 {gold}을(를) 남기고 대열을 떠났다.", done);
                    break;
                }

                case EventEffectKind.SacrificeRandomMercForRelic:
                {
                    RosterMember m = RunState.RemoveRandomMercenary();
                    if (m == null)
                    {
                        ShowResult(ev, "제물로 바칠 동료가 없다. 정령은 실망한 듯 사그라들었다.", done);
                        break;
                    }
                    ShowResult(ev, $"{m.DisplayName}이(가) 불길 속으로 사라졌다. 재 위에 유물이 남았다.",
                        () => relicAcquirePanel.Show(RelicDatabase.GetRandom(), done));
                    break;
                }

                case EventEffectKind.GambleGold:
                {
                    int before = RunState.Gold;
                    if (before <= 0)
                    {
                        ShowResult(ev, "걸 금화가 없어 수레바퀴는 헛돌았다.", done);
                        break;
                    }
                    if (UnityEngine.Random.value < 0.5f)
                    {
                        RunState.DoubleGold();
                        ShowResult(ev, $"운이 따랐다! 금화가 {before}에서 {RunState.Gold}(으)로 불어났다.", done);
                    }
                    else
                    {
                        RunState.SpendAllGold();
                        ShowResult(ev, $"수레바퀴가 멈췄다. 금화 {before}을(를) 모두 잃었다.", done);
                    }
                    break;
                }

                case EventEffectKind.GambleRelicOrCurse:
                    if (UnityEngine.Random.value < 0.6f)
                    {
                        relicAcquirePanel.Show(RelicDatabase.GetRandom(), done);
                    }
                    else
                    {
                        RunState.NextBattleBaseDamage += amount;
                        ShowResult(ev, $"함정이었다! 다음 전투를 손상된 기지로 시작한다. (-{amt})", done);
                    }
                    break;

                case EventEffectKind.GambleFortifyOrDamage:
                    if (UnityEngine.Random.value < 0.5f)
                    {
                        RunState.FortifyNextBattleBase(amount);
                        ShowResult(ev, $"몸이 뜨거워지며 사기가 치솟는다. 다음 전투를 강화된 기지로 시작한다. (+{amt})", done);
                    }
                    else
                    {
                        RunState.NextBattleBaseDamage += amount;
                        ShowResult(ev, $"속이 뒤틀린다. 다음 전투를 약해진 기지로 시작한다. (-{amt})", done);
                    }
                    break;

                case EventEffectKind.None:
                default:
                    if (!string.IsNullOrEmpty(resultBody)) ShowResult(ev, resultBody, done);
                    else done();
                    break;
            }
        }

        private void ShowResult(MapEventDefinition ev, string body, Action onDismiss)
        {
            stubResultPanel.Show(ev.Title, body, onDismiss);
        }
    }
}
