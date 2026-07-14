using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GenericGachaRPG
{
    public enum ProceduralCharacterArchetype
    {
        Vanguard,
        Striker,
        Mystic,
        Seeded
    }

    /// <summary>
    /// Creates a colorful original placeholder fighter entirely from Unity
    /// primitives. The generated hierarchy follows the same socket contract as
    /// future authored character prefabs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProceduralCharacterBuilder : MonoBehaviour
    {
        [Header("Build")]
        [SerializeField] private bool buildOnAwake = true;
        [SerializeField] private int styleSeed = 1;
        [SerializeField] private ProceduralCharacterArchetype archetype = ProceduralCharacterArchetype.Seeded;
        [Range(0.65f, 1.45f)] [SerializeField] private float characterScale = 1f;

        [Header("Palette")]
        [SerializeField] private bool derivePaletteFromSeed;
        [SerializeField] private Color primaryColor = new Color(0.18f, 0.48f, 0.98f, 1f);
        [SerializeField] private Color accentColor = new Color(1f, 0.42f, 0.15f, 1f);
        [SerializeField] private Color skinColor = new Color(1f, 0.72f, 0.52f, 1f);

        private readonly List<Material> runtimeMaterials = new List<Material>();
        private Transform generatedModelRoot;
        private Transform generatedGroundSocket;
        private Transform generatedTargetSocket;
        private Transform generatedHealthBarSocket;

        public CharacterView View { get; private set; }

        private void Awake()
        {
            if (buildOnAwake)
            {
                Build();
            }
        }

        private void OnDestroy()
        {
            DestroyRuntimeMaterials();
        }

        /// <summary>Configures the next build without requiring serialized assets.</summary>
        public void Configure(
            int seed,
            Color primary,
            Color accent,
            ProceduralCharacterArchetype newArchetype = ProceduralCharacterArchetype.Seeded,
            float scale = 1f)
        {
            styleSeed = seed;
            primaryColor = primary;
            accentColor = accent;
            archetype = newArchetype;
            characterScale = Mathf.Clamp(scale, 0.65f, 1.45f);
            derivePaletteFromSeed = false;
        }

        /// <summary>
        /// Convenience factory for scene bootstrap code. The returned view is
        /// ready to animate and contains no dependency on prefabs or asset files.
        /// </summary>
        public static CharacterView Create(
            string characterName,
            Transform parent,
            Vector3 worldPosition,
            Quaternion worldRotation,
            Color primary,
            Color accent,
            int seed = 1,
            ProceduralCharacterArchetype newArchetype = ProceduralCharacterArchetype.Seeded,
            float scale = 1f)
        {
            var host = new GameObject(string.IsNullOrWhiteSpace(characterName) ? "ProceduralCharacter" : characterName);
            host.SetActive(false);
            if (parent != null)
            {
                host.transform.SetParent(parent, true);
            }

            host.transform.SetPositionAndRotation(worldPosition, worldRotation);
            var builder = host.AddComponent<ProceduralCharacterBuilder>();
            builder.buildOnAwake = false;
            builder.Configure(seed, primary, accent, newArchetype, scale);
            CharacterView result = builder.Build();
            host.SetActive(true);
            return result;
        }

        /// <summary>Destroys the previous generated model and creates a fresh one.</summary>
        public CharacterView Build()
        {
            ClearGeneratedHierarchy();
            DestroyRuntimeMaterials();

            Color primary = primaryColor;
            Color accent = accentColor;
            Color skin = skinColor;
            if (derivePaletteFromSeed)
            {
                DerivePalette(styleSeed, out primary, out accent, out skin);
            }

            Material primaryMaterial = CreateMaterial("Primary", primary, 0.12f, 0.48f);
            Material accentMaterial = CreateMaterial("Accent", accent, 0.05f, 0.66f);
            Material skinMaterial = CreateMaterial("Skin", skin, 0f, 0.42f);
            Material darkMaterial = CreateMaterial("Details", new Color(0.035f, 0.045f, 0.075f, 1f), 0.05f, 0.35f);
            Color glowColor = Color.Lerp(accent, Color.white, 0.24f);
            Material glowMaterial = CreateMaterial("Glow", glowColor, 0f, 0.8f, true);

            generatedModelRoot = CreateSocket("ModelRoot", transform, new Vector3(0f, 0f, 0f));
            generatedModelRoot.localScale = Vector3.one * characterScale;
            generatedGroundSocket = CreateSocket("GroundVfx", transform, new Vector3(0f, 0.025f, 0f));
            generatedTargetSocket = CreateSocket("Target", transform, new Vector3(0f, 1.13f * characterScale, 0f));
            generatedHealthBarSocket = CreateSocket("HealthBar", transform, new Vector3(0f, 2.42f * characterScale, 0f));

            // Legs and boots.
            CreateLimb("LeftLeg", generatedModelRoot, new Vector3(-0.22f, 0.72f, 0f), new Vector3(-0.27f, 0.18f, 0.015f), 0.105f, primaryMaterial);
            CreateLimb("RightLeg", generatedModelRoot, new Vector3(0.22f, 0.72f, 0f), new Vector3(0.27f, 0.18f, 0.015f), 0.105f, primaryMaterial);
            CreatePart("LeftBoot", PrimitiveType.Cube, generatedModelRoot, new Vector3(-0.28f, 0.13f, 0.08f), new Vector3(0.28f, 0.16f, 0.42f), Quaternion.identity, darkMaterial);
            CreatePart("RightBoot", PrimitiveType.Cube, generatedModelRoot, new Vector3(0.28f, 0.13f, 0.08f), new Vector3(0.28f, 0.16f, 0.42f), Quaternion.identity, darkMaterial);

            // Torso, belt, shoulder line and chest emblem.
            CreatePart("Torso", PrimitiveType.Capsule, generatedModelRoot, new Vector3(0f, 1.18f, 0f), new Vector3(0.37f, 0.36f, 0.25f), Quaternion.identity, primaryMaterial);
            CreatePart("Belt", PrimitiveType.Cube, generatedModelRoot, new Vector3(0f, 0.91f, 0f), new Vector3(0.63f, 0.12f, 0.42f), Quaternion.identity, darkMaterial);
            CreatePart("ChestStripe", PrimitiveType.Cube, generatedModelRoot, new Vector3(0f, 1.25f, 0.242f), new Vector3(0.42f, 0.12f, 0.055f), Quaternion.Euler(0f, 0f, -12f), accentMaterial);
            CreatePart("ChestCore", PrimitiveType.Sphere, generatedModelRoot, new Vector3(0f, 1.38f, 0.265f), new Vector3(0.15f, 0.15f, 0.08f), Quaternion.identity, glowMaterial);

            Transform rightHand = CreateSocket("RightHand", generatedModelRoot, new Vector3(0.69f, 1.13f, 0f));
            Transform leftHand = CreateSocket("LeftHand", generatedModelRoot, new Vector3(-0.69f, 1.13f, 0f));
            CreateLimb("RightArm", generatedModelRoot, new Vector3(0.31f, 1.43f, 0f), rightHand.localPosition, 0.09f, primaryMaterial);
            CreateLimb("LeftArm", generatedModelRoot, new Vector3(-0.31f, 1.43f, 0f), leftHand.localPosition, 0.09f, primaryMaterial);
            CreatePart("RightHandMesh", PrimitiveType.Sphere, rightHand, Vector3.zero, Vector3.one * 0.205f, Quaternion.identity, skinMaterial);
            CreatePart("LeftHandMesh", PrimitiveType.Sphere, leftHand, Vector3.zero, Vector3.one * 0.205f, Quaternion.identity, skinMaterial);

            // Head, face, and asymmetric hair make the silhouette readable at mobile scale.
            CreatePart("Head", PrimitiveType.Sphere, generatedModelRoot, new Vector3(0f, 1.86f, 0f), new Vector3(0.52f, 0.56f, 0.5f), Quaternion.identity, skinMaterial);
            CreatePart("LeftEye", PrimitiveType.Sphere, generatedModelRoot, new Vector3(-0.105f, 1.91f, 0.235f), new Vector3(0.065f, 0.09f, 0.045f), Quaternion.identity, darkMaterial);
            CreatePart("RightEye", PrimitiveType.Sphere, generatedModelRoot, new Vector3(0.105f, 1.91f, 0.235f), new Vector3(0.065f, 0.09f, 0.045f), Quaternion.identity, darkMaterial);
            CreatePart("Brow", PrimitiveType.Cube, generatedModelRoot, new Vector3(0f, 2.025f, 0.232f), new Vector3(0.29f, 0.045f, 0.04f), Quaternion.Euler(0f, 0f, -5f), darkMaterial);
            CreatePart("HairTop", PrimitiveType.Cube, generatedModelRoot, new Vector3(-0.04f, 2.13f, -0.015f), new Vector3(0.48f, 0.20f, 0.45f), Quaternion.Euler(0f, 0f, -8f), darkMaterial);
            CreatePart("HairSpike", PrimitiveType.Cube, generatedModelRoot, new Vector3(0.17f, 2.27f, -0.02f), new Vector3(0.13f, 0.32f, 0.18f), Quaternion.Euler(0f, 0f, -25f), accentMaterial);

            ProceduralCharacterArchetype resolvedArchetype = archetype == ProceduralCharacterArchetype.Seeded
                ? (ProceduralCharacterArchetype)Mathf.Abs(styleSeed % 3)
                : archetype;
            AddArchetypeDetails(resolvedArchetype, rightHand, leftHand, primaryMaterial, accentMaterial, darkMaterial, glowMaterial);

            Transform skillVfx = CreateSocket("SkillVfx", generatedModelRoot, new Vector3(0f, 1.34f, 0.36f));
            Transform projectile = CreateSocket("Projectile", rightHand, new Vector3(0f, 0.1f, 0.16f));

            View = GetComponent<CharacterView>();
            if (View == null)
            {
                View = gameObject.AddComponent<CharacterView>();
            }

            View.ConfigureSockets(
                generatedModelRoot,
                rightHand,
                leftHand,
                skillVfx,
                projectile,
                generatedGroundSocket,
                generatedTargetSocket,
                generatedHealthBarSocket,
                GetComponentInChildren<Animator>());
            return View;
        }

        private void AddArchetypeDetails(
            ProceduralCharacterArchetype resolvedArchetype,
            Transform rightHand,
            Transform leftHand,
            Material primaryMaterial,
            Material accentMaterial,
            Material darkMaterial,
            Material glowMaterial)
        {
            switch (resolvedArchetype)
            {
                case ProceduralCharacterArchetype.Vanguard:
                    CreatePart("BladeGrip", PrimitiveType.Cylinder, rightHand, new Vector3(0f, 0.05f, 0f), new Vector3(0.07f, 0.20f, 0.07f), Quaternion.identity, darkMaterial);
                    CreatePart("EnergyBlade", PrimitiveType.Cube, rightHand, new Vector3(0f, 0.58f, 0f), new Vector3(0.11f, 0.92f, 0.075f), Quaternion.Euler(0f, 0f, -8f), glowMaterial);
                    CreatePart("LeftShoulderGuard", PrimitiveType.Sphere, generatedModelRoot, new Vector3(-0.37f, 1.47f, 0f), new Vector3(0.34f, 0.23f, 0.36f), Quaternion.identity, accentMaterial);
                    break;

                case ProceduralCharacterArchetype.Striker:
                    CreatePart("RightGauntlet", PrimitiveType.Cube, rightHand, new Vector3(0f, 0f, 0.04f), new Vector3(0.31f, 0.28f, 0.36f), Quaternion.Euler(8f, 0f, 0f), accentMaterial);
                    CreatePart("LeftGauntlet", PrimitiveType.Cube, leftHand, new Vector3(0f, 0f, 0.04f), new Vector3(0.31f, 0.28f, 0.36f), Quaternion.Euler(8f, 0f, 0f), accentMaterial);
                    CreatePart("BackFin", PrimitiveType.Cube, generatedModelRoot, new Vector3(0f, 1.34f, -0.24f), new Vector3(0.16f, 0.72f, 0.17f), Quaternion.Euler(0f, 0f, 34f), primaryMaterial);
                    break;

                case ProceduralCharacterArchetype.Mystic:
                    CreatePart("FocusOrb", PrimitiveType.Sphere, leftHand, new Vector3(0f, 0.13f, 0.06f), Vector3.one * 0.34f, Quaternion.identity, glowMaterial);
                    CreatePart("CrownLeft", PrimitiveType.Cube, generatedModelRoot, new Vector3(-0.19f, 2.24f, 0f), new Vector3(0.10f, 0.34f, 0.12f), Quaternion.Euler(0f, 0f, 19f), accentMaterial);
                    CreatePart("CrownRight", PrimitiveType.Cube, generatedModelRoot, new Vector3(0.19f, 2.24f, 0f), new Vector3(0.10f, 0.34f, 0.12f), Quaternion.Euler(0f, 0f, -19f), accentMaterial);
                    CreatePart("BackFocus", PrimitiveType.Cylinder, generatedModelRoot, new Vector3(0f, 1.45f, -0.30f), new Vector3(0.46f, 0.035f, 0.46f), Quaternion.Euler(90f, 0f, 0f), glowMaterial);
                    break;
            }
        }

        private GameObject CreateLimb(
            string partName,
            Transform parent,
            Vector3 start,
            Vector3 end,
            float radius,
            Material material)
        {
            Vector3 direction = end - start;
            float length = direction.magnitude;
            Quaternion rotation = length > 0.0001f
                ? Quaternion.FromToRotation(Vector3.up, direction.normalized)
                : Quaternion.identity;
            return CreatePart(
                partName,
                PrimitiveType.Cylinder,
                parent,
                Vector3.Lerp(start, end, 0.5f),
                new Vector3(radius, length * 0.5f, radius),
                rotation,
                material);
        }

        private GameObject CreatePart(
            string partName,
            PrimitiveType primitiveType,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material)
        {
            GameObject part = GameObject.CreatePrimitive(primitiveType);
            part.name = partName;
            part.layer = gameObject.layer;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation;
            part.transform.localScale = localScale;

            Collider generatedCollider = part.GetComponent<Collider>();
            if (generatedCollider != null)
            {
                generatedCollider.enabled = false;
                Destroy(generatedCollider);
            }

            Renderer partRenderer = part.GetComponent<Renderer>();
            if (partRenderer != null)
            {
                partRenderer.sharedMaterial = material;
                partRenderer.shadowCastingMode = ShadowCastingMode.On;
                partRenderer.receiveShadows = true;
            }

            return part;
        }

        private static Transform CreateSocket(string socketName, Transform parent, Vector3 localPosition)
        {
            var socket = new GameObject(socketName).transform;
            socket.SetParent(parent, false);
            socket.localPosition = localPosition;
            socket.localRotation = Quaternion.identity;
            socket.localScale = Vector3.one;
            return socket;
        }

        private Material CreateMaterial(
            string materialName,
            Color color,
            float metallic,
            float smoothness,
            bool emission = false)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                shader = Shader.Find("Hidden/InternalErrorShader");
            }

            var material = new Material(shader)
            {
                name = $"Runtime_{name}_{materialName}",
                color = color
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            if (emission && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.45f);
            }

            runtimeMaterials.Add(material);
            return material;
        }

        private void ClearGeneratedHierarchy()
        {
            if (generatedModelRoot != null)
            {
                Destroy(generatedModelRoot.gameObject);
            }

            if (generatedGroundSocket != null)
            {
                Destroy(generatedGroundSocket.gameObject);
            }

            if (generatedTargetSocket != null)
            {
                Destroy(generatedTargetSocket.gameObject);
            }

            if (generatedHealthBarSocket != null)
            {
                Destroy(generatedHealthBarSocket.gameObject);
            }

            generatedModelRoot = null;
            generatedGroundSocket = null;
            generatedTargetSocket = null;
            generatedHealthBarSocket = null;
        }

        private void DestroyRuntimeMaterials()
        {
            foreach (Material runtimeMaterial in runtimeMaterials)
            {
                if (runtimeMaterial != null)
                {
                    Destroy(runtimeMaterial);
                }
            }

            runtimeMaterials.Clear();
        }

        private static void DerivePalette(int seed, out Color primary, out Color accent, out Color skin)
        {
            float absoluteSeed = Mathf.Abs((float)seed);
            float hue = Mathf.Repeat(absoluteSeed * 0.173f + 0.08f, 1f);
            primary = Color.HSVToRGB(hue, 0.72f, 0.92f);
            accent = Color.HSVToRGB(Mathf.Repeat(hue + 0.43f, 1f), 0.82f, 1f);
            float skinValue = 0.82f + Mathf.Repeat(absoluteSeed * 0.071f, 0.15f);
            skin = Color.HSVToRGB(0.065f, 0.36f, skinValue);
        }
    }
}
