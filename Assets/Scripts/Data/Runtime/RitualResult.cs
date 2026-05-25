using System;
using System.Collections.Generic;

namespace RunicTower.Data.Runtime
{
    [Serializable]
    public sealed class RitualResult
    {
        public bool WasSuccessful;
        public bool TriggeredFailureFallback;
        public int FailureSelfDamage;
        public int ManaCost;
        public int OverheatScore;
        public float SuccessChance;
        public string DisplayName = string.Empty;
        public string VfxKey = string.Empty;
        public string VfxSpawnLocation = string.Empty;
        public string VfxTargetLocation = string.Empty;
        public float VfxTravelTime;
        public string SfxKey = string.Empty;
        public string Summary = string.Empty;
        public List<CombatEffectData> Effects = new();
        public List<string> Notes = new();
    }
}
