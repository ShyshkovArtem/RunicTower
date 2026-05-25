using System;
using RunicTower.Core;
using RunicTower.Data.Definitions;

namespace RunicTower.Data.Runtime
{
    [Serializable]
    public sealed class FloorRewardChoice
    {
        public RewardType RewardType;
        public string Description = string.Empty;
        public ElementalRuneDefinition RuneReward;
        public int HealAmount;
        public int ManaBlessingAmount;
    }
}
