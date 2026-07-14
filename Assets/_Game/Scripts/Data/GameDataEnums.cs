namespace GenericGachaRPG
{
    public enum CharacterRole
    {
        Guardian,
        Striker,
        Support
    }

    public enum Rarity
    {
        R = 0,
        SR = 1,
        SSR = 2,
        SP = 3,
        UR = 4
    }

    public enum SkillCategory
    {
        Damage,
        Healing
    }

    public enum SkillTargetMode
    {
        SingleEnemy,
        AllEnemies,
        LowestHealthAlly,
        AllAllies
    }
}
