using System;
using System.Collections.Generic;
using UnityEngine;

namespace GenericGachaRPG
{
    /// <summary>Central runtime content catalog used by the generated demo scene.</summary>
    [CreateAssetMenu(fileName = "GameDatabase", menuName = "Generic Gacha RPG/Game Database")]
    public sealed class GameDatabase : ScriptableObject
    {
        [Min(0), SerializeField] private int startingCurrency = 1000;
        [SerializeField] private List<string> starterCharacterIds = new List<string>();
        [SerializeField] private List<string> demoPlayerBattleCharacterIds = new List<string>();
        [SerializeField] private List<string> demoEnemyBattleCharacterIds = new List<string>();
        [SerializeField] private List<CharacterDefinition> characters = new List<CharacterDefinition>();
        [SerializeField] private List<SkillDefinition> skills = new List<SkillDefinition>();
        [SerializeField] private List<GachaBannerDefinition> gachaBanners = new List<GachaBannerDefinition>();
        [SerializeField] private List<StageDefinition> stages = new List<StageDefinition>();

        public int StartingCurrency => startingCurrency;
        public IReadOnlyList<string> StarterCharacterIds => starterCharacterIds;
        public IReadOnlyList<string> DemoPlayerBattleCharacterIds => demoPlayerBattleCharacterIds;
        public IReadOnlyList<string> DemoEnemyBattleCharacterIds => demoEnemyBattleCharacterIds;
        public IReadOnlyList<CharacterDefinition> Characters => characters;
        public IReadOnlyList<SkillDefinition> Skills => skills;
        public IReadOnlyList<GachaBannerDefinition> GachaBanners => gachaBanners;
        public IReadOnlyList<StageDefinition> Stages => stages;
        public GachaBannerDefinition DefaultBanner => gachaBanners.Count > 0 ? gachaBanners[0] : null;
        public StageDefinition FirstStage => stages.Count > 0 ? stages[0] : null;

        public CharacterDefinition GetCharacter(string characterId)
        {
            TryGetCharacter(characterId, out CharacterDefinition character);
            return character;
        }

        public bool TryGetCharacter(string characterId, out CharacterDefinition character)
        {
            character = null;
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return false;
            }

            string normalizedId = characterId.Trim();
            for (int i = 0; i < characters.Count; i++)
            {
                CharacterDefinition candidate = characters[i];
                if (candidate != null && string.Equals(candidate.Id, normalizedId, StringComparison.Ordinal))
                {
                    character = candidate;
                    return true;
                }
            }

            return false;
        }

        public SkillDefinition GetSkill(string skillId)
        {
            TryGetSkill(skillId, out SkillDefinition skill);
            return skill;
        }

        public bool TryGetSkill(string skillId, out SkillDefinition skill)
        {
            skill = null;
            if (string.IsNullOrWhiteSpace(skillId))
            {
                return false;
            }

            string normalizedId = skillId.Trim();
            for (int i = 0; i < skills.Count; i++)
            {
                SkillDefinition candidate = skills[i];
                if (candidate != null && string.Equals(candidate.Id, normalizedId, StringComparison.Ordinal))
                {
                    skill = candidate;
                    return true;
                }
            }

            return false;
        }

        public GachaBannerDefinition GetBanner(string bannerId)
        {
            TryGetBanner(bannerId, out GachaBannerDefinition banner);
            return banner;
        }

        public bool TryGetBanner(string bannerId, out GachaBannerDefinition banner)
        {
            banner = null;
            if (string.IsNullOrWhiteSpace(bannerId))
            {
                return false;
            }

            string normalizedId = bannerId.Trim();
            for (int i = 0; i < gachaBanners.Count; i++)
            {
                GachaBannerDefinition candidate = gachaBanners[i];
                if (candidate != null && string.Equals(candidate.Id, normalizedId, StringComparison.Ordinal))
                {
                    banner = candidate;
                    return true;
                }
            }

            return false;
        }

        public StageDefinition GetStage(string stageId)
        {
            TryGetStage(stageId, out StageDefinition stage);
            return stage;
        }

        public bool TryGetStage(string stageId, out StageDefinition stage)
        {
            stage = null;
            if (string.IsNullOrWhiteSpace(stageId))
            {
                return false;
            }

            string normalizedId = stageId.Trim();
            for (int index = 0; index < stages.Count; index++)
            {
                StageDefinition candidate = stages[index];
                if (candidate != null && string.Equals(candidate.Id, normalizedId, StringComparison.Ordinal))
                {
                    stage = candidate;
                    return true;
                }
            }

            return false;
        }

        public StageDefinition GetCurrentStage(PlayerState state)
        {
            if (stages == null || stages.Count == 0)
            {
                return null;
            }

            for (int index = 0; index < stages.Count; index++)
            {
                StageDefinition stage = stages[index];
                if (stage != null && IsStageUnlocked(stage, state) &&
                    (state == null || !state.IsStageCleared(stage.Id)))
                {
                    return stage;
                }
            }

            return stages[stages.Count - 1];
        }

