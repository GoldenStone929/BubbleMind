using System;
using UnityEngine;

namespace GenericGachaRPG
{
    /// <summary>
    /// Shared deterministic rules for team size, fixed formation space, and
    /// presentation timing. Simulation and presentation must use this layout
    /// together so "nearest" always refers to the unit shown on screen.
    /// </summary>
    public static class BattleRules
    {
        public const int TeamSize = 5;
        public const float BasicAttackHitDelay = 0.7f;
        public const float MinimumAttackRange = 0.1f;
        public const float GuardianAttackRange = 1.45f;
        public const float StrikerAttackRange = 2.8f;
        public const float SupportAttackRange = 4.2f;
        public const float GuardianMoveSpeed = 3.3f;
        public const float StrikerMoveSpeed = 3.7f;
        public const float SupportMoveSpeed = 3.1f;
        public const float RangeEpsilon = 0.001f;

        private static readonly Vector3[] PlayerSlotPositions =
        {
            new Vector3(-3.75f, 0f, 0f),
            new Vector3(-4.2f, 0f, -1.45f),
            new Vector3(-4.2f, 0f, 1.45f),
            new Vector3(-4.35f, 0f, -2.9f),
            new Vector3(-4.35f, 0f, 2.9f)
        };

        private static readonly Vector3[] EnemySlotPositions =
        {
            new Vector3(3.75f, 0f, 0f),
            new Vector3(4.2f, 0f, -1.45f),
            new Vector3(4.2f, 0f, 1.45f),
            new Vector3(4.35f, 0f, -2.9f),
            new Vector3(4.35f, 0f, 2.9f)
        };

        public static Vector3 GetSlotPosition(BattleTeamSide side, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= TeamSize)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(slotIndex),
                    slotIndex,
                    $"Battle slot must be between 0 and {TeamSize - 1}.");
            }

            return side == BattleTeamSide.Player
                ? PlayerSlotPositions[slotIndex]
                : EnemySlotPositions[slotIndex];
        }

        public static float GetSquaredDistance(
            BattleTeamSide firstSide,
            int firstSlot,
            BattleTeamSide secondSide,
            int secondSlot)
        {
            Vector3 delta = GetSlotPosition(firstSide, firstSlot) - GetSlotPosition(secondSide, secondSlot);
            return delta.sqrMagnitude;
        }

        public static float GetDefaultAttackRange(CharacterRole role)
        {
            switch (role)
            {
                case CharacterRole.Guardian:
                    return GuardianAttackRange;
                case CharacterRole.Support:
                    return SupportAttackRange;
                case CharacterRole.Striker:
                default:
                    return StrikerAttackRange;
            }
        }

        public static float GetDefaultMoveSpeed(CharacterRole role)
        {
            switch (role)
            {
                case CharacterRole.Guardian:
                    return GuardianMoveSpeed;
                case CharacterRole.Support:
                    return SupportMoveSpeed;
                case CharacterRole.Striker:
                default:
                    return StrikerMoveSpeed;
            }
        }

        public static bool IsWithinAttackRange(Vector3 actorPosition, Vector3 targetPosition, float attackRange)
        {
            float permittedDistance = Mathf.Max(MinimumAttackRange, attackRange) + RangeEpsilon;
            return (actorPosition - targetPosition).sqrMagnitude <= permittedDistance * permittedDistance;
        }
    }
}
