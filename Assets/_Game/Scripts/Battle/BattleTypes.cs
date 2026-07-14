namespace GenericGachaRPG
{
    public enum BattleTeamSide
    {
        Player = 0,
        Enemy = 1
    }

    public enum BattleOutcome
    {
        None = 0,
        PlayerVictory = 1,
        EnemyVictory = 2,
        Timeout = 3
    }

    public enum BattleEventType
    {
        BattleStarted = 0,
        BasicAttackStarted = 1,
        SkillCastStarted = 2,
        DamageApplied = 3,
        HealingApplied = 4,
        EnergyChanged = 5,
        UnitDefeated = 6,
        BattleFinished = 7
    }
}
