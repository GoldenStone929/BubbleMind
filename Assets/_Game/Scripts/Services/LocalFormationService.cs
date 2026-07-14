using System;
using System.Collections.Generic;

namespace GenericGachaRPG
{
    public sealed class LocalFormationService : IFormationService
    {
        private readonly GameDatabase database;
        private readonly GameStateService gameState;

        public TeamFormationState CurrentFormation => gameState.State == null ? null : gameState.State.TeamFormation;

        public bool HasValidFormation
        {
            get
            {
                IReadOnlyList<string> currentIds = CurrentFormation == null
                    ? null
                    : CurrentFormation.CharacterIds;
                return IsValidFormation(currentIds, out _);
            }
        }

        public LocalFormationService(GameDatabase gameDatabase, GameStateService gameStateService)
        {
            database = gameDatabase ?? throw new ArgumentNullException(nameof(gameDatabase));
            gameState = gameStateService ?? throw new ArgumentNullException(nameof(gameStateService));
        }

        public bool IsValidFormation(IReadOnlyList<string> characterIds, out string reason)
        {
            if (characterIds == null || characterIds.Count != TeamFormationState.RequiredMemberCount)
            {
                reason = $"A formation must contain exactly {TeamFormationState.RequiredMemberCount} characters.";
                return false;
            }

            var unique = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < characterIds.Count; i++)
            {
                string id = characterIds[i] == null ? string.Empty : characterIds[i].Trim();
                if (string.IsNullOrEmpty(id))
                {
                    reason = "A formation contains an empty character id.";
                    return false;
                }

                if (!unique.Add(id))
                {
                    reason = "A character cannot occupy more than one formation slot.";
                    return false;
                }

                if (!gameState.IsOwned(id))
                {
                    reason = $"Character '{id}' is not owned.";
                    return false;
                }

                if (!database.TryGetCharacter(id, out _))
                {
                    reason = $"Character '{id}' is missing from the game database.";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        public bool TrySetFormation(IReadOnlyList<string> characterIds, out string reason)
        {
            if (!IsValidFormation(characterIds, out reason))
            {
                return false;
            }

            var copy = new List<string>(TeamFormationState.RequiredMemberCount);
            for (int i = 0; i < characterIds.Count; i++)
            {
                copy.Add(characterIds[i].Trim());
            }

            gameState.CommitFormation(copy);
            reason = string.Empty;
            return true;
        }
    }
}
