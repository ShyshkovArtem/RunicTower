using System;
using System.Collections.Generic;

namespace RunicTower.Data.Runtime
{
    [Serializable]
    public sealed class PlayerRunState
    {
        public int CurrentFloor = 1;
        public int CurrentHp = 30;
        public int MaxHp = 30;
        public int CurrentMana = 6;
        public int MaxMana = 6;
        public int ManaRegenPerTurn = 3;
        public List<RuneInstance> Deck = new();
    }
}
