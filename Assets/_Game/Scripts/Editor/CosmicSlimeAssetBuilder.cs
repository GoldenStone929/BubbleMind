using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GenericGachaRPG.Editor
{
    public static class CosmicSlimeAssetBuilder
    {
        public const string ModelPath = "Assets/_Game/Art/Generated/UR_CosmicSlime/Runtime/UR_CosmicSlime.fbx";
        public const string PrefabPath = "Assets/_Game/Prefabs/Characters/PF_UR_CosmicSlime.prefab";

        private const string MaterialFolder = "Assets/_Game/Art/Generated/UR_CosmicSlime/Runtime/Materials";
        private const string ShellMaterialPath = MaterialFolder + "/MAT_UR_CosmicSlime_Shell.mat";
        private const string NebulaMaterialPath = MaterialFolder + "/MAT_UR_CosmicSlime_Nebula.mat";
        private const string CoreMaterialPath = MaterialFolder + "/MAT_UR_CosmicSlime_Core.mat";
        private const string BlackCoreMaterialPath = MaterialFolder + "/MAT_UR_CosmicSlime_BlackCore.mat";
        private const string OrbitMaterialPath = MaterialFolder + "/MAT_UR_CosmicSlime_Orbit.mat";
        private const string OrbitTrimMaterialPath = MaterialFolder + "/MAT_UR_CosmicSlime_OrbitTrim.mat";
        private const string SlimeToonShaderName = "BubbleMind/Slime Toon";
        private const string BlackHoleVfxShaderName = "BubbleMind/Black Hole VFX";

        public static GameObject EnsureAssets()
        {
            EnsureFolder(MaterialFolder);
            EnsureFolder("Assets/_Game/Prefabs/Characters");
            ConfigureModelImporter();

            Material shell = EnsureSlimeMaterial(ShellMaterialPath, false);
            Material nebula = EnsureSlimeMaterial(NebulaMaterialPath, true);
            Material core = EnsureMaterial(
                CoreMaterialPath,
                new Color(0.80f, 0.46f, 1f, 1f),
                new Color(0.68f, 0.15f, 1f, 1f) * 5.4f,
                0.02f,
                0.90f,
                false);
            Material blackCore = EnsureMaterial(
                BlackCoreMaterialPath,
                Color.black,
                Color.black,
                0f,
                0.02f,
                false);
            Material orbit = EnsureMaterial(
                OrbitMaterialPath,
                new Color(0.030f, 0.006f, 0.068f, 1f),
                new Color(0.09f, 0.008f, 0.22f, 1f) * 0.24f,
                0.88f,
                0.78f,
                false);
            Material orbitTrim = EnsureMaterial(
                OrbitTrimMaterialPath,
                new Color(0.34f, 0.18f, 0.052f, 1f),
                new Color(0.12f, 0.032f, 0.005f, 1f) * 0.12f,
                0.92f,
                0.74f,
                false);

            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (modelAsset == null)
            {
                throw new InvalidOperationException($"Cosmic Slime FBX is missing at '{ModelPath}'.");
            }

            GameObject root = new GameObject("PF_UR_CosmicSlime");
            try
            {
                CharacterView view = root.AddComponent<CharacterView>();
                root.AddComponent<CosmicSlimeVisualController>();
                CatherineSkillVfxController skillVfxController = root.AddComponent<CatherineSkillVfxController>();

                Transform modelRoot = new GameObject("ModelRoot").transform;
                modelRoot.SetParent(root.transform, false);
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
                if (PrefabUtility.IsPartOfPrefabInstance(model))
                {
                    PrefabUtility.UnpackPrefabInstance(
                        model,
                        PrefabUnpackMode.Completely,
                        InteractionMode.AutomatedAction);
                }

                model.name = "UR_CosmicSlime_Model";
                model.transform.SetParent(modelRoot, false);
                model.transform.localPosition = Vector3.zero;
                // The battle camera is side-on, so a slight three-quarter presentation keeps the facial singularity readable.
                model.transform.localRotation = Quaternion.Euler(0f, -34f, 0f);
                model.transform.localScale = Vector3.one * 0.92f;

                ConvertBlendShapeMeshes(model);
                AssignMaterials(model, shell, nebula, core, blackCore, orbit, orbitTrim);
                ValidateAnimatedHierarchy(model, blackCore);
                ValidateBlendShapes(model);

                Transform rightHand = FindDescendant(model.transform, "RightHandSocket") ??
                                      CreateSocket(modelRoot, "RightHand", new Vector3(0.72f, 0.68f, 0f));
                Transform leftHand = FindDescendant(model.transform, "LeftHandSocket") ??
                                     CreateSocket(modelRoot, "LeftHand", new Vector3(-0.72f, 0.68f, 0f));
                Transform skill = FindDescendant(model.transform, "SkillVfxSocket") ??
                                  CreateSocket(modelRoot, "SkillVfx", new Vector3(0f, 0.62f, -0.72f));
                Transform projectile = FindDescendant(model.transform, "ProjectileSocket") ??
                                       CreateSocket(modelRoot, "Projectile", new Vector3(0f, 0.76f, -0.78f));
                Transform ground = CreateSocket(root.transform, "GroundVfx", new Vector3(0f, 0.03f, 0f));
                Transform target = CreateSocket(root.transform, "Target", new Vector3(0f, 0.95f, 0f));
                Transform health = CreateSocket(root.transform, "HealthBar", new Vector3(0f, 2.35f, 0f));

                view.ConfigureSockets(modelRoot, rightHand, leftHand, skill, projectile, ground, target, health);
                Shader vfxShader = Shader.Find(BlackHoleVfxShaderName);
                if (vfxShader == null)
                {
                    throw new InvalidOperationException($"Required shader '{BlackHoleVfxShaderName}' is unavailable.");
                }

                skillVfxController.Configure(view.SkillVfx, view.ModelRoot, null, vfxShader);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        public static void LogModelDiagnostics()
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                Debug.LogError($"[BubbleMind] Missing model at '{ModelPath}'.");
                return;
            }

            Component[] components = model.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] is SkinnedMeshRenderer skinned)
                {
                    Mesh mesh = skinned.sharedMesh;
                    Debug.Log($"[BubbleMind] SKINNED {skinned.name}: mesh={mesh?.name}, shapes={DescribeBlendShapes(mesh)}");
                }
                else if (components[i] is MeshFilter filter)
                {
                    Mesh mesh = filter.sharedMesh;
                    Debug.Log($"[BubbleMind] FILTER {filter.name}: mesh={mesh?.name}, shapes={DescribeBlendShapes(mesh)}");
                }
            }
        }

        private static string DescribeBlendShapes(Mesh mesh)
        {
            if (mesh == null || mesh.blendShapeCount == 0)
            {
                return "none";
            }

            string[] names = new string[mesh.blendShapeCount];
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                names[i] = mesh.GetBlendShapeName(i);
            }

            return string.Join(",", names);
        }

        private static void ConfigureModelImporter()
        {
            ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"ModelImporter is unavailable for '{ModelPath}'.");
            }

            bool changed = importer.importAnimation ||
                           importer.animationType != ModelImporterAnimationType.None ||
                           !importer.preserveHierarchy ||
                           importer.isReadable ||
                           importer.addCollider ||
                           importer.materialImportMode != ModelImporterMaterialImportMode.None ||
                           importer.importNormals != ModelImporterNormals.Import ||
                           importer.importTangents != ModelImporterTangents.CalculateMikk;
            if (!changed)
            {
                return;
            }

            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.preserveHierarchy = true;
            importer.isReadable = false;
            importer.addCollider = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.SaveAndReimport();
        }

        private static Material EnsureMaterial(
            string path,
            Color baseColor,
            Color emission,
            float metallic,
            float smoothness,
            bool transparent)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            SetColor(material, "_BaseColor", "_Color", baseColor);
            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", emission);
                material.EnableKeyword("_EMISSION");
            }

            ConfigureSurface(material, transparent);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureSlimeMaterial(string path, bool internalLayer)
        {
            Shader shader = Shader.Find(SlimeToonShaderName);
            if (shader == null)
            {
                throw new InvalidOperationException($"Required shader '{SlimeToonShaderName}' is unavailable.");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_TopColor", internalLayer
                ? new Color(0.25f, 0.050f, 0.55f, 1f)
                : new Color(0.075f, 0.014f, 0.17f, 1f));
            material.SetColor("_BottomColor", internalLayer
                ? new Color(0.032f, 0.003f, 0.095f, 1f)
                : new Color(0.006f, 0.001f, 0.020f, 1f));
            material.SetColor("_ShadowColor", internalLayer
                ? new Color(0.18f, 0.065f, 0.34f, 1f)
                : new Color(0.10f, 0.028f, 0.18f, 1f));
            material.SetColor("_InnerColor", internalLayer
                ? new Color(0.46f, 0.12f, 0.86f, 1f)
                : new Color(0.22f, 0.035f, 0.48f, 1f));
            material.SetColor("_FresnelColor", internalLayer
                ? new Color(0.72f, 0.32f, 1f, 1f)
                : new Color(0.55f, 0.30f, 0.92f, 1f));
            material.SetColor("_HighlightColor", new Color(1f, 0.91f, 1f, 1f));
            material.SetColor("_AbsorptionColor", internalLayer
                ? new Color(0.080f, 0.008f, 0.22f, 1f)
                : new Color(0.035f, 0.002f, 0.090f, 1f));
            material.SetColor("_NebulaColorA", internalLayer
                ? new Color(0.24f, 0.030f, 0.64f, 1f)
                : new Color(0.16f, 0.012f, 0.42f, 1f));
            material.SetColor("_NebulaColorB", internalLayer
                ? new Color(0.76f, 0.12f, 1f, 1f)
                : new Color(0.50f, 0.055f, 0.82f, 1f));
            material.SetColor("_StarColor", internalLayer
                ? new Color(0.70f, 0.84f, 1f, 1f)
                : new Color(0.88f, 0.72f, 1f, 1f));
            material.SetFloat("_GradientOffset", internalLayer ? 0.30f : 0.38f);
            material.SetFloat("_GradientScale", internalLayer ? 1.10f : 0.92f);
            material.SetFloat("_ShadowThreshold", internalLayer ? 0.44f : 0.50f);
            material.SetFloat("_ShadowSoftness", 0.095f);
            material.SetFloat("_InnerStrength", internalLayer ? 0.62f : 0.38f);
            material.SetFloat("_ThicknessStrength", internalLayer ? 0.58f : 0.48f);
            material.SetFloat("_FresnelPower", internalLayer ? 2.4f : 3.1f);
            material.SetFloat("_FresnelStrength", internalLayer ? 0.72f : 0.58f);
            material.SetFloat("_HighlightThreshold", internalLayer ? 0.82f : 0.86f);
            material.SetFloat("_HighlightSoftness", 0.065f);
            material.SetFloat("_HighlightStrength", internalLayer ? 0.88f : 0.94f);
            material.SetFloat("_Realism", internalLayer ? 0.74f : 0.86f);
            material.SetFloat("_Roughness", internalLayer ? 0.28f : 0.17f);
            material.SetFloat("_CoatStrength", internalLayer ? 0.42f : 0.92f);
            material.SetFloat("_CoatRoughness", internalLayer ? 0.24f : 0.12f);
            material.SetFloat("_MicroSurfaceScale", internalLayer ? 15f : 21f);
            material.SetFloat("_MicroSurfaceStrength", internalLayer ? 0.16f : 0.10f);
            material.SetFloat("_AbsorptionStrength", internalLayer ? 0.48f : 0.64f);
            material.SetFloat("_TransmissionStrength", internalLayer ? 0.96f : 0.82f);
            material.SetFloat("_NebulaScale", internalLayer ? 4.5f : 3.6f);
            material.SetFloat("_NebulaStrength", internalLayer ? 0.82f : 0.58f);
            material.SetFloat("_NebulaSpeed", internalLayer ? 0.24f : 0.14f);
            material.SetFloat("_StarDensity", internalLayer ? 0.22f : 0.10f);
            material.SetFloat("_StarScale", internalLayer ? 25f : 19f);
            material.SetFloat("_StarStrength", internalLayer ? 1.45f : 0.82f);
            material.SetFloat("_StarSpeed", internalLayer ? 2.3f : 1.6f);
            material.SetFloat("_Opacity", internalLayer ? 0.94f : 1f);
            material.SetFloat("_VfxDarken", 0f);
            material.SetFloat("_VfxDissolve", 0f);
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHAMODULATE_ON");
            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.renderQueue = (int)RenderQueue.AlphaTest;
            material.SetShaderPassEnabled("ShadowCaster", true);
            material.SetShaderPassEnabled("DepthOnly", true);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureSurface(Material material, bool transparent)
        {
            if (!material.HasProperty("_Surface"))
            {
                material.renderQueue = transparent ? (int)RenderQueue.Transparent : -1;
                return;
            }

            material.SetFloat("_Surface", transparent ? 1f : 0f);
            material.SetFloat("_ZWrite", transparent ? 0f : 1f);
            if (transparent)
            {
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = -1;
            }
        }

        private static void SetColor(Material material, string preferred, string fallback, Color color)
        {
            if (material.HasProperty(preferred))
            {
                material.SetColor(preferred, color);
            }
            else if (material.HasProperty(fallback))
            {
                material.SetColor(fallback, color);
            }
        }

        private static void AssignMaterials(
            GameObject model,
            Material shell,
            Material nebula,
            Material core,
            Material blackCore,
            Material orbit,
            Material orbitTrim)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                string objectName = renderers[i].name;
                Material selected;
                if (objectName.StartsWith("OrbitTrim", StringComparison.Ordinal))
                {
                    selected = orbitTrim;
                }
                else if (objectName.StartsWith("OrbitBand", StringComparison.Ordinal))
                {
                    selected = orbit;
                }
                else if (objectName == "NebulaInner" ||
                         objectName.StartsWith("CosmicDroplet", StringComparison.Ordinal))
                {
                    selected = nebula;
                }
                else if (objectName == "SingularityCore" || objectName == "ForeheadSigilInner")
                {
                    selected = blackCore;
                }
                else if (objectName.StartsWith("Eye", StringComparison.Ordinal) ||
                         objectName.StartsWith("AccretionSpiral", StringComparison.Ordinal) ||
                         objectName.StartsWith("NebulaVeil", StringComparison.Ordinal) ||
                         objectName == "StarCloudPoints" ||
                         objectName == "ForeheadSigil" ||
                         objectName == "SingularityAccretion" ||
                         objectName == "Horn_Center" ||
                         objectName == "Horn_RightFluid")
                {
                    selected = core;
                }
                else
                {
                    selected = shell;
                }
                Material[] slots = renderers[i].sharedMaterials;
                for (int slot = 0; slot < slots.Length; slot++)
                {
                    slots[slot] = selected;
                }

                renderers[i].sharedMaterials = slots;
            }
        }

        private static void ConvertBlendShapeMeshes(GameObject model)
        {
            MeshFilter[] filters = model.GetComponentsInChildren<MeshFilter>(true);
            int converted = 0;
            for (int i = 0; i < filters.Length; i++)
            {
                MeshFilter filter = filters[i];
                Mesh mesh = filter.sharedMesh;
                MeshRenderer sourceRenderer = filter.GetComponent<MeshRenderer>();
                if (mesh == null || mesh.blendShapeCount == 0 || sourceRenderer == null)
                {
                    continue;
                }

                Material[] materials = sourceRenderer.sharedMaterials;
                GameObject host = filter.gameObject;
                bool rendererEnabled = sourceRenderer.enabled;
                ShadowCastingMode shadowCastingMode = sourceRenderer.shadowCastingMode;
                bool receiveShadows = sourceRenderer.receiveShadows;
                LightProbeUsage lightProbeUsage = sourceRenderer.lightProbeUsage;
                ReflectionProbeUsage reflectionProbeUsage = sourceRenderer.reflectionProbeUsage;
                Transform probeAnchor = sourceRenderer.probeAnchor;
                bool allowOcclusion = sourceRenderer.allowOcclusionWhenDynamic;
                uint renderingLayerMask = sourceRenderer.renderingLayerMask;

                UnityEngine.Object.DestroyImmediate(sourceRenderer);
                UnityEngine.Object.DestroyImmediate(filter);

                SkinnedMeshRenderer skinned = host.AddComponent<SkinnedMeshRenderer>();
                skinned.sharedMesh = mesh;
                skinned.sharedMaterials = materials;
                skinned.localBounds = mesh.bounds;
                skinned.updateWhenOffscreen = true;
                skinned.enabled = rendererEnabled;
                skinned.shadowCastingMode = shadowCastingMode;
                skinned.receiveShadows = receiveShadows;
                skinned.lightProbeUsage = lightProbeUsage;
                skinned.reflectionProbeUsage = reflectionProbeUsage;
                skinned.probeAnchor = probeAnchor;
                skinned.allowOcclusionWhenDynamic = allowOcclusion;
                skinned.renderingLayerMask = renderingLayerMask;
                converted++;
            }

            if (converted < 2)
            {
                throw new InvalidOperationException(
                    $"Cosmic Slime requires two blend-shape renderers; converted {converted}.");
            }
        }

        private static void ValidateAnimatedHierarchy(GameObject model, Material blackCore)
        {
            Transform eventHorizon = FindDescendant(model.transform, "SingularityCore");
            Transform accretionDisk = FindDescendant(model.transform, "SingularityAccretion");
            Transform lowerOrbit = FindDescendant(model.transform, "OrbitRig_Lower");
            Transform upperOrbit = FindDescendant(model.transform, "OrbitRig_Upper");
            if (eventHorizon == null || accretionDisk == null || lowerOrbit == null || upperOrbit == null)
            {
                throw new InvalidOperationException("Cosmic Slime animation hierarchy is incomplete.");
            }

            int spiralCount = 0;
            Transform[] descendants = model.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                if (descendants[i].name.StartsWith("AccretionSpiral_", StringComparison.Ordinal))
                {
                    spiralCount++;
                }
            }

            if (spiralCount < 2)
            {
                throw new InvalidOperationException("Cosmic Slime requires multiple accretion spirals for phased animation.");
            }

            Renderer eventHorizonRenderer = eventHorizon.GetComponent<Renderer>();
            if (eventHorizonRenderer == null || Array.IndexOf(eventHorizonRenderer.sharedMaterials, blackCore) < 0)
            {
                throw new InvalidOperationException("Cosmic Slime event horizon must use the shared black-core material.");
            }

            Color blackBase = blackCore.HasProperty("_BaseColor")
                ? blackCore.GetColor("_BaseColor")
                : blackCore.GetColor("_Color");
            Color blackEmission = blackCore.HasProperty("_EmissionColor")
                ? blackCore.GetColor("_EmissionColor")
                : Color.black;
            if (blackBase.maxColorComponent > 0.001f || blackEmission.maxColorComponent > 0.001f)
            {
                throw new InvalidOperationException("Cosmic Slime event horizon material must remain pure black.");
            }
        }

        private static void ValidateBlendShapes(GameObject model)
        {
            string[] required = { "IdleBreath", "Squash", "Stretch", "UltimateCollapse" };
            SkinnedMeshRenderer[] renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            int completeRendererCount = 0;
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Mesh mesh = renderers[rendererIndex].sharedMesh;
                if (mesh == null || mesh.blendShapeCount == 0)
                {
                    continue;
                }

                bool complete = true;
                for (int requiredIndex = 0; requiredIndex < required.Length; requiredIndex++)
                {
                    bool found = false;
                    for (int shapeIndex = 0; shapeIndex < mesh.blendShapeCount; shapeIndex++)
                    {
                        string shapeName = mesh.GetBlendShapeName(shapeIndex);
                        if (string.Equals(shapeName, required[requiredIndex], StringComparison.Ordinal) ||
                            shapeName.EndsWith("." + required[requiredIndex], StringComparison.Ordinal))
                        {
                            found = true;
                            break;
                        }
                    }

                    complete &= found;
                }

                if (complete)
                {
                    completeRendererCount++;
                }
            }

            if (completeRendererCount == 0)
            {
                throw new InvalidOperationException(
                    "Cosmic Slime FBX must expose IdleBreath, Squash, Stretch, and UltimateCollapse blend shapes.");
            }
        }

        private static Transform CreateSocket(Transform parent, string name, Vector3 localPosition)
        {
            Transform socket = new GameObject(name).transform;
            socket.SetParent(parent, false);
            socket.localPosition = localPosition;
            return socket;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                if (descendants[i].name == name)
                {
                    return descendants[i];
                }
            }

            return null;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
