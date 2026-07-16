using System;
using System.Collections.Generic;

namespace GenericGachaRPG
{
    public sealed class GameStateService
    {
        private readonly ISaveService saveService;

        public GameDatabase Database { get; }
        public PlayerState State { get; private set; }
        public PlayerState CurrentState => State;
        public int Currency => State == null ? 0 : State.Currency;

        public event Action<PlayerState> StateChanged;
        public event Action<int> CurrencyChanged;
        public event Action<IReadOnlyList<OwnedCharacterState>> CollectionChanged;
        public event Action<TeamFormationState> FormationChanged;
        public event Action<PlayerState> ProgressChanged;
        public event Action<IReadOnlyList<InventoryItemState>> InventoryChanged;
        public event Action<PlayerSettingsState> SettingsChanged;

        public GameStateService(GameDatabase database)
            : this(database, CreateSaveService(database))
        {
        }

        public GameStateService(GameDatabase database, ISaveService playerSaveService)
        {
            Database = database;
            saveService = playerSaveService ?? throw new ArgumentNullException(nameof(playerSaveService));
            State = saveService.Load() ?? CreateDefaultState(database);
            if (!State.Normalize())
            {
                State = saveService.Reset();
            }
        }

        public void Reload()
        {
            State = saveService.Load() ?? CreateDefaultState(Database);
            RaiseAllChanged();
        }

        public void Save()
        {
            saveService.Save(State);
            StateChanged?.Invoke(State);
        }

        public void Reset()
        {
            State = saveService.Reset() ?? CreateDefaultState(Database);
            RaiseAllChanged();
        }

        public bool IsOwned(string characterId)
        {
            return State != null && State.HasCharacter(characterId);
        }

        public List<CharacterDefinition> GetOwnedCharacterDefinitions()
        {
            var result = new List<CharacterDefinition>();
            if (State == null || Database == null)
            {
                return result;
            }

            for (int i = 0; i < State.OwnedCharacters.Count; i++)
            {
                OwnedCharacterState owned = State.OwnedCharacters[i];
                if (owned != null && Database.TryGetCharacter(owned.CharacterId, out CharacterDefinition character))
                {
                    result.Add(character);
                }
            }

            return result;
        }

        internal bool TryCommitGachaDraw(
            CharacterDefinition character,
            int cost,
            out bool isNewCharacter,
            out string errorMessage)
        {
            isNewCharacter = false;
            errorMessage = string.Empty;

            if (character == null)
            {
                errorMessage = "The selected character is invalid.";
                return false;
            }

            if (Database != null && !Database.TryGetCharacter(character.Id, out _))
            {
                errorMessage = "The selected character is not in the game database.";
                return false;
            }

            if (cost < 0)
            {
                errorMessage = "The draw cost is invalid.";
                return false;
            }

            if (State == null || !State.TrySpendCurrency(cost))
            {
                errorMessage = "Not enough currency.";
                return false;
            }

            isNewCharacter = State.RegisterCharacterCopy(character.Id);
            State.RecordGachaDraw(!isNewCharacter);
            saveService.Save(State);
            CurrencyChanged?.Invoke(State.Currency);
            CollectionChanged?.Invoke(State.OwnedCharacters);
            InventoryChanged?.Invoke(State.InventoryItems);
            ProgressChanged?.Invoke(State);
            StateChanged?.Invoke(State);
            return true;
        }

        internal void CommitFormation(IReadOnlyList<string> characterIds)
        {
            if (State == null)
            {
                throw new InvalidOperationException("Player state has not been loaded.");
            }

            State.ReplaceFormation(characterIds);
            saveService.Save(State);
            FormationChanged?.Invoke(State.TeamFormation);
            StateChanged?.Invoke(State);
        }

