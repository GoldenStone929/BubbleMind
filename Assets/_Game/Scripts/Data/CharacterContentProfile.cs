using System;
using System.Collections.Generic;
using UnityEngine;

namespace GenericGachaRPG
{
    [Serializable]
    public sealed class SkillValueRecord
    {
        [SerializeField] private string key = "value";
        [SerializeField] private float value;
        [SerializeField] private SkillValueUnit unit = SkillValueUnit.Flat;

        public SkillValueRecord(string valueKey, float amount, SkillValueUnit valueUnit)
        {
            key = NormalizeId(valueKey, "value");
            value = IsFinite(amount) ? amount : 0f;
            unit = valueUnit;
        }

        public string Key => key;
        public float Value => value;
        public SkillValueUnit Unit => unit;

        private static string NormalizeId(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static bool IsFinite(float amount)
        {
            return !float.IsNaN(amount) && !float.IsInfinity(amount);
        }
    }

    [Serializable]
    public sealed class SkillRankRecord
    {
        [Min(1), SerializeField] private int level = 1;
        [TextArea, SerializeField] private string summary = string.Empty;
        [SerializeField] private List<SkillValueRecord> values = new List<SkillValueRecord>();

        public SkillRankRecord(
            int abilityLevel,
            string rankSummary,
            IEnumerable<SkillValueRecord> rankValues = null)
        {
            level = Mathf.Max(1, abilityLevel);
            summary = rankSummary ?? string.Empty;
            values = rankValues == null
                ? new List<SkillValueRecord>()
                : new List<SkillValueRecord>(rankValues);
        }

        public int Level => level;
        public string Summary => summary;
        public IReadOnlyList<SkillValueRecord> Values => values;
    }

    [Serializable]
    public sealed class CharacterAbilityRecord
    {
        [SerializeField] private string abilityId = "ability";
        [SerializeField] private CharacterAbilityKind kind = CharacterAbilityKind.Basic;
        [SerializeField] private RuntimeSkillSlot runtimeSlot = RuntimeSkillSlot.None;
        [SerializeField] private SkillDefinition runtimeSkill;
        [SerializeField] private string displayName = "Ability";
        [TextArea, SerializeField] private string summary = string.Empty;
        [SerializeField] private string triggerSummary = string.Empty;
        [SerializeField] private string targetSummary = string.Empty;
        [SerializeField] private string effectSummary = string.Empty;
        [Min(1), SerializeField] private int unlockLevel = 1;
        [Min(1), SerializeField] private int maxLevel = 1;
        [SerializeField] private SkillTag tags = SkillTag.None;
        [SerializeField] private List<SkillRankRecord> ranks = new List<SkillRankRecord>();

        public CharacterAbilityRecord(
            string id,
            CharacterAbilityKind abilityKind,
            RuntimeSkillSlot slot,
            SkillDefinition skill,
            string name,
            string abilitySummary,
            string trigger,
            string targets,
            string effects,
            int requiredLevel,
            int abilityMaxLevel,
            SkillTag abilityTags,
            IEnumerable<SkillRankRecord> rankRecords = null)
        {
            abilityId = string.IsNullOrWhiteSpace(id) ? "ability" : id.Trim();
            kind = abilityKind;
            runtimeSlot = slot;
            runtimeSkill = skill;
            displayName = string.IsNullOrWhiteSpace(name)
                ? skill == null ? abilityId : skill.DisplayName
                : name.Trim();
            summary = abilitySummary ?? string.Empty;
            triggerSummary = trigger ?? string.Empty;
            targetSummary = targets ?? string.Empty;
            effectSummary = effects ?? string.Empty;
            unlockLevel = Mathf.Max(1, requiredLevel);
            maxLevel = Mathf.Max(1, abilityMaxLevel);
            tags = abilityTags;
            ranks = rankRecords == null
                ? new List<SkillRankRecord>()
                : new List<SkillRankRecord>(rankRecords);
        }

        public string AbilityId => abilityId;
        public CharacterAbilityKind Kind => kind;
        public RuntimeSkillSlot RuntimeSlot => runtimeSlot;
        public SkillDefinition RuntimeSkill => runtimeSkill;
        public string DisplayName => runtimeSkill == null ? displayName : runtimeSkill.DisplayName;
        public string Summary => summary;
        public string TriggerSummary => triggerSummary;
        public string TargetSummary => targetSummary;
        public string EffectSummary => effectSummary;
        public int UnlockLevel => unlockLevel;
        public int MaxLevel => maxLevel;
        public SkillTag Tags => runtimeSkill == null ? tags : runtimeSkill.Tags;
        public IReadOnlyList<SkillRankRecord> Ranks => ranks;
    }

