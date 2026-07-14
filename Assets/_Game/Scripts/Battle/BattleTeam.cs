using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GenericGachaRPG
{
    /// <summary>
    /// Immutable, fixed-slot input for one side of a P0 battle.
    /// </summary>
    public sealed class BattleTeam
    {
        public const int RequiredMemberCount = 3;

        private readonly CharacterDefinition[] _members;
        private readonly ReadOnlyCollection<CharacterDefinition> _readOnlyMembers;

        public BattleTeam(
            CharacterDefinition slot0,
            CharacterDefinition slot1,
            CharacterDefinition slot2)
            : this(new[] { slot0, slot1, slot2 })
        {
        }

        public BattleTeam(IEnumerable<CharacterDefinition> members)
        {
            if (members == null)
            {
                throw new ArgumentNullException(nameof(members));
            }

            var collectedMembers = new List<CharacterDefinition>(members);
            if (collectedMembers.Count != RequiredMemberCount)
            {
                throw new ArgumentException(
                    $"A battle team must contain exactly {RequiredMemberCount} members.",
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

        public CharacterDefinition this[int slot] => _members[slot];
    }
}