        public bool CanEnterStage(StageDefinition stage, out string reason)
        {
            if (stage == null)
            {
                reason = "No stage is selected.";
                return false;
            }

            if (State == null)
            {
                reason = "Player state is unavailable.";
                return false;
            }

            if (Database != null && !Database.IsStageUnlocked(stage, State))
            {
                reason = string.IsNullOrEmpty(stage.PrerequisiteStageId)
                    ? "This stage is locked."
                    : $"Clear {stage.PrerequisiteStageId.Replace("stage_", string.Empty).Replace('_', '-')} first.";
                return false;
            }

            if (State.Energy < stage.EnergyCost)
            {
                reason = $"Not enough energy. Need {stage.EnergyCost}.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public bool TryStartStage(StageDefinition stage, out string reason)
        {
            if (!CanEnterStage(stage, out reason))
            {
                return false;
            }

            if (!State.TrySpendEnergy(stage.EnergyCost))
            {
                reason = "Not enough energy.";
                return false;
            }

            SaveAndRaiseProgress();
            return true;
        }

        public StageRewardGrant CommitBattleResult(StageDefinition stage, BattleResult result)
        {
            bool victory = result != null && result.Outcome == BattleOutcome.PlayerVictory;
            if (stage == null || !victory || State == null)
            {
                return StageRewardGrant.None(stage == null ? string.Empty : stage.Id, victory);
            }

            bool firstClear = State.RecordStageVictory(stage.Id);
            int crystals = firstClear ? stage.FirstClearCrystalReward : 0;
            State.AddCurrency(crystals);
            State.AddGold(stage.GoldReward);
            State.AddInventoryItem("echo_gel", stage.MaterialReward);
            int rareMaterials = stage.IsBossStage && firstClear ? 1 : 0;
            if (rareMaterials > 0)
            {
                State.AddInventoryItem("void_fragment", rareMaterials);
            }

            SaveAndRaiseProgress();
            return new StageRewardGrant(
                stage.Id,
                true,
                firstClear,
                crystals,
                stage.GoldReward,
                stage.MaterialReward,
                rareMaterials);
        }

        public bool TryClaimMission(string missionId, out string reason)
        {
            DemoMissionDefinition mission = DemoMissionCatalog.Get(missionId);
            if (mission == null || State == null)
            {
                reason = "Mission is unavailable.";
                return false;
            }

            if (State.IsMissionClaimed(mission.Id))
            {
                reason = "Reward already claimed.";
                return false;
            }

            if (!mission.IsComplete(State))
            {
                reason = "Mission is not complete.";
                return false;
            }

            if (!State.MarkMissionClaimed(mission.Id))
            {
                reason = "Reward already claimed.";
                return false;
            }

            State.AddCurrency(mission.CrystalReward);
            State.AddGold(mission.GoldReward);
            SaveAndRaiseProgress();
            reason = "Reward claimed.";
            return true;
        }

        public void SetMusicVolume(float value)
        {
            if (State == null)
            {
                return;
            }

            State.SetMusicVolume(value);
            SaveAndRaiseSettings();
        }

        public void SetEffectsVolume(float value)
        {
            if (State == null)
            {
                return;
            }

            State.SetEffectsVolume(value);
            SaveAndRaiseSettings();
        }

        public void SetFullscreen(bool value)
        {
            if (State == null)
            {
                return;
            }

            State.SetFullscreen(value);
            SaveAndRaiseSettings();
        }

        public void SetSixtyFps(bool value)
        {
            if (State == null)
            {
                return;
            }

            State.SetSixtyFps(value);
            SaveAndRaiseSettings();
        }

        private void SaveAndRaiseProgress()
        {
            saveService.Save(State);
            CurrencyChanged?.Invoke(State.Currency);
            InventoryChanged?.Invoke(State.InventoryItems);
            ProgressChanged?.Invoke(State);
            StateChanged?.Invoke(State);
        }

        private void SaveAndRaiseSettings()
        {
            saveService.Save(State);
            SettingsChanged?.Invoke(State.Settings);
            StateChanged?.Invoke(State);
        }

        private void RaiseAllChanged()
        {
            StateChanged?.Invoke(State);
            CurrencyChanged?.Invoke(Currency);
            CollectionChanged?.Invoke(State == null ? Array.Empty<OwnedCharacterState>() : State.OwnedCharacters);
            FormationChanged?.Invoke(State == null ? null : State.TeamFormation);
            ProgressChanged?.Invoke(State);
            InventoryChanged?.Invoke(State == null
                ? Array.Empty<InventoryItemState>()
                : State.InventoryItems);
            SettingsChanged?.Invoke(State == null ? null : State.Settings);
        }

        private static ISaveService CreateSaveService(GameDatabase database)
        {
            return new PlayerPrefsJsonSaveService(() => CreateDefaultState(database));
        }

        private static PlayerState CreateDefaultState(GameDatabase database)
        {
            return database == null ? PlayerState.CreateDefault() : database.CreateDefaultPlayerState();
        }
    }
}
