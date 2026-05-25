using System;
using System.Collections.Generic;
using System.Linq;
using RunicTower.Core;
using RunicTower.Data.Runtime;

namespace RunicTower.Services
{
    public sealed class EnemyDecisionService
    {
        public RitualBuild BuildEnemyRitual(CombatantState enemy, HandState enemyHand, Random random)
        {
            List<RuneInstance> orderedRunes = enemyHand.AvailableRunes
                .Where(rune => rune?.Definition != null)
                .OrderByDescending(rune => GetPriority(enemy, rune))
                .ThenBy(_ => random.Next())
                .ToList();

            RitualCombinationRuleTable ruleTable = RitualCombinationRuleTable.LoadDefault();
            RitualBuild bestBuild = new();
            int bestScore = int.MinValue;

            for (int count = 1; count <= Math.Min(3, orderedRunes.Count); count++)
            {
                EvaluateCombinations(
                    enemy,
                    orderedRunes,
                    ruleTable,
                    random,
                    count,
                    0,
                    new RitualBuild(),
                    ref bestBuild,
                    ref bestScore);
            }

            return bestBuild;
        }

        private static void EvaluateCombinations(
            CombatantState enemy,
            IReadOnlyList<RuneInstance> orderedRunes,
            RitualCombinationRuleTable ruleTable,
            Random random,
            int targetCount,
            int startIndex,
            RitualBuild currentBuild,
            ref RitualBuild bestBuild,
            ref int bestScore)
        {
            if (currentBuild.SelectedRunes.Count == targetCount)
            {
                int manaCost = currentBuild.SelectedRunes.Sum(rune => rune.Definition.ManaCost);
                if (ruleTable == null ||
                    !ruleTable.TryFind(currentBuild, out _) ||
                    manaCost > enemy.CurrentMana)
                {
                    return;
                }

                int score = currentBuild.SelectedRunes.Sum(rune => GetPriority(enemy, rune)) +
                            currentBuild.SelectedRunes.Count +
                            random.Next(0, 3);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestBuild = new RitualBuild();
                    bestBuild.SelectedRunes.AddRange(currentBuild.SelectedRunes);
                }

                return;
            }

            for (int index = startIndex; index < orderedRunes.Count; index++)
            {
                currentBuild.SelectedRunes.Add(orderedRunes[index]);
                EvaluateCombinations(
                    enemy,
                    orderedRunes,
                    ruleTable,
                    random,
                    targetCount,
                    index + 1,
                    currentBuild,
                    ref bestBuild,
                    ref bestScore);
                currentBuild.SelectedRunes.RemoveAt(currentBuild.SelectedRunes.Count - 1);
            }
        }

        private static int GetPriority(CombatantState enemy, RuneInstance rune)
        {
            if (enemy.EnemyDefinition == null)
            {
                return 0;
            }

            bool preferred = enemy.EnemyDefinition.PreferredRunes.Contains(rune.Definition);
            int preferenceBonus = preferred ? 10 : 0;
            int archetypeBonus = enemy.EnemyDefinition.Archetype switch
            {
                EnemyArchetype.Aggressive when rune.Definition.Element is ElementType.Fire or ElementType.Air => 5,
                EnemyArchetype.Defensive when rune.Definition.Element == ElementType.Earth => 5,
                EnemyArchetype.Adaptive when rune.Definition.Element == ElementType.Water => 4,
                EnemyArchetype.Boss when rune.Definition.Size == RuneSize.Large => 6,
                _ => 0
            };

            return preferenceBonus + archetypeBonus + (int)rune.Definition.Size;
        }
    }
}
