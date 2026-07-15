using System;
using System.Collections.Generic;
using UnityEngine;

namespace GenericGachaRPG
{
    /// <summary>
    /// Headless, fixed-tick auto-battle simulation. Run produces a complete
    /// ordered event log that can be replayed by Unity presentation code later.
    /// </summary>
    public sealed class BattleSimulation
    {
        private enum CatherinePhase
        {
            Skill1Impact,
            Skill2FirstHit,
            Skill2SecondHit,
            UltimateTransform,
            UltimateHit,
            UltimateCollapse
        }

        private sealed class CatherineRuntimeState
        {
            public int ImaginaryMassStacks = CatherineYukiBattleKit.InitialImaginaryMassStacks;
            public int SuperArmorUntilTick;
            public bool DeathAwakeningUsed;
            public bool RevivalPending;
        }

        private sealed class PendingCatherinePhase
        {
            public int DueTick;
            public long ScheduleOrder;
            public BattleUnitState Actor;
            public BattleUnitState PrimaryTarget;
            public List<BattleUnitState> Targets;
            public CatherinePhase Phase;
            public int HitIndex;
            public float UltimateScaling;
            public bool DeathAwakening;
        }

        private sealed class TauntState
        {
            public BattleUnitState Source;
            public int ExpiresAtTick;
        }

        private sealed class PendingSkillHit
        {
            public int DueTick;
            public long ScheduleOrder;
            public BattleUnitState Actor;
            public SkillDefinition Skill;
            public List<BattleUnitState> Targets;
        }

        private sealed class PendingBasicHit
        {
            public int DueTick;
            public long ScheduleOrder;
            public BattleUnitState Actor;
            public BattleUnitState Target;
        }

        private sealed class MovementStep
        {
            public BattleUnitState Actor;
            public BattleUnitState Target;
            public Vector3 Destination;
        }

        private readonly BattleContext _context;
        private readonly DeterministicRandom _random;
        private readonly List<BattleUnitState> _playerUnits = new List<BattleUnitState>(BattleTeam.MaximumMemberCount);
        private readonly List<BattleUnitState> _enemyUnits = new List<BattleUnitState>(BattleTeam.MaximumMemberCount);
        private readonly List<BattleUnitState> _actionOrder = new List<BattleUnitState>(BattleTeam.MaximumMemberCount * 2);
        private readonly List<BattleEvent> _events = new List<BattleEvent>();
        private readonly List<PendingBasicHit> _pendingBasicHits = new List<PendingBasicHit>();
        private readonly List<PendingSkillHit> _pendingSkillHits = new List<PendingSkillHit>();
        private readonly List<PendingCatherinePhase> _pendingCatherinePhases =
            new List<PendingCatherinePhase>();
        private readonly Dictionary<string, CatherineRuntimeState> _catherineStates =
            new Dictionary<string, CatherineRuntimeState>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _defenseBreakUntilTick =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _gravityDebuffUntilTick =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, TauntState> _taunts =
            new Dictionary<string, TauntState>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _controlledUntilTick =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _assassinBacklineTargetIds =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private BattleResult _result;
        private long _nextEventSequence;
        private long _nextScheduleOrder;
        private int _currentTick;
        private bool _finished;

        public BattleSimulation(BattleContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _random = new DeterministicRandom(context.Seed);

            AddTeam(context.PlayerTeam, BattleTeamSide.Player, _playerUnits);
            AddTeam(context.EnemyTeam, BattleTeamSide.Enemy, _enemyUnits);

            // A stable slot order removes frame-rate and collection-order effects.
            _actionOrder.AddRange(_playerUnits);
            _actionOrder.AddRange(_enemyUnits);

            for (int index = 0; index < _actionOrder.Count; index++)
            {
                BattleUnitState unit = _actionOrder[index];
                if (CatherineYukiBattleKit.IsCatherine(unit.CharacterId))
                {
                    _catherineStates.Add(unit.RuntimeId, new CatherineRuntimeState());
                }
            }
        }

        public BattleContext Context => _context;

        public int CurrentTick => _currentTick;

        public float CurrentTime => TimeAtTick(_currentTick);

        public uint RandomState => _random.State;

        public BattleResult Run()
        {
            if (_result != null)
            {
                return _result;
            }

            Emit(BattleEventType.BattleStarted);
            EmitInitialCatherineState();
            ScheduleInitialActions();

            for (_currentTick = 1; _currentTick <= _context.MaxTickCount && !_finished; _currentTick++)
            {
                ResolvePendingCatherinePhases();
                if (_finished)
                {
                    break;
                }

                ResolvePendingBasicHits();
                if (_finished)
                {
                    break;
                }

                ResolvePendingSkillHits();
                if (_finished)
                {
                    break;
                }

                UpdateTargetLocks();
                MoveUnitsTowardLockedTargets();
                ExecuteTimedActiveSkills();
                if (_finished)
                {
                    break;
                }

                for (var index = 0; index < _actionOrder.Count; index++)
                {
                    var actor = _actionOrder[index];
                    if (!actor.IsAlive || actor.NextActionTick > _currentTick)
                    {
                        continue;
                    }

                    ExecuteAutomaticAction(actor);
                    if (_finished)
                    {
                        break;
                    }
                }
            }

            if (!_finished)
            {
                _currentTick = _context.MaxTickCount;
                Finish(BattleOutcome.Timeout);
            }

            return _result;
        }

        private void AddTeam(
            BattleTeam team,
            BattleTeamSide side,
            List<BattleUnitState> destination)
        {
            for (var slot = 0; slot < team.Count; slot++)
            {
                float healthMultiplier = side == BattleTeamSide.Enemy
                    ? _context.EnemyHealthMultiplier
                    : 1f;
                float attackMultiplier = side == BattleTeamSide.Enemy
                    ? _context.EnemyAttackMultiplier
                    : 1f;
                destination.Add(new BattleUnitState(
                    team[slot],
                    side,
                    slot,
                    healthMultiplier,
                    attackMultiplier));
            }
        }

        private void EmitInitialCatherineState()
        {
            for (int index = 0; index < _actionOrder.Count; index++)
            {
                BattleUnitState unit = _actionOrder[index];
                if (_catherineStates.TryGetValue(unit.RuntimeId, out CatherineRuntimeState state))
                {
                    Emit(
                        BattleEventType.StatusApplied,
                        unit,
                        unit,
                        state.ImaginaryMassStacks,
                        unit.CurrentHealth,
                        unit.CurrentEnergy,
                        CatherineYukiBattleKit.ImaginaryMassStatusId);
                }
            }
        }

        private void ScheduleInitialActions()
        {
            for (var index = 0; index < _actionOrder.Count; index++)
            {
                var unit = _actionOrder[index];
                unit.NextActionTick = TicksForDuration(unit.AttackInterval);
                unit.NextSkill2Tick = TicksForDuration(BattleRules.Skill2InitialCastTime);
                unit.NextSkill3Tick = TicksForDuration(BattleRules.Skill3InitialCastTime);
            }
        }

