using System;
using System.Collections.Generic;
using UnityEngine;
using RunicTower.Core;
using RunicTower.Data.Definitions;
using RunicTower.Data.Runtime;
using RunicTower.Services;

namespace RunicTower.Combat
{
    public sealed class BattleFlowController : MonoBehaviour
    {
        [Header("Player Setup")]
        [SerializeField] private List<ElementalRuneDefinition> playerDeckDefinitions = new();
        [SerializeField] private List<ModifierRuneDefinition> availableModifiers = new();
        [SerializeField] private int playerMaxHp = 30;
        [SerializeField] private int playerMaxMana = 6;
        [SerializeField] private int playerManaRegen = 3;

        [Header("Enemy Setup")]
        [SerializeField] private EnemyDefinition enemyDefinition;

        [Header("Turn Flow")]
        [SerializeField] private bool autoResolveEnemyTurn = true;

        public BattleState CurrentState { get; private set; } = new();
        public RitualBuild PendingEnemyBuild { get; private set; }
        public RitualResult PendingEnemyResult { get; private set; }
        public bool HasPendingEnemyTurn =>
            CurrentState != null &&
            CurrentState.Phase == BattlePhase.EnemyTurn &&
            PendingEnemyBuild != null &&
            PendingEnemyResult != null;
        public bool AutoResolveEnemyTurn => autoResolveEnemyTurn;

        public event Action<BattleState> BattleStateChanged;
        public event Action<RitualResult> RitualResolved;
        public event Action<RitualBuild, RitualResult> EnemyTurnPrepared;
        public event Action<RitualResult> EnemyRitualResolved;
        public event Action<string> LogEmitted;

        public void SetAutoResolveEnemyTurn(bool enabled)
        {
            autoResolveEnemyTurn = enabled;
        }

        private readonly HandDrawService _handDrawService = new();
        private readonly RitualValidationService _ritualValidationService = new();
        private readonly RitualCalculationService _ritualCalculationService = new();
        private readonly BattleResolutionService _battleResolutionService = new();
        private readonly EnemyDecisionService _enemyDecisionService = new();
        private readonly System.Random _random = new();

        public void StartBattle()
        {
            CurrentState = new BattleState
            {
                RoundNumber = 0,
                Phase = BattlePhase.Setup,
                Player = CreatePlayerState(),
                Enemy = CreateEnemyState()
            };
            PendingEnemyBuild = null;
            PendingEnemyResult = null;

            EmitLog("Battle initialized.");
            BeginPlayerTurn();
        }

        public void BeginPlayerTurn()
        {
            if (CurrentState.HasPendingBattleEnd)
            {
                return;
            }

            CurrentState.RoundNumber++;
            CurrentState.Phase = BattlePhase.PlayerTurn;
            CurrentState.Player.PromotePendingStatuses();
            CurrentState.Player.RestoreManaForTurn();
            CurrentState.Enemy.PromotePendingStatuses();
            CurrentState.Enemy.RestoreManaForTurn();
            if (CurrentState.PlayerHand.AvailableRunes.Count == 0 && CurrentState.PlayerHand.DrawQueue.Count == 0)
            {
                _handDrawService.InitializeCyclingHand(
                    CurrentState.PlayerHand,
                    CurrentState.Player.Deck,
                    BuildModifierPool(),
                    _random);
            }
            else
            {
                _handDrawService.RefreshCyclingHandModifier(
                    CurrentState.PlayerHand,
                    BuildModifierPool(),
                    _random);
            }

            _handDrawService.DrawNewTurnHand(
                CurrentState.EnemyHand,
                CurrentState.Enemy.Deck,
                new List<ModifierInstance>(),
                _random);

            EmitLog($"Round {CurrentState.RoundNumber} started.");
            RaiseBattleStateChanged();
        }

        public RitualValidationResult PreviewRitual(RitualBuild build, out RitualResult result)
        {
            result = null;
            RitualValidationResult validation = _ritualValidationService.Validate(CurrentState, build);
            if (!validation.IsValid)
            {
                return validation;
            }

            result = _ritualCalculationService.BuildResult(build);
            if (result.ManaCost > CurrentState.Player.CurrentMana)
            {
                validation.IsValid = false;
                validation.Errors.Add("Not enough mana for this ritual.");
            }

            return validation;
        }

        public RitualValidationResult CastPlayerRitual(RitualBuild build)
        {
            if (CurrentState == null || CurrentState.Phase != BattlePhase.PlayerTurn)
            {
                RitualValidationResult invalidPhase = RitualValidationResult.Success();
                invalidPhase.IsValid = false;
                invalidPhase.Errors.Add("You can only cast during the player's turn.");
                return invalidPhase;
            }

            RitualValidationResult validation = PreviewRitual(build, out RitualResult preview);
            if (!validation.IsValid)
            {
                foreach (string error in validation.Errors)
                {
                    EmitLog(error);
                }

                return validation;
            }

            CurrentState.CurrentSelection = build;
            RitualResult resolved = _battleResolutionService.FinalizeRitualResult(preview, _random);
            _battleResolutionService.SpendMana(CurrentState.Player, resolved);
            _battleResolutionService.ApplyRitualEffects(CurrentState, resolved);
            ConsumeSelectedRunes(build);
            _battleResolutionService.RefreshBattleOutcome(CurrentState);

            RitualResolved?.Invoke(resolved);
            EmitLog(resolved.Summary);
            RaiseBattleStateChanged();

            if (CurrentState.HasPendingBattleEnd)
            {
                return validation;
            }

            PrepareEnemyTurn();

            if (autoResolveEnemyTurn && CurrentState.Phase == BattlePhase.EnemyTurn)
            {
                ResolveEnemyTurn();
            }

            return validation;
        }