    [Serializable]
    public sealed class ProgressionStageRecord
    {
        [SerializeField] private ProgressionTrack track = ProgressionTrack.Level;
        [Min(0), SerializeField] private int requiredValue;
        [Min(1), SerializeField] private int levelCap = 1;
        [SerializeField] private string title = "Stage";
        [TextArea, SerializeField] private string summary = string.Empty;

        public ProgressionStageRecord(
            ProgressionTrack stageTrack,
            int gateValue,
            int stageLevelCap,
            string stageTitle,
            string stageSummary)
        {
            track = stageTrack;
            requiredValue = Mathf.Max(0, gateValue);
            levelCap = Mathf.Max(1, stageLevelCap);
            title = string.IsNullOrWhiteSpace(stageTitle) ? stageTrack.ToString() : stageTitle.Trim();
            summary = stageSummary ?? string.Empty;
        }

        public ProgressionTrack Track => track;
        public int RequiredValue => requiredValue;
        public int LevelCap => levelCap;
        public string Title => title;
        public string Summary => summary;
    }

    [Serializable]
    public sealed class AcquisitionRecord
    {
        [SerializeField] private AcquisitionSource source = AcquisitionSource.StandardRecruitment;
        [SerializeField] private string label = "Recruitment";
        [TextArea, SerializeField] private string availability = string.Empty;
        [TextArea, SerializeField] private string duplicateRule = string.Empty;

        public AcquisitionRecord(
            AcquisitionSource acquisitionSource,
            string sourceLabel,
            string availabilitySummary,
            string duplicateSummary)
        {
            source = acquisitionSource;
            label = string.IsNullOrWhiteSpace(sourceLabel) ? acquisitionSource.ToString() : sourceLabel.Trim();
            availability = availabilitySummary ?? string.Empty;
            duplicateRule = duplicateSummary ?? string.Empty;
        }

        public AcquisitionSource Source => source;
        public string Label => label;
        public string Availability => availability;
        public string DuplicateRule => duplicateRule;
    }

    [CreateAssetMenu(fileName = "CharacterContentProfile", menuName = "Generic Gacha RPG/Character Content Profile")]
    public sealed class CharacterContentProfile : ScriptableObject
    {
        [SerializeField] private string ownerCharacterId = "character";
        [SerializeField] private string schemaVersion = "1.0";
        [SerializeField] private string contentVersion = "2026.07.15";
        [SerializeField] private ContentApprovalStatus approvalStatus = ContentApprovalStatus.Draft;

        [Header("Identity Extension")]
        [SerializeField] private string title = "Signal";
        [SerializeField] private CharacterElement element = CharacterElement.Neutral;
        [SerializeField] private string faction = "Observatory";
        [SerializeField] private List<string> keywords = new List<string>();

        [Header("Archive and Progression")]
        [Min(1), SerializeField] private int maxLevel = 1;
        [TextArea, SerializeField] private string progressionSummary = string.Empty;
        [TextArea, SerializeField] private string awakeningSummary = string.Empty;
        [SerializeField] private List<ProgressionStageRecord> progressionStages = new List<ProgressionStageRecord>();
        [SerializeField] private List<CharacterAbilityRecord> abilities = new List<CharacterAbilityRecord>();
        [SerializeField] private List<AcquisitionRecord> acquisition = new List<AcquisitionRecord>();

        [Header("Relationships")]
        [SerializeField] private List<string> synergyTags = new List<string>();
        [SerializeField] private List<string> counterTags = new List<string>();

        [Header("Originality and Provenance")]
        [SerializeField] private bool originalText;
        [SerializeField] private bool independentlyAuthoredNumbers;
        [SerializeField] private string artSourceRecord = string.Empty;
        [TextArea, SerializeField] private string authoringNotes = string.Empty;

