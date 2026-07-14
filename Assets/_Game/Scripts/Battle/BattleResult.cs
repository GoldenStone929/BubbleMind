using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GenericGachaRPG
{
    public sealed class BattleResult
    {
        private readonly ReadOnlyCollection<BattleEvent> _events;
        private readonly ReadOnlyCollection<BattleUnitState> _playerUnits;
        private readonly ReadOnlyCollection<BattleUnitState> _enemyUnits;

        internal BattleResult(
            BattleOutcome outcome,
            int elapsedTicks,
            float elapsedTime,
            IList<BattleEvent> events,
            IList<BattleUnitState> playerUnits,
            IList<BattleUnitState> enemyUnits)
        {
            Outcome = outcome;
            ElapsedTicks = elapsedTicks;
            ElapsedTime = elapsedTime;
            _events = Array.AsReadOnly(new List<BattleEvent>(events).ToArray());
            _playerUnits = Array.AsReadOnly(new List<BattleUnitState>(playerUnits).ToArray());
            _enemyUnits = Array.AsReadOnly(new List<BattleUnitState>(enemyUnits).ToArray());
        }

        public BattleOutcome Outcome { get; }

        public int ElapsedTicks { get; }

        public float ElapsedTime { get; }

        public bool IsTimeout => Outcome == BattleOutcome.Timeout;

        public BattleTeamSide? WinningSide
        {
            get
            {
                if (Outcome == BattleOutcome.PlayerVictory)
                {
                    return BattleTeamSide.Player;
                }

                if (Outcome == BattleOutcome.EnemyVictory)
                {
                    return BattleTeamSide.Enemy;
                }

                return null;
            }
        }

        public IReadOnlyList<BattleEvent> Events => _events;

        public IReadOnlyList<BattleUnitState> PlayerUnits => _playerUnits;

        public IReadOnlyList<BattleUnitState> EnemyUnits => _enemyUnits;
    }
}
