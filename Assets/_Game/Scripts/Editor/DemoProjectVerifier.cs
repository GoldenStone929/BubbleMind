using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GenericGachaRPG.Editor
{
    public static class DemoProjectVerifier
    {
        public const string PassMarker = "[GenericGachaRPG][P0_VERIFY_PASS_20260713]";

        private const string MenuPath = "Tools/Generic Gacha RPG/Verify P0 Demo";
        private const string DatabasePath = "Assets/_Game/Data/GameDatabase.asset";
        private const string ScenePath = "Assets/_Game/Scenes/GachaRPGDemo.unity";
        private const string BackdropTexturePath =
            "Assets/_Game/Art/Generated/Environments/AbyssalObservatory/Textures/Resources/AbyssalObservatory_Concept.png";
        private const int VerificationSeed = 731925;

        [MenuItem(MenuPath, priority = 12)]
        public static void VerifyFromMenu()
        {
            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            Verify(database);
        }

        public static void Verify(GameDatabase database)
        {
            VerifyRulesContract();
            GachaBannerDefinition banner = VerifyDatabase(database);
            GameStateService gameState = VerifyDefaultSave(database);
            VerifySingleDraw(database, banner, gameState);
            VerifyFormation(database, gameState);
            VerifyBattleDeterminism(database, gameState.State.TeamFormation.CharacterIds);
            VerifySceneAndBuildSettings();

            Debug.Log(
                $"{PassMarker} Database, in-memory save, gacha, formation, deterministic battle, scene, and Build Settings all passed.");
        }

        private static GachaBannerDefinition VerifyDatabase(GameDatabase database)
        {
            Require(database != null, $"Database asset is missing at '{DatabasePath}'.");
            Require(database.Characters != null, "Database character list is null.");
            Require(database.Skills != null, "Database skill list is null.");
            Require(database.GachaBanners != null, "Database banner list is null.");
            Require(database.Characters.Count == 7,
                $"Database must contain exactly 7 characters; found {database.Characters.Count}.");
            Require(database.Skills.Count == 9,
                $"Database must contain exactly 9 skills; found {database.Skills.Count}.");

            var characterIds = new HashSet<string>(StringComparer.Ordinal);
            int limitedCharacterCount = 0;
            for (int i = 0; i < database.Characters.Count; i++)
            {
                CharacterDefinition character = database.Characters[i];
                Require(character != null, $"Character slot {i} is null.");
                Require(!string.IsNullOrWhiteSpace(character.Id), $"Character slot {i} has an empty id.");
                Require(characterIds.Add(character.Id), $"Character id '{character.Id}' is duplicated.");
                Require(character.UltimateSkill != null && character.Skill2 != null && character.Skill3 != null,
                    $"Character '{character.Id}' does not have all three skill slots.");
                Require(ContainsReference(database.Skills, character.UltimateSkill) &&
                        ContainsReference(database.Skills, character.Skill2) &&
                        ContainsReference(database.Skills, character.Skill3),
                    $"Character '{character.Id}' references a skill outside the database.");
                Require(character.UltimateSkill.RageCost == BattleRules.MaxRage,
                    $"Character '{character.Id}' ultimate does not cost 1000 Rage.");
                Require(character.Skill2.RageCost == 0 && character.Skill3.RageCost == 0,
                    $"Character '{character.Id}' timed active skills must not consume Rage.");
                Require(character.MaxRage == BattleRules.MaxRage &&
                        character.RagePerAttack == BattleRules.RagePerBasicAttackHit &&
                        character.RageWhenHit == BattleRules.RagePerDamageReceived,
                    $"Character '{character.Id}' Rage profile does not match the global contract.");
                Require(IsFinitePositive(character.MaxHealth),
                    $"Character '{character.Id}' has invalid MaxHealth {character.MaxHealth}.");
                Require(IsFiniteNonNegative(character.Attack) && IsFiniteNonNegative(character.Defense),
                    $"Character '{character.Id}' has invalid combat stats.");
                Require(IsFinitePositive(character.AttackInterval),
                    $"Character '{character.Id}' has invalid AttackInterval {character.AttackInterval}.");
                Require(IsFinitePositive(character.AttackRange),
                    $"Character '{character.Id}' has invalid AttackRange {character.AttackRange}.");
                Require(Mathf.Approximately(
                            character.AttackRange,
                            BattleRules.GetDefaultAttackRange(character.Role)),
                    $"Character '{character.Id}' does not use its role's fixed attack range.");
                Require(IsFinitePositive(character.MoveSpeed),
                    $"Character '{character.Id}' has invalid MoveSpeed {character.MoveSpeed}.");
                Require(Enum.IsDefined(typeof(Rarity), character.Rarity),
                    $"Character '{character.Id}' has an undefined rarity value {(int)character.Rarity}.");
                Require(Enum.IsDefined(typeof(CharacterRole), character.Role),
                    $"Character '{character.Id}' has an undefined role value {(int)character.Role}.");
                if (character.IsLimited)
                {
                    limitedCharacterCount++;
                }
            }

            Require(limitedCharacterCount == 1,
                $"Demo database must contain exactly one limited character; found {limitedCharacterCount}.");

            var skillIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < database.Skills.Count; i++)
            {
                SkillDefinition skill = database.Skills[i];
                Require(skill != null, $"Skill slot {i} is null.");
                Require(!string.IsNullOrWhiteSpace(skill.Id), $"Skill slot {i} has an empty id.");
                Require(skillIds.Add(skill.Id), $"Skill id '{skill.Id}' is duplicated.");
                Require(skill.TargetCount > 0, $"Skill '{skill.Id}' has no valid targets.");
                Require(skill.RageCost >= 0, $"Skill '{skill.Id}' has a negative rage cost.");
                Require(IsFiniteNonNegative(skill.HitTiming), $"Skill '{skill.Id}' has invalid hit timing.");
            }

            SkillDefinition spectrumNova = database.GetSkill("spectrum_nova");
            Require(spectrumNova != null &&
                    spectrumNova.Category == SkillCategory.Damage &&
                    spectrumNova.TargetMode == SkillTargetMode.AllEnemies &&
                    spectrumNova.TargetCount == BattleRules.TeamSize,
                "Spectrum Nova must target every opponent in the five-unit battle.");
            VerifyCatherineSkillDefinitions(database);

            Require(database.GachaBanners.Count > 0, "Database has no gacha banner.");
            GachaBannerDefinition banner = database.DefaultBanner;
            Require(banner != null, "Database default banner is null.");
            Require(banner.SingleDrawCost > 0,
                $"Default banner cost must be positive; found {banner.SingleDrawCost}.");
            Require(banner.Entries != null && banner.Entries.Count > 0,
                "Default banner has no pool entries.");

            double validWeight = 0d;
            for (int i = 0; i < banner.Entries.Count; i++)
            {
                GachaPoolEntry entry = banner.Entries[i];
                Require(entry != null, $"Default banner entry {i} is null.");
                Require(IsFinitePositive(entry.Weight),
                    $"Default banner entry {i} has invalid weight {entry.Weight}.");
                Require(database.TryGetCharacter(entry.CharacterId, out CharacterDefinition bannerCharacter),
                    $"Default banner entry {i} references unknown character '{entry.CharacterId}'.");
                Require(!bannerCharacter.IsLimited,
                    $"Standard banner must not contain limited character '{entry.CharacterId}'.");
                validWeight += entry.Weight;
            }

            Require(validWeight > 0d && !double.IsNaN(validWeight) && !double.IsInfinity(validWeight),
                "Default banner total weight is invalid.");
            Require(IsFinitePositive(banner.TotalWeight),
                $"Default banner reports invalid TotalWeight {banner.TotalWeight}.");
            Require(Math.Abs(validWeight - banner.TotalWeight) <= 0.001d,
                $"Default banner TotalWeight mismatch: entries={validWeight}, banner={banner.TotalWeight}.");
            Require(database.StartingCurrency >= banner.SingleDrawCost,
                "Starting currency must be enough for at least one demo draw.");

            CharacterDefinition cosmicSlime = database.GetCharacter("ur_cosmic_slime");
            Require(cosmicSlime != null, "Cosmic Slime definition is missing.");
            Require(cosmicSlime.Rarity == Rarity.UR, "Cosmic Slime must use UR rarity.");
            Require(cosmicSlime.Role == CharacterRole.Tank, "Cosmic Slime must use the tank role.");
            Require(cosmicSlime.IsLimited, "Cosmic Slime must be marked as limited.");
            Require(Mathf.Approximately(cosmicSlime.AttackRange, BattleRules.MeleeAttackRange),
                $"Cosmic Slime must use melee tank range; found {cosmicSlime.AttackRange}.");
            Require(cosmicSlime.CharacterPrefab != null, "Cosmic Slime prefab is missing.");
            Require(cosmicSlime.CharacterPrefab.GetComponent<CharacterView>() != null,
                "Cosmic Slime prefab must have a root CharacterView.");
            Require(cosmicSlime.CharacterPrefab.GetComponent<CosmicSlimeVisualController>() != null,
                "Catherine Yuki prefab must have CosmicSlimeVisualController.");
            Require(cosmicSlime.CharacterPrefab.GetComponent<CatherineSkillVfxController>() != null,
                "Catherine Yuki prefab must have CatherineSkillVfxController.");
            Require(Shader.Find("BubbleMind/Slime Toon") != null,
                "BubbleMind/Slime Toon shader is unavailable.");
            Require(Shader.Find("BubbleMind/Black Hole VFX") != null,
                "BubbleMind/Black Hole VFX shader is unavailable.");
            Require(PrefabUsesShader(cosmicSlime.CharacterPrefab, "BubbleMind/Slime Toon"),
                "Catherine Yuki shell must use the BubbleMind/Slime Toon shader.");
            Require(cosmicSlime.Skill != null &&
                    string.Equals(
                        cosmicSlime.Skill.Id,
                        CatherineYukiBattleKit.UltimateId,
                        StringComparison.Ordinal),
                "Catherine Yuki must expose Infinite Void as the authored character skill.");
            Require(cosmicSlime.Skill2 != null &&
                    string.Equals(cosmicSlime.Skill2.Id, CatherineYukiBattleKit.Skill1Id, StringComparison.Ordinal) &&
                    cosmicSlime.Skill3 != null &&
                    string.Equals(cosmicSlime.Skill3.Id, CatherineYukiBattleKit.Skill2Id, StringComparison.Ordinal),
                "Catherine Skill 2/3 slots must be Wind Wheel: Break and Wind Wheel: Dance.");

            RequireCharacterRarity(database, "azure_vanguard", Rarity.R);
            RequireCharacterRarity(database, "ember_striker", Rarity.R);
            RequireCharacterRarity(database, "verdant_medic", Rarity.SR);
            RequireCharacterRarity(database, "violet_arcanist", Rarity.SR);
            RequireCharacterRarity(database, "gold_ranger", Rarity.SSR);
            RequireCharacterRarity(database, "cyan_warden", Rarity.SSR);
            RequireCharacterCombatProfile(database, "azure_vanguard", BattleRules.MeleeAttackRange, 3.2f);
            RequireCharacterCombatProfile(database, "ember_striker", BattleRules.MeleeAttackRange, 4.2f);
            RequireCharacterCombatProfile(database, "verdant_medic", BattleRules.RangedAttackRange, 3.0f);
            RequireCharacterCombatProfile(database, "ur_cosmic_slime", BattleRules.MeleeAttackRange, 3.3f);
            RequireCharacterCombatProfile(database, "violet_arcanist", BattleRules.RangedAttackRange, 3.4f);
            RequireCharacterCombatProfile(database, "gold_ranger", BattleRules.RangedAttackRange, 3.8f);
            RequireCharacterCombatProfile(database, "cyan_warden", BattleRules.MeleeAttackRange, 3.15f);

            VerifyBasicSlimeAssets(database);
            VerifyBackdropTextureImport();

            Material backdropMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                AbyssalObservatoryAssetBuilder.BackdropMaterialPath);
            Require(backdropMaterial != null, "Abyssal Observatory backdrop material is missing.");
            Require(backdropMaterial.shader != null, "Abyssal Observatory backdrop material has no shader.");

            return banner;
        }

        private static void VerifyCatherineSkillDefinitions(GameDatabase database)
        {
            SkillDefinition skill1 = database.GetSkill(CatherineYukiBattleKit.Skill1Id);
            SkillDefinition skill2 = database.GetSkill(CatherineYukiBattleKit.Skill2Id);
            SkillDefinition skill3 = database.GetSkill(CatherineYukiBattleKit.Skill3Id);
            SkillDefinition ultimate = database.GetSkill(CatherineYukiBattleKit.UltimateId);

            Require(skill1 != null &&
                    skill1.RageCost == 0 &&
                    skill1.TargetMode == SkillTargetMode.AllEnemies &&
                    Mathf.Approximately(
                        skill1.DamageMultiplier,
                        CatherineYukiBattleKit.Skill1DamageMultiplier),
                "Catherine Skill 1 must be the max-level 600% line-break profile.");
            Require(skill2 != null &&
                    skill2.RageCost == 0 &&
                    skill2.TargetMode == SkillTargetMode.SingleEnemy &&
                    Mathf.Approximately(
                        skill2.DamageMultiplier,
                        CatherineYukiBattleKit.Skill2HitDamageMultiplier * 2f) &&
                    Mathf.Approximately(
                        skill2.HealingMultiplier,
                        CatherineYukiBattleKit.Skill2HealingFromDamageMultiplier),
                "Catherine Skill 2 must be the max-level two-hit and 140% recovery profile.");
            Require(skill3 != null &&
                    skill3.RageCost == 0 &&
                    skill3.TargetMode == SkillTargetMode.AllEnemies &&
                    Mathf.Approximately(skill3.DamageMultiplier, 0f),
                "Catherine Skill 3 must be the deterministic debuff and mass-stack test profile.");
            Require(ultimate != null &&
                    ultimate.RageCost == BattleRules.MaxRage &&
                    ultimate.TargetMode == SkillTargetMode.AllEnemies &&
                    ultimate.TargetCount == BattleRules.TeamSize &&
                    Mathf.Approximately(
                        ultimate.DamageMultiplier,
                        CatherineYukiBattleKit.UltimateBaseDamageMultiplier),
                "Catherine ultimate must use the max-level 960% all-enemy base profile.");
            Require(CatherineYukiBattleKit.InitialImaginaryMassStacks == 30 &&
                    Mathf.Approximately(
                        CatherineYukiBattleKit.GetUltimateScaling(30, false),
                        4f),
                "Catherine demo must start at 30 Imaginary Mass stacks and resolve a 4x ultimate.");
        }

        private static bool PrefabUsesShader(GameObject prefab, string shaderName)
        {
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Material[] materials = renderers[rendererIndex].sharedMaterials;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (material != null &&
                        material.shader != null &&
                        string.Equals(material.shader.name, shaderName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void VerifyBasicSlimeAssets(GameDatabase database)
        {
            RequireBasicSlimeCharacter(database, "azure_vanguard", BasicSlimeElement.Water);
            RequireBasicSlimeCharacter(database, "ember_striker", BasicSlimeElement.Fire);
            RequireBasicSlimeCharacter(database, "gold_ranger", BasicSlimeElement.Earth);
            RequireBasicSlimeCharacter(database, "verdant_medic", BasicSlimeElement.Wind);
            RequireBasicSlimeCharacter(database, "violet_arcanist", BasicSlimeElement.Lightning);
            RequireBasicSlimeCharacter(database, "cyan_warden", BasicSlimeElement.Water);

            BasicSlimeElement[] elements = BasicElementSlimeAssetBuilder.GetAllElements();
            for (int i = 0; i < elements.Length; i++)
            {
                BasicSlimeElement element = elements[i];
                string modelPath = BasicElementSlimeAssetBuilder.GetModelPath(element);
                GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                Require(modelAsset != null, $"Basic {element} Slime FBX is missing at '{modelPath}'.");

                ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
                Require(importer != null, $"Basic {element} Slime has no ModelImporter.");
                Require(!importer.importAnimation &&
                        importer.animationType == ModelImporterAnimationType.None &&
                        importer.preserveHierarchy &&
                        !importer.isReadable &&
                        !importer.addCollider &&
                        importer.materialImportMode == ModelImporterMaterialImportMode.None &&
                        importer.importNormals == ModelImporterNormals.Import &&
                        importer.importTangents == ModelImporterTangents.CalculateMikk,
                    $"Basic {element} Slime ModelImporter settings do not match the runtime contract.");

                string prefabPath = BasicElementSlimeAssetBuilder.GetPrefabPath(element);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Require(prefab != null, $"Basic {element} Slime prefab is missing at '{prefabPath}'.");

                CharacterView view = prefab.GetComponent<CharacterView>();
                BasicSlimeVisualController visualController = prefab.GetComponent<BasicSlimeVisualController>();
                Require(view != null, $"Basic {element} Slime prefab has no root CharacterView.");
                Require(visualController != null && visualController.Element == element,
                    $"Basic {element} Slime prefab has an invalid visual-controller marker.");
                Require(prefab.GetComponent<ProceduralCharacterBuilder>() == null,
                    $"Basic {element} Slime prefab must not use ProceduralCharacterBuilder.");

                Transform modelRoot = FindDirectChild(prefab.transform, "ModelRoot");
                Require(modelRoot != null && view.ModelRoot == modelRoot,
                    $"Basic {element} Slime prefab must expose a dedicated ModelRoot.");
                Require(view.RightHand != null &&
                        view.LeftHand != null &&
                        view.SkillVfx != null &&
                        view.Projectile != null &&
                        view.GroundVfx != null &&
                        view.Target != null &&
                        view.HealthBar != null,
                    $"Basic {element} Slime prefab is missing one or more CharacterView sockets.");
                Require(FindDescendant(prefab.transform, "SlimeBody") != null,
                    $"Basic {element} Slime prefab is missing the SlimeBody model node.");
                Require(HasDescendantWithPrefix(prefab.transform, "FaceDark", "EyeDark", "EyeHighlight", "Eye_"),
                    $"Basic {element} Slime prefab is missing readable face nodes.");
                Require(HasDescendantWithPrefix(prefab.transform, GetDecorationPrefixes(element)),
                    $"Basic {element} Slime prefab is missing its element decoration nodes.");

                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                Require(renderers.Length > 0, $"Basic {element} Slime prefab has no renderers.");
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Material[] materials = renderers[rendererIndex].sharedMaterials;
                    Require(materials != null && materials.Length > 0,
                        $"Basic {element} Slime renderer '{renderers[rendererIndex].name}' has no materials.");
                    for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                    {
                        Material material = materials[materialIndex];
                        string materialPath = AssetDatabase.GetAssetPath(material);
                        Require(material != null &&
                                material.shader != null &&
                                materialPath.StartsWith(
                                    BasicElementSlimeAssetBuilder.RuntimeFolder + "/Materials/",
                                    StringComparison.Ordinal),
                            $"Basic {element} Slime renderer '{renderers[rendererIndex].name}' has an invalid material.");
                        Require(!material.HasProperty("_Surface") || material.GetFloat("_Surface") < 0.5f,
                            $"Basic {element} Slime material '{material.name}' must remain opaque.");
                    }
                }

                VerifyBasicSlimeMaterialRouting(prefab, element);
            }
        }

        private static void VerifyBasicSlimeMaterialRouting(GameObject prefab, BasicSlimeElement element)
        {
            string prefix = "MAT_BasicSlime_" + element;
            RequireRendererUsesMaterial(prefab, "SlimeBody", prefix + "_Body");
            RequireRendererUsesMaterial(prefab, "EyeWhite_L", "MAT_BasicSlime_EyeWhite");
            RequireRendererUsesMaterial(prefab, "EyeIris_L", prefix + "_Detail");
            RequireRendererUsesMaterial(prefab, "FaceDark_Pupil_L", "MAT_BasicSlime_Face");
            RequireRendererUsesMaterial(prefab, "EyeHighlight_L", "MAT_BasicSlime_EyeHighlight");
            RequireRendererUsesMaterial(prefab, "Cheek_L", prefix + "_Accent");

            switch (element)
            {
                case BasicSlimeElement.Water:
                    RequireRendererUsesMaterial(prefab, "WaterCrest", prefix + "_Accent");
                    RequireRendererUsesMaterial(prefab, "WaterWave_Front", prefix + "_Detail");
                    RequireRendererUsesMaterial(prefab, "Bubble_01_Highlight", "MAT_BasicSlime_EyeHighlight");
                    break;
                case BasicSlimeElement.Fire:
                    RequireRendererUsesMaterial(prefab, "Flame_Center", prefix + "_Detail");
                    RequireRendererUsesMaterial(prefab, "Flame_Left", prefix + "_Body");
                    RequireRendererUsesMaterial(prefab, "Flame_Inner", prefix + "_Accent");
                    break;
                case BasicSlimeElement.Earth:
                    RequireRendererUsesMaterial(prefab, "Rock_Crown_01", prefix + "_Accent");
                    RequireRendererUsesMaterial(prefab, "Leaf_01", prefix + "_Detail");
                    break;
                case BasicSlimeElement.Wind:
                    RequireRendererUsesMaterial(prefab, "Wind_Spiral", prefix + "_Accent");
                    RequireRendererUsesMaterial(prefab, "Wind_Ribbon_L01", prefix + "_Detail");
                    break;
                case BasicSlimeElement.Lightning:
                    RequireRendererUsesMaterial(prefab, "Spark_Crown", prefix + "_Accent");
                    RequireRendererUsesMaterial(prefab, "Spark_Left", prefix + "_Detail");
                    break;
            }
        }

        private static void RequireRendererUsesMaterial(
            GameObject prefab,
            string rendererObjectName,
            string expectedMaterialName)
        {
            Transform part = FindDescendant(prefab.transform, rendererObjectName);
            Renderer renderer = part == null ? null : part.GetComponent<Renderer>();
            Require(renderer != null,
                $"Prefab '{prefab.name}' is missing renderer node '{rendererObjectName}'.");

            Material[] materials = renderer.sharedMaterials;
            Require(materials != null && materials.Length > 0,
                $"Renderer '{rendererObjectName}' on '{prefab.name}' has no material slots.");
            for (int i = 0; i < materials.Length; i++)
            {
                Require(materials[i] != null &&
                        string.Equals(materials[i].name, expectedMaterialName, StringComparison.Ordinal),
                    $"Renderer '{rendererObjectName}' on '{prefab.name}' must use " +
                    $"'{expectedMaterialName}', found '{materials[i]?.name ?? "<missing>"}'.");
            }
        }

        private static void RequireBasicSlimeCharacter(
            GameDatabase database,
            string characterId,
            BasicSlimeElement expectedElement)
        {
            CharacterDefinition character = database.GetCharacter(characterId);
            Require(character != null, $"Required character '{characterId}' is missing.");
            Require(BasicElementSlimeAssetBuilder.IsExpectedPrefab(character.CharacterPrefab, expectedElement),
                $"Character '{characterId}' must use the Basic {expectedElement} Slime prefab.");

            BasicSlimeVisualController visualController =
                character.CharacterPrefab.GetComponent<BasicSlimeVisualController>();
            Require(visualController != null && visualController.Element == expectedElement,
                $"Character '{characterId}' has an invalid Basic Slime element marker.");
        }

        private static void VerifyBackdropTextureImport()
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BackdropTexturePath);
            Require(texture != null, $"Abyssal Observatory artwork is missing at '{BackdropTexturePath}'.");
            float aspect = texture.width / (float)Mathf.Max(1, texture.height);
            Require(Mathf.Abs(aspect - 16f / 9f) <= 0.06f,
                $"Abyssal Observatory artwork must remain near 16:9; found {texture.width}x{texture.height}.");

            TextureImporter importer = AssetImporter.GetAtPath(BackdropTexturePath) as TextureImporter;
            Require(importer != null, "Abyssal Observatory artwork has no TextureImporter.");
            Require(importer.textureType == TextureImporterType.Default,
                "Abyssal Observatory artwork must use the Default texture type.");
            Require(importer.sRGBTexture, "Abyssal Observatory artwork must import as sRGB.");
            Require(!importer.isReadable, "Abyssal Observatory artwork must remain non-readable at runtime.");
            Require(importer.mipmapEnabled, "Abyssal Observatory artwork must keep mipmaps enabled.");
            Require(importer.maxTextureSize == 2048,
                $"Abyssal Observatory artwork max size must remain 2048; found {importer.maxTextureSize}.");
            Require(importer.npotScale == TextureImporterNPOTScale.None,
                "Abyssal Observatory artwork must use NPOT scale None.");
            Require(importer.wrapModeU == TextureWrapMode.Clamp &&
                    importer.wrapModeV == TextureWrapMode.Clamp &&
                    importer.wrapModeW == TextureWrapMode.Clamp,
                "Abyssal Observatory artwork must clamp on every wrap axis.");
        }

        private static string[] GetDecorationPrefixes(BasicSlimeElement element)
        {
            switch (element)
            {
                case BasicSlimeElement.Water:
                    return new[] { "Element_", "Bubble_", "Drop_", "Water_" };
                case BasicSlimeElement.Fire:
                    return new[] { "Element_", "Flame_", "Fire_" };
                case BasicSlimeElement.Earth:
                    return new[] { "Element_", "Leaf_", "Rock_", "Stone_", "Earth_" };
                case BasicSlimeElement.Wind:
                    return new[] { "Element_", "Wind_", "Ribbon_", "Swirl_" };
                case BasicSlimeElement.Lightning:
                    return new[] { "Element_", "Spark_", "Bolt_", "Lightning_" };
                default:
                    return new[] { "Element_" };
            }
        }

        private static Transform FindDirectChild(Transform parent, string objectName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(child.name, objectName, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static Transform FindDescendant(Transform root, string objectName)
        {
            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                if (string.Equals(descendants[i].name, objectName, StringComparison.Ordinal))
                {
                    return descendants[i];
                }
            }

            return null;
        }

        private static bool HasDescendantWithPrefix(Transform root, params string[] prefixes)
        {
            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                for (int prefixIndex = 0; prefixIndex < prefixes.Length; prefixIndex++)
                {
                    if (descendants[i].name.StartsWith(prefixes[prefixIndex], StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static GameStateService VerifyDefaultSave(GameDatabase database)
        {
            var save = new InMemorySaveService(database.CreateDefaultPlayerState);
            Require(!save.HasSave, "Fresh in-memory save unexpectedly reports existing data.");

            var gameState = new GameStateService(database, save);
            Require(save.HasSave, "Loading the default state did not create an in-memory save.");
            VerifyDefaultState(database, gameState.State);

            PlayerState firstLoad = save.Load();
            Require(firstLoad != null, "Default save could not be loaded back from JSON.");
            Require(firstLoad.SchemaVersion == PlayerState.CurrentSchemaVersion,
                $"Default save schema mismatch: {firstLoad.SchemaVersion}.");
            Require(firstLoad.Currency == database.StartingCurrency,
                "Default save currency did not survive a JSON round trip.");

            return gameState;
        }

        private static void VerifyDefaultState(GameDatabase database, PlayerState state)
        {
            Require(state != null, "Default player state is null.");
            Require(state.SchemaVersion == PlayerState.CurrentSchemaVersion,
                $"Default player schema must be {PlayerState.CurrentSchemaVersion}; found {state.SchemaVersion}.");
            Require(state.Currency == database.StartingCurrency,
                $"Default currency mismatch: expected {database.StartingCurrency}, found {state.Currency}.");
            Require(state.Currency >= 0, "Default currency is negative.");
            Require(database.StarterCharacterIds.Count == TeamFormationState.RequiredMemberCount,
                $"Database must define exactly {TeamFormationState.RequiredMemberCount} starter characters.");
            Require(string.Equals(database.StarterCharacterIds[0], "ur_cosmic_slime", StringComparison.Ordinal),
                "Cosmic Slime must occupy the first default formation slot.");
            Require(state.OwnedCharacters.Count >= TeamFormationState.RequiredMemberCount,
                "Default state does not own enough characters for a battle.");
            Require(state.TeamFormation != null && state.TeamFormation.IsComplete,
                "Default state does not contain a complete five-character formation.");

            var unique = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < state.TeamFormation.CharacterIds.Count; i++)
            {
                string id = state.TeamFormation.CharacterIds[i];
                Require(unique.Add(id), $"Default formation duplicates character '{id}'.");
                Require(state.HasCharacter(id), $"Default formation contains unowned character '{id}'.");
                Require(database.TryGetCharacter(id, out _),
                    $"Default formation references unknown character '{id}'.");
            }
        }

        private static void VerifySingleDraw(
            GameDatabase database,
            GachaBannerDefinition banner,
            GameStateService gameState)
        {
            int currencyBefore = gameState.Currency;
            var gacha = new LocalGachaService(
                database,
                gameState,
                new SeededRandomService(VerificationSeed));

            GachaResult result = gacha.DrawSingle(banner);
            Require(result != null && result.Success,
                $"A funded single draw failed: '{result?.ErrorMessage ?? "no result"}'.");
            Require(database.TryGetCharacter(result.CharacterId, out _),
                $"Single draw returned unknown character '{result.CharacterId}'.");
            Require(result.CurrencySpent == banner.SingleDrawCost,
                $"Single draw reported cost {result.CurrencySpent}, expected {banner.SingleDrawCost}.");
            Require(gameState.Currency == currencyBefore - banner.SingleDrawCost,
                $"Single draw currency mismatch: before={currencyBefore}, after={gameState.Currency}, cost={banner.SingleDrawCost}.");
            Require(result.RemainingCurrency == gameState.Currency,
                "Gacha result balance does not match player state.");
            Require(gameState.Currency >= 0, "Single draw produced a negative balance.");
            Require(gameState.IsOwned(result.CharacterId),
                $"Drawn character '{result.CharacterId}' was not registered as owned.");

            int postDrawCurrency = gameState.Currency;
            gameState.Reload();
            Require(gameState.Currency == postDrawCurrency,
                "Single-draw currency did not survive in-memory save reload.");
            Require(gameState.IsOwned(result.CharacterId),
                "Drawn character did not survive in-memory save reload.");

            int insufficientBalance = Math.Max(0, banner.SingleDrawCost - 1);
            var insufficientSave = new InMemorySaveService(
                () => PlayerState.CreateDefault(insufficientBalance, database.StarterCharacterIds));
            var insufficientState = new GameStateService(database, insufficientSave);
            var insufficientGacha = new LocalGachaService(
                database,
                insufficientState,
                new SeededRandomService(VerificationSeed));

            GachaResult failedResult = insufficientGacha.DrawSingle(banner);
            Require(failedResult != null && !failedResult.Success,
                "An underfunded single draw unexpectedly succeeded.");
            Require(insufficientState.Currency == insufficientBalance,
                "An underfunded draw changed the balance.");
            Require(insufficientState.Currency >= 0,
                "An underfunded draw produced a negative balance.");
        }

        private static void VerifyFormation(GameDatabase database, GameStateService gameState)
        {
            var formation = new LocalFormationService(database, gameState);
            var desiredIds = new List<string>(TeamFormationState.RequiredMemberCount);
            for (int i = 0; i < TeamFormationState.RequiredMemberCount; i++)
            {
                desiredIds.Add(database.StarterCharacterIds[i]);
            }

            Require(formation.IsValidFormation(desiredIds, out string validationReason),
                $"Expected starter formation is invalid: '{validationReason}'.");
            Require(formation.TrySetFormation(desiredIds, out string setReason),
                $"Could not save the starter formation: '{setReason}'.");
            Require(formation.HasValidFormation, "Saved formation is not considered valid.");
            Require(formation.CurrentFormation != null && formation.CurrentFormation.IsComplete,
                "Saved formation does not contain exactly five characters.");

            var duplicateIds = new List<string>(desiredIds);
            duplicateIds[duplicateIds.Count - 1] = desiredIds[0];
            Require(!formation.TrySetFormation(duplicateIds, out _),
                "Formation service accepted a duplicate character.");

            var unownedIds = new List<string>(desiredIds);
            unownedIds[unownedIds.Count - 1] = "verification_missing_character";
            Require(!formation.TrySetFormation(unownedIds, out _),
                "Formation service accepted an unowned or unknown character.");

            gameState.Reload();
            Require(gameState.State.TeamFormation.IsComplete,
                "Five-character formation did not survive save reload.");
            for (int i = 0; i < desiredIds.Count; i++)
            {
                Require(string.Equals(
                        gameState.State.TeamFormation.CharacterIds[i],
                        desiredIds[i],
                        StringComparison.Ordinal),
                    $"Formation slot {i} changed after save reload.");
            }

            gameState.Reset();
            VerifyDefaultState(database, gameState.State);
        }

        private static void VerifyBattleDeterminism(
            GameDatabase database,
            IReadOnlyList<string> playerCharacterIds)
        {
            var playerCharacters = new List<CharacterDefinition>(BattleTeam.RequiredMemberCount);
            for (int i = 0; i < BattleTeam.RequiredMemberCount; i++)
            {
                CharacterDefinition character = database.GetCharacter(playerCharacterIds[i]);
                Require(character != null,
                    $"Cannot build player battle team from character '{playerCharacterIds[i]}'.");
                playerCharacters.Add(character);
            }

            var enemyCharacters = new List<CharacterDefinition>(BattleTeam.RequiredMemberCount);
            for (int i = database.Characters.Count - 1;
                 i >= 0 && enemyCharacters.Count < BattleTeam.RequiredMemberCount;
                 i--)
            {
                CharacterDefinition character = database.Characters[i];
                Require(character != null, $"Enemy battle character slot {i} is null.");
                if (!character.IsLimited)
                {
                    enemyCharacters.Add(character);
                }
            }

            enemyCharacters.Reverse();

            var playerTeam = new BattleTeam(playerCharacters);
            var enemyTeam = new BattleTeam(enemyCharacters);
            var firstContext = new BattleContext(
                playerTeam,
                enemyTeam,
                VerificationSeed,
                BattleContext.DefaultTickDuration,
                BattleContext.DefaultMaxDuration,
                CatherineYukiBattleKit.DemoEnemyHealthMultiplier,
                CatherineYukiBattleKit.DemoEnemyAttackMultiplier);
            var secondContext = new BattleContext(
                playerTeam,
                enemyTeam,
                VerificationSeed,
                BattleContext.DefaultTickDuration,
                BattleContext.DefaultMaxDuration,
                CatherineYukiBattleKit.DemoEnemyHealthMultiplier,
                CatherineYukiBattleKit.DemoEnemyAttackMultiplier);
            BattleResult first = new BattleSimulation(
                firstContext).Run();
            BattleResult second = new BattleSimulation(
                secondContext).Run();

            Require(first != null && second != null, "Battle simulation returned a null result.");
            Require(first.Outcome != BattleOutcome.None, "Battle simulation did not reach an outcome.");
            Require(first.Outcome == second.Outcome,
                $"Same-seed battle outcome diverged: {first.Outcome} vs {second.Outcome}.");
            Require(first.ElapsedTicks == second.ElapsedTicks && first.ElapsedTime.Equals(second.ElapsedTime),
                "Same-seed battle duration diverged.");
            Require(first.Events.Count > 1, "Battle simulation emitted no meaningful events.");
            Require(first.Events.Count == second.Events.Count,
                $"Same-seed event counts diverged: {first.Events.Count} vs {second.Events.Count}.");

            for (int i = 0; i < first.Events.Count; i++)
            {
                CompareBattleEvent(first.Events[i], second.Events[i], i);
            }

            Require(first.Events[first.Events.Count - 1].Type == BattleEventType.BattleFinished,
                "Battle event sequence does not end with BattleFinished.");
            CompareUnitSnapshots(first.PlayerUnits, second.PlayerUnits, "player");
            CompareUnitSnapshots(first.EnemyUnits, second.EnemyUnits, "enemy");
            VerifyDemoEnemyRuntimeScaling(first, enemyCharacters);
            VerifyCatherineMaxedKitEvents(first);
            VerifyBattleMovementRules(first, playerCharacters, enemyCharacters);
            VerifyCatherineDeathAwakening(database, playerCharacters);
        }

        private static void CompareBattleEvent(BattleEvent left, BattleEvent right, int index)
        {
            Require(left != null && right != null, $"Battle event {index} is null.");
            bool equal = left.Sequence == right.Sequence
                         && left.Tick == right.Tick
                         && left.Time.Equals(right.Time)
                         && left.Type == right.Type
                         && string.Equals(left.ActorRuntimeId, right.ActorRuntimeId, StringComparison.Ordinal)
                         && left.ActorSide == right.ActorSide
                         && left.ActorSlot == right.ActorSlot
                         && string.Equals(left.TargetRuntimeId, right.TargetRuntimeId, StringComparison.Ordinal)
                         && left.TargetSide == right.TargetSide
                         && left.TargetSlot == right.TargetSlot
                         && left.Amount.Equals(right.Amount)
                         && left.HealthAfter.Equals(right.HealthAfter)
                         && left.MaxHealthAfter.Equals(right.MaxHealthAfter)
                         && left.EnergyAfter == right.EnergyAfter
                         && string.Equals(left.SkillId, right.SkillId, StringComparison.Ordinal)
                         && left.Outcome == right.Outcome
                         && left.ActorPositionAfter.Equals(right.ActorPositionAfter)
                         && left.TargetPositionAfter.Equals(right.TargetPositionAfter)
                         && left.Duration.Equals(right.Duration);
            Require(equal,
                $"Same-seed battle event diverged at index {index} (sequence {left.Sequence}, type {left.Type}).");
        }

        private static void VerifyDemoEnemyRuntimeScaling(
            BattleResult result,
            IReadOnlyList<CharacterDefinition> enemyCharacters)
        {
            Require(result.EnemyUnits.Count == BattleRules.TeamSize,
                "Demo enemy runtime snapshot does not contain five units.");
            for (int index = 0; index < result.EnemyUnits.Count; index++)
            {
                BattleUnitState runtimeUnit = result.EnemyUnits[index];
                CharacterDefinition definition = enemyCharacters[index];
                Require(Mathf.Approximately(
                            runtimeUnit.MaxHealth,
                            definition.MaxHealth * CatherineYukiBattleKit.DemoEnemyHealthMultiplier),
                    $"Enemy slot {index} did not receive the demo 10x HP multiplier.");
                Require(Mathf.Approximately(
                            runtimeUnit.Attack,
                            definition.Attack * CatherineYukiBattleKit.DemoEnemyAttackMultiplier),
                    $"Enemy slot {index} did not receive the demo 0.1x ATK multiplier.");
                Require(Mathf.Approximately(definition.MaxHealth * 10f, runtimeUnit.MaxHealth) &&
                        Mathf.Approximately(definition.Attack * 0.1f, runtimeUnit.Attack),
                    "Demo enemy multipliers must remain exactly 10x HP and 0.1x ATK.");
            }
        }

        private static void VerifyCatherineMaxedKitEvents(BattleResult result)
        {
            var timedSkill2Casts = new List<BattleEvent>();
            var timedSkill3Casts = new List<BattleEvent>();
            var timedSkill2Ticks = new HashSet<int>();
            bool initialMass = false;
            bool passiveMassGain = false;
            int latestMassStacks = CatherineYukiBattleKit.InitialImaginaryMassStacks;
            bool timedSkill2KnockUp = false;
            int timedSkill3Hits = 0;
            bool timedSkill3Healing = false;
            bool timedSkill3Taunt = false;
            bool timedSkill3SuperArmor = false;
            bool activeStarRageCast = false;
            bool sawRageFromAttack = false;
            bool sawRageFromDamage = false;
            bool sawFullRage = false;
            bool sawUltimateRageSpend = false;
            bool sawUltimateCast = false;
            bool ultimateCharge = false;
            bool ultimateTransform = false;
            bool ultimateScalingMatchesMass = false;
            bool ultimateCollapse = false;
            int ultimatePullCount = 0;
            int ultimateDamageEvents = 0;
            int ultimateKnockUpCount = 0;
            int ultimateChargeStacks = CatherineYukiBattleKit.InitialImaginaryMassStacks;
            int firstCatherineRageEventIndex = -1;

            for (int index = 0; index < result.Events.Count; index++)
            {
                BattleEvent battleEvent = result.Events[index];
                bool catherineActor = string.Equals(
                    battleEvent.ActorRuntimeId,
                    "P0",
                    StringComparison.Ordinal);
                if (battleEvent.Type == BattleEventType.StatusApplied && catherineActor &&
                    battleEvent.Amount >= CatherineYukiBattleKit.InitialImaginaryMassStacks &&
                    battleEvent.Amount <= CatherineYukiBattleKit.AwakenedImaginaryMassStackCap)
                {
                    int stacks = Mathf.RoundToInt(battleEvent.Amount);
                    if (stacks == CatherineYukiBattleKit.InitialImaginaryMassStacks)
                    {
                        initialMass = true;
                    }

                    passiveMassGain |= stacks > CatherineYukiBattleKit.InitialImaginaryMassStacks;
                    latestMassStacks = Math.Max(latestMassStacks, stacks);
                }

                if (battleEvent.Type == BattleEventType.SkillCastStarted && catherineActor)
                {
                    if (string.Equals(
                            battleEvent.SkillId,
                            CatherineYukiBattleKit.TimedSkill2Id,
                            StringComparison.Ordinal))
                    {
                        timedSkill2Casts.Add(battleEvent);
                        timedSkill2Ticks.Add(battleEvent.Tick);
                    }
                    else if (string.Equals(
                                 battleEvent.SkillId,
                                 CatherineYukiBattleKit.TimedSkill3Id,
                                 StringComparison.Ordinal))
                    {
                        timedSkill3Casts.Add(battleEvent);
                    }
                    else if (string.Equals(
                                 battleEvent.SkillId,
                                 CatherineYukiBattleKit.StarRagePassiveId,
                                 StringComparison.Ordinal))
                    {
                        activeStarRageCast = true;
                    }
                    else if (string.Equals(
                                 battleEvent.SkillId,
                                 CatherineYukiBattleKit.RageUltimateSkillId,
                                 StringComparison.Ordinal))
                    {
                        sawUltimateCast = true;
                    }
                }

                if (battleEvent.Type == BattleEventType.RageChanged &&
                    string.Equals(battleEvent.TargetRuntimeId, "P0", StringComparison.Ordinal))
                {
                    if (firstCatherineRageEventIndex < 0 && battleEvent.Amount > 0f)
                    {
                        firstCatherineRageEventIndex = index;
                        Require(Mathf.Approximately(battleEvent.Amount, battleEvent.RageAfter),
                            "Catherine Rage did not start from zero.");
                    }

                    sawRageFromAttack |= catherineActor &&
                                         Mathf.Approximately(
                                             battleEvent.Amount,
                                             BattleRules.RagePerBasicAttackHit);
                    sawRageFromDamage |= !catherineActor &&
                                         Mathf.Approximately(
                                             battleEvent.Amount,
                                             BattleRules.RagePerDamageReceived);
                    sawFullRage |= battleEvent.RageAfter == BattleRules.MaxRage;
                    sawUltimateRageSpend |= catherineActor &&
                                             battleEvent.RageAfter == 0 &&
                                             Mathf.Approximately(
                                                 battleEvent.Amount,
                                                 -BattleRules.MaxRage) &&
                                             string.Equals(
                                                 battleEvent.SkillId,
                                                 CatherineYukiBattleKit.RageUltimateSkillId,
                                                 StringComparison.Ordinal);
                }

                if (battleEvent.Type == BattleEventType.UnitKnockedUp && catherineActor)
                {
                    if (string.Equals(
                            battleEvent.SkillId,
                            CatherineYukiBattleKit.TimedSkill2Id,
                            StringComparison.Ordinal))
                    {
                        timedSkill2KnockUp = true;
                    }
                    else if (string.Equals(
                                 battleEvent.SkillId,
                                 CatherineYukiBattleKit.RageUltimateSkillId,
                                 StringComparison.Ordinal))
                    {
                        ultimateKnockUpCount += 1;
                    }
                }

                if (catherineActor &&
                    string.Equals(
                        battleEvent.SkillId,
                        CatherineYukiBattleKit.TimedSkill3Id,
                        StringComparison.Ordinal))
                {
                    if (battleEvent.Type == BattleEventType.DamageApplied)
                    {
                        timedSkill3Hits += 1;
                    }
                    else if (battleEvent.Type == BattleEventType.HealingApplied)
                    {
                        timedSkill3Healing = true;
                    }
                }

                timedSkill3Taunt |= battleEvent.Type == BattleEventType.DebuffApplied &&
                                    catherineActor &&
                                    string.Equals(
                                        battleEvent.SkillId,
                                        CatherineYukiBattleKit.TauntDebuffId,
                                        StringComparison.Ordinal);
                timedSkill3SuperArmor |= battleEvent.Type == BattleEventType.StatusApplied &&
                                         catherineActor &&
                                         string.Equals(
                                             battleEvent.SkillId,
                                             CatherineYukiBattleKit.SuperArmorStatusId,
                                             StringComparison.Ordinal);

                if (battleEvent.Type == BattleEventType.UltimatePhase &&
                    catherineActor)
                {
                    if (string.Equals(
                            battleEvent.SkillId,
                            CatherineYukiBattleKit.UltimateChargePhaseId,
                            StringComparison.Ordinal))
                    {
                        ultimateCharge = true;
                        ultimateChargeStacks = Mathf.RoundToInt(battleEvent.Amount);
                    }

                    if (string.Equals(
                            battleEvent.SkillId,
                            CatherineYukiBattleKit.UltimateTransformPhaseId,
                            StringComparison.Ordinal))
                    {
                        ultimateTransform = true;
                        ultimateScalingMatchesMass |= Mathf.Approximately(
                            battleEvent.Amount,
                            CatherineYukiBattleKit.GetUltimateScaling(ultimateChargeStacks, false));
                    }

                    ultimateCollapse |= string.Equals(
                        battleEvent.SkillId,
                        CatherineYukiBattleKit.UltimateCollapsePhaseId,
                        StringComparison.Ordinal);
                }

                if (catherineActor &&
                    string.Equals(
                        battleEvent.SkillId,
                        CatherineYukiBattleKit.RageUltimateSkillId,
                        StringComparison.Ordinal))
                {
                    if (battleEvent.Type == BattleEventType.UnitPulled)
                    {
                        ultimatePullCount++;
                    }
                    else if (battleEvent.Type == BattleEventType.DamageApplied)
                    {
                        ultimateDamageEvents += 1;
                    }
                }
            }

            Require(initialMass, "Catherine did not start the demo at 30 Imaginary Mass stacks.");
            Require(passiveMassGain && latestMassStacks > CatherineYukiBattleKit.InitialImaginaryMassStacks,
                "Star Rage did not passively gain Imaginary Mass from enemy active skills.");
            Require(!activeStarRageCast,
                "Star Rage must remain passive and cannot occupy an active skill cast slot.");
            int expectedSkill2Casts = ExpectedRecurringCastCount(
                result.ElapsedTime,
                BattleRules.Skill2InitialCastTime);
            int expectedSkill3Casts = ExpectedRecurringCastCount(
                result.ElapsedTime,
                BattleRules.Skill3InitialCastTime);
            Require(timedSkill2Casts.Count == expectedSkill2Casts &&
                    timedSkill3Casts.Count == expectedSkill3Casts,
                $"Catherine timed active-skill count does not match the completed battle window " +
                $"(Skill 2={timedSkill2Casts.Count}, Skill 3={timedSkill3Casts.Count}, " +
                $"expected={expectedSkill2Casts}/{expectedSkill3Casts}, " +
                $"battle={result.ElapsedTime:0.0}s/{result.Outcome}).");
            Require(timedSkill2Casts.Count > 0 && timedSkill3Casts.Count > 0,
                "Catherine battle ended before both timed active skills could be observed.");
            float tickTolerance = BattleContext.DefaultTickDuration + 0.001f;
            Require(Mathf.Abs(timedSkill2Casts[0].Time - BattleRules.Skill2InitialCastTime) <= tickTolerance,
                "Catherine Skill 2 did not first cast at 5 seconds.");
            Require(Mathf.Abs(timedSkill3Casts[0].Time - BattleRules.Skill3InitialCastTime) <= tickTolerance,
                "Catherine Skill 3 did not first cast at 10 seconds.");
            for (int index = 1; index < timedSkill2Casts.Count; index++)
            {
                Require(Mathf.Abs(
                            timedSkill2Casts[index].Time - timedSkill2Casts[index - 1].Time -
                            BattleRules.ActiveSkillCooldown) <= tickTolerance,
                    "Catherine Skill 2 did not repeat on a 10-second cooldown.");
            }

            for (int index = 0; index < timedSkill3Casts.Count; index++)
            {
                Require(!timedSkill2Ticks.Contains(timedSkill3Casts[index].Tick),
                    "Catherine Skill 2 and Skill 3 cast on the same tick.");
                if (index > 0)
                {
                    Require(Mathf.Abs(
                                timedSkill3Casts[index].Time - timedSkill3Casts[index - 1].Time -
                                BattleRules.ActiveSkillCooldown) <= tickTolerance,
                        "Catherine Skill 3 did not repeat on a 10-second cooldown.");
                }
            }

            Require(timedSkill2KnockUp,
                "Catherine timed Skill 2 emitted no Wind Wheel: Break knock-up event.");
            Require(timedSkill3Hits >= 2 && timedSkill3Healing && timedSkill3Taunt && timedSkill3SuperArmor,
                "Catherine timed Skill 3 is missing two-hit, 140% recovery, Taunt, or Super Armor semantics.");
            Require(sawRageFromAttack && sawRageFromDamage && sawFullRage,
                "Catherine did not gain Rage from both attacks and received damage up to 1000.");
            Require(sawUltimateRageSpend && sawUltimateCast,
                "Catherine did not spend 1000 Rage to cast the slot-1 ultimate.");
            Require(ultimateCharge && ultimateTransform && ultimateScalingMatchesMass && ultimateCollapse,
                "Catherine ultimate is missing charge, mass-scaled transform, or collapse phases.");
            Require(ultimatePullCount >= BattleRules.TeamSize,
                $"Catherine ultimate must pull at least five living enemies; found {ultimatePullCount}.");
            Require(ultimateDamageEvents >= CatherineYukiBattleKit.UltimateHitCount,
                "Catherine ultimate emitted fewer than four continuous damage beats.");
            Require(ultimateKnockUpCount > 0,
                "Catherine ultimate collapse emitted no knock-up event.");
        }

        private static void VerifyCatherineDeathAwakening(
            GameDatabase database,
            IReadOnlyList<CharacterDefinition> playerCharacters)
        {
            CharacterDefinition executioner = ScriptableObject.CreateInstance<CharacterDefinition>();
            try
            {
                executioner.Configure(
                    "verification_executioner",
                    "Verification Executioner",
                    CharacterRole.Assassin,
                    Rarity.R,
                    Color.red,
                    1200f,
                    50000f,
                    0f,
                    0.1f,
                    10f,
                    1f,
                    100,
                    0,
                    0,
                    database.GetSkill("pulse_strike"),
                    "Editor-only deterministic death-awakening fixture.");

                var enemies = new List<CharacterDefinition>(BattleRules.TeamSize);
                for (int index = 0; index < BattleRules.TeamSize; index++)
                {
                    enemies.Add(executioner);
                }

                BattleResult result = new BattleSimulation(
                    new BattleContext(
                        new BattleTeam(playerCharacters),
                        new BattleTeam(enemies),
                        VerificationSeed + 1,
                        BattleContext.DefaultTickDuration,
                        20f)).Run();
                int revivalCount = 0;
                int deathUltimateCount = 0;
                bool sixTimesTransform = false;
                for (int index = 0; index < result.Events.Count; index++)
                {
                    BattleEvent battleEvent = result.Events[index];
                    if (!string.Equals(battleEvent.ActorRuntimeId, "P0", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (battleEvent.Type == BattleEventType.SkillCastStarted &&
                        string.Equals(
                            battleEvent.SkillId,
                            CatherineYukiBattleKit.DeathUltimateId,
                            StringComparison.Ordinal))
                    {
                        deathUltimateCount++;
                    }
                    else if (battleEvent.Type == BattleEventType.UltimatePhase &&
                             string.Equals(
                                 battleEvent.SkillId,
                                 CatherineYukiBattleKit.UltimateTransformPhaseId,
                                 StringComparison.Ordinal) &&
                             battleEvent.Amount >= CatherineYukiBattleKit.MinimumDeathUltimateScaling)
                    {
                        sixTimesTransform = true;
                    }
                    else if (battleEvent.Type == BattleEventType.UnitRevived)
                    {
                        revivalCount++;
                        Require(Mathf.Approximately(
                                    battleEvent.HealthAfter,
                                    result.PlayerUnits[0].MaxHealth *
                                    CatherineYukiBattleKit.RevivalHealthRatio),
                            "Catherine death awakening did not restore 99% HP.");
                    }
                }

                Require(deathUltimateCount == 1 && revivalCount == 1 && sixTimesTransform,
                    "Catherine death awakening must trigger one minimum-6x ultimate and one 99% revival.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(executioner);
            }
        }

        private static void CompareUnitSnapshots(
            IReadOnlyList<BattleUnitState> left,
            IReadOnlyList<BattleUnitState> right,
            string sideLabel)
        {
            Require(left.Count == right.Count, $"Same-seed {sideLabel} snapshot counts diverged.");
            for (int i = 0; i < left.Count; i++)
            {
                Require(
                    string.Equals(left[i].CharacterId, right[i].CharacterId, StringComparison.Ordinal)
                    && left[i].MaxHealth.Equals(right[i].MaxHealth)
                    && left[i].CurrentHealth.Equals(right[i].CurrentHealth)
                    && left[i].CurrentEnergy == right[i].CurrentEnergy
                    && left[i].IsAlive == right[i].IsAlive
                    && left[i].AttackRange.Equals(right[i].AttackRange)
                    && left[i].MoveSpeed.Equals(right[i].MoveSpeed)
                    && left[i].CurrentPosition.Equals(right[i].CurrentPosition)
                    && string.Equals(
                        left[i].LockedTargetRuntimeId,
                        right[i].LockedTargetRuntimeId,
                        StringComparison.Ordinal),
                    $"Same-seed {sideLabel} unit snapshot diverged at slot {i}.");
            }
        }

        private static void VerifyBattleMovementRules(
            BattleResult result,
            IReadOnlyList<CharacterDefinition> playerCharacters,
            IReadOnlyList<CharacterDefinition> enemyCharacters)
        {
            var definitions = new Dictionary<string, CharacterDefinition>(StringComparer.Ordinal);
            var positions = new Dictionary<string, Vector3>(StringComparer.Ordinal);
            for (int slot = 0; slot < BattleTeam.RequiredMemberCount; slot++)
            {
                string playerId = $"P{slot}";
                string enemyId = $"E{slot}";
                definitions[playerId] = playerCharacters[slot];
                definitions[enemyId] = enemyCharacters[slot];
                positions[playerId] = BattleRules.GetSlotPosition(BattleTeamSide.Player, slot);
                positions[enemyId] = BattleRules.GetSlotPosition(BattleTeamSide.Enemy, slot);
            }

            var lockedTargets = new Dictionary<string, string>(StringComparer.Ordinal);
            var tauntExpiresAtTick = new Dictionary<string, int>(StringComparer.Ordinal);
            var defeatedUnits = new HashSet<string>(StringComparer.Ordinal);
            var playerTankTargets = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, Vector3> tickStartPositions = null;
            int activeTick = -1;
            int movementEventCount = 0;
            int playerTankMovementCount = 0;
            int playerTankActionCount = 0;
            bool verifiedPlayerTankRangeBoundary = false;
            bool verifiedTauntExpiryRetarget = false;

            for (int index = 0; index < result.Events.Count; index++)
            {
                BattleEvent battleEvent = result.Events[index];
                if (battleEvent.Tick != activeTick)
                {
                    activeTick = battleEvent.Tick;
                    tickStartPositions = new Dictionary<string, Vector3>(positions, StringComparer.Ordinal);
                }

                if (battleEvent.Type == BattleEventType.UnitDefeated)
                {
                    defeatedUnits.Add(battleEvent.TargetRuntimeId);
                    continue;
                }

                if (battleEvent.Type == BattleEventType.UnitRevived)
                {
                    defeatedUnits.Remove(battleEvent.TargetRuntimeId);
                    continue;
                }

                if (battleEvent.Type == BattleEventType.UnitPulled ||
                    battleEvent.Type == BattleEventType.UnitKnockedUp)
                {
                    if (!string.IsNullOrEmpty(battleEvent.TargetRuntimeId))
                    {
                        positions[battleEvent.TargetRuntimeId] = battleEvent.TargetPositionAfter;
                        tickStartPositions[battleEvent.TargetRuntimeId] = battleEvent.TargetPositionAfter;
                    }

                    continue;
                }

                if (battleEvent.Type == BattleEventType.DebuffApplied &&
                    string.Equals(
                        battleEvent.SkillId,
                        CatherineYukiBattleKit.TauntDebuffId,
                        StringComparison.Ordinal) &&
                    !string.IsNullOrEmpty(battleEvent.TargetRuntimeId) &&
                    !string.IsNullOrEmpty(battleEvent.ActorRuntimeId))
                {
                    lockedTargets[battleEvent.TargetRuntimeId] = battleEvent.ActorRuntimeId;
                    int tauntDurationTicks = Math.Max(
                        1,
                        Mathf.CeilToInt(battleEvent.Duration / BattleContext.DefaultTickDuration));
                    tauntExpiresAtTick[battleEvent.TargetRuntimeId] =
                        battleEvent.Tick + tauntDurationTicks;
                    continue;
                }

                if (battleEvent.Type == BattleEventType.UnitMoved)
                {
                    movementEventCount++;
                    Require(definitions.TryGetValue(battleEvent.ActorRuntimeId, out CharacterDefinition actorDefinition),
                        $"Movement event {index} has unknown actor '{battleEvent.ActorRuntimeId}'.");
                    Vector3 actorPositionBefore = default;
                    Vector3 targetPositionBefore = default;
                    Require(tickStartPositions.TryGetValue(battleEvent.ActorRuntimeId, out actorPositionBefore),
                        $"Movement event {index} has an unknown actor position.");
                    Require(tickStartPositions.TryGetValue(battleEvent.TargetRuntimeId, out targetPositionBefore),
                        $"Movement event {index} has an unknown target position.");
                    Require(!BattleRules.IsWithinAttackRange(
                            actorPositionBefore,
                            targetPositionBefore,
                            actorDefinition.AttackRange),
                        $"Actor '{battleEvent.ActorRuntimeId}' moved despite already being in attack range.");
                    Require(battleEvent.Duration.Equals(BattleContext.DefaultTickDuration),
                        $"Movement event {index} duration must equal one fixed tick.");

                    float stepDistance = Vector3.Distance(actorPositionBefore, battleEvent.ActorPositionAfter);
                    float maximumStep = actorDefinition.MoveSpeed * battleEvent.Duration + 0.001f;
                    Require(stepDistance > 0f && stepDistance <= maximumStep,
                        $"Actor '{battleEvent.ActorRuntimeId}' movement step {stepDistance} exceeds {maximumStep}.");
                    Require(Vector3.Distance(battleEvent.ActorPositionAfter, targetPositionBefore) <=
                            Vector3.Distance(actorPositionBefore, targetPositionBefore) + 0.001f,
                        $"Actor '{battleEvent.ActorRuntimeId}' moved away from its locked target.");

                    verifiedTauntExpiryRetarget |= VerifyTargetLock(
                        battleEvent.ActorRuntimeId,
                        battleEvent.TargetRuntimeId,
                        lockedTargets,
                        tauntExpiresAtTick,
                        defeatedUnits,
                        battleEvent.Tick,
                        index);
                    positions[battleEvent.ActorRuntimeId] = battleEvent.ActorPositionAfter;
                    if (string.Equals(battleEvent.ActorRuntimeId, "P0", StringComparison.Ordinal))
                    {
                        playerTankMovementCount++;
                        playerTankTargets.Add(battleEvent.TargetRuntimeId);
                    }

                    continue;
                }

                if (battleEvent.Type == BattleEventType.BasicAttackStarted ||
                    battleEvent.Type == BattleEventType.SkillCastStarted)
                {
                    Require(definitions.TryGetValue(battleEvent.ActorRuntimeId, out CharacterDefinition actorDefinition),
                        $"Action event {index} has unknown actor '{battleEvent.ActorRuntimeId}'.");
                    SkillDefinition actionSkill = battleEvent.Type == BattleEventType.SkillCastStarted
                        ? ResolveActiveSkill(actorDefinition, battleEvent.SkillId)
                        : null;
                    bool damagingAction = battleEvent.Type == BattleEventType.BasicAttackStarted ||
                                          (actionSkill != null && actionSkill.Category == SkillCategory.Damage);
                    if (!damagingAction)
                    {
                        continue;
                    }

                    Require(!defeatedUnits.Contains(battleEvent.TargetRuntimeId),
                        $"Actor '{battleEvent.ActorRuntimeId}' targeted defeated unit '{battleEvent.TargetRuntimeId}'.");
                    verifiedTauntExpiryRetarget |= VerifyTargetLock(
                        battleEvent.ActorRuntimeId,
                        battleEvent.TargetRuntimeId,
                        lockedTargets,
                        tauntExpiresAtTick,
                        defeatedUnits,
                        battleEvent.Tick,
                        index);
                    Vector3 actorPosition = default;
                    Vector3 targetPosition = default;
                    Require(positions.TryGetValue(battleEvent.ActorRuntimeId, out actorPosition),
                        $"Action event {index} has an unknown actor position.");
                    Require(positions.TryGetValue(battleEvent.TargetRuntimeId, out targetPosition),
                        $"Action event {index} has an unknown target position.");
                    Require(BattleRules.IsWithinAttackRange(
                            actorPosition,
                            targetPosition,
                            actorDefinition.AttackRange),
                        $"Actor '{battleEvent.ActorRuntimeId}' started a damage action outside attack range.");

                    if (!verifiedPlayerTankRangeBoundary &&
                        battleEvent.Type == BattleEventType.BasicAttackStarted &&
                        string.Equals(battleEvent.ActorRuntimeId, "P0", StringComparison.Ordinal))
                    {
                        float distanceAtFirstAttack = Vector3.Distance(actorPosition, targetPosition);
                        Require(Mathf.Abs(distanceAtFirstAttack - actorDefinition.AttackRange) <= 0.02f,
                            $"Player tank first attacked at distance {distanceAtFirstAttack:0.###}; " +
                            $"expected its maximum range {actorDefinition.AttackRange:0.###}.");
                        verifiedPlayerTankRangeBoundary = true;
                    }

                    if (battleEvent.Type == BattleEventType.SkillCastStarted &&
                        actionSkill != null &&
                        actionSkill.TargetMode == SkillTargetMode.SingleEnemy)
                    {
                        Require(string.Equals(
                                lockedTargets[battleEvent.ActorRuntimeId],
                                battleEvent.TargetRuntimeId,
                                StringComparison.Ordinal),
                            $"Single-target skill from '{battleEvent.ActorRuntimeId}' ignored its locked target.");
                    }

                    if (string.Equals(battleEvent.ActorRuntimeId, "P0", StringComparison.Ordinal))
                    {
                        playerTankTargets.Add(battleEvent.TargetRuntimeId);
                        playerTankActionCount++;
                    }
                }
            }

            Require(movementEventCount > 0, "Battle emitted no fixed-tick movement events.");
            Require(playerTankMovementCount > 0, "Player slot 0 tank emitted no movement events.");
            Require(playerTankActionCount > 0, "Player slot 0 tank never acted after entering range.");
            Require(verifiedPlayerTankRangeBoundary,
                "Player slot 0 tank never performed a basic attack at its maximum range boundary.");
            Require(verifiedTauntExpiryRetarget,
                "No unit released Catherine's temporary Taunt lock after its configured duration.");
            Require(playerTankTargets.Count >= 2,
                "Player slot 0 tank never retargeted after its first locked target was defeated.");
            Require(Vector3.Distance(
                    result.PlayerUnits[0].CurrentPosition,
                    BattleRules.GetSlotPosition(BattleTeamSide.Player, 0)) > 0.5f,
                "Player slot 0 tank returned to its formation spawn instead of holding the frontline.");
        }

        private static SkillDefinition ResolveActiveSkill(CharacterDefinition definition, string skillId)
        {
            if (definition == null || string.IsNullOrEmpty(skillId))
            {
                return null;
            }

            SkillDefinition[] activeSkills =
            {
                definition.UltimateSkill,
                definition.Skill2,
                definition.Skill3
            };
            for (int index = 0; index < activeSkills.Length; index++)
            {
                SkillDefinition skill = activeSkills[index];
                if (skill != null && string.Equals(skill.Id, skillId, StringComparison.Ordinal))
                {
                    return skill;
                }
            }

            return null;
        }

        private static int ExpectedRecurringCastCount(float elapsedTime, float initialCastTime)
        {
            if (elapsedTime + BattleRules.RangeEpsilon < initialCastTime)
            {
                return 0;
            }

            return 1 + Mathf.FloorToInt(
                (elapsedTime - initialCastTime + BattleRules.RangeEpsilon) /
                BattleRules.ActiveSkillCooldown);
        }

        private static bool VerifyTargetLock(
            string actorRuntimeId,
            string targetRuntimeId,
            IDictionary<string, string> lockedTargets,
            IDictionary<string, int> tauntExpiresAtTick,
            ISet<string> defeatedUnits,
            int currentTick,
            int eventIndex)
        {
            Require(!string.IsNullOrEmpty(actorRuntimeId) && !string.IsNullOrEmpty(targetRuntimeId),
                $"Targeted event {eventIndex} has an empty actor or target id.");
            bool releasedExpiredTaunt = false;
            if (lockedTargets.TryGetValue(actorRuntimeId, out string previousTarget) &&
                !string.Equals(previousTarget, targetRuntimeId, StringComparison.Ordinal))
            {
                bool previousTargetDefeated = defeatedUnits.Contains(previousTarget);
                bool tauntExpired = tauntExpiresAtTick.TryGetValue(actorRuntimeId, out int expiresAtTick) &&
                                    currentTick > expiresAtTick;
                Require(previousTargetDefeated || tauntExpired,
                    $"Event {eventIndex} at tick {currentTick}: actor '{actorRuntimeId}' switched " +
                    $"from living target '{previousTarget}' to '{targetRuntimeId}'.");
                tauntExpiresAtTick.Remove(actorRuntimeId);
                releasedExpiredTaunt = tauntExpired && !previousTargetDefeated;
            }

            lockedTargets[actorRuntimeId] = targetRuntimeId;
            return releasedExpiredTaunt;
        }

        private static void VerifySceneAndBuildSettings()
        {
            SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Require(scene != null, $"Demo scene is missing at '{ScenePath}'.");

            bool foundEnabledScene = false;
            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            for (int i = 0; i < buildScenes.Length; i++)
            {
                EditorBuildSettingsScene buildScene = buildScenes[i];
                if (buildScene != null
                    && buildScene.enabled
                    && string.Equals(buildScene.path, ScenePath, StringComparison.Ordinal))
                {
                    foundEnabledScene = true;
                    break;
                }
            }

            Require(foundEnabledScene,
                $"Demo scene '{ScenePath}' is not enabled in Build Settings.");
        }

        private static void VerifyRulesContract()
        {
            Require(Enum.GetValues(typeof(Rarity)).Length == 5,
                "Rarity must contain exactly the five supported tiers.");
            Require((int)Rarity.R == 0 &&
                    (int)Rarity.SR == 1 &&
                    (int)Rarity.SSR == 2 &&
                    (int)Rarity.SP == 3 &&
                    (int)Rarity.UR == 4,
                "Rarity order must be R, SR, SSR, SP, UR with stable serialized values 0 through 4.");
            Require(BattleRules.TeamSize == 5,
                $"BattleRules.TeamSize must be 5; found {BattleRules.TeamSize}.");
            Require(TeamFormationState.RequiredMemberCount == BattleRules.TeamSize,
                "Formation and battle team-size rules have diverged.");
            Require(Mathf.Approximately(BattleRules.GetDefaultAttackRange(CharacterRole.Tank), 1f) &&
                    Mathf.Approximately(BattleRules.GetDefaultAttackRange(CharacterRole.Assassin), 1f) &&
                    Mathf.Approximately(BattleRules.GetDefaultAttackRange(CharacterRole.Support), 5f) &&
                    Mathf.Approximately(BattleRules.GetDefaultAttackRange(CharacterRole.Ranged), 5f) &&
                    Mathf.Approximately(BattleRules.GetDefaultAttackRange(CharacterRole.Mage), 5f),
                "Tank/Assassin range must be 1 and every other role range must be 5.");
            Require(BattleRules.MaxRage == 1000 &&
                    BattleRules.RagePerBasicAttackHit == 100 &&
                    BattleRules.RagePerDamageReceived == 50,
                "Rage must use the authored 1000 max, +100 hit, and +50 damaged contract.");
            Require(Mathf.Approximately(BattleRules.Skill2InitialCastTime, 5f) &&
                    Mathf.Approximately(BattleRules.Skill3InitialCastTime, 10f) &&
                    Mathf.Approximately(BattleRules.ActiveSkillCooldown, 10f),
                "Skill 2/3 must use the 5s/10s offset and 10s individual cooldown.");
            Require(IsFinitePositive(BattleRules.TankMoveSpeed) &&
                    IsFinitePositive(BattleRules.AssassinMoveSpeed) &&
                    IsFinitePositive(BattleRules.SupportMoveSpeed) &&
                    IsFinitePositive(BattleRules.RangedMoveSpeed) &&
                    IsFinitePositive(BattleRules.MageMoveSpeed),
                "Default movement speeds must be finite and positive.");
            Require((int)BattleEventType.RageChanged == 5 &&
                    (int)BattleEventType.BattleFinished == 7 &&
                    (int)BattleEventType.UnitMoved == 8 &&
                    (int)BattleEventType.UnitPulled == 9 &&
                    (int)BattleEventType.UnitKnockedUp == 10 &&
                    (int)BattleEventType.DebuffApplied == 11 &&
                    (int)BattleEventType.UltimatePhase == 12 &&
                    (int)BattleEventType.UnitRevived == 13 &&
                    (int)BattleEventType.StatusApplied == 14,
                "Catherine events must be appended without changing existing serialized values.");
        }

        private static void RequireCharacterRarity(
            GameDatabase database,
            string characterId,
            Rarity expectedRarity)
        {
            CharacterDefinition character = database.GetCharacter(characterId);
            Require(character != null, $"Required character '{characterId}' is missing.");
            Require(character.Rarity == expectedRarity,
                $"Character '{characterId}' must use {expectedRarity}; found {character.Rarity}.");
            Require(!character.IsLimited,
                $"Standard character '{characterId}' must not be marked as limited.");
        }

        private static void RequireCharacterCombatProfile(
            GameDatabase database,
            string characterId,
            float expectedAttackRange,
            float expectedMoveSpeed)
        {
            CharacterDefinition character = database.GetCharacter(characterId);
            Require(character != null, $"Required character '{characterId}' is missing.");
            Require(Mathf.Approximately(character.AttackRange, expectedAttackRange) &&
                    Mathf.Approximately(character.MoveSpeed, expectedMoveSpeed),
                $"Character '{characterId}' combat profile mismatch: " +
                $"range={character.AttackRange}, move={character.MoveSpeed}.");
        }

        private static bool ContainsReference<T>(IReadOnlyList<T> items, T target)
            where T : UnityEngine.Object
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == target)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsFinitePositive(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFiniteNonNegative(float value)
        {
            return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException($"[GenericGachaRPG][P0_VERIFY_FAIL] {message}");
            }
        }

        private sealed class InMemorySaveService : ISaveService
        {
            private readonly Func<PlayerState> defaultFactory;
            private string json;

            public bool HasSave => !string.IsNullOrEmpty(json);

            public InMemorySaveService(Func<PlayerState> playerStateFactory)
            {
                defaultFactory = playerStateFactory
                                 ?? throw new ArgumentNullException(nameof(playerStateFactory));
            }

            public PlayerState Load()
            {
                if (!HasSave)
                {
                    PlayerState initialState = defaultFactory();
                    if (initialState == null)
                    {
                        throw new InvalidOperationException(
                            "[GenericGachaRPG][P0_VERIFY_FAIL] In-memory default-state factory returned null.");
                    }

                    Save(initialState);
                }

                PlayerState loaded = JsonUtility.FromJson<PlayerState>(json);
                if (loaded == null)
                {
                    throw new InvalidOperationException(
                        "[GenericGachaRPG][P0_VERIFY_FAIL] In-memory JSON save could not be deserialized.");
                }

                return loaded;
            }

            public void Save(PlayerState state)
            {
                if (state == null)
                {
                    throw new ArgumentNullException(nameof(state));
                }

                json = JsonUtility.ToJson(state);
                if (string.IsNullOrEmpty(json))
                {
                    throw new InvalidOperationException(
                        "[GenericGachaRPG][P0_VERIFY_FAIL] In-memory JSON save was empty.");
                }
            }

            public PlayerState Reset()
            {
                json = null;
                return Load();
            }
        }
    }
}