        public string OwnerCharacterId => ownerCharacterId;
        public string SchemaVersion => schemaVersion;
        public string ContentVersion => contentVersion;
        public ContentApprovalStatus ApprovalStatus => approvalStatus;
        public string Title => title;
        public CharacterElement Element => element;
        public string Faction => faction;
        public IReadOnlyList<string> Keywords => keywords;
        public int MaxLevel => maxLevel;
        public string ProgressionSummary => progressionSummary;
        public string AwakeningSummary => awakeningSummary;
        public IReadOnlyList<ProgressionStageRecord> ProgressionStages => progressionStages;
        public IReadOnlyList<CharacterAbilityRecord> Abilities => abilities;
        public IReadOnlyList<AcquisitionRecord> Acquisition => acquisition;
        public IReadOnlyList<string> SynergyTags => synergyTags;
        public IReadOnlyList<string> CounterTags => counterTags;
        public bool OriginalText => originalText;
        public bool IndependentlyAuthoredNumbers => independentlyAuthoredNumbers;
        public string ArtSourceRecord => artSourceRecord;
        public string AuthoringNotes => authoringNotes;

        public void Configure(
            string profileSchemaVersion,
            string profileContentVersion,
            ContentApprovalStatus status,
            string characterTitle,
            CharacterElement characterElement,
            string characterFaction,
            IEnumerable<string> characterKeywords,
            int characterMaxLevel,
            string characterProgressionSummary,
            string characterAwakeningSummary,
            IEnumerable<ProgressionStageRecord> stages,
            IEnumerable<CharacterAbilityRecord> abilityRecords,
            IEnumerable<AcquisitionRecord> acquisitionRecords,
            IEnumerable<string> synergies,
            IEnumerable<string> counters,
            bool hasOriginalText,
            bool hasIndependentlyAuthoredNumbers,
            string sourceRecord,
            string notes)
        {
            schemaVersion = NormalizeText(profileSchemaVersion, "1.0");
            contentVersion = NormalizeText(profileContentVersion, "unversioned");
            approvalStatus = status;
            title = NormalizeText(characterTitle, "Signal");
            element = characterElement;
            faction = NormalizeText(characterFaction, "Independent");
            keywords = NormalizeList(characterKeywords);
            maxLevel = Mathf.Max(1, characterMaxLevel);
            progressionSummary = characterProgressionSummary ?? string.Empty;
            awakeningSummary = characterAwakeningSummary ?? string.Empty;
            progressionStages = stages == null
                ? new List<ProgressionStageRecord>()
                : new List<ProgressionStageRecord>(stages);
            abilities = abilityRecords == null
                ? new List<CharacterAbilityRecord>()
                : new List<CharacterAbilityRecord>(abilityRecords);
            acquisition = acquisitionRecords == null
                ? new List<AcquisitionRecord>()
                : new List<AcquisitionRecord>(acquisitionRecords);
            synergyTags = NormalizeList(synergies);
            counterTags = NormalizeList(counters);
            originalText = hasOriginalText;
            independentlyAuthoredNumbers = hasIndependentlyAuthoredNumbers;
            artSourceRecord = sourceRecord == null ? string.Empty : sourceRecord.Trim();
            authoringNotes = notes ?? string.Empty;
        }

        public void BindToCharacter(string characterId)
        {
            ownerCharacterId = characterId == null ? string.Empty : characterId.Trim();
        }

        public CharacterAbilityRecord GetAbility(RuntimeSkillSlot slot)
        {
            for (int i = 0; i < abilities.Count; i++)
            {
                CharacterAbilityRecord ability = abilities[i];
                if (ability != null && ability.RuntimeSlot == slot)
                {
                    return ability;
                }
            }

            return null;
        }

