using System;
using System.Collections.Generic;

namespace RunicTower.Data.Runtime
{
    [Serializable]
    public sealed class RitualValidationResult
    {
        public bool IsValid;
        public List<string> Errors = new();

        public static RitualValidationResult Success()
        {
            return new RitualValidationResult
            {
                IsValid = true
            };
        }
    }
}
