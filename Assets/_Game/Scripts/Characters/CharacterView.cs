using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GenericGachaRPG
{
    /// <summary>
    /// The presentation states understood by <see cref="CharacterView"/>.
    /// Gameplay code remains responsible for deciding when a state is played.
    /// </summary>
    public enum CharacterViewState
    {
        Idle,
        Attacking,
        CastingSkill,
        Hit,
        Dead
    }

    /// <summary>
    /// A model-agnostic presentation facade for battle characters. It exposes a
    /// stable socket layout and works both with authored Animators and with a
    /// procedural animation fallback.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Header("Sockets")]
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Transform rightHandSocket;
        [SerializeField] private Transform leftHandSocket;
        [SerializeField] private Transform skillVfxSocket;
        [SerializeField] private Transform projectileSocket;
        [SerializeField] private Transform groundVfxSocket;
        [SerializeField] private Transform targetSocket;
        [SerializeField] private Transform healthBarSocket;

        [Header("Optional Animator")]
        [SerializeField] private Animator animator;
        [SerializeField] private string idleTrigger = "Idle";
        [SerializeField] private string attackTrigger = "Attack";
        [SerializeField] private string skillTrigger = "Skill";
        [SerializeField] private string hitTrigger = "Hit";
        [SerializeField] private string deathTrigger = "Death";

        [Header("Presentation Timing")]
        [Min(0.05f)] [SerializeField] private float attackDuration = 0.46f;
        [Range(0.05f, 0.95f)] [SerializeField] private float attackImpactTime = 0.55f;
        [Min(0.1f)] [SerializeField] private float skillDuration = 0.86f;
        [Range(0.05f, 0.95f)] [SerializeField] private float skillImpactTime = 0.62f;
        [Min(0.05f)] [SerializeField] private float hitDuration = 0.24f;
        [Min(0.1f)] [SerializeField] private float deathDuration = 0.65f;
        [Min(0f)] [SerializeField] private float lungeDistance = 0.48f;

        [Header("Fallback Idle")]
        [Min(0f)] [SerializeField] private float breathingHeight = 0.035f;
        [Min(0.1f)] [SerializeField] private float breathingSpeed = 2.2f;

        private readonly List<RendererColorInfo> rendererColors = new List<RendererColorInfo>();
        private Coroutine activeAction;
        private Coroutine idleRoutine;
        private Coroutine movementRoutine;
        private Vector3 restLocalPosition;
        private Quaternion restLocalRotation;
        private Vector3 restLocalScale = Vector3.one;
        private Vector3 spawnWorldPosition;
        private bool restPoseCaptured;
        private bool spawnPositionCaptured;
        private bool isDead;

        public Transform ModelRoot => modelRoot != null ? modelRoot : transform;
        public Transform RightHand => rightHandSocket;
        public Transform LeftHand => leftHandSocket;
        public Transform SkillVfx => skillVfxSocket;
        public Transform Projectile => projectileSocket;
        public Transform GroundVfx => groundVfxSocket;
        public Transform Target => targetSocket;
        public Transform HealthBar => healthBarSocket;
        public Animator Animator => animator;
        public CharacterViewState State { get; private set; } = CharacterViewState.Idle;
        public bool IsDead => isDead;
        public bool UsesAnimator => animator != null && animator.runtimeAnimatorController != null;
        public bool HasPerformedApproach { get; private set; }
        public float MaximumRootTravelDistance { get; private set; }

        /// <summary>Raised when the visible state changes. It never changes battle state.</summary>
        public event Action<CharacterView, CharacterViewState> StateChanged;

        /// <summary>Raised at the authored impact beat of an attack or skill.</summary>
        public event Action<CharacterView> ActionImpact;

        private void Awake()
        {
            ResolveSocketsByName();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            CaptureSpawnPosition();
            CaptureRestPose();
            RefreshRenderers();
        }

        private void OnEnable()
        {
            if (!isDead)
            {
                BeginIdle();
            }
        }

        private void OnDisable()
        {
            StopAllPresentationCoroutines(false);
            StopRootMovement(false);
        }

        /// <summary>
        /// Assigns the standardized sockets. Procedural builders and future model
        /// importers can both use this method without requiring prefab-specific code.
        /// </summary>
        public void ConfigureSockets(
            Transform newModelRoot,
            Transform newRightHand,
            Transform newLeftHand,
            Transform newSkillVfx,
            Transform newProjectile,
            Transform newGroundVfx,
            Transform newTarget,
            Transform newHealthBar,
            Animator newAnimator = null)
        {
            StopAllPresentationCoroutines(false);
            modelRoot = newModelRoot != null ? newModelRoot : transform;
            rightHandSocket = newRightHand;
            leftHandSocket = newLeftHand;
            skillVfxSocket = newSkillVfx;
            projectileSocket = newProjectile;
            groundVfxSocket = newGroundVfx;
            targetSocket = newTarget;
            healthBarSocket = newHealthBar;
            animator = newAnimator != null ? newAnimator : GetComponentInChildren<Animator>();
            CaptureRestPose();
            RefreshRenderers();

            if (isActiveAndEnabled && !isDead)
            {
                BeginIdle();
            }
        }

        /// <summary>Attempts to wire any missing sockets from their canonical names.</summary>
        public void ResolveSocketsByName()
        {
            modelRoot = modelRoot != null ? modelRoot : FindDescendantByName(transform, "ModelRoot");
            rightHandSocket = rightHandSocket != null ? rightHandSocket : FindDescendantByNames(transform, "RightHand", "RightHandSocket");
            leftHandSocket = leftHandSocket != null ? leftHandSocket : FindDescendantByNames(transform, "LeftHand", "LeftHandSocket");
            skillVfxSocket = skillVfxSocket != null ? skillVfxSocket : FindDescendantByNames(transform, "SkillVfx", "SkillVfxSocket");
            projectileSocket = projectileSocket != null ? projectileSocket : FindDescendantByNames(transform, "Projectile", "ProjectileSocket");
            groundVfxSocket = groundVfxSocket != null ? groundVfxSocket : FindDescendantByNames(transform, "GroundVfx", "GroundVfxSocket");
            targetSocket = targetSocket != null ? targetSocket : FindDescendantByNames(transform, "Target", "TargetSocket");
            healthBarSocket = healthBarSocket != null ? healthBarSocket : FindDescendantByNames(transform, "HealthBar", "HealthBarSocket");
        }

        public void PlayIdle()
        {
            if (isDead)
            {
                return;
            }

            StopAllPresentationCoroutines(true);
            BeginIdle();
        }

        public Coroutine PlayAttack(Action onImpact = null)
        {
            return PlayAttack(transform.position + transform.forward * 2f, onImpact);
        }

        public Coroutine PlayAttack(Transform target, Action onImpact = null)
        {
            Vector3 targetPosition = target != null
                ? target.position
                : transform.position + transform.forward * 2f;
            return PlayAttack(targetPosition, onImpact);
        }

        public Coroutine PlayAttack(Vector3 targetWorldPosition, Action onImpact = null)
        {
            if (isDead || !isActiveAndEnabled)
            {
                return null;
            }

            StopAllPresentationCoroutines(true);
            activeAction = StartCoroutine(HasAnimatorTrigger(attackTrigger)
                ? PlayAnimatorAction(CharacterViewState.Attacking, attackTrigger, attackDuration, attackImpactTime, onImpact, true)
                : PlayFallbackAttack(targetWorldPosition, onImpact));
            return activeAction;
        }

        /// <summary>
        /// Plays a basic attack whose authored impact is synchronized with the
        /// deterministic simulation hit. World movement is driven separately by
        /// UnitMoved events and is never rolled back by an attack.
        /// </summary>
        public Coroutine PlayBasicAttack(
            Vector3 targetWorldPosition,
            float impactDelay,
            Action onImpact = null)
        {
            if (isDead || !isActiveAndEnabled)
            {
                return null;
            }

            StopAllPresentationCoroutines(true);
            activeAction = StartCoroutine(PlayTimedBasicAttack(
                targetWorldPosition,
                Mathf.Max(0.05f, impactDelay),
                onImpact));
            return activeAction;
        }

        public Coroutine PlayBasicAttack(
            Transform target,
            float impactDelay,
            Action onImpact = null)
        {
            Vector3 targetWorldPosition = target != null
                ? target.position
                : transform.position + transform.forward * 2f;
            if (isDead || !isActiveAndEnabled)
            {
                return null;
            }

            StopAllPresentationCoroutines(true);
            activeAction = StartCoroutine(PlayTimedBasicAttack(
                targetWorldPosition,
                Mathf.Max(0.05f, impactDelay),
                onImpact));
            return activeAction;
        }

        /// <summary>Moves the world root to a deterministic position and stays there.</summary>
        public Coroutine MoveRootTo(Vector3 destinationWorldPosition, float duration)
        {
            if (isDead || !isActiveAndEnabled)
            {
                return null;
            }

            StopRootMovement(false);
            destinationWorldPosition.y = transform.position.y;
            FaceTarget(destinationWorldPosition);
            movementRoutine = StartCoroutine(MoveRootPersistent(
                destinationWorldPosition,
                Mathf.Max(0f, duration)));
            return movementRoutine;
        }

        public Coroutine PlaySkill(Action onImpact = null)
        {
            return PlaySkill(transform.position + transform.forward * 2f, onImpact);
        }

        public Coroutine PlaySkill(Transform target, Action onImpact = null)
        {
            Vector3 targetPosition = target != null
                ? target.position
                : transform.position + transform.forward * 2f;
            return PlaySkill(targetPosition, onImpact);
        }

        public Coroutine PlaySkill(Vector3 targetWorldPosition, Action onImpact = null)
        {
            if (isDead || !isActiveAndEnabled)
            {
                return null;
            }

            StopAllPresentationCoroutines(true);
            activeAction = StartCoroutine(HasAnimatorTrigger(skillTrigger)
                ? PlayAnimatorAction(CharacterViewState.CastingSkill, skillTrigger, skillDuration, skillImpactTime, onImpact, true)
                : PlayFallbackSkill(targetWorldPosition, onImpact));
            return activeAction;
        }

        public Coroutine PlayHit()
        {
            return PlayHit(transform.position + transform.forward);
        }

        public Coroutine PlayHit(Vector3 sourceWorldPosition)
        {
            if (isDead || !isActiveAndEnabled)
            {
                return null;
            }

            StopAllPresentationCoroutines(true);
            activeAction = StartCoroutine(HasAnimatorTrigger(hitTrigger)
                ? PlayAnimatorHit()
                : PlayFallbackHit(sourceWorldPosition));
            return activeAction;
        }

        public Coroutine PlayDeath()
        {
            if (!isActiveAndEnabled)
            {
                isDead = true;
                SetState(CharacterViewState.Dead);
                return null;
            }

            StopAllPresentationCoroutines(true);
            StopRootMovement(false);
            isDead = true;
            activeAction = StartCoroutine(HasAnimatorTrigger(deathTrigger) ? PlayAnimatorDeath() : PlayFallbackDeath());
            return activeAction;
        }

        /// <summary>Restores the model for pooling or a new battle.</summary>
        public void ResetView(bool playIdle = true)
        {
            StopAllPresentationCoroutines(true);
            StopRootMovement(true);
            isDead = false;
            HasPerformedApproach = false;
            MaximumRootTravelDistance = 0f;
            RestoreRendererColors();
            SetState(CharacterViewState.Idle);

            if (playIdle && isActiveAndEnabled)
            {
                BeginIdle();
            }
        }

        /// <summary>Rotates only around the vertical axis, preserving the battle layout.</summary>
        public void FaceTarget(Vector3 targetWorldPosition)
        {
            Vector3 direction = targetWorldPosition - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }

        public void RefreshRenderers()
        {
            rendererColors.Clear();
            Renderer[] renderers = ModelRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer item in renderers)
            {
                Material material = item.sharedMaterial;
                if (material == null)
                {
                    continue;
                }

                int propertyId;
                if (material.HasProperty(BaseColorId))
                {
                    propertyId = BaseColorId;
                }
                else if (material.HasProperty(ColorId))
                {
                    propertyId = ColorId;
                }
                else
                {
                    continue;
                }

                rendererColors.Add(new RendererColorInfo(item, propertyId, material.GetColor(propertyId)));
            }
        }

        private IEnumerator PlayAnimatorAction(
            CharacterViewState actionState,
            string triggerName,
            float duration,
            float normalizedImpactTime,
            Action onImpact,
            bool returnToIdle)
        {
            SetState(actionState);
            SetAnimatorTrigger(triggerName);

            float impactDelay = duration * normalizedImpactTime;
            yield return WaitForDuration(impactDelay);
            InvokeImpact(onImpact);
            yield return WaitForDuration(Mathf.Max(0f, duration - impactDelay));

            activeAction = null;
            if (returnToIdle && !isDead)
            {
                BeginIdle();
            }
        }

        private IEnumerator PlayAnimatorHit()
        {
            SetState(CharacterViewState.Hit);
            SetAnimatorTrigger(hitTrigger);
            SetRendererFlash(Color.white);
            yield return WaitForDuration(hitDuration * 0.55f);
            RestoreRendererColors();
            yield return WaitForDuration(hitDuration * 0.45f);
            activeAction = null;
            if (!isDead)
            {
                BeginIdle();
            }
        }

        private IEnumerator PlayAnimatorDeath()
        {
            SetState(CharacterViewState.Dead);
            SetAnimatorTrigger(deathTrigger);
            yield return WaitForDuration(deathDuration);
            activeAction = null;
        }

        private IEnumerator PlayFallbackAttack(Vector3 targetWorldPosition, Action onImpact)
        {
            SetState(CharacterViewState.Attacking);
            Vector3 direction = GetLocalHorizontalDirection(targetWorldPosition, Vector3.forward);
            Vector3 startPosition = restLocalPosition;
            Vector3 anticipationPosition = startPosition - direction * (lungeDistance * 0.16f);
            Vector3 strikePosition = startPosition + direction * lungeDistance;

            yield return InterpolatePose(
                0.12f,
                startPosition,
                anticipationPosition,
                restLocalRotation,
                restLocalRotation * Quaternion.Euler(-8f, 0f, 0f),
                restLocalScale,
                Vector3.Scale(restLocalScale, new Vector3(1.08f, 0.91f, 1.08f)));

            yield return InterpolatePose(
                0.12f,
                anticipationPosition,
                strikePosition,
                restLocalRotation * Quaternion.Euler(-8f, 0f, 0f),
                restLocalRotation * Quaternion.Euler(13f, 0f, 0f),
                Vector3.Scale(restLocalScale, new Vector3(1.08f, 0.91f, 1.08f)),
                Vector3.Scale(restLocalScale, new Vector3(0.94f, 1.11f, 0.94f)));

            InvokeImpact(onImpact);
            yield return InterpolatePose(
                0.22f,
                strikePosition,
                restLocalPosition,
                restLocalRotation * Quaternion.Euler(13f, 0f, 0f),
                restLocalRotation,
                Vector3.Scale(restLocalScale, new Vector3(0.94f, 1.11f, 0.94f)),
                restLocalScale);

            RestoreRestPose();
            activeAction = null;
            if (!isDead)
            {
                BeginIdle();
            }
        }

        private IEnumerator PlayTimedBasicAttack(
            Vector3 targetWorldPosition,
            float impactDelay,
            Action onImpact)
        {
            SetState(CharacterViewState.Attacking);
            bool useAnimator = HasAnimatorTrigger(attackTrigger);
            if (useAnimator)
            {
                SetAnimatorTrigger(attackTrigger);
            }

            bool animateFallbackPose = !useAnimator && ModelRoot != transform;
            Vector3 localDirection = GetLocalHorizontalDirection(targetWorldPosition, Vector3.forward);
            Vector3 anticipationPosition = restLocalPosition - localDirection * (lungeDistance * 0.12f);
            Vector3 strikePosition = restLocalPosition + localDirection * lungeDistance;
            Quaternion anticipationRotation = restLocalRotation * Quaternion.Euler(-8f, 0f, 0f);
            Quaternion strikeRotation = restLocalRotation * Quaternion.Euler(13f, 0f, 0f);
            Vector3 anticipationScale = Vector3.Scale(restLocalScale, new Vector3(1.08f, 0.91f, 1.08f));
            Vector3 strikeScale = Vector3.Scale(restLocalScale, new Vector3(0.94f, 1.11f, 0.94f));

            float elapsed = 0f;
            while (elapsed < impactDelay)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / impactDelay);
                if (animateFallbackPose)
                {
                    if (t < 0.58f)
                    {
                        float poseT = Mathf.SmoothStep(0f, 1f, t / 0.58f);
                        SetModelPose(
                            Vector3.LerpUnclamped(restLocalPosition, anticipationPosition, poseT),
                            Quaternion.SlerpUnclamped(restLocalRotation, anticipationRotation, poseT),
                            Vector3.LerpUnclamped(restLocalScale, anticipationScale, poseT));
                    }
                    else
                    {
                        float poseT = Mathf.SmoothStep(0f, 1f, (t - 0.58f) / 0.42f);
                        SetModelPose(
                            Vector3.LerpUnclamped(anticipationPosition, strikePosition, poseT),
                            Quaternion.SlerpUnclamped(anticipationRotation, strikeRotation, poseT),
                            Vector3.LerpUnclamped(anticipationScale, strikeScale, poseT));
                    }
                }

                yield return null;
            }

            if (animateFallbackPose)
            {
                SetModelPose(strikePosition, strikeRotation, strikeScale);
            }

            InvokeImpact(onImpact);

            const float recoveryDuration = 0.2f;
            elapsed = 0f;
            while (elapsed < recoveryDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / recoveryDuration));
                if (animateFallbackPose)
                {
                    SetModelPose(
                        Vector3.LerpUnclamped(strikePosition, restLocalPosition, t),
                        Quaternion.SlerpUnclamped(strikeRotation, restLocalRotation, t),
                        Vector3.LerpUnclamped(strikeScale, restLocalScale, t));
                }

                yield return null;
            }

            RestoreRestPose();
            activeAction = null;
            if (!isDead)
            {
                BeginIdle();
            }
        }

        private IEnumerator PlayFallbackSkill(Vector3 targetWorldPosition, Action onImpact)
        {
            SetState(CharacterViewState.CastingSkill);
            Vector3 direction = GetLocalHorizontalDirection(targetWorldPosition, Vector3.forward);
            Vector3 crouchPosition = restLocalPosition - Vector3.up * 0.11f - direction * 0.08f;
            Vector3 crouchScale = Vector3.Scale(restLocalScale, new Vector3(1.18f, 0.78f, 1.18f));

            SetRendererFlash(new Color(1f, 0.94f, 0.45f, 1f));
            yield return InterpolatePose(
                0.22f,
                restLocalPosition,
                crouchPosition,
                restLocalRotation,
                restLocalRotation * Quaternion.Euler(-16f, 0f, 0f),
                restLocalScale,
                crouchScale);

            float elapsed = 0f;
            float burstDuration = 0.42f;
            bool impactInvoked = false;
            Vector3 burstScale = Vector3.Scale(restLocalScale, new Vector3(0.88f, 1.26f, 0.88f));
            while (elapsed < burstDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / burstDuration);
                float eased = EaseOutBack(t);
                ModelRoot.localPosition = Vector3.LerpUnclamped(crouchPosition, restLocalPosition + direction * 0.28f, eased)
                    + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.28f;
                ModelRoot.localRotation = restLocalRotation * Quaternion.Euler(
                    -16f * (1f - t),
                    360f * t,
                    Mathf.Sin(t * Mathf.PI) * -12f);
                ModelRoot.localScale = Vector3.LerpUnclamped(crouchScale, burstScale, eased);

                if (!impactInvoked && t >= skillImpactTime)
                {
                    impactInvoked = true;
                    InvokeImpact(onImpact);
                }

                yield return null;
            }

            if (!impactInvoked)
            {
                InvokeImpact(onImpact);
            }

            RestoreRendererColors();
            yield return InterpolatePose(
                0.22f,
                ModelRoot.localPosition,
                restLocalPosition,
                ModelRoot.localRotation,
                restLocalRotation,
                ModelRoot.localScale,
                restLocalScale);

            RestoreRestPose();
            activeAction = null;
            if (!isDead)
            {
                BeginIdle();
            }
        }

        private IEnumerator PlayFallbackHit(Vector3 sourceWorldPosition)
        {
            SetState(CharacterViewState.Hit);
            Vector3 away = GetLocalHorizontalDirection(sourceWorldPosition, Vector3.back) * -1f;
            Vector3 recoilPosition = restLocalPosition + away * 0.13f;
            Quaternion recoilRotation = restLocalRotation * Quaternion.Euler(0f, 0f, away.x * -12f);
            SetRendererFlash(Color.white);

            yield return InterpolatePose(
                hitDuration * 0.35f,
                restLocalPosition,
                recoilPosition,
                restLocalRotation,
                recoilRotation,
                restLocalScale,
                Vector3.Scale(restLocalScale, new Vector3(1.1f, 0.88f, 1.1f)));

            RestoreRendererColors();
            yield return InterpolatePose(
                hitDuration * 0.65f,
                recoilPosition,
                restLocalPosition,
                recoilRotation,
                restLocalRotation,
                Vector3.Scale(restLocalScale, new Vector3(1.1f, 0.88f, 1.1f)),
                restLocalScale);

            RestoreRestPose();
            activeAction = null;
            if (!isDead)
            {
                BeginIdle();
            }
        }

        private IEnumerator PlayFallbackDeath()
        {
            SetState(CharacterViewState.Dead);
            Vector3 fallenPosition = restLocalPosition + Vector3.down * 0.48f;
            Quaternion fallenRotation = restLocalRotation * Quaternion.Euler(0f, 0f, -82f);
            Vector3 fallenScale = Vector3.Scale(restLocalScale, new Vector3(1.08f, 0.88f, 1.08f));
            SetRendererFlash(new Color(0.48f, 0.5f, 0.56f, 1f));

            yield return InterpolatePose(
                deathDuration,
                restLocalPosition,
                fallenPosition,
                restLocalRotation,
                fallenRotation,
                restLocalScale,
                fallenScale);

            ModelRoot.localPosition = fallenPosition;
            ModelRoot.localRotation = fallenRotation;
            ModelRoot.localScale = fallenScale;
            activeAction = null;
        }

        private IEnumerator IdleBreathing()
        {
            float phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            while (!isDead && enabled && gameObject.activeInHierarchy)
            {
                phase += Time.deltaTime * breathingSpeed;
                float breath = (Mathf.Sin(phase) + 1f) * 0.5f;
                ModelRoot.localPosition = restLocalPosition + Vector3.up * (breathingHeight * breath);
                float stretch = 1f + breath * 0.012f;
                ModelRoot.localScale = Vector3.Scale(restLocalScale, new Vector3(1f / stretch, stretch, 1f / stretch));
                yield return null;
            }

            idleRoutine = null;
        }

        private IEnumerator InterpolatePose(
            float duration,
            Vector3 fromPosition,
            Vector3 toPosition,
            Quaternion fromRotation,
            Quaternion toRotation,
            Vector3 fromScale,
            Vector3 toScale)
        {
            if (duration <= 0f)
            {
                ModelRoot.localPosition = toPosition;
                ModelRoot.localRotation = toRotation;
                ModelRoot.localScale = toScale;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                ModelRoot.localPosition = Vector3.LerpUnclamped(fromPosition, toPosition, t);
                ModelRoot.localRotation = Quaternion.SlerpUnclamped(fromRotation, toRotation, t);
                ModelRoot.localScale = Vector3.LerpUnclamped(fromScale, toScale, t);
                yield return null;
            }

            ModelRoot.localPosition = toPosition;
            ModelRoot.localRotation = toRotation;
            ModelRoot.localScale = toScale;
        }

        private void BeginIdle()
        {
            if (isDead || !isActiveAndEnabled)
            {
                return;
            }

            SetState(CharacterViewState.Idle);
            if (HasAnimatorTrigger(idleTrigger))
            {
                SetAnimatorTrigger(idleTrigger);
                return;
            }

            RestoreRestPose();
            if (idleRoutine == null)
            {
                idleRoutine = StartCoroutine(IdleBreathing());
            }
        }

        private void CaptureRestPose()
        {
            Transform root = ModelRoot;
            restLocalPosition = root.localPosition;
            restLocalRotation = root.localRotation;
            restLocalScale = root.localScale;
            restPoseCaptured = true;
        }

        private void CaptureSpawnPosition()
        {
            spawnWorldPosition = transform.position;
            spawnPositionCaptured = true;
        }

        private void RestoreRestPose()
        {
            if (!restPoseCaptured)
            {
                CaptureRestPose();
            }

            Transform root = ModelRoot;
            root.localPosition = restLocalPosition;
            root.localRotation = restLocalRotation;
            root.localScale = restLocalScale;
        }

        private void StopAllPresentationCoroutines(bool restorePose)
        {
            if (activeAction != null)
            {
                StopCoroutine(activeAction);
                activeAction = null;
            }

            if (idleRoutine != null)
            {
                StopCoroutine(idleRoutine);
                idleRoutine = null;
            }

            RestoreRendererColors();
            if (restorePose && restPoseCaptured)
            {
                RestoreRestPose();
            }

        }

        private IEnumerator MoveRootPersistent(Vector3 destinationWorldPosition, float duration)
        {
            Vector3 startWorldPosition = transform.position;
            if (duration <= 0f)
            {
                transform.position = destinationWorldPosition;
                RecordRootTravel();
                movementRoutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.LerpUnclamped(startWorldPosition, destinationWorldPosition, t);
                RecordRootTravel();
                yield return null;
            }

            transform.position = destinationWorldPosition;
            RecordRootTravel();
            movementRoutine = null;
        }

        private void StopRootMovement(bool restoreSpawnPosition)
        {
            if (movementRoutine != null)
            {
                StopCoroutine(movementRoutine);
                movementRoutine = null;
            }

            if (restoreSpawnPosition && spawnPositionCaptured)
            {
                transform.position = spawnWorldPosition;
            }
        }

        private void RecordRootTravel()
        {
            if (!spawnPositionCaptured)
            {
                CaptureSpawnPosition();
            }

            float distance = Vector3.Distance(spawnWorldPosition, transform.position);
            MaximumRootTravelDistance = Mathf.Max(MaximumRootTravelDistance, distance);
            if (distance > 0.05f)
            {
                HasPerformedApproach = true;
            }
        }

        private void SetModelPose(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Transform root = ModelRoot;
            root.localPosition = position;
            root.localRotation = rotation;
            root.localScale = scale;
        }

        private Vector3 GetLocalHorizontalDirection(Vector3 targetWorldPosition, Vector3 fallback)
        {
            Vector3 worldDirection = targetWorldPosition - transform.position;
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude < 0.0001f)
            {
                worldDirection = transform.TransformDirection(fallback);
            }

            worldDirection.Normalize();
            Transform parent = ModelRoot.parent;
            Vector3 localDirection = parent != null
                ? parent.InverseTransformDirection(worldDirection)
                : worldDirection;
            localDirection.y = 0f;
            return localDirection.sqrMagnitude > 0.0001f ? localDirection.normalized : fallback.normalized;
        }

        private void SetAnimatorTrigger(string triggerName)
        {
            if (!HasAnimatorTrigger(triggerName))
            {
                return;
            }

            int triggerHash = Animator.StringToHash(triggerName);
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.nameHash == triggerHash && parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    animator.SetTrigger(triggerHash);
                    return;
                }
            }
        }

        private bool HasAnimatorTrigger(string triggerName)
        {
            if (!UsesAnimator || string.IsNullOrWhiteSpace(triggerName))
            {
                return false;
            }

            int triggerHash = Animator.StringToHash(triggerName);
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.nameHash == triggerHash && parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    return true;
                }
            }

            return false;
        }

        private void InvokeImpact(Action callback)
        {
            callback?.Invoke();
            ActionImpact?.Invoke(this);
        }

        private void SetState(CharacterViewState newState)
        {
            if (State == newState)
            {
                return;
            }

            State = newState;
            StateChanged?.Invoke(this, newState);
        }

        private void SetRendererFlash(Color flashColor)
        {
            foreach (RendererColorInfo info in rendererColors)
            {
                if (info.Renderer == null)
                {
                    continue;
                }

                var block = new MaterialPropertyBlock();
                info.Renderer.GetPropertyBlock(block);
                block.SetColor(info.PropertyId, Color.Lerp(info.OriginalColor, flashColor, 0.78f));
                info.Renderer.SetPropertyBlock(block);
            }
        }

        private void RestoreRendererColors()
        {
            foreach (RendererColorInfo info in rendererColors)
            {
                if (info.Renderer == null)
                {
                    continue;
                }

                var block = new MaterialPropertyBlock();
                info.Renderer.GetPropertyBlock(block);
                block.SetColor(info.PropertyId, info.OriginalColor);
                info.Renderer.SetPropertyBlock(block);
            }
        }

        private static IEnumerator WaitForDuration(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private static float EaseOutBack(float value)
        {
            const float overshoot = 1.70158f;
            float shifted = value - 1f;
            return 1f + (overshoot + 1f) * shifted * shifted * shifted + overshoot * shifted * shifted;
        }

        private static Transform FindDescendantByName(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            for (int index = 0; index < root.childCount; index++)
            {
                Transform child = root.GetChild(index);
                if (child.name == childName)
                {
                    return child;
                }

                Transform nested = FindDescendantByName(child, childName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static Transform FindDescendantByNames(Transform root, params string[] childNames)
        {
            for (int i = 0; i < childNames.Length; i++)
            {
                Transform match = FindDescendantByName(root, childNames[i]);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private readonly struct RendererColorInfo
        {
            public RendererColorInfo(Renderer renderer, int propertyId, Color originalColor)
            {
                Renderer = renderer;
                PropertyId = propertyId;
                OriginalColor = originalColor;
            }

            public Renderer Renderer { get; }
            public int PropertyId { get; }
            public Color OriginalColor { get; }
        }
    }
}