        private void ExecuteTimedActiveSkills()
        {
            int cooldownTicks = TicksForDuration(BattleRules.ActiveSkillCooldown);
            for (int index = 0; index < _actionOrder.Count && !_finished; index++)
            {
                BattleUnitState actor = _actionOrder[index];
                bool skill2Due = actor.NextSkill2Tick <= _currentTick;
                bool skill3Due = actor.NextSkill3Tick <= _currentTick;
                if (!skill2Due && !skill3Due)
                {
                    continue;
                }

                // The authored offsets prevent a collision in normal play. If a
                // unit was unavailable for several periods, only one slot may run
                // on a tick; the other remains due for the following fixed tick.
                bool castSkill2 = skill2Due &&
                                  (!skill3Due || actor.NextSkill2Tick <= actor.NextSkill3Tick);
                if (!actor.IsAlive || IsControlled(actor))
                {
                    continue;
                }

                if (_catherineStates.TryGetValue(actor.RuntimeId, out CatherineRuntimeState catherineState) &&
                    catherineState.RevivalPending)
                {
                    continue;
                }

                if (!TryBeginTimedActiveSkill(actor, castSkill2))
                {
                    continue;
                }

                if (castSkill2)
                {
                    actor.NextSkill2Tick = AdvanceRecurringTick(actor.NextSkill2Tick, cooldownTicks);
                }
                else
                {
                    actor.NextSkill3Tick = AdvanceRecurringTick(actor.NextSkill3Tick, cooldownTicks);
                }
            }
        }

        private bool TryBeginTimedActiveSkill(BattleUnitState actor, bool skill2Slot)
        {
            BattleUnitState lockedTarget = FindLockedAliveOpponent(actor);
            if (_catherineStates.TryGetValue(actor.RuntimeId, out CatherineRuntimeState catherineState))
            {
                if (lockedTarget == null || !IsWithinAttackRange(actor, lockedTarget))
                {
                    return false;
                }

                CancelPendingBasicHits(actor);
                if (skill2Slot)
                {
                    // Catherine slot 2 is Wind Wheel: Break. Star Rage remains
                    // her passive and is never inserted into the active cadence.
                    BeginCatherineSkill1(actor, lockedTarget);
                }
                else
                {
                    // Catherine slot 3 is Wind Wheel: Dance.
                    BeginCatherineSkill2(actor, lockedTarget, catherineState);
                }

                return true;
            }

            SkillDefinition skill = skill2Slot ? actor.Skill2 : actor.Skill3;
            if (skill2Slot &&
                skill != null &&
                AssassinBattleKit.IsBacklineShift(actor.CharacterId, skill.Id))
            {
                return TryBeginAssassinBacklineShift(actor, skill);
            }

            if (skill == null ||
                (skill.Category == SkillCategory.Damage &&
                 (lockedTarget == null || !IsWithinAttackRange(actor, lockedTarget))))
            {
                return false;
            }

            List<BattleUnitState> targets = SelectSkillTargets(actor, skill);
            if (targets.Count == 0)
            {
                return false;
            }

            CancelPendingBasicHits(actor);
            CastSkill(actor, skill, targets, false);
            return true;
        }

        private bool TryBeginAssassinBacklineShift(
            BattleUnitState actor,
            SkillDefinition skill)
        {
            BattleUnitState target = null;
            if (_assassinBacklineTargetIds.TryGetValue(actor.RuntimeId, out string retainedTargetId))
            {
                target = FindAliveOpponentByRuntimeId(actor, retainedTargetId);
            }

            if (target == null)
            {
                target = FindDeepestAliveBacklineOpponent(actor);
            }

            if (target == null)
            {
                return false;
            }

            _assassinBacklineTargetIds[actor.RuntimeId] = target.RuntimeId;
            CancelPendingBasicHits(actor);
            actor.LockTarget(target);
            CastSkill(actor, skill, new List<BattleUnitState> { target }, false);

            Vector3 destination = AssassinBattleKit.CalculateBacklineDestination(
                actor.CurrentPosition,
                target.CurrentPosition,
                target.Side);
            actor.SetCurrentPosition(destination);
            Emit(
                BattleEventType.UnitTeleported,
                actor,
                target,
                AssassinBattleKit.TeleportDistance,
                target.CurrentHealth,
                target.CurrentEnergy,
                skill.Id,
                duration: AssassinBattleKit.TeleportPresentationDuration);
            return true;
        }

        private int AdvanceRecurringTick(int scheduledTick, int cooldownTicks)
        {
            int nextTick = AddTicksSafely(scheduledTick, cooldownTicks);
            while (nextTick <= _currentTick && nextTick < int.MaxValue)
            {
                nextTick = AddTicksSafely(nextTick, cooldownTicks);
            }

            return nextTick;
        }

        private void CancelPendingBasicHits(BattleUnitState actor)
        {
            for (int index = _pendingBasicHits.Count - 1; index >= 0; index--)
            {
                if (_pendingBasicHits[index].Actor == actor)
                {
                    _pendingBasicHits.RemoveAt(index);
                }
            }
        }

        private void ExecuteAutomaticAction(BattleUnitState actor)
        {
            if (IsControlled(actor))
            {
                return;
            }

            BattleUnitState lockedTarget = FindLockedAliveOpponent(actor);
            if (_catherineStates.ContainsKey(actor.RuntimeId))
            {
                if (lockedTarget != null && IsWithinAttackRange(actor, lockedTarget))
                {
                    if (actor.CanCastUltimate && CanStartCatherineUltimateBeforeNextActiveSkill(actor))
                    {
                        SpendUltimateRage(actor, CatherineYukiBattleKit.UltimateId);
                        CancelPendingBasicHits(actor);
                        BeginCatherineUltimate(actor, GetAliveOpponents(actor), false);
                    }
                    else
                    {
                        BeginBasicAttack(actor, lockedTarget);
                    }
                }

                return;
            }

            if (actor.CanCastUltimate)
            {
                if (actor.UltimateSkill.Category == SkillCategory.Damage &&
                    (lockedTarget == null || !IsWithinAttackRange(actor, lockedTarget)))
                {
                    return;
                }

                var skillTargets = SelectSkillTargets(actor, actor.UltimateSkill);
                if (skillTargets.Count > 0)
                {
                    CastSkill(actor, actor.UltimateSkill, skillTargets, true);
                    return;
                }
            }

            if (lockedTarget != null && IsWithinAttackRange(actor, lockedTarget))
            {
                BeginBasicAttack(actor, lockedTarget);
            }
        }

        private bool CanStartCatherineUltimateBeforeNextActiveSkill(BattleUnitState actor)
        {
            CatherineRuntimeState state = _catherineStates[actor.RuntimeId];
            if (state.RevivalPending)
            {
                return false;
            }

            int nextTimedSkillTick = Math.Min(actor.NextSkill2Tick, actor.NextSkill3Tick);
            int ultimateDurationTicks =
                TicksForDuration(CatherineYukiBattleKit.UltimateChargeDuration) +
                TicksForDuration(CatherineYukiBattleKit.UltimatePullDuration) +
                (CatherineYukiBattleKit.UltimateHitCount - 1) *
                TicksForDuration(CatherineYukiBattleKit.UltimateHitInterval) +
                TicksForDuration(CatherineYukiBattleKit.UltimateCollapseDelay) +
                TicksForDuration(CatherineYukiBattleKit.KnockUpDuration);
            // An exact fit is valid: the ultimate resolves before timed skills are
            // processed on the boundary tick, so rejecting equality can starve a
            // full-Rage Catherine on the repeating 5-second cadence.
            return AddTicksSafely(_currentTick, ultimateDurationTicks) <= nextTimedSkillTick;
        }

