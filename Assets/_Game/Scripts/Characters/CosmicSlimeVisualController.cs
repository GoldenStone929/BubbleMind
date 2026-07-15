using System;
using System.Collections.Generic;
using UnityEngine;

namespace GenericGachaRPG
{
    [DisallowMultipleComponent]
    public sealed class CosmicSlimeVisualController : MonoBehaviour
    {
        private enum MorphAction
        {
            None,
            Squash,
            Stretch,
            Dance,
            UltimateCollapse
        }

        [SerializeField] private float lowerOrbitSpeed = 12f;
        [SerializeField] private float upperOrbitSpeed = -17f;
        [SerializeField] private float accretionSpeed = 24f;
        [SerializeField] private float accretionDiskSpeed = -16f;
        [SerializeField] private float accretionPulseSpeed = 2.6f;
        [SerializeField, Range(0f, 0.12f)] private float accretionPulseAmount = 0.055f;
        [SerializeField] private float corePulseSpeed = 1.8f;
        [SerializeField, Range(0f, 0.08f)] private float corePulseAmount = 0.025f;
        [SerializeField, Range(0f, 100f)] private float idleBreathBaseWeight = 24f;
        [SerializeField, Range(0f, 50f)] private float idleBreathPulseWeight = 18f;
        [SerializeField] private float idleBreathSpeed = 2.15f;

        private readonly List<AnimatedPart> lowerOrbit = new List<AnimatedPart>();
        private readonly List<AnimatedPart> upperOrbit = new List<AnimatedPart>();
        private readonly List<AnimatedPart> accretionSpirals = new List<AnimatedPart>();
        private readonly List<BlendShapeBinding> blendShapeBindings = new List<BlendShapeBinding>();
        private AnimatedPart accretionDisk;
        private AnimatedPart eventHorizon;
        private float animationStartTime;
        private MorphAction morphAction;
        private float morphActionStartTime;
        private float morphActionDuration;

        public int BlendShapeRendererCount => blendShapeBindings.Count;

