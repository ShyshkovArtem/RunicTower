using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RunicTower.Core;
using RunicTower.Data.Runtime;
using RunicTower.Services;

namespace RunicTower.Presentation3D
{
    [System.Serializable]
    public sealed class KeyedRitualVfx
    {
        [SerializeField] private string key;
        [SerializeField] private GameObject prefab;
        [SerializeField] private float holdBeforeTravel = 0.35f;
        [SerializeField] private bool rotateOnEnemyCast = true;
        [SerializeField] private bool persistWhileShielded;
        [SerializeField] private Vector3 projectileRotationEuler;

        public string Key => key;
        public GameObject Prefab => prefab;
        public float HoldBeforeTravel => holdBeforeTravel;
        public bool RotateOnEnemyCast => rotateOnEnemyCast;
        public bool PersistWhileShielded => persistWhileShielded;
        public Vector3 ProjectileRotationEuler => projectileRotationEuler;
    }

    public enum DebugVfxTarget
    {
        Enemy,
        Player
    }

    public sealed class RitualVfxController : MonoBehaviour
    {
        [Header("Rune Lights")]
        [SerializeField] private GameObject runeLightPrefab;
        [SerializeField] private Vector3 runeLightOffset = new(0f, 0.03f, 0f);
        [SerializeField] private bool parentRuneLightsToSpots = true;

        [Header("Magic Circle")]
        [SerializeField] private GameObject magicCirclePrefab;
        [SerializeField] private Transform magicCircleAnchor;
        [SerializeField] private Vector3 magicCircleOffset = new(0f, 0.35f, 0f);
        [SerializeField] private Vector3 magicCircleRotationEuler;
        [SerializeField] private float magicCircleLifetime = 1.2f;
        [SerializeField] private bool parentMagicCircleToAnchor = true;

        [Header("Failure VFX")]
        [SerializeField] private GameObject failureVfxPrefab;
        [SerializeField] private string failureSfxKey = string.Empty;
        [SerializeField] private Vector3 failureVfxOffset = new(0f, 1f, 0f);
        [SerializeField] private Vector3 failureVfxRotationEuler;
        [SerializeField] private float failureVfxLifetime = 1.2f;
        [SerializeField] private bool parentFailureVfxToAnchor;

        [Header("Persistent Status VFX")]
        [SerializeField] private Vector3 shieldStatusVfxOffset = new(0f, 1f, 0f);
        [SerializeField] private Vector3 shieldStatusVfxRotationEuler;
        [SerializeField] private bool parentShieldStatusVfxToCharacter = true;

        [Header("Table VFX")]
        [SerializeField] private KeyedRitualVfx[] keyedVfxPrefabs;
        [SerializeField] private float tableVfxStartDelay = 0.08f;
        [SerializeField] private bool detachProjectileAfterTravel = true;
        [SerializeField] private Vector3 magicCircleTableVfxSpawnOffset = new(0f, 0.25f, 0f);
        [SerializeField] private Vector3 characterVfxOffset = new(0f, 1f, 0f);
        [SerializeField] private Vector3 groundVfxOffset = Vector3.zero;
        [SerializeField] private bool debugTableVfxLogs;

        [Header("Debug Test")]
        [SerializeField] private string debugTestVfxKey = string.Empty;
        [SerializeField] private DebugVfxTarget debugTestTarget = DebugVfxTarget.Enemy;

        private readonly List<GameObject> _activeRuneLights = new();
        private readonly List<GameObject> _activeTableVfx = new();
        private GameObject _activeMagicCircle;
        private GameObject _playerShieldStatusVfx;
        private GameObject _enemyShieldStatusVfx;
        private string _playerShieldStatusVfxKey = string.Empty;
        private string _enemyShieldStatusVfxKey = string.Empty;
        private Coroutine _circleRoutine;

