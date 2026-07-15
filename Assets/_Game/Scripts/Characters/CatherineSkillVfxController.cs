using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GenericGachaRPG
{
    public enum CatherineUltimateVfxStage
    {
        Charge = 0,
        Transform = 1,
        Pull = 2,
        Collapse = 3,
        KnockUp = 4,
        Complete = 5
    }

    /// <summary>
    /// Owns Catherine's runtime-only skill presentation. Battle state, damage,
    /// control effects and target selection remain the presenter's responsibility.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CatherineSkillVfxController : MonoBehaviour
    {
        private const string VfxShaderName = "BubbleMind/Black Hole VFX";

        private static readonly int ModeId = Shader.PropertyToID("_Mode");
        private static readonly int CoreColorId = Shader.PropertyToID("_CoreColor");
        private static readonly int InnerColorId = Shader.PropertyToID("_InnerColor");
        private static readonly int OuterColorId = Shader.PropertyToID("_OuterColor");
        private static readonly int AccentColorId = Shader.PropertyToID("_AccentColor");
        private static readonly int RadiusId = Shader.PropertyToID("_Radius");
        private static readonly int RingWidthId = Shader.PropertyToID("_RingWidth");
        private static readonly int DiskFlattenId = Shader.PropertyToID("_DiskFlatten");
        private static readonly int TwistId = Shader.PropertyToID("_Twist");
        private static readonly int SpeedId = Shader.PropertyToID("_Speed");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int PhaseId = Shader.PropertyToID("_Phase");
        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
        private static readonly int AspectId = Shader.PropertyToID("_Aspect");

        [Header("Bindings")]
        [SerializeField] private Transform vfxSocket;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Camera effectCamera;
        [SerializeField] private Shader vfxShader;

        [Header("Catherine Palette")]
        [SerializeField] private Color coreColor = new Color(0f, 0f, 0f, 1f);
        [SerializeField] private Color innerColor = new Color(0.34f, 0.06f, 1.45f, 1f);
        [SerializeField] private Color outerColor = new Color(0.08f, 0.66f, 1.65f, 1f);
        [SerializeField] private Color accentColor = new Color(1.35f, 0.66f, 1.85f, 1f);
        [SerializeField] private Color controlColor = new Color(0.25f, 0.88f, 1.65f, 1f);

        [Header("Skill Timing")]
        [Min(0.1f), SerializeField] private float skill1Duration = 0.58f;
        [Min(0.1f), SerializeField] private float skill2Duration = 0.29f;
        [Min(0.1f), SerializeField] private float debuffDuration = 0.52f;
        [Min(0.1f), SerializeField] private float stackGainDuration = 0.45f;

        [Header("Ultimate Timing")]
        [Min(0.1f), SerializeField] private float ultimateChargeDuration = 0.45f;
        [Min(0.1f), SerializeField] private float ultimateTransformDuration = 0.22f;
        [Min(0.2f), SerializeField] private float ultimatePullDuration = 1.15f;
        [Min(0.1f), SerializeField] private float ultimateCollapseDuration = 0.32f;
        [Min(0.1f), SerializeField] private float ultimateKnockUpDuration = 0.48f;

        [Header("Ultimate Shape")]
        [Min(0.2f), SerializeField] private float ultimateVisualDiameter = 3.5f;
        [Min(0f), SerializeField] private float ultimateTargetScatter = 0.12f;
        [Min(0f), SerializeField] private float ultimateKnockUpHeight = 1.9f;

        private readonly List<EffectScope> liveScopes = new List<EffectScope>();
        private MaterialPropertyBlock propertyBlock;
        private CharacterView characterView;
        private Coroutine ultimateRoutine;
        private EffectScope ultimateScope;
        private bool isShuttingDown;

        public bool IsPlayingUltimate => ultimateRoutine != null;
        public CatherineUltimateVfxStage CurrentUltimateStage { get; private set; }
        public Shader VfxShader => vfxShader;

        public event Action<CatherineUltimateVfxStage> UltimateStageChanged;
        public event Action<float> UltimatePullProgress;

        public void Configure(
            Transform effectOrigin,
            Transform actorVisualRoot,
            Camera camera = null,
            Shader shader = null)
        {
            vfxSocket = effectOrigin;
            visualRoot = actorVisualRoot;
            effectCamera = camera;
            if (shader != null)
            {
                vfxShader = shader;
            }

            ResolveReferences();
        }

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            ResolveReferences();
        }

        private void OnDisable()
        {
            CleanupAllEffects();
        }

        private void OnDestroy()
        {
            CleanupAllEffects();
        }

        public Coroutine PlaySkill1LineBreak(
            Vector3 targetWorldPosition,
            Action onImpact = null,
            Action onComplete = null)
        {
            EffectScope scope = CreateScope("Skill1_LineBreak", onImpact, onComplete);
            if (scope == null)
            {
                return null;
            }

            Material material = scope.CreateMaterial(vfxShader, "Catherine.Skill1.Runtime");
            EffectVisual charge = scope.CreateQuad("WindWheelCharge", material, true);
            EffectVisual beam = scope.CreateQuad("LineBreakBeam", material, false);
            return StartManaged(
                PlaySkill1Sequence(charge, beam, targetWorldPosition, onImpact, onComplete),
                scope);
        }

        public Coroutine PlaySkill2Dance(
            Vector3 targetWorldPosition,
            Action onFirstHit = null,
            Action onChargeHit = null,
            Action onComplete = null)
        {
            EffectScope scope = CreateScope("Skill2_WindWheelDance", onFirstHit, onChargeHit, onComplete);
            if (scope == null)
            {
                return null;
            }

            Material material = scope.CreateMaterial(vfxShader, "Catherine.Skill2.Runtime");
            EffectVisual wheel = scope.CreateQuad("WindWheel", material, true);
            EffectVisual trail = scope.CreateQuad("WindWheelTrail", material, false);
            EffectVisual armor = scope.CreateQuad("SuperArmorAura", material, true);
            return StartManaged(
                PlaySkill2Sequence(
                    wheel,
                    trail,
                    armor,
                    targetWorldPosition,
                    onFirstHit,
                    onChargeHit,
                    onComplete),
                scope);
        }

        public Coroutine PlayDebuff(
            Vector3 targetWorldPosition,
            Action onApplied = null,
            Action onComplete = null)
        {
            EffectScope scope = CreateScope("Debuff", onApplied, onComplete);
            if (scope == null)
            {
                return null;
            }

            Material material = scope.CreateMaterial(vfxShader, "Catherine.Debuff.Runtime");
            EffectVisual ring = scope.CreateQuad("ImaginaryMassDebuff", material, true);
            return StartManaged(PlayDebuffSequence(ring, targetWorldPosition, onApplied, onComplete), scope);
        }

        public Coroutine PlayStackGain(int stackCount, Action onComplete = null)
        {
            EffectScope scope = CreateScope("ImaginaryMassStack", onComplete);
            if (scope == null)
            {
                return null;
            }

            int moteCount = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(1, stackCount) / 8f), 1, 6);
            Material material = scope.CreateMaterial(vfxShader, "Catherine.StackGain.Runtime");
            EffectVisual halo = scope.CreateQuad("StackHalo", material, true);
            var motes = new List<EffectVisual>(moteCount);
            for (int i = 0; i < moteCount; i++)
            {
                motes.Add(scope.CreateQuad($"MassMote_{i:00}", material, true));
            }

            return StartManaged(PlayStackGainSequence(halo, motes, stackCount, onComplete), scope);
        }

        /// <summary>
        /// Plays the full ultimate timeline. Pass an empty/null target list when
        /// the presenter owns UnitPulled and UnitKnockedUp transform playback.
        /// </summary>
        public Coroutine PlayUltimatePull(
            IReadOnlyList<Transform> targets,
            Action onCollapseImpact = null,
            Action onKnockUpImpact = null,
            Action onComplete = null,
            Action<CatherineUltimateVfxStage> onStageChanged = null)
        {
            return PlayUltimatePull(
                EffectOrigin,
                targets,
                onCollapseImpact,
                onKnockUpImpact,
                onComplete,
                onStageChanged);
        }

        public Coroutine PlayUltimatePull(
            Vector3 blackHoleWorldPosition,
            IReadOnlyList<Transform> targets,
            Action onCollapseImpact = null,
            Action onKnockUpImpact = null,
            Action onComplete = null,
            Action<CatherineUltimateVfxStage> onStageChanged = null)
        {
            StopUltimateEffect();

            EffectScope scope = CreateScope(
                "Ultimate_InfiniteVoid",
                onCollapseImpact,
                onKnockUpImpact,
                onComplete);
            if (scope == null)
            {
                return null;
            }

            ultimateScope = scope;
            VisualRootSnapshot actorSnapshot = new VisualRootSnapshot(visualRoot);
            List<TargetPose> targetPoses = CaptureTargetPoses(targets);
            scope.RegisterCleanup(actorSnapshot.Restore);
            scope.RegisterCleanup(() => RestoreTargets(targetPoses));

            Material material = scope.CreateMaterial(vfxShader, "Catherine.Ultimate.Runtime");
            EffectVisual charge = scope.CreateQuad("InfiniteVoidCharge", material, true);
            EffectVisual horizon = scope.CreateQuad("EventHorizon", material, false);
            EffectVisual disk = scope.CreateQuad("AccretionDisk", material, false);
            EffectVisual wave = scope.CreateQuad("GravityWave", material, false);
            EffectVisual collapse = scope.CreateQuad("CollapseFlash", material, false);

            IEnumerator sequence = PlayUltimateSequence(
                charge,
                horizon,
                disk,
                wave,
                collapse,
                actorSnapshot,
                targetPoses,
                blackHoleWorldPosition,
                onCollapseImpact,
                onKnockUpImpact,
                onComplete,
                onStageChanged);

            ultimateRoutine = StartManaged(sequence, scope, () =>
            {
                if (ReferenceEquals(ultimateScope, scope))
                {
                    ultimateScope = null;
                    ultimateRoutine = null;
                }
            });
            return ultimateRoutine;
        }

        public void StopUltimateEffect()
        {
            Coroutine running = ultimateRoutine;
            EffectScope scope = ultimateScope;
            ultimateRoutine = null;
            ultimateScope = null;

            if (running != null)
            {
                StopCoroutine(running);
            }

            if (scope != null)
            {
                scope.Dispose();
                liveScopes.Remove(scope);
            }
        }

        private IEnumerator PlaySkill1Sequence(
            EffectVisual charge,
            EffectVisual beam,
            Vector3 targetWorldPosition,
            Action onImpact,
            Action onComplete)
        {
            float totalDuration = Mathf.Max(0.1f, skill1Duration);
            float chargeDuration = totalDuration * 0.30f;
            float elapsed = 0f;

            while (elapsed < chargeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / chargeDuration);
                charge.Transform.position = EffectOrigin;
                charge.Transform.localScale = Vector3.one * Mathf.Lerp(0.12f, 0.76f, EaseOutCubic(t));
                FaceCamera(charge.Transform);
                ApplyEffect(charge.Renderer, 3f, t * 0.28f, 1.8f, 1f, 1f, 0.08f, 2.5f, 6f, 1.5f);
                yield return null;
            }

            beam.GameObject.SetActive(true);
            onImpact?.Invoke();
            elapsed = 0f;
            float beamDuration = totalDuration - chargeDuration;
            while (elapsed < beamDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / beamDuration);
                float width = Mathf.Lerp(0.18f, 0.68f, Mathf.Sin(t * Mathf.PI));
                AlignBeam(beam.Transform, EffectOrigin, targetWorldPosition, Mathf.Max(0.08f, width));
                ApplyEffect(beam.Renderer, 4f, t, 2.35f, 1f - EaseInCubic(t) * 0.68f, 1f, 0.05f, 1f, 4f, 2.8f);

                charge.Transform.position = EffectOrigin;
                charge.Transform.localScale = Vector3.one * Mathf.Lerp(0.72f, 0.08f, EaseInCubic(t));
                FaceCamera(charge.Transform);
                ApplyEffect(charge.Renderer, 3f, Mathf.Lerp(0.28f, 1f, t), 2.1f, 1f - t, 1f, 0.08f, 2.5f, 6f, 2.4f);
                yield return null;
            }

            onComplete?.Invoke();
        }

        private IEnumerator PlaySkill2Sequence(
            EffectVisual wheel,
            EffectVisual trail,
            EffectVisual armor,
            Vector3 targetWorldPosition,
            Action onFirstHit,
            Action onChargeHit,
            Action onComplete)
        {
            Vector3 start = EffectOrigin;
            Vector3 direction = targetWorldPosition - start;
            Vector3 lateral = Vector3.Cross(Vector3.up, direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : transform.forward);
            if (lateral.sqrMagnitude < 0.0001f)
            {
                lateral = transform.right;
            }

            lateral.Normalize();
            float firstDuration = Mathf.Max(0.05f, skill2Duration * 0.46f);
            float elapsed = 0f;
            Vector3 firstHitPosition = Vector3.Lerp(start, targetWorldPosition, 0.42f);
            while (elapsed < firstDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / firstDuration);
                Vector3 orbit = lateral * Mathf.Cos(t * Mathf.PI * 2f) * 0.34f;
                orbit += Vector3.up * (0.16f + Mathf.Sin(t * Mathf.PI) * 0.42f);
                wheel.Transform.position = Vector3.Lerp(start, firstHitPosition, EaseInOut(t)) + orbit;
                wheel.Transform.localScale = Vector3.one * Mathf.Lerp(0.42f, 0.82f, Mathf.Sin(t * Mathf.PI));
                FaceCamera(wheel.Transform);
                ApplyEffect(wheel.Renderer, 1f, t, 1.55f, 0.95f, 1f, 0.065f, 3.3f, 5.5f, 1.7f);

                armor.Transform.position = EffectOrigin;
                armor.Transform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.48f, t);
                FaceCamera(armor.Transform);
                ApplyEffect(armor.Renderer, 2f, t, 0.86f, 0.72f * (1f - t * 0.55f), 1f, 0.06f, 2.2f, 6f, 1f);
                yield return null;
            }

            onFirstHit?.Invoke();
            trail.GameObject.SetActive(true);
            float chargeDuration = Mathf.Max(0.05f, skill2Duration - firstDuration);
            elapsed = 0f;
            Vector3 chargeStart = wheel.Transform.position;
            while (elapsed < chargeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / chargeDuration);
                Vector3 previous = wheel.Transform.position;
                wheel.Transform.position = Vector3.Lerp(chargeStart, targetWorldPosition, EaseInCubic(t));
                wheel.Transform.localScale = new Vector3(
                    Mathf.Lerp(0.72f, 0.32f, t),
                    Mathf.Lerp(0.72f, 1.08f, t),
                    1f);
                FaceCamera(wheel.Transform);
                ApplyEffect(wheel.Renderer, 1f, t, 2f, 1f - t * 0.24f, 1f, 0.055f, 3.8f, 7f, 2.4f);

                AlignBeam(trail.Transform, chargeStart, wheel.Transform.position, Mathf.Lerp(0.34f, 0.16f, t));
                ApplyEffect(trail.Renderer, 4f, t, 1.25f, 0.64f * (1f - t * 0.45f), 1f, 0.04f, 1f, 4f, 2.8f);

                if ((wheel.Transform.position - previous).sqrMagnitude < 0.000001f)
                {
                    FaceCamera(wheel.Transform);
                }

                yield return null;
            }

            onChargeHit?.Invoke();
            onComplete?.Invoke();
        }

        private IEnumerator PlayDebuffSequence(
            EffectVisual ring,
            Vector3 targetWorldPosition,
            Action onApplied,
            Action onComplete)
        {
            float duration = Mathf.Max(0.1f, debuffDuration);
            float elapsed = 0f;
            bool applied = false;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                ring.Transform.position = targetWorldPosition + Vector3.up * Mathf.Lerp(0.16f, 0.72f, t);
                ring.Transform.localScale = Vector3.one * Mathf.Lerp(0.22f, 1.72f, EaseOutCubic(t));
                FaceCamera(ring.Transform);
                ApplyEffect(ring.Renderer, 2f, t, 1.18f, 1f - t * 0.86f, 1f, 0.055f, 2f, 7f, 1f);

                if (!applied && t >= 0.34f)
                {
                    applied = true;
                    onApplied?.Invoke();
                }

                yield return null;
            }

            if (!applied)
            {
                onApplied?.Invoke();
            }

            onComplete?.Invoke();
        }

        private IEnumerator PlayStackGainSequence(
            EffectVisual halo,
            IReadOnlyList<EffectVisual> motes,
            int stackCount,
            Action onComplete)
        {
            float duration = Mathf.Max(0.1f, stackGainDuration);
            float elapsed = 0f;
            float stackStrength = Mathf.Clamp01(Mathf.Max(1, stackCount) / 50f);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector3 center = EffectOrigin + Vector3.up * 0.34f;

                halo.Transform.position = center;
                halo.Transform.localScale = Vector3.one * Mathf.Lerp(0.28f, 1.18f + stackStrength * 0.34f, EaseOutCubic(t));
                FaceCamera(halo.Transform);
                ApplyEffect(halo.Renderer, 2f, t, 0.84f + stackStrength, 1f - t * 0.82f, 1f, 0.045f, 2f, 6f, 1f);

                for (int i = 0; i < motes.Count; i++)
                {
                    float phase = Mathf.PI * 2f * i / Mathf.Max(1, motes.Count);
                    float angle = phase + t * Mathf.PI * 2.6f;
                    float radius = Mathf.Lerp(0.48f, 0.08f, t);
                    Vector3 offset = transform.right * Mathf.Cos(angle) * radius;
                    offset += Vector3.up * (Mathf.Sin(angle) * radius * 0.38f + Mathf.Lerp(0f, 0.58f, t));
                    motes[i].Transform.position = center + offset;
                    motes[i].Transform.localScale = Vector3.one * Mathf.Lerp(0.16f, 0.06f, t);
                    FaceCamera(motes[i].Transform);
                    ApplyEffect(motes[i].Renderer, 3f, t * 0.48f, 1.2f, 1f - t * 0.6f, 1f, 0.035f, 2.2f, 5f, 1.4f);
                }

                yield return null;
            }

            onComplete?.Invoke();
        }

        private IEnumerator PlayUltimateSequence(
            EffectVisual charge,
            EffectVisual horizon,
            EffectVisual disk,
            EffectVisual wave,
            EffectVisual collapse,
            VisualRootSnapshot actorSnapshot,
            IReadOnlyList<TargetPose> targets,
            Vector3 blackHoleWorldPosition,
            Action onCollapseImpact,
            Action onKnockUpImpact,
            Action onComplete,
            Action<CatherineUltimateVfxStage> onStageChanged)
        {
            RaiseUltimateStage(CatherineUltimateVfxStage.Charge, onStageChanged);
            float elapsed = 0f;
            float duration = Mathf.Max(0.1f, ultimateChargeDuration);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                charge.Transform.position = EffectOrigin;
                charge.Transform.localScale = Vector3.one * Mathf.Lerp(0.12f, 1.36f, EaseOutCubic(t));
                FaceCamera(charge.Transform);
                ApplyEffect(charge.Renderer, 3f, t * 0.33f, 2.2f, 1f, 1f, 0.07f, 2.4f, 8f, 2f);
                actorSnapshot.SetScaleMultiplier(1f + Mathf.Sin(t * Mathf.PI * 5f) * 0.045f);
                yield return null;
            }

            RaiseUltimateStage(CatherineUltimateVfxStage.Transform, onStageChanged);
            horizon.GameObject.SetActive(true);
            disk.GameObject.SetActive(true);
            elapsed = 0f;
            duration = Mathf.Max(0.1f, ultimateTransformDuration);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseInCubic(t);
                actorSnapshot.SetScaleMultiplier(Mathf.Lerp(1f, 0.025f, eased));

                charge.Transform.position = Vector3.Lerp(EffectOrigin, blackHoleWorldPosition, t);
                charge.Transform.localScale = Vector3.one * Mathf.Lerp(1.36f, 0.12f, eased);
                FaceCamera(charge.Transform);
                ApplyEffect(charge.Renderer, 3f, Mathf.Lerp(0.33f, 0.72f, t), 2.6f, 1f - t, 1f, 0.06f, 2.4f, 8f, 2.2f);

                float diameter = Mathf.Lerp(0.18f, ultimateVisualDiameter, EaseOutCubic(t));
                PositionBlackHoleLayers(horizon, disk, blackHoleWorldPosition, diameter, t);
                yield return null;
            }

            actorSnapshot.SetVisible(false);
            RaiseUltimateStage(CatherineUltimateVfxStage.Pull, onStageChanged);
            wave.GameObject.SetActive(true);
            elapsed = 0f;
            duration = Mathf.Max(0.2f, ultimatePullDuration);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = 1f + Mathf.Sin(t * Mathf.PI * 8f) * 0.055f;
                PositionBlackHoleLayers(horizon, disk, blackHoleWorldPosition, ultimateVisualDiameter * pulse, t);

                wave.Transform.position = blackHoleWorldPosition;
                wave.Transform.localScale = Vector3.one * ultimateVisualDiameter * 1.45f;
                FaceCamera(wave.Transform);
                ApplyEffect(wave.Renderer, 2f, Mathf.Repeat(t * 2.35f, 1f), 1.18f, 0.72f, 1f, 0.065f, 2f, 7f, 1f);

                ApplyPullToTargets(targets, blackHoleWorldPosition, t);
                UltimatePullProgress?.Invoke(t);
                yield return null;
            }

            RaiseUltimateStage(CatherineUltimateVfxStage.Collapse, onStageChanged);
            collapse.GameObject.SetActive(true);
            elapsed = 0f;
            duration = Mathf.Max(0.1f, ultimateCollapseDuration);
            bool collapseImpactSent = false;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float shrinkingDiameter = ultimateVisualDiameter * Mathf.Lerp(1f, 0.06f, EaseInCubic(t));
                PositionBlackHoleLayers(horizon, disk, blackHoleWorldPosition, shrinkingDiameter, t);

                collapse.Transform.position = blackHoleWorldPosition;
                collapse.Transform.localScale = Vector3.one * ultimateVisualDiameter * Mathf.Lerp(0.72f, 1.75f, EaseOutCubic(t));
                FaceCamera(collapse.Transform);
                ApplyEffect(collapse.Renderer, 3f, t, 3.8f, 1f, 1f, 0.07f, 2.6f, 9f, 2.8f);

                if (!collapseImpactSent && t >= 0.52f)
                {
                    collapseImpactSent = true;
                    onCollapseImpact?.Invoke();
                }

                yield return null;
            }

            if (!collapseImpactSent)
            {
                onCollapseImpact?.Invoke();
            }

            horizon.GameObject.SetActive(false);
            disk.GameObject.SetActive(false);
            wave.GameObject.SetActive(false);
            actorSnapshot.Restore();

            RaiseUltimateStage(CatherineUltimateVfxStage.KnockUp, onStageChanged);
            onKnockUpImpact?.Invoke();
            elapsed = 0f;
            duration = Mathf.Max(0.1f, ultimateKnockUpDuration);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                AnimateKnockUp(targets, blackHoleWorldPosition, t);

                collapse.Transform.position = blackHoleWorldPosition;
                collapse.Transform.localScale = Vector3.one * ultimateVisualDiameter * Mathf.Lerp(1.75f, 2.35f, t);
                FaceCamera(collapse.Transform);
                ApplyEffect(collapse.Renderer, 2f, t, 2.1f, 1f - t, 1f, 0.08f, 2.5f, 8f, 1.8f);
                yield return null;
            }

            RestoreTargets(targets);
            RaiseUltimateStage(CatherineUltimateVfxStage.Complete, onStageChanged);
            onComplete?.Invoke();
        }

        private void PositionBlackHoleLayers(
            EffectVisual horizon,
            EffectVisual disk,
            Vector3 center,
            float diameter,
            float progress)
        {
            horizon.Transform.position = center;
            horizon.Transform.localScale = Vector3.one * diameter;
            FaceCamera(horizon.Transform);
            ApplyEffect(horizon.Renderer, 0f, progress, 2.2f, 1f, 1f, 0.065f, 3.1f, 7f, 1.6f);

            disk.Transform.position = center;
            disk.Transform.localScale = new Vector3(diameter * 1.48f, diameter * 0.92f, 1f);
            FaceCamera(disk.Transform);
            ApplyEffect(disk.Renderer, 1f, progress, 2.15f, 0.94f, 1f, 0.05f, 3.4f, 6f, 2.3f);
        }

        private void ApplyPullToTargets(IReadOnlyList<TargetPose> targets, Vector3 center, float progress)
        {
            if (targets == null)
            {
                return;
            }

            float pull = EaseInCubic(progress);
            for (int i = 0; i < targets.Count; i++)
            {
                TargetPose pose = targets[i];
                if (pose.Target == null)
                {
                    continue;
                }

                Vector3 outward = pose.WorldPosition - center;
                outward.y = 0f;
                if (outward.sqrMagnitude < 0.0001f)
                {
                    outward = Quaternion.Euler(0f, i * 137.5f, 0f) * Vector3.forward;
                }

                outward.Normalize();
                Vector3 tangent = Vector3.Cross(Vector3.up, outward);
                float phase = i * 1.73f + progress * Mathf.PI * 5f;
                float remainingOrbit = (1f - pull) * (0.18f + 0.04f * i);
                Vector3 destination = center + outward * ultimateTargetScatter;
                destination += tangent * Mathf.Sin(phase) * remainingOrbit;
                destination += Vector3.up * (0.08f + Mathf.Cos(phase) * remainingOrbit * 0.45f);
                pose.Target.position = Vector3.Lerp(pose.WorldPosition, destination, pull);
            }
        }

        private void AnimateKnockUp(IReadOnlyList<TargetPose> targets, Vector3 center, float progress)
        {
            if (targets == null)
            {
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                TargetPose pose = targets[i];
                if (pose.Target == null)
                {
                    continue;
                }

                Vector3 start = center + Vector3.up * 0.08f;
                Vector3 horizontalReturn = Vector3.Lerp(start, pose.WorldPosition, EaseOutCubic(progress));
                float height = Mathf.Sin(progress * Mathf.PI) * ultimateKnockUpHeight;
                pose.Target.position = horizontalReturn + Vector3.up * height;
            }
        }

        private void ApplyEffect(
            Renderer target,
            float mode,
            float progress,
            float intensity,
            float opacity,
            float aspect,
            float softness,
            float flatten,
            float twist,
            float speed)
        {
            if (target == null)
            {
                return;
            }

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            propertyBlock.Clear();
            propertyBlock.SetFloat(ModeId, mode);
            propertyBlock.SetColor(CoreColorId, coreColor);
            propertyBlock.SetColor(InnerColorId, innerColor);
            propertyBlock.SetColor(OuterColorId, mode == 2f ? controlColor : outerColor);
            propertyBlock.SetColor(AccentColorId, accentColor);
            propertyBlock.SetFloat(RadiusId, 0.34f);
            propertyBlock.SetFloat(RingWidthId, 0.075f);
            propertyBlock.SetFloat(DiskFlattenId, flatten);
            propertyBlock.SetFloat(TwistId, twist);
            propertyBlock.SetFloat(SpeedId, speed);
            propertyBlock.SetFloat(IntensityId, intensity);
            propertyBlock.SetFloat(ProgressId, Mathf.Clamp01(progress));
            propertyBlock.SetFloat(PhaseId, target.transform.GetSiblingIndex() * 1.731f);
            propertyBlock.SetFloat(OpacityId, Mathf.Clamp01(opacity));
            propertyBlock.SetFloat(SoftnessId, softness);
            propertyBlock.SetFloat(AspectId, aspect);
            target.SetPropertyBlock(propertyBlock);
        }

        private void AlignBeam(Transform beam, Vector3 start, Vector3 end, float width)
        {
            Vector3 direction = end - start;
            float length = direction.magnitude;
            if (length < 0.001f)
            {
                direction = transform.forward;
                length = 0.001f;
            }

            Vector3 midpoint = (start + end) * 0.5f;
            beam.position = midpoint;
            Vector3 cameraDirection = GetCameraDirection(midpoint);
            Vector3 up = direction / length;
            if (Mathf.Abs(Vector3.Dot(cameraDirection, up)) > 0.985f)
            {
                cameraDirection = Vector3.Cross(up, transform.right).normalized;
            }

            beam.rotation = Quaternion.LookRotation(cameraDirection, up);
            beam.localScale = new Vector3(Mathf.Max(0.01f, width), length, 1f);
        }

        private void FaceCamera(Transform effect)
        {
            Vector3 direction = GetCameraDirection(effect.position);
            if (direction.sqrMagnitude > 0.0001f)
            {
                effect.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }

        private Vector3 GetCameraDirection(Vector3 position)
        {
            ResolveCamera();
            if (effectCamera != null)
            {
                return (effectCamera.transform.position - position).normalized;
            }

            return -transform.forward;
        }

        private Vector3 EffectOrigin
        {
            get
            {
                if (vfxSocket != null)
                {
                    return vfxSocket.position;
                }

                return transform.position + Vector3.up * 0.72f;
            }
        }

        private void RaiseUltimateStage(
            CatherineUltimateVfxStage stage,
            Action<CatherineUltimateVfxStage> callback)
        {
            CurrentUltimateStage = stage;
            UltimateStageChanged?.Invoke(stage);
            callback?.Invoke(stage);
        }

        private EffectScope CreateScope(string name, params Delegate[] fallbackCallbacks)
        {
            ResolveReferences();
            if (vfxShader != null)
            {
                return new EffectScope(this, name);
            }

            Debug.LogError(
                $"[BubbleMind] Catherine VFX shader '{VfxShaderName}' is unavailable; effect '{name}' was skipped.",
                this);
            for (int i = 0; i < fallbackCallbacks.Length; i++)
            {
                if (fallbackCallbacks[i] is Action action)
                {
                    action.Invoke();
                }
            }

            return null;
        }

        private Coroutine StartManaged(
            IEnumerator sequence,
            EffectScope scope,
            Action onFinished = null)
        {
            liveScopes.Add(scope);
            return StartCoroutine(RunManaged(sequence, scope, onFinished));
        }

        private IEnumerator RunManaged(IEnumerator sequence, EffectScope scope, Action onFinished)
        {
            try
            {
                yield return sequence;
            }
            finally
            {
                scope.Dispose();
                if (!isShuttingDown)
                {
                    liveScopes.Remove(scope);
                    onFinished?.Invoke();
                }
            }
        }

        private void ResolveReferences()
        {
            if (characterView == null)
            {
                characterView = GetComponent<CharacterView>();
            }

            if (vfxSocket == null && characterView != null)
            {
                vfxSocket = characterView.SkillVfx;
            }

            if (visualRoot == null && characterView != null)
            {
                visualRoot = characterView.ModelRoot;
            }

            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            if (vfxShader == null)
            {
                vfxShader = Shader.Find(VfxShaderName);
            }

            ResolveCamera();
        }

        private void ResolveCamera()
        {
            if (effectCamera == null && Application.isPlaying)
            {
                effectCamera = Camera.main;
            }
        }

        private void CleanupAllEffects()
        {
            if (isShuttingDown)
            {
                return;
            }

            isShuttingDown = true;
            StopAllCoroutines();
            for (int i = liveScopes.Count - 1; i >= 0; i--)
            {
                liveScopes[i].Dispose();
            }

            liveScopes.Clear();
            ultimateRoutine = null;
            ultimateScope = null;
            isShuttingDown = false;
        }

        private static List<TargetPose> CaptureTargetPoses(IReadOnlyList<Transform> targets)
        {
            var result = new List<TargetPose>();
            if (targets == null)
            {
                return result;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                Transform target = targets[i];
                if (target != null)
                {
                    result.Add(new TargetPose(target));
                }
            }

            return result;
        }

        private static void RestoreTargets(IReadOnlyList<TargetPose> targets)
        {
            if (targets == null)
            {
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                TargetPose pose = targets[i];
                if (pose.Target != null)
                {
                    pose.Target.position = pose.WorldPosition;
                }
            }
        }

        private static float EaseInCubic(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * value;
        }

        private static float EaseOutCubic(float value)
        {
            value = 1f - Mathf.Clamp01(value);
            return 1f - value * value * value;
        }

        private static float EaseInOut(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static void ReleaseUnityObject(UnityEngine.Object value)
        {
            if (value == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(value);
            }
            else
            {
                DestroyImmediate(value);
            }
        }

        private readonly struct EffectVisual
        {
            public EffectVisual(GameObject gameObject, Renderer renderer)
            {
                GameObject = gameObject;
                Renderer = renderer;
            }

            public GameObject GameObject { get; }
            public Transform Transform => GameObject.transform;
            public Renderer Renderer { get; }
        }

        private sealed class EffectScope : IDisposable
        {
            private readonly GameObject root;
            private readonly List<Material> materials = new List<Material>();
            private readonly List<Action> cleanupActions = new List<Action>();
            private bool disposed;

            public EffectScope(CatherineSkillVfxController owner, string effectName)
            {
                root = new GameObject($"CatherineVfx_{effectName}");
                root.transform.SetParent(owner.transform, false);
            }

            public Material CreateMaterial(Shader shader, string materialName)
            {
                var material = new Material(shader)
                {
                    name = materialName,
                    hideFlags = HideFlags.DontSave
                };
                materials.Add(material);
                return material;
            }

            public EffectVisual CreateQuad(string name, Material sharedMaterial, bool active)
            {
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
                visual.name = name;
                visual.transform.SetParent(root.transform, false);
                Renderer renderer = visual.GetComponent<Renderer>();
                renderer.sharedMaterial = sharedMaterial;
                renderer.sortingOrder = GetSortingOrder(name);
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

                Collider collider = visual.GetComponent<Collider>();
                if (collider != null)
                {
                    ReleaseUnityObject(collider);
                }

                visual.SetActive(active);
                return new EffectVisual(visual, renderer);
            }

            private static int GetSortingOrder(string effectName)
            {
                switch (effectName)
                {
                    case "AccretionDisk":
                        return 1;
                    case "GravityWave":
                        return 2;
                    case "EventHorizon":
                        return 3;
                    case "CollapseFlash":
                        return 4;
                    default:
                        return 0;
                }
            }

            public void RegisterCleanup(Action cleanup)
            {
                if (cleanup != null)
                {
                    cleanupActions.Add(cleanup);
                }
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                for (int i = cleanupActions.Count - 1; i >= 0; i--)
                {
                    cleanupActions[i]?.Invoke();
                }

                cleanupActions.Clear();
                ReleaseUnityObject(root);
                for (int i = 0; i < materials.Count; i++)
                {
                    ReleaseUnityObject(materials[i]);
                }

                materials.Clear();
            }
        }

        private sealed class VisualRootSnapshot
        {
            private readonly Transform root;
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;
            private readonly Renderer[] renderers;
            private readonly bool[] rendererStates;

            public VisualRootSnapshot(Transform target)
            {
                root = target;
                if (root == null)
                {
                    localPosition = Vector3.zero;
                    localRotation = Quaternion.identity;
                    localScale = Vector3.one;
                    renderers = Array.Empty<Renderer>();
                    rendererStates = Array.Empty<bool>();
                    return;
                }

                localPosition = root.localPosition;
                localRotation = root.localRotation;
                localScale = root.localScale;
                renderers = root.GetComponentsInChildren<Renderer>(true);
                rendererStates = new bool[renderers.Length];
                for (int i = 0; i < renderers.Length; i++)
                {
                    rendererStates[i] = renderers[i] != null && renderers[i].enabled;
                }
            }

            public void SetScaleMultiplier(float multiplier)
            {
                if (root != null)
                {
                    root.localScale = localScale * Mathf.Max(0.001f, multiplier);
                }
            }

            public void SetVisible(bool visible)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                    {
                        renderers[i].enabled = visible && rendererStates[i];
                    }
                }
            }

            public void Restore()
            {
                if (root != null)
                {
                    root.localPosition = localPosition;
                    root.localRotation = localRotation;
                    root.localScale = localScale;
                }

                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                    {
                        renderers[i].enabled = rendererStates[i];
                    }
                }
            }
        }

        private sealed class TargetPose
        {
            public TargetPose(Transform target)
            {
                Target = target;
                WorldPosition = target.position;
            }

            public Transform Target { get; }
            public Vector3 WorldPosition { get; }
        }
    }
}
