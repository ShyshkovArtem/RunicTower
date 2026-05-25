using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RunicTower.Audio;
using RunicTower.Combat;
using RunicTower.Core;
using RunicTower.Data.Runtime;
using RunicTower.UI;

namespace RunicTower.Presentation3D
{
    public sealed partial class BattleBoard3DController : MonoBehaviour
    {
        [Header("Logic")]
        [SerializeField] private BattleFlowController battleController;

        [Header("Optional Overlay UI")]
        [SerializeField] private CombatantPanelUI playerPanel;
        [SerializeField] private CombatantPanelUI enemyPanel;
        [SerializeField] private RitualPreviewPanelUI previewPanel;
        [SerializeField] private TMP_Text roundLabel;
        [SerializeField] private Button previewButton;
        [SerializeField] private Button castButton;
        [SerializeField] private Button skipTurnButton;
        [SerializeField] private RitualPreviewBook3D ritualPreviewBook;
        [SerializeField] private BattleAudioController audioController;
        [SerializeField] private RitualVfxController ritualVfxController;
        [SerializeField] private Transform playerMageRoot;
        [SerializeField] private Transform enemyMageRoot;

        [Header("3D Board")]
        [SerializeField] private Camera interactionCamera;
        [SerializeField] private LayerMask interactionLayers = ~0;
        [SerializeField] private Transform cardRoot;
        [SerializeField] private RuneCard3D runeCardPrefab;
        [SerializeField] private RuneSpot3D[] playerHandElementSpots;
        [SerializeField] private RuneSpot3D[] playerUpcomingRuneSpots;
        [SerializeField] private RuneSpot3D playerModifierSpot;
        [SerializeField] private RuneSpot3D[] enemyHandElementSpots;
        [SerializeField] private RuneSpot3D enemyModifierSpot;
        [SerializeField] private RitualPedestal3D ritualPedestal;

        [Header("Rune Card Presentation")]
        [SerializeField] private Vector3 cardHandScale = Vector3.one;
        [SerializeField] private Vector3 cardUpcomingScale = new(0.75f, 0.75f, 0.75f);
        [SerializeField] private Vector3 cardRitualScale = new(0.5f, 0.5f, 0.5f);
        [SerializeField] private Vector3 modifierRitualScale = new(0.35f, 0.35f, 0.35f);
        [SerializeField] private bool playCardIdleInHand = true;
        [SerializeField] private bool playCardIdleInUpcoming = true;
        [SerializeField] private Vector3 cardIdlePositionAmplitude = new(0.08f, 0.05f, 0.1f);
        [SerializeField] private Vector3 cardIdleRotationAmplitude = new(4f, 8f, 5f);
        [SerializeField] private float cardIdlePositionFrequency = 1.15f;
        [SerializeField] private float cardIdleRotationFrequency = 1.45f;
        [SerializeField] private Vector3 cardUpcomingIdlePositionAmplitude = new(0.04f, 0.03f, 0.05f);
        [SerializeField] private Vector3 cardUpcomingIdleRotationAmplitude = new(2f, 4f, 2f);
        [SerializeField] private float cardUpcomingIdlePositionFrequency = 0.9f;
        [SerializeField] private float cardUpcomingIdleRotationFrequency = 1.1f;

        [Header("Animation")]
        [SerializeField] private float cardMoveDuration = 0.56f;
        [SerializeField] private float flightArcHeight = 0.45f;
        [SerializeField] private float liftPhaseRatio = 0.2f;
        [SerializeField] private float landPhaseRatio = 0.2f;
        [SerializeField] private float playerCastWindupDuration = 0.8f;
        [SerializeField] private float enemyCastWindupDuration = 0.8f;
        [SerializeField] private float postCastBeforeDamageDelay = 0.15f;
        [SerializeField] private float damageReactionDuration = 0.75f;
        [SerializeField] private float repeatedDamageReactionDelay = 0.95f;
        [SerializeField] private float postDamageDelay = 0.2f;
        [SerializeField] private float turnHandoffDelay = 0.35f;
        [SerializeField] private float enemyRunePlacementStepDelay = 0.16f;

        [Header("Testing")]
        [SerializeField] private bool autoStartBattle = true;
        [SerializeField] private bool allowRoundLabelDoubleTapRestart = true;
        [SerializeField] private float restartDoubleTapWindow = 0.35f;
        [SerializeField] private bool debugFlightLogs;
        [SerializeField] private bool debugRitualPresentationLogs;
        [SerializeField] private bool driveEnemyTurnAnimations = true;

        [Header("Performance")]
        [SerializeField] private bool configureFramePacing = true;
        [SerializeField] private int mobileTargetFrameRate = 60;
        [SerializeField] private int desktopTargetFrameRate = -1;
        [SerializeField] private float mobileFixedDeltaTime = 0.02f;

