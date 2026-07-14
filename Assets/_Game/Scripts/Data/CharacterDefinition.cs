using UnityEngine;

namespace GenericGachaRPG
{
    [CreateAssetMenu(fileName = "CharacterDefinition", menuName = "Generic Gacha RPG/Character Definition")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [SerializeField] private string id = "character";
        [SerializeField] private string displayName = "Character";
        [TextArea, SerializeField] private string description = string.Empty;
        [SerializeField] private CharacterRole role = CharacterRole.Striker;
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
        [Min(BattleRules.MinimumAttackRange), SerializeField] private float attackRange = BattleRules.StrikerAttackRange;
        [Min(0.05f), SerializeField] private float moveSpeed = BattleRules.StrikerMoveSpeed;
        [Min(1), SerializeField] private int maxEnergy = 100;
        [Min(0), SerializeField] private int energyPerAttack = 25;
        [Min(0), SerializeField] private int energyWhenHit = 10;
        [SerializeField] private SkillDefinition skill;

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
        public int MaxEnergy => maxEnergy;
        public int EnergyPerAttack => energyPerAttack;
        public int EnergyWhenHit => energyWhenHit;
        public SkillDefinition Skill => skill;

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
            int characterMaxEnergy,
            int characterEnergyPerAttack,
            int characterEnergyWhenHit,
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
            attackRange = SanitizePositive(characterAttackRange, BattleRules.GetDefaultAttackRange(characterRole));
            moveSpeed = SanitizePositive(characterMoveSpeed, BattleRules.GetDefaultMoveSpeed(characterRole));
            maxEnergy = Mathf.Max(1, characterMaxEnergy);
            energyPerAttack = Mathf.Max(0, characterEnergyPerAttack);
            energyWhenHit = Mathf.Max(0, characterEnergyWhenHit);
            skill = characterSkill;
        }

        private void OnValidate()
        {
            id = id == null ? string.Empty : id.Trim();
            displayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName.Trim();
            maxHealth = Mathf.Max(1f, maxHealth);
            attack = Mathf.Max(0f, attack);
            defense = Mathf.Max(0f, defense);
            attackInterval = Mathf.Max(0.05f, attackInterval);
            attackRange = SanitizePositive(attackRange, BattleRules.GetDefaultAttackRange(role));
            moveSpeed = SanitizePositive(moveSpeed, BattleRules.GetDefaultMoveSpeed(role));
            maxEnergy = Mathf.Max(1, maxEnergy);
            energyPerAttack = Mathf.Max(0, energyPerAttack);
            energyWhenHit = Mathf.Max(0, energyWhenHit);
        }

        private static float SanitizePositive(float value, float fallback)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value)
                ? value
                : fallback;
        }
    }
}
