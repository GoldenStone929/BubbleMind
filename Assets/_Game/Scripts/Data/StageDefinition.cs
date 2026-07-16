using System;
using System.Collections.Generic;
using UnityEngine;

namespace GenericGachaRPG
{
    [CreateAssetMenu(fileName = "Stage", menuName = "Generic Gacha RPG/Stage Definition")]
    public sealed class StageDefinition : ScriptableObject
    {
        [SerializeField] private string stageId = string.Empty;
        [SerializeField] private string chapterId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [TextArea(2, 4), SerializeField] private string description = string.Empty;
        [SerializeField] private string prerequisiteStageId = string.Empty;
        [SerializeField] private List<string> enemyCharacterIds = new List<string>();
        [Min(0), SerializeField] private int energyCost = 6;
        [Min(0), SerializeField] private int recommendedPower = 1000;
        [Min(0), SerializeField] private int firstClearCrystalReward = 100;
        [Min(0), SerializeField] private int goldReward = 250;
        [Min(0), SerializeField] private int materialReward = 2;
        [SerializeField] private bool bossStage;

        public string Id => stageId;
        public string ChapterId => chapterId;
        public string DisplayName => displayName;
        public string Description => description;
        public string PrerequisiteStageId => prerequisiteStageId;
        public IReadOnlyList<string> EnemyCharacterIds => enemyCharacterIds;
        public int EnergyCost => energyCost;
        public int RecommendedPower => recommendedPower;
        public int FirstClearCrystalReward => firstClearCrystalReward;
        public int GoldReward => goldReward;
        public int MaterialReward => materialReward;
        public bool IsBossStage => bossStage;

        public void Configure(
            string id,
            string chapter,
            string name,
            string stageDescription,
            string prerequisite,
            IEnumerable<string> enemyIds,
            int entryEnergy,
            int power,
            int firstClearCrystals,
            int repeatGold,
            int materials,
            bool isBoss)
        {
            stageId = Normalize(id);
            chapterId = Normalize(chapter);
            displayName = string.IsNullOrWhiteSpace(name) ? stageId : name.Trim();
            description = string.IsNullOrWhiteSpace(stageDescription)
                ? string.Empty
                : stageDescription.Trim();
            prerequisiteStageId = Normalize(prerequisite);
            enemyCharacterIds = enemyIds == null
                ? new List<string>()
                : new List<string>(enemyIds);
            energyCost = Mathf.Max(0, entryEnergy);
            recommendedPower = Mathf.Max(0, power);
            firstClearCrystalReward = Mathf.Max(0, firstClearCrystals);
            goldReward = Mathf.Max(0, repeatGold);
            materialReward = Mathf.Max(0, materials);
            bossStage = isBoss;
            NormalizeList();
        }

        private void OnValidate()
        {
            stageId = Normalize(stageId);
            chapterId = Normalize(chapterId);
            displayName = string.IsNullOrWhiteSpace(displayName) ? stageId : displayName.Trim();
            description = description == null ? string.Empty : description.Trim();
            prerequisiteStageId = Normalize(prerequisiteStageId);
            energyCost = Mathf.Max(0, energyCost);
            recommendedPower = Mathf.Max(0, recommendedPower);
            firstClearCrystalReward = Mathf.Max(0, firstClearCrystalReward);
            goldReward = Mathf.Max(0, goldReward);
            materialReward = Mathf.Max(0, materialReward);
            NormalizeList();
        }

        private void NormalizeList()
        {
            if (enemyCharacterIds == null)
            {
                enemyCharacterIds = new List<string>();
                return;
            }

            var unique = new HashSet<string>(StringComparer.Ordinal);
            for (int index = enemyCharacterIds.Count - 1; index >= 0; index--)
            {
                string id = Normalize(enemyCharacterIds[index]);
                if (string.IsNullOrEmpty(id) || !unique.Add(id))
                {
                    enemyCharacterIds.RemoveAt(index);
                }
                else
                {
                    enemyCharacterIds[index] = id;
                }
            }
        }

        private static string Normalize(string value)
        {
            return value == null ? string.Empty : value.Trim();
        }
    }

    public sealed class StageRewardGrant
    {
        public StageRewardGrant(
            string stageId,
            bool victory,
            bool firstClear,
            int crystals,
            int gold,
            int materials,
            int rareMaterials = 0)
        {
            StageId = stageId ?? string.Empty;
            Victory = victory;
            FirstClear = firstClear;
            Crystals = Mathf.Max(0, crystals);
            Gold = Mathf.Max(0, gold);
            Materials = Mathf.Max(0, materials);
            RareMaterials = Mathf.Max(0, rareMaterials);
        }

        public string StageId { get; }
        public bool Victory { get; }
        public bool FirstClear { get; }
        public int Crystals { get; }
        public int Gold { get; }
        public int Materials { get; }
        public int RareMaterials { get; }

        public static StageRewardGrant None(string stageId, bool victory)
        {
            return new StageRewardGrant(stageId, victory, false, 0, 0, 0);
        }
    }
}
