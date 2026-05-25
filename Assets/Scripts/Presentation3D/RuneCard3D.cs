using System.Collections.Generic;
using TMPro;
using UnityEngine;
using RunicTower.Core;
using RunicTower.Data.Runtime;
using RunicTower.UI;

namespace RunicTower.Presentation3D
{
    public enum BoardCardOwner
    {
        Player,
        Enemy
    }

    public enum BoardCardLocation
    {
        Hand,
        Upcoming,
        Ritual
    }

    public sealed partial class RuneCard3D : MonoBehaviour
    {
        [Header("Body Variants")]
        [SerializeField] private GameObject smallBodyRoot;
        [SerializeField] private GameObject mediumBodyRoot;
        [SerializeField] private GameObject largeBodyRoot;
        [SerializeField] private GameObject modifierBodyRoot;

        [Header("Placement")]
        [SerializeField] private bool followSpotRotation;
        [SerializeField] private Vector3 rotationOffsetEuler;
        [SerializeField] private Transform visualRoot;

        [HideInInspector] [SerializeField] private Vector3 handScale = Vector3.one;
        [HideInInspector] [SerializeField] private Vector3 upcomingScale = new(0.75f, 0.75f, 0.75f);
        [HideInInspector] [SerializeField] private Vector3 ritualScale = new(0.5f, 0.5f, 0.5f);
        [HideInInspector] [SerializeField] private Vector3 modifierRitualScale = new(0.35f, 0.35f, 0.35f);
        [HideInInspector] [SerializeField] private bool playIdleInHand = true;
        [HideInInspector] [SerializeField] private bool playIdleInUpcoming = true;
        [HideInInspector] [SerializeField] private Vector3 idlePositionAmplitude = new(0.08f, 0.05f, 0.10f);
        [HideInInspector] [SerializeField] private Vector3 idleRotationAmplitude = new(4f, 8f, 5f);
        [HideInInspector] [SerializeField] private float idlePositionFrequency = 1.15f;
        [HideInInspector] [SerializeField] private float idleRotationFrequency = 1.45f;
        [HideInInspector] [SerializeField] private Vector3 upcomingIdlePositionAmplitude = new(0.04f, 0.03f, 0.05f);
        [HideInInspector] [SerializeField] private Vector3 upcomingIdleRotationAmplitude = new(2f, 4f, 2f);
        [HideInInspector] [SerializeField] private float upcomingIdlePositionFrequency = 0.9f;
        [HideInInspector] [SerializeField] private float upcomingIdleRotationFrequency = 1.1f;

        [Header("Face Symbols")]
        [SerializeField] private SpriteRenderer[] symbolRenderers;

        [Header("Tint Groups")]
        [SerializeField] private Transform edgeTintRoot;
        [SerializeField] private Renderer[] edgeTintRenderers;
        [SerializeField] [Range(0f, 1f)] private float edgeTintStrength = 1f;
        [SerializeField] private Transform bodyTintRoot;
        [SerializeField] private Renderer[] bodyTintRenderers;
        [SerializeField] [Range(0f, 1f)] private float bodyTintStrength = 0.2f;
        [SerializeField] private string edgeTintChildName = "EdgeLines";
        [SerializeField] private string bodyTintChildName = "StoneBody";

        [Header("Optional UI")]
        [SerializeField] private TMP_Text costLabel;
        [SerializeField] private GameObject selectedHighlight;
        [SerializeField] private Collider interactionCollider;