        private void BeginCatherineSkill1(BattleUnitState actor, BattleUnitState lockedTarget)
        {
            List<BattleUnitState> targets = SelectLineTargets(actor, lockedTarget);
            EmitSkillCast(actor, lockedTarget, CatherineYukiBattleKit.Skill1Id);
            int delayTicks = TicksForDuration(CatherineYukiBattleKit.Skill1HitDelay);
            actor.NextActionTick = AddTicksSafely(
                _currentTick,
                Math.Max(TicksForDuration(actor.AttackInterval), AddTicksSafely(delayTicks, 1)));
            ScheduleCatherinePhase(
                actor,
                lockedTarget,
                targets,
                CatherinePhase.Skill1Impact,
                AddTicksSafely(_currentTick, delayTicks));
        }

        private void BeginCatherineSkill2(
            BattleUnitState actor,
            BattleUnitState lockedTarget,
            CatherineRuntimeState state)
        {
            EmitSkillCast(actor, lockedTarget, CatherineYukiBattleKit.Skill2Id);
            state.SuperArmorUntilTick = AddTicksSafely(
                _currentTick,
                TicksForDuration(CatherineYukiBattleKit.SuperArmorDuration));
            Emit(
                BattleEventType.StatusApplied,
                actor,
                actor,
                CatherineYukiBattleKit.SuperArmorDuration,
                actor.CurrentHealth,
                actor.CurrentEnergy,
                CatherineYukiBattleKit.SuperArmorStatusId,
                duration: CatherineYukiBattleKit.SuperArmorDuration);

            int firstHitTicks = TicksForDuration(CatherineYukiBattleKit.Skill2FirstHitDelay);
            int secondHitTicks = TicksForDuration(CatherineYukiBattleKit.Skill2SecondHitDelay);
            actor.NextActionTick = AddTicksSafely(
                _currentTick,
                Math.Max(TicksForDuration(actor.AttackInterval), AddTicksSafely(secondHitTicks, 1)));
            var targets = new List<BattleUnitState> { lockedTarget };
            ScheduleCatherinePhase(
                actor,
                lockedTarget,
                targets,
                CatherinePhase.Skill2FirstHit,
                AddTicksSafely(_currentTick, firstHitTicks));
            ScheduleCatherinePhase(
                actor,
                lockedTarget,
                targets,
                CatherinePhase.Skill2SecondHit,
                AddTicksSafely(_currentTick, secondHitTicks));
        }

        private void BeginCatherineUltimate(
            BattleUnitState actor,
            List<BattleUnitState> targets,
            bool deathAwakening)
        {
            if (targets.Count == 0)
            {
                return;
            }

            CatherineRuntimeState state = _catherineStates[actor.RuntimeId];
            string castSkillId = deathAwakening
                ? CatherineYukiBattleKit.DeathUltimateId
                : CatherineYukiBattleKit.UltimateId;
            BattleUnitState primaryTarget = FindLockedAliveOpponent(actor) ?? targets[0];
            EmitSkillCast(actor, primaryTarget, castSkillId, !deathAwakening);
            Emit(
                BattleEventType.UltimatePhase,
                actor,
                primaryTarget,
                state.ImaginaryMassStacks,
                actor.CurrentHealth,
                actor.CurrentEnergy,
                CatherineYukiBattleKit.UltimateChargePhaseId,
                duration: CatherineYukiBattleKit.UltimateChargeDuration);

            float scaling = CatherineYukiBattleKit.GetUltimateScaling(
                state.ImaginaryMassStacks,
                deathAwakening);
            int transformTick = AddTicksSafely(
                _currentTick,
                TicksForDuration(CatherineYukiBattleKit.UltimateChargeDuration));
            ScheduleCatherinePhase(
                actor,
                primaryTarget,
                targets,
                CatherinePhase.UltimateTransform,
                transformTick,
                ultimateScaling: scaling,
                deathAwakening: deathAwakening);

            int firstHitTick = AddTicksSafely(
                transformTick,
                TicksForDuration(CatherineYukiBattleKit.UltimatePullDuration));
            int hitSpacing = TicksForDuration(CatherineYukiBattleKit.UltimateHitInterval);
            for (int hitIndex = 0; hitIndex < CatherineYukiBattleKit.UltimateHitCount; hitIndex++)
            {
                ScheduleCatherinePhase(
                    actor,
                    primaryTarget,
                    targets,
                    CatherinePhase.UltimateHit,
                    AddTicksSafely(firstHitTick, hitIndex * hitSpacing),
                    hitIndex,
                    scaling,
                    deathAwakening);
            }

            int collapseTick = AddTicksSafely(
                AddTicksSafely(
                    firstHitTick,
                    (CatherineYukiBattleKit.UltimateHitCount - 1) * hitSpacing),
                TicksForDuration(CatherineYukiBattleKit.UltimateCollapseDelay));
            ScheduleCatherinePhase(
                actor,
                primaryTarget,
                targets,
                CatherinePhase.UltimateCollapse,
                collapseTick,
                CatherineYukiBattleKit.UltimateHitCount,
                scaling,
                deathAwakening);
            actor.NextActionTick = deathAwakening
                ? int.MaxValue
                : AddTicksSafely(collapseTick, TicksForDuration(actor.AttackInterval));
        }

        private void EmitSkillCast(
            BattleUnitState actor,
            BattleUnitState target,
            string skillId,
            bool triggerStarRage = true)
        {
            Emit(
                BattleEventType.SkillCastStarted,
                actor,
                target,
                0f,
                target == null ? 0f : target.CurrentHealth,
                target == null ? 0 : target.CurrentEnergy,
                skillId);
            if (triggerStarRage)
            {
                TriggerStarRage(actor);
            }
        }

        private void ScheduleCatherinePhase(
            BattleUnitState actor,
            BattleUnitState primaryTarget,
            List<BattleUnitState> targets,
            CatherinePhase phase,
            int dueTick,
            int hitIndex = 0,
            float ultimateScaling = 1f,
            bool deathAwakening = false)
        {
            _pendingCatherinePhases.Add(new PendingCatherinePhase
            {
                DueTick = dueTick,
                ScheduleOrder = _nextScheduleOrder++,
                Actor = actor,
                PrimaryTarget = primaryTarget,
                Targets = targets,
                Phase = phase,
                HitIndex = hitIndex,
                UltimateScaling = ultimateScaling,
                DeathAwakening = deathAwakening
            });
        }

        private void BeginBasicAttack(BattleUnitState actor, BattleUnitState target)
        {
            Emit(BattleEventType.BasicAttackStarted, actor, target);

            int hitDelayTicks = Math.Max(1, TicksForDuration(BattleRules.BasicAttackHitDelay));
            actor.NextActionTick = AddTicksSafely(
                _currentTick,
                Math.Max(TicksForDuration(actor.AttackInterval), AddTicksSafely(hitDelayTicks, 1)));
            _pendingBasicHits.Add(new PendingBasicHit
            {
                DueTick = AddTicksSafely(_currentTick, hitDelayTicks),
                ScheduleOrder = _nextScheduleOrder++,
                Actor = actor,
                Target = target
            });
        }

        private void ResolvePendingBasicHits()
        {
            if (_pendingBasicHits.Count == 0)
            {
                return;
            }

            var dueHits = new List<PendingBasicHit>();
            for (int index = _pendingBasicHits.Count - 1; index >= 0; index--)
            {
                PendingBasicHit pendingHit = _pendingBasicHits[index];
                if (pendingHit.DueTick <= _currentTick)
                {
                    dueHits.Add(pendingHit);
                    _pendingBasicHits.RemoveAt(index);
                }
            }

            dueHits.Sort((left, right) =>
            {
                int tickComparison = left.DueTick.CompareTo(right.DueTick);
                return tickComparison != 0
                    ? tickComparison
                    : left.ScheduleOrder.CompareTo(right.ScheduleOrder);
            });

            for (int index = 0; index < dueHits.Count && !_finished; index++)
            {
                PendingBasicHit pendingHit = dueHits[index];
                if (pendingHit.Actor.IsAlive && pendingHit.Target.IsAlive && !IsControlled(pendingHit.Actor))
                {
                    ResolveBasicHit(pendingHit.Actor, pendingHit.Target);
                }
            }
        }

