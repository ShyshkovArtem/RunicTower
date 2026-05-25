using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using RunicTower.Core;
using RunicTower.Data.Runtime;

namespace RunicTower.Services
{
    public sealed class RitualCombinationRuleTable
    {
        private const string DefaultResourcePath = "Data/Rituals/RuneCombinations";
        private readonly List<RitualCombinationRule> _rules = new();
        private static RitualCombinationRuleTable _defaultTable;

        public bool HasRules => _rules.Count > 0;

        public static RitualCombinationRuleTable LoadDefault()
        {
            if (_defaultTable != null)
            {
                return _defaultTable;
            }

            TextAsset textAsset = Resources.Load<TextAsset>(DefaultResourcePath);
            _defaultTable = new RitualCombinationRuleTable();
            if (textAsset != null)
            {
                _defaultTable.LoadFromCsv(textAsset.text);
            }

            return _defaultTable;
        }

        public void LoadFromCsv(string csvText)
        {
            _rules.Clear();
            if (string.IsNullOrWhiteSpace(csvText))
            {
                return;
            }

            string[] lines = csvText
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n');

            int headerIndex = Array.FindIndex(lines, HasManaHeader);
            if (headerIndex < 0)
            {
                return;
            }

            string[] headers = SplitSemicolonLine(lines[headerIndex]);
            Dictionary<string, int> columnByName = new(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < headers.Length; index++)
            {
                string header = headers[index].Trim();
                if (!string.IsNullOrWhiteSpace(header) && !columnByName.ContainsKey(header))
                {
                    columnByName.Add(header, index);
                }
            }

            for (int lineIndex = headerIndex + 1; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] cells = SplitSemicolonLine(line);
                if (!TryParseRule(cells, columnByName, out RitualCombinationRule rule))
                {
                    continue;
                }

                _rules.Add(rule);
            }
        }

        public bool TryFind(RitualBuild build, out RitualCombinationRule rule)
        {
            rule = null;
            if (build?.SelectedRunes == null || _rules.Count == 0)
            {
                return false;
            }

            List<RuneSignature> signatures = GetRuneSignatures(build.SelectedRunes);
            rule = _rules.FirstOrDefault(candidate => candidate.Matches(signatures));
            return rule != null;
        }

        public bool TryFindByVfxKey(string vfxKey, out RitualCombinationRule rule)
        {
            rule = null;
            if (string.IsNullOrWhiteSpace(vfxKey) || _rules.Count == 0)
            {
                return false;
            }

            rule = _rules.FirstOrDefault(candidate =>
                !string.IsNullOrWhiteSpace(candidate.VfxKey) &&
                string.Equals(candidate.VfxKey, vfxKey.Trim(), StringComparison.OrdinalIgnoreCase));
            return rule != null;
        }

        private static bool TryParseRule(
            string[] cells,
            IReadOnlyDictionary<string, int> columnByName,
            out RitualCombinationRule rule)
        {
            rule = null;

            string displayName = GetCell(cells, columnByName, "Disp. Name");
            string effectType = GetCell(cells, columnByName, "Effect Type");
            if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(effectType))
            {
                return false;
            }

            List<RuneSignature> requiredRunes = new();
            AddRuneSignature(cells, columnByName, requiredRunes, "R1 Element", "R1 Size");
            AddRuneSignature(cells, columnByName, requiredRunes, "R2 Element", "R2 Size");
            AddRuneSignature(cells, columnByName, requiredRunes, "R3 Element", "R3 Size");
            if (requiredRunes.Count == 0)
            {
                return false;
            }

            rule = new RitualCombinationRule
            {
                ManaCost = ParseInt(GetCell(cells, columnByName, "Mana")),
                DisplayName = displayName.Trim(),
                CombinationRule = GetCell(cells, columnByName, "Comb. Rule").Trim(),
                VfxKey = GetCell(cells, columnByName, "VFX Key").Trim(),
                VfxSpawnLocation = GetCell(cells, columnByName, "VFX Spawn Location").Trim(),
                VfxTargetLocation = GetCell(cells, columnByName, "VFX Target Location").Trim(),
                VfxTravelTime = ParseFloat(GetCell(cells, columnByName, "VFX Travel Time")),
                SfxKey = GetCell(cells, columnByName, "SFX Key").Trim(),
                RequiredRunes = requiredRunes
            };

