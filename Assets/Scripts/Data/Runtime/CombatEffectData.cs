using System;
using RunicTower.Core;

namespace RunicTower.Data.Runtime
{
    [Serializable]
    public sealed class CombatEffectData
    {
        public EffectType EffectType;
        public CombatTargetSide TargetSide;
        public int Magnitude;
        public string SourceLabel = string.Empty;
        public string PersistentVfxKey = string.Empty;
        public bool HasShieldElement;
        public ElementType ShieldElement;

        public CombatEffectData()
        {
        }

        public CombatEffectData(EffectType effectType, CombatTargetSide targetSide, int magnitude, string sourceLabel)
        {
            EffectType = effectType;
            TargetSide = targetSide;
            Magnitude = magnitude;
            SourceLabel = sourceLabel;
        }

        public CombatEffectData(
            EffectType effectType,
            CombatTargetSide targetSide,
            int magnitude,
            string sourceLabel,
            string persistentVfxKey)
            : this(effectType, targetSide, magnitude, sourceLabel)
        {
            PersistentVfxKey = persistentVfxKey;
        }

        public CombatEffectData(
            EffectType effectType,
            CombatTargetSide targetSide,
            int magnitude,
            string sourceLabel,
            string persistentVfxKey,
            bool hasShieldElement,
            ElementType shieldElement)
            : this(effectType, targetSide, magnitude, sourceLabel, persistentVfxKey)
        {
            HasShieldElement = hasShieldElement;
            ShieldElement = shieldElement;
        }
    }
}