        private readonly RuneInstance[] _selectedRunes = new RuneInstance[3];
        private readonly List<RuneCard3D> _spawnedCards = new();
        private readonly Dictionary<string, int> _playerHandSlotByRuneId = new();
        private readonly HashSet<string> _cardFlightKeys = new();

        private ModifierInstance _selectedModifier;
        private RitualValidationResult _lastValidation;
        private RitualResult _lastPreview;
        private RitualResult _lastResolvedPlayerRitual;
        private bool _hasAutoStarted;
        private bool _playerCastSequenceRunning;
        private bool _enemyTurnSequenceRunning;
        private float _lastRoundLabelTapTime = -10f;
        private int _activeFlightCount;
        private Animator _playerMageAnimator;
        private Animator _enemyMageAnimator;
        private int _lastPlayerHp = -1;
        private int _lastEnemyHp = -1;
        private bool _useAlternateRunePress;
        private bool _useAlternateEnemyRunePress;
        private RitualBuild _queuedEnemyBuild;
        private RitualResult _queuedEnemyResult;
        private bool _deferredBoardRefreshRequested;
        private Vector3 _appliedCardHandScale;
        private Vector3 _appliedCardUpcomingScale;
        private Vector3 _appliedCardRitualScale;
        private Vector3 _appliedModifierRitualScale;
        private bool _appliedPlayCardIdleInHand;
        private bool _appliedPlayCardIdleInUpcoming;
        private Vector3 _appliedCardIdlePositionAmplitude;
        private Vector3 _appliedCardIdleRotationAmplitude;
        private float _appliedCardIdlePositionFrequency;
        private float _appliedCardIdleRotationFrequency;
        private Vector3 _appliedCardUpcomingIdlePositionAmplitude;
        private Vector3 _appliedCardUpcomingIdleRotationAmplitude;
        private float _appliedCardUpcomingIdlePositionFrequency;
        private float _appliedCardUpcomingIdleRotationFrequency;

        public RitualPedestal3D RitualPedestal => ritualPedestal;
        public Transform PlayerMageRoot => playerMageRoot;
        public Transform EnemyMageRoot => enemyMageRoot;

        private Camera ActiveCamera => interactionCamera != null ? interactionCamera : Camera.main;
        private bool HasActiveFlights => _activeFlightCount > 0;
        private bool IsCastSequenceRunning => _playerCastSequenceRunning || _enemyTurnSequenceRunning;
        private bool IsPreviewBookBlockingInteraction => ritualPreviewBook != null && ritualPreviewBook.BlocksBattleInteraction;
        private static readonly int CastTrigger = Animator.StringToHash("Cast");
        private static readonly int RunePressATrigger = Animator.StringToHash("RunePressA");
        private static readonly int RunePressBTrigger = Animator.StringToHash("RunePressB");
        private static readonly int HitTrigger = Animator.StringToHash("Hit");

        private void Awake()
        {
            ApplyFramePacingSettings();
            CacheAppliedCardPresentationSettings();

            if (battleController == null)
            {
                battleController = ResolveSharedController<BattleFlowController>();
            }

            if (audioController == null)
            {
                audioController = ResolveSharedController<BattleAudioController>();
            }

            if (ritualVfxController == null)
            {
                ritualVfxController = ResolveSharedController<RitualVfxController>();
            }

            _playerMageAnimator = ResolveMageAnimator(playerMageRoot);
            _enemyMageAnimator = ResolveMageAnimator(enemyMageRoot);

            if (previewButton != null)
            {
                previewButton.onClick.AddListener(ToggleRitualPreviewBook);
            }

            if (castButton != null)
            {
                castButton.onClick.AddListener(HandleCast);
            }

            if (skipTurnButton != null)
            {
                skipTurnButton.onClick.AddListener(HandleSkipTurn);
            }

            if (ritualPreviewBook != null && previewPanel != null)
            {
                previewPanel.gameObject.SetActive(false);
            }

            if (battleController != null && driveEnemyTurnAnimations)
            {
                battleController.SetAutoResolveEnemyTurn(false);
            }
        }

        private void OnEnable()
        {
            if (ritualPreviewBook != null)
            {
                ritualPreviewBook.StateChanged += HandlePreviewBookStateChanged;
            }

            if (battleController == null)
            {
                return;
            }

            battleController.BattleStateChanged += HandleBattleStateChanged;
            battleController.RitualResolved += HandleRitualResolved;
            battleController.EnemyTurnPrepared += HandleEnemyTurnPrepared;
            battleController.EnemyRitualResolved += HandleEnemyRitualResolved;

            TryAutoStartBattle();
            RefreshAll();
        }