            AddEffect(rule, effectType, GetCell(cells, columnByName, "Effect Value"), GetCell(cells, columnByName, "Target"));
            AddEffect(rule, GetCell(cells, columnByName, "Effect2 Type"), GetCell(cells, columnByName, "Effect2 Value"), GetCell(cells, columnByName, "Target"));
            return rule.Effects.Count > 0;
        }

        private static void AddRuneSignature(
            string[] cells,
            IReadOnlyDictionary<string, int> columnByName,
            List<RuneSignature> requiredRunes,
            string elementColumn,
            string sizeColumn)
        {
            string elementText = GetCell(cells, columnByName, elementColumn);
            string sizeText = GetCell(cells, columnByName, sizeColumn);
            if (!TryParseEnum(elementText, out ElementType element) || !TryParseEnum(sizeText, out RuneSize size))
            {
                return;
            }

            requiredRunes.Add(new RuneSignature(element, size));
        }

        private static void AddEffect(RitualCombinationRule rule, string effectTypeText, string valueText, string targetText)
        {
            if (!TryParseEffectType(effectTypeText, out EffectType effectType))
            {
                return;
            }

            int value = ParseInt(valueText);
            if (value <= 0)
            {
                return;
            }

            foreach (CombatTargetSide targetSide in ParseTargets(targetText))
            {
                rule.Effects.Add(new CombatEffectData(effectType, targetSide, value, rule.DisplayName));
            }
        }

        private static IEnumerable<CombatTargetSide> ParseTargets(string targetText)
        {
            string normalized = (targetText ?? string.Empty).Trim();
            if (normalized.Equals("Both", StringComparison.OrdinalIgnoreCase))
            {
                yield return CombatTargetSide.Self;
                yield return CombatTargetSide.Opponent;
                yield break;
            }

            if (normalized.Equals("Player", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("Self", StringComparison.OrdinalIgnoreCase))
            {
                yield return CombatTargetSide.Self;
                yield break;
            }

            yield return CombatTargetSide.Opponent;
        }

        private static List<RuneSignature> GetRuneSignatures(IEnumerable<RuneInstance> selectedRunes)
        {
            return selectedRunes
                .Where(rune => rune?.Definition != null)
                .Select(rune => new RuneSignature(rune.Definition.Element, rune.Definition.Size))
                .OrderBy(signature => signature.Element)
                .ThenBy(signature => signature.Size)
                .ToList();
        }

        private static string GetCell(string[] cells, IReadOnlyDictionary<string, int> columnByName, string columnName)
        {
            if (!columnByName.TryGetValue(columnName, out int index) || index < 0 || index >= cells.Length)
            {
                return string.Empty;
            }

            return cells[index].Trim();
        }

        private static string[] SplitSemicolonLine(string line)
        {
            return line.Split(';');
        }

        private static bool HasManaHeader(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            return SplitSemicolonLine(line)
                .Any(cell => cell.Trim().Equals("Mana", StringComparison.OrdinalIgnoreCase));
        }

        private static int ParseInt(string value)
        {
            string normalized = (value ?? string.Empty)
                .Trim()
                .Replace("%", string.Empty);

            return int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : 0;
        }

        private static float ParseFloat(string value)
        {
            string normalized = (value ?? string.Empty).Trim().Replace(',', '.');
            return float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
                ? parsed
                : 0f;
        }

        private static bool TryParseEnum<T>(string value, out T parsed) where T : struct
        {
            return Enum.TryParse(value?.Trim(), true, out parsed);
        }

        private static bool TryParseEffectType(string value, out EffectType effectType)
        {
            string normalized = (value ?? string.Empty)
                .Trim()
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace("_", string.Empty);

            if (string.Equals(normalized, "BuffCasterDamage", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "CasterDamageBuff", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "BuffDamage", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "DamageBuff", StringComparison.OrdinalIgnoreCase))
            {
                effectType = EffectType.DamageBoost;
                return true;
            }

            if (string.Equals(normalized, "DebuffEnemyDamage", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "EnemyDamageDebuff", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "DebuffDamage", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "DamageDebuff", StringComparison.OrdinalIgnoreCase))
            {
                effectType = EffectType.DamageWeakness;
                return true;
            }

            if (string.Equals(normalized, "Break", StringComparison.OrdinalIgnoreCase))
            {
                effectType = EffectType.DefenseBreak;
                return true;
            }

            return Enum.TryParse(value?.Trim(), true, out effectType);
        }
    }

    public sealed class RitualCombinationRule
    {
        public int ManaCost;
        public string DisplayName = string.Empty;
        public string CombinationRule = string.Empty;
        public string VfxKey = string.Empty;
        public string VfxSpawnLocation = string.Empty;
        public string VfxTargetLocation = string.Empty;
        public float VfxTravelTime;
        public string SfxKey = string.Empty;
        public List<RuneSignature> RequiredRunes = new();
        public List<CombatEffectData> Effects = new();

        public bool Matches(IReadOnlyList<RuneSignature> signatures)
        {
            if (signatures == null || signatures.Count != RequiredRunes.Count)
            {
                return false;
            }

            List<RuneSignature> orderedRequired = RequiredRunes
                .OrderBy(signature => signature.Element)
                .ThenBy(signature => signature.Size)
                .ToList();

            for (int index = 0; index < signatures.Count; index++)
            {
                if (!signatures[index].Equals(orderedRequired[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public readonly struct RuneSignature : IEquatable<RuneSignature>
    {
        public readonly ElementType Element;
        public readonly RuneSize Size;

        public RuneSignature(ElementType element, RuneSize size)
        {
            Element = element;
            Size = size;
        }

        public bool Equals(RuneSignature other)
        {
            return Element == other.Element && Size == other.Size;
        }

        public override bool Equals(object obj)
        {
            return obj is RuneSignature other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ((int)Element * 397) ^ (int)Size;
        }
    }
}
