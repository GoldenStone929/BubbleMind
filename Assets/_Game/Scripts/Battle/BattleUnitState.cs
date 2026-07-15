using System;
using UnityEngine;

namespace GenericGachaRPG
{
    /// <summary>
    /// Runtime-only combat state. Static character data is snapshotted when the
    /// simulation starts so later asset changes cannot alter an in-flight battle.
    /// </summary>
    public sealed class BattleUnitState
    {
        internal BattleUnitState(
            CharacterDefinition definition,
            BattleTeamSide side,
            int slotIndex,
            float healthMultiplier = 1f,
            float attackMultiplier = 1f)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Side = side;
            SlotIndex = slotIndex;
            RuntimeId = side == BattleTeamSide.Player ? $"P{slotIndex}" : $"E{slotIndex}";

            CharacterId = definition.Id ?? string.Empty;
            DisplayName = definition.DisplayName ?? CharacterId;
            Role = definition.Role;
            MaxHealth = SanitizePositive(definition.MaxHealth * healthMultiplier, 1f);
            BaseMaxHealth = MaxHealth;
            Attack = SanitizeNonNegative(definition.Attack * attackMultiplier);
            Defense = SanitizeNonNegative(definition.Defense);
            AttackInterval = SanitizePositive(definition.AttackInterval, BattleContext.DefaultTickDuration);
            AttackRange = BattleRules.GetDefaultAttackRange(definition.Role);
            MoveSpeed = SanitizePositive(
                definition.MoveSpeed,
                BattleRules.GetDefaultMoveSpeed(definition.Role));
            MaxRage = BattleRules.MaxRage;
            RagePerAttack = BattleRules.RagePerBasicAttackHit;
            RageWhenHit = BattleRules.RagePerDamageReceived;
            UltimateSkill = definition.UltimateSkill;
            Skill2 = definition.Skill2;
            Skill3 = definition.Skill3;

            CurrentHealth = MaxHealth;
            CurrentRage = 0;
            CurrentPosition = BattleRules.GetSlotPosition(side, slotIndex);
        }

        private BattleUnitState(BattleUnitState source)
        {
            Definition = source.Definition;
            Side = source.Side;
            SlotIndex = source.SlotIndex;
            RuntimeId = source.RuntimeId;
            CharacterId = source.CharacterId;
            DisplayName = source.DisplayName;
            Role = source.Role;
            MaxHealth = source.MaxHealth;
            BaseMaxHealth = source.BaseMaxHealth;
            Attack = source.Attack;
            Defense = source.Defense;
            AttackInterval = source.AttackInterval;
            AttackRange = source.AttackRange;
            MoveSpeed = source.MoveSpeed;
            MaxRage = source.MaxRage;
            RagePerAttack = source.RagePerAttack;
            RageWhenHit = source.RageWhenHit;
            UltimateSkill = source.UltimateSkill;
            Skill2 = source.Skill2;
            Skill3 = source.Skill3;
            CurrentHealth = source.CurrentHealth;
            CurrentRage = source.CurrentRage;
            CurrentPosition = source.CurrentPosition;
            LockedTargetRuntimeId = source.LockedTargetRuntimeId;
            NextActionTick = source.NextActionTick;
            NextSkill2Tick = source.NextSkill2Tick;
            NextSkill3Tick = source.NextSkill3Tick;
        }

        public CharacterDefinition Definition { get; }

        public string RuntimeId { get; }

        public string CharacterId { get; }

        public string DisplayName { get; }

        public CharacterRole Role { get; }

        public BattleTeamSide Side { get; }

        public int SlotIndex { get; }

        public float MaxHealth { get; private set; }

        public float BaseMaxHealth { get; }

        public float Attack { get; }

        public float Defense { get; }

        public float AttackInterval { get; }

        public float AttackRange { get; }

        public float MoveSpeed { get; }

        public int MaxRage { get; }

        public int RagePerAttack { get; }

        public int RageWhenHit { get; }

        public SkillDefinition UltimateSkill { get; }

        public SkillDefinition Skill2 { get; }

