using System.Collections.Generic;
using UnityEngine;
using RunicTower.Core;

namespace RunicTower.Data.Definitions
{
    [CreateAssetMenu(menuName = "RunicTower/Enemies/Enemy", fileName = "Enemy_")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [field: SerializeField] public string Id { get; private set; } = string.Empty;
        [field: SerializeField] public string DisplayName { get; private set; } = string.Empty;
        [field: SerializeField] public Sprite Icon { get; private set; }
        [field: SerializeField] public EnemyArchetype Archetype { get; private set; }
        [field: SerializeField] public int MaxHp { get; private set; } = 20;
        [field: SerializeField] public int MaxMana { get; private set; } = 5;
        [field: SerializeField] public int ManaRegenPerTurn { get; private set; } = 2;
        [field: SerializeField] public List<ElementalRuneDefinition> PreferredRunes { get; private set; } = new();
        [field: SerializeField] public bool IsBoss { get; private set; }
        [field: SerializeField] public string BossRuleDescription { get; private set; } = string.Empty;
    }
}