        private void OnDisable()
        {
            ritualVfxController?.ClearAll();

            if (ritualPreviewBook != null)
            {
                ritualPreviewBook.StateChanged -= HandlePreviewBookStateChanged;
            }

            if (battleController == null)
            {
                return;
            }

            battleController.BattleStateChanged -= HandleBattleStateChanged;
            battleController.RitualResolved -= HandleRitualResolved;
            battleController.EnemyTurnPrepared -= HandleEnemyTurnPrepared;
            battleController.EnemyRitualResolved -= HandleEnemyRitualResolved;
        }

        private void OnDestroy()
        {
            if (previewButton != null)
            {
                previewButton.onClick.RemoveListener(ToggleRitualPreviewBook);
            }

            if (castButton != null)
            {
                castButton.onClick.RemoveListener(HandleCast);
            }

            if (skipTurnButton != null)
            {
                skipTurnButton.onClick.RemoveListener(HandleSkipTurn);
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                ApplyFramePacingSettings();
            }
        }

        private void Update()
        {
            HandleBoardInput();
            HandleRoundLabelDoubleTap();
            ApplyChangedCardPresentationSettings();
        }

        private void TryAutoStartBattle()
        {
            if (!autoStartBattle || _hasAutoStarted || battleController == null)
            {
                return;
            }

            _hasAutoStarted = true;
            ClearSelection();
            battleController.StartBattle();
        }

        private void RestartBattleForTesting()
        {
            StopAllCoroutines();
            _activeFlightCount = 0;
            _cardFlightKeys.Clear();
            _playerCastSequenceRunning = false;
            _enemyTurnSequenceRunning = false;
            _queuedEnemyBuild = null;
            _queuedEnemyResult = null;
            ritualVfxController?.ClearAll();
            ClearSelection();
            battleController.StartBattle();
        }

        private void ApplyFramePacingSettings()
        {
            if (!configureFramePacing)
            {
                return;
            }

            QualitySettings.vSyncCount = 0;

#if UNITY_ANDROID || UNITY_IOS
            Application.targetFrameRate = Mathf.Max(30, mobileTargetFrameRate);
            Time.fixedDeltaTime = Mathf.Max(0.001f, mobileFixedDeltaTime);
#else
            Application.targetFrameRate = desktopTargetFrameRate;
#endif
        }

        private void ToggleRitualPreviewBook()
        {
            if (ritualPreviewBook == null)
            {
                return;
            }

            ritualPreviewBook.Toggle();
        }

        private void HandleCast()
        {
            if (IsPreviewBookBlockingInteraction)
            {
                return;
            }

            if (_playerCastSequenceRunning || _enemyTurnSequenceRunning || battleController == null)
            {
                return;
            }

            RitualBuild build = BuildCurrentRitual();
            if (build == null || build.RuneCount == 0)
            {
                RefreshButtons();
                return;
            }

            RitualValidationResult validation = battleController.PreviewRitual(build, out _);
            if (!validation.IsValid)
            {
                _lastValidation = validation;
                _lastPreview = null;
                previewPanel?.RenderValidationErrors(validation);
                UpdateRitualPreviewBook();
                RefreshButtons();
                return;
            }

            StartCoroutine(PlayPlayerCastSequence(build));
        }

        private void HandleSkipTurn()
        {
            if (IsPreviewBookBlockingInteraction)
            {
                return;
            }

            if (_playerCastSequenceRunning || _enemyTurnSequenceRunning || battleController == null)
            {
                return;
            }

            RitualBuild currentBuild = BuildCurrentRitual();
            if (currentBuild == null || currentBuild.RuneCount > 0 || currentBuild.HasModifier)
            {
                RefreshButtons();
                return;
            }

            StartCoroutine(PlayPlayerCastSequence(new RitualBuild()));
        }

        private void HandleBattleStateChanged(BattleState state)
        {
            RemoveInvalidSelections(state);
            if (!IsCastSequenceRunning)
            {
                TriggerDamageAnimations(state);
                RefreshAll();
                return;
            }

            _deferredBoardRefreshRequested = true;
            RefreshButtons();
        }

        private void HandlePreviewBookStateChanged()
        {
            RefreshButtons();
        }

        private void HandleRitualResolved(RitualResult result)
        {
            _lastResolvedPlayerRitual = result;
            _lastPreview = result;
            _lastValidation = RitualValidationResult.Success();
            previewPanel?.RenderPreview(result);
            UpdateRitualPreviewBook();
            RefreshButtons();
        }

        private void HandleEnemyTurnPrepared(RitualBuild build, RitualResult result)
        {
            if (!driveEnemyTurnAnimations || battleController == null || battleController.AutoResolveEnemyTurn)
            {
                return;
            }

            if (_playerCastSequenceRunning)
            {
                _queuedEnemyBuild = build;
                _queuedEnemyResult = result;
                return;
            }

            StartCoroutine(PlayEnemyTurnSequence(build, result));
        }