        public SkillDefinition Skill3 { get; }

        public int MaxEnergy => MaxRage;

        public int EnergyPerAttack => RagePerAttack;

        public int EnergyWhenHit => RageWhenHit;

        public SkillDefinition Skill => UltimateSkill;

        public float CurrentHealth { get; private set; }

        public int CurrentRage { get; private set; }

        public int CurrentEnergy => CurrentRage;

        public Vector3 CurrentPosition { get; private set; }

        public string LockedTargetRuntimeId { get; private set; }

        public bool IsAlive => CurrentHealth > 0f;

        public float HealthNormalized => MaxHealth <= 0f ? 0f : CurrentHealth / MaxHealth;

        public float RageNormalized => MaxRage <= 0 ? 0f : (float)CurrentRage / MaxRage;

        public float EnergyNormalized => RageNormalized;

        internal int NextActionTick { get; set; }

        internal int NextSkill2Tick { get; set; }

        internal int NextSkill3Tick { get; set; }

        internal bool CanCastUltimate => UltimateSkill != null && CurrentRage >= MaxRage;

        internal bool CanCastSkill => CanCastUltimate;

        internal void SetCurrentPosition(Vector3 position)
        {
            if (!IsFinite(position.x) || !IsFinite(position.y) || !IsFinite(position.z))
            {
                throw new ArgumentOutOfRangeException(nameof(position), "Battle position must be finite.");
            }

            CurrentPosition = position;
        }

        internal void LockTarget(BattleUnitState target)
        {
            LockedTargetRuntimeId = target == null ? null : target.RuntimeId;
        }

        internal float ApplyDamage(float requestedDamage)
        {
            if (!IsAlive || requestedDamage <= 0f)
            {
                return 0f;
            }

            var previousHealth = CurrentHealth;
            CurrentHealth = Math.Max(0f, CurrentHealth - requestedDamage);
            return previousHealth - CurrentHealth;
        }

        internal float ApplyHealing(float requestedHealing)
        {
            if (!IsAlive || requestedHealing <= 0f)
            {
                return 0f;
            }

            var previousHealth = CurrentHealth;
            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + requestedHealing);
            return CurrentHealth - previousHealth;
        }

        internal float Revive(float requestedHealth)
        {
            if (IsAlive || requestedHealth <= 0f)
            {
                return 0f;
            }

            CurrentHealth = Math.Min(MaxHealth, requestedHealth);
            return CurrentHealth;
        }

        internal float ConvertStacksToMaxHealth(int stackCount, float maxHealthRatioPerStack)
        {
            if (!IsAlive || stackCount <= 0 || maxHealthRatioPerStack <= 0f)
            {
                return 0f;
            }

            float addedMaxHealth = BaseMaxHealth * maxHealthRatioPerStack * stackCount;
            if (float.IsNaN(addedMaxHealth) || float.IsInfinity(addedMaxHealth) || addedMaxHealth <= 0f)
            {
                return 0f;
            }

            MaxHealth += addedMaxHealth;
            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + addedMaxHealth);
            return addedMaxHealth;
        }

        internal int GainRage(int requestedRage)
        {
            if (MaxRage <= 0 || requestedRage <= 0)
            {
                return 0;
            }

            int previousRage = CurrentRage;
            CurrentRage = Math.Min(MaxRage, CurrentRage + requestedRage);
            return CurrentRage - previousRage;
        }

        internal int SpendUltimateRage()
        {
            if (CurrentRage <= 0)
            {
                return 0;
            }

            int spentRage = CurrentRage;
            CurrentRage = 0;
            return spentRage;
        }

        internal int GainEnergy(int requestedEnergy) => GainRage(requestedEnergy);

        internal int SpendSkillEnergy() => SpendUltimateRage();

        internal BattleUnitState CreateSnapshot()
        {
            return new BattleUnitState(this);
        }

        private static float SanitizePositive(float value, float fallback)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value) ? value : fallback;
        }

        private static float SanitizeNonNegative(float value)
        {
            return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value) ? value : 0f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
