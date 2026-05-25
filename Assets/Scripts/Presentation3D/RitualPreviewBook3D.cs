using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using RunicTower.Audio;

namespace RunicTower.Presentation3D
{
    public sealed class RitualPreviewBook3D : MonoBehaviour
    {
        public event System.Action StateChanged;

        [Header("Book Transforms")]
        [SerializeField] private Transform bookRoot;
        [SerializeField] private Transform focusPoint;
        [SerializeField] private Collider interactionCollider;
        [SerializeField] private bool keepFocusRotationWhileOpen = true;
        [SerializeField] private bool keepFocusPositionWhileOpen = true;

        [Header("Page Content")]
        [SerializeField] private GameObject pageContentRoot;
        [SerializeField] private TMP_Text runeLineLabel;
        [SerializeField] private TMP_Text manaCostLabel;
        [SerializeField] private TMP_Text successRateLabel;
        [SerializeField] private TMP_Text effectLabel;
        [SerializeField] private TMP_Text ritualNameLabel;

        [Header("Audio")]
        [SerializeField] private BattleAudioController audioController;

        [Header("Flight")]
        [SerializeField] private float moveDuration = 0.65f;
        [SerializeField] private float flightArcHeight = 0.35f;
        [SerializeField] private Vector3 openedScaleMultiplier = new(2f, 2f, 2f);

        [Header("Animator")]
        [SerializeField] private Animator animator;
        [SerializeField] private int animationLayer;
        [SerializeField] private string openStateName = "Open";
        [SerializeField] private string flipStateName = "Flip";
        [SerializeField] private string closeStateName = "Close";
        [SerializeField] private float openAnimationDuration = 0.55f;
        [SerializeField] private float flipAnimationDuration = 0.55f;
        [SerializeField] private float closeAnimationDuration = 0.45f;
        [SerializeField] private float closeReturnDelay = 0.1f;
        [SerializeField] private float animatorCrossFade = 0.05f;
        [SerializeField] private bool forceClosedStateOnStart = true;
        [SerializeField] private bool disableAnimatorDuringFlight = true;

        private Vector3 _closedPosition;
        private Quaternion _closedRotation;
        private Vector3 _closedScale = Vector3.one;
        private Coroutine _toggleRoutine;
        private bool _isOpening;
        private bool _isClosing;
        private bool _lockToFocusPose;
        private readonly Dictionary<string, float> _clipLengthByName = new();

        public bool IsOpen { get; private set; }
        public bool IsAnimating => _toggleRoutine != null;
        public bool BlocksBattleInteraction => IsOpen || IsAnimating;

        private Transform BookTransform => bookRoot != null ? bookRoot : transform;

        private void Awake()
        {
            if (audioController == null)
            {
                audioController = ResolveSharedController<BattleAudioController>();
            }

            if (interactionCollider == null)
            {
                interactionCollider = GetComponentInChildren<Collider>(true);
            }

            CacheAnimationClipLengths();
            CacheClosedPose();
            ForceClosedVisualState();
            SetAnimatorPlaybackEnabled(false);
            SetPageContentVisible(false);
        }

        private void LateUpdate()
        {
            if (!keepFocusRotationWhileOpen && !keepFocusPositionWhileOpen)
            {
                return;
            }

            Transform target = BookTransform;
            if (target == null || focusPoint == null)
            {
                return;
            }

            if (!IsOpen && !_isOpening && !_isClosing)
            {
                return;
            }

            if (!_lockToFocusPose)
            {
                return;
            }

            if (keepFocusPositionWhileOpen)
            {
                target.position = focusPoint.position;
            }

            if (keepFocusRotationWhileOpen)
            {
                target.rotation = focusPoint.rotation;
            }
        }

        public void SetPreviewContent(
            string runeLine,
            string manaCost,
            string successRate,
            string ritualName,
            string effectSummary)
        {
            SetText(runeLineLabel, runeLine);
            SetText(manaCostLabel, manaCost);
            SetText(successRateLabel, successRate);
            SetText(ritualNameLabel, ritualName);
            SetText(effectLabel, effectSummary);
        }

        public void Toggle()
        {
            if (IsAnimating)
            {
                return;
            }

            NotifyStateChanged();
            _toggleRoutine = StartCoroutine(IsOpen ? CloseRoutine() : OpenRoutine());
        }

        public bool MatchesCollider(Collider hitCollider)
        {
            if (hitCollider == null)
            {
                return false;
            }

            Transform root = BookTransform != null ? BookTransform : transform;
            return hitCollider == interactionCollider ||
                   hitCollider.transform == root ||
                   hitCollider.transform.IsChildOf(root);
        }

        private IEnumerator OpenRoutine()
        {
            Transform target = BookTransform;
            if (target == null)
            {
                _toggleRoutine = null;
                yield break;
            }

            _isOpening = true;
            _lockToFocusPose = false;
            CacheClosedPose();
            SetInteractionEnabled(false);
            SetPageContentVisible(false);
            SetAnimatorPlaybackEnabled(false);

            Vector3 openedScale = Vector3.Scale(_closedScale, openedScaleMultiplier);
            audioController?.PlayBookFlight(target.position);
            yield return PlayFlight(
                target,
                _closedPosition,
                focusPoint != null ? focusPoint.position : _closedPosition,
                _closedRotation,
                focusPoint != null ? focusPoint.rotation : _closedRotation,
                _closedScale,
                openedScale);

            _lockToFocusPose = true;
            SetAnimatorPlaybackEnabled(true);
            audioController?.PlayBookOpen(target.position);
            PlayAnimationState(openStateName, openAnimationDuration);
            yield return WaitForSecondsRealtimeSafe(openAnimationDuration);

            audioController?.PlayBookFlip(target.position);
            PlayAnimationState(flipStateName, flipAnimationDuration);
            yield return WaitForSecondsRealtimeSafe(flipAnimationDuration);

            SetPageContentVisible(true);
            IsOpen = true;
            _isOpening = false;
            SetInteractionEnabled(true);
            _toggleRoutine = null;
            NotifyStateChanged();
        }

