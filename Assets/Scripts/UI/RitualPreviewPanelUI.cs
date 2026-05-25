using System.Linq;
using TMPro;
using UnityEngine;
using RunicTower.Data.Runtime;

namespace RunicTower.UI
{
    public sealed class RitualPreviewPanelUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text manaCostLabel;
        [SerializeField] private TMP_Text successChanceLabel;
        [SerializeField] private TMP_Text finalEffectLabel;

        public void RenderValidationErrors(RitualValidationResult validation)
        {
            SetText(manaCostLabel, "Mana: -");
            SetText(successChanceLabel, "Succ: 0%");
            SetText(finalEffectLabel, validation == null || validation.IsValid
                ? "Effect: -"
                : $"Effect: {string.Join(" | ", validation.Errors)}");
        }

        public void RenderPreview(RitualResult result)
        {
            if (result == null)
            {
                Clear();
                return;
            }

            SetText(manaCostLabel, $"Mana: {result.ManaCost}");
            SetText(successChanceLabel, $"Succ: {result.SuccessChance:0}%");
            SetText(finalEffectLabel, $"Effect: {BuildFinalEffectSummary(result)}");
        }

        public void Clear()
        {
            SetText(manaCostLabel, "Mana: -");
            SetText(successChanceLabel, "Succ % -");
            SetText(finalEffectLabel, "Effect: -");
        }

        public void RenderSkipPreview()
        {
            SetText(manaCostLabel, "Mana: 0");
            SetText(successChanceLabel, "Succ: 100%");
            SetText(finalEffectLabel, "Effect: PASS");
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label != null)
            {
                label.text = value;
            }
        }

        public static string BuildFinalEffectSummary(RitualResult result)
        {
            if (result.Effects == null || result.Effects.Count == 0)
            {
                return "-";
            }

            string summary = string.Join(", ",
                result.Effects
                    .GroupBy(effect => new { effect.EffectType, effect.TargetSide })
                    .Select(group =>
                        FormatEffectChunk(
                            group.Key.EffectType,
                            group.Key.TargetSide,
                            group.Sum(effect => effect.Magnitude))));

            return string.IsNullOrWhiteSpace(summary) ? "-" : summary;
        }

        private static string FormatEffectChunk(
            RunicTower.Core.EffectType effectType,
            RunicTower.Core.CombatTargetSide targetSide,
            int magnitude)
        {
            string effectName = effectType switch
            {
                RunicTower.Core.EffectType.Damage => "DMG",
                RunicTower.Core.EffectType.Shield => "SHD",
                RunicTower.Core.EffectType.Heal => "HEAL",
                RunicTower.Core.EffectType.Regeneration => "REG",
                RunicTower.Core.EffectType.DefenseBreak => "BRK",
                RunicTower.Core.EffectType.Burn => "BRN",
                RunicTower.Core.EffectType.DamageBoost => "DMG+",
                RunicTower.Core.EffectType.DamageWeakness => "DMG-",
                _ => effectType.ToString().ToUpperInvariant()
            };

            string target = targetSide switch
            {
                RunicTower.Core.CombatTargetSide.Self => "SELF",
                RunicTower.Core.CombatTargetSide.Opponent => "FOE",
                _ => string.Empty
            };

            string formattedMagnitude = effectType is RunicTower.Core.EffectType.DefenseBreak
                or RunicTower.Core.EffectType.DamageBoost
                or RunicTower.Core.EffectType.DamageWeakness
                ? $"+{magnitude}%"
                : $"+{magnitude}";

            return string.IsNullOrEmpty(target)
                ? $"{formattedMagnitude} {effectName}"
                : $"{formattedMagnitude} {effectName} {target}";
        }
    }
}
