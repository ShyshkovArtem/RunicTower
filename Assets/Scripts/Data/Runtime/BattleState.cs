using System;
using RunicTower.Core;

namespace RunicTower.Data.Runtime
{
    [Serializable]
    public sealed class BattleState
    {
        public int RoundNumber;
        public BattlePhase Phase;
        public CombatantState Player = new();
        public CombatantState Enemy = new();
        public HandState PlayerHand = new();
        public HandState EnemyHand = new();
        public RitualBuild CurrentSelection = new();

        public bool HasPendingBattleEnd =>
            Phase == BattlePhase.Victory || Phase == BattlePhase.Defeat;
    }
}
