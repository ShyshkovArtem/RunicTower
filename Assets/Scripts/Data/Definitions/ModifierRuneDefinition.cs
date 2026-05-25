using UnityEngine;
using RunicTower.Core;

namespace RunicTower.Data.Definitions
{
    [CreateAssetMenu(menuName = "RunicTower/Runes/Modifier Rune", fileName = "Modifier_")]
    public sealed class ModifierRuneDefinition : ScriptableObject
    {
        [field: SerializeField] public string Id { get; private set; } = string.Empty;
        [field: SerializeField] public string DisplayName { get; private set; } = string.Empty;
        [field: SerializeField] public Sprite Icon { get; private set; }
        [field: SerializeField] public Sprite CardBackground { get; private set; }
        [field: SerializeField] public ModifierType ModifierType { get; private set; }
        [field: SerializeField] public int ManaCostDelta { get; private set; }
        [field: SerializeField] public float SuccessChanceDelta { get; private set; }
    }
}
