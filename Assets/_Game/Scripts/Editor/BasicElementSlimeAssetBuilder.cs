using System;
using UnityEditor;
using UnityEngine;

namespace GenericGachaRPG.Editor
{
    public static class BasicElementSlimeAssetBuilder
    {
        public const string RuntimeFolder = "Assets/_Game/Art/Generated/BasicElementSlimes/Runtime";
        public const string PrefabFolder = "Assets/_Game/Prefabs/Characters";

        private const string MaterialFolder = RuntimeFolder + "/Materials";
        private const string FaceMaterialPath = MaterialFolder + "/MAT_BasicSlime_Face.mat";
        private const string EyeWhiteMaterialPath = MaterialFolder + "/MAT_BasicSlime_EyeWhite.mat";
        private const string HighlightMaterialPath = MaterialFolder + "/MAT_BasicSlime_EyeHighlight.mat";

        private static readonly BasicSlimeElement[] Elements =
        {
            BasicSlimeElement.Water,
            BasicSlimeElement.Fire,
            BasicSlimeElement.Earth,
            BasicSlimeElement.Wind,
            BasicSlimeElement.Lightning
        };

        public static void EnsureAssets()
        {
            EnsureFolder(MaterialFolder);
            EnsureFolder(PrefabFolder);

            Material face = EnsureMaterial(
                FaceMaterialPath,
                new Color(0.018f, 0.026f, 0.045f, 1f),
                Color.black,
                0f,
                0.12f);
            Material eyeWhite = EnsureMaterial(
                EyeWhiteMaterialPath,
                new Color(0.955f, 0.97f, 1f, 1f),
                Color.black,
                0f,
                0.16f);
            Material highlight = EnsureMaterial(
                HighlightMaterialPath,
                new Color(0.96f, 0.985f, 1f, 1f),
                new Color(0.76f, 0.91f, 1f, 1f) * 0.28f,
                0f,
                0.22f);

            for (int i = 0; i < Elements.Length; i++)
            {
                BasicSlimeElement element = Elements[i];
                string modelPath = GetModelPath(element);
                ConfigureModelImporter(modelPath);

                GetPalette(
                    element,
                    out Color bodyColor,
                    out Color accentColor,
                    out Color detailColor,
                    out Color accentEmission);
                Material body = EnsureMaterial(
                    GetBodyMaterialPath(element),
                    bodyColor,
                    Color.black,
                    0f,
                    0.18f);
                Material accent = EnsureMaterial(
                    GetAccentMaterialPath(element),
                    accentColor,
                    accentEmission,
                    0f,
                    0.20f);
                Material detail = EnsureMaterial(
                    GetDetailMaterialPath(element),
                    detailColor,
                    Color.black,
                    0f,
                    0.16f);

                BuildPrefab(element, modelPath, body, accent, detail, eyeWhite, face, highlight);
            }

            AssetDatabase.SaveAssets();
        }

        public static BasicSlimeElement[] GetAllElements()
        {
            return (BasicSlimeElement[])Elements.Clone();
        }

        public static GameObject GetPrefab(BasicSlimeElement element)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(GetPrefabPath(element));
        }

        public static bool IsExpectedPrefab(GameObject prefab, BasicSlimeElement element)
        {
            return prefab != null && string.Equals(
                AssetDatabase.GetAssetPath(prefab),
                GetPrefabPath(element),
                StringComparison.Ordinal);
        }

        public static string GetModelPath(BasicSlimeElement element)
        {
            return RuntimeFolder + "/BasicSlime_" + element + ".fbx";
        }

        public static string GetPrefabPath(BasicSlimeElement element)
        {
            return PrefabFolder + "/PF_BasicSlime_" + element + ".prefab";
        }

        private static string GetBodyMaterialPath(BasicSlimeElement element)
        {
            return MaterialFolder + "/MAT_BasicSlime_" + element + "_Body.mat";
        }

        private static string GetAccentMaterialPath(BasicSlimeElement element)
        {
            return MaterialFolder + "/MAT_BasicSlime_" + element + "_Accent.mat";
        }

        private static string GetDetailMaterialPath(BasicSlimeElement element)
        {
            return MaterialFolder + "/MAT_BasicSlime_" + element + "_Detail.mat";
        }

        private static void BuildPrefab(
            BasicSlimeElement element,
            string modelPath,
            Material body,
            Material accent,
            Material detail,
            Material eyeWhite,
            Material face,
            Material highlight)
        {
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelAsset == null)
            {
                throw new InvalidOperationException($"Basic {element} Slime FBX is missing at '{modelPath}'.");
            }

