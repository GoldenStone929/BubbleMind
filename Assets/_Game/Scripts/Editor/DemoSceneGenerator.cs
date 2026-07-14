using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GenericGachaRPG.Editor
{
    [InitializeOnLoad]
    public static class DemoSceneGenerator
    {
        public const string ScenePath = "Assets/_Game/Scenes/GachaRPGDemo.unity";
        public const string DatabasePath = "Assets/_Game/Data/GameDatabase.asset";

        private const string SkillFolder = "Assets/_Game/Data/Skills";
        private const string CharacterFolder = "Assets/_Game/Data/Characters";
        private const string GachaFolder = "Assets/_Game/Data/Gacha";

        static DemoSceneGenerator()
        {
            EditorApplication.delayCall += TryAutoGenerate;
        }

        [MenuItem("Tools/Generic Gacha RPG/Generate or Repair Demo _F7", priority = 10)]
        public static void GenerateOrRepairDemo()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                Debug.LogWarning("[GenericGachaRPG] Unity is still compiling or importing. Generation will retry after it finishes.");
                EditorApplication.delayCall += GenerateOrRepairDemo;
                return;
            }

            EnsureFolders();
            AbyssalObservatoryAssetBuilder.EnsureAssets();
            GameDatabase database = GenerateContentIfNeeded();
            bool sceneCreated = GenerateSceneIfNeeded(database);
            ApplySafeProjectSettings();
            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            DemoProjectVerifier.Verify(database);
            AssetDatabase.Refresh();

            Debug.Log(sceneCreated
                ? $"[GenericGachaRPG] Demo generated successfully at {ScenePath}. Open it and press Play."
                : $"[GenericGachaRPG] Demo content verified. Existing scene was preserved at {ScenePath}.");
        }

        [MenuItem("Tools/Generic Gacha RPG/Open Demo Scene _F8", priority = 11)]
        public static void OpenDemoScene()
        {
            if (!File.Exists(Path.GetFullPath(ScenePath)))
            {
                GenerateOrRepairDemo();
            }

            if (File.Exists(Path.GetFullPath(ScenePath)) &&
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
        }

        private static void TryAutoGenerate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryAutoGenerate;
                return;
            }

            GameDatabase existingDatabase = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (File.Exists(Path.GetFullPath(ScenePath)) && HasCompleteGeneratedContent(existingDatabase))
            {
                return;
            }

            try
            {
                GenerateOrRepairDemo();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[GenericGachaRPG] Automatic demo generation failed: {exception}");
            }
        }

        private static bool HasCompleteGeneratedContent(GameDatabase database)
        {
            if (database == null || database.Characters.Count != 7 || database.Skills.Count != 3)
            {
                return false;
            }

            for (int i = 0; i < database.Characters.Count; i++)
            {
                CharacterDefinition character = database.Characters[i];
                if (character == null ||
                    !IsFinitePositive(character.AttackRange) ||
                    !IsFinitePositive(character.MoveSpeed))
                {
                    return false;
                }
            }

            GachaBannerDefinition banner = database.DefaultBanner;
            if (banner == null || banner.Entries.Count != 6 || banner.TotalWeight <= 0f)
            {
                return false;
            }

            if (database.StarterCharacterIds.Count != TeamFormationState.RequiredMemberCount ||
                !string.Equals(database.StarterCharacterIds[0], "ur_cosmic_slime", System.StringComparison.Ordinal))
            {
                return false;
            }

            for (int i = 0; i < banner.Entries.Count; i++)
            {
                GachaPoolEntry entry = banner.Entries[i];
                CharacterDefinition standardCharacter = entry == null
                    ? null
                    : database.GetCharacter(entry.CharacterId);
                if (standardCharacter == null || standardCharacter.IsLimited)
                {
                    return false;
                }
            }

            CharacterDefinition cosmicSlime = database.GetCharacter("ur_cosmic_slime");
            SkillDefinition spectrumNova = database.GetSkill("spectrum_nova");
            return cosmicSlime != null &&
                   cosmicSlime.Role == CharacterRole.Guardian &&
                   cosmicSlime.Rarity == Rarity.UR &&
                   cosmicSlime.IsLimited &&
                   cosmicSlime.AttackRange <= BattleRules.GuardianAttackRange + BattleRules.RangeEpsilon &&
                   cosmicSlime.CharacterPrefab != null &&
                   HasRarity(database, "azure_vanguard", Rarity.R) &&
                   HasRarity(database, "ember_striker", Rarity.R) &&
                   HasRarity(database, "verdant_medic", Rarity.SR) &&
                   HasRarity(database, "violet_arcanist", Rarity.SR) &&
                   HasRarity(database, "gold_ranger", Rarity.SSR) &&
                   HasRarity(database, "cyan_warden", Rarity.SSR) &&
                   HasCombatProfile(database, "azure_vanguard", 1.45f, 3.2f) &&
                   HasCombatProfile(database, "ember_striker", 1.55f, 4.2f) &&
                   HasCombatProfile(database, "verdant_medic", 4.4f, 3.0f) &&
                   HasCombatProfile(database, "ur_cosmic_slime", 1.45f, 3.3f) &&
                   HasCombatProfile(database, "violet_arcanist", 3.8f, 3.4f) &&
                   HasCombatProfile(database, "gold_ranger", 4.6f, 3.8f) &&
                   HasCombatProfile(database, "cyan_warden", 1.55f, 3.15f) &&
                   AssetDatabase.LoadAssetAtPath<Material>(AbyssalObservatoryAssetBuilder.BackdropMaterialPath) != null &&
                   database.GetSkill("pulse_strike") != null &&
                   spectrumNova != null &&
                   spectrumNova.TargetMode == SkillTargetMode.AllEnemies &&
                   spectrumNova.TargetCount == BattleRules.TeamSize &&
                   database.GetSkill("restore_wave") != null;
        }

        private static bool HasRarity(GameDatabase database, string characterId, Rarity expectedRarity)
        {
            CharacterDefinition character = database.GetCharacter(characterId);
            return character != null && character.Rarity == expectedRarity && !character.IsLimited;
        }

        private static bool IsFinitePositive(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool HasCombatProfile(
            GameDatabase database,
            string characterId,
            float expectedAttackRange,
            float expectedMoveSpeed)
        {
            CharacterDefinition character = database.GetCharacter(characterId);
            return character != null &&
                   Mathf.Approximately(character.AttackRange, expectedAttackRange) &&
                   Mathf.Approximately(character.MoveSpeed, expectedMoveSpeed);
        }

        private static GameDatabase GenerateContentIfNeeded()
        {
            GameObject cosmicSlimePrefab = CosmicSlimeAssetBuilder.EnsureAssets();
            SkillDefinition strikeSkill = GetOrCreateAsset<SkillDefinition>(
                $"{SkillFolder}/Skill_PulseStrike.asset",
                out _);
            strikeSkill.Configure(
                "pulse_strike",
                "Pulse Strike",
                SkillCategory.Damage,
                SkillTargetMode.SingleEnemy,
                2.35f,
                0f,
                100,
                0.34f,
                1,
                "A focused burst against one target.");
            EditorUtility.SetDirty(strikeSkill);

            SkillDefinition novaSkill = GetOrCreateAsset<SkillDefinition>(
                $"{SkillFolder}/Skill_SpectrumNova.asset",
                out _);
            novaSkill.Configure(
                "spectrum_nova",
                "Spectrum Nova",
                SkillCategory.Damage,
                SkillTargetMode.AllEnemies,
                1.28f,
                0f,
                100,
                0.48f,
                BattleRules.TeamSize,
                "A wide spectrum blast that hits every opponent.");
            EditorUtility.SetDirty(novaSkill);

            SkillDefinition healSkill = GetOrCreateAsset<SkillDefinition>(
                $"{SkillFolder}/Skill_RestoreWave.asset",
                out _);
            healSkill.Configure(
                "restore_wave",
                "Restore Wave",
                SkillCategory.Healing,
                SkillTargetMode.LowestHealthAlly,
                0f,
                1.45f,
                100,
                0.42f,
                1,
                "Restores the ally with the lowest health ratio.");
            EditorUtility.SetDirty(healSkill);

            var skills = new List<SkillDefinition> { strikeSkill, novaSkill, healSkill };
            var characters = new List<CharacterDefinition>
            {
                CreateCharacter(
                    "azure_vanguard",
                    "Azure Vanguard",
                    CharacterRole.Guardian,
                    Rarity.R,
                    new Color(0.13f, 0.48f, 0.95f, 1f),
                    1620f,
                    116f,
                    58f,
                    1.45f,
                    1.45f,
                    3.2f,
                    strikeSkill,
                    "A steady frontline defender."),
                CreateCharacter(
                    "ember_striker",
                    "Ember Striker",
                    CharacterRole.Striker,
                    Rarity.R,
                    new Color(0.96f, 0.28f, 0.14f, 1f),
                    1180f,
                    172f,
                    29f,
                    1.12f,
                    1.55f,
                    4.2f,
                    strikeSkill,
                    "A fast close-range attacker."),
                CreateCharacter(
                    "verdant_medic",
                    "Verdant Medic",
                    CharacterRole.Support,
                    Rarity.SR,
                    new Color(0.20f, 0.78f, 0.37f, 1f),
                    1250f,
                    122f,
                    34f,
                    1.34f,
                    4.4f,
                    3.0f,
                    healSkill,
                    "A support unit that restores weakened allies."),
                CreateCharacter(
                    "ur_cosmic_slime",
                    "Abyssal Slime",
                    CharacterRole.Guardian,
                    Rarity.UR,
                    new Color(0.52f, 0.16f, 0.96f, 1f),
                    2280f,
                    124f,
                    86f,
                    1.52f,
                    1.45f,
                    3.3f,
                    novaSkill,
                    "A limited singularity tank that advances first and anchors the frontline.",
                    cosmicSlimePrefab,
                    true),
                CreateCharacter(
                    "violet_arcanist",
                    "Violet Arcanist",
                    CharacterRole.Striker,
                    Rarity.SR,
                    new Color(0.62f, 0.25f, 0.91f, 1f),
                    1110f,
                    158f,
                    26f,
                    1.28f,
                    3.8f,
                    3.4f,
                    novaSkill,
                    "A ranged caster with a team-wide burst."),
                CreateCharacter(
                    "gold_ranger",
                    "Gold Ranger",
                    CharacterRole.Striker,
                    Rarity.SSR,
                    new Color(0.96f, 0.68f, 0.13f, 1f),
                    1220f,
                    184f,
                    31f,
                    1.02f,
                    4.6f,
                    3.8f,
                    strikeSkill,
                    "A precise high-speed specialist."),
                CreateCharacter(
                    "cyan_warden",
                    "Cyan Warden",
                    CharacterRole.Guardian,
                    Rarity.SSR,
                    new Color(0.10f, 0.77f, 0.82f, 1f),
                    1780f,
                    132f,
                    64f,
                    1.50f,
                    1.55f,
                    3.15f,
                    novaSkill,
                    "A durable guardian with an area disruption skill.")
            };

            GachaBannerDefinition banner = GetOrCreateAsset<GachaBannerDefinition>(
                $"{GachaFolder}/Banner_StandardSignal.asset",
                out _);
            banner.Configure(
                "standard_signal",
                "Standard Signal",
                100,
                new[]
                {
                    new GachaPoolEntry("azure_vanguard", 30f),
                    new GachaPoolEntry("ember_striker", 30f),
                    new GachaPoolEntry("verdant_medic", 17f),
                    new GachaPoolEntry("violet_arcanist", 17f),
                    new GachaPoolEntry("gold_ranger", 3f),
                    new GachaPoolEntry("cyan_warden", 3f)
                },
                "Six original signals. R 60% • SR 34% • SSR 6%");
            EditorUtility.SetDirty(banner);

            GameDatabase database = GetOrCreateAsset<GameDatabase>(DatabasePath, out _);
            database.Configure(
                3000,
                new[]
                {
                    "ur_cosmic_slime",
                    "ember_striker",
                    "verdant_medic",
                    "violet_arcanist",
                    "gold_ranger"
                },
                characters,
                skills,
                new[] { banner });

            EditorUtility.SetDirty(database);
            return database;
        }

        private static CharacterDefinition CreateCharacter(
            string id,
            string displayName,
            CharacterRole role,
            Rarity rarity,
            Color color,
            float maxHealth,
            float attack,
            float defense,
            float attackInterval,
            float attackRange,
            float moveSpeed,
            SkillDefinition skill,
            string description,
            GameObject prefab = null,
            bool isLimited = false)
        {
            string path = $"{CharacterFolder}/Character_{id}.asset";
            CharacterDefinition character = GetOrCreateAsset<CharacterDefinition>(path, out _);
            character.Configure(
                id,
                displayName,
                role,
                rarity,
                color,
                maxHealth,
                attack,
                defense,
                attackInterval,
                attackRange,
                moveSpeed,
                100,
                24,
                12,
                skill,
                description,
                null,
                prefab,
                isLimited);
            EditorUtility.SetDirty(character);

            return character;
        }

        private static bool GenerateSceneIfNeeded(GameDatabase database)
        {
            if (File.Exists(Path.GetFullPath(ScenePath)))
            {
                return false;
            }

            Scene previousActive = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            scene.name = "GachaRPGDemo";
            SceneManager.SetActiveScene(scene);

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.045f, 0.085f, 1f);
            camera.fieldOfView = 46f;
            camera.transform.position = new Vector3(0f, 5.6f, -12.8f);
            camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1.15f, 0f) - camera.transform.position);

            GameObject lightObject = new GameObject("Directional Light", typeof(Light));
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.91f, 0.95f, 1f, 1f);
            light.intensity = 1.35f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(42f, -32f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.14f, 0.19f, 0.28f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.08f, 0.10f, 0.16f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.035f, 0.045f, 0.07f, 1f);

            GameObject root = new GameObject("DemoGameRoot");
            DemoGameController controller = root.AddComponent<DemoGameController>();
            controller.Configure(database, camera);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.CloseScene(scene, true);

            if (previousActive.IsValid() && previousActive.isLoaded)
            {
                SceneManager.SetActiveScene(previousActive);
            }

            return true;
        }

        private static void ApplySafeProjectSettings()
        {
            PlayerSettings.companyName = "Independent Demo Lab";
            PlayerSettings.productName = "BubbleMind First Demo";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(
                BuildTarget.StandaloneWindows64,
                new[] { GraphicsDeviceType.Direct3D11 });
        }

        private static void AddSceneToBuildSettings()
        {
            var paths = new List<string> { ScenePath };
            EditorBuildSettingsScene[] existingScenes = EditorBuildSettings.scenes;
            for (int i = 0; i < existingScenes.Length; i++)
            {
                string path = existingScenes[i].path;
                if (!string.IsNullOrEmpty(path) && !paths.Contains(path))
                {
                    paths.Add(path);
                }
            }

            var scenes = new EditorBuildSettingsScene[paths.Count];
            for (int i = 0; i < paths.Count; i++)
            {
                scenes[i] = new EditorBuildSettingsScene(paths[i], true);
            }

            EditorBuildSettings.scenes = scenes;
        }

        private static T GetOrCreateAsset<T>(string path, out bool created)
            where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                created = false;
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            created = true;
            return asset;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "_Game");
            EnsureFolder("Assets/_Game", "Data");
            EnsureFolder("Assets/_Game/Data", "Skills");
            EnsureFolder("Assets/_Game/Data", "Characters");
            EnsureFolder("Assets/_Game/Data", "Gacha");
            EnsureFolder("Assets/_Game", "Scenes");
            EnsureFolder("Assets/_Game", "Art");
            EnsureFolder("Assets/_Game/Art", "Generated");
            EnsureFolder("Assets/_Game", "Prefabs");
            EnsureFolder("Assets/_Game", "Tests");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
