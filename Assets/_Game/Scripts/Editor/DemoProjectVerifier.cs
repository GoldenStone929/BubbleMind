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
        private const int VerificationSeed = 731925;

        [MenuItem(MenuPath, priority = 12)]
        public static void VerifyFromMenu()
        {
            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            Verify(database);
        }

        public static void Verify(GameDatabase database)
        {
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
            Require(database.Skills.Count == 3,
                $"Database must contain exactly 3 skills; found {database.Skills.Count}.");

            var characterIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < database.Characters.Count; i++)
            {
                CharacterDefinition character = database.Characters[i];
                Require(character != null, $"Character slot {i} is null.");
                Require(!string.IsNullOrWhiteSpace(character.Id), $"Character slot {i} has an empty id.");
                Require(characterIds.Add(character.Id), $"Character id '{character.Id}' is duplicated.");
                Require(character.Skill != null, $"Character '{character.Id}' has no skill.");
                Require(ContainsReference(database.Skills, character.Skill),
                    $"Character '{character.Id}' references a skill outside the database.");
                Require(IsFinitePositive(character.MaxHealth),
                    $"Character '{character.Id}' has invalid MaxHealth {character.MaxHealth}.");
                Require(IsFiniteNonNegative(character.Attack) && IsFiniteNonNegative(character.Defense),
                    $"Character '{character.Id}' has invalid combat stats.");
                Require(IsFinitePositive(character.AttackInterval),
                    $"Character '{character.Id}' has invalid AttackInterval {character.AttackInterval}.");
            }

            var skillIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < database.Skills.Count; i++)
            {
                SkillDefinition skill = database.Skills[i];
                Require(skill != null, $"Skill slot {i} is null.");
                Require(!string.IsNullOrWhiteSpace(skill.Id), $"Skill slot {i} has an empty id.");
                Require(skillIds.Add(skill.Id), $"Skill id '{skill.Id}' is duplicated.");
                Require(skill.TargetCount > 0, $"Skill '{skill.Id}' has no valid targets.");
                Require(skill.EnergyCost >= 0, $"Skill '{skill.Id}' has a negative energy cost.");
                Require(IsFiniteNonNegative(skill.HitTiming), $"Skill '{skill.Id}' has invalid hit timing.");
            }

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
                Require(database.TryGetCharacter(entry.CharacterId, out _),
                    $"Default banner entry {i} references unknown character '{entry.CharacterId}'.");
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
            Require(cosmicSlime.Rarity == Rarity.UltraRare, "Cosmic Slime must use UltraRare rarity.");
            Require(cosmicSlime.CharacterPrefab != null, "Cosmic Slime prefab is missing.");
            Require(cosmicSlime.CharacterPrefab.GetComponent<CharacterView>() != null,
                "Cosmic Slime prefab must have a root CharacterView.");

            Material backdropMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                AbyssalObservatoryAssetBuilder.BackdropMaterialPath);
            Require(backdropMaterial != null, "Abyssal Observatory backdrop material is missing.");
            Require(backdropMaterial.shader != null, "Abyssal Observatory backdrop material has no shader.");

            return banner;
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
            Require(state.OwnedCharacters.Count >= TeamFormationState.RequiredMemberCount,
                "Default state does not own enough characters for a battle.");
            Require(state.TeamFormation != null && state.TeamFormation.IsComplete,
                "Default state does not contain a complete three-character formation.");

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
                "Saved formation does not contain exactly three characters.");

            var duplicateIds = new List<string>
            {
                desiredIds[0],
                desiredIds[0],
                desiredIds[1]
            };
            Require(!formation.TrySetFormation(duplicateIds, out _),
                "Formation service accepted a duplicate character.");

            var unownedIds = new List<string>
            {
                desiredIds[0],
                desiredIds[1],
                "verification_missing_character"
            };
            Require(!formation.TrySetFormation(unownedIds, out _),
                "Formation service accepted an unowned or unknown character.");

            gameState.Reload();
            Require(gameState.State.TeamFormation.IsComplete,
                "Three-character formation did not survive save reload.");
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
            for (int i = database.Characters.Count - BattleTeam.RequiredMemberCount;
                 i < database.Characters.Count;
                 i++)
            {
                CharacterDefinition character = database.Characters[i];
                Require(character != null, $"Enemy battle character slot {i} is null.");
                enemyCharacters.Add(character);
            }

            var playerTeam = new BattleTeam(playerCharacters);
            var enemyTeam = new BattleTeam(enemyCharacters);
            BattleResult first = new BattleSimulation(
                new BattleContext(playerTeam, enemyTeam, VerificationSeed)).Run();
            BattleResult second = new BattleSimulation(
                new BattleContext(playerTeam, enemyTeam, VerificationSeed)).Run();

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
                         && left.EnergyAfter == right.EnergyAfter
                         && string.Equals(left.SkillId, right.SkillId, StringComparison.Ordinal)
                         && left.Outcome == right.Outcome;
            Require(equal,
                $"Same-seed battle event diverged at index {index} (sequence {left.Sequence}, type {left.Type}).");
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
                    && left[i].CurrentHealth.Equals(right[i].CurrentHealth)
                    && left[i].CurrentEnergy == right[i].CurrentEnergy
                    && left[i].IsAlive == right[i].IsAlive,
                    $"Same-seed {sideLabel} unit snapshot diverged at slot {i}.");
            }
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
