using System;

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

    public enum CharacterElement
    {
        Neutral = 0,
        Water = 1,
        Fire = 2,
        Earth = 3,
        Wind = 4,
        Lightning = 5,
        Void = 6
    }

    public enum CharacterAbilityKind
    {
        Basic = 0,
        Ultimate = 1,
        Active = 2,
        Passive = 3,
        Domain = 4,
        Awakening = 5
    }

    public enum RuntimeSkillSlot
    {
        None = 0,
        Ultimate = 1,
        Skill2 = 2,
        Skill3 = 3
    }

    public enum ProgressionTrack
    {
        Ownership = 0,
        Level = 1,
        Rank = 2,
        Star = 3,
        Awakening = 4,
        Equipment = 5,
        Bond = 6
    }

    public enum AcquisitionSource
    {
        Starter = 0,
        StandardRecruitment = 1,
        LimitedRecruitment = 2,
        Story = 3,
        Event = 4,
        Exchange = 5
    }

    public enum SkillValueUnit
    {
        Flat = 0,
        Percent = 1,
        PercentOfAttack = 2,
        PercentOfDamage = 3,
        PercentOfMaxHealth = 4,
        Multiplier = 5,
        Seconds = 6,
        Stacks = 7
    }

    public enum ContentApprovalStatus
    {
        Draft = 0,
        Review = 1,
        Approved = 2
    }

    [Flags]
    public enum SkillTag
    {
        None = 0,
        Damage = 1 << 0,
        Healing = 1 << 1,
        Control = 1 << 2,
        Survival = 1 << 3,
        Enhancement = 1 << 4,
        Taunt = 1 << 5,
        Mobility = 1 << 6,
        Area = 1 << 7,
        Physical = 1 << 8,
        Magical = 1 << 9,
        Revival = 1 << 10
    }
}
