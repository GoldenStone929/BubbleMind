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
        private const string CharacterProfileFolder = "Assets/_Game/Data/CharacterProfiles";
        private const string GachaFolder = "Assets/_Game/Data/Gacha";
        private const string StageFolder = "Assets/_Game/Data/Stages";
        private const string PortraitFolder = "Assets/_Game/Art/Generated/UI/Portraits";
        private const string PixelSpriteFolder = "Assets/_Game/Art/Generated/Pixel2D/Characters/Resources/BattleSprites";
        private const string PixelArenaPath = "Assets/_Game/Art/Generated/Pixel2D/Environments/AbyssalObservatory/Textures/Resources/AbyssalObservatory_Pixel.png";
        private static readonly string[] RequiredCharacterIds =
        {
            "azure_vanguard",
            "ember_striker",
            "verdant_medic",
            "ur_cosmic_slime",
            "violet_arcanist",
            "gold_ranger",
            "cyan_warden"
        };
        private static readonly string[] RequiredSkillIds =
        {
            "pulse_strike",
            "spectrum_nova",
            "restore_wave",
            "timed_impact",
            "timed_wave",
            AssassinBattleKit.BacklineShiftSkillId,
            CatherineYukiBattleKit.Skill1Id,
            CatherineYukiBattleKit.Skill2Id,
            CatherineYukiBattleKit.Skill3Id,
            CatherineYukiBattleKit.UltimateId
        };
        private static readonly string[] RequiredStageIds =
        {
            "stage_1_1",
            "stage_1_2",
            "stage_1_3"
        };
        private static readonly string[] RequiredStageEnemyCharacterIds =
        {
            "cyan_warden",
            "azure_vanguard",
            "violet_arcanist",
            "gold_ranger",
            "verdant_medic"
        };

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
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigurePortraitImportSettings();
            ConfigurePixelImportSettings();
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
            if (database == null ||
                !ContainsAllCharacters(database, RequiredCharacterIds) ||
                !ContainsAllSkills(database, RequiredSkillIds) ||
                !ContainsAllStages(database, RequiredStageIds) ||
                !HasExpectedStageContent(database))
            {
                return false;
            }

            for (int i = 0; i < RequiredCharacterIds.Length; i++)
            {
                CharacterDefinition character = database.GetCharacter(RequiredCharacterIds[i]);
                if (character == null ||
                    !IsFinitePositive(character.AttackRange) ||
                    !IsFinitePositive(character.MoveSpeed) ||
                    character.MaxRage != BattleRules.MaxRage ||
                    character.RagePerAttack != BattleRules.RagePerBasicAttackHit ||
                    character.RageWhenHit != BattleRules.RagePerDamageReceived ||
                    character.UltimateSkill == null ||
                    character.Skill2 == null ||
                    character.Skill3 == null ||
                    character.ContentProfile == null ||
                    character.Portrait == null)
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
                !string.Equals(database.DemoPlayerBattleCharacterIds[2], "ember_striker", System.StringComparison.Ordinal) ||
                !string.Equals(database.DemoPlayerBattleCharacterIds[3], "verdant_medic", System.StringComparison.Ordinal) ||
                !string.Equals(database.DemoPlayerBattleCharacterIds[4], "violet_arcanist", System.StringComparison.Ordinal))
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

        private static bool ContainsAllCharacters(GameDatabase database, IReadOnlyList<string> requiredIds)
        {
            for (int i = 0; i < requiredIds.Count; i++)
            {
                if (database.GetCharacter(requiredIds[i]) == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ContainsAllSkills(GameDatabase database, IReadOnlyList<string> requiredIds)
        {
            for (int i = 0; i < requiredIds.Count; i++)
            {
                if (database.GetSkill(requiredIds[i]) == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ContainsAllStages(GameDatabase database, IReadOnlyList<string> requiredIds)
        {
            if (database == null || database.Stages == null)
            {
                return false;
            }

            for (int i = 0; i < requiredIds.Count; i++)
            {
                if (database.GetStage(requiredIds[i]) == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasExpectedStageContent(GameDatabase database)
        {
            return database.FirstStage != null &&
                   string.Equals(database.FirstStage.Id, RequiredStageIds[0], StringComparison.Ordinal) &&
                   HasExpectedStage(
                       database.GetStage(RequiredStageIds[0]),
                       "Fracture Gate",
                       string.Empty,
                       6,
                       1000,
                       100,
                       250,
                       2,
                       false) &&
                   HasExpectedStage(
                       database.GetStage(RequiredStageIds[1]),
                       "Resonance Gallery",
                       RequiredStageIds[0],
                       8,
                       1800,
                       120,
                       350,
                       3,
                       false) &&
                   HasExpectedStage(
                       database.GetStage(RequiredStageIds[2]),
                       "Event Horizon",
                       RequiredStageIds[1],
                       10,
                       2600,
                       200,
                       500,
                       5,
                       true);
        }

        private static bool HasExpectedStage(
            StageDefinition stage,
            string displayName,
            string prerequisiteStageId,
            int energyCost,
            int recommendedPower,
            int firstClearCrystals,
            int goldReward,
            int materialReward,
            bool isBoss)
        {
            if (stage == null ||
                !string.Equals(stage.ChapterId, "chapter_1", StringComparison.Ordinal) ||
                !string.Equals(stage.DisplayName, displayName, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(stage.Description) ||
                !string.Equals(stage.PrerequisiteStageId, prerequisiteStageId, StringComparison.Ordinal) ||
                stage.EnergyCost != energyCost ||
                stage.RecommendedPower != recommendedPower ||
                stage.FirstClearCrystalReward != firstClearCrystals ||
                stage.GoldReward != goldReward ||
                stage.MaterialReward != materialReward ||
                stage.IsBossStage != isBoss ||
                stage.EnemyCharacterIds == null ||
                stage.EnemyCharacterIds.Count != RequiredStageEnemyCharacterIds.Length)
            {
                return false;
            }

            for (int index = 0; index < RequiredStageEnemyCharacterIds.Length; index++)
            {
                if (!string.Equals(
                        stage.EnemyCharacterIds[index],
                        RequiredStageEnemyCharacterIds[index],
                        StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        internal static List<T> MergeDefinitions<T>(
            IReadOnlyList<T> existing,
            IEnumerable<T> generated,
            Func<T, string> getId)
            where T : UnityEngine.Object
        {
            var merged = new List<T>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (generated != null)
            {
                foreach (T item in generated)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    string id = getId(item);
                    if (!string.IsNullOrWhiteSpace(id) && ids.Add(id))
                    {
                        merged.Add(item);
                    }
                }
            }

            if (existing == null)
            {
                return merged;
            }

            for (int i = 0; i < existing.Count; i++)
            {
                T item = existing[i];
                if (item == null)
                {
                    continue;
                }

                string id = getId(item);
                if (!string.IsNullOrWhiteSpace(id) && ids.Add(id))
                {
                    merged.Add(item);
                }
            }

            return merged;
        }

        internal static bool ShouldInitializeProfile(
            bool profileCreated,
            CharacterContentProfile profile)
        {
            return profileCreated || profile == null || string.IsNullOrWhiteSpace(profile.SchemaVersion);
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
            strikeSkill.ConfigurePresentation(SkillTag.Damage | SkillTag.Physical);
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
            novaSkill.ConfigurePresentation(SkillTag.Damage | SkillTag.Area | SkillTag.Magical);
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
            healSkill.ConfigurePresentation(SkillTag.Healing | SkillTag.Survival);
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
            timedImpactSkill.ConfigurePresentation(SkillTag.Damage | SkillTag.Control);
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
            timedWaveSkill.ConfigurePresentation(SkillTag.Damage | SkillTag.Area);
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
            assassinBacklineShift.ConfigurePresentation(
                SkillTag.Damage | SkillTag.Mobility | SkillTag.Physical);
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
            catherineSkill1.ConfigurePresentation(
                SkillTag.Damage | SkillTag.Control | SkillTag.Area | SkillTag.Physical);
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
            catherineSkill2.ConfigurePresentation(
                SkillTag.Damage | SkillTag.Healing | SkillTag.Control | SkillTag.Survival |
                SkillTag.Taunt | SkillTag.Physical);
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
            catherineSkill3.ConfigurePresentation(
                SkillTag.Survival | SkillTag.Enhancement | SkillTag.Control);
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
            catherineUltimate.ConfigurePresentation(
                SkillTag.Damage | SkillTag.Control | SkillTag.Area | SkillTag.Physical);
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

            CreateStandardCharacterProfile(
                characters[0],
                CharacterElement.Water,
                "Tidewall Sentinel",
                new[] { "frontline", "guard", "steady" },
                "Standard Signal recruitment.",
                false);
            CreateStandardCharacterProfile(
                characters[1],
                CharacterElement.Fire,
                "Cinderstep Hunter",
                new[] { "assassin", "backline", "mobility" },
                "Standard Signal recruitment and demo starter access.",
                true);
            CreateStandardCharacterProfile(
                characters[2],
                CharacterElement.Wind,
                "Canopy Field Medic",
                new[] { "support", "healing", "sustain" },
                "Standard Signal recruitment and demo starter access.",
                true);
            CreateCatherineContentProfile(
                characters[3],
                catherineSkill3);
            CreateStandardCharacterProfile(
                characters[4],
                CharacterElement.Lightning,
                "Stormglass Arcanist",
                new[] { "mage", "area", "burst" },
                "Standard Signal recruitment and demo starter access.",
                true);
            CreateStandardCharacterProfile(
                characters[5],
                CharacterElement.Earth,
                "Gilded Horizon Ranger",
                new[] { "ranged", "precision", "tempo" },
                "Standard Signal recruitment and demo starter access.",
                true);
            CreateStandardCharacterProfile(
                characters[6],
                CharacterElement.Water,
                "Prismatic Tide Warden",
                new[] { "tank", "disruption", "area" },
                "Standard Signal recruitment.",
                false);

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

            var generatedStages = new List<StageDefinition>
            {
                CreateStage(
                    "Stage_1_1.asset",
                    RequiredStageIds[0],
                    "Fracture Gate",
                    "Secure the broken relay before the rift spreads.",
                    string.Empty,
                    6,
                    1000,
                    100,
                    250,
                    2,
                    false),
                CreateStage(
                    "Stage_1_2.asset",
                    RequiredStageIds[1],
                    "Resonance Gallery",
                    "Push through a corridor where every impact returns as an echo.",
                    RequiredStageIds[0],
                    8,
                    1800,
                    120,
                    350,
                    3,
                    false),
                CreateStage(
                    "Stage_1_3.asset",
                    RequiredStageIds[2],
                    "Event Horizon",
                    "Defeat the singularity guard at the observatory core.",
                    RequiredStageIds[1],
                    10,
                    2600,
                    200,
                    500,
                    5,
                    true)
            };

            GameDatabase database = GetOrCreateAsset<GameDatabase>(DatabasePath, out _);
            characters = MergeDefinitions(database.Characters, characters, character => character.Id);
            skills = MergeDefinitions(database.Skills, skills, skill => skill.Id);
            List<GachaBannerDefinition> banners = MergeDefinitions(
                database.GachaBanners,
                new[] { banner },
                candidate => candidate.Id);
            List<StageDefinition> stages = MergeDefinitions(
                database.Stages,
                generatedStages,
                stage => stage.Id);
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
                    "ember_striker",
                    "verdant_medic",
                    "violet_arcanist"
                },
                RequiredStageEnemyCharacterIds,
                characters,
                skills,
                banners,
                stages);

            EditorUtility.SetDirty(database);
            return database;
        }

        private static StageDefinition CreateStage(
            string assetName,
            string id,
            string displayName,
            string description,
            string prerequisiteStageId,
            int energyCost,
            int recommendedPower,
            int firstClearCrystals,
            int goldReward,
            int materialReward,
            bool isBoss)
        {
            StageDefinition stage = GetOrCreateAsset<StageDefinition>(
                $"{StageFolder}/{assetName}",
                out _);
            stage.Configure(
                id,
                "chapter_1",
                displayName,
                description,
                prerequisiteStageId,
                RequiredStageEnemyCharacterIds,
                energyCost,
                recommendedPower,
                firstClearCrystals,
                goldReward,
                materialReward,
                isBoss);
            EditorUtility.SetDirty(stage);
            return stage;
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
            Sprite portrait = AssetDatabase.LoadAssetAtPath<Sprite>($"{PortraitFolder}/Portrait_{id}.png");
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
                portrait,
                prefab,
                isLimited);
            character.ConfigureActiveSkills(skill2, skill3);
            EditorUtility.SetDirty(character);

            return character;
        }

        private static void CreateStandardCharacterProfile(
            CharacterDefinition character,
            CharacterElement element,
            string title,
            IEnumerable<string> keywords,
            string acquisitionSummary,
            bool includeStarterSource)
        {
            CharacterContentProfile profile = GetOrCreateAsset<CharacterContentProfile>(
                $"{CharacterProfileFolder}/Profile_{character.Id}.asset",
                out bool profileCreated);
            var abilities = new List<CharacterAbilityRecord>
            {
                new CharacterAbilityRecord(
                    $"{character.Id}_basic",
                    CharacterAbilityKind.Basic,
                    RuntimeSkillSlot.None,
                    null,
                    "Resonant Strike",
                    "A role-aware basic attack that uses the character's authored range and tempo.",
                    "Repeats after the attack interval when a valid target is in range.",
                    "Current locked target.",
                    "Deals physical damage and builds Rage on hit.",
                    1,
                    1,
                    SkillTag.Damage | SkillTag.Physical,
                    new[] { new SkillRankRecord(1, "Base combat pattern available.") }),
                CreateRuntimeAbilityRecord(character, character.UltimateSkill, RuntimeSkillSlot.Ultimate),
                CreateRuntimeAbilityRecord(character, character.Skill2, RuntimeSkillSlot.Skill2),
                CreateRuntimeAbilityRecord(character, character.Skill3, RuntimeSkillSlot.Skill3),
                new CharacterAbilityRecord(
                    $"{character.Id}_resonance",
                    CharacterAbilityKind.Passive,
                    RuntimeSkillSlot.None,
                    null,
                    $"{element} Resonance",
                    "Archive-ready passive slot reserved for future progression content.",
                    "Passive while deployed after its progression gate is met.",
                    "Self and compatible allies.",
                    "Carries elemental and role synergy tags without changing the current demo battle.",
                    20,
                    1,
                    SkillTag.Enhancement,
                    new[] { new SkillRankRecord(1, "Template hook; no runtime modifier in this milestone.") })
            };
            var stages = new[]
            {
                new ProgressionStageRecord(
                    ProgressionTrack.Ownership,
                    0,
                    1,
                    "Signal Registered",
                    "Unlocks the archive card, portrait, and base combat definition."),
                new ProgressionStageRecord(
                    ProgressionTrack.Level,
                    1,
                    20,
                    "Field Calibration",
                    "Base statistics and the three current runtime abilities are active."),
                new ProgressionStageRecord(
                    ProgressionTrack.Rank,
                    1,
                    40,
                    "Resonance Channel",
                    "Reserved template stage for rank materials and passive activation."),
                new ProgressionStageRecord(
                    ProgressionTrack.Awakening,
                    1,
                    60,
                    "Awakened Signal",
                    "Reserved template stage for future awakening effects and presentation assets.")
            };
            var acquisition = new List<AcquisitionRecord>
            {
                new AcquisitionRecord(
                    AcquisitionSource.StandardRecruitment,
                    "Standard Signal",
                    acquisitionSummary,
                    "Duplicates are counted in the current local demo state.")
            };
            if (includeStarterSource)
            {
                acquisition.Add(new AcquisitionRecord(
                    AcquisitionSource.Starter,
                    "Demo Starter",
                    "Granted in a fresh local demo save for formation testing.",
                    "Not applicable to the initial grant."));
            }

            profile.BindToCharacter(character.Id);
            if (ShouldInitializeProfile(profileCreated, profile))
            {
                profile.Configure(
                    "1.0",
                    "2026.07.15",
                    ContentApprovalStatus.Approved,
                    title,
                    element,
                    "Abyssal Observatory",
                    keywords,
                    60,
                    "Static authoring template only; the current save keeps Level and Copies without upgrade commands.",
                    "Awakening is documented as a future content gate and does not alter the P0 simulation.",
                    stages,
                    abilities,
                    acquisition,
                    new[] { element.ToString().ToLowerInvariant(), character.Role.ToString().ToLowerInvariant() },
                    new[] { "control_pressure", "formation_spacing" },
                    true,
                    true,
                    "ART-UI-CHARACTER-PORTRAITS-001",
                    "Original BubbleMind profile generated from the reusable content template.");
            }

            EditorUtility.SetDirty(profile);
            character.ConfigureContentProfile(profile);
            EditorUtility.SetDirty(character);
        }

        private static CharacterAbilityRecord CreateRuntimeAbilityRecord(
            CharacterDefinition character,
            SkillDefinition skill,
            RuntimeSkillSlot slot)
        {
            CharacterAbilityKind kind = slot == RuntimeSkillSlot.Ultimate
                ? CharacterAbilityKind.Ultimate
                : CharacterAbilityKind.Active;
            string trigger = slot == RuntimeSkillSlot.Ultimate
                ? $"Automatically casts when Rage reaches {skill.RageCost}."
                : slot == RuntimeSkillSlot.Skill2
                    ? "Automatically casts at 5 seconds, then every 10 seconds."
                    : "Automatically casts at 10 seconds, then every 10 seconds.";
            return new CharacterAbilityRecord(
                $"{character.Id}_{slot.ToString().ToLowerInvariant()}",
                kind,
                slot,
                skill,
                skill.DisplayName,
                skill.Description,
                trigger,
                skill.TargetMode.ToString(),
                skill.Description,
                1,
                1,
                skill.Tags,
                new[]
                {
                    new SkillRankRecord(
                        1,
                        "Runtime power is read directly from the linked SkillDefinition.")
                });
        }

        private static void CreateCatherineContentProfile(
            CharacterDefinition character,
            SkillDefinition starRageSkill)
        {
            CharacterContentProfile profile = GetOrCreateAsset<CharacterContentProfile>(
                $"{CharacterProfileFolder}/Profile_{character.Id}.asset",
                out bool profileCreated);
            var abilities = new List<CharacterAbilityRecord>
            {
                new CharacterAbilityRecord(
                    "catherine_basic_gravity_strike",
                    CharacterAbilityKind.Basic,
                    RuntimeSkillSlot.None,
                    null,
                    "Gravity Strike",
                    "A close-range physical attack that builds Rage and holds the nearest target.",
                    "Repeats after the attack interval while the locked target remains alive.",
                    "Nearest living enemy within two grid units.",
                    "Deals physical damage and grants Rage on hit.",
                    1,
                    1,
                    SkillTag.Damage | SkillTag.Physical,
                    new[] { new SkillRankRecord(1, "Maxed demo basic attack pattern.") }),
                new CharacterAbilityRecord(
                    "catherine_infinite_void",
                    CharacterAbilityKind.Ultimate,
                    RuntimeSkillSlot.Ultimate,
                    character.UltimateSkill,
                    character.UltimateSkill.DisplayName,
                    "Transforms into a humanoid black hole, gathers every enemy, deals repeated damage, then collapses and launches them.",
                    "Casts at 1000 Rage and returns Rage to zero.",
                    "All living enemies.",
                    "Charge, pull, repeated physical damage, collapse, and launch.",
                    1,
                    9,
                    SkillTag.Damage | SkillTag.Control | SkillTag.Area | SkillTag.Physical,
                    CreateDamageRanks(new[] { 720f, 750f, 780f, 810f, 840f, 870f, 900f, 930f, 960f })),
                new CharacterAbilityRecord(
                    "catherine_wind_wheel_break",
                    CharacterAbilityKind.Active,
                    RuntimeSkillSlot.Skill2,
                    character.Skill2,
                    character.Skill2.DisplayName,
                    "Shapes the wind wheel into a focused line attack that breaks through enemies and lifts them.",
                    "Automatically casts at 5 seconds, then every 10 seconds.",
                    "Enemies in a forward line; knockback is capped by the 20-grid arena.",
                    "Piercing physical damage, defense break presentation, and knock-up control.",
                    11,
                    5,
                    SkillTag.Damage | SkillTag.Control | SkillTag.Area | SkillTag.Physical,
                    CreateDamageRanks(new[] { 480f, 510f, 540f, 570f, 600f })),
                new CharacterAbilityRecord(
                    "catherine_wind_wheel_dance",
                    CharacterAbilityKind.Active,
                    RuntimeSkillSlot.Skill3,
                    character.Skill3,
                    character.Skill3.DisplayName,
                    "A two-hit assault that restores health from damage, charges forward, Taunts, and grants Super Armor during the sequence.",
                    "Automatically casts at 10 seconds, then every 10 seconds.",
                    "Current locked enemy, then a forward charge target.",
                    "Two physical hits, 140% damage-to-healing conversion, Taunt, and Super Armor.",
                    41,
                    4,
                    SkillTag.Damage | SkillTag.Healing | SkillTag.Control | SkillTag.Survival |
                    SkillTag.Taunt | SkillTag.Physical,
                    CreateDamageRanks(new[] { 400f, 440f, 480f, 520f })),
                new CharacterAbilityRecord(
                    "catherine_star_rage",
                    CharacterAbilityKind.Domain,
                    RuntimeSkillSlot.None,
                    starRageSkill,
                    starRageSkill.DisplayName,
                    "Enemy active skills can grant Imaginary Mass, improving final damage reduction and scaling Infinite Void before stacks convert into maximum health.",
                    "Checks whenever an enemy hero casts an active skill.",
                    "Self.",
                    "Up to 30 stacks; 10/20/30 stacks scale the ultimate to 2x/3x/4x, then up to 20 stacks convert to Max HP.",
                    61,
                    9,
                    SkillTag.Survival | SkillTag.Enhancement,
                    CreateStarRageRanks()),
                new CharacterAbilityRecord(
                    "catherine_awakening",
                    CharacterAbilityKind.Awakening,
                    RuntimeSkillSlot.None,
                    null,
                    "Singularity Awakening",
                    "Permanent offense and defense gains expand the stack ceiling; a once-per-battle death trigger detonates Infinite Void and revives Catherine.",
                    "Permanent after awakening; the revival clause triggers once on death.",
                    "Self and all enemies during the death detonation.",
                    "Damage dealt and reduction +35%; 50-stack ultimate can reach 6x; death detonation revives to 99% HP and grants 20 stacks.",
                    61,
                    2,
                    SkillTag.Damage | SkillTag.Survival | SkillTag.Enhancement | SkillTag.Revival,
                    new[]
                    {
                        new SkillRankRecord(1, "Permanent bonuses and expanded Imaginary Mass scaling."),
                        new SkillRankRecord(2, "Once-per-battle death detonation and revival.")
                    })
            };
            var stages = new[]
            {
                new ProgressionStageRecord(
                    ProgressionTrack.Ownership,
                    0,
                    1,
                    "Limited Signal Registered",
                    "Unlocks the archive, event-horizon portrait, and tank battle definition."),
                new ProgressionStageRecord(
                    ProgressionTrack.Level,
                    11,
                    11,
                    "Wind Wheel: Break",
                    "First timed active ability becomes available in the complete progression model."),
                new ProgressionStageRecord(
                    ProgressionTrack.Level,
                    41,
                    41,
                    "Wind Wheel: Dance",
                    "Survival and Taunt sequence becomes available."),
                new ProgressionStageRecord(
                    ProgressionTrack.Level,
                    61,
                    61,
                    "Star Rage",
                    "Domain and Imaginary Mass progression become available."),
                new ProgressionStageRecord(
                    ProgressionTrack.Awakening,
                    1,
                    61,
                    "Singularity Awakening",
                    "Adds permanent bonuses, expanded stacks, death detonation, and revival.")
            };
            var acquisition = new[]
            {
                new AcquisitionRecord(
                    AcquisitionSource.LimitedRecruitment,
                    "Limited Signal",
                    "Excluded from the Standard Signal pool; reserved for a limited banner template.",
                    "Duplicate conversion is reserved for a future inventory schema."),
                new AcquisitionRecord(
                    AcquisitionSource.Starter,
                    "Demo Test Grant",
                    "Granted in fresh demo saves so the maxed UR battle kit can always be tested.",
                    "Not applicable to the initial grant.")
            };

            profile.BindToCharacter(character.Id);
            if (ShouldInitializeProfile(profileCreated, profile))
            {
                profile.Configure(
                    "1.0",
                    "2026.07.15",
                    ContentApprovalStatus.Approved,
                    "Limited Singularity Guardian",
                    CharacterElement.Void,
                    "Abyssal Observatory",
                    new[] { "tank", "gravity", "control", "survival", "limited", "maxed-demo" },
                    61,
                    "The current demo always runs the maximum authored ability ranks and full awakening test behavior.",
                    "Two-stage awakening expands Imaginary Mass scaling and adds a once-per-battle revival sequence.",
                    stages,
                    abilities,
                    acquisition,
                    new[] { "frontline_anchor", "area_damage", "backline_followup" },
                    new[] { "healing_denial", "stack_suppression", "displacement_immunity" },
                    true,
                    true,
                    "ART-CHAR-UR-COSMIC-SLIME-001; ART-UI-CHARACTER-PORTRAITS-001",
                    "User-authored maxed UR design normalized into the BubbleMind content template.");
            }

            EditorUtility.SetDirty(profile);
            character.ConfigureContentProfile(profile);
            EditorUtility.SetDirty(character);
        }

        private static IEnumerable<SkillRankRecord> CreateDamageRanks(IReadOnlyList<float> percentages)
        {
            var ranks = new List<SkillRankRecord>();
            for (int i = 0; i < percentages.Count; i++)
            {
                ranks.Add(new SkillRankRecord(
                    i + 1,
                    $"Damage scales to {percentages[i]:0}% of Attack.",
                    new[]
                    {
                        new SkillValueRecord(
                            "damage",
                            percentages[i],
                            SkillValueUnit.PercentOfAttack)
                    }));
            }

            return ranks;
        }

        private static IEnumerable<SkillRankRecord> CreateStarRageRanks()
        {
            return new[]
            {
                new SkillRankRecord(1, "40% trigger chance.", new[] { new SkillValueRecord("trigger_chance", 40f, SkillValueUnit.Percent) }),
                new SkillRankRecord(2, "50% trigger chance.", new[] { new SkillValueRecord("trigger_chance", 50f, SkillValueUnit.Percent) }),
                new SkillRankRecord(3, "60% trigger chance.", new[] { new SkillValueRecord("trigger_chance", 60f, SkillValueUnit.Percent) }),
                new SkillRankRecord(4, "70% trigger chance.", new[] { new SkillValueRecord("trigger_chance", 70f, SkillValueUnit.Percent) }),
                new SkillRankRecord(5, "80% trigger chance.", new[] { new SkillValueRecord("trigger_chance", 80f, SkillValueUnit.Percent) }),
                new SkillRankRecord(6, "Gain two stacks per trigger.", new[] { new SkillValueRecord("stacks_per_trigger", 2f, SkillValueUnit.Stacks) }),
                new SkillRankRecord(7, "Each converted stack grants 3% Max HP.", new[] { new SkillValueRecord("max_hp_per_stack", 3f, SkillValueUnit.PercentOfMaxHealth) }),
                new SkillRankRecord(8, "Each converted stack grants 4% Max HP.", new[] { new SkillValueRecord("max_hp_per_stack", 4f, SkillValueUnit.PercentOfMaxHealth) }),
                new SkillRankRecord(9, "99% trigger chance.", new[] { new SkillValueRecord("trigger_chance", 99f, SkillValueUnit.Percent) })
            };
        }

        private static void ConfigurePortraitImportSettings()
        {
            string[] ids =
            {
                "azure_vanguard",
                "ember_striker",
                "verdant_medic",
                "ur_cosmic_slime",
                "violet_arcanist",
                "gold_ranger",
                "cyan_warden"
            };

            for (int i = 0; i < ids.Length; i++)
            {
                string path = $"{PortraitFolder}/Portrait_{ids[i]}.png";
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    throw new FileNotFoundException($"Required character portrait is missing: {path}");
                }

                bool changed = importer.textureType != TextureImporterType.Sprite ||
                               importer.spriteImportMode != SpriteImportMode.Single ||
                               importer.mipmapEnabled ||
                               importer.wrapMode != TextureWrapMode.Clamp ||
                               importer.filterMode != FilterMode.Bilinear ||
                               !importer.sRGBTexture ||
                               importer.alphaIsTransparency ||
                               importer.maxTextureSize != 1024;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.sRGBTexture = true;
                importer.alphaIsTransparency = false;
                importer.maxTextureSize = 1024;
                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static void ConfigurePixelImportSettings()
        {
            string[] characterIds =
            {
                "ur_cosmic_slime",
                "azure_vanguard",
                "ember_striker",
                "gold_ranger",
                "verdant_medic",
                "violet_arcanist",
                "cyan_warden"
            };

            for (int i = 0; i < characterIds.Length; i++)
            {
                ConfigurePixelSpriteImporter($"{PixelSpriteFolder}/Pixel_{characterIds[i]}.png", false);
            }

            ConfigurePixelSpriteImporter($"{PixelSpriteFolder}/PixelShadow.png", true);

            TextureImporter arenaImporter = AssetImporter.GetAtPath(PixelArenaPath) as TextureImporter;
            if (arenaImporter == null)
            {
                throw new FileNotFoundException($"Required Pixel2D arena is missing: {PixelArenaPath}");
            }

            bool arenaChanged = arenaImporter.textureType != TextureImporterType.Default ||
                                arenaImporter.mipmapEnabled ||
                                arenaImporter.wrapMode != TextureWrapMode.Clamp ||
                                arenaImporter.filterMode != FilterMode.Point ||
                                !arenaImporter.sRGBTexture ||
                                arenaImporter.textureCompression != TextureImporterCompression.Uncompressed ||
                                arenaImporter.npotScale != TextureImporterNPOTScale.None ||
                                arenaImporter.maxTextureSize != 512;
            arenaImporter.textureType = TextureImporterType.Default;
            arenaImporter.mipmapEnabled = false;
            arenaImporter.wrapMode = TextureWrapMode.Clamp;
            arenaImporter.filterMode = FilterMode.Point;
            arenaImporter.sRGBTexture = true;
            arenaImporter.textureCompression = TextureImporterCompression.Uncompressed;
            arenaImporter.npotScale = TextureImporterNPOTScale.None;
            arenaImporter.maxTextureSize = 512;
            if (arenaChanged)
            {
                arenaImporter.SaveAndReimport();
            }
        }

        private static void ConfigurePixelSpriteImporter(string path, bool shadow)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException($"Required Pixel2D sprite is missing: {path}");
            }

            Vector2 requiredPivot = shadow ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0.03f);
            bool changed = importer.textureType != TextureImporterType.Sprite ||
                           importer.spriteImportMode != SpriteImportMode.Single ||
                           importer.mipmapEnabled ||
                           importer.wrapMode != TextureWrapMode.Clamp ||
                           importer.filterMode != FilterMode.Point ||
                           !importer.sRGBTexture ||
                           !importer.alphaIsTransparency ||
                           importer.textureCompression != TextureImporterCompression.Uncompressed ||
                           !Mathf.Approximately(importer.spritePixelsPerUnit, PixelCharacterBuilder.PixelsPerUnit) ||
                           Vector2.Distance(importer.spritePivot, requiredPivot) > 0.0001f ||
                           importer.maxTextureSize != 256;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Point;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = PixelCharacterBuilder.PixelsPerUnit;
            importer.spritePivot = requiredPivot;
            importer.maxTextureSize = 256;
            if (changed)
            {
                importer.SaveAndReimport();
            }
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
            camera.orthographic = true;
            camera.orthographicSize = 6.35f;
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
            EnsureFolder("Assets/_Game/Data", "CharacterProfiles");
            EnsureFolder("Assets/_Game/Data", "Gacha");
            EnsureFolder("Assets/_Game/Data", "Stages");
            EnsureFolder("Assets/_Game", "Scenes");
            EnsureFolder("Assets/_Game", "Art");
            EnsureFolder("Assets/_Game/Art", "Generated");
            EnsureFolder("Assets/_Game/Art/Generated", "UI");
            EnsureFolder("Assets/_Game/Art/Generated/UI", "Portraits");
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