        public bool IsStageUnlocked(StageDefinition stage, PlayerState state)
        {
            return stage != null &&
                   (string.IsNullOrEmpty(stage.PrerequisiteStageId) ||
                    (state != null && state.IsStageCleared(stage.PrerequisiteStageId)));
        }

        public PlayerState CreateDefaultPlayerState()
        {
            return PlayerState.CreateDefault(startingCurrency, starterCharacterIds);
        }

        public void Configure(
            int initialCurrency,
            IEnumerable<string> starterIds,
            IEnumerable<string> demoPlayerBattleIds,
            IEnumerable<string> demoEnemyBattleIds,
            IEnumerable<CharacterDefinition> characterDefinitions,
            IEnumerable<SkillDefinition> skillDefinitions,
            IEnumerable<GachaBannerDefinition> banners)
        {
            Configure(
                initialCurrency,
                starterIds,
                demoPlayerBattleIds,
                demoEnemyBattleIds,
                characterDefinitions,
                skillDefinitions,
                banners,
                stages);
        }

        public void Configure(
            int initialCurrency,
            IEnumerable<string> starterIds,
            IEnumerable<string> demoPlayerBattleIds,
            IEnumerable<string> demoEnemyBattleIds,
            IEnumerable<CharacterDefinition> characterDefinitions,
            IEnumerable<SkillDefinition> skillDefinitions,
            IEnumerable<GachaBannerDefinition> banners,
            IEnumerable<StageDefinition> stageDefinitions)
        {
            startingCurrency = Mathf.Max(0, initialCurrency);
            starterCharacterIds = starterIds == null ? new List<string>() : new List<string>(starterIds);
            demoPlayerBattleCharacterIds = demoPlayerBattleIds == null
                ? new List<string>()
                : new List<string>(demoPlayerBattleIds);
            demoEnemyBattleCharacterIds = demoEnemyBattleIds == null
                ? new List<string>()
                : new List<string>(demoEnemyBattleIds);
            characters = characterDefinitions == null
                ? new List<CharacterDefinition>()
                : new List<CharacterDefinition>(characterDefinitions);
            skills = skillDefinitions == null
                ? new List<SkillDefinition>()
                : new List<SkillDefinition>(skillDefinitions);
            gachaBanners = banners == null
                ? new List<GachaBannerDefinition>()
                : new List<GachaBannerDefinition>(banners);
            stages = stageDefinitions == null
                ? new List<StageDefinition>()
                : new List<StageDefinition>(stageDefinitions);
            NormalizeLists();
        }

        private void OnValidate()
        {
            startingCurrency = Mathf.Max(0, startingCurrency);
            NormalizeLists();
        }

        private void NormalizeLists()
        {
            if (starterCharacterIds == null)
            {
                starterCharacterIds = new List<string>();
            }

            if (demoPlayerBattleCharacterIds == null)
            {
                demoPlayerBattleCharacterIds = new List<string>();
            }

            if (demoEnemyBattleCharacterIds == null)
            {
                demoEnemyBattleCharacterIds = new List<string>();
            }

            if (characters == null)
            {
                characters = new List<CharacterDefinition>();
            }

            if (skills == null)
            {
                skills = new List<SkillDefinition>();
            }

            if (gachaBanners == null)
            {
                gachaBanners = new List<GachaBannerDefinition>();
            }

            if (stages == null)
            {
                stages = new List<StageDefinition>();
            }

            var uniqueStarterIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = starterCharacterIds.Count - 1; i >= 0; i--)
            {
                string id = starterCharacterIds[i] == null ? string.Empty : starterCharacterIds[i].Trim();
                if (string.IsNullOrEmpty(id) || !uniqueStarterIds.Add(id))
                {
                    starterCharacterIds.RemoveAt(i);
                }
                else
                {
                    starterCharacterIds[i] = id;
                }
            }

            var uniqueDemoBattleIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = demoPlayerBattleCharacterIds.Count - 1; i >= 0; i--)
            {
                string id = demoPlayerBattleCharacterIds[i] == null
                    ? string.Empty
                    : demoPlayerBattleCharacterIds[i].Trim();
                if (string.IsNullOrEmpty(id) || !uniqueDemoBattleIds.Add(id))
                {
                    demoPlayerBattleCharacterIds.RemoveAt(i);
                }
                else
                {
                    demoPlayerBattleCharacterIds[i] = id;
                }
            }

            var uniqueDemoEnemyIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = demoEnemyBattleCharacterIds.Count - 1; i >= 0; i--)
            {
                string id = demoEnemyBattleCharacterIds[i] == null
                    ? string.Empty
                    : demoEnemyBattleCharacterIds[i].Trim();
                if (string.IsNullOrEmpty(id) || !uniqueDemoEnemyIds.Add(id))
                {
                    demoEnemyBattleCharacterIds.RemoveAt(i);
                }
                else
                {
                    demoEnemyBattleCharacterIds[i] = id;
                }
            }
        }
    }
}
