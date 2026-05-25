using System;
using System.Collections.Generic;
using RunicTower.Core;
using RunicTower.Data.Runtime;

namespace RunicTower.Services
{
    public sealed class BattleResolutionService
    {
        public RitualResult FinalizeRitualResult(
            RitualResult calculatedResult,
            Random random)
        {
            if (calculatedResult == null)
            {
                return null;
            }

            calculatedResult.WasSuccessful = random.NextDouble() * 100f <= calculatedResult.SuccessChance;

            if (!calculatedResult.WasSuccessful)
            {
                calculatedResult.TriggeredFailureFallback = true;
                calculatedResult.Notes.Add("Ritual failed.");
                calculatedResult.Effects.Clear();

                if (calculatedResult.FailureSelfDamage > 0)
                {
                    calculatedResult.Effects.Add(new CombatEffectData(
                        EffectType.Damage,
                        CombatTargetSide.Self,
                        calculatedResult.FailureSelfDamage,
                        "Backlash"));
                }
            }

            return calculatedResult;
        }

        public void SpendMana(CombatantState caster, RitualResult result)
        {
            if (caster == null || result == null)
            {
                return;
            }

            caster.CurrentMana -= result.ManaCost;
            caster.ClampResources();
        }

        public void ApplyRitualEffects(BattleState perspectiveState, RitualResult result)
        {
            if (perspectiveState == null || result == null)
            {
                return;
            }

            ApplyEffectsOfTypes(perspectiveState, result, EffectType.Heal, EffectType.Regeneration);
            ApplyEffectsOfTypes(perspectiveState, result, EffectType.DamageBoost, EffectType.DamageWeakness);
            ApplyEffectsOfTypes(perspectiveState, result, EffectType.Shield);
            ApplyEffectsOfTypes(perspectiveState, result, EffectType.Damage);
            ApplyEffectsOfTypes(perspectiveState, result, EffectType.DefenseBreak, EffectType.Burn);
        }

        private void ApplyEffectsOfTypes(BattleState state, RitualResult result, params EffectType[] effectTypes)
        {
            if (state == null || result?.Effects == null || effectTypes == null || effectTypes.Length == 0)
            {
                return;
            }

            HashSet<EffectType> allowedTypes = new(effectTypes);
            foreach (CombatEffectData effect in result.Effects)
            {
                if (effect != null && allowedTypes.Contains(effect.EffectType))
                {
                    ApplyEffect(state, effect);
                }
            }
        }

        private static void ApplyEffect(BattleState state, CombatEffectData effect)
        {
            CombatantState target = effect.TargetSide == CombatTargetSide.Self
                ? state.Player
                : state.Enemy;
            CombatantState source = state.Player;

            switch (effect.EffectType)
            {
                case EffectType.Damage:
                    ApplyDamage(source, target, effect.Magnitude);
                    break;
                case EffectType.Shield:
                    target.AddShield(
                        effect.Magnitude,
                        3,
                        effect.PersistentVfxKey,
                        effect.HasShieldElement,
                        effect.ShieldElement);
                    if (IsWindBarrier(effect))
                    {
                        target.ExtendBurn(1);
                    }
                    if (ClearsBreak(effect))
                    {
                        target.ClearDefenseBreak();
                    }
                    break;
                case EffectType.Heal:
                    target.ApplyImmediateHealing(effect.Magnitude);
                    break;
                case EffectType.Regeneration:
                    target.ApplyImmediateHealing(effect.Magnitude);
                    break;
                case EffectType.DefenseBreak:
                    target.AddDefenseBreak(effect.Magnitude, 2);
                    break;
                case EffectType.Burn:
                    target.AddPendingBurn(effect.Magnitude, 2);
                    break;
                case EffectType.DamageBoost:
                    target.AddDamageBoost(effect.Magnitude, 2);
                    break;
                case EffectType.DamageWeakness:
                    target.AddDamageWeakness(effect.Magnitude, 2);
                    break;
            }
        }

        private static void ApplyDamage(CombatantState source, CombatantState target, int amount)
        {
            int sourceBoost = source != null ? source.DamageBoost : 0;
            int sourceWeakness = source != null ? source.DamageWeakness : 0;
            int damagePercent = Math.Max(0, 100 + sourceBoost - sourceWeakness + target.DefenseBreak);
            int adjustedAmount = Math.Max(0, (int)Math.Round(amount * damagePercent / 100f, MidpointRounding.AwayFromZero));
            target.ClearDefenseBreak();

            if (target.Shield > 0)
            {
                int absorbed = Math.Min(target.Shield, adjustedAmount);
                target.Shield -= absorbed;
                adjustedAmount -= absorbed;
                target.ClearShieldVfxIfEmpty();
            }

            target.CurrentHp -= adjustedAmount;
            target.ClampResources();
        }

        private static bool IsWindBarrier(CombatEffectData effect)
        {
            if (effect == null ||
                effect.EffectType != EffectType.Shield ||
                string.IsNullOrWhiteSpace(effect.SourceLabel))
            {
                return false;
            }

            string sourceLabel = effect.SourceLabel.Trim();
            return effect.HasShieldElement && effect.ShieldElement == ElementType.Air ||
                   sourceLabel.IndexOf("Air Barrier", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   sourceLabel.IndexOf("Wind Barrier", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ClearsBreak(CombatEffectData effect)
        {
            if (effect == null ||
                effect.EffectType != EffectType.Shield ||
                string.IsNullOrWhiteSpace(effect.SourceLabel))
            {
                return false;
            }

            string sourceLabel = effect.SourceLabel.Trim();
            return sourceLabel.IndexOf("Flame Barrier", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   sourceLabel.IndexOf("Magma Barrier", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public void RefreshBattleOutcome(BattleState state)
        {
            if (!state.Enemy.IsAlive)
            {
                state.Phase = BattlePhase.Victory;
                return;
            }

            if (!state.Player.IsAlive)
            {
                state.Phase = BattlePhase.Defeat;
            }
        }
    }
}