        public void ShowRuneLights(RitualPedestal3D pedestal, RitualBuild build)
        {
            ClearRuneLights();

            if (runeLightPrefab == null || pedestal == null || build?.SelectedRunes == null)
            {
                return;
            }

            int count = Mathf.Min(build.SelectedRunes.Count, pedestal.ElementalSpotCount);
            for (int index = 0; index < count; index++)
            {
                RuneInstance rune = build.SelectedRunes[index];
                RuneSpot3D spot = pedestal.GetElementalSpot(index);
                if (rune?.Definition == null || spot?.Anchor == null)
                {
                    continue;
                }

                Transform parent = parentRuneLightsToSpots ? spot.Anchor : transform;
                GameObject lightInstance = Instantiate(
                    runeLightPrefab,
                    spot.Anchor.position + runeLightOffset,
                    spot.Anchor.rotation,
                    parent);

                if (parentRuneLightsToSpots)
                {
                    lightInstance.transform.localPosition = runeLightOffset;
                }

                _activeRuneLights.Add(lightInstance);
            }
        }

        public void ClearRuneLights()
        {
            for (int index = _activeRuneLights.Count - 1; index >= 0; index--)
            {
                GameObject lightInstance = _activeRuneLights[index];
                if (lightInstance != null)
                {
                    Destroy(lightInstance);
                }
            }

            _activeRuneLights.Clear();
        }

        public void PlayMagicCircle(RitualPedestal3D pedestal)
        {
            if (magicCirclePrefab == null)
            {
                return;
            }

            ClearMagicCircle();

            Transform anchor = magicCircleAnchor != null
                ? magicCircleAnchor
                : pedestal != null
                    ? pedestal.transform
                    : transform;

            Transform parent = parentMagicCircleToAnchor ? anchor : transform;
            Quaternion rotation = anchor.rotation * Quaternion.Euler(magicCircleRotationEuler);
            _activeMagicCircle = Instantiate(
                magicCirclePrefab,
                anchor.position + magicCircleOffset,
                rotation,
                parent);

            if (parentMagicCircleToAnchor)
            {
                _activeMagicCircle.transform.localPosition = magicCircleOffset;
                _activeMagicCircle.transform.localRotation = Quaternion.Euler(magicCircleRotationEuler);
            }

            if (magicCircleLifetime > 0f)
            {
                _circleRoutine = StartCoroutine(ClearMagicCircleAfterDelay(magicCircleLifetime));
            }
        }

        public void PlayFailureVfx(RitualPedestal3D pedestal, System.Action<Vector3, string> spawnSfxCallback = null)
        {
            if (failureVfxPrefab == null)
            {
                return;
            }

            Transform anchor = magicCircleAnchor != null
                ? magicCircleAnchor
                : pedestal != null
                    ? pedestal.transform
                    : transform;

            Transform parent = parentFailureVfxToAnchor ? anchor : transform;
            Quaternion rotation = anchor.rotation * Quaternion.Euler(failureVfxRotationEuler);
            GameObject instance = Instantiate(
                failureVfxPrefab,
                GetMagicCirclePosition(pedestal) + failureVfxOffset,
                rotation,
                parent);

            if (parentFailureVfxToAnchor)
            {
                instance.transform.localPosition = magicCircleOffset + failureVfxOffset;
                instance.transform.localRotation = Quaternion.Euler(failureVfxRotationEuler);
            }

            _activeTableVfx.Add(instance);
            spawnSfxCallback?.Invoke(instance.transform.position, failureSfxKey);
            if (failureVfxLifetime > 0f)
            {
                StartCoroutine(ClearTableVfxAfterDelay(instance, failureVfxLifetime));
            }
        }

