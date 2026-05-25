using UnityEngine;
using RunicTower.Core;

namespace RunicTower.Data.Definitions
{
    [CreateAssetMenu(menuName = "RunicTower/Runes/Elemental Rune", fileName = "Rune_")]
    public sealed class ElementalRuneDefinition : ScriptableObject
    {
        [field: SerializeField] public string Id { get; private set; } = string.Empty;
        [field: SerializeField] public string DisplayName { get; private set; } = string.Empty;
        [field: SerializeField] public Sprite Icon { get; private set; }
        [field: SerializeField] public Sprite CardBackground { get; private set; }
        [field: SerializeField] public ElementType Element { get; private set; }
        [field: SerializeField] public RuneSize Size { get; private set; }
        [field: SerializeField] public int ManaCost { get; private set; } = 1;
        [field: SerializeField] public int PrimaryValue { get; private set; } = 1;
        [field: SerializeField] public int SecondaryValue { get; private set; }
    }
}
