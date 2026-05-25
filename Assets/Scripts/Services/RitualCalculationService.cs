using System.Collections.Generic;
using System.Linq;
using RunicTower.Core;
using RunicTower.Data.Runtime;

namespace RunicTower.Services
{
    public sealed class RitualCalculationService
    {
        private RitualCombinationRuleTable _ruleTable;

        public RitualResult BuildResult(RitualBuild build)
        {
            RitualResult result = new();
            RitualCombinationRule tableRule = null;
            RitualCombinationRuleTable ruleTable = GetRuleTable();
            bool hasTableRule = ruleTable != null && ruleTable.TryFind(build, out tableRule);
            int manaCost = build.SelectedRunes.Sum(rune => rune.Definition.ManaCost);

            int overheatScore = 0;
            float successChance = hasTableRule ? 100f : 0f;
            List<CombatEffectData> effects = hasTableRule
                ? CloneEffects(tableRule.Effects)
                : new List<CombatEffectData>();

            result.ManaCost = manaCost;
            result.OverheatScore = overheatScore;
            result.SuccessChance = UnityEngine.Mathf.Clamp(successChance, 0f, 100f);
            result.Effects = effects;
            result.FailureSelfDamage = 0;
            if (hasTableRule)
            {
                result.DisplayName = tableRule.DisplayName;
                result.VfxKey = tableRule.VfxKey;
                result.VfxSpawnLocation = tableRule.VfxSpawnLocation;
                result.VfxTargetLocation = tableRule.VfxTargetLocation;
                result.VfxTravelTime = tableRule.VfxTravelTime;
                result.SfxKey = tableRule.SfxKey;
                AssignPersistentShieldVfx(effects, tableRule);
            }

            result.Summary = BuildSummary(build, manaCost, successChance, effects, hasTableRule ? tableRule.DisplayName : null);

            return result;
        }

        private RitualCombinationRuleTable GetRuleTable()
        {
            _ruleTable ??= RitualCombinationRuleTable.LoadDefault();
            return _ruleTable;
        }

        private static List<CombatEffectData> CloneEffects(IEnumerable<CombatEffectData> effects)
        {
            return effects
                .Where(effect => effect != null)
                .Select(effect => new CombatEffectData(
                    effect.EffectType,
                    effect.TargetSide,
                    effect.Magnitude,
                    effect.SourceLabel,
                    effect.PersistentVfxKey,
                    effect.HasShieldElement,
                    effect.ShieldElement))
                .ToList();
        }

        private static void AssignPersistentShieldVfx(List<CombatEffectData> effects, RitualCombinationRule tableRule)
        {
            if (effects == null || tableRule == null || string.IsNullOrWhiteSpace(tableRule.VfxKey))
            {
                return;
            }

            bool hasShieldElement = TryResolveShieldElement(tableRule, out ElementType shieldElement);
            foreach (CombatEffectData effect in effects)
            {
                if (effect != null && effect.EffectType == EffectType.Shield)
                {
                    effect.PersistentVfxKey = tableRule.VfxKey.Trim();
                    if (hasShieldElement)
                    {
                        effect.HasShieldElement = true;
                        effect.ShieldElement = shieldElement;
                    }
                }
            }
        }

        private static bool TryResolveShieldElement(RitualCombinationRule tableRule, out ElementType shieldElement)
        {
            shieldElement = default;
            if (tableRule?.RequiredRunes == null || tableRule.RequiredRunes.Count == 0)
            {
                return false;
            }

            RuneSignature waterRune = tableRule.RequiredRunes
                .FirstOrDefault(rune => rune.Element == ElementType.Water);
            shieldElement = waterRune.Element == ElementType.Water
                ? ElementType.Water
                : tableRule.RequiredRunes[0].Element;
            return true;
        }

        private static string BuildSummary(
            RitualBuild build,
            int manaCost,
            float successChance,
            List<CombatEffectData> effects,
            string ritualName)
        {
            string runes = string.Join(", ", build.SelectedRunes.Select(rune => rune.Definition.DisplayName));
            string effectSummary = string.Join(", ", effects.Select(effect => $"{effect.EffectType} {effect.Magnitude}"));
            string namePrefix = string.IsNullOrWhiteSpace(ritualName) ? string.Empty : $"{ritualName} | ";
            return $"{namePrefix}Runes: [{runes}] | Mana: {manaCost} | Success: {successChance:0}% | Effects: {effectSummary}";
        }
    }
}