        public IEnumerator PlayTableVfx(
            RitualResult result,
            RitualPedestal3D pedestal,
            Transform playerRoot,
            Transform enemyRoot,
            bool playerCast,
            System.Action impactCallback = null,
            System.Action spawnCallback = null)
        {
            if (result == null || string.IsNullOrWhiteSpace(result.VfxKey))
            {
                yield break;
            }

            KeyedRitualVfx vfx = FindVfx(result.VfxKey);
            if (ShouldUsePersistentShieldVfx(result, vfx))
            {
                spawnCallback?.Invoke();
                yield break;
            }

            if (vfx?.Prefab == null)
            {
                if (debugTableVfxLogs)
                {
                    Debug.LogWarning(
                        $"[RitualVfxController] No VFX prefab registered for key '{result.VfxKey}'.",
                        this);
                }

                yield break;
            }

            Transform casterRoot = playerCast ? playerRoot : enemyRoot;
            Transform opponentRoot = playerCast ? enemyRoot : playerRoot;
            Transform casterTarget = ResolveCharacterTarget(casterRoot);
            Transform opponentTarget = ResolveCharacterTarget(opponentRoot);
            Vector3 spawnPosition = ResolveLocation(result.VfxSpawnLocation, pedestal, casterTarget, opponentTarget);
            if (IsMagicCircleLocation(result.VfxSpawnLocation))
            {
                spawnPosition += magicCircleTableVfxSpawnOffset;
            }

            string targetLocation = string.IsNullOrWhiteSpace(result.VfxTargetLocation)
                ? result.VfxSpawnLocation
                : result.VfxTargetLocation;
            Vector3 targetPosition = ResolveLocation(targetLocation, pedestal, casterTarget, opponentTarget);

            if (tableVfxStartDelay > 0f)
            {
                yield return WaitForSecondsRealtimeSafe(tableVfxStartDelay);
            }

            bool rotateProjectile = ShouldRotateProjectile(result, playerCast, vfx);
            GameObject instance;
            Transform movingTransform;
            if (rotateProjectile)
            {
                instance = CreateRotatingProjectileInstance(vfx, spawnPosition, targetPosition);
                movingTransform = instance != null ? instance.transform : null;
            }
            else
            {
                instance = Instantiate(vfx.Prefab, spawnPosition, vfx.Prefab.transform.rotation, transform);
                movingTransform = instance.transform;
            }

            if (instance == null)
            {
                yield break;
            }

            _activeTableVfx.Add(instance);
            spawnCallback?.Invoke();
            if (string.IsNullOrWhiteSpace(result.VfxTargetLocation))
            {
                impactCallback?.Invoke();
            }

            if (debugTableVfxLogs)
            {
                Debug.Log(
                    $"[RitualVfxController] Playing VFX '{result.VfxKey}' from '{result.VfxSpawnLocation}' to '{result.VfxTargetLocation}' over {result.VfxTravelTime:0.##}s.",
                    this);
            }

            float duration = Mathf.Max(0f, result.VfxTravelTime);
            if (!string.IsNullOrWhiteSpace(result.VfxTargetLocation))
            {
                if (vfx.HoldBeforeTravel > 0f)
                {
                    yield return WaitForSecondsRealtimeSafe(vfx.HoldBeforeTravel);
                }

                yield return MoveVfx(
                    movingTransform,
                    spawnPosition,
                    targetPosition,
                    duration,
                    rotateProjectile);
                impactCallback?.Invoke();
                if (detachProjectileAfterTravel)
                {
                    _activeTableVfx.Remove(instance);
                    Destroy(instance);
                }

                yield break;
            }
            else
            {
                yield return WaitForSecondsRealtimeSafe(duration);
            }

            if (instance != null)
            {
                _activeTableVfx.Remove(instance);
                Destroy(instance);
            }
        }

        public void ClearAll()
        {
            ClearRuneLights();
            ClearMagicCircle();
            ClearTableVfx();
            ClearPersistentStatusVfx();
        }

        public void SyncPersistentStatusVfx(
            RitualPedestal3D pedestal,
            Transform playerRoot,
            Transform enemyRoot,
            BattleState state)
        {
            SyncShieldStatusVfx(
                ref _playerShieldStatusVfx,
                ref _playerShieldStatusVfxKey,
                state?.Player?.Shield > 0,
                state?.Player?.ShieldVfxKey,
                playerRoot,
                pedestal);

            SyncShieldStatusVfx(
                ref _enemyShieldStatusVfx,
                ref _enemyShieldStatusVfxKey,
                state?.Enemy?.Shield > 0,
                state?.Enemy?.ShieldVfxKey,
                enemyRoot,
                pedestal);
        }

        public void PlayDebugTableVfxFromInspector()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[RitualVfxController] Enter Play Mode to test table VFX from the inspector.", this);
                return;
            }

