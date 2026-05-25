using System.Linq;
using RunicTower.Data.Runtime;

namespace RunicTower.Services
{
    public sealed class RitualValidationService
    {
        private RitualCombinationRuleTable _ruleTable;

        public RitualValidationResult Validate(BattleState state, RitualBuild build)
        {
            RitualValidationResult result = RitualValidationResult.Success();

            if (build == null)
            {
                result.IsValid = false;
                result.Errors.Add("Ritual build is missing.");
                return result;
            }

            if (build.RuneCount == 0)
            {
                if (build.HasModifier)
                {
                    result.IsValid = false;
                    result.Errors.Add("Modifiers are not part of the current ritual rules.");
                }

                return result;
            }

            if (build.RuneCount > 3)
            {
                result.IsValid = false;
                result.Errors.Add("A ritual can only contain up to 3 elemental runes.");
            }

            bool containsUnknownRune = build.SelectedRunes.Any(rune =>
                rune == null ||
                rune.Definition == null ||
                state.PlayerHand.AvailableRunes.All(available => available.InstanceId != rune.InstanceId));

            if (containsUnknownRune)
            {
                result.IsValid = false;
                result.Errors.Add("The ritual contains a rune that is not in the current hand.");
            }

            if (build.HasModifier)
            {
                result.IsValid = false;
                result.Errors.Add("Modifiers are not part of the current ritual rules.");
            }

            RitualCombinationRuleTable ruleTable = GetRuleTable();
            if (ruleTable == null || !ruleTable.TryFind(build, out _))
            {
                result.IsValid = false;
                result.Errors.Add("This rune formula is not described in the ritual table.");
            }

            return result;
        }

        private RitualCombinationRuleTable GetRuleTable()
        {
            _ruleTable ??= RitualCombinationRuleTable.LoadDefault();
            return _ruleTable;
        }
    }
}
