using System;

namespace GenericGachaRPG
{
    /// <summary>
    /// Shared data-driven P0 formulas. No character ids or presentation state are
    /// involved, so adding a new character does not require a source-code branch.
    /// </summary>
    public static class SkillEffectCalculator
    {
        public static float CalculateBasicAttackDamage(float attack, float defense)
        {
            return CalculateDamage(attack, defense, 1f);
        }

        public static float CalculateDamage(float attack, float defense, float multiplier)
        {
            var safeAttack = SanitizeNonNegative(attack);
            var safeDefense = SanitizeNonNegative(defense);
            var safeMultiplier = SanitizePositive(multiplier, 1f);
            var rawDamage = safeAttack * safeMultiplier - safeDefense;
            return RoundForBattle(Math.Max(1f, rawDamage));
        }

        public static float CalculateHealing(float attack, float multiplier)
        {
            var safeAttack = SanitizeNonNegative(attack);
            var safeMultiplier = SanitizePositive(multiplier, 1f);
            return RoundForBattle(Math.Max(0f, safeAttack * safeMultiplier));
        }

        private static float RoundForBattle(float value)
        {
            return (float)Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private static float SanitizePositive(float value, float fallback)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value) ? value : fallback;
        }

        private static float SanitizeNonNegative(float value)
        {
            return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value) ? value : 0f;
        }
    }
}
