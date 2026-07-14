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
            saveService.Save(State);
            CurrencyChanged?.Invoke(State.Currency);
            CollectionChanged?.Invoke(State.OwnedCharacters);
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

        private void RaiseAllChanged()
        {
            StateChanged?.Invoke(State);
            CurrencyChanged?.Invoke(Currency);
            CollectionChanged?.Invoke(State == null ? Array.Empty<OwnedCharacterState>() : State.OwnedCharacters);
            FormationChanged?.Invoke(State == null ? null : State.TeamFormation);
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
