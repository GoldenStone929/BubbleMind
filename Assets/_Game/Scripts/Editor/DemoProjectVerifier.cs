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
            Require(database.Skills.Count == 3,
                $"Database must contain exactly 3 skills; found {database.Skills.Count}.");

            var characterIds = new HashSet<string>(StringComparer.Ordinal);
            int limitedCharacterCount = 0;
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
                Require(IsFinitePositive(character.AttackRange),
                    $"Character '{character.Id}' has invalid AttackRange {character.AttackRange}.");
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
                Require(skill.EnergyCost >= 0, $"Skill '{skill.Id}' has a negative energy cost.");
                Require(IsFiniteNonNegative(skill.HitTiming), $"Skill '{skill.Id}' has invalid hit timing.");
            }

            SkillDefinition spectrumNova = database.GetSkill("spectrum_nova");
            Require(spectrumNova != null &&
                    spectrumNova.Category == SkillCategory.Damage &&
                    spectrumNova.TargetMode == SkillTargetMode.AllEnemies &&
                    spectrumNova.TargetCount == BattleRules.TeamSize,
                "Spectrum Nova must target every opponent in the five-unit battle.");

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
            Require(cosmicSlime.Role == CharacterRole.Guardian, "Cosmic Slime must use the tank role.");
            Require(cosmicSlime.IsLimited, "Cosmic Slime must be marked as limited.");
            Require(cosmicSlime.AttackRange <= BattleRules.GuardianAttackRange + BattleRules.RangeEpsilon,
                $"Cosmic Slime must use melee tank range; found {cosmicSlime.AttackRange}.");
            Require(cosmicSlime.CharacterPrefab != null, "Cosmic Slime prefab is missing.");
            Require(cosmicSlime.CharacterPrefab.GetComponent<CharacterView>() != null,
                "Cosmic Slime prefab must have a root CharacterView.");

            RequireCharacterRarity(database, "azure_vanguard", Rarity.R);
            RequireCharacterRarity(database, "ember_striker", Rarity.R);
            RequireCharacterRarity(database, "verdant_medic", Rarity.SR);
            RequireCharacterRarity(database, "violet_arcanist", Rarity.SR);
            RequireCharacterRarity(database, "gold_ranger", Rarity.SSR);
            RequireCharacterRarity(database, "cyan_warden", Rarity.SSR);
            RequireCharacterCombatProfile(database, "azure_vanguard", 1.45f, 3.2f);
            RequireCharacterCombatProfile(database, "ember_striker", 1.55f, 4.2f);
            RequireCharacterCombatProfile(database, "verdant_medic", 4.4f, 3.0f);
            RequireCharacterCombatProfile(database, "ur_cosmic_slime", 1.45f, 3.3f);
            RequireCharacterCombatProfile(database, "violet_arcanist", 3.8f, 3.4f);
            RequireCharacterCombatProfile(database, "gold_ranger", 4.6f, 3.8f);
            RequireCharacterCombatProfile(database, "cyan_warden", 1.55f, 3.15f);

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
            VerifyBattleMovementRules(first, playerCharacters, enemyCharacters);
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
                         && left.Outcome == right.Outcome
                         && left.ActorPositionAfter.Equals(right.ActorPositionAfter)
                         && left.Duration.Equals(right.Duration);
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
            var defeatedUnits = new HashSet<string>(StringComparer.Ordinal);
            var playerTankTargets = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, Vector3> tickStartPositions = null;
            int activeTick = -1;
            int movementEventCount = 0;
            int playerTankMovementCount = 0;
            int playerTankAttackCount = 0;

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

                    VerifyTargetLock(
                        battleEvent.ActorRuntimeId,
                        battleEvent.TargetRuntimeId,
                        lockedTargets,
                        defeatedUnits,
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
                    bool damagingAction = battleEvent.Type == BattleEventType.BasicAttackStarted ||
                                          (actorDefinition.Skill != null &&
                                           actorDefinition.Skill.Category == SkillCategory.Damage);
                    if (!damagingAction)
                    {
                        continue;
                    }

                    Require(!defeatedUnits.Contains(battleEvent.TargetRuntimeId),
                        $"Actor '{battleEvent.ActorRuntimeId}' targeted defeated unit '{battleEvent.TargetRuntimeId}'.");
                    VerifyTargetLock(
                        battleEvent.ActorRuntimeId,
                        battleEvent.TargetRuntimeId,
                        lockedTargets,
                        defeatedUnits,
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

                    if (battleEvent.Type == BattleEventType.SkillCastStarted &&
                        actorDefinition.Skill.TargetMode == SkillTargetMode.SingleEnemy)
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
                        if (battleEvent.Type == BattleEventType.BasicAttackStarted)
                        {
                            playerTankAttackCount++;
                        }
                    }
                }
            }

            Require(movementEventCount > 0, "Battle emitted no fixed-tick movement events.");
            Require(playerTankMovementCount > 0, "Player slot 0 tank emitted no movement events.");
            Require(playerTankAttackCount > 0, "Player slot 0 tank never attacked after entering range.");
            Require(playerTankTargets.Count >= 2,
                "Player slot 0 tank never retargeted after its first locked target was defeated.");
            Require(Vector3.Distance(
                    result.PlayerUnits[0].CurrentPosition,
                    BattleRules.GetSlotPosition(BattleTeamSide.Player, 0)) > 0.5f,
                "Player slot 0 tank returned to its formation spawn instead of holding the frontline.");
        }

        private static void VerifyTargetLock(
            string actorRuntimeId,
            string targetRuntimeId,
            IDictionary<string, string> lockedTargets,
            ISet<string> defeatedUnits,
            int eventIndex)
        {
            Require(!string.IsNullOrEmpty(actorRuntimeId) && !string.IsNullOrEmpty(targetRuntimeId),
                $"Targeted event {eventIndex} has an empty actor or target id.");
            if (lockedTargets.TryGetValue(actorRuntimeId, out string previousTarget) &&
                !string.Equals(previousTarget, targetRuntimeId, StringComparison.Ordinal))
            {
                Require(defeatedUnits.Contains(previousTarget),
                    $"Actor '{actorRuntimeId}' switched from living target '{previousTarget}' to '{targetRuntimeId}'.");
            }

            lockedTargets[actorRuntimeId] = targetRuntimeId;
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
            Require(BattleRules.GuardianAttackRange < BattleRules.StrikerAttackRange &&
                    BattleRules.StrikerAttackRange < BattleRules.SupportAttackRange,
                "Default attack ranges must progress from Guardian to Striker to Support.");
            Require(IsFinitePositive(BattleRules.GuardianMoveSpeed) &&
                    IsFinitePositive(BattleRules.StrikerMoveSpeed) &&
                    IsFinitePositive(BattleRules.SupportMoveSpeed),
                "Default movement speeds must be finite and positive.");
            Require((int)BattleEventType.UnitMoved == 8 && (int)BattleEventType.BattleFinished == 7,
                "UnitMoved must be appended without changing serialized battle event values.");
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
