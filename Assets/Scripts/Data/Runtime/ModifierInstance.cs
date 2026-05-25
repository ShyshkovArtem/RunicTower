using System;
using RunicTower.Data.Definitions;

namespace RunicTower.Data.Runtime
{
    [Serializable]
    public sealed class ModifierInstance
    {
        public string InstanceId = Guid.NewGuid().ToString("N");
        public ModifierRuneDefinition Definition;

        public ModifierInstance()
        {
        }

        public ModifierInstance(ModifierRuneDefinition definition)
        {
            Definition = definition;
        }
    }
}
