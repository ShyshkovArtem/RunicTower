using System.Linq;
using System.Text;
using UnityEngine;
using RunicTower.Combat;
using RunicTower.Core;
using RunicTower.Data.Runtime;

namespace RunicTower.Debugging
{
    public sealed class BattleConsoleLogger : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BattleFlowController battleController;

        [Header("Debug")]
        [SerializeField] private bool loggingEnabled = true;
        [SerializeField] private bool logControllerMessages = true;
        [SerializeField] private bool logRitualResults = true;
        [SerializeField] private bool logBattleStateSnapshots = true;
        [SerializeField] private bool logHandsInSnapshots = true;

        private void OnEnable()
        {
            if (battleController == null)
            {
                return;
            }

            battleController.LogEmitted += HandleControllerLog;
            battleController.RitualResolved += HandleRitualResolved;
            battleController.BattleStateChanged += HandleBattleStateChanged;
        }

        private void OnDisable()
        {
            if (battleController == null)
            {
                return;
            }

            battleController.LogEmitted -= HandleControllerLog;
            battleController.RitualResolved -= HandleRitualResolved;
            battleController.BattleStateChanged -= HandleBattleStateChanged;
        }

        private void HandleControllerLog(string message)
        {
            if (!loggingEnabled || !logControllerMessages || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            UnityEngine.Debug.Log($"[BattleLog] {message}", this);
        }

        private void HandleRitualResolved(RitualResult result)
        {
            if (!loggingEnabled || !logRitualResults || result == null)
            {
                return;
            }

            StringBuilder builder = new();
            builder.Append("[BattleRitual] ");
            builder.Append(result.WasSuccessful ? "Success" : "Fail");
            builder.Append(" | Mana ");
            builder.Append(result.ManaCost);
            builder.Append(" | Chance ");
            builder.Append(result.SuccessChance.ToString("0"));
            builder.Append("%");

            if (result.TriggeredFailureFallback)
            {
                builder.Append(" | Backlash ");
                builder.Append(result.FailureSelfDamage);
            }

            if (result.Effects != null && result.Effects.Count > 0)
            {
                builder.Append(" | Effects: ");
                builder.Append(string.Join(", ", result.Effects.Select(FormatEffect)));
            }
            else
            {
                builder.Append(" | Effects: none");
            }

            if (result.Notes != null && result.Notes.Count > 0)
            {
                builder.Append(" | Notes: ");
                builder.Append(string.Join(", ", result.Notes));
            }

            UnityEngine.Debug.Log(builder.ToString(), this);
        }

        private void HandleBattleStateChanged(BattleState state)
        {
            if (!loggingEnabled || !logBattleStateSnapshots || state == null)
            {
                return;
            }

            StringBuilder builder = new();
            builder.Append("[BattleState] Round ");
            builder.Append(state.RoundNumber);
            builder.Append(" | Phase ");
            builder.Append(state.Phase);
            builder.AppendLine();
            builder.Append("Player => ");
            builder.Append(FormatCombatant(state.Player));
            builder.AppendLine();
            builder.Append("Enemy => ");
            builder.Append(FormatCombatant(state.Enemy));

            if (logHandsInSnapshots)
            {
                builder.AppendLine();
                builder.Append("Player Hand => ");
                builder.Append(FormatHand(state.PlayerHand));
                builder.AppendLine();
                builder.Append("Enemy Hand => ");
                builder.Append(FormatHand(state.EnemyHand));
            }

            UnityEngine.Debug.Log(builder.ToString(), this);
        }

        private static string FormatCombatant(CombatantState combatant)
        {
            if (combatant == null)
            {
                return "null";
            }

            return string.Join(" | ", new[]
            {
                $"{combatant.DisplayName}",
                $"HP {combatant.CurrentHp}/{combatant.MaxHp}",
                $"Mana {combatant.CurrentMana}/{combatant.MaxMana}",
                $"Shield {combatant.Shield} ({combatant.ShieldRoundsRemaining}r)",
                $"HealStore {combatant.GetDisplayedRegeneration()}",
                $"Burn {combatant.GetDisplayedBurn()}",
                $"Break {combatant.DefenseBreak}",
                $"Dmg+ {combatant.DamageBoost} ({combatant.DamageBoostRoundsRemaining}r)",
                $"Dmg- {combatant.DamageWeakness} ({combatant.DamageWeaknessRoundsRemaining}r)"
            });
        }

        private static string FormatHand(HandState hand)
        {
            if (hand == null)
            {
                return "null";
            }

            string runes = hand.AvailableRunes == null || hand.AvailableRunes.Count == 0
                ? "none"
                : string.Join(", ", hand.AvailableRunes
                    .Where(rune => rune?.Definition != null)
                    .Select(rune => rune.Definition.DisplayName));

            string modifier = hand.ActiveModifier?.Definition == null
                ? "none"
                : hand.ActiveModifier.Definition.DisplayName;

            return $"Runes [{runes}] | Modifier [{modifier}]";
        }

        private static string FormatEffect(CombatEffectData effect)
        {
            if (effect == null)
            {
                return "null";
            }

            string target = effect.TargetSide switch
            {
                CombatTargetSide.Self => "self",
                CombatTargetSide.Opponent => "foe",
                _ => "?"
            };

            return $"{effect.SourceLabel}:{effect.EffectType} {effect.Magnitude} {target}";
        }
    }
}
