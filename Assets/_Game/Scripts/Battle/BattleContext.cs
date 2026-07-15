using System;

namespace GenericGachaRPG
{
    /// <summary>
    /// Complete deterministic input for a bounded, asymmetric team simulation.
    /// </summary>
    public sealed class BattleContext
    {
        public const float DefaultTickDuration = 0.1f;
        public const float DefaultMaxDuration = 90f;

        public BattleContext(
            BattleTeam playerTeam,
            BattleTeam enemyTeam,
            int seed,
            float tickDuration = DefaultTickDuration,
            float maxDuration = DefaultMaxDuration,
            float enemyHealthMultiplier = 1f,
            float enemyAttackMultiplier = 1f)
        {
            PlayerTeam = playerTeam ?? throw new ArgumentNullException(nameof(playerTeam));
            EnemyTeam = enemyTeam ?? throw new ArgumentNullException(nameof(enemyTeam));

            if (!IsFinitePositive(tickDuration))
            {
                throw new ArgumentOutOfRangeException(nameof(tickDuration), "Tick duration must be finite and positive.");
            }

            if (!IsFinitePositive(maxDuration))
            {
                throw new ArgumentOutOfRangeException(nameof(maxDuration), "Maximum duration must be finite and positive.");
            }

            if (!IsFinitePositive(enemyHealthMultiplier))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(enemyHealthMultiplier),
                    "Enemy health multiplier must be finite and positive.");
            }

            if (!IsFiniteNonNegative(enemyAttackMultiplier))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(enemyAttackMultiplier),
                    "Enemy attack multiplier must be finite and non-negative.");
            }

            var requestedTickCount = CeilingTickCount(maxDuration, tickDuration);
            if (requestedTickCount > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDuration), "The requested battle contains too many ticks.");
            }

            Seed = seed;
            TickDuration = tickDuration;
            MaxDuration = maxDuration;
            MaxTickCount = Math.Max(1, (int)requestedTickCount);
            EnemyHealthMultiplier = enemyHealthMultiplier;
            EnemyAttackMultiplier = enemyAttackMultiplier;
        }

        public BattleTeam PlayerTeam { get; }

        public BattleTeam EnemyTeam { get; }

        public int Seed { get; }

        public float TickDuration { get; }

        public float MaxDuration { get; }

        public int MaxTickCount { get; }

        public float EnemyHealthMultiplier { get; }

        public float EnemyAttackMultiplier { get; }

        private static bool IsFinitePositive(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFiniteNonNegative(float value)
        {
            return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        internal static double CeilingTickCount(float duration, float tickDuration)
        {
            var exactTickCount = (double)duration / tickDuration;
            // Decimal timing values such as 0.3f / 0.1f can otherwise become
            // 3.00000007 and incorrectly consume a fourth simulation tick.
            var tolerance = 0.000001d * Math.Max(1d, Math.Abs(exactTickCount));
            return Math.Ceiling(exactTickCount - tolerance);
        }
    }
}
