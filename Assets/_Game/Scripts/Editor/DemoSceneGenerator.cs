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
            EnsureSceneLighting();
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
            if (database == null || database.Characters.Count != 7 || database.Skills.Count != 10)
            {
                return false;
            }

            for (int i = 0; i < database.Characters.Count; i++)
            {
                CharacterDefinition character = database.Characters[i];
                if (character == null ||
                    !IsFinitePositive(character.AttackRange) ||
                    !IsFinitePositive(character.MoveSpeed) ||
                    character.MaxRage != BattleRules.MaxRage ||
                    character.RagePerAttack != BattleRules.RagePerBasicAttackHit ||
                    character.RageWhenHit != BattleRules.RagePerDamageReceived ||
                    character.UltimateSkill == null ||
                    character.Skill2 == null ||
                    character.Skill3 == null)
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

            if (database.DemoPlayerBattleCharacterIds.Count != BattleRules.DemoPlayerTeamSize ||
                !string.Equals(database.DemoPlayerBattleCharacterIds[0], "ur_cosmic_slime", System.StringComparison.Ordinal) ||
                !string.Equals(database.DemoPlayerBattleCharacterIds[1], "gold_ranger", System.StringComparison.Ordinal) ||
                !string.Equals(database.DemoPlayerBattleCharacterIds[2], "ember_striker", System.StringComparison.Ordinal))
            {
                return false;
            }

            if (database.DemoEnemyBattleCharacterIds.Count != BattleRules.DemoEnemyTeamSize ||
                !string.Equals(database.DemoEnemyBattleCharacterIds[0], "cyan_warden", System.StringComparison.Ordinal) ||
                !string.Equals(database.DemoEnemyBattleCharacterIds[1], "azure_vanguard", System.StringComparison.Ordinal) ||
                !string.Equals(database.DemoEnemyBattleCharacterIds[2], "violet_arcanist", System.StringComparison.Ordinal) ||
                !string.Equals(database.DemoEnemyBattleCharacterIds[3], "gold_ranger", System.StringComparison.Ordinal) ||
                !string.Equals(database.DemoEnemyBattleCharacterIds[4], "verdant_medic", System.StringComparison.Ordinal))
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
            CharacterDefinition assassin = database.GetCharacter(AssassinBattleKit.CharacterId);
            SkillDefinition spectrumNova = database.GetSkill("spectrum_nova");
            SkillDefinition catherineUltimate = database.GetSkill(CatherineYukiBattleKit.UltimateId);
            return cosmicSlime != null &&
                   cosmicSlime.Role == CharacterRole.Tank &&
                   cosmicSlime.Rarity == Rarity.UR &&
                   cosmicSlime.IsLimited &&
                   Mathf.Approximately(cosmicSlime.AttackRange, BattleRules.MeleeAttackRange) &&
                   cosmicSlime.CharacterPrefab != null &&
                   cosmicSlime.CharacterPrefab.GetComponent<CatherineSkillVfxController>() != null &&
                   cosmicSlime.UltimateSkill == catherineUltimate &&
                   cosmicSlime.Skill2 == database.GetSkill(CatherineYukiBattleKit.Skill1Id) &&
                   cosmicSlime.Skill3 == database.GetSkill(CatherineYukiBattleKit.Skill2Id) &&
                   HasBasicSlimePrefab(database, "azure_vanguard", BasicSlimeElement.Water) &&
                   HasBasicSlimePrefab(database, "ember_striker", BasicSlimeElement.Fire) &&
                   HasBasicSlimePrefab(database, "verdant_medic", BasicSlimeElement.Wind) &&
                   HasBasicSlimePrefab(database, "violet_arcanist", BasicSlimeElement.Lightning) &&
                   HasBasicSlimePrefab(database, "gold_ranger", BasicSlimeElement.Earth) &&
                   HasBasicSlimePrefab(database, "cyan_warden", BasicSlimeElement.Water) &&
                   HasRarity(database, "azure_vanguard", Rarity.R) &&
                   HasRarity(database, "ember_striker", Rarity.R) &&
                   HasRarity(database, "verdant_medic", Rarity.SR) &&
                   HasRarity(database, "violet_arcanist", Rarity.SR) &&
                   HasRarity(database, "gold_ranger", Rarity.SSR) &&
                   HasRarity(database, "cyan_warden", Rarity.SSR) &&
                   HasCombatProfile(database, "azure_vanguard", BattleRules.MeleeAttackRange, 3.2f) &&
                   HasCombatProfile(database, "ember_striker", BattleRules.MeleeAttackRange, 4.2f) &&
                   HasCombatProfile(database, "verdant_medic", BattleRules.RangedAttackRange, 3.0f) &&
                   HasCombatProfile(database, "ur_cosmic_slime", BattleRules.MeleeAttackRange, 3.3f) &&
                   HasCombatProfile(database, "violet_arcanist", BattleRules.RangedAttackRange, 3.4f) &&
                   HasCombatProfile(database, "gold_ranger", BattleRules.RangedAttackRange, 3.8f) &&
                   HasCombatProfile(database, "cyan_warden", BattleRules.MeleeAttackRange, 3.15f) &&
                   AssetDatabase.LoadAssetAtPath<Material>(AbyssalObservatoryAssetBuilder.BackdropMaterialPath) != null &&
                   database.GetSkill("pulse_strike") != null &&
                   spectrumNova != null &&
                   spectrumNova.TargetMode == SkillTargetMode.AllEnemies &&
                   spectrumNova.TargetCount == BattleRules.TeamSize &&
                   database.GetSkill("restore_wave") != null &&
                   database.GetSkill("timed_impact") != null &&
                   database.GetSkill("timed_impact").RageCost == 0 &&
                   database.GetSkill("timed_wave") != null &&
                   database.GetSkill("timed_wave").RageCost == 0 &&
                   database.GetSkill(AssassinBattleKit.BacklineShiftSkillId) != null &&
                   assassin != null &&
                   assassin.Skill2 ==
                       database.GetSkill(AssassinBattleKit.BacklineShiftSkillId) &&
                   database.GetSkill(CatherineYukiBattleKit.Skill1Id) != null &&
                   database.GetSkill(CatherineYukiBattleKit.Skill2Id) != null &&
                   database.GetSkill(CatherineYukiBattleKit.Skill3Id) != null &&
                   catherineUltimate != null &&
                   Mathf.Approximately(
                       catherineUltimate.DamageMultiplier,
                       CatherineYukiBattleKit.UltimateBaseDamageMultiplier);
        }

        private static bool HasRarity(GameDatabase database, string characterId, Rarity expectedRarity)
        {
            CharacterDefinition character = database.GetCharacter(characterId);
            return character != null && character.Rarity == expectedRarity && !character.IsLimited;
        }

        private static bool HasBasicSlimePrefab(
            GameDatabase database,
            string characterId,
            BasicSlimeElement expectedElement)
        {
            CharacterDefinition character = database.GetCharacter(characterId);
            if (character == null ||
                !BasicElementSlimeAssetBuilder.IsExpectedPrefab(character.CharacterPrefab, expectedElement))
            {
                return false;
            }

            BasicSlimeVisualController visualController =
                character.CharacterPrefab.GetComponent<BasicSlimeVisualController>();
            return visualController != null && visualController.Element == expectedElement;
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
            BasicElementSlimeAssetBuilder.EnsureAssets();
            GameObject waterSlimePrefab = BasicElementSlimeAssetBuilder.GetPrefab(BasicSlimeElement.Water);
            GameObject fireSlimePrefab = BasicElementSlimeAssetBuilder.GetPrefab(BasicSlimeElement.Fire);
            GameObject earthSlimePrefab = BasicElementSlimeAssetBuilder.GetPrefab(BasicSlimeElement.Earth);
            GameObject windSlimePrefab = BasicElementSlimeAssetBuilder.GetPrefab(BasicSlimeElement.Wind);
            GameObject lightningSlimePrefab = BasicElementSlimeAssetBuilder.GetPrefab(BasicSlimeElement.Lightning);
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
                BattleRules.MaxRage,
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
                BattleRules.MaxRage,
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
                BattleRules.MaxRage,
                0.42f,
                1,
                "Restores the ally with the lowest health ratio.");
            EditorUtility.SetDirty(healSkill);

            SkillDefinition timedImpactSkill = GetOrCreateAsset<SkillDefinition>(
                $"{SkillFolder}/Skill_TimedImpact.asset",
                out _);
            timedImpactSkill.Configure(
                "timed_impact",
                "Tactical Impact",
                SkillCategory.Damage,
                SkillTargetMode.SingleEnemy,
                1.1f,
                0f,
                0,
                0.28f,
                1,
                "Automatic skill slot 2. Cast at 5 seconds, then every 10 seconds.");
            EditorUtility.SetDirty(timedImpactSkill);

            SkillDefinition timedWaveSkill = GetOrCreateAsset<SkillDefinition>(
                $"{SkillFolder}/Skill_TimedWave.asset",
                out _);
            timedWaveSkill.Configure(
                "timed_wave",
                "Tactical Wave",
                SkillCategory.Damage,
                SkillTargetMode.AllEnemies,
                0.72f,
                0f,
                0,
                0.38f,
                BattleRules.TeamSize,
                "Automatic skill slot 3. Cast at 10 seconds, then every 10 seconds.");
            EditorUtility.SetDirty(timedWaveSkill);

            SkillDefinition assassinBacklineShift = GetOrCreateAsset<SkillDefinition>(
                $"{SkillFolder}/Skill_AssassinBacklineShift.asset",
                out _);
            assassinBacklineShift.Configure(
                AssassinBattleKit.BacklineShiftSkillId,
                "Backline Shift",
                SkillCategory.Damage,
                SkillTargetMode.SingleEnemy,
                1.1f,
                0f,
                0,
                0.22f,
                1,
                "Teleports behind the deepest enemy backline unit, strikes, and keeps attacking it.");
            EditorUtility.SetDirty(assassinBacklineShift);

            SkillDefinition catherineSkill1 = GetOrCreateAsset<SkillDefinition>(
                $"{SkillFolder}/Skill_CatherineWindWheelBreak.asset",
                out _);
            catherineSkill1.Configure(
                CatherineYukiBattleKit.Skill1Id,
                "Wind Wheel: Break",
                SkillCategory.Damage,
                SkillTargetMode.AllEnemies,
                CatherineYukiBattleKit.Skill1DamageMultiplier,
                0f,
                0,
                CatherineYukiBattleKit.Skill1HitDelay,
                BattleRules.TeamSize,
                "Max-level line break: pierces defenses and knocks targets up.");
            EditorUtility.SetDirty(catherineSkill1);

            SkillDefinition catherineSkill2 = GetOrCreateAsset<SkillDefinition>(
                $"{SkillFolder}/Skill_CatherineWindWheelDance.asset",
                out _);
            catherineSkill2.Configure(
                CatherineYukiBattleKit.Skill2Id,
                "Wind Wheel: Dance",
                SkillCategory.Damage,
                SkillTargetMode.SingleEnemy,
                CatherineYukiBattleKit.Skill2HitDamageMultiplier * 2f,
                CatherineYukiBattleKit.Skill2HealingFromDamageMultiplier,
                0,
                CatherineYukiBattleKit.Skill2SecondHitDelay,
                1,
                "Max-level two-hit charge with damage-based healing, Taunt, and Super Armor.");
            EditorUtility.SetDirty(catherineSkill2);

            SkillDefinition catherineSkill3 = GetOrCreateAsset<SkillDefinition>(
                $"{SkillFolder}/Skill_CatherineStarRage.asset",
                out _);
            catherineSkill3.Configure(
                CatherineYukiBattleKit.Skill3Id,
                "Star Rage",
                SkillCategory.Damage,
                SkillTargetMode.AllEnemies,
                0f,
                0f,
                0,
                CatherineYukiBattleKit.Skill3HitDelay,
                BattleRules.TeamSize,
                "Max-level domain test: applies gravity debuff and gains Imaginary Mass.");
            EditorUtility.SetDirty(catherineSkill3);

            SkillDefinition catherineUltimate = GetOrCreateAsset<SkillDefinition>(
                $"{SkillFolder}/Skill_CatherineInfiniteVoid.asset",
                out _);
            catherineUltimate.Configure(
                CatherineYukiBattleKit.UltimateId,
                "Imaginary Mass: Infinite Void",
                SkillCategory.Damage,
                SkillTargetMode.AllEnemies,
                CatherineYukiBattleKit.UltimateBaseDamageMultiplier,
                0f,
                BattleRules.MaxRage,
                CatherineYukiBattleKit.UltimateChargeDuration,
                BattleRules.TeamSize,
                "Max-level 960% base AoE: charge, transform, pull, multi-hit, collapse, and launch.");
            EditorUtility.SetDirty(catherineUltimate);

            var skills = new List<SkillDefinition>
            {
                strikeSkill,
                novaSkill,
                healSkill,
                timedImpactSkill,
                timedWaveSkill,
                assassinBacklineShift,
                catherineSkill1,
                catherineSkill2,
                catherineSkill3,
                catherineUltimate
            };
            var characters = new List<CharacterDefinition>
            {
                CreateCharacter(
                    "azure_vanguard",
                    "Azure Vanguard",
                    CharacterRole.Tank,
                    Rarity.R,
                    new Color(0.13f, 0.48f, 0.95f, 1f),
                    1620f,
                    116f,
                    58f,
                    1.45f,
                    BattleRules.MeleeAttackRange,
                    3.2f,
                    strikeSkill,
                    timedImpactSkill,
                    timedWaveSkill,
                    "A steady frontline defender.",
                    waterSlimePrefab),
                CreateCharacter(
                    "ember_striker",
                    "Ember Striker",
                    CharacterRole.Assassin,
                    Rarity.R,
                    new Color(0.96f, 0.28f, 0.14f, 1f),
                    1180f,
                    172f,
                    29f,
                    1.12f,
                    BattleRules.MeleeAttackRange,
                    4.2f,
                    strikeSkill,
                    assassinBacklineShift,
                    timedWaveSkill,
                    "A fast close-range attacker.",
                    fireSlimePrefab),
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
                    BattleRules.RangedAttackRange,
                    3.0f,
                    healSkill,
                    timedImpactSkill,
                    timedWaveSkill,
                    "A support unit that restores weakened allies.",
                    windSlimePrefab),
                CreateCharacter(
                    "ur_cosmic_slime",
                    "Catherine Yuki",
                    CharacterRole.Tank,
                    Rarity.UR,
                    new Color(0.52f, 0.16f, 0.96f, 1f),
                    2280f,
                    124f,
                    86f,
                    1.52f,
                    BattleRules.MeleeAttackRange,
                    3.3f,
                    catherineUltimate,
                    catherineSkill1,
                    catherineSkill2,
                    "A max-level limited singularity tank with Imaginary Mass and Infinite Void.",
                    cosmicSlimePrefab,
                    true),
                CreateCharacter(
                    "violet_arcanist",
                    "Violet Arcanist",
                    CharacterRole.Mage,
                    Rarity.SR,
                    new Color(0.62f, 0.25f, 0.91f, 1f),
                    1110f,
                    158f,
                    26f,
                    1.28f,
                    BattleRules.RangedAttackRange,
                    3.4f,
                    novaSkill,
                    timedImpactSkill,
                    timedWaveSkill,
                    "A ranged caster with a team-wide burst.",
                    lightningSlimePrefab),
                CreateCharacter(
                    "gold_ranger",
                    "Gold Ranger",
                    CharacterRole.Ranged,
                    Rarity.SSR,
                    new Color(0.96f, 0.68f, 0.13f, 1f),
                    1220f,
                    184f,
                    31f,
                    1.02f,
                    BattleRules.RangedAttackRange,
                    3.8f,
                    strikeSkill,
                    timedImpactSkill,
                    timedWaveSkill,
                    "A precise high-speed specialist.",
                    earthSlimePrefab),
                CreateCharacter(
                    "cyan_warden",
                    "Cyan Warden",
                    CharacterRole.Tank,
                    Rarity.SSR,
                    new Color(0.10f, 0.77f, 0.82f, 1f),
                    1780f,
                    132f,
                    64f,
                    1.50f,
                    BattleRules.MeleeAttackRange,
                    3.15f,
                    novaSkill,
                    timedImpactSkill,
                    timedWaveSkill,
                    "A durable guardian with an area disruption skill.",
                    waterSlimePrefab)
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
                    "gold_ranger",
                    "ember_striker",
                    "verdant_medic",
                    "violet_arcanist"
                },
                new[]
                {
                    "ur_cosmic_slime",
                    "gold_ranger",
                    "ember_striker"
                },
                new[]
                {
                    "cyan_warden",
                    "azure_vanguard",
                    "violet_arcanist",
                    "gold_ranger",
                    "verdant_medic"
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
            SkillDefinition ultimateSkill,
            SkillDefinition skill2,
            SkillDefinition skill3,
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
                BattleRules.MaxRage,
                BattleRules.RagePerBasicAttackHit,
                BattleRules.RagePerDamageReceived,
                ultimateSkill,
                description,
                null,
                prefab,
                isLimited);
            character.ConfigureActiveSkills(skill2, skill3);
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

        private static void EnsureSceneLighting()
        {
            Scene previousActive = SceneManager.GetActiveScene();
            Scene targetScene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForRepair = !targetScene.IsValid() || !targetScene.isLoaded;
            if (openedForRepair)
            {
                targetScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            bool wasDirty = targetScene.isDirty;
            SceneManager.SetActiveScene(targetScene);

            ConfigureLight(
                targetScene,
                "Directional Light",
                LightType.Directional,
                new Color(1f, 0.90f, 0.82f, 1f),
                1.08f,
                0f,
                Vector3.zero,
                Quaternion.Euler(48f, -34f, 0f),
                LightShadows.Soft);
            ConfigureLight(
                targetScene,
                "Catherine Soft Fill",
                LightType.Point,
                new Color(0.34f, 0.52f, 1f, 1f),
                4.8f,
                18f,
                new Vector3(-4.6f, 4.4f, -4.8f),
                Quaternion.identity,
                LightShadows.None);
            ConfigureLight(
                targetScene,
                "Catherine Rim Light",
                LightType.Point,
                new Color(0.78f, 0.28f, 1f, 1f),
                6.2f,
                17f,
                new Vector3(4.2f, 3.8f, 4.9f),
                Quaternion.identity,
                LightShadows.None);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.10f, 0.14f, 0.22f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.055f, 0.070f, 0.12f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.020f, 0.024f, 0.040f, 1f);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.reflectionIntensity = 0.72f;

            EditorSceneManager.MarkSceneDirty(targetScene);
            if (openedForRepair || !wasDirty)
            {
                EditorSceneManager.SaveScene(targetScene, ScenePath);
            }
            else
            {
                Debug.LogWarning(
                    "[GenericGachaRPG] Demo lighting was repaired in memory; the already-dirty scene was not auto-saved.");
            }

            if (openedForRepair)
            {
                EditorSceneManager.CloseScene(targetScene, true);
            }

            if (previousActive.IsValid() && previousActive.isLoaded)
            {
                SceneManager.SetActiveScene(previousActive);
            }
        }

        private static void ConfigureLight(
            Scene scene,
            string objectName,
            LightType type,
            Color color,
            float intensity,
            float range,
            Vector3 position,
            Quaternion rotation,
            LightShadows shadows)
        {
            GameObject lightObject = null;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                {
                    lightObject = roots[i];
                    break;
                }
            }

            if (lightObject == null)
            {
                lightObject = new GameObject(objectName, typeof(Light));
                SceneManager.MoveGameObjectToScene(lightObject, scene);
            }

            Light light = lightObject.GetComponent<Light>();
            if (light == null)
            {
                light = lightObject.AddComponent<Light>();
            }

            light.type = type;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = shadows;
            light.shadowStrength = shadows == LightShadows.None ? 0f : 0.82f;
            light.shadowBias = 0.04f;
            light.shadowNormalBias = 0.32f;
            light.renderMode = LightRenderMode.ForcePixel;
            lightObject.transform.SetPositionAndRotation(position, rotation);
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
