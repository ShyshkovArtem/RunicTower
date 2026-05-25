using System.Collections.Generic;
using UnityEngine;

namespace RunicTower.Data.Definitions
{
    [CreateAssetMenu(menuName = "RunicTower/Enemies/Encounter", fileName = "Encounter_")]
    public sealed class EncounterDefinition : ScriptableObject
    {
        [field: SerializeField] public string Id { get; private set; } = string.Empty;
        [field: SerializeField] public int FloorNumber { get; private set; } = 1;
        [field: SerializeField] public bool IsBossEncounter { get; private set; }
        [field: SerializeField] public List<EnemyDefinition> Enemies { get; private set; } = new();
    }
}
