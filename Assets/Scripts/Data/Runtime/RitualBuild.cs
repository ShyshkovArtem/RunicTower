using System;
using System.Collections.Generic;
using System.Linq;

namespace RunicTower.Data.Runtime
{
    [Serializable]
    public sealed class RitualBuild
    {
        public List<RuneInstance> SelectedRunes = new();
        public ModifierInstance SelectedModifier;

        public bool HasModifier => SelectedModifier?.Definition != null;
        public int RuneCount => SelectedRunes.Count;

        public IEnumerable<string> SelectedRuneInstanceIds()
        {
            return SelectedRunes.Select(rune => rune.InstanceId);
        }
    }
}