        public void ResolveEnemyTurn()
        {
            if (CurrentState == null || CurrentState.HasPendingBattleEnd || CurrentState.Phase != BattlePhase.EnemyTurn)
            {
                return;
            }

            if (PendingEnemyBuild == null || PendingEnemyResult == null || PendingEnemyBuild.RuneCount == 0)
            {
                EmitLog("Enemy skipped the turn.");
                ResolveRoundEnd();
                return;
            }

            _battleResolutionService.SpendMana(CurrentState.Enemy, PendingEnemyResult);
            _battleResolutionService.ApplyRitualEffects(SwapCombatants(CurrentState), PendingEnemyResult);
            ConsumeEnemySelectedRunes(PendingEnemyBuild);
            _battleResolutionService.RefreshBattleOutcome(CurrentState);

            EnemyRitualResolved?.Invoke(PendingEnemyResult);
            EmitLog($"Enemy casts: {PendingEnemyResult.Summary}");
            RaiseBattleStateChanged();
        }

        public void FinalizeRoundAfterEnemyTurn()
        {
            if (CurrentState == null || CurrentState.HasPendingBattleEnd)
            {
                return;
            }

            if (CurrentState.Phase != BattlePhase.EnemyTurn && CurrentState.Phase != BattlePhase.Resolution)
            {
                return;
            }

            ResolveRoundEnd();
        }

        private void PrepareEnemyTurn()
        {
            CurrentState.Phase = BattlePhase.EnemyTurn;
            PendingEnemyBuild = _enemyDecisionService.BuildEnemyRitual(CurrentState.Enemy, CurrentState.EnemyHand, _random);
            PendingEnemyResult = PendingEnemyBuild.RuneCount > 0
                ? _battleResolutionService.FinalizeRitualResult(
                    _ritualCalculationService.BuildResult(PendingEnemyBuild),
                    _random)
                : null;

            EnemyTurnPrepared?.Invoke(PendingEnemyBuild, PendingEnemyResult);
            RaiseBattleStateChanged();
        }

        private void ResolveRoundEnd()
        {
            PendingEnemyBuild = null;
            PendingEnemyResult = null;
            CurrentState.Phase = BattlePhase.Resolution;
            CurrentState.Player.ApplyEndOfRoundStatuses();
            CurrentState.Enemy.ApplyEndOfRoundStatuses();
            _battleResolutionService.RefreshBattleOutcome(CurrentState);

            if (!CurrentState.HasPendingBattleEnd)
            {
                BeginPlayerTurn();
            }
            else
            {
                RaiseBattleStateChanged();
            }
        }

        private BattleState SwapCombatants(BattleState state)
        {
            return new BattleState
            {
                RoundNumber = state.RoundNumber,
                Phase = state.Phase,
                Player = state.Enemy,
                Enemy = state.Player,
                PlayerHand = state.EnemyHand,
                EnemyHand = state.PlayerHand,
                CurrentSelection = state.CurrentSelection
            };
        }

        private CombatantState CreatePlayerState()
        {
            return new CombatantState
            {
                DisplayName = "Player",
                MaxHp = playerMaxHp,
                CurrentHp = playerMaxHp,
                MaxMana = playerMaxMana,
                CurrentMana = playerMaxMana,
                ManaRegenPerTurn = playerManaRegen,
                IsPlayer = true,
                Deck = playerDeckDefinitions.ConvertAll(definition => new RuneInstance(definition))
            };
        }

        private CombatantState CreateEnemyState()
        {
            CombatantState enemy = new CombatantState
            {
                DisplayName = enemyDefinition == null ? "Enemy" : enemyDefinition.DisplayName,
                MaxHp = enemyDefinition == null ? 20 : enemyDefinition.MaxHp,
                CurrentHp = enemyDefinition == null ? 20 : enemyDefinition.MaxHp,
                MaxMana = enemyDefinition == null ? 5 : enemyDefinition.MaxMana,
                CurrentMana = enemyDefinition == null ? 5 : enemyDefinition.MaxMana,
                ManaRegenPerTurn = enemyDefinition == null ? 2 : enemyDefinition.ManaRegenPerTurn,
                EnemyDefinition = enemyDefinition,
                Deck = new List<RuneInstance>()
            };

            if (enemyDefinition != null)
            {
                foreach (ElementalRuneDefinition rune in enemyDefinition.PreferredRunes)
                {
                    enemy.Deck.Add(new RuneInstance(rune));
                }
            }

            return enemy;
        }

        private List<ModifierInstance> BuildModifierPool()
        {
            return new List<ModifierInstance>();
        }

        private void ConsumeSelectedRunes(RitualBuild build)
        {
            _handDrawService.ConsumeAndRefillCyclingHand(
                CurrentState.PlayerHand,
                build.SelectedRunes,
                CurrentState.Player.Deck,
                _random);

            if (build.HasModifier)
            {
                CurrentState.PlayerHand.ActiveModifier = null;
            }

            CurrentState.CurrentSelection = new RitualBuild();
        }

        private void ConsumeEnemySelectedRunes(RitualBuild build)
        {
            foreach (RuneInstance rune in build.SelectedRunes)
            {
                CurrentState.EnemyHand.AvailableRunes.RemoveAll(candidate => candidate.InstanceId == rune.InstanceId);
            }

            if (build.HasModifier)
            {
                CurrentState.EnemyHand.ActiveModifier = null;
            }
        }

        private void EmitLog(string message)
        {
            LogEmitted?.Invoke(message);
        }

        private void RaiseBattleStateChanged()
        {
            BattleStateChanged?.Invoke(CurrentState);
        }
    }
}