        public bool TryValidate(CharacterDefinition character, out string issue)
        {
            if (character == null)
            {
                issue = "Character reference is missing.";
                return false;
            }

            if (!string.Equals(ownerCharacterId, character.Id, StringComparison.Ordinal))
            {
                issue =
                    $"Profile owner '{ownerCharacterId}' does not match character '{character.Id}'.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(schemaVersion) || string.IsNullOrWhiteSpace(contentVersion))
            {
                issue = "Schema or content version is empty.";
                return false;
            }

            if (approvalStatus != ContentApprovalStatus.Approved)
            {
                issue = $"Profile status is {approvalStatus}; Approved is required.";
                return false;
            }

            if (!originalText || !independentlyAuthoredNumbers || string.IsNullOrWhiteSpace(artSourceRecord))
            {
                issue = "Originality or asset provenance is incomplete.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(faction) ||
                string.IsNullOrWhiteSpace(progressionSummary) || string.IsNullOrWhiteSpace(awakeningSummary) ||
                keywords == null || keywords.Count == 0 || synergyTags == null || synergyTags.Count == 0 ||
                counterTags == null || counterTags.Count == 0)
            {
                issue = "Identity, progression, or relationship metadata is incomplete.";
                return false;
            }

            if (abilities == null || abilities.Count < 4)
            {
                issue = "At least Basic plus the three runtime abilities are required.";
                return false;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var slots = new HashSet<RuntimeSkillSlot>();
            int basicCount = 0;
            for (int i = 0; i < abilities.Count; i++)
            {
                CharacterAbilityRecord ability = abilities[i];
                if (ability == null || string.IsNullOrWhiteSpace(ability.AbilityId))
                {
                    issue = $"Ability record {i} is null or has no id.";
                    return false;
                }

                if (!ids.Add(ability.AbilityId))
                {
                    issue = $"Ability id '{ability.AbilityId}' is duplicated.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(ability.DisplayName) ||
                    string.IsNullOrWhiteSpace(ability.Summary) ||
                    string.IsNullOrWhiteSpace(ability.TriggerSummary) ||
                    string.IsNullOrWhiteSpace(ability.TargetSummary) ||
                    string.IsNullOrWhiteSpace(ability.EffectSummary) ||
                    ability.Tags == SkillTag.None)
                {
                    issue = $"Ability '{ability.AbilityId}' has incomplete player-facing details.";
                    return false;
                }

                if (ability.Kind == CharacterAbilityKind.Basic)
                {
                    basicCount++;
                    if (ability.RuntimeSlot != RuntimeSkillSlot.None)
                    {
                        issue = $"Basic ability '{ability.AbilityId}' cannot occupy a runtime skill slot.";
                        return false;
                    }
                }

                if (ability.RuntimeSlot != RuntimeSkillSlot.None && !slots.Add(ability.RuntimeSlot))
                {
                    issue = $"Runtime slot {ability.RuntimeSlot} is duplicated.";
                    return false;
                }

                if (ability.UnlockLevel > maxLevel || ability.MaxLevel < 1)
                {
                    issue = $"Ability '{ability.AbilityId}' has an invalid level range.";
                    return false;
                }

                if (ability.Ranks == null || ability.Ranks.Count == 0)
                {
                    issue = $"Ability '{ability.AbilityId}' has no rank records.";
                    return false;
                }

                if (ability.Ranks.Count != ability.MaxLevel)
                {
                    issue =
                        $"Ability '{ability.AbilityId}' must define every rank from 1 to {ability.MaxLevel}.";
                    return false;
                }

                int previousRank = 0;
                for (int rankIndex = 0; rankIndex < ability.Ranks.Count; rankIndex++)
                {
                    SkillRankRecord rank = ability.Ranks[rankIndex];
                    int expectedRank = rankIndex + 1;
                    if (rank == null || rank.Level != expectedRank ||
                        rank.Level <= previousRank || rank.Level > ability.MaxLevel)
                    {
                        issue =
                            $"Ability '{ability.AbilityId}' must define contiguous ranks; " +
                            $"expected {expectedRank}.";
                        return false;
                    }

                    previousRank = rank.Level;
                    var valueKeys = new HashSet<string>(StringComparer.Ordinal);
                    for (int valueIndex = 0; valueIndex < rank.Values.Count; valueIndex++)
                    {
                        SkillValueRecord valueRecord = rank.Values[valueIndex];
                        if (valueRecord == null || string.IsNullOrWhiteSpace(valueRecord.Key) ||
                            !valueKeys.Add(valueRecord.Key) || float.IsNaN(valueRecord.Value) ||
                            float.IsInfinity(valueRecord.Value) ||
                            !Enum.IsDefined(typeof(SkillValueUnit), valueRecord.Unit))
                        {
                            issue = $"Ability '{ability.AbilityId}' rank {rank.Level} has invalid values.";
                            return false;
                        }
                    }
                }

                if (previousRank != ability.MaxLevel)
                {
                    issue = $"Ability '{ability.AbilityId}' does not end at MaxLevel {ability.MaxLevel}.";
                    return false;
                }

                if (!ValidateRuntimePower(ability, out issue))
                {
                    return false;
                }
            }

            if (basicCount != 1)
            {
                issue = $"Exactly one Basic ability is required; found {basicCount}.";
                return false;
            }

            if (!ValidateRuntimeSlot(
                    RuntimeSkillSlot.Ultimate,
                    CharacterAbilityKind.Ultimate,
                    character.UltimateSkill,
                    out issue) ||
                !ValidateRuntimeSlot(
                    RuntimeSkillSlot.Skill2,
                    CharacterAbilityKind.Active,
                    character.Skill2,
                    out issue) ||
                !ValidateRuntimeSlot(
                    RuntimeSkillSlot.Skill3,
                    CharacterAbilityKind.Active,
                    character.Skill3,
                    out issue))
            {
                return false;
            }

            if (character.UltimateSkill.RageCost <= 0 || character.UltimateSkill.RageCost > character.MaxRage)
            {
                issue = "Ultimate Rage cost must be positive and no greater than the character Rage cap.";
                return false;
            }

            if (progressionStages == null || progressionStages.Count == 0)
            {
                issue = "No progression stage is defined.";
                return false;
            }

            var progressionKeys = new HashSet<string>(StringComparer.Ordinal);
            bool hasOwnershipStage = false;
            for (int i = 0; i < progressionStages.Count; i++)
            {
                ProgressionStageRecord stage = progressionStages[i];
                string key = stage == null ? string.Empty : $"{stage.Track}:{stage.RequiredValue}";
                if (stage == null || string.IsNullOrWhiteSpace(stage.Title) ||
                    string.IsNullOrWhiteSpace(stage.Summary) || stage.RequiredValue > stage.LevelCap ||
                    !Enum.IsDefined(typeof(ProgressionTrack), stage.Track) || !progressionKeys.Add(key))
                {
                    issue = $"Progression stage {i} is incomplete or duplicated.";
                    return false;
                }

                hasOwnershipStage |= stage.Track == ProgressionTrack.Ownership;
            }

            if (!hasOwnershipStage)
            {
                issue = "An Ownership progression stage is required.";
                return false;
            }

            if (acquisition == null || acquisition.Count == 0)
            {
                issue = "No acquisition source is defined.";
                return false;
            }

            var acquisitionKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < acquisition.Count; i++)
            {
                AcquisitionRecord record = acquisition[i];
                string key = record == null ? string.Empty : $"{record.Source}:{record.Label}";
                if (record == null || string.IsNullOrWhiteSpace(record.Label) ||
                    string.IsNullOrWhiteSpace(record.Availability) ||
                    string.IsNullOrWhiteSpace(record.DuplicateRule) ||
                    !Enum.IsDefined(typeof(AcquisitionSource), record.Source) ||
                    !acquisitionKeys.Add(key))
                {
                    issue = $"Acquisition record {i} is incomplete or duplicated.";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        private bool ValidateRuntimeSlot(
            RuntimeSkillSlot slot,
            CharacterAbilityKind expectedKind,
            SkillDefinition expectedSkill,
            out string issue)
        {
            CharacterAbilityRecord ability = GetAbility(slot);
            if (ability == null || ability.Kind != expectedKind || ability.RuntimeSkill != expectedSkill)
            {
                issue = $"Runtime slot {slot} does not match CharacterDefinition.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private static bool ValidateRuntimePower(CharacterAbilityRecord ability, out string issue)
        {
            issue = string.Empty;
            if (ability.RuntimeSkill == null || ability.Ranks.Count == 0)
            {
                return true;
            }

            SkillRankRecord finalRank = ability.Ranks[ability.Ranks.Count - 1];
            SkillValueRecord authoredPower = null;
            for (int i = 0; i < finalRank.Values.Count; i++)
            {
                SkillValueRecord value = finalRank.Values[i];
                if (value != null &&
                    (string.Equals(value.Key, "damage", StringComparison.Ordinal) ||
                     string.Equals(value.Key, "power", StringComparison.Ordinal)))
                {
                    authoredPower = value;
                    break;
                }
            }

            if (authoredPower == null || authoredPower.Unit != SkillValueUnit.PercentOfAttack)
            {
                return true;
            }

            float runtimePower = ability.RuntimeSkill.Category == SkillCategory.Healing
                ? ability.RuntimeSkill.HealingMultiplier * 100f
                : ability.RuntimeSkill.DamageMultiplier * 100f;
            if (Mathf.Approximately(authoredPower.Value, runtimePower))
            {
                return true;
            }

            issue =
                $"Ability '{ability.AbilityId}' max-rank power {authoredPower.Value:0.##}% " +
                $"does not match runtime skill power {runtimePower:0.##}%.";
            return false;
        }

        private static string NormalizeText(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static List<string> NormalizeList(IEnumerable<string> values)
        {
            var normalized = new List<string>();
            if (values == null)
            {
                return normalized;
            }

            var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string value in values)
            {
                string item = value == null ? string.Empty : value.Trim();
                if (!string.IsNullOrEmpty(item) && unique.Add(item))
                {
                    normalized.Add(item);
                }
            }

            return normalized;
        }
    }
}