        private void HandleEnemyRitualResolved(RitualResult result)
        {
        }

        private void RefreshAll()
        {
            BattleState state = battleController != null ? battleController.CurrentState : null;
            RefreshStatusOnly(state);
            ClearSpotOccupancy();
            RebuildCards(state);
            RecalculatePreview();
            RefreshButtons();
        }

        private void RefreshStatusOnly(BattleState state)
        {
            playerPanel?.Render(state?.Player);
            enemyPanel?.Render(state?.Enemy);
            ritualVfxController?.SyncPersistentStatusVfx(
                ritualPedestal,
                playerMageRoot,
                enemyMageRoot,
                state);

            if (roundLabel != null)
            {
                roundLabel.text = $" {state?.RoundNumber ?? 0}";
            }

            RefreshButtons();
        }

        private void TriggerRunePressAnimation()
        {
            int trigger = _useAlternateRunePress ? RunePressBTrigger : RunePressATrigger;
            _useAlternateRunePress = !_useAlternateRunePress;
            TriggerAnimator(_playerMageAnimator, trigger);
        }

        private void TriggerEnemyRunePressAnimation()
        {
            int trigger = _useAlternateEnemyRunePress ? RunePressBTrigger : RunePressATrigger;
            _useAlternateEnemyRunePress = !_useAlternateEnemyRunePress;
            TriggerAnimator(_enemyMageAnimator, trigger);
        }

        private void TriggerDamageAnimations(BattleState state)
        {
            if (state?.Player != null)
            {
                if (_lastPlayerHp >= 0 && state.Player.CurrentHp < _lastPlayerHp)
                {
                    TriggerAnimator(_playerMageAnimator, HitTrigger);
                }

                _lastPlayerHp = state.Player.CurrentHp;
            }

            if (state?.Enemy != null)
            {
                if (_lastEnemyHp >= 0 && state.Enemy.CurrentHp < _lastEnemyHp)
                {
                    TriggerAnimator(_enemyMageAnimator, HitTrigger);
                }

                _lastEnemyHp = state.Enemy.CurrentHp;
            }
        }

        private static Animator ResolveMageAnimator(Transform root)
        {
            return root != null ? root.GetComponentInChildren<Animator>(true) : null;
        }