        private void ResolveBasicHit(BattleUnitState actor, BattleUnitState target)
        {
            float damage = CalculateFinalDamage(actor, target, 1f);
            ApplyDamageValue(actor, target, damage, null, true);

            ChangeRage(actor, actor.RagePerAttack);
            TryFinishFromDefeat();
        }

        private void CastSkill(
            BattleUnitState actor,
            SkillDefinition skill,
            List<BattleUnitState> targets,
            bool spendUltimateRage)
        {
            BattleUnitState eventTarget = skill.Category == SkillCategory.Damage
                ? FindLockedAliveOpponent(actor)
                : targets[0];
            if (eventTarget == null)
            {
                return;
            }

            if (spendUltimateRage)
            {
                SpendUltimateRage(actor, skill.Id);
            }

            Emit(
                BattleEventType.SkillCastStarted,
                actor,
                eventTarget,
                0f,
                eventTarget.CurrentHealth,
                eventTarget.CurrentEnergy,
                skill.Id);
            TriggerStarRage(actor);

            var hitDelayTicks = skill.HitTiming <= 0f ? 0 : TicksForDuration(skill.HitTiming);
            actor.NextActionTick = AddTicksSafely(
                _currentTick,
                Math.Max(TicksForDuration(actor.AttackInterval), AddTicksSafely(hitDelayTicks, 1)));

            if (hitDelayTicks == 0)
            {
                ResolveSkillHit(actor, skill, targets);
                return;
            }

            _pendingSkillHits.Add(new PendingSkillHit
            {
                DueTick = AddTicksSafely(_currentTick, hitDelayTicks),
                ScheduleOrder = _nextScheduleOrder++,
                Actor = actor,
                Skill = skill,
                Targets = targets
            });
        }

        private void ResolvePendingSkillHits()
        {
            if (_pendingSkillHits.Count == 0)
            {
                return;
            }

            var dueHits = new List<PendingSkillHit>();
            for (var index = _pendingSkillHits.Count - 1; index >= 0; index--)
            {
                var pendingHit = _pendingSkillHits[index];
                if (pendingHit.DueTick <= _currentTick)
                {
                    dueHits.Add(pendingHit);
                    _pendingSkillHits.RemoveAt(index);
                }
            }

            dueHits.Sort((left, right) =>
            {
                var tickComparison = left.DueTick.CompareTo(right.DueTick);
                return tickComparison != 0
                    ? tickComparison
                    : left.ScheduleOrder.CompareTo(right.ScheduleOrder);
            });

            for (var index = 0; index < dueHits.Count && !_finished; index++)
            {
                var pendingHit = dueHits[index];
                // Death cancels an effect that has not reached its configured hit tick.
                if (pendingHit.Actor.IsAlive && !IsControlled(pendingHit.Actor))
                {
                    ResolveSkillHit(pendingHit.Actor, pendingHit.Skill, pendingHit.Targets);
                }
            }
        }

        private void ResolvePendingCatherinePhases()
        {
            if (_pendingCatherinePhases.Count == 0)
            {
                return;
            }

            var duePhases = new List<PendingCatherinePhase>();
            for (int index = _pendingCatherinePhases.Count - 1; index >= 0; index--)
            {
                PendingCatherinePhase pending = _pendingCatherinePhases[index];
                if (pending.DueTick <= _currentTick)
                {
                    duePhases.Add(pending);
                    _pendingCatherinePhases.RemoveAt(index);
                }
            }

            duePhases.Sort((left, right) =>
            {
                int tickComparison = left.DueTick.CompareTo(right.DueTick);
                return tickComparison != 0
                    ? tickComparison
                    : left.ScheduleOrder.CompareTo(right.ScheduleOrder);
            });

            for (int index = 0; index < duePhases.Count && !_finished; index++)
            {
                PendingCatherinePhase pending = duePhases[index];
                if (!pending.Actor.IsAlive && !pending.DeathAwakening)
                {
                    continue;
                }

                ResolveCatherinePhase(pending);
            }

            // A normal ultimate is cancelled if Catherine dies before a phase.
            // Re-check defeat after removing those cancelled phases so a stale
            // collapse cannot hold the battle open until the global timeout.
            if (!_finished)
            {
                TryFinishFromDefeat();
            }
        }

        private void ResolveCatherinePhase(PendingCatherinePhase pending)
        {
            switch (pending.Phase)
            {
                case CatherinePhase.Skill1Impact:
                    ResolveCatherineSkill1(pending);
                    break;
                case CatherinePhase.Skill2FirstHit:
                    ResolveCatherineSkill2Hit(pending, false);
                    break;
                case CatherinePhase.Skill2SecondHit:
                    ResolveCatherineSkill2Hit(pending, true);
                    break;
                case CatherinePhase.UltimateTransform:
                    ResolveCatherineUltimateTransform(pending);
                    break;
                case CatherinePhase.UltimateHit:
                    ResolveCatherineUltimateHit(pending);
                    break;
                case CatherinePhase.UltimateCollapse:
                    ResolveCatherineUltimateCollapse(pending);
                    break;
            }

            TryFinishFromDefeat();
        }

        private void ResolveCatherineSkill1(PendingCatherinePhase pending)
        {
            for (int index = 0; index < pending.Targets.Count; index++)
            {
                BattleUnitState target = pending.Targets[index];
                if (!target.IsAlive)
                {
                    continue;
                }

                ApplyDamage(
                    pending.Actor,
                    target,
                    CatherineYukiBattleKit.Skill1DamageMultiplier,
                    CatherineYukiBattleKit.Skill1Id);
                if (!target.IsAlive)
                {
                    continue;
                }

                _defenseBreakUntilTick[target.RuntimeId] = AddTicksSafely(
                    _currentTick,
                    TicksForDuration(CatherineYukiBattleKit.DefenseBreakDuration));
                Emit(
                    BattleEventType.DebuffApplied,
                    pending.Actor,
                    target,
                    CatherineYukiBattleKit.DefenseBreakDuration,
                    target.CurrentHealth,
                    target.CurrentEnergy,
                    CatherineYukiBattleKit.DefenseBreakDebuffId,
                    duration: CatherineYukiBattleKit.DefenseBreakDuration);
                KnockUpTarget(
                    pending.Actor,
                    target,
                    CatherineYukiBattleKit.Skill1Id,
                    CatherineYukiBattleKit.WindWheelBreakKnockbackDistance);
            }
        }

