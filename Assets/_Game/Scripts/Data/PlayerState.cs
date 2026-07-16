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
    public sealed class InventoryItemState
    {
        [SerializeField] private string itemId = string.Empty;
        [Min(0), SerializeField] private int amount;

        public string ItemId => itemId;
        public int Amount => amount;

        public InventoryItemState()
        {
        }

        public InventoryItemState(string id, int initialAmount)
        {
            itemId = id == null ? string.Empty : id.Trim();
            amount = Math.Max(0, initialAmount);
        }

        internal void Add(int value)
        {
            if (value <= 0)
            {
                return;
            }

            long next = (long)amount + value;
            amount = next >= int.MaxValue ? int.MaxValue : (int)next;
        }

        internal void Normalize()
        {
            itemId = itemId == null ? string.Empty : itemId.Trim();
            amount = Math.Max(0, amount);
        }
    }

    [Serializable]
    public sealed class PlayerSettingsState
    {
        [Range(0f, 1f), SerializeField] private float musicVolume = 0.75f;
        [Range(0f, 1f), SerializeField] private float effectsVolume = 0.85f;
        [SerializeField] private bool fullscreen = true;
        [SerializeField] private bool sixtyFps = true;

        public float MusicVolume => musicVolume;
        public float EffectsVolume => effectsVolume;
        public bool Fullscreen => fullscreen;
        public bool SixtyFps => sixtyFps;

        internal void SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
        }

        internal void SetEffectsVolume(float value)
        {
            effectsVolume = Mathf.Clamp01(value);
        }

        internal void SetFullscreen(bool value)
        {
            fullscreen = value;
        }

        internal void SetSixtyFps(bool value)
        {
            sixtyFps = value;
        }

        internal void Normalize()
        {
            musicVolume = Mathf.Clamp01(musicVolume);
            effectsVolume = Mathf.Clamp01(effectsVolume);
        }
    }

    [Serializable]
    public sealed class TeamFormationState
    {
        public const int RequiredMemberCount = BattleRules.TeamSize;

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
        public const int CurrentSchemaVersion = 4;

        [SerializeField] private int schemaVersion = CurrentSchemaVersion;
        [SerializeField] private string playerName = "Observer 07";
        [Min(1), SerializeField] private int playerLevel = 12;
        [Min(0), SerializeField] private int currency;
        [Min(0), SerializeField] private int gold = 2500;
        [Min(0), SerializeField] private int energy = 120;
        [Min(1), SerializeField] private int maxEnergy = 120;
        [Min(0), SerializeField] private int totalDraws;
        [Min(0), SerializeField] private int battleWins;
        [SerializeField] private List<OwnedCharacterState> ownedCharacters = new List<OwnedCharacterState>();
        [SerializeField] private TeamFormationState teamFormation = new TeamFormationState();
        [SerializeField] private List<InventoryItemState> inventoryItems = new List<InventoryItemState>();
        [SerializeField] private List<string> clearedStageIds = new List<string>();
        [SerializeField] private List<string> claimedMissionIds = new List<string>();
        [SerializeField] private PlayerSettingsState settings = new PlayerSettingsState();
        [SerializeField] private string lastSavedUtc = string.Empty;

        public int SchemaVersion => schemaVersion;
        public string PlayerName => playerName;
        public int PlayerLevel => playerLevel;
        public int Currency => currency;
        public int Gold => gold;
        public int Energy => energy;
        public int MaxEnergy => maxEnergy;
        public int TotalDraws => totalDraws;
        public int BattleWins => battleWins;
        public IReadOnlyList<OwnedCharacterState> OwnedCharacters => ownedCharacters;
        public TeamFormationState TeamFormation => teamFormation;
        public IReadOnlyList<InventoryItemState> InventoryItems => inventoryItems;
        public IReadOnlyList<string> ClearedStageIds => clearedStageIds;
        public IReadOnlyList<string> ClaimedMissionIds => claimedMissionIds;
        public PlayerSettingsState Settings => settings;
        public string LastSavedUtc => lastSavedUtc;

        public static PlayerState CreateDefault(int startingCurrency = 1000, IEnumerable<string> starterCharacterIds = null)
        {
            var state = new PlayerState
            {
                schemaVersion = CurrentSchemaVersion,
                playerName = "Observer 07",
                playerLevel = 12,
                currency = Math.Max(0, startingCurrency),
                gold = 2500,
                energy = 120,
                maxEnergy = 120,
                ownedCharacters = new List<OwnedCharacterState>(),
                teamFormation = new TeamFormationState(),
                inventoryItems = new List<InventoryItemState>
                {
                    new InventoryItemState("standard_ticket", 3),
                    new InventoryItemState("echo_gel", 12),
                    new InventoryItemState("void_fragment", 1),
                    new InventoryItemState("universal_shard", 0)
                },
                clearedStageIds = new List<string>(),
                claimedMissionIds = new List<string>(),
                settings = new PlayerSettingsState()
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

        public int GetItemAmount(string itemId)
        {
            InventoryItemState item = FindInventoryItem(itemId);
            return item == null ? 0 : item.Amount;
        }

        public InventoryItemState FindInventoryItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || inventoryItems == null)
            {
                return null;
            }

            string normalizedId = itemId.Trim();
            for (int index = 0; index < inventoryItems.Count; index++)
            {
                InventoryItemState item = inventoryItems[index];
                if (item != null && string.Equals(item.ItemId, normalizedId, StringComparison.Ordinal))
                {
                    return item;
                }
            }

            return null;
        }

        public bool IsStageCleared(string stageId)
        {
            return ContainsId(clearedStageIds, stageId);
        }

        public bool IsMissionClaimed(string missionId)
        {
            return ContainsId(claimedMissionIds, missionId);
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

        internal bool TrySpendEnergy(int amount)
        {
            if (amount < 0 || energy < amount)
            {
                return false;
            }

            energy -= amount;
            return true;
        }

        internal void AddCurrency(int amount)
        {
            currency = SaturatingAdd(currency, amount);
        }

        internal void AddGold(int amount)
        {
            gold = SaturatingAdd(gold, amount);
        }

        internal void AddInventoryItem(string itemId, int amount)
        {
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            {
                return;
            }

            if (inventoryItems == null)
            {
                inventoryItems = new List<InventoryItemState>();
            }

            string normalizedId = itemId.Trim();
            InventoryItemState existing = FindInventoryItem(normalizedId);
            if (existing == null)
            {
                inventoryItems.Add(new InventoryItemState(normalizedId, amount));
            }
            else
            {
                existing.Add(amount);
            }
        }

        internal void RecordGachaDraw(bool duplicate)
        {
            totalDraws = SaturatingAdd(totalDraws, 1);
            if (duplicate)
            {
                AddInventoryItem("universal_shard", 10);
            }
        }

        internal bool RecordStageVictory(string stageId)
        {
            battleWins = SaturatingAdd(battleWins, 1);
            if (string.IsNullOrWhiteSpace(stageId))
            {
                return false;
            }

            if (clearedStageIds == null)
            {
                clearedStageIds = new List<string>();
            }

            string normalizedId = stageId.Trim();
            if (ContainsId(clearedStageIds, normalizedId))
            {
                return false;
            }

            clearedStageIds.Add(normalizedId);
            return true;
        }

        internal bool MarkMissionClaimed(string missionId)
        {
            if (string.IsNullOrWhiteSpace(missionId))
            {
                return false;
            }

            if (claimedMissionIds == null)
            {
                claimedMissionIds = new List<string>();
            }

            string normalizedId = missionId.Trim();
            if (ContainsId(claimedMissionIds, normalizedId))
            {
                return false;
            }

            claimedMissionIds.Add(normalizedId);
            return true;
        }

        internal void SetMusicVolume(float value)
        {
            EnsureSettings();
            settings.SetMusicVolume(value);
        }

        internal void SetEffectsVolume(float value)
        {
            EnsureSettings();
            settings.SetEffectsVolume(value);
        }

        internal void SetFullscreen(bool value)
        {
            EnsureSettings();
            settings.SetFullscreen(value);
        }

        internal void SetSixtyFps(bool value)
        {
            EnsureSettings();
            settings.SetSixtyFps(value);
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
            if (schemaVersion < 3 || schemaVersion > CurrentSchemaVersion)
            {
                return false;
            }

            bool migrateFromVersionThree = schemaVersion == 3;
            schemaVersion = CurrentSchemaVersion;

            playerName = string.IsNullOrWhiteSpace(playerName) ? "Observer 07" : playerName.Trim();
            playerLevel = Math.Max(1, playerLevel);

            currency = Math.Max(0, currency);
            gold = migrateFromVersionThree && gold <= 0 ? 2500 : Math.Max(0, gold);
            maxEnergy = migrateFromVersionThree && maxEnergy <= 0 ? 120 : Math.Max(1, maxEnergy);
            energy = migrateFromVersionThree && energy <= 0
                ? maxEnergy
                : Math.Max(0, Math.Min(energy, maxEnergy));
            totalDraws = Math.Max(0, totalDraws);
            battleWins = Math.Max(0, battleWins);
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
            NormalizeInventory(migrateFromVersionThree);
            NormalizeIdList(ref clearedStageIds);
            NormalizeIdList(ref claimedMissionIds);
            EnsureSettings();
            settings.Normalize();
            lastSavedUtc = lastSavedUtc ?? string.Empty;
            return true;
        }

        private void NormalizeInventory(bool addStarterItems)
        {
            if (inventoryItems == null)
            {
                inventoryItems = new List<InventoryItemState>();
            }

            var byId = new Dictionary<string, InventoryItemState>(StringComparer.Ordinal);
            for (int index = inventoryItems.Count - 1; index >= 0; index--)
            {
                InventoryItemState item = inventoryItems[index];
                if (item == null)
                {
                    inventoryItems.RemoveAt(index);
                    continue;
                }

                item.Normalize();
                if (string.IsNullOrEmpty(item.ItemId))
                {
                    inventoryItems.RemoveAt(index);
                    continue;
                }

                if (byId.TryGetValue(item.ItemId, out InventoryItemState existing))
                {
                    existing.Add(item.Amount);
                    inventoryItems.RemoveAt(index);
                }
                else
                {
                    byId.Add(item.ItemId, item);
                }
            }

            if (addStarterItems)
            {
                AddInventoryItem("standard_ticket", 3);
                AddInventoryItem("echo_gel", 12);
                AddInventoryItem("void_fragment", 1);
            }

            if (FindInventoryItem("universal_shard") == null)
            {
                inventoryItems.Add(new InventoryItemState("universal_shard", 0));
            }
        }

        private void EnsureSettings()
        {
            if (settings == null)
            {
                settings = new PlayerSettingsState();
            }
        }

        private static bool ContainsId(IReadOnlyList<string> values, string value)
        {
            if (values == null || string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string normalized = value.Trim();
            for (int index = 0; index < values.Count; index++)
            {
                if (string.Equals(values[index], normalized, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static int SaturatingAdd(int current, int amount)
        {
            if (amount <= 0)
            {
                return Math.Max(0, current);
            }

            long next = (long)Math.Max(0, current) + amount;
            return next >= int.MaxValue ? int.MaxValue : (int)next;
        }

        private static void NormalizeIdList(ref List<string> values)
        {
            if (values == null)
            {
                values = new List<string>();
                return;
            }

            var unique = new HashSet<string>(StringComparer.Ordinal);
            for (int index = values.Count - 1; index >= 0; index--)
            {
                string id = values[index] == null ? string.Empty : values[index].Trim();
                if (string.IsNullOrEmpty(id) || !unique.Add(id))
                {
                    values.RemoveAt(index);
                }
                else
                {
                    values[index] = id;
                }
            }
        }
    }
}
