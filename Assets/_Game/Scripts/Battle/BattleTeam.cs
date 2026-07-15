using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GenericGachaRPG
{
    /// <summary>
    /// Immutable, bounded-slot input for one side of a battle.
    /// </summary>
    public sealed class BattleTeam
    {
        public const int MaximumMemberCount = BattleRules.MaximumTeamSize;
        public const int RequiredMemberCount = MaximumMemberCount;

        private readonly CharacterDefinition[] _members;
        private readonly ReadOnlyCollection<CharacterDefinition> _readOnlyMembers;

        public BattleTeam(IEnumerable<CharacterDefinition> members)
        {
            if (members == null)
            {
                throw new ArgumentNullException(nameof(members));
            }

            var collectedMembers = new List<CharacterDefinition>(members);
            if (collectedMembers.Count < 1 || collectedMembers.Count > MaximumMemberCount)
            {
                throw new ArgumentException(
                    $"A battle team must contain between 1 and {MaximumMemberCount} members.",
                    nameof(members));
            }

            for (var index = 0; index < collectedMembers.Count; index++)
            {
                if (collectedMembers[index] == null)
                {
                    throw new ArgumentException($"Battle team slot {index} is empty.", nameof(members));
                }
            }

            _members = collectedMembers.ToArray();
            _readOnlyMembers = Array.AsReadOnly(_members);
        }

        public IReadOnlyList<CharacterDefinition> Members => _readOnlyMembers;

        public int Count => _members.Length;

        public CharacterDefinition this[int slot] => _members[slot];
    }
}
