using System;
using System.Collections.Generic;
using System.Linq;
using RunicTower.Core;
using RunicTower.Data.Runtime;

namespace RunicTower.Services
{
    public sealed class HandDrawService
    {
        private const int PlayerDeckSize = 8;
        private const int TargetHandSize = 5;
        private const int PreviewQueueSize = 2;

        public int UpcomingPreviewSize => PreviewQueueSize;

        public void InitializeCyclingHand(
            HandState hand,
            IReadOnlyList<RuneInstance> deck,
            IReadOnlyList<ModifierInstance> modifiers,
            Random random)
        {
            if (hand == null)
            {
                return;
            }

            List<RuneInstance> sourceRunes = BuildPlayerDrawSource(deck);

            hand.AvailableRunes = new List<RuneInstance>();
            hand.DrawQueue = new List<RuneInstance>();

            for (int index = 0; index < TargetHandSize && sourceRunes.Count > 0; index++)
            {
                hand.AvailableRunes.Add(CreateRandomRuneFromDeck(sourceRunes, random));
            }

            RefillUpcomingQueue(hand, sourceRunes, random);
            hand.ActiveModifier = PickModifier(modifiers, random);
        }

        public void RefreshCyclingHandModifier(
            HandState hand,
            IReadOnlyList<ModifierInstance> modifiers,
            Random random)
        {
            if (hand == null)
            {
                return;
            }

            hand.ActiveModifier = PickModifier(modifiers, random);
        }

        public void ConsumeAndRefillCyclingHand(
            HandState hand,
            IReadOnlyList<RuneInstance> consumedRunes,
            IReadOnlyList<RuneInstance> deck,
            Random random)
        {
            if (hand == null || consumedRunes == null)
            {
                return;
            }

            List<RuneInstance> sourceRunes = BuildPlayerDrawSource(deck);
            int removedRunes = 0;
            foreach (RuneInstance consumedRune in consumedRunes)
            {
                if (consumedRune?.Definition == null)
                {
                    continue;
                }

                int removedCount = hand.AvailableRunes.RemoveAll(candidate => candidate.InstanceId == consumedRune.InstanceId);
                if (removedCount <= 0)
                {
                    continue;
                }

                removedRunes += removedCount;
            }

            int drawCount = Math.Min(removedRunes, TargetHandSize - hand.AvailableRunes.Count);
            DrawFromUpcomingQueue(hand, sourceRunes, drawCount, random);
            RefillUpcomingQueue(hand, sourceRunes, random);
        }

        private static void DrawFromUpcomingQueue(
            HandState hand,
            IReadOnlyList<RuneInstance> sourceRunes,
            int requestedCount,
            Random random)
        {
            if (hand == null || requestedCount <= 0)
            {
                return;
            }

            while (requestedCount > 0 && hand.AvailableRunes.Count < TargetHandSize)
            {
                if (hand.DrawQueue == null)
                {
                    hand.DrawQueue = new List<RuneInstance>();
                }

                if (hand.DrawQueue.Count == 0)
                {
                    RuneInstance randomRune = CreateRandomRuneFromDeck(sourceRunes, random);
                    if (randomRune == null)
                    {
                        return;
                    }

                    hand.DrawQueue.Add(randomRune);
                }

                RuneInstance nextRune = hand.DrawQueue[0];
                hand.DrawQueue.RemoveAt(0);
                if (nextRune?.Definition != null)
                {
                    hand.AvailableRunes.Add(nextRune);
                    requestedCount--;
                }
            }
        }

        private static void RefillUpcomingQueue(
            HandState hand,
            IReadOnlyList<RuneInstance> sourceRunes,
            Random random)
        {
            if (hand == null)
            {
                return;
            }

            hand.DrawQueue ??= new List<RuneInstance>();
            while (hand.DrawQueue.Count < PreviewQueueSize)
            {
                RuneInstance randomRune = CreateRandomRuneFromDeck(sourceRunes, random);
                if (randomRune == null)
                {
                    return;
                }

                hand.DrawQueue.Add(randomRune);
            }
        }

