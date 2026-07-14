using System;
using System.Collections.Generic;

namespace GenericGachaRPG
{
    /// <summary>
    /// Headless, fixed-tick 3v3 auto-battle simulation. Run produces a complete
    /// ordered event log that can be replayed by Unity presentation code later.
    /// </summary>
    public sealed class BattleSimulation
    {
        private sealed class PendingSkillHit
        {
            public int DueTick;
            public long ScheduleOrder;
            public BattleUnitState Actor;
            public SkillDefinition Skill;
            public List<BattleUnitState> Targets;
        }

        private readonly BattleContext _context;
        private readonly DeterministicRandom _random;
        private readonly List<BattleUnitState> _playerUnits = new List<BattleUnitState>(BattleTeam.RequiredMemberCount);
        private readonly List<BattleUnitState> _enemyUnits = new List<BattleUnitState>(BattleTeam.RequiredMemberCount);
        private readonly List<BattleUnitState> _actionOrder = new List<BattleUnitState>(BattleTeam.RequiredMemberCount * 2);
        private readonly List<BattleEvent> _events = new List<BattleEvent>();
        private readonly List<PendingSkillHit> _pendingSkillHits = new List<PendingSkillHit>();

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
            ScheduleInitialActions();

            for (_currentTick = 1; _currentTick <= _context.MaxTickCount && !_finished; _currentTick++)
            {
                ResolvePendingSkillHits();
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
            for (var slot = 0; slot < BattleTeam.RequiredMemberCount; slot++)
            {
                destination.Add(new BattleUnitState(team[slot], side, slot));
            }
        }

        private void ScheduleInitialActions()
        {
            for (var index = 0; index < _actionOrder.Count; index++)
            {
                var unit = _actionOrder[index];
                unit.NextActionTick = TicksForDuration(unit.AttackInterval);
            }
        }

        private void ExecuteAutomaticAction(BattleUnitState actor)
        {
            if (actor.CanCastSkill)
            {
                var skillTargets = SelectSkillTargets(actor, actor.Skill);
                if (skillTargets.Count > 0)
                {
                    CastSkill(actor, actor.Skill, skillTargets);
                    return;
                }
            }

            ExecuteBasicAttack(actor);
            actor.NextActionTick = AddTicksSafely(_currentTick, TicksForDuration(actor.AttackInterval));
        }

        private void ExecuteBasicAttack(BattleUnitState actor)
        {
            var target = FirstAliveOpponent(actor.Side);
            if (target == null)
            {
                Finish(actor.Side == BattleTeamSide.Player
                    ? BattleOutcome.PlayerVictory
                    : BattleOutcome.EnemyVictory);
                return;
            }

            Emit(BattleEventType.BasicAttackStarted, actor, target);

            var damage = SkillEffectCalculator.CalculateBasicAttackDamage(actor.Attack, target.Defense);
            var appliedDamage = target.ApplyDamage(damage);
            Emit(
                BattleEventType.DamageApplied,
                actor,
                target,
                appliedDamage,
                target.CurrentHealth,
                target.CurrentEnergy);

            ChangeEnergy(actor, actor.EnergyPerAttack);

            if (target.IsAlive)
            {
                ChangeEnergy(target, target.EnergyWhenHit, actor);
            }
            else
            {
                Emit(BattleEventType.UnitDefeated, actor, target, 0f, target.CurrentHealth, target.CurrentEnergy);
            }

            TryFinishFromDefeat();
        }

        private void CastSkill(
            BattleUnitState actor,
            SkillDefinition skill,
            List<BattleUnitState> targets)
        {
            var spentEnergy = actor.SpendSkillEnergy();
            if (spentEnergy > 0)
            {
                Emit(
                    BattleEventType.EnergyChanged,
                    actor,
                    actor,
                    -spentEnergy,
                    actor.CurrentHealth,
                    actor.CurrentEnergy,
                    skill.Id);
            }

            Emit(
                BattleEventType.SkillCastStarted,
                actor,
                targets[0],
                0f,
                targets[0].CurrentHealth,
                targets[0].CurrentEnergy,
                skill.Id);

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
                if (pendingHit.Actor.IsAlive)
                {
                    ResolveSkillHit(pendingHit.Actor, pendingHit.Skill, pendingHit.Targets);
                }
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

                var damage = SkillEffectCalculator.CalculateDamage(
                    actor.Attack,
                    target.Defense,
                    skill.DamageMultiplier);
                var appliedDamage = target.ApplyDamage(damage);
                Emit(
                    BattleEventType.DamageApplied,
                    actor,
                    target,
                    appliedDamage,
                    target.CurrentHealth,
                    target.CurrentEnergy,
                    skill.Id);

                if (target.IsAlive)
                {
                    ChangeEnergy(target, target.EnergyWhenHit, actor, skill.Id);
                }
                else
                {
                    Emit(
                        BattleEventType.UnitDefeated,
                        actor,
                        target,
                        0f,
                        target.CurrentHealth,
                        target.CurrentEnergy,
                        skill.Id);
                }
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
                    AddRandomAliveOpponent(opponents, selected);
                    break;
            }

            return selected;
        }

        private void AddRandomAliveOpponent(
            List<BattleUnitState> opponents,
            List<BattleUnitState> destination)
        {
            var alive = new List<BattleUnitState>(opponents.Count);
            AddAliveBySlot(opponents, alive, opponents.Count);
            if (alive.Count > 0)
            {
                destination.Add(alive[_random.NextInt(alive.Count)]);
            }
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

        private BattleUnitState FirstAliveOpponent(BattleTeamSide actorSide)
        {
            var opponents = actorSide == BattleTeamSide.Player ? _enemyUnits : _playerUnits;
            for (var index = 0; index < opponents.Count; index++)
            {
                if (opponents[index].IsAlive)
                {
                    return opponents[index];
                }
            }

            return null;
        }

        private void ChangeEnergy(
            BattleUnitState unit,
            int requestedEnergy,
            BattleUnitState eventActor = null,
            string skillId = null)
        {
            var gainedEnergy = unit.GainEnergy(requestedEnergy);
            if (gainedEnergy <= 0)
            {
                return;
            }

            Emit(
                BattleEventType.EnergyChanged,
                eventActor ?? unit,
                unit,
                gainedEnergy,
                unit.CurrentHealth,
                unit.CurrentEnergy,
                skillId);
        }

        private void TryFinishFromDefeat()
        {
            var playerAlive = HasLivingUnit(_playerUnits);
            var enemyAlive = HasLivingUnit(_enemyUnits);

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
            BattleOutcome outcome = BattleOutcome.None)
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
                energyAfter,
                skillId,
                outcome));
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
