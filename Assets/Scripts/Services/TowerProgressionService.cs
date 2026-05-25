using System.Collections.Generic;
using System.Linq;
using RunicTower.Core;
using RunicTower.Data.Definitions;
using RunicTower.Data.Runtime;

namespace RunicTower.Services
{
    public sealed class TowerProgressionService
    {
        public EncounterDefinition GetEncounterForFloor(int floor, IReadOnlyList<EncounterDefinition> encounters)
        {
            EncounterDefinition exactMatch = encounters.FirstOrDefault(encounter => encounter.FloorNumber == floor);
            if (exactMatch != null)
            {
                return exactMatch;
            }

            bool isBossFloor = floor % 5 == 0;
            return encounters.FirstOrDefault(encounter => encounter.IsBossEncounter == isBossFloor);
        }

        public void AdvanceFloor(PlayerRunState runState)
        {
            runState.CurrentFloor++;
        }

        public List<FloorRewardChoice> CreateBasicRewards(IEnumerable<ElementalRuneDefinition> availableRunes)
        {
            ElementalRuneDefinition firstRune = availableRunes.FirstOrDefault();

            return new List<FloorRewardChoice>
            {
                new()
                {
                    RewardType = RewardType.Heal,
                    Description = "Recover 5 HP.",
                    HealAmount = 5
                },
                new()
                {
                    RewardType = RewardType.ManaBlessing,
                    Description = "Start the next battle with +1 mana.",
                    ManaBlessingAmount = 1
                },
                new()
                {
                    RewardType = RewardType.AddRune,
                    Description = firstRune == null ? "No rune available." : $"Add {firstRune.DisplayName} to the deck.",
                    RuneReward = firstRune
                }
            };
        }
    }
}
