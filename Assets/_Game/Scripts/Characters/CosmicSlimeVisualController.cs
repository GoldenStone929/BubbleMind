using System.Collections.Generic;
using UnityEngine;

namespace GenericGachaRPG
{
    [DisallowMultipleComponent]
    public sealed class CosmicSlimeVisualController : MonoBehaviour
    {
        [SerializeField] private float lowerOrbitSpeed = 12f;
        [SerializeField] private float upperOrbitSpeed = -17f;
        [SerializeField] private float corePulseSpeed = 2.4f;
        [SerializeField] private float corePulseAmount = 0.055f;

        private readonly List<Transform> lowerOrbit = new List<Transform>();
        private readonly List<Transform> upperOrbit = new List<Transform>();
        private Transform core;
        private Vector3 coreBaseScale = Vector3.one;
        private float phase;

        private void Awake()
        {
            Transform[] descendants = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                Transform item = descendants[i];
                if (item.name.StartsWith("OrbitRing_A", System.StringComparison.Ordinal))
                {
                    lowerOrbit.Add(item);
                }
                else if (item.name.StartsWith("OrbitRing_B", System.StringComparison.Ordinal))
                {
                    upperOrbit.Add(item);
                }
                else if (item.name == "SingularityAccretion")
                {
                    core = item;
                    coreBaseScale = item.localScale;
                }
            }

            phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            float delta = Time.deltaTime;
            Rotate(lowerOrbit, lowerOrbitSpeed * delta);
            Rotate(upperOrbit, upperOrbitSpeed * delta);

            if (core != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * corePulseSpeed + phase) * corePulseAmount;
                core.localScale = coreBaseScale * pulse;
            }
        }

        private static void Rotate(List<Transform> transforms, float degrees)
        {
            for (int i = 0; i < transforms.Count; i++)
            {
                if (transforms[i] != null)
                {
                    transforms[i].Rotate(0f, degrees, 0f, Space.Self);
                }
            }
        }
    }
}
