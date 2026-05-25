namespace RunicTower.Core
{
    public enum ElementType
    {
        Fire,
        Air,
        Earth,
        Water
    }

    public enum RuneSize
    {
        Small,
        Medium,
        Large
    }

    public enum ModifierType
    {
        Stabilization,
        Direction,
        Focus,
        Echo
    }

    public enum EffectType
    {
        Damage,
        Shield,
        Heal,
        Regeneration,
        DefenseBreak,
        Burn,
        DamageBoost,
        DamageWeakness
    }

    public enum EnemyArchetype
    {
        Aggressive,
        Defensive,
        Adaptive,
        Boss
    }

    public enum CombatTargetSide
    {
        Self,
        Opponent
    }

    public enum BattlePhase
    {
        None,
        Setup,
        PlayerTurn,
        EnemyTurn,
        Resolution,
        Victory,
        Defeat
    }

    public enum RewardType
    {
        AddRune,
        Heal,
        ManaBlessing,
        RandomChest
    }
}
