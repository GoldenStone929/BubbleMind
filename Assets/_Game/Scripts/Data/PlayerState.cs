using System;
using System.Collections.Generic;
using UnityEngine;

namespace GenericGachaRPG
{
    [Serializable]
    public sealed class OwnedCharacterState
    {
        [SerializeField] private string characterId = string.Empty;
        [Min(1), SerializeField] private int level = 1;
        [Min(1), SerializeField] private int copies = 1;

        public string CharacterId => characterId;
        public int Level => level;
        public int Copies => copies;

        public OwnedCharacterState()
        {
        }

        public OwnedCharacterState(string id, int initialLevel = 1, int initialCopies = 1)
        {
            characterId = id == null ? string.Empty : id.Trim();
            level = Math.Max(1, initialLevel);
            copies = Math.Max(1, initialCopies);
        }

        public void RegisterCopy()
        {
            copies = copies >= int.MaxValue ? int.MaxValue : Math.Max(1, copies + 1);
        }

        internal void Normalize()
        {
            characterId = characterId == null ? string.Empty : characterId.Trim();
            level = Math.Max(1, level);
            copies = Math.Max(1, copies);
        }
    }

    [Serializable]
    public sealed class TeamFormationState
    {
        public const int RequiredMemberCount = 3;

        [SerializeField] private List<string> characterIds = new List<string>();

        public IReadOnlyList<string> CharacterIds => characterIds;
        public int Count => characterIds == null ? 0 : characterIds.Count;
        public bool IsComplete => Count == RequiredMemberCount;

        public TeamFormationState()
        {
        }

        public TeamFormationState(IEnumerable<string> ids)
        {
            Replace(ids);
        }

        internal void Replace(IEnumerable<string> ids)
        {
            characterIds = ids == null ? new List<string>() : new List<string>(ids);
            Normalize();
        }

        internal void Normalize()
        {
            if (characterIds == null)
            {
                characterIds = new List<string>();
                return;
            }

            var unique = new HashSet<string>(StringComparer.Ordinal);
            for (int i = characterIds.Count - 1; i >= 0; i--)
            {
                string id = characterIds[i] == null ? string.Empty : characterIds[i].Trim();
                if (string.IsNullOrEmpty(id) || !unique.Add(id))
                {
                    characterIds.RemoveAt(i);
                }
                else
                {
                    characterIds[i] = id;
                }
            }

            if (characterIds.Count > RequiredMemberCount)
            {
                characterIds.RemoveRange(RequiredMemberCount, characterIds.Count - RequiredMemberCount);
            }
        }
    }

    [Serializable]
    public sealed class PlayerState
    {
        public const int CurrentSchemaVersion = 1;

        [SerializeField] private int schemaVersion = CurrentSchemaVersion;
        [Min(0), SerializeField] private int currency;
        [SerializeField] private List<OwnedCharacterState> ownedCharacters = new List<OwnedCharacterState>();
        [SerializeField] private TeamFormationState teamFormation = new TeamFormationState();
        [SerializeField] private string lastSavedUtc = string.Empty;

        public int SchemaVersion => schemaVersion;
        public int Currency => currency;
        public IReadOnlyList<OwnedCharacterState> OwnedCharacters => ownedCharacters;
        public TeamFormationState TeamFormation => teamFormation;
        public string LastSavedUtc => lastSavedUtc;

        public static PlayerState CreateDefault(int startingCurrency = 1000, IEnumerable<string> starterCharacterIds = null)
        {
            var state = new PlayerState
            {
                schemaVersion = CurrentSchemaVersion,
                currency = Math.Max(0, startingCurrency),
                ownedCharacters = new List<OwnedCharacterState>(),
                teamFormation = new TeamFormationState()
            };

            if (starterCharacterIds != null)
            {
                var unique = new HashSet<string>(StringComparer.Ordinal);
                foreach (string rawId in starterCharacterIds)
                {
                    string id = rawId == null ? string.Empty : rawId.Trim();
                    if (!string.IsNullOrEmpty(id) && unique.Add(id))
                    {
                        state.ownedCharacters.Add(new OwnedCharacterState(id));
                    }
                }
            }

            if (state.ownedCharacters.Count >= TeamFormationState.RequiredMemberCount)
            {
                var initialTeam = new List<string>(TeamFormationState.RequiredMemberCount);
                for (int i = 0; i < TeamFormationState.RequiredMemberCount; i++)
                {
                    initialTeam.Add(state.ownedCharacters[i].CharacterId);
                }

                state.teamFormation.Replace(initialTeam);
            }

            state.Normalize();
            return state;
        }

        public bool HasCharacter(string characterId)
        {
            return FindOwnedCharacter(characterId) != null;
        }

        public OwnedCharacterState FindOwnedCharacter(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || ownedCharacters == null)
            {
                return null;
            }

            string normalizedId = characterId.Trim();
            for (int i = 0; i < ownedCharacters.Count; i++)
            {
                OwnedCharacterState owned = ownedCharacters[i];
                if (owned != null && string.Equals(owned.CharacterId, normalizedId, StringComparison.Ordinal))
                {
                    return owned;
                }
            }

            return null;
        }

        internal bool TrySpendCurrency(int amount)
        {
            if (amount < 0 || currency < amount)
            {
                return false;
            }

            currency -= amount;
            return true;
        }

        internal bool RegisterCharacterCopy(string characterId)
        {
            string id = characterId == null ? string.Empty : characterId.Trim();
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Character id cannot be empty.", nameof(characterId));
            }

            OwnedCharacterState existing = FindOwnedCharacter(id);
            if (existing != null)
            {
                existing.RegisterCopy();
                return false;
            }

            ownedCharacters.Add(new OwnedCharacterState(id));
            return true;
        }

        internal void ReplaceFormation(IEnumerable<string> characterIds)
        {
            if (teamFormation == null)
            {
                teamFormation = new TeamFormationState();
            }

            teamFormation.Replace(characterIds);
        }

        internal void MarkSaved()
        {
            schemaVersion = CurrentSchemaVersion;
            lastSavedUtc = DateTime.UtcNow.ToString("O");
        }

        internal bool Normalize()
        {
            if (schemaVersion != CurrentSchemaVersion)
            {
                return false;
            }

            currency = Math.Max(0, currency);
            if (ownedCharacters == null)
            {
                ownedCharacters = new List<OwnedCharacterState>();
            }

            var unique = new HashSet<string>(StringComparer.Ordinal);
            for (int i = ownedCharacters.Count - 1; i >= 0; i--)
            {
                OwnedCharacterState owned = ownedCharacters[i];
                if (owned == null)
                {
                    ownedCharacters.RemoveAt(i);
                    continue;
                }

                owned.Normalize();
                if (string.IsNullOrEmpty(owned.CharacterId) || !unique.Add(owned.CharacterId))
                {
                    ownedCharacters.RemoveAt(i);
                }
            }

            if (teamFormation == null)
            {
                teamFormation = new TeamFormationState();
            }

            teamFormation.Normalize();
            var validTeam = new List<string>(TeamFormationState.RequiredMemberCount);
            for (int i = 0; i < teamFormation.CharacterIds.Count; i++)
            {
                string id = teamFormation.CharacterIds[i];
                if (HasCharacter(id))
                {
                    validTeam.Add(id);
                }
            }

            teamFormation.Replace(validTeam);
            lastSavedUtc = lastSavedUtc ?? string.Empty;
            return true;
        }
    }
}
