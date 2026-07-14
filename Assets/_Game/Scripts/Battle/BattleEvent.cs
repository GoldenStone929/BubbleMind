namespace GenericGachaRPG
{
    /// <summary>
    /// Presentation-neutral event emitted by the simulation. Positive Amount is
    /// used for damage/healing/energy gain; energy spending emits a negative value.
    /// </summary>
    public sealed class BattleEvent
    {
        internal BattleEvent(
            long sequence,
            int tick,
            float time,
            BattleEventType type,
            BattleUnitState actor,
            BattleUnitState target,
            float amount,
            float healthAfter,
            int energyAfter,
            string skillId,
            BattleOutcome outcome)
        {
            Sequence = sequence;
            Tick = tick;
            Time = time;
            Type = type;
            ActorRuntimeId = actor?.RuntimeId;
            ActorSide = actor?.Side;
            ActorSlot = actor?.SlotIndex ?? -1;
            TargetRuntimeId = target?.RuntimeId;
            TargetSide = target?.Side;
            TargetSlot = target?.SlotIndex ?? -1;
            Amount = amount;
            HealthAfter = healthAfter;
            EnergyAfter = energyAfter;
            SkillId = skillId;
            Outcome = outcome;
        }

        public long Sequence { get; }

        public int Tick { get; }

        public float Time { get; }

        public BattleEventType Type { get; }

        public string ActorRuntimeId { get; }

        public BattleTeamSide? ActorSide { get; }

        public int ActorSlot { get; }

        public string TargetRuntimeId { get; }

        public BattleTeamSide? TargetSide { get; }

        public int TargetSlot { get; }

        public float Amount { get; }

        public float HealthAfter { get; }

        public int EnergyAfter { get; }

        public string SkillId { get; }

        public BattleOutcome Outcome { get; }
    }
}