        private static List<RuneInstance> BuildPlayerDrawSource(IReadOnlyList<RuneInstance> deck)
        {
            return (deck ?? Array.Empty<RuneInstance>())
                .Where(rune => rune?.Definition != null)
                .Take(PlayerDeckSize)
                .Select(rune => new RuneInstance(rune.Definition))
                .ToList();
        }

        private static RuneInstance CreateRandomRuneFromDeck(IReadOnlyList<RuneInstance> sourceRunes, Random random)
        {
            if (sourceRunes == null || sourceRunes.Count == 0)
            {
                return null;
            }

            RuneInstance template = sourceRunes[random.Next(sourceRunes.Count)];
            return template?.Definition == null ? null : new RuneInstance(template.Definition);
        }

        public void DrawNewTurnHand(
            HandState hand,
            IReadOnlyList<RuneInstance> deck,
            IReadOnlyList<ModifierInstance> modifiers,
            Random random)
        {
            List<RuneInstance> sourceRunes = (deck ?? Array.Empty<RuneInstance>())
                .Where(rune => rune?.Definition != null)
                .ToList();

            List<RuneInstance> draw = new();

            if (sourceRunes.Count == 0)
            {
                hand.AvailableRunes = draw;
                hand.ActiveModifier = PickModifier(modifiers, random);
                return;
            }

            Dictionary<(RuneSize Size, ElementType Element), List<RuneInstance>> runeBuckets = sourceRunes
                .GroupBy(rune => (rune.Definition.Size, rune.Definition.Element))
                .ToDictionary(group => group.Key, group => group.ToList());

            for (int index = 0; index < TargetHandSize; index++)
            {
                RuneSize rolledSize = RollRuneSize(random);
                ElementType rolledElement = RollElement(random);
                RuneInstance selectedRune = SelectRuneForRoll(sourceRunes, runeBuckets, rolledSize, rolledElement, random);
                if (selectedRune == null)
                {
                    continue;
                }

                draw.Add(new RuneInstance(selectedRune.Definition));
            }

            hand.AvailableRunes = draw;
            hand.ActiveModifier = PickModifier(modifiers, random);
        }

        private static ModifierInstance PickModifier(IReadOnlyList<ModifierInstance> modifiers, Random random)
        {
            return modifiers == null || modifiers.Count == 0
                ? null
                : modifiers[random.Next(modifiers.Count)];
        }

        private static RuneInstance SelectRuneForRoll(
            List<RuneInstance> sourceRunes,
            IReadOnlyDictionary<(RuneSize Size, ElementType Element), List<RuneInstance>> runeBuckets,
            RuneSize rolledSize,
            ElementType rolledElement,
            Random random)
        {
            if (TryPickFromBucket(runeBuckets, rolledSize, rolledElement, random, out RuneInstance exactMatch))
            {
                return exactMatch;
            }

            ElementType[] allElements =
            {
                ElementType.Fire,
                ElementType.Air,
                ElementType.Earth,
                ElementType.Water
            };

            foreach (ElementType element in allElements.OrderBy(_ => random.Next()))
            {
                if (TryPickFromBucket(runeBuckets, rolledSize, element, random, out RuneInstance sizeMatch))
                {
                    return sizeMatch;
                }
            }

            return sourceRunes[random.Next(sourceRunes.Count)];
        }

        private static bool TryPickFromBucket(
            IReadOnlyDictionary<(RuneSize Size, ElementType Element), List<RuneInstance>> runeBuckets,
            RuneSize size,
            ElementType element,
            Random random,
            out RuneInstance rune)
        {
            rune = null;

            if (!runeBuckets.TryGetValue((size, element), out List<RuneInstance> bucket) || bucket == null || bucket.Count == 0)
            {
                return false;
            }

            rune = bucket[random.Next(bucket.Count)];
            return true;
        }

        private static RuneSize RollRuneSize(Random random)
        {
            int roll = random.Next(100);
            if (roll < 50)
            {
                return RuneSize.Small;
            }

            if (roll < 80)
            {
                return RuneSize.Medium;
            }

            return RuneSize.Large;
        }

        private static ElementType RollElement(Random random)
        {
            return random.Next(4) switch
            {
                0 => ElementType.Fire,
                1 => ElementType.Air,
                2 => ElementType.Earth,
                _ => ElementType.Water
            };
        }
    }
}
