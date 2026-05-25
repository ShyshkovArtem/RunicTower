using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RunicTower.Data.Runtime;
using RunicTower.UI;

namespace RunicTower.Presentation3D
{
    public sealed partial class BattleBoard3DController
    {
        private void RecalculatePreview()
        {
            if (battleController == null)
            {
                _lastValidation = null;
                _lastPreview = null;
                previewPanel?.Clear();
                UpdateRitualPreviewBook();
                return;
            }

            RitualBuild build = BuildCurrentRitual();
            if (!build.SelectedRunes.Any() && !build.HasModifier)
            {
                _lastValidation = RitualValidationResult.Success();
                _lastPreview = new RitualResult
                {
                    WasSuccessful = true,
                    ManaCost = 0,
                    SuccessChance = 100f,
                    Summary = "Player passed the round."
                };
                previewPanel?.RenderSkipPreview();
                UpdateRitualPreviewBook();
                return;
            }

            _lastValidation = battleController.PreviewRitual(build, out _lastPreview);
            if (_lastValidation.IsValid)
            {
                previewPanel?.RenderPreview(_lastPreview);
            }
            else
            {
                _lastPreview = null;
                previewPanel?.RenderValidationErrors(_lastValidation);
            }

            UpdateRitualPreviewBook();
        }

        private RitualBuild BuildCurrentRitual()
        {
            RitualBuild build = new();
            build.SelectedRunes.AddRange(_selectedRunes.Where(rune => rune?.Definition != null));
            build.SelectedModifier = _selectedModifier;
            return build;
        }

        private void UpdateRitualPreviewBook()
        {
            if (ritualPreviewBook == null)
            {
                return;
            }

            RitualBuild build = BuildCurrentRitual();
            string runeLine = BuildRuneLineSummary(build);
            string manaCost = $"Mana: {CalculatePreviewManaCost(build)}";
            string successRate = $"Success: {CalculatePreviewSuccessRate():0}%";
            string ritualName = BuildBookRitualName();
            string effect = BuildBookEffectSummary();

            ritualPreviewBook.SetPreviewContent(runeLine, manaCost, successRate, ritualName, effect);
        }

        private static int CalculatePreviewManaCost(RitualBuild build)
        {
            if (build == null)
            {
                return 0;
            }

            int manaCost = 0;

            if (build.SelectedRunes != null)
            {
                foreach (RuneInstance rune in build.SelectedRunes)
                {
                    if (rune?.Definition == null)
                    {
                        continue;
                    }

                    manaCost += rune.Definition.ManaCost;
                }
            }

            if (build.SelectedModifier?.Definition != null)
            {
                manaCost += build.SelectedModifier.Definition.ManaCostDelta;
            }

            return Mathf.Max(0, manaCost);
        }

        private float CalculatePreviewSuccessRate()
        {
            if (_lastValidation != null && !_lastValidation.IsValid)
            {
                return 0f;
            }

            return _lastPreview?.SuccessChance ?? 100f;
        }

        private string BuildBookRitualName()
        {
            if (_lastValidation != null && !_lastValidation.IsValid)
            {
                return "Ritual: Invalid";
            }

            if (_lastPreview == null)
            {
                return "Ritual: -";
            }

            if (string.IsNullOrWhiteSpace(_lastPreview.DisplayName))
            {
                return _lastPreview.ManaCost == 0 && (_lastPreview.Effects == null || _lastPreview.Effects.Count == 0)
                    ? "Ritual: PASS"
                    : "Ritual: -";
            }

            return $"Ritual: {_lastPreview.DisplayName}";
        }

        private string BuildBookEffectSummary()
        {
            if (_lastValidation != null && !_lastValidation.IsValid)
            {
                return _lastValidation.Errors != null && _lastValidation.Errors.Count > 0
                    ? $"Effect: {string.Join(" | ", _lastValidation.Errors)}"
                    : "Effect: Invalid ritual.";
            }

            if (_lastPreview == null || _lastPreview.Effects == null || _lastPreview.Effects.Count == 0)
            {
                return "Effect: PASS";
            }

            return $"Effect: {RitualPreviewPanelUI.BuildFinalEffectSummary(_lastPreview)}";
        }

        private static string BuildRuneLineSummary(RitualBuild build)
        {
            if (build == null)
            {
                return "Runes: -";
            }

            List<string> parts = new();
            if (build.SelectedRunes != null)
            {
                foreach (RuneInstance rune in build.SelectedRunes)
                {
                    if (rune?.Definition == null)
                    {
                        continue;
                    }

                    parts.Add(rune.Definition.DisplayName);
                }
            }

            if (build.SelectedModifier?.Definition != null)
            {
                parts.Add($"Mod: {build.SelectedModifier.Definition.DisplayName}");
            }

            return parts.Count == 0
                ? "Runes: PASS"
                : $"Runes: {string.Join(" + ", parts)}";
        }
    }
}
