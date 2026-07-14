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
            int slotIndex)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Side = side;
            SlotIndex = slotIndex;
            RuntimeId = side == BattleTeamSide.Player ? $"P{slotIndex}" : $"E{slotIndex}";

            CharacterId = definition.Id ?? string.Empty;
            DisplayName = definition.DisplayName ?? CharacterId;
            MaxHealth = SanitizePositive(definition.MaxHealth, 1f);
            Attack = SanitizeNonNegative(definition.Attack);
            Defense = SanitizeNonNegative(definition.Defense);
            AttackInterval = SanitizePositive(definition.AttackInterval, BattleContext.DefaultTickDuration);
            AttackRange = SanitizePositive(
                definition.AttackRange,
                BattleRules.GetDefaultAttackRange(definition.Role));
            MoveSpeed = SanitizePositive(
                definition.MoveSpeed,
                BattleRules.GetDefaultMoveSpeed(definition.Role));
            MaxEnergy = Math.Max(0, definition.MaxEnergy);
            EnergyPerAttack = Math.Max(0, definition.EnergyPerAttack);
            EnergyWhenHit = Math.Max(0, definition.EnergyWhenHit);
            Skill = definition.Skill;

            CurrentHealth = MaxHealth;
            CurrentEnergy = 0;
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
            MaxHealth = source.MaxHealth;
            Attack = source.Attack;
            Defense = source.Defense;
            AttackInterval = source.AttackInterval;
            AttackRange = source.AttackRange;
            MoveSpeed = source.MoveSpeed;
            MaxEnergy = source.MaxEnergy;
            EnergyPerAttack = source.EnergyPerAttack;
            EnergyWhenHit = source.EnergyWhenHit;
            Skill = source.Skill;
            CurrentHealth = source.CurrentHealth;
            CurrentEnergy = source.CurrentEnergy;
            CurrentPosition = source.CurrentPosition;
            LockedTargetRuntimeId = source.LockedTargetRuntimeId;
            NextActionTick = source.NextActionTick;
        }

        public CharacterDefinition Definition { get; }

        public string RuntimeId { get; }

        public string CharacterId { get; }

        public string DisplayName { get; }

        public BattleTeamSide Side { get; }

        public int SlotIndex { get; }

        public float MaxHealth { get; }

        public float Attack { get; }

        public float Defense { get; }

        public float AttackInterval { get; }

        public float AttackRange { get; }

        public float MoveSpeed { get; }

        public int MaxEnergy { get; }

        public int EnergyPerAttack { get; }

        public int EnergyWhenHit { get; }

        public SkillDefinition Skill { get; }

        public float CurrentHealth { get; private set; }

        public int CurrentEnergy { get; private set; }

        public Vector3 CurrentPosition { get; private set; }

        public string LockedTargetRuntimeId { get; private set; }

        public bool IsAlive => CurrentHealth > 0f;

        public float HealthNormalized => MaxHealth <= 0f ? 0f : CurrentHealth / MaxHealth;

        public float EnergyNormalized => MaxEnergy <= 0 ? 0f : (float)CurrentEnergy / MaxEnergy;

        internal int NextActionTick { get; set; }

        internal bool CanCastSkill => Skill != null && MaxEnergy > 0 && CurrentEnergy >= MaxEnergy;

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

        internal int GainEnergy(int requestedEnergy)
        {
            if (!IsAlive || MaxEnergy <= 0 || requestedEnergy <= 0)
            {
                return 0;
            }

            var previousEnergy = CurrentEnergy;
            CurrentEnergy = Math.Min(MaxEnergy, CurrentEnergy + requestedEnergy);
            return CurrentEnergy - previousEnergy;
        }

        internal int SpendSkillEnergy()
        {
            if (CurrentEnergy <= 0)
            {
                return 0;
            }

            var configuredCost = Skill == null ? 0 : Skill.EnergyCost;
            var cost = configuredCost > 0 ? Math.Min(CurrentEnergy, configuredCost) : CurrentEnergy;
            CurrentEnergy -= cost;
            return cost;
        }

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