            if (string.IsNullOrWhiteSpace(debugTestVfxKey))
            {
                Debug.LogWarning("[RitualVfxController] Debug test VFX key is empty.", this);
                return;
            }

            RitualCombinationRuleTable ruleTable = RitualCombinationRuleTable.LoadDefault();
            if (ruleTable == null || !ruleTable.TryFindByVfxKey(debugTestVfxKey, out RitualCombinationRule rule))
            {
                Debug.LogWarning(
                    $"[RitualVfxController] No ritual table rule found for VFX key '{debugTestVfxKey}'.",
                    this);
                return;
            }

            RitualResult debugResult = new()
            {
                VfxKey = rule.VfxKey,
                VfxSpawnLocation = rule.VfxSpawnLocation,
                VfxTargetLocation = string.IsNullOrWhiteSpace(rule.VfxTargetLocation)
                    ? string.Empty
                    : debugTestTarget == DebugVfxTarget.Player
                        ? "Opponent"
                        : "Target",
                VfxTravelTime = Mathf.Max(0f, rule.VfxTravelTime)
            };

            BattleBoard3DController board = ResolveBattleBoard();
            RitualPedestal3D pedestal = board != null ? board.RitualPedestal : null;
            Transform playerRoot = board != null ? board.PlayerMageRoot : null;
            Transform enemyRoot = board != null ? board.EnemyMageRoot : null;
            bool playerCast = debugTestTarget == DebugVfxTarget.Enemy;