        private RuneInstance _boundRune;
        private ModifierInstance _boundModifier;
        private Quaternion _baseRotation;
        private Coroutine _idleRoutine;
        private Vector3 _visualBaseLocalPosition;
        private Quaternion _visualBaseLocalRotation;
        private Vector3 _visualBaseLocalScale = Vector3.one;
        private bool _hasCachedVisualBaseline;
        private Vector3 _appliedHandScale;
        private Vector3 _appliedUpcomingScale;
        private Vector3 _appliedRitualScale;
        private Vector3 _appliedModifierRitualScale;
        private bool _appliedPlayIdleInHand;
        private bool _appliedPlayIdleInUpcoming;
        private float _idleSeed;
        private BoxCollider _boxInteractionCollider;
        private SphereCollider _sphereInteractionCollider;
        private CapsuleCollider _capsuleInteractionCollider;
        private Vector3 _baseBoxColliderSize;
        private Vector3 _baseBoxColliderCenter;
        private float _baseSphereColliderRadius;
        private Vector3 _baseSphereColliderCenter;
        private float _baseCapsuleColliderRadius;
        private float _baseCapsuleColliderHeight;
        private Vector3 _baseCapsuleColliderCenter;
        private readonly Dictionary<Renderer, MaterialPropertyBlock> _propertyBlocks = new();
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");

        public RuneInstance BoundRune => _boundRune;
        public ModifierInstance BoundModifier => _boundModifier;
        public bool IsModifier => _boundModifier?.Definition != null;
        public BoardCardOwner Owner { get; private set; }
        public BoardCardLocation Location { get; private set; }

        private void Awake()
        {
            _baseRotation = transform.rotation;
            CacheVisualBaseline();
            _idleSeed = Random.Range(0f, 100f);

            if (interactionCollider == null)
            {
                interactionCollider = GetComponentInChildren<Collider>(true);
            }

            CacheInteractionColliderShape();
            CacheAppliedPresentationSettings();
        }

        private void Update()
        {
            ApplyChangedPresentationSettings();
        }

        private void OnDisable()
        {
            StopIdleAnimation();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                return;
            }

            CacheVisualBaseline();
            ApplyLocationScale();
            UpdateIdleAnimationState();
            CacheAppliedPresentationSettings();
        }

