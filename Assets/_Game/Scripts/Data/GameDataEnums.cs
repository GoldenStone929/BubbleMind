namespace GenericGachaRPG
{
    public enum CharacterRole
    {
        Tank = 0,
        Assassin = 1,
        Support = 2,
        Ranged = 3,
        Mage = 4
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