        private void ResolveCatherineSkill2Hit(PendingCatherinePhase pending, bool chargeHit)
        {
            BattleUnitState target = pending.PrimaryTarget;
            if (target == null || !target.IsAlive)
            {
                return;
            }

            float appliedDamage = ApplyDamage(
                pending.Actor,
                target,
                CatherineYukiBattleKit.Skill2HitDamageMultiplier,
                CatherineYukiBattleKit.Skill2Id);
            float requestedHealing = appliedDamage * CatherineYukiBattleKit.Skill2HealingFromDamageMultiplier;
            float appliedHealing = pending.Actor.ApplyHealing(requestedHealing);
            Emit(
                BattleEventType.HealingApplied,
                pending.Actor,
                pending.Actor,
                appliedHealing,
                pending.Actor.CurrentHealth,
                pending.Actor.CurrentEnergy,
                CatherineYukiBattleKit.Skill2Id);

            if (!chargeHit || !target.IsAlive)
            {
                return;
            }

            _taunts[target.RuntimeId] = new TauntState
            {
                Source = pending.Actor,
                ExpiresAtTick = AddTicksSafely(
                    _currentTick,
                    TicksForDuration(CatherineYukiBattleKit.TauntDuration))
            };
            target.LockTarget(pending.Actor);
            Emit(
                BattleEventType.DebuffApplied,
                pending.Actor,
                target,
                CatherineYukiBattleKit.TauntDuration,
                target.CurrentHealth,
                target.CurrentEnergy,
                CatherineYukiBattleKit.TauntDebuffId,
                duration: CatherineYukiBattleKit.TauntDuration);
        }

        private void ResolveCatherineUltimateTransform(PendingCatherinePhase pending)
        {
            Emit(
                BattleEventType.UltimatePhase,
                pending.Actor,
                pending.PrimaryTarget,
                pending.UltimateScaling,
                pending.Actor.CurrentHealth,
                pending.Actor.CurrentEnergy,
                CatherineYukiBattleKit.UltimateTransformPhaseId,
                duration: CatherineYukiBattleKit.UltimatePullDuration);

            for (int index = 0; index < pending.Targets.Count; index++)
            {
                BattleUnitState target = pending.Targets[index];
                if (!target.IsAlive)
                {
                    continue;
                }

                target.SetCurrentPosition(pending.Actor.CurrentPosition);
                int controlDurationTicks = TicksForDuration(CatherineYukiBattleKit.UltimatePullDuration) +
                                           (CatherineYukiBattleKit.UltimateHitCount - 1) *
                                           TicksForDuration(CatherineYukiBattleKit.UltimateHitInterval) +
                                           TicksForDuration(CatherineYukiBattleKit.UltimateCollapseDelay);
                _controlledUntilTick[target.RuntimeId] = AddTicksSafely(
                    _currentTick,
                    controlDurationTicks);
                Emit(
                    BattleEventType.UnitPulled,
                    pending.Actor,
                    target,
                    0f,
                    target.CurrentHealth,
                    target.CurrentEnergy,
                    pending.DeathAwakening
                        ? CatherineYukiBattleKit.DeathUltimateId
                        : CatherineYukiBattleKit.UltimateId,
                    duration: CatherineYukiBattleKit.UltimatePullDuration);
            }
        }

        private void ResolveCatherineUltimateHit(PendingCatherinePhase pending)
        {
            string skillId = pending.DeathAwakening
                ? CatherineYukiBattleKit.DeathUltimateId
                : CatherineYukiBattleKit.UltimateId;
            float totalMultiplier = CatherineYukiBattleKit.UltimateBaseDamageMultiplier *
                                    pending.UltimateScaling;
            for (int index = 0; index < pending.Targets.Count; index++)
            {
                BattleUnitState target = pending.Targets[index];
                if (target.IsAlive)
                {
                    float splitDamage = CalculateFinalDamage(
                        pending.Actor,
                        target,
                        totalMultiplier,
                        CatherineYukiBattleKit.UltimateHitCount);
                    ApplyDamageValue(pending.Actor, target, splitDamage, skillId, true);
                }
            }
        }

        private void ResolveCatherineUltimateCollapse(PendingCatherinePhase pending)
        {
            string skillId = pending.DeathAwakening
                ? CatherineYukiBattleKit.DeathUltimateId
                : CatherineYukiBattleKit.UltimateId;
            Emit(
                BattleEventType.UltimatePhase,
                pending.Actor,
                pending.PrimaryTarget,
                pending.UltimateScaling,
                pending.Actor.CurrentHealth,
                pending.Actor.CurrentEnergy,
                CatherineYukiBattleKit.UltimateCollapsePhaseId,
                duration: CatherineYukiBattleKit.KnockUpDuration);

            for (int index = 0; index < pending.Targets.Count; index++)
            {
                BattleUnitState target = pending.Targets[index];
                if (target.IsAlive)
                {
                    KnockUpTarget(
                        pending.Actor,
                        target,
                        skillId,
                        CatherineYukiBattleKit.UltimateCollapseLaunchDistance);
                }
            }

            CatherineRuntimeState state = _catherineStates[pending.Actor.RuntimeId];
            if (pending.DeathAwakening)
            {
                float revivedHealth = pending.Actor.Revive(
                    pending.Actor.MaxHealth * CatherineYukiBattleKit.RevivalHealthRatio);
                state.RevivalPending = false;
                state.ImaginaryMassStacks = Math.Min(
                    CatherineYukiBattleKit.AwakenedImaginaryMassStackCap,
                    state.ImaginaryMassStacks + CatherineYukiBattleKit.RevivalMassStacks);
                pending.Actor.NextActionTick = AddTicksSafely(
                    _currentTick,
                    TicksForDuration(pending.Actor.AttackInterval));
                Emit(
                    BattleEventType.UnitRevived,
                    pending.Actor,
                    pending.Actor,
                    revivedHealth,
                    pending.Actor.CurrentHealth,
                    pending.Actor.CurrentEnergy,
                    CatherineYukiBattleKit.DeathUltimateId);
                EmitImaginaryMass(pending.Actor, state, CatherineYukiBattleKit.DeathUltimateId);
            }
            else
            {
                int convertedStacks = Math.Min(
                    CatherineYukiBattleKit.MaximumConvertedMassStacks,
                    state.ImaginaryMassStacks);
                state.ImaginaryMassStacks -= convertedStacks;
                float addedMaxHealth = pending.Actor.ConvertStacksToMaxHealth(
                    convertedStacks,
                    CatherineYukiBattleKit.MaxHealthRatioPerConvertedStack);
                Emit(
                    BattleEventType.HealingApplied,
                    pending.Actor,
                    pending.Actor,
                    addedMaxHealth,
                    pending.Actor.CurrentHealth,
                    pending.Actor.CurrentEnergy,
                    CatherineYukiBattleKit.MassConversionHealingId);
                EmitImaginaryMass(pending.Actor, state, CatherineYukiBattleKit.UltimateId);
            }
        }

        private void ResolveSkillHit(
            BattleUnitState actor,
            SkillDefinition skill,
            List<BattleUnitState> targets)
        {
            if (skill.Category == SkillCategory.Healing)
            {
                ResolveHealingSkill(actor, skill, targets);
            }
            else
            {
                ResolveDamageSkill(actor, skill, targets);
            }

            TryFinishFromDefeat();
        }

        private void ResolveDamageSkill(
            BattleUnitState actor,
            SkillDefinition skill,
            List<BattleUnitState> targets)
        {
            for (var index = 0; index < targets.Count; index++)
            {
                var target = targets[index];
                if (!target.IsAlive)
                {
                    continue;
                }

                ApplyDamage(actor, target, skill.DamageMultiplier, skill.Id);
            }
        }

        private void ResolveHealingSkill(
            BattleUnitState actor,
            SkillDefinition skill,
            List<BattleUnitState> targets)
        {
            var healing = SkillEffectCalculator.CalculateHealing(actor.Attack, skill.HealingMultiplier);
            for (var index = 0; index < targets.Count; index++)
            {
                var target = targets[index];
                if (!target.IsAlive)
                {
                    continue;
                }

                var appliedHealing = target.ApplyHealing(healing);
                Emit(
                    BattleEventType.HealingApplied,
                    actor,
                    target,
                    appliedHealing,
                    target.CurrentHealth,
                    target.CurrentEnergy,
                    skill.Id);
            }
        }