        public void BindRune(RuneInstance rune, BoardCardOwner owner, BoardCardLocation location, bool selected, bool interactable)
        {
            _boundRune = rune;
            _boundModifier = null;
            Owner = owner;
            Location = location;

            if (rune?.Definition == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            SetBodyVariant(rune.Definition.Size, false);
            ApplySymbol(rune.Definition.Icon, rune.Definition.Icon != null);
            ApplyTint(UIRuneVisuals.GetElementColor(rune.Definition.Element));

            if (costLabel != null)
            {
                costLabel.text = rune.Definition.ManaCost.ToString();
            }

            if (selectedHighlight != null)
            {
                selectedHighlight.SetActive(selected);
            }

            if (interactionCollider != null)
            {
                interactionCollider.enabled = interactable;
            }

            ApplyLocationScale();
            UpdateIdleAnimationState();
        }

        public void BindModifier(ModifierInstance modifier, BoardCardOwner owner, BoardCardLocation location, bool selected, bool interactable)
        {
            _boundRune = null;
            _boundModifier = modifier;
            Owner = owner;
            Location = location;

            if (modifier?.Definition == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            SetBodyVariant(RuneSize.Small, true);
            ApplySymbol(modifier.Definition.Icon, modifier.Definition.Icon != null);
            ApplyTint(new Color(0.72f, 0.34f, 0.94f));

            if (costLabel != null)
            {
                costLabel.text = "M";
            }

            if (selectedHighlight != null)
            {
                selectedHighlight.SetActive(selected);
            }

            if (interactionCollider != null)
            {
                interactionCollider.enabled = interactable;
            }

            ApplyLocationScale();
            UpdateIdleAnimationState();
        }

        public void SetPose(Transform target)
        {
            if (target == null)
            {
                return;
            }

            transform.SetPositionAndRotation(target.position, GetPoseRotation(target));
        }

        public Quaternion GetPoseRotation(Transform target)
        {
            Quaternion offsetRotation = Quaternion.Euler(rotationOffsetEuler);
            if (target != null && followSpotRotation)
            {
                return target.rotation * offsetRotation;
            }

            return _baseRotation * offsetRotation;
        }

        public void SetInteractable(bool interactable)
        {
            if (interactionCollider != null)
            {
                interactionCollider.enabled = interactable;
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectedHighlight != null)
            {
                selectedHighlight.SetActive(selected);
            }
        }

        public void SetIdleEnabled(bool enabled)
        {
            if (enabled)
            {
                UpdateIdleAnimationState();
                return;
            }

            StopIdleAnimation();
        }

        public void SetLocation(BoardCardLocation location)
        {
            Location = location;
            ApplyLocationScale();
            UpdateIdleAnimationState();
        }

        public void ConfigurePresentation(
            Vector3 handScaleMultiplier,
            Vector3 upcomingScaleMultiplier,
            Vector3 ritualScaleMultiplier,
            Vector3 modifierRitualScaleMultiplier,
            bool idleInHand,
            bool idleInUpcoming,
            Vector3 idlePosition,
            Vector3 idleRotation,
            float idlePositionSpeed,
            float idleRotationSpeed,
            Vector3 upcomingIdlePosition,
            Vector3 upcomingIdleRotation,
            float upcomingIdlePositionSpeed,
            float upcomingIdleRotationSpeed)
        {
            handScale = handScaleMultiplier;
            upcomingScale = upcomingScaleMultiplier;
            ritualScale = ritualScaleMultiplier;
            modifierRitualScale = modifierRitualScaleMultiplier;
            playIdleInHand = idleInHand;
            playIdleInUpcoming = idleInUpcoming;
            idlePositionAmplitude = idlePosition;
            idleRotationAmplitude = idleRotation;
            idlePositionFrequency = idlePositionSpeed;
            idleRotationFrequency = idleRotationSpeed;
            upcomingIdlePositionAmplitude = upcomingIdlePosition;
            upcomingIdleRotationAmplitude = upcomingIdleRotation;
            upcomingIdlePositionFrequency = upcomingIdlePositionSpeed;
            upcomingIdleRotationFrequency = upcomingIdleRotationSpeed;

            ApplyLocationScale();
            UpdateIdleAnimationState();
            CacheAppliedPresentationSettings();
        }

        public Vector3 GetLocationScale(BoardCardLocation location)
        {
            Vector3 scaleMultiplier;
            if (location == BoardCardLocation.Ritual)
            {
                scaleMultiplier = IsModifier ? modifierRitualScale : ritualScale;
            }
            else if (location == BoardCardLocation.Upcoming)
            {
                scaleMultiplier = upcomingScale;
            }
            else
            {
                scaleMultiplier = handScale;
            }

            return Vector3.Scale(GetVisualBaseLocalScale(), scaleMultiplier);
        }

        public void SetVisualScale(Vector3 scale)
        {
            Transform scaleTarget = GetIdleTarget();
            if (scaleTarget != null)
            {
                scaleTarget.localScale = scale;
            }

            SyncInteractionColliderScale(scale);
        }

        private void SetBodyVariant(RuneSize size, bool isModifier)
        {
            SetActiveSafe(smallBodyRoot, !isModifier && size == RuneSize.Small);
            SetActiveSafe(mediumBodyRoot, !isModifier && size == RuneSize.Medium);
            SetActiveSafe(largeBodyRoot, !isModifier && size == RuneSize.Large);
            SetActiveSafe(modifierBodyRoot, isModifier);
        }

        private void ApplySymbol(Sprite symbolSprite, bool enabled)
        {
            if (symbolRenderers == null)
            {
                return;
            }

            foreach (SpriteRenderer renderer in symbolRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.sprite = symbolSprite;
                renderer.enabled = enabled;
                renderer.color = Color.white;
            }
        }

        private void ApplyTint(Color tintColor)
        {
            ApplyTintToRenderers(GetEdgeTintRenderers(), Color.Lerp(Color.white, tintColor, edgeTintStrength));
            ApplyTintToRenderers(GetBodyTintRenderers(), Color.Lerp(Color.white, tintColor, bodyTintStrength));
        }

        private void ApplyTintToRenderers(Renderer[] renderers, Color tintColor)
        {
            if (renderers == null)
            {
                return;
            }

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                Material sharedMaterial = renderer.sharedMaterial;
                if (sharedMaterial == null)
                {
                    continue;
                }

                if (!_propertyBlocks.TryGetValue(renderer, out MaterialPropertyBlock block))
                {
                    block = new MaterialPropertyBlock();
                    _propertyBlocks.Add(renderer, block);
                }

                renderer.GetPropertyBlock(block);
                if (sharedMaterial.HasProperty(BaseColorPropertyId))
                {
                    block.SetColor(BaseColorPropertyId, tintColor);
                }
                else if (sharedMaterial.HasProperty(ColorPropertyId))
                {
                    block.SetColor(ColorPropertyId, tintColor);
                }
                else
                {
                    continue;
                }

                renderer.SetPropertyBlock(block);
            }
        }

        private void UpdateIdleAnimationState()
        {
            if (playIdleInHand && Location == BoardCardLocation.Hand)
            {
                StartIdleAnimation();
                return;
            }

            if (playIdleInUpcoming && Location == BoardCardLocation.Upcoming)
            {
                StartIdleAnimation();
                return;
            }

            StopIdleAnimation();
        }

        private void ApplyLocationScale()
        {
            SetVisualScale(GetLocationScale(Location));
        }

        private void ApplyChangedPresentationSettings()
        {
            if (_appliedHandScale == handScale &&
                _appliedUpcomingScale == upcomingScale &&
                _appliedRitualScale == ritualScale &&
                _appliedModifierRitualScale == modifierRitualScale &&
                _appliedPlayIdleInHand == playIdleInHand &&
                _appliedPlayIdleInUpcoming == playIdleInUpcoming)
            {
                return;
            }

            ApplyLocationScale();
            UpdateIdleAnimationState();
            CacheAppliedPresentationSettings();
        }

        private void CacheAppliedPresentationSettings()
        {
            _appliedHandScale = handScale;
            _appliedUpcomingScale = upcomingScale;
            _appliedRitualScale = ritualScale;
            _appliedModifierRitualScale = modifierRitualScale;
            _appliedPlayIdleInHand = playIdleInHand;
            _appliedPlayIdleInUpcoming = playIdleInUpcoming;
        }

        private Vector3 GetCurrentIdlePositionAmplitude()
        {
            return Location == BoardCardLocation.Upcoming
                ? upcomingIdlePositionAmplitude
                : idlePositionAmplitude;
        }

        private Vector3 GetCurrentIdleRotationAmplitude()
        {
            return Location == BoardCardLocation.Upcoming
                ? upcomingIdleRotationAmplitude
                : idleRotationAmplitude;
        }

        private float GetCurrentIdlePositionFrequency()
        {
            return Location == BoardCardLocation.Upcoming
                ? upcomingIdlePositionFrequency
                : idlePositionFrequency;
        }

        private float GetCurrentIdleRotationFrequency()
        {
            return Location == BoardCardLocation.Upcoming
                ? upcomingIdleRotationFrequency
                : idleRotationFrequency;
        }

        private void CacheVisualBaseline()
        {
            if (_hasCachedVisualBaseline)
            {
                return;
            }

            Transform idleTarget = GetIdleTarget();
            if (idleTarget == null)
            {
                return;
            }

            _visualBaseLocalPosition = idleTarget.localPosition;
            _visualBaseLocalRotation = idleTarget.localRotation;
            _visualBaseLocalScale = idleTarget.localScale;
            _hasCachedVisualBaseline = true;
        }

        private Vector3 GetVisualBaseLocalScale()
        {
            CacheVisualBaseline();
            return _visualBaseLocalScale;
        }

        private Vector3 GetHandVisualScale()
        {
            return Vector3.Scale(GetVisualBaseLocalScale(), handScale);
        }

        private static void SetActiveSafe(GameObject target, bool value)
        {
            if (target != null)
            {
                target.SetActive(value);
            }
        }

        private static float SafeDivide(float value, float divisor)
        {
            return Mathf.Abs(divisor) <= 0.0001f ? 1f : value / divisor;
        }
    }
}