        public bool HasRequiredBlendShapes
        {
            get
            {
                if (blendShapeBindings.Count == 0)
                {
                    return false;
                }

                for (int i = 0; i < blendShapeBindings.Count; i++)
                {
                    if (!blendShapeBindings[i].HasAllRequiredShapes)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private void Awake()
        {
            Transform[] descendants = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                Transform item = descendants[i];
                if (item.name == "OrbitRig_Lower")
                {
                    lowerOrbit.Add(new AnimatedPart(item));
                }
                else if (item.name == "OrbitRig_Upper")
                {
                    upperOrbit.Add(new AnimatedPart(item));
                }
                else if (item.name == "SingularityCore")
                {
                    eventHorizon = new AnimatedPart(item);
                }
                else if (item.name == "SingularityAccretion")
                {
                    accretionDisk = new AnimatedPart(item);
                }
                else if (item.name.StartsWith("AccretionSpiral_", System.StringComparison.Ordinal))
                {
                    accretionSpirals.Add(new AnimatedPart(item));
                }
            }

            SkinnedMeshRenderer[] skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinnedRenderers.Length; i++)
            {
                var binding = new BlendShapeBinding(skinnedRenderers[i]);
                if (binding.HasAnyRequiredShape)
                {
                    blendShapeBindings.Add(binding);
                }
            }
        }

        private void OnEnable()
        {
            animationStartTime = Time.time;
            morphAction = MorphAction.None;
            RestoreAllParts();
            ResetBlendShapes();
        }

        private void OnDisable()
        {
            RestoreAllParts();
            ResetBlendShapes();
        }

        private void Update()
        {
            float elapsed = Mathf.Max(0f, Time.time - animationStartTime);
            AnimateOrbit(lowerOrbit, lowerOrbitSpeed, elapsed);
            AnimateOrbit(upperOrbit, upperOrbitSpeed, elapsed);

            if (eventHorizon != null)
            {
                // Keep the event horizon anchored and opaque-looking; only its silhouette breathes.
                float pulse = 1f + Mathf.Sin(elapsed * corePulseSpeed) * corePulseAmount;
                eventHorizon.Apply(Vector3.zero, pulse);
            }

            AnimateAccretion(elapsed);
            AnimateBlendShapes(elapsed);
        }

        public void PlaySquash(float duration = 0.32f)
        {
            BeginMorph(MorphAction.Squash, duration);
        }

        public void PlayStretch(float duration = 0.42f)
        {
            BeginMorph(MorphAction.Stretch, duration);
        }

        public void PlayDance(float duration = 0.29f)
        {
            BeginMorph(MorphAction.Dance, duration);
        }

        public void PlayUltimateCollapse(float duration = 0.30f)
        {
            BeginMorph(MorphAction.UltimateCollapse, duration);
        }

        public void PresentUltimateStage(CatherineUltimateVfxStage stage)
        {
            switch (stage)
            {
                case CatherineUltimateVfxStage.Charge:
                    PlaySquash(0.38f);
                    break;
                case CatherineUltimateVfxStage.Transform:
                    PlayUltimateCollapse(0.22f);
                    break;
                case CatherineUltimateVfxStage.KnockUp:
                    PlaySquash(0.39f);
                    break;
                case CatherineUltimateVfxStage.Complete:
                    PlayStretch(0.26f);
                    break;
            }
        }

        private void AnimateAccretion(float elapsed)
        {
            if (accretionDisk != null)
            {
                float diskPulse = 1f + Mathf.Sin(elapsed * accretionPulseSpeed) * accretionPulseAmount * 0.45f;
                float diskAngle = Mathf.Repeat(accretionDiskSpeed * elapsed, 360f);
                accretionDisk.Apply(Vector3.forward * diskAngle, diskPulse);
            }

            int phaseCount = accretionSpirals.Count + 1;
            for (int i = 0; i < accretionSpirals.Count; i++)
            {
                float phase = Mathf.PI * 2f * (i + 1f) / phaseCount;
                float pulse = 1f + Mathf.Sin(elapsed * accretionPulseSpeed + phase) * accretionPulseAmount;
                float speedVariation = 1f + i * 0.12f;
                float spiralAngle = Mathf.Repeat(accretionSpeed * speedVariation * elapsed, 360f);
                accretionSpirals[i].Apply(Vector3.forward * spiralAngle, pulse);
            }
        }

        private void AnimateBlendShapes(float elapsed)
        {
            float idleWeight = Mathf.Clamp(
                idleBreathBaseWeight + Mathf.Sin(elapsed * idleBreathSpeed) * idleBreathPulseWeight,
                0f,
                100f);
            float squashWeight = 0f;
            float stretchWeight = 0f;
            float collapseWeight = 0f;

            if (morphAction != MorphAction.None)
            {
                float duration = Mathf.Max(0.05f, morphActionDuration);
                float progress = Mathf.Clamp01((Time.time - morphActionStartTime) / duration);
                if (progress >= 1f)
                {
                    morphAction = MorphAction.None;
                }
                else if (morphAction == MorphAction.Dance)
                {
                    const float split = 0.46f;
                    if (progress < split)
                    {
                        squashWeight = Mathf.Sin(progress / split * Mathf.PI) * 100f;
                    }
                    else
                    {
                        stretchWeight = Mathf.Sin((progress - split) / (1f - split) * Mathf.PI) * 100f;
                    }
                }
                else
                {
                    float actionWeight = Mathf.Sin(progress * Mathf.PI) * 100f;
                    switch (morphAction)
                    {
                        case MorphAction.Squash:
                            squashWeight = actionWeight;
                            break;
                        case MorphAction.Stretch:
                            stretchWeight = actionWeight;
                            break;
                        case MorphAction.UltimateCollapse:
                            collapseWeight = actionWeight;
                            break;
                    }
                }
            }

            for (int i = 0; i < blendShapeBindings.Count; i++)
            {
                blendShapeBindings[i].Apply(idleWeight, squashWeight, stretchWeight, collapseWeight);
            }
        }

        private void BeginMorph(MorphAction action, float duration)
        {
            morphAction = action;
            morphActionStartTime = Time.time;
            morphActionDuration = Mathf.Max(0.05f, duration);
        }

        private static void AnimateOrbit(List<AnimatedPart> parts, float speed, float elapsed)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                float angle = Mathf.Repeat(speed * elapsed, 360f);
                parts[i].Apply(Vector3.up * angle, 1f);
            }
        }