        private float ApplyDamage(
            BattleUnitState actor,
            BattleUnitState target,
            float damageMultiplier,
            string skillId)
        {
            float damage = CalculateFinalDamage(actor, target, damageMultiplier);
            return ApplyDamageValue(actor, target, damage, skillId, true);
        }

        private float ApplyDamageValue(
            BattleUnitState actor,
            BattleUnitState target,
            float damage,
            string skillId,
            bool grantRageWhenHit)
        {
            float appliedDamage = target.ApplyDamage(damage);
            Emit(
                BattleEventType.DamageApplied,
                actor,
                target,
                appliedDamage,
                target.CurrentHealth,
                target.CurrentEnergy,
                skillId);

            if (grantRageWhenHit && appliedDamage > 0f)
            {
                ChangeRage(target, target.RageWhenHit, actor, skillId);
            }

            if (!target.IsAlive)
            {
                Emit(
                    BattleEventType.UnitDefeated,
                    actor,
                    target,
                    0f,
                    target.CurrentHealth,
                    target.CurrentEnergy,
                    skillId);
                TryStartDeathAwakening(target);
            }

            return appliedDamage;
        }

        private float CalculateFinalDamage(
            BattleUnitState actor,
            BattleUnitState target,
            float damageMultiplier,
            int splitCount = 1)
        {
            float effectiveDefense = target.Defense;
            if (_defenseBreakUntilTick.TryGetValue(target.RuntimeId, out int defenseBreakUntil) &&
                defenseBreakUntil >= _currentTick)
            {
                effectiveDefense *= 0.65f;
            }

            float damage = SkillEffectCalculator.CalculateDamage(
                actor.Attack,
                effectiveDefense,
                damageMultiplier);
            float outgoingMultiplier = 1f;
            if (CatherineYukiBattleKit.IsCatherine(actor.CharacterId))
            {
                outgoingMultiplier *= CatherineYukiBattleKit.AwakeningDamageMultiplier;
            }

            if (_gravityDebuffUntilTick.TryGetValue(actor.RuntimeId, out int gravityDebuffUntil) &&
                gravityDebuffUntil >= _currentTick)
            {
                outgoingMultiplier *= 0.8f;
            }

            damage *= outgoingMultiplier;
            if (_catherineStates.TryGetValue(target.RuntimeId, out CatherineRuntimeState state))
            {
                float stackReduction = Math.Min(
                    0.75f,
                    state.ImaginaryMassStacks * CatherineYukiBattleKit.DamageReductionPerStack);
                damage *= CatherineYukiBattleKit.AwakeningDamageTakenMultiplier * (1f - stackReduction);
            }

            damage /= Math.Max(1, splitCount);
            return RoundForBattle(Math.Max(0.01f, damage));
        }

        private void TryStartDeathAwakening(BattleUnitState catherine)
        {
            if (!_catherineStates.TryGetValue(catherine.RuntimeId, out CatherineRuntimeState state) ||
                state.DeathAwakeningUsed)
            {
                return;
            }

            List<BattleUnitState> targets = GetAliveOpponents(catherine);
            if (targets.Count == 0)
            {
                return;
            }

            state.DeathAwakeningUsed = true;
            state.RevivalPending = true;
            BeginCatherineUltimate(catherine, targets, true);
        }

        private void TriggerStarRage(BattleUnitState skillActor)
        {
            List<BattleUnitState> opposingUnits = skillActor.Side == BattleTeamSide.Player
                ? _enemyUnits
                : _playerUnits;
            for (int index = 0; index < opposingUnits.Count; index++)
            {
                BattleUnitState candidate = opposingUnits[index];
                if (!candidate.IsAlive || !_catherineStates.ContainsKey(candidate.RuntimeId))
                {
                    continue;
                }

                if (_random.NextFloat() < CatherineYukiBattleKit.StarRageTriggerChance)
                {
                    GainImaginaryMass(
                        candidate,
                        CatherineYukiBattleKit.StarRageStacksPerTrigger,
                        CatherineYukiBattleKit.Skill3Id);
                }
            }
        }

        private void GainImaginaryMass(BattleUnitState actor, int requestedStacks, string sourceSkillId)
        {
            if (!_catherineStates.TryGetValue(actor.RuntimeId, out CatherineRuntimeState state) ||
                requestedStacks <= 0)
            {
                return;
            }

            int previousStacks = state.ImaginaryMassStacks;
            state.ImaginaryMassStacks = Math.Min(
                CatherineYukiBattleKit.AwakenedImaginaryMassStackCap,
                state.ImaginaryMassStacks + requestedStacks);
            if (state.ImaginaryMassStacks != previousStacks)
            {
                EmitImaginaryMass(actor, state, sourceSkillId);
            }
        }

        private void EmitImaginaryMass(
            BattleUnitState actor,
            CatherineRuntimeState state,
            string sourceSkillId)
        {
            Emit(
                BattleEventType.StatusApplied,
                actor,
                actor,
                state.ImaginaryMassStacks,
                actor.CurrentHealth,
                actor.CurrentEnergy,
                string.IsNullOrEmpty(sourceSkillId)
                    ? CatherineYukiBattleKit.ImaginaryMassStatusId
                    : sourceSkillId);
        }

        private void KnockUpTarget(
            BattleUnitState actor,
            BattleUnitState target,
            string skillId,
            float launchDistance)
        {
            if (_catherineStates.TryGetValue(target.RuntimeId, out CatherineRuntimeState targetState) &&
                (targetState.SuperArmorUntilTick >= _currentTick ||
                 targetState.ImaginaryMassStacks >= CatherineYukiBattleKit.OverlordStackThreshold))
            {
                return;
            }

            Vector3 destination = BattleRules.CalculateKnockbackDestination(
                actor.CurrentPosition,
                target.CurrentPosition,
                target.Side,
                target.SlotIndex,
                launchDistance);
            target.SetCurrentPosition(destination);
            _controlledUntilTick[target.RuntimeId] = AddTicksSafely(
                _currentTick,
                TicksForDuration(CatherineYukiBattleKit.KnockUpDuration));
            Emit(
                BattleEventType.UnitKnockedUp,
                actor,
                target,
                launchDistance,
                target.CurrentHealth,
                target.CurrentEnergy,
                skillId,
                duration: CatherineYukiBattleKit.KnockUpDuration);
        }

        private List<BattleUnitState> SelectLineTargets(
            BattleUnitState actor,
            BattleUnitState primaryTarget)
        {
            List<BattleUnitState> opponents = GetAliveOpponents(actor);
            var selected = new List<BattleUnitState>();
            Vector3 direction3 = primaryTarget.CurrentPosition - actor.CurrentPosition;
            var direction = new Vector2(direction3.x, direction3.z);
            if (direction.sqrMagnitude <= 0.0001f)
            {
                selected.Add(primaryTarget);
                return selected;
            }

            direction.Normalize();
            for (int index = 0; index < opponents.Count; index++)
            {
                BattleUnitState candidate = opponents[index];
                Vector3 offset3 = candidate.CurrentPosition - actor.CurrentPosition;
                var offset = new Vector2(offset3.x, offset3.z);
                float forward = Vector2.Dot(offset, direction);
                float perpendicular = Math.Abs(direction.x * offset.y - direction.y * offset.x);
                if (forward >= -BattleRules.RangeEpsilon && perpendicular <= 1.05f)
                {
                    selected.Add(candidate);
                }
            }

            if (!selected.Contains(primaryTarget))
            {
                selected.Insert(0, primaryTarget);
            }

            selected.Sort((left, right) => left.SlotIndex.CompareTo(right.SlotIndex));
            return selected;
        }

