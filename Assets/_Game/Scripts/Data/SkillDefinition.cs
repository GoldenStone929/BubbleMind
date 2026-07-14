using UnityEngine;

namespace GenericGachaRPG
{
    [CreateAssetMenu(fileName = "SkillDefinition", menuName = "Generic Gacha RPG/Skill Definition")]
    public sealed class SkillDefinition : ScriptableObject
    {
        [SerializeField] private string id = "skill";
        [SerializeField] private string displayName = "Skill";
        [TextArea, SerializeField] private string description = string.Empty;
        [SerializeField] private SkillCategory category = SkillCategory.Damage;
        [SerializeField] private SkillTargetMode targetMode = SkillTargetMode.SingleEnemy;
        [Min(0f), SerializeField] private float damageMultiplier = 1.5f;
        [Min(0f), SerializeField] private float healingMultiplier = 1f;
        [Min(0), SerializeField] private int energyCost = 100;
        [Min(0f), SerializeField] private float hitTiming = 0.35f;
        [Min(1), SerializeField] private int targetCount = 1;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public SkillCategory Category => category;
        public SkillTargetMode TargetMode => targetMode;
        public float DamageMultiplier => damageMultiplier;
        public float HealingMultiplier => healingMultiplier;
        public int EnergyCost => energyCost;
        public float HitTiming => hitTiming;
        public int TargetCount => targetCount;

        public void Configure(
            string skillId,
            string skillDisplayName,
            SkillCategory skillCategory,
            SkillTargetMode skillTargetMode,
            float skillDamageMultiplier,
            float skillHealingMultiplier,
            int skillEnergyCost,
            float skillHitTiming,
            int skillTargetCount,
            string skillDescription = "")
        {
            id = skillId == null ? string.Empty : skillId.Trim();
            displayName = string.IsNullOrWhiteSpace(skillDisplayName) ? id : skillDisplayName.Trim();
            description = skillDescription ?? string.Empty;
            category = skillCategory;
            targetMode = skillTargetMode;
            damageMultiplier = Mathf.Max(0f, skillDamageMultiplier);
            healingMultiplier = Mathf.Max(0f, skillHealingMultiplier);
            energyCost = Mathf.Max(0, skillEnergyCost);
            hitTiming = Mathf.Max(0f, skillHitTiming);
            targetCount = Mathf.Max(1, skillTargetCount);
        }

        private void OnValidate()
        {
            id = id == null ? string.Empty : id.Trim();
            displayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName.Trim();
            damageMultiplier = Mathf.Max(0f, damageMultiplier);
            healingMultiplier = Mathf.Max(0f, healingMultiplier);
            energyCost = Mathf.Max(0, energyCost);
            hitTiming = Mathf.Max(0f, hitTiming);
            targetCount = Mathf.Max(1, targetCount);
        }
    }
}