            GameObject root = new GameObject("PF_BasicSlime_" + element);
            root.SetActive(false);
            try
            {
                CharacterView view = root.AddComponent<CharacterView>();
                BasicSlimeVisualController visualController = root.AddComponent<BasicSlimeVisualController>();

                Transform modelRoot = new GameObject("ModelRoot").transform;
                modelRoot.SetParent(root.transform, false);
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
                model.name = "BasicSlime_" + element + "_Model";
                model.transform.SetParent(modelRoot, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;

                AssignMaterials(model, element, body, accent, detail, eyeWhite, face, highlight);
                ValidateModelHierarchy(model, element);

                Transform rightHand = FindDescendant(model.transform, "RightHandSocket") ??
                                      CreateSocket(modelRoot, "RightHand", new Vector3(0.45f, 0.58f, 0.22f));
                Transform leftHand = FindDescendant(model.transform, "LeftHandSocket") ??
                                     CreateSocket(modelRoot, "LeftHand", new Vector3(-0.45f, 0.58f, 0.22f));
                Transform skill = FindDescendant(model.transform, "SkillVfxSocket") ??
                                  CreateSocket(modelRoot, "SkillVfx", new Vector3(0f, 0.72f, 0.42f));
                Transform projectile = FindDescendant(model.transform, "ProjectileSocket") ??
                                       CreateSocket(modelRoot, "Projectile", new Vector3(0f, 0.72f, 0.60f));
                Transform ground = CreateSocket(root.transform, "GroundVfx", new Vector3(0f, 0.03f, 0f));
                Transform target = CreateSocket(root.transform, "Target", new Vector3(0f, 0.72f, 0f));
                Transform health = CreateSocket(root.transform, "HealthBar", new Vector3(0f, 1.72f, 0f));

                view.ConfigureSockets(modelRoot, rightHand, leftHand, skill, projectile, ground, target, health);
                visualController.Configure(element);

                string prefabPath = GetPrefabPath(element);
                root.SetActive(true);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException($"Failed to save Basic {element} Slime prefab at '{prefabPath}'.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            ValidatePrefab(element);
        }

        private static void ConfigureModelImporter(string modelPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"ModelImporter is unavailable for '{modelPath}'.");
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
            float smoothness)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader == null)
                {
                    throw new InvalidOperationException("No supported opaque shader is available for Basic Slime materials.");
                }

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
                if (emission.maxColorComponent > 0.001f)
                {
                    material.EnableKeyword("_EMISSION");
                }
                else
                {
                    material.DisableKeyword("_EMISSION");
                }
            }

            ConfigureOpaqueSurface(material);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureOpaqueSurface(Material material)
        {
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 0f);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 1f);
            }

            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = -1;
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
            BasicSlimeElement element,
            Material body,
            Material accent,
            Material detail,
            Material eyeWhite,
            Material face,
            Material highlight)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material selected = SelectMaterial(
                    renderers[i].name,
                    element,
                    body,
                    accent,
                    detail,
                    eyeWhite,
                    face,
                    highlight);

                Material[] slots = renderers[i].sharedMaterials;
                if (slots == null || slots.Length == 0)
                {
                    renderers[i].sharedMaterial = selected;
                    continue;
                }

                for (int slot = 0; slot < slots.Length; slot++)
                {
                    slots[slot] = selected;
                }

                renderers[i].sharedMaterials = slots;
            }
        }

        private static Material SelectMaterial(
            string objectName,
            BasicSlimeElement element,
            Material body,
            Material accent,
            Material detail,
            Material eyeWhite,
            Material face,
            Material highlight)
        {
            if (StartsWithAny(objectName, "EyeHighlight", "Eye_Highlight", "Highlight_") ||
                objectName.EndsWith("_Highlight", StringComparison.OrdinalIgnoreCase))
            {
                return highlight;
            }

            if (StartsWithAny(objectName, "EyeWhite"))
            {
                return eyeWhite;
            }

            if (StartsWithAny(objectName, "FaceDark", "EyeDark", "Pupil_", "Mouth_", "Brow_"))
            {
                return face;
            }

            if (StartsWithAny(objectName, "EyeIris"))
            {
                return detail;
            }

            if (StartsWithAny(objectName, "Cheek_"))
            {
                return accent;
            }

            switch (element)
            {
                case BasicSlimeElement.Water:
                    return StartsWithAny(objectName, "WaterWave_") ? detail :
                        StartsWithAny(objectName, "WaterCrest", "Bubble_", "Drop_", "Water_") ? accent : body;
                case BasicSlimeElement.Fire:
                    if (StartsWithAny(objectName, "Flame_Left", "Flame_Right"))
                    {
                        return body;
                    }

                    return StartsWithAny(objectName, "Flame_Center") ? detail :
                        StartsWithAny(objectName, "Flame_", "Fire_") ? accent : body;
                case BasicSlimeElement.Earth:
                    return StartsWithAny(objectName, "Leaf_") ? detail :
                        StartsWithAny(objectName, "Rock_", "Stone_", "Earth_") ? accent : body;
                case BasicSlimeElement.Wind:
                    return StartsWithAny(objectName, "Wind_Ribbon", "Ribbon_") ? detail :
                        StartsWithAny(objectName, "Wind_", "Swirl_") ? accent : body;
                case BasicSlimeElement.Lightning:
                    return StartsWithAny(objectName, "Spark_Left", "Spark_Right") ? detail :
                        StartsWithAny(objectName, "Spark_", "Bolt_", "Lightning_") ? accent : body;
                default:
                    return body;
            }
        }

        private static void ValidateModelHierarchy(GameObject model, BasicSlimeElement element)
        {
            if (FindDescendant(model.transform, "SlimeBody") == null)
            {
                throw new InvalidOperationException($"Basic {element} Slime model is missing the required SlimeBody node.");
            }

            Transform[] descendants = model.GetComponentsInChildren<Transform>(true);
            bool hasFace = false;
            bool hasElementDecoration = false;
            for (int i = 0; i < descendants.Length; i++)
            {
                string objectName = descendants[i].name;
                hasFace |= StartsWithAny(objectName, "FaceDark", "EyeDark", "EyeHighlight", "Eye_");
                hasElementDecoration |= IsElementDecorationName(objectName);
            }

            if (!hasFace)
            {
                throw new InvalidOperationException($"Basic {element} Slime model is missing readable face nodes.");
            }

            if (!hasElementDecoration)
            {
                throw new InvalidOperationException($"Basic {element} Slime model is missing an element decoration node.");
            }

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException($"Basic {element} Slime model has no renderers.");
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] materials = renderers[i].sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    throw new InvalidOperationException($"Renderer '{renderers[i].name}' has no material slot.");
                }

                for (int slot = 0; slot < materials.Length; slot++)
                {
                    string materialPath = AssetDatabase.GetAssetPath(materials[slot]);
                    if (materials[slot] == null ||
                        !materialPath.StartsWith(MaterialFolder + "/", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Renderer '{renderers[i].name}' does not use a shared Basic Slime material.");
                    }
                }
            }
        }

        private static void ValidatePrefab(BasicSlimeElement element)
        {
            string path = GetPrefabPath(element);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Basic {element} Slime prefab is missing at '{path}'.");
            }

            CharacterView view = prefab.GetComponent<CharacterView>();
            BasicSlimeVisualController visualController = prefab.GetComponent<BasicSlimeVisualController>();
            if (view == null || visualController == null || visualController.Element != element)
            {
                throw new InvalidOperationException($"Basic {element} Slime prefab root contract is incomplete.");
            }

            if (view.ModelRoot == null ||
                view.GroundVfx == null ||
                view.Target == null ||
                view.HealthBar == null ||
                prefab.GetComponentsInChildren<Renderer>(true).Length == 0)
            {
                throw new InvalidOperationException($"Basic {element} Slime prefab socket or renderer contract is incomplete.");
            }
        }

        private static bool IsElementDecorationName(string objectName)
        {
            return StartsWithAny(
                objectName,
                "Element_",
                "Bubble_",
                "Drop_",
                "Water_",
                "Flame_",
                "Fire_",
                "Leaf_",
                "Rock_",
                "Stone_",
                "Earth_",
                "Wind_",
                "Ribbon_",
                "Swirl_",
                "Spark_",
                "Bolt_",
                "Lightning_");
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

        private static void GetPalette(
            BasicSlimeElement element,
            out Color body,
            out Color accent,
            out Color detail,
            out Color accentEmission)
        {
            switch (element)
            {
                case BasicSlimeElement.Water:
                    body = new Color(0.22f, 0.62f, 0.90f, 1f);
                    accent = new Color(0.58f, 0.88f, 1f, 1f);
                    detail = new Color(0.07f, 0.30f, 0.82f, 1f);
                    accentEmission = new Color(0.20f, 0.58f, 0.86f, 1f) * 0.22f;
                    break;
                case BasicSlimeElement.Fire:
                    body = new Color(0.94f, 0.34f, 0.20f, 1f);
                    accent = new Color(1f, 0.72f, 0.23f, 1f);
                    detail = new Color(0.72f, 0.055f, 0.035f, 1f);
                    accentEmission = new Color(1f, 0.34f, 0.08f, 1f) * 0.30f;
                    break;
                case BasicSlimeElement.Earth:
                    body = new Color(0.40f, 0.63f, 0.29f, 1f);
                    accent = new Color(0.70f, 0.43f, 0.20f, 1f);
                    detail = new Color(0.30f, 0.58f, 0.11f, 1f);
                    accentEmission = Color.black;
                    break;
                case BasicSlimeElement.Wind:
                    body = new Color(0.43f, 0.82f, 0.69f, 1f);
                    accent = new Color(0.82f, 0.98f, 0.87f, 1f);
                    detail = new Color(0.10f, 0.57f, 0.49f, 1f);
                    accentEmission = new Color(0.28f, 0.74f, 0.56f, 1f) * 0.18f;
                    break;
                case BasicSlimeElement.Lightning:
                    body = new Color(0.91f, 0.68f, 0.16f, 1f);
                    accent = new Color(1f, 0.94f, 0.42f, 1f);
                    detail = new Color(0.48f, 0.16f, 0.88f, 1f);
                    accentEmission = new Color(1f, 0.72f, 0.10f, 1f) * 0.26f;
                    break;
                default:
                    body = Color.white;
                    accent = Color.gray;
                    detail = Color.gray;
                    accentEmission = Color.black;
                    break;
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
                if (string.Equals(descendants[i].name, name, StringComparison.Ordinal))
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
