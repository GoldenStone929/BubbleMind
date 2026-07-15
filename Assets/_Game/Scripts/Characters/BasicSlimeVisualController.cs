using System;
using System.Collections.Generic;
using UnityEngine;

namespace GenericGachaRPG
{
    public enum BasicSlimeElement
    {
        Water = 0,
        Fire = 1,
        Earth = 2,
        Wind = 3,
        Lightning = 4
    }

    [DisallowMultipleComponent]
    public sealed class BasicSlimeVisualController : MonoBehaviour
    {
        [SerializeField] private BasicSlimeElement element;
        [Min(0.1f), SerializeField] private float animationSpeed = 1.8f;
        [Range(0f, 0.2f), SerializeField] private float positionAmount = 0.035f;
        [Range(0f, 0.2f), SerializeField] private float scaleAmount = 0.065f;
        [Range(0f, 24f), SerializeField] private float rotationAmount = 9f;

        private readonly List<AnimatedDecoration> decorations = new List<AnimatedDecoration>();
        private float animationStartTime;

        public BasicSlimeElement Element => element;
        public int AnimatedDecorationCount => decorations.Count;

        public void Configure(BasicSlimeElement newElement)
        {
            element = newElement;
            CacheDecorations();
        }

        private void Awake()
        {
            CacheDecorations();
        }

        private void OnEnable()
        {
            if (decorations.Count == 0)
            {
                CacheDecorations();
            }

            animationStartTime = Time.time;
            RestoreDecorations();
        }

        private void OnDisable()
        {
            RestoreDecorations();
        }

        private void Update()
        {
            float elapsed = Mathf.Max(0f, Time.time - animationStartTime) * animationSpeed;
            for (int i = 0; i < decorations.Count; i++)
            {
                float phase = i * 1.618f;
                float wave = Mathf.Sin(elapsed + phase);
                float secondaryWave = Mathf.Sin(elapsed * 1.73f + phase * 0.67f);
                AnimatedDecoration part = decorations[i];

                switch (element)
                {
                    case BasicSlimeElement.Water:
                        part.Apply(
                            Vector3.up * (wave * positionAmount),
                            Vector3.up * (secondaryWave * rotationAmount * 0.45f),
                            1f + secondaryWave * scaleAmount * 0.35f);
                        break;
                    case BasicSlimeElement.Fire:
                        part.Apply(
                            Vector3.up * (Mathf.Abs(wave) * positionAmount * 0.55f),
                            Vector3.forward * (secondaryWave * rotationAmount * 0.7f),
                            1f + wave * scaleAmount);
                        break;
                    case BasicSlimeElement.Earth:
                        part.Apply(
                            Vector3.up * (wave * positionAmount * 0.18f),
                            new Vector3(0f, secondaryWave * rotationAmount * 0.25f, wave * rotationAmount),
                            1f + secondaryWave * scaleAmount * 0.12f);
                        break;
                    case BasicSlimeElement.Wind:
                        part.Apply(
                            Vector3.up * (wave * positionAmount * 0.65f),
                            new Vector3(0f, Mathf.Repeat(elapsed * 32f + phase * 18f, 360f), secondaryWave * rotationAmount),
                            1f + wave * scaleAmount * 0.25f);
                        break;
                    case BasicSlimeElement.Lightning:
                        float flicker = Mathf.Sin(elapsed * 3.2f + phase) >= 0f ? 1f : -0.35f;
                        part.Apply(
                            Vector3.up * (secondaryWave * positionAmount * 0.3f),
                            Vector3.forward * (flicker * rotationAmount * 0.45f),
                            1f + flicker * scaleAmount * 0.55f);
                        break;
                    default:
                        part.Restore();
                        break;
                }
            }
        }

        private void CacheDecorations()
        {
            RestoreDecorations();
            decorations.Clear();

            Transform[] descendants = GetComponentsInChildren<Transform>(true);
            var selected = new HashSet<Transform>();
            for (int i = 0; i < descendants.Length; i++)
            {
                Transform candidate = descendants[i];
                if (candidate == transform || !IsDecorationName(candidate.name, element))
                {
                    continue;
                }

                if (HasSelectedAncestor(candidate.parent, selected))
                {
                    continue;
                }

                selected.Add(candidate);
                decorations.Add(new AnimatedDecoration(candidate));
            }
        }

        private static bool HasSelectedAncestor(Transform candidate, HashSet<Transform> selected)
        {
            while (candidate != null)
            {
                if (selected.Contains(candidate))
                {
                    return true;
                }

                candidate = candidate.parent;
            }

            return false;
        }

        private static bool IsDecorationName(string objectName, BasicSlimeElement slimeElement)
        {
            if (objectName.StartsWith("Element_", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            switch (slimeElement)
            {
                case BasicSlimeElement.Water:
                    return StartsWithAny(objectName, "Bubble_", "Drop_", "Water_");
                case BasicSlimeElement.Fire:
                    return StartsWithAny(objectName, "Flame_", "Fire_");
                case BasicSlimeElement.Earth:
                    return StartsWithAny(objectName, "Leaf_", "Rock_", "Stone_", "Earth_");
                case BasicSlimeElement.Wind:
                    return StartsWithAny(objectName, "Wind_", "Ribbon_", "Swirl_");
                case BasicSlimeElement.Lightning:
                    return StartsWithAny(objectName, "Spark_", "Bolt_", "Lightning_");
                default:
                    return false;
            }
        }

        private static bool StartsWithAny(string value, params string[] prefixes)
        {
            for (int i = 0; i < prefixes.Length; i++)
            {
                if (value.StartsWith(prefixes[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void RestoreDecorations()
        {
            for (int i = 0; i < decorations.Count; i++)
            {
                decorations[i].Restore();
            }
        }

        private sealed class AnimatedDecoration
        {
            private readonly Transform target;
            private readonly Vector3 baseLocalPosition;
            private readonly Quaternion baseLocalRotation;
            private readonly Vector3 baseLocalScale;

            public AnimatedDecoration(Transform target)
            {
                this.target = target;
                baseLocalPosition = target.localPosition;
                baseLocalRotation = target.localRotation;
                baseLocalScale = target.localScale;
            }

            public void Apply(Vector3 localPositionOffset, Vector3 localEulerOffset, float scaleMultiplier)
            {
                if (target == null)
                {
                    return;
                }

                target.localPosition = baseLocalPosition + localPositionOffset;
                target.localRotation = baseLocalRotation * Quaternion.Euler(localEulerOffset);
                target.localScale = baseLocalScale * Mathf.Max(0.01f, scaleMultiplier);
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
    }
}