        private IEnumerator CloseRoutine()
        {
            Transform target = BookTransform;
            if (target == null)
            {
                _toggleRoutine = null;
                yield break;
            }

            _isClosing = true;
            SetInteractionEnabled(false);
            SetPageContentVisible(false);
            SetAnimatorPlaybackEnabled(true);
            audioController?.PlayBookClose(target.position);
            PlayAnimationState(closeStateName, closeAnimationDuration);
            yield return WaitForSecondsRealtimeSafe(closeAnimationDuration);
            yield return WaitForSecondsRealtimeSafe(closeReturnDelay);

            _lockToFocusPose = false;
            SetAnimatorPlaybackEnabled(false);
            audioController?.PlayBookFlight(target.position);
            yield return PlayFlight(
                target,
                target.position,
                _closedPosition,
                target.rotation,
                _closedRotation,
                target.localScale,
                _closedScale);

            IsOpen = false;
            _isClosing = false;
            SetAnimatorPlaybackEnabled(false);
            SetInteractionEnabled(true);
            _toggleRoutine = null;
            NotifyStateChanged();
        }

        private void CacheClosedPose()
        {
            Transform target = BookTransform;
            if (target == null)
            {
                return;
            }

            _closedPosition = target.position;
            _closedRotation = target.rotation;
            _closedScale = target.localScale;
        }

        private void PlayAnimationState(string stateName, float desiredDuration)
        {
            if (animator == null || string.IsNullOrWhiteSpace(stateName))
            {
                return;
            }

            animator.speed = GetAnimatorSpeedForState(stateName, desiredDuration);
            animator.CrossFadeInFixedTime(stateName, animatorCrossFade, animationLayer, 0f);
        }

        private void ForceClosedVisualState()
        {
            if (!forceClosedStateOnStart || animator == null || string.IsNullOrWhiteSpace(closeStateName))
            {
                return;
            }

            Transform target = BookTransform;
            Vector3 originalPosition = Vector3.zero;
            Quaternion originalRotation = Quaternion.identity;
            Vector3 originalScale = Vector3.one;

            if (target != null)
            {
                originalPosition = target.position;
                originalRotation = target.rotation;
                originalScale = target.localScale;
            }

            animator.Play(closeStateName, animationLayer, 1f);
            animator.Update(0f);

            if (target != null)
            {
                target.SetPositionAndRotation(originalPosition, originalRotation);
                target.localScale = originalScale;
            }
        }

        private void SetAnimatorPlaybackEnabled(bool enabled)
        {
            if (!disableAnimatorDuringFlight || animator == null)
            {
                return;
            }

            animator.enabled = enabled;
        }

        private void CacheAnimationClipLengths()
        {
            _clipLengthByName.Clear();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip == null || string.IsNullOrWhiteSpace(clip.name))
                {
                    continue;
                }

                _clipLengthByName[clip.name] = clip.length;
            }
        }

        private float GetAnimatorSpeedForState(string stateName, float desiredDuration)
        {
            if (desiredDuration <= 0.01f)
            {
                return 1f;
            }

            if (!_clipLengthByName.TryGetValue(stateName, out float clipLength) || clipLength <= 0.01f)
            {
                return 1f;
            }

            return Mathf.Max(0.01f, clipLength / desiredDuration);
        }

        private void SetInteractionEnabled(bool enabled)
        {
            if (interactionCollider != null)
            {
                interactionCollider.enabled = enabled;
            }
        }

        private void SetPageContentVisible(bool visible)
        {
            if (pageContentRoot != null)
            {
                pageContentRoot.SetActive(visible);
            }
        }

        private IEnumerator PlayFlight(
            Transform target,
            Vector3 startPosition,
            Vector3 endPosition,
            Quaternion startRotation,
            Quaternion endRotation,
            Vector3 startScale,
            Vector3 endScale)
        {
            if (target == null)
            {
                yield break;
            }

            float duration = Mathf.Max(0.01f, moveDuration);
            float elapsed = 0f;
            Vector3 peakStart = startPosition + Vector3.up * flightArcHeight;
            Vector3 peakEnd = endPosition + Vector3.up * flightArcHeight;

            while (elapsed < duration)
            {
                if (target == null)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutCubic(t);
                Vector3 position;

                if (eased < 0.25f)
                {
                    position = Vector3.Lerp(startPosition, peakStart, eased / 0.25f);
                }
                else if (eased < 0.75f)
                {
                    position = Vector3.Lerp(peakStart, peakEnd, (eased - 0.25f) / 0.5f);
                }
                else
                {
                    position = Vector3.Lerp(peakEnd, endPosition, (eased - 0.75f) / 0.25f);
                }

                target.position = position;
                target.rotation = Quaternion.Slerp(startRotation, endRotation, eased);
                target.localScale = Vector3.Lerp(startScale, endScale, eased);
                yield return null;
            }

            if (target == null)
            {
                yield break;
            }

            target.SetPositionAndRotation(endPosition, endRotation);
            target.localScale = endScale;
        }

        private static float EaseOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label != null)
            {
                label.text = value;
            }
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

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke();
        }
    }
}