        private void RestoreAllParts()
        {
            Restore(lowerOrbit);
            Restore(upperOrbit);
            Restore(accretionSpirals);
            accretionDisk?.Restore();
            eventHorizon?.Restore();
        }

        private void ResetBlendShapes()
        {
            for (int i = 0; i < blendShapeBindings.Count; i++)
            {
                blendShapeBindings[i].Apply(0f, 0f, 0f, 0f);
            }
        }

        private static void Restore(List<AnimatedPart> parts)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                parts[i].Restore();
            }
        }

        private sealed class AnimatedPart
        {
            private readonly Transform target;
            private readonly Vector3 baseLocalPosition;
            private readonly Quaternion baseLocalRotation;
            private readonly Vector3 baseLocalScale;

            public AnimatedPart(Transform target)
            {
                this.target = target;
                baseLocalPosition = target.localPosition;
                baseLocalRotation = target.localRotation;
                baseLocalScale = target.localScale;
            }

            public void Apply(Vector3 localEulerOffset, float scaleMultiplier)
            {
                if (target == null)
                {
                    return;
                }

                target.localPosition = baseLocalPosition;
                target.localRotation = baseLocalRotation * Quaternion.Euler(localEulerOffset);
                target.localScale = baseLocalScale * scaleMultiplier;
            }

            public void Restore()
            {
                if (target == null)
                {
                    return;
                }

                target.localPosition = baseLocalPosition;
                target.localRotation = baseLocalRotation;
                target.localScale = baseLocalScale;
            }
        }

        private sealed class BlendShapeBinding
        {
            private const string IdleBreathName = "IdleBreath";
            private const string SquashName = "Squash";
            private const string StretchName = "Stretch";
            private const string UltimateCollapseName = "UltimateCollapse";

            private readonly SkinnedMeshRenderer renderer;
            private readonly int idleBreathIndex;
            private readonly int squashIndex;
            private readonly int stretchIndex;
            private readonly int ultimateCollapseIndex;

            public BlendShapeBinding(SkinnedMeshRenderer renderer)
            {
                this.renderer = renderer;
                Mesh mesh = renderer == null ? null : renderer.sharedMesh;
                idleBreathIndex = FindBlendShapeIndex(mesh, IdleBreathName);
                squashIndex = FindBlendShapeIndex(mesh, SquashName);
                stretchIndex = FindBlendShapeIndex(mesh, StretchName);
                ultimateCollapseIndex = FindBlendShapeIndex(mesh, UltimateCollapseName);
            }

            public bool HasAnyRequiredShape =>
                idleBreathIndex >= 0 || squashIndex >= 0 || stretchIndex >= 0 || ultimateCollapseIndex >= 0;

            public bool HasAllRequiredShapes =>
                idleBreathIndex >= 0 && squashIndex >= 0 && stretchIndex >= 0 && ultimateCollapseIndex >= 0;

            public void Apply(float idleBreath, float squash, float stretch, float ultimateCollapse)
            {
                if (renderer == null)
                {
                    return;
                }

                SetWeight(idleBreathIndex, idleBreath);
                SetWeight(squashIndex, squash);
                SetWeight(stretchIndex, stretch);
                SetWeight(ultimateCollapseIndex, ultimateCollapse);
            }

            private void SetWeight(int index, float weight)
            {
                if (index >= 0)
                {
                    renderer.SetBlendShapeWeight(index, Mathf.Clamp(weight, 0f, 100f));
                }
            }

            private static int FindBlendShapeIndex(Mesh mesh, string requiredName)
            {
                if (mesh == null)
                {
                    return -1;
                }

                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    string candidate = mesh.GetBlendShapeName(i);
                    if (string.Equals(candidate, requiredName, StringComparison.Ordinal) ||
                        candidate.EndsWith("." + requiredName, StringComparison.Ordinal))
                    {
                        return i;
                    }
                }

                return -1;
            }
        }
    }
}
