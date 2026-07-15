using System;

namespace GenericGachaRPG
{
    /// <summary>
    /// Max-level demo contract for the limited Catherine Yuki combat kit.
    /// The simulation consumes these stable ids while authored presentation
    /// remains free to map each phase to upgraded VFX later.
    /// </summary>
    public static class CatherineYukiBattleKit
    {
        public const string CharacterId = "ur_cosmic_slime";
        public const string Skill1Id = "catherine_wind_wheel_break";
        public const string Skill2Id = "catherine_wind_wheel_dance";
        public const string Skill3Id = "catherine_star_rage";
        public const string UltimateId = "catherine_infinite_void";
        public const string DeathUltimateId = "catherine_death_infinite_void";

        // Runtime slot contract: the ultimate occupies skill slot 1, while the
        // two authored active skills alternate on the global 5/10-second cadence.
        // Star Rage remains a passive domain trigger.
        public const string RageUltimateSkillId = UltimateId;
        public const string TimedSkill2Id = Skill1Id;
        public const string TimedSkill3Id = Skill2Id;
        public const string StarRagePassiveId = Skill3Id;

        public const string UltimateChargePhaseId = "catherine_ultimate_charge";
        public const string UltimateTransformPhaseId = "catherine_ultimate_transform";
        public const string UltimateCollapsePhaseId = "catherine_ultimate_collapse";
        public const string ImaginaryMassStatusId = "catherine_imaginary_mass";
        public const string DefenseBreakDebuffId = "catherine_defense_break";
        public const string GravityDebuffId = "catherine_gravity_debuff";
        public const string TauntDebuffId = "catherine_taunt";
        public const string SuperArmorStatusId = "catherine_super_armor";
        public const string MassConversionHealingId = "catherine_mass_conversion";

        public const float DemoEnemyHealthMultiplier = 10f;
        public const float DemoEnemyAttackMultiplier = 0.1f;

        public const int InitialImaginaryMassStacks = 30;
        public const int BaseImaginaryMassStackCap = 30;
        public const int AwakenedImaginaryMassStackCap = 50;
        public const int OverlordStackThreshold = 25;
        public const int StarRageStacksPerTrigger = 2;
        public const float StarRageTriggerChance = 0.99f;
        public const float DamageReductionPerStack = 0.015f;
        public const float AwakeningDamageMultiplier = 1.35f;
        public const float AwakeningDamageTakenMultiplier = 0.65f;

        public const float Skill1DamageMultiplier = 6f;
        public const float Skill2HitDamageMultiplier = 2f;
        public const float Skill2HealingFromDamageMultiplier = 1.4f;
        public const float UltimateBaseDamageMultiplier = 9.6f;
        public const int UltimateHitCount = 4;
        public const float MinimumDeathUltimateScaling = 6f;
        public const float RevivalHealthRatio = 0.99f;
        public const int RevivalMassStacks = 20;
        public const int MaximumConvertedMassStacks = 20;
        public const float MaxHealthRatioPerConvertedStack = 0.04f;

        public const float Skill1HitDelay = 0.36f;
        public const float Skill2FirstHitDelay = 0.18f;
        public const float Skill2SecondHitDelay = 0.46f;
        public const float Skill3HitDelay = 0.28f;
        public const float UltimateChargeDuration = 0.6f;
        public const float UltimatePullDuration = 0.48f;
        public const float UltimateHitInterval = 0.32f;
        public const float UltimateCollapseDelay = 0.36f;
        public const float KnockUpDuration = 0.62f;
        public const float DefenseBreakDuration = 5f;
        public const float GravityDebuffDuration = 5f;
        public const float TauntDuration = 4f;
        public const float SuperArmorDuration = 1.1f;

        public static bool IsCatherine(string characterId)
        {
            return string.Equals(characterId, CharacterId, StringComparison.Ordinal);
        }

        public static float GetUltimateScaling(int imaginaryMassStacks, bool deathAwakening)
        {
            if (deathAwakening)
            {
                return MinimumDeathUltimateScaling;
            }

            int safeStacks = Math.Max(0, Math.Min(AwakenedImaginaryMassStackCap, imaginaryMassStacks));
            return Math.Max(1f, Math.Min(6f, 1f + safeStacks / 10));
        }

        public static string GetDisplayName(string skillId)
        {
            switch (skillId)
            {
                case Skill1Id:
                    return "Wind Wheel: Break";
                case Skill2Id:
                    return "Wind Wheel: Dance";
                case Skill3Id:
                    return "Star Rage";
                case UltimateId:
                case DeathUltimateId:
                    return "Imaginary Mass: Infinite Void";
                default:
                    return string.Empty;
            }
        }
    }
}