        private List<BattleUnitState> GetAliveOpponents(BattleUnitState actor)
        {
            List<BattleUnitState> opponents = actor.Side == BattleTeamSide.Player
                ? _enemyUnits
                : _playerUnits;
            var selected = new List<BattleUnitState>(opponents.Count);
            AddAliveBySlot(opponents, selected, opponents.Count);
            return selected;
        }

        private static float RoundForBattle(float value)
        {
            return (float)Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private List<BattleUnitState> SelectSkillTargets(
            BattleUnitState actor,
            SkillDefinition skill)
        {
            var allies = actor.Side == BattleTeamSide.Player ? _playerUnits : _enemyUnits;
            var opponents = actor.Side == BattleTeamSide.Player ? _enemyUnits : _playerUnits;
            var selected = new List<BattleUnitState>();

            switch (skill.TargetMode)
            {
                case SkillTargetMode.LowestHealthAlly:
                    var lowestHealthAlly = FindLowestHealthUnit(allies);
                    if (lowestHealthAlly != null)
                    {
                        selected.Add(lowestHealthAlly);
                    }
                    break;

                case SkillTargetMode.AllAllies:
                    AddAliveBySlot(allies, selected, EffectiveTargetCount(skill.TargetCount, allies.Count));
                    break;

                case SkillTargetMode.AllEnemies:
                    AddAliveBySlot(opponents, selected, EffectiveTargetCount(skill.TargetCount, opponents.Count));
                    break;

                case SkillTargetMode.SingleEnemy:
                default:
                    BattleUnitState lockedTarget = FindLockedAliveOpponent(actor);
                    if (lockedTarget != null)
                    {
                        selected.Add(lockedTarget);
                    }
                    break;
            }

            return selected;
        }

        private static int EffectiveTargetCount(int configuredCount, int availableSlots)
        {
            return configuredCount <= 0 ? availableSlots : Math.Min(configuredCount, availableSlots);
        }

        private static void AddAliveBySlot(
            List<BattleUnitState> source,
            List<BattleUnitState> destination,
            int maxCount)
        {
            for (var index = 0; index < source.Count && destination.Count < maxCount; index++)
            {
                if (source[index].IsAlive)
                {
                    destination.Add(source[index]);
                }
            }
        }

        private static BattleUnitState FindLowestHealthUnit(List<BattleUnitState> units)
        {
            BattleUnitState selected = null;
            for (var index = 0; index < units.Count; index++)
            {
                var candidate = units[index];
                if (!candidate.IsAlive)
                {
                    continue;
                }

                if (selected == null || candidate.HealthNormalized < selected.HealthNormalized)
                {
                    selected = candidate;
                }
            }

            return selected;
        }

        private BattleUnitState FindClosestAliveOpponent(BattleUnitState actor)
        {
            List<BattleUnitState> opponents = actor.Side == BattleTeamSide.Player ? _enemyUnits : _playerUnits;
            BattleUnitState selected = null;
            float selectedDistance = float.PositiveInfinity;
            for (var index = 0; index < opponents.Count; index++)
            {
                BattleUnitState candidate = opponents[index];
                if (!candidate.IsAlive)
                {
                    continue;
                }

                float distance = (actor.CurrentPosition - candidate.CurrentPosition).sqrMagnitude;
                if (selected == null || distance < selectedDistance ||
                    (Math.Abs(distance - selectedDistance) <= 0.0001f && candidate.SlotIndex < selected.SlotIndex))
                {
                    selected = candidate;
                    selectedDistance = distance;
                }
            }

            return selected;
        }

        private BattleUnitState FindDeepestAliveBacklineOpponent(BattleUnitState actor)
        {
            List<BattleUnitState> opponents = actor.Side == BattleTeamSide.Player
                ? _enemyUnits
                : _playerUnits;
            BattleUnitState selected = null;
            bool selectedIsBacklineRole = false;
            float selectedDepth = float.NegativeInfinity;
            float selectedLaneDistance = float.PositiveInfinity;

            for (int index = 0; index < opponents.Count; index++)
            {
                BattleUnitState candidate = opponents[index];
                if (!candidate.IsAlive)
                {
                    continue;
                }

                bool isBacklineRole = candidate.Role == CharacterRole.Ranged ||
                                      candidate.Role == CharacterRole.Mage ||
                                      candidate.Role == CharacterRole.Support;
                float depth = candidate.Side == BattleTeamSide.Enemy
                    ? candidate.CurrentPosition.x
                    : -candidate.CurrentPosition.x;
                float laneDistance = Math.Abs(candidate.CurrentPosition.z - actor.CurrentPosition.z);
                bool isBetter = selected == null ||
                                (isBacklineRole && !selectedIsBacklineRole) ||
                                (isBacklineRole == selectedIsBacklineRole &&
                                 (depth > selectedDepth + BattleRules.RangeEpsilon ||
                                  (Math.Abs(depth - selectedDepth) <= BattleRules.RangeEpsilon &&
                                   (laneDistance < selectedLaneDistance - BattleRules.RangeEpsilon ||
                                    (Math.Abs(laneDistance - selectedLaneDistance) <= BattleRules.RangeEpsilon &&
                                     candidate.SlotIndex < selected.SlotIndex)))));
                if (!isBetter)
                {
                    continue;
                }

                selected = candidate;
                selectedIsBacklineRole = isBacklineRole;
                selectedDepth = depth;
                selectedLaneDistance = laneDistance;
            }

            return selected;
        }

        private void UpdateTargetLocks()
        {
            for (var index = 0; index < _actionOrder.Count; index++)
            {
                BattleUnitState actor = _actionOrder[index];
                if (!actor.IsAlive)
                {
                    actor.LockTarget(null);
                    continue;
                }

                if (_taunts.TryGetValue(actor.RuntimeId, out TauntState taunt))
                {
                    if (taunt.Source != null && taunt.Source.IsAlive && taunt.ExpiresAtTick >= _currentTick)
                    {
                        actor.LockTarget(taunt.Source);
                        continue;
                    }

                    actor.LockTarget(null);
                    _taunts.Remove(actor.RuntimeId);
                }

                BattleUnitState target = FindLockedAliveOpponent(actor);
                if (target == null)
                {
                    actor.LockTarget(FindClosestAliveOpponent(actor));
                }
            }
        }

        private void MoveUnitsTowardLockedTargets()
        {
            var movementSteps = new List<MovementStep>(_actionOrder.Count);
            float maximumStep = _context.TickDuration;

            // Calculate every destination before applying any of them so both
            // teams observe the same start-of-tick position snapshot.
            for (var index = 0; index < _actionOrder.Count; index++)
            {
                BattleUnitState actor = _actionOrder[index];
                BattleUnitState target = FindLockedAliveOpponent(actor);
                if (!actor.IsAlive ||
                    target == null ||
                    IsWithinAttackRange(actor, target) ||
                    IsControlled(actor))
                {
                    continue;
                }

                Vector3 toTarget = target.CurrentPosition - actor.CurrentPosition;
                float distance = toTarget.magnitude;
                float remainingDistance = Math.Max(0f, distance - actor.AttackRange);
                float step = Math.Min(remainingDistance, actor.MoveSpeed * maximumStep);
                BattleUnitState targetLock = FindLockedAliveOpponent(target);
                bool targetWillMoveTowardActor = targetLock == actor &&
                                                 !IsWithinAttackRange(target, actor) &&
                                                 !IsControlled(target);
                if (targetWillMoveTowardActor)
                {
                    // Both destinations are calculated from the same snapshot.
                    // Divide the remaining closing distance so simultaneous
                    // movement lands on, rather than crosses, the first range edge.
                    float sharedRangeEdge = Math.Max(actor.AttackRange, target.AttackRange);
                    float sharedClosingDistance = Math.Max(0f, distance - sharedRangeEdge);
                    float combinedMoveSpeed = actor.MoveSpeed + target.MoveSpeed;
                    float actorShare = combinedMoveSpeed <= BattleRules.RangeEpsilon
                        ? sharedClosingDistance * 0.5f
                        : sharedClosingDistance * actor.MoveSpeed / combinedMoveSpeed;
                    step = Math.Min(step, actorShare);
                }

                if (step <= BattleRules.RangeEpsilon || distance <= BattleRules.RangeEpsilon)
                {
                    continue;
                }

                movementSteps.Add(new MovementStep
                {
                    Actor = actor,
                    Target = target,
                    Destination = Vector3.MoveTowards(actor.CurrentPosition, target.CurrentPosition, step)
                });
            }

            for (var index = 0; index < movementSteps.Count; index++)
            {
                MovementStep movement = movementSteps[index];
                movement.Actor.SetCurrentPosition(movement.Destination);
                Emit(
                    BattleEventType.UnitMoved,
                    movement.Actor,
                    movement.Target,
                    duration: _context.TickDuration);
            }
        }

        private BattleUnitState FindLockedAliveOpponent(BattleUnitState actor)
        {
            if (actor == null || string.IsNullOrEmpty(actor.LockedTargetRuntimeId))
            {
                return null;
            }

            List<BattleUnitState> opponents = actor.Side == BattleTeamSide.Player ? _enemyUnits : _playerUnits;
            for (var index = 0; index < opponents.Count; index++)
            {
                BattleUnitState candidate = opponents[index];
                if (candidate.IsAlive &&
                    string.Equals(candidate.RuntimeId, actor.LockedTargetRuntimeId, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }

        private BattleUnitState FindAliveOpponentByRuntimeId(
            BattleUnitState actor,
            string runtimeId)
        {
            if (actor == null || string.IsNullOrEmpty(runtimeId))
            {
                return null;
            }

            List<BattleUnitState> opponents = actor.Side == BattleTeamSide.Player
                ? _enemyUnits
                : _playerUnits;
            for (int index = 0; index < opponents.Count; index++)
            {
                BattleUnitState candidate = opponents[index];
                if (candidate.IsAlive &&
                    string.Equals(candidate.RuntimeId, runtimeId, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static bool IsWithinAttackRange(BattleUnitState actor, BattleUnitState target)
        {
            return BattleRules.IsWithinAttackRange(
                actor.CurrentPosition,
                target.CurrentPosition,
                actor.AttackRange);
        }

        private bool IsControlled(BattleUnitState unit)
        {
            return unit != null &&
                   _controlledUntilTick.TryGetValue(unit.RuntimeId, out int controlledUntil) &&
                   controlledUntil >= _currentTick;
        }

        private void SpendUltimateRage(BattleUnitState unit, string skillId)
        {
            int spentRage = unit.SpendUltimateRage();
            if (spentRage <= 0)
            {
                return;
            }

            Emit(
                BattleEventType.RageChanged,
                unit,
                unit,
                -spentRage,
                unit.CurrentHealth,
                unit.CurrentRage,
                skillId);
        }

        private void ChangeRage(
            BattleUnitState unit,
            int requestedRage,
            BattleUnitState eventActor = null,
            string skillId = null)
        {
            int gainedRage = unit.GainRage(requestedRage);
            if (gainedRage <= 0)
            {
                return;
            }

            Emit(
                BattleEventType.RageChanged,
                eventActor ?? unit,
                unit,
                gainedRage,
                unit.CurrentHealth,
                unit.CurrentRage,
                skillId);
        }

        private void TryFinishFromDefeat()
        {
            var playerAlive = HasLivingUnit(_playerUnits);
            var enemyAlive = HasLivingUnit(_enemyUnits);

            if ((!playerAlive || !enemyAlive) && HasPendingUltimateResolution())
            {
                return;
            }

            if (!playerAlive && !enemyAlive)
            {
                // This cannot occur with the current sequential P0 effects, but a
                // deterministic timeout-like result is safer than picking a side.
                Finish(BattleOutcome.Timeout);
            }
            else if (!enemyAlive)
            {
                Finish(BattleOutcome.PlayerVictory);
            }
            else if (!playerAlive)
            {
                Finish(BattleOutcome.EnemyVictory);
            }
        }

        private bool HasPendingUltimateResolution()
        {
            for (int index = 0; index < _pendingCatherinePhases.Count; index++)
            {
                PendingCatherinePhase pending = _pendingCatherinePhases[index];
                if (pending.Phase == CatherinePhase.UltimateCollapse)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasLivingUnit(List<BattleUnitState> units)
        {
            for (var index = 0; index < units.Count; index++)
            {
                if (units[index].IsAlive)
                {
                    return true;
                }
            }

            return false;
        }

        private void Finish(BattleOutcome outcome)
        {
            if (_finished)
            {
                return;
            }

            _finished = true;
            Emit(BattleEventType.BattleFinished, outcome: outcome);

            var playerSnapshots = CreateSnapshots(_playerUnits);
            var enemySnapshots = CreateSnapshots(_enemyUnits);
            _result = new BattleResult(
                outcome,
                _currentTick,
                TimeAtTick(_currentTick),
                _events,
                playerSnapshots,
                enemySnapshots);
        }

        private static List<BattleUnitState> CreateSnapshots(List<BattleUnitState> source)
        {
            var snapshots = new List<BattleUnitState>(source.Count);
            for (var index = 0; index < source.Count; index++)
            {
                snapshots.Add(source[index].CreateSnapshot());
            }

            return snapshots;
        }

        private void Emit(
            BattleEventType type,
            BattleUnitState actor = null,
            BattleUnitState target = null,
            float amount = 0f,
            float healthAfter = 0f,
            int energyAfter = 0,
            string skillId = null,
            BattleOutcome outcome = BattleOutcome.None,
            float duration = 0f)
        {
            _events.Add(new BattleEvent(
                _nextEventSequence++,
                _currentTick,
                TimeAtTick(_currentTick),
                type,
                actor,
                target,
                amount,
                healthAfter,
                target == null ? 0f : target.MaxHealth,
                energyAfter,
                skillId,
                outcome,
                actor == null ? Vector3.zero : actor.CurrentPosition,
                target == null ? Vector3.zero : target.CurrentPosition,
                duration));
        }

        private int TicksForDuration(float duration)
        {
            if (duration <= 0f || float.IsNaN(duration) || float.IsInfinity(duration))
            {
                return 1;
            }

            var requestedTicks = BattleContext.CeilingTickCount(duration, _context.TickDuration);
            return requestedTicks >= int.MaxValue ? int.MaxValue : Math.Max(1, (int)requestedTicks);
        }

        private static int AddTicksSafely(int startTick, int additionalTicks)
        {
            return additionalTicks >= int.MaxValue - startTick
                ? int.MaxValue
                : startTick + additionalTicks;
        }

        private float TimeAtTick(int tick)
        {
            return Math.Min(_context.MaxDuration, tick * _context.TickDuration);
        }
    }
}
