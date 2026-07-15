using System;
using UnityEngine;

namespace GenericGachaRPG
{
    /// <summary>Stable runtime contract for the demo assassin's backline entry.</summary>
    public static class AssassinBattleKit
    {
        public const string CharacterId = "ember_striker";
        public const string BacklineShiftSkillId = "assassin_backline_shift";
        public const float TeleportDistance = BattleRules.MeleeAttackRange;
        public const float TeleportPresentationDuration = 0.16f;

        public static Vector3 CalculateBacklineDestination(
            Vector3 actorPosition,
            Vector3 targetPosition,
            BattleTeamSide targetSide)
        {
            float baseDirection = targetSide == BattleTeamSide.Enemy ? 1f : -1f;
            Vector3 destination = BattleRules.ClampToBattlefield(
                targetPosition + Vector3.right * baseDirection * TeleportDistance);
            Vector3 offset = destination - targetPosition;
            offset.y = 0f;
            float minimumSeparation = TeleportDistance - BattleRules.RangeEpsilon;
            if (offset.sqrMagnitude >= minimumSeparation * minimumSeparation)
            {
                return destination;
            }

            float remainingLaneDistance = Mathf.Sqrt(
                Mathf.Max(0f, TeleportDistance * TeleportDistance - offset.x * offset.x));
            float preferredLaneDirection = actorPosition.z <= targetPosition.z ? 1f : -1f;
            float preferredSpace = preferredLaneDirection > 0f
                ? BattleRules.BattlefieldHalfDepth - targetPosition.z
                : targetPosition.z + BattleRules.BattlefieldHalfDepth;
            if (preferredSpace + BattleRules.RangeEpsilon < remainingLaneDistance)
            {
                preferredLaneDirection *= -1f;
            }

            destination.z = targetPosition.z + preferredLaneDirection * remainingLaneDistance;
            return BattleRules.ClampToBattlefield(destination);
        }

        public static bool IsBacklineShift(string characterId, string skillId)
        {
            return string.Equals(characterId, CharacterId, StringComparison.Ordinal) &&
                   string.Equals(skillId, BacklineShiftSkillId, StringComparison.Ordinal);
        }
    }
}
