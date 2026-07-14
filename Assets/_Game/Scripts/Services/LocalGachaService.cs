using System;

namespace GenericGachaRPG
{
    public sealed class LocalGachaService : IGachaService
    {
        private readonly GameDatabase database;
        private readonly GameStateService gameState;
        private readonly IRandomService random;

        public LocalGachaService(GameDatabase gameDatabase, GameStateService gameStateService, IRandomService randomService)
        {
            database = gameDatabase ?? throw new ArgumentNullException(nameof(gameDatabase));
            gameState = gameStateService ?? throw new ArgumentNullException(nameof(gameStateService));
            random = randomService ?? throw new ArgumentNullException(nameof(randomService));
        }

        public GachaResult DrawSingle()
        {
            return DrawSingle(database.DefaultBanner);
        }

        public GachaResult DrawSingle(string bannerId)
        {
            if (!database.TryGetBanner(bannerId, out GachaBannerDefinition banner))
            {
                return GachaResult.Failed("The requested banner does not exist.", gameState.Currency, bannerId);
            }

            return DrawSingle(banner);
        }

        public GachaResult DrawSingle(GachaBannerDefinition banner)
        {
            if (!CanDrawSingle(banner, out string reason, out double totalWeight))
            {
                return GachaResult.Failed(reason, gameState.Currency, banner == null ? string.Empty : banner.Id);
            }

            double roll = random.NextDouble() * totalWeight;
            CharacterDefinition selectedCharacter = null;
            CharacterDefinition lastValidCharacter = null;

            for (int i = 0; i < banner.Entries.Count; i++)
            {
                GachaPoolEntry entry = banner.Entries[i];
                if (!TryGetValidEntryCharacter(entry, out CharacterDefinition character))
                {
                    continue;
                }

                lastValidCharacter = character;
                roll -= entry.Weight;
                if (roll < 0d)
                {
                    selectedCharacter = character;
                    break;
                }
            }

            selectedCharacter = selectedCharacter ?? lastValidCharacter;
            if (selectedCharacter == null)
            {
                return GachaResult.Failed("The banner has no valid draw entries.", gameState.Currency, banner.Id);
            }

            if (!gameState.TryCommitGachaDraw(
                    selectedCharacter,
                    banner.SingleDrawCost,
                    out bool isNewCharacter,
                    out string errorMessage))
            {
                return GachaResult.Failed(errorMessage, gameState.Currency, banner.Id);
            }

            return GachaResult.Succeeded(
                banner.Id,
                selectedCharacter,
                isNewCharacter,
                banner.SingleDrawCost,
                gameState.Currency);
        }

        public bool CanDrawSingle(string bannerId, out string reason)
        {
            if (!database.TryGetBanner(bannerId, out GachaBannerDefinition banner))
            {
                reason = "The requested banner does not exist.";
                return false;
            }

            return CanDrawSingle(banner, out reason, out _);
        }

        public bool CanDrawSingle(GachaBannerDefinition banner, out string reason)
        {
            return CanDrawSingle(banner, out reason, out _);
        }

        private bool CanDrawSingle(GachaBannerDefinition banner, out string reason, out double totalWeight)
        {
            totalWeight = 0d;
            if (banner == null)
            {
                reason = "No gacha banner is available.";
                return false;
            }

            if (banner.SingleDrawCost < 0)
            {
                reason = "The banner draw cost is invalid.";
                return false;
            }

            if (gameState.State == null || gameState.Currency < banner.SingleDrawCost)
            {
                reason = "Not enough currency.";
                return false;
            }

            for (int i = 0; i < banner.Entries.Count; i++)
            {
                GachaPoolEntry entry = banner.Entries[i];
                if (TryGetValidEntryCharacter(entry, out _))
                {
                    totalWeight += entry.Weight;
                }
            }

            if (totalWeight <= 0d || double.IsNaN(totalWeight) || double.IsInfinity(totalWeight))
            {
                reason = "The banner has no valid weighted entries.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private bool TryGetValidEntryCharacter(GachaPoolEntry entry, out CharacterDefinition character)
        {
            character = null;
            return entry != null
                   && entry.Weight > 0f
                   && !float.IsNaN(entry.Weight)
                   && !float.IsInfinity(entry.Weight)
                   && database.TryGetCharacter(entry.CharacterId, out character);
        }
    }
}
