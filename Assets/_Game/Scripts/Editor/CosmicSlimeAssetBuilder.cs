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
        private const string CoreMaterialPath = MaterialFolder + "/MAT_UR_CosmicSlime_Core.mat";
        private const string OrbitMaterialPath = MaterialFolder + "/MAT_UR_CosmicSlime_Orbit.mat";

        public static GameObject EnsureAssets()
        {
            EnsureFolder(MaterialFolder);
            EnsureFolder("Assets/_Game/Prefabs/Characters");
            ConfigureModelImporter();

            Material shell = EnsureMaterial(
                ShellMaterialPath,
                new Color(0.055f, 0.008f, 0.16f, 0.93f),
                new Color(0.16f, 0.018f, 0.48f, 1f) * 0.8f,
                0.08f,
                0.84f,
                true);
            Material core = EnsureMaterial(
                CoreMaterialPath,
                new Color(0.56f, 0.10f, 1f, 1f),
                new Color(0.64f, 0.14f, 1f, 1f) * 4.2f,
                0.05f,
                0.78f,
                false);
            Material orbit = EnsureMaterial(
                OrbitMaterialPath,
                new Color(0.28f, 0.15f, 0.34f, 1f),
                new Color(0.18f, 0.035f, 0.42f, 1f) * 1.15f,
                0.76f,
                0.48f,
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

                Transform modelRoot = new GameObject("ModelRoot").transform;
                modelRoot.SetParent(root.transform, false);
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
                model.name = "UR_CosmicSlime_Model";
                model.transform.SetParent(modelRoot, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;

                AssignMaterials(model, shell, core, orbit);

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
                Transform health = CreateSocket(root.transform, "HealthBar", new Vector3(0f, 1.88f, 0f));

                view.ConfigureSockets(modelRoot, rightHand, leftHand, skill, projectile, ground, target, health);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
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

        private static void AssignMaterials(GameObject model, Material shell, Material core, Material orbit)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                string objectName = renderers[i].name;
                Material selected = objectName.StartsWith("Orbit", StringComparison.Ordinal) ||
                                    objectName == "ForeheadSigilInner" ||
                                    objectName == "SingularityCore"
                    ? orbit
                    : objectName.StartsWith("Eye", StringComparison.Ordinal) ||
                      objectName.StartsWith("CosmicDroplet", StringComparison.Ordinal) ||
                      objectName == "ForeheadSigil" ||
                      objectName == "SingularityAccretion"
                        ? core
                        : shell;
                Material[] slots = renderers[i].sharedMaterials;
                for (int slot = 0; slot < slots.Length; slot++)
                {
                    slots[slot] = selected;
                }

                renderers[i].sharedMaterials = slots;
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
