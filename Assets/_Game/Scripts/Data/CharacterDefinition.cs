using UnityEngine;
using UnityEngine.Serialization;

namespace GenericGachaRPG
{
    [CreateAssetMenu(fileName = "CharacterDefinition", menuName = "Generic Gacha RPG/Character Definition")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [SerializeField] private string id = "character";
        [SerializeField] private string displayName = "Character";
        [TextArea, SerializeField] private string description = string.Empty;
        [SerializeField] private CharacterRole role = CharacterRole.Assassin;
        [SerializeField] private Rarity rarity = Rarity.R;
        [SerializeField] private bool isLimited;
        [SerializeField] private Color displayColor = Color.white;
        [SerializeField] private Sprite portrait;
        [SerializeField] private GameObject characterPrefab;

        [Header("Battle Stats")]
        [Min(1f), SerializeField] private float maxHealth = 1000f;
        [Min(0f), SerializeField] private float attack = 100f;
        [Min(0f), SerializeField] private float defense = 30f;
        [Min(0.05f), SerializeField] private float attackInterval = 1.5f;
        [Min(BattleRules.MinimumAttackRange), SerializeField] private float attackRange = BattleRules.MeleeAttackRange;
        [Min(0.05f), SerializeField] private float moveSpeed = BattleRules.AssassinMoveSpeed;
        [FormerlySerializedAs("maxEnergy"), Min(1), SerializeField] private int maxRage = BattleRules.MaxRage;
        [FormerlySerializedAs("energyPerAttack"), Min(0), SerializeField] private int ragePerAttack = BattleRules.RagePerBasicAttackHit;
        [FormerlySerializedAs("energyWhenHit"), Min(0), SerializeField] private int rageWhenHit = BattleRules.RagePerDamageReceived;
        [Tooltip("Skill slot 1. This is the Rage-powered ultimate.")]
        [SerializeField] private SkillDefinition skill;
        [Tooltip("Automatic active skill cast at 5, 15, 25... battle seconds.")]
        [SerializeField] private SkillDefinition skill2;
        [Tooltip("Automatic active skill cast at 10, 20, 30... battle seconds.")]
        [SerializeField] private SkillDefinition skill3;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public CharacterRole Role => role;
        public Rarity Rarity => rarity;
        public bool IsLimited => isLimited;
        public Color DisplayColor => displayColor;
        public Sprite Portrait => portrait;
        public GameObject CharacterPrefab => characterPrefab;
        public float MaxHealth => maxHealth;
        public float Attack => attack;
        public float Defense => defense;
        public float AttackInterval => attackInterval;
        public float AttackRange => attackRange;
        public float MoveSpeed => moveSpeed;
        public int MaxRage => maxRage;
        public int RagePerAttack => ragePerAttack;
        public int RageWhenHit => rageWhenHit;

        // Temporary source compatibility while presentation and authored assets
        // migrate from the former Energy naming to the Rage contract.
        public int MaxEnergy => maxRage;
        public int EnergyPerAttack => ragePerAttack;
        public int EnergyWhenHit => rageWhenHit;
        public SkillDefinition Skill => skill;
        public SkillDefinition UltimateSkill => skill;
        public SkillDefinition Skill2 => skill2;
        public SkillDefinition Skill3 => skill3;

        // Compatibility aliases for presentation and future tuning code.
        public float Speed => 1f / Mathf.Max(0.05f, attackInterval);
        public float BasicAttackPower => attack;

        public void Configure(
            string characterId,
            string characterDisplayName,
            CharacterRole characterRole,
            Rarity characterRarity,
            Color characterDisplayColor,
            float characterMaxHealth,
            float characterAttack,
            float characterDefense,
            float characterAttackInterval,
            float characterAttackRange,
            float characterMoveSpeed,
            int characterMaxRage,
            int characterRagePerAttack,
            int characterRageWhenHit,
            SkillDefinition characterSkill,
            string characterDescription = "",
            Sprite characterPortrait = null,
            GameObject prefab = null,
            bool limited = false)
        {
            id = characterId == null ? string.Empty : characterId.Trim();
            displayName = string.IsNullOrWhiteSpace(characterDisplayName) ? id : characterDisplayName.Trim();
            description = characterDescription ?? string.Empty;
            role = characterRole;
            rarity = characterRarity;
            isLimited = limited;
            displayColor = characterDisplayColor;
            portrait = characterPortrait;
            characterPrefab = prefab;
            maxHealth = Mathf.Max(1f, characterMaxHealth);
            attack = Mathf.Max(0f, characterAttack);
            defense = Mathf.Max(0f, characterDefense);
            attackInterval = Mathf.Max(0.05f, characterAttackInterval);
            attackRange = BattleRules.GetDefaultAttackRange(characterRole);
            moveSpeed = SanitizePositive(characterMoveSpeed, BattleRules.GetDefaultMoveSpeed(characterRole));
            maxRage = Mathf.Max(1, characterMaxRage);
            ragePerAttack = Mathf.Max(0, characterRagePerAttack);
            rageWhenHit = Mathf.Max(0, characterRageWhenHit);
            skill = characterSkill;
        }

        public void ConfigureActiveSkills(
            SkillDefinition characterSkill2,
            SkillDefinition characterSkill3)
        {
            skill2 = characterSkill2;
            skill3 = characterSkill3;
        }

        private void OnValidate()
        {
            id = id == null ? string.Empty : id.Trim();
            displayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName.Trim();
            maxHealth = Mathf.Max(1f, maxHealth);
            attack = Mathf.Max(0f, attack);
            defense = Mathf.Max(0f, defense);
            attackInterval = Mathf.Max(0.05f, attackInterval);
            attackRange = BattleRules.GetDefaultAttackRange(role);
            moveSpeed = SanitizePositive(moveSpeed, BattleRules.GetDefaultMoveSpeed(role));
            maxRage = Mathf.Max(1, maxRage);
            ragePerAttack = Mathf.Max(0, ragePerAttack);
            rageWhenHit = Mathf.Max(0, rageWhenHit);
        }

        private static float SanitizePositive(float value, float fallback)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value)
                ? value
                : fallback;
        }
    }
}