        private T ResolveSharedController<T>() where T : Component
        {
            T component = GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            component = GetComponentInParent<T>(true);
            if (component != null)
            {
                return component;
            }

            Transform parent = transform.parent;
            if (parent != null)
            {
                component = parent.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            Transform root = transform.root;
            if (root != null)
            {
                component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return FindAnyObjectByType<T>(FindObjectsInactive.Include);
        }

        private static void TriggerAnimator(Animator animator, int triggerHash)
        {
            if (animator == null)
            {
                return;
            }

            animator.ResetTrigger(CastTrigger);
            animator.ResetTrigger(RunePressATrigger);
            animator.ResetTrigger(RunePressBTrigger);
            animator.ResetTrigger(HitTrigger);
            animator.SetTrigger(triggerHash);
        }

        private void RefreshButtons()
        {
            BattleState state = battleController != null ? battleController.CurrentState : null;
            RitualBuild build = BuildCurrentRitual();
            bool canUsePlayerTurnButton = battleController != null &&
                                          !IsPreviewBookBlockingInteraction &&
                                          !HasActiveFlights &&
                                          !IsCastSequenceRunning &&
                                          state != null &&
                                          state.Phase == BattlePhase.PlayerTurn;

            if (previewButton != null)
            {
                previewButton.interactable = !HasActiveFlights && !IsCastSequenceRunning;
            }

            if (castButton != null)
            {
                castButton.interactable = canUsePlayerTurnButton &&
                                          build != null &&
                                          build.RuneCount > 0 &&
                                          _lastValidation != null &&
                                          _lastValidation.IsValid;
            }

            if (skipTurnButton != null)
            {
                skipTurnButton.interactable = canUsePlayerTurnButton &&
                                              build != null &&
                                              build.RuneCount == 0 &&
                                              !build.HasModifier;
            }
        }

        private void ClearSelection()
        {
            for (int index = 0; index < _selectedRunes.Length; index++)
            {
                _selectedRunes[index] = null;
            }

            _selectedModifier = null;
            _lastValidation = null;
            _lastPreview = null;
            _lastResolvedPlayerRitual = null;
            previewPanel?.Clear();
            RefreshAll();
        }

        private IEnumerator PlayPlayerCastSequence(RitualBuild build)
        {
            _playerCastSequenceRunning = true;
            RefreshButtons();
            bool isSkippingTurn = build == null || build.RuneCount == 0;

            int playerHpBeforeCast = battleController.CurrentState?.Player?.CurrentHp ?? -1;
            int enemyHpBeforeCast = battleController.CurrentState?.Enemy?.CurrentHp ?? -1;

            if (build != null && build.RuneCount > 0)
            {
                ritualVfxController?.ShowRuneLights(ritualPedestal, build);
                audioController?.PlayRitualCast(GetAnimatorWorldPosition(_playerMageAnimator, playerMageRoot));
                TriggerAnimator(_playerMageAnimator, CastTrigger);
                yield return WaitForSecondsRealtimeSafe(playerCastWindupDuration);
            }

            RitualValidationResult validation = battleController.CastPlayerRitual(build);
            if (!validation.IsValid)
            {
                _playerCastSequenceRunning = false;
                ritualVfxController?.ClearAll();
                _lastValidation = validation;
                _lastPreview = null;
                previewPanel?.RenderValidationErrors(validation);
                UpdateRitualPreviewBook();
                RefreshButtons();
                yield break;
            }

            RemoveResolvedRitualCards(build, BoardCardOwner.Player);
            RefreshStatusOnly(battleController != null ? battleController.CurrentState : null);
            ritualVfxController?.ClearRuneLights();
            if (!isSkippingTurn)
            {
                ritualVfxController?.PlayMagicCircle(ritualPedestal);
            }

            RitualResult playerResult = _lastResolvedPlayerRitual;
            bool playerDamageReactionHandledByVfx = false;
            LogMissingTableVfx(playerResult);
            if (playerResult?.WasSuccessful == false)
            {
                ritualVfxController?.PlayFailureVfx(ritualPedestal, PlayFailureSfx);
            }

            if (playerResult?.WasSuccessful == true)
            {
                if (ritualVfxController != null)
                {
                    yield return ritualVfxController.PlayTableVfx(
                        playerResult,
                        ritualPedestal,
                        playerMageRoot,
                        enemyMageRoot,
                        true,
                        () =>
                        {
                            playerDamageReactionHandledByVfx = TryStartDamageReactionLoop(
                                playerHpBeforeCast,
                                enemyHpBeforeCast,
                                true,
                                playerResult);
                        },
                        () => PlayTableSfx(playerResult));

                    if (playerDamageReactionHandledByVfx)
                    {
                        SyncTrackedHealth(battleController != null ? battleController.CurrentState : null);
                        RefreshStatusOnly(battleController != null ? battleController.CurrentState : null);
                    }
                }
            }

            if (!HasTableProjectile(playerResult))
            {
                yield return WaitForSecondsRealtimeSafe(postCastBeforeDamageDelay);
            }

            if (!playerDamageReactionHandledByVfx)
            {
                yield return PlayRitualAftermathSequence(playerHpBeforeCast, enemyHpBeforeCast, playerCast: true, playerResult);
            }

            _playerCastSequenceRunning = false;
            ClearSelection();

            if (_queuedEnemyBuild != null && _queuedEnemyResult != null)
            {
                RitualBuild queuedBuild = _queuedEnemyBuild;
                RitualResult queuedResult = _queuedEnemyResult;
                _queuedEnemyBuild = null;
                _queuedEnemyResult = null;
                yield return WaitForSecondsRealtimeSafe(turnHandoffDelay);
                StartCoroutine(PlayEnemyTurnSequence(queuedBuild, queuedResult));
            }
            else
            {
                RefreshButtons();
            }
        }

        private IEnumerator PlayEnemyTurnSequence(RitualBuild build, RitualResult result)
        {
            yield return null;

            if (battleController == null || battleController.CurrentState == null || battleController.CurrentState.Phase != BattlePhase.EnemyTurn)
            {
                yield break;
            }

            _enemyTurnSequenceRunning = true;
            RefreshButtons();

            if (build == null || result == null || build.RuneCount == 0)
            {
                battleController.ResolveEnemyTurn();
                yield return WaitForSecondsRealtimeSafe(postDamageDelay);
                battleController.FinalizeRoundAfterEnemyTurn();
                _enemyTurnSequenceRunning = false;
                ApplyDeferredBoardRefresh();
                yield break;
            }

            yield return AnimateEnemyRunesToPedestal(build);

            if (build.HasModifier)
            {
                yield return AnimateEnemyModifierToPedestal(build.SelectedModifier);
            }

            int playerHpBeforeCast = battleController.CurrentState?.Player?.CurrentHp ?? -1;
            int enemyHpBeforeCast = battleController.CurrentState?.Enemy?.CurrentHp ?? -1;
            ritualVfxController?.ShowRuneLights(ritualPedestal, build);
            audioController?.PlayRitualCast(GetAnimatorWorldPosition(_enemyMageAnimator, enemyMageRoot));
            TriggerAnimator(_enemyMageAnimator, CastTrigger);
            yield return WaitForSecondsRealtimeSafe(enemyCastWindupDuration);
            battleController.ResolveEnemyTurn();
            RemoveResolvedRitualCards(build, BoardCardOwner.Enemy);
            RefreshStatusOnly(battleController != null ? battleController.CurrentState : null);
            ritualVfxController?.ClearRuneLights();
            ritualVfxController?.PlayMagicCircle(ritualPedestal);
            bool enemyDamageReactionHandledByVfx = false;
            LogMissingTableVfx(result);
            if (result?.WasSuccessful == false)
            {
                ritualVfxController?.PlayFailureVfx(ritualPedestal, PlayFailureSfx);
            }

            if (result?.WasSuccessful == true)
            {
                if (ritualVfxController != null)
                {
                    yield return ritualVfxController.PlayTableVfx(
                        result,
                        ritualPedestal,
                        playerMageRoot,
                        enemyMageRoot,
                        false,
                        () =>
                        {
                            enemyDamageReactionHandledByVfx = TryStartDamageReactionLoop(
                                playerHpBeforeCast,
                                enemyHpBeforeCast,
                                false,
                                result);
                        },
                        () => PlayTableSfx(result));

                    if (enemyDamageReactionHandledByVfx)
                    {
                        SyncTrackedHealth(battleController != null ? battleController.CurrentState : null);
                        RefreshStatusOnly(battleController != null ? battleController.CurrentState : null);
                    }
                }
            }

            if (!HasTableProjectile(result))
            {
                yield return WaitForSecondsRealtimeSafe(postCastBeforeDamageDelay);
            }

            if (!enemyDamageReactionHandledByVfx)
            {
                yield return PlayRitualAftermathSequence(playerHpBeforeCast, enemyHpBeforeCast, playerCast: false, result);
            }
            battleController.FinalizeRoundAfterEnemyTurn();
            _enemyTurnSequenceRunning = false;
            ApplyDeferredBoardRefresh();
        }

        private IEnumerator PlayRitualAftermathSequence(
            int playerHpBeforeCast,
            int enemyHpBeforeCast,
            bool playerCast,
            RitualResult result)
        {
            BattleState state = battleController != null ? battleController.CurrentState : null;
            bool playedDamageReaction = false;
            bool anyHpLoss = DidHpDrop(playerHpBeforeCast, state?.Player?.CurrentHp ?? -1) ||
                             DidHpDrop(enemyHpBeforeCast, state?.Enemy?.CurrentHp ?? -1);

            if (result?.WasSuccessful == false && !anyHpLoss)
            {
                SyncTrackedHealth(state);
                RefreshStatusOnly(state);
                yield break;
            }

            if (playerCast)
            {
                if (TryTriggerDamageReaction(_enemyMageAnimator, enemyHpBeforeCast, state?.Enemy?.CurrentHp ?? -1))
                {
                    playedDamageReaction = true;
                    yield return WaitForSecondsRealtimeSafe(damageReactionDuration);
                }

                if (TryTriggerDamageReaction(_playerMageAnimator, playerHpBeforeCast, state?.Player?.CurrentHp ?? -1))
                {
                    playedDamageReaction = true;
                    yield return WaitForSecondsRealtimeSafe(damageReactionDuration);
                }
            }
            else
            {
                if (TryTriggerDamageReaction(_playerMageAnimator, playerHpBeforeCast, state?.Player?.CurrentHp ?? -1))
                {
                    playedDamageReaction = true;
                    yield return WaitForSecondsRealtimeSafe(damageReactionDuration);
                }

                if (TryTriggerDamageReaction(_enemyMageAnimator, enemyHpBeforeCast, state?.Enemy?.CurrentHp ?? -1))
                {
                    playedDamageReaction = true;
                    yield return WaitForSecondsRealtimeSafe(damageReactionDuration);
                }
            }

            if (playedDamageReaction)
            {
                yield return WaitForSecondsRealtimeSafe(postDamageDelay);
            }

            SyncTrackedHealth(state);
            RefreshStatusOnly(state);
        }

        private static bool DidHpDrop(int hpBefore, int hpAfter)
        {
            return hpBefore >= 0 && hpAfter >= 0 && hpAfter < hpBefore;
        }

        private bool TryTriggerDamageReaction(Animator animator, int hpBefore, int hpAfter)
        {
            if (!DidHpDrop(hpBefore, hpAfter))
            {
                return false;
            }

            audioController?.PlayDamage(GetAnimatorWorldPosition(animator, null));
            TriggerAnimator(animator, HitTrigger);
            return animator != null;
        }

        private bool TryStartDamageReactionLoop(
            int playerHpBeforeCast,
            int enemyHpBeforeCast,
            bool playerCast,
            RitualResult result)
        {
            bool triggered = TryTriggerDamageReactions(playerHpBeforeCast, enemyHpBeforeCast, playerCast);
            if (!triggered || result == null || !string.IsNullOrWhiteSpace(result.VfxTargetLocation))
            {
                return triggered;
            }

            float duration = Mathf.Max(0f, result.VfxTravelTime);
            if (duration > repeatedDamageReactionDelay)
            {
                StartCoroutine(RepeatDamageReactionsDuringVfx(
                    playerHpBeforeCast,
                    enemyHpBeforeCast,
                    playerCast,
                    duration));
            }

            return true;
        }

        private IEnumerator RepeatDamageReactionsDuringVfx(
            int playerHpBeforeCast,
            int enemyHpBeforeCast,
            bool playerCast,
            float duration)
        {
            float interval = Mathf.Max(0.1f, repeatedDamageReactionDelay);
            float elapsed = interval;
            while (elapsed < duration)
            {
                yield return WaitForSecondsRealtimeSafe(interval);
                elapsed += interval;
                TryTriggerDamageReactions(playerHpBeforeCast, enemyHpBeforeCast, playerCast);
            }
        }

        private bool TryTriggerDamageReactions(int playerHpBeforeCast, int enemyHpBeforeCast, bool playerCast)
        {
            BattleState state = battleController != null ? battleController.CurrentState : null;
            bool triggered = false;

            if (playerCast)
            {
                triggered |= TryTriggerDamageReaction(_enemyMageAnimator, enemyHpBeforeCast, state?.Enemy?.CurrentHp ?? -1);
                triggered |= TryTriggerDamageReaction(_playerMageAnimator, playerHpBeforeCast, state?.Player?.CurrentHp ?? -1);
                return triggered;
            }

            triggered |= TryTriggerDamageReaction(_playerMageAnimator, playerHpBeforeCast, state?.Player?.CurrentHp ?? -1);
            triggered |= TryTriggerDamageReaction(_enemyMageAnimator, enemyHpBeforeCast, state?.Enemy?.CurrentHp ?? -1);
            return triggered;
        }

        private void PlayTableSfx(RitualResult result)
        {
            if (result == null || string.IsNullOrWhiteSpace(result.SfxKey))
            {
                return;
            }

            Vector3 position = ritualPedestal != null ? ritualPedestal.transform.position : transform.position;
            audioController?.PlaySfxKey(result.SfxKey, position);
        }

        private void PlayFailureSfx(Vector3 position, string sfxKey)
        {
            if (string.IsNullOrWhiteSpace(sfxKey))
            {
                return;
            }

            audioController?.PlaySfxKey(sfxKey, position);
        }

        private static bool HasTableProjectile(RitualResult result)
        {
            return result != null &&
                   result.WasSuccessful &&
                   !string.IsNullOrWhiteSpace(result.VfxKey) &&
                   !string.IsNullOrWhiteSpace(result.VfxTargetLocation);
        }

        private void LogMissingTableVfx(RitualResult result)
        {
            if (!debugRitualPresentationLogs || result == null || !result.WasSuccessful)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(result.VfxKey))
            {
                Debug.LogWarning(
                    $"[BattleBoard3D] Successful ritual '{result.DisplayName}' has no VFX key from the table.",
                    this);
            }
        }

        private void SyncTrackedHealth(BattleState state)
        {
            if (state?.Player != null)
            {
                _lastPlayerHp = state.Player.CurrentHp;
            }

            if (state?.Enemy != null)
            {
                _lastEnemyHp = state.Enemy.CurrentHp;
            }
        }

        private static Vector3 GetAnimatorWorldPosition(Animator animator, Transform fallbackRoot)
        {
            if (animator != null)
            {
                return animator.transform.position;
            }

            return fallbackRoot != null ? fallbackRoot.position : Vector3.zero;
        }

        private void RemoveResolvedRitualCards(RitualBuild build, BoardCardOwner owner)
        {
            if (build == null)
            {
                return;
            }

            for (int index = _spawnedCards.Count - 1; index >= 0; index--)
            {
                RuneCard3D card = _spawnedCards[index];
                if (card == null || card.Owner != owner || card.Location != BoardCardLocation.Ritual)
                {
                    continue;
                }

                bool matchesRune = !card.IsModifier &&
                                   build.SelectedRunes.Any(rune => rune?.InstanceId == card.BoundRune?.InstanceId);
                bool matchesModifier = card.IsModifier &&
                                       build.SelectedModifier?.InstanceId == card.BoundModifier?.InstanceId;
                if (!matchesRune && !matchesModifier)
                {
                    continue;
                }

                _spawnedCards.RemoveAt(index);
                Destroy(card.gameObject);
            }

            ritualPedestal?.ClearOccupancy();
        }

        private void ApplyDeferredBoardRefresh()
        {
            if (_deferredBoardRefreshRequested)
            {
                _deferredBoardRefreshRequested = false;
                RefreshAll();
                return;
            }

            RefreshStatusOnly(battleController != null ? battleController.CurrentState : null);
        }

        private void ApplyCardPresentationSettings(RuneCard3D card)
        {
            if (card == null)
            {
                return;
            }

            card.ConfigurePresentation(
                cardHandScale,
                cardUpcomingScale,
                cardRitualScale,
                modifierRitualScale,
                playCardIdleInHand,
                playCardIdleInUpcoming,
                cardIdlePositionAmplitude,
                cardIdleRotationAmplitude,
                cardIdlePositionFrequency,
                cardIdleRotationFrequency,
                cardUpcomingIdlePositionAmplitude,
                cardUpcomingIdleRotationAmplitude,
                cardUpcomingIdlePositionFrequency,
                cardUpcomingIdleRotationFrequency);
        }

        private void ApplyCardPresentationSettingsToSpawnedCards()
        {
            foreach (RuneCard3D card in _spawnedCards)
            {
                ApplyCardPresentationSettings(card);
            }
        }

        private void ApplyChangedCardPresentationSettings()
        {
            if (_appliedCardHandScale == cardHandScale &&
                _appliedCardUpcomingScale == cardUpcomingScale &&
                _appliedCardRitualScale == cardRitualScale &&
                _appliedModifierRitualScale == modifierRitualScale &&
                _appliedPlayCardIdleInHand == playCardIdleInHand &&
                _appliedPlayCardIdleInUpcoming == playCardIdleInUpcoming &&
                _appliedCardIdlePositionAmplitude == cardIdlePositionAmplitude &&
                _appliedCardIdleRotationAmplitude == cardIdleRotationAmplitude &&
                Mathf.Approximately(_appliedCardIdlePositionFrequency, cardIdlePositionFrequency) &&
                Mathf.Approximately(_appliedCardIdleRotationFrequency, cardIdleRotationFrequency) &&
                _appliedCardUpcomingIdlePositionAmplitude == cardUpcomingIdlePositionAmplitude &&
                _appliedCardUpcomingIdleRotationAmplitude == cardUpcomingIdleRotationAmplitude &&
                Mathf.Approximately(_appliedCardUpcomingIdlePositionFrequency, cardUpcomingIdlePositionFrequency) &&
                Mathf.Approximately(_appliedCardUpcomingIdleRotationFrequency, cardUpcomingIdleRotationFrequency))
            {
                return;
            }

            ApplyCardPresentationSettingsToSpawnedCards();
            CacheAppliedCardPresentationSettings();
        }

        private void CacheAppliedCardPresentationSettings()
        {
            _appliedCardHandScale = cardHandScale;
            _appliedCardUpcomingScale = cardUpcomingScale;
            _appliedCardRitualScale = cardRitualScale;
            _appliedModifierRitualScale = modifierRitualScale;
            _appliedPlayCardIdleInHand = playCardIdleInHand;
            _appliedPlayCardIdleInUpcoming = playCardIdleInUpcoming;
            _appliedCardIdlePositionAmplitude = cardIdlePositionAmplitude;
            _appliedCardIdleRotationAmplitude = cardIdleRotationAmplitude;
            _appliedCardIdlePositionFrequency = cardIdlePositionFrequency;
            _appliedCardIdleRotationFrequency = cardIdleRotationFrequency;
            _appliedCardUpcomingIdlePositionAmplitude = cardUpcomingIdlePositionAmplitude;
            _appliedCardUpcomingIdleRotationAmplitude = cardUpcomingIdleRotationAmplitude;
            _appliedCardUpcomingIdlePositionFrequency = cardUpcomingIdlePositionFrequency;
            _appliedCardUpcomingIdleRotationFrequency = cardUpcomingIdleRotationFrequency;
        }

        private void RemoveInvalidSelections(BattleState state)
        {
            if (state?.PlayerHand?.AvailableRunes == null)
            {
                System.Array.Clear(_selectedRunes, 0, _selectedRunes.Length);
                _selectedModifier = null;
                return;
            }

            HashSet<string> validIds = state.PlayerHand.AvailableRunes
                .Where(rune => rune?.Definition != null)
                .Select(rune => rune.InstanceId)
                .ToHashSet();

            for (int index = 0; index < _selectedRunes.Length; index++)
            {
                RuneInstance rune = _selectedRunes[index];
                if (rune != null && !validIds.Contains(rune.InstanceId))
                {
                    _selectedRunes[index] = null;
                }
            }

            if (state.PlayerHand.ActiveModifier?.Definition == null ||
                (_selectedModifier != null && state.PlayerHand.ActiveModifier.InstanceId != _selectedModifier.InstanceId))
            {
                _selectedModifier = null;
            }
        }

        private static float EaseOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        private static float EaseInCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t;
        }

        private static IEnumerator WaitForSecondsRealtimeSafe(float duration)
        {
            float elapsed = 0f;
            float total = Mathf.Max(0.01f, duration);
            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
}