            StartCoroutine(PlayTableVfx(
                debugResult,
                pedestal,
                playerRoot,
                enemyRoot,
                playerCast));
        }

        private void ClearMagicCircle()
        {
            if (_circleRoutine != null)
            {
                StopCoroutine(_circleRoutine);
                _circleRoutine = null;
            }

            if (_activeMagicCircle != null)
            {
                Destroy(_activeMagicCircle);
                _activeMagicCircle = null;
            }
        }

        private IEnumerator ClearMagicCircleAfterDelay(float delay)
        {
            float elapsed = 0f;
            while (elapsed < delay)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            _circleRoutine = null;
            ClearMagicCircle();
        }

        private BattleBoard3DController ResolveBattleBoard()
        {
            BattleBoard3DController localBoard = GetComponentInParent<BattleBoard3DController>();
            if (localBoard != null)
            {
                return localBoard;
            }

            return FindFirstObjectByType<BattleBoard3DController>();
        }

        private KeyedRitualVfx FindVfx(string key)
        {
            if (keyedVfxPrefabs == null)
            {
                return null;
            }

            KeyedRitualVfx directMatch = FindRegisteredVfx(key);
            if (directMatch != null)
            {
                return directMatch;
            }

            string fallbackKey = GetFallbackVfxKey(key);
            if (!string.IsNullOrWhiteSpace(fallbackKey))
            {
                return FindRegisteredVfx(fallbackKey);
            }

            return null;
        }

        private KeyedRitualVfx FindRegisteredVfx(string key)
        {
            foreach (KeyedRitualVfx entry in keyedVfxPrefabs)
            {
                if (entry?.Prefab != null &&
                    string.Equals(entry.Key, key, System.StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            return null;
        }

        private static bool ShouldUsePersistentShieldVfx(RitualResult result, KeyedRitualVfx vfx)
        {
            if (result?.Effects == null || vfx == null || !vfx.PersistWhileShielded)
            {
                return false;
            }

            foreach (CombatEffectData effect in result.Effects)
            {
                if (effect != null && effect.EffectType == EffectType.Shield)
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetFallbackVfxKey(string key)
        {
            string normalized = (key ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return string.Empty;
            }

            return normalized switch
            {
                "vfx_sFireBurst" => "vfx_sFireRain",
                "vfx_FireBurst" => "vfx_FireRain",
                "vfx_dsFlameBlade" => "vfx_sFlameBlade",
                "vfx_dFlameBlade" => "vfx_FlameBlade",
                "vfx_tFireball" => "vfx_Fireball",
                "vfx_FlameBarrier" => "vfx_FireRain",
                "vfx_gFireball" => "vfx_bFireball",
                "vfx_Hellwind" => "vfx_bFireRain",
                "vfx_sAirImpact" => "vfx_sFlameBlade",
                "vfx_AirImpact" => "vfx_FlameBlade",
                "vfx_sAirBarrier" => "vfx_sFireRain",
                "vfx_AirBarrier" => "vfx_FireRain",
                "vfx_sAirBlade" => "vfx_sFlameBlade",
                "vfx_AirBlade" => "vfx_FlameBlade",
                "vfx_AirStrike" => "vfx_bFireball",
                "vfx_multiAirBlade" => "vfx_FireRain",
                _ => string.Empty
            };
        }

        private Vector3 ResolveLocation(
            string location,
            RitualPedestal3D pedestal,
            Transform casterRoot,
            Transform opponentRoot)
        {
            string normalized = (location ?? string.Empty).Trim();
            if (IsMagicCircleLocation(normalized))
            {
                return GetMagicCirclePosition(pedestal);
            }

            if (normalized.Equals("Enemy", System.StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("Opponent", System.StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("Target", System.StringComparison.OrdinalIgnoreCase))
            {
                return opponentRoot != null ? opponentRoot.position + characterVfxOffset : GetMagicCirclePosition(pedestal);
            }

            if (normalized.Equals("Player", System.StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("Self", System.StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("Caster", System.StringComparison.OrdinalIgnoreCase))
            {
                return casterRoot != null ? casterRoot.position + characterVfxOffset : GetMagicCirclePosition(pedestal);
            }

            if (normalized.Equals("EnemyGround", System.StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("TargetGround", System.StringComparison.OrdinalIgnoreCase))
            {
                return opponentRoot != null ? opponentRoot.position + groundVfxOffset : GetMagicCirclePosition(pedestal);
            }

            if (normalized.Equals("PlayerGround", System.StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("CasterGround", System.StringComparison.OrdinalIgnoreCase))
            {
                return casterRoot != null ? casterRoot.position + groundVfxOffset : GetMagicCirclePosition(pedestal);
            }

            return GetMagicCirclePosition(pedestal);
        }

        private static bool IsMagicCircleLocation(string location)
        {
            string normalized = (location ?? string.Empty).Trim();
            return normalized.Equals("MagicalCircle", System.StringComparison.OrdinalIgnoreCase) ||
                   normalized.Equals("MagicCircle", System.StringComparison.OrdinalIgnoreCase) ||
                   normalized.Equals("Pedestal", System.StringComparison.OrdinalIgnoreCase);
        }

        private static Transform ResolveCharacterTarget(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            Animator animator = root.GetComponentInChildren<Animator>(true);
            return animator != null ? animator.transform : root;
        }

        private Vector3 GetMagicCirclePosition(RitualPedestal3D pedestal)
        {
            Transform anchor = magicCircleAnchor != null
                ? magicCircleAnchor
                : pedestal != null
                    ? pedestal.transform
                    : transform;

            return anchor.position + magicCircleOffset;
        }

        private IEnumerator MoveVfx(
            Transform movingTransform,
            Vector3 startPosition,
            Vector3 targetPosition,
            float duration,
            bool rotateProjectile)
        {
            if (movingTransform == null)
            {
                yield break;
            }

            float total = Mathf.Max(0.01f, duration);
            float elapsed = 0f;
            if (rotateProjectile)
            {
                TrySetProjectileLookRotation(movingTransform, startPosition, targetPosition);
            }

            while (elapsed < total)
            {
                if (movingTransform == null)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / total);
                movingTransform.position = Vector3.Lerp(startPosition, targetPosition, EaseInOutCubic(t));
                if (rotateProjectile)
                {
                    TrySetProjectileLookRotation(movingTransform, movingTransform.position, targetPosition);
                }

                yield return null;
            }

            if (movingTransform != null)
            {
                movingTransform.position = targetPosition;
                if (rotateProjectile)
                {
                    TrySetProjectileLookRotation(movingTransform, startPosition, targetPosition);
                }
            }
        }

        private static bool ShouldRotateProjectile(RitualResult result, bool playerCast, KeyedRitualVfx vfx)
        {
            return !playerCast &&
                   result != null &&
                   vfx != null &&
                   vfx.RotateOnEnemyCast &&
                   !string.IsNullOrWhiteSpace(result.VfxTargetLocation);
        }

        private GameObject CreateRotatingProjectileInstance(
            KeyedRitualVfx vfx,
            Vector3 spawnPosition,
            Vector3 targetPosition)
        {
            if (vfx?.Prefab == null)
            {
                return null;
            }

            GameObject root = new($"{vfx.Prefab.name}_ProjectileRoot");
            root.transform.SetParent(transform, false);
            root.transform.position = spawnPosition;
            root.transform.rotation = GetProjectileLookRotation(spawnPosition, targetPosition);

            GameObject child = Instantiate(vfx.Prefab, root.transform);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.Euler(vfx.ProjectileRotationEuler);

            return root;
        }

        private static Quaternion GetProjectileLookRotation(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return Quaternion.identity;
            }

            return Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private static void TrySetProjectileLookRotation(Transform movingTransform, Vector3 from, Vector3 to)
        {
            if (movingTransform == null)
            {
                return;
            }

            Vector3 direction = to - from;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            movingTransform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private void ClearTableVfx()
        {
            for (int index = _activeTableVfx.Count - 1; index >= 0; index--)
            {
                GameObject instance = _activeTableVfx[index];
                if (instance != null)
                {
                    Destroy(instance);
                }
            }

            _activeTableVfx.Clear();
        }

        private void ClearPersistentStatusVfx()
        {
            ClearStatusVfx(ref _playerShieldStatusVfx);
            ClearStatusVfx(ref _enemyShieldStatusVfx);
            _playerShieldStatusVfxKey = string.Empty;
            _enemyShieldStatusVfxKey = string.Empty;
        }

        private void SyncShieldStatusVfx(
            ref GameObject instance,
            ref string activeKey,
            bool shouldShow,
            string vfxKey,
            Transform characterRoot,
            RitualPedestal3D pedestal)
        {
            KeyedRitualVfx vfx = shouldShow ? FindVfx(vfxKey) : null;
            if (!shouldShow || vfx?.Prefab == null)
            {
                ClearStatusVfx(ref instance);
                activeKey = string.Empty;
                return;
            }

            if (!vfx.PersistWhileShielded)
            {
                ClearStatusVfx(ref instance);
                activeKey = string.Empty;
                return;
            }

            string normalizedKey = vfx.Key;
            if (instance != null &&
                string.Equals(activeKey, normalizedKey, System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            ClearStatusVfx(ref instance);
            activeKey = normalizedKey;

            Transform characterTarget = ResolveCharacterTarget(characterRoot);
            Transform parent = parentShieldStatusVfxToCharacter && characterTarget != null
                ? characterTarget
                : transform;
            Vector3 position = characterTarget != null
                ? characterTarget.position + shieldStatusVfxOffset
                : GetMagicCirclePosition(pedestal) + shieldStatusVfxOffset;
            Quaternion rotation = parent.rotation * Quaternion.Euler(shieldStatusVfxRotationEuler);

            instance = Instantiate(vfx.Prefab, position, rotation, parent);
            if (parentShieldStatusVfxToCharacter && characterTarget != null)
            {
                instance.transform.localPosition = shieldStatusVfxOffset;
                instance.transform.localRotation = Quaternion.Euler(shieldStatusVfxRotationEuler);
            }
        }

        private static void ClearStatusVfx(ref GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            Destroy(instance);
            instance = null;
        }

        private IEnumerator ClearTableVfxAfterDelay(GameObject instance, float delay)
        {
            float elapsed = 0f;
            float total = Mathf.Max(0f, delay);
            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (instance != null)
            {
                _activeTableVfx.Remove(instance);
                Destroy(instance);
            }
        }

        private static float EaseInOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t < 0.5f
                ? 4f * t * t * t
                : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
        }

        private static IEnumerator WaitForSecondsRealtimeSafe(float duration)
        {
            float elapsed = 0f;
            float total = Mathf.Max(0f, duration);
            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
}
