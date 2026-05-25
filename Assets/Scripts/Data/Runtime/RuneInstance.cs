using System;
using RunicTower.Data.Definitions;

namespace RunicTower.Data.Runtime
{
    [Serializable]
    public sealed class RuneInstance
    {
        public string InstanceId = Guid.NewGuid().ToString("N");
        public ElementalRuneDefinition Definition;

        public RuneInstance()
        {
        }

        public RuneInstance(ElementalRuneDefinition definition)
        {
            Definition = definition;
        }
    }
}
