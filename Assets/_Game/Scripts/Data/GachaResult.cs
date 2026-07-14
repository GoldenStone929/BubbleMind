using System;
using UnityEngine;

namespace GenericGachaRPG
{
    [Serializable]
    public sealed class GachaResult
    {
        [SerializeField] private bool success;
        [SerializeField] private string errorMessage = string.Empty;
        [SerializeField] private string bannerId = string.Empty;
        [SerializeField] private string characterId = string.Empty;
        [SerializeField] private Rarity rarity;
        [SerializeField] private bool isNewCharacter;
        [SerializeField] private int currencySpent;
        [SerializeField] private int remainingCurrency;
        [NonSerialized] private CharacterDefinition character;

        public bool Success => success;
        public string ErrorMessage => errorMessage;
        public string BannerId => bannerId;
        public string CharacterId => characterId;
        public CharacterDefinition Character => character;
        public Rarity Rarity => rarity;
        public bool IsNewCharacter => isNewCharacter;
        public bool IsNew => isNewCharacter;
        public int CurrencySpent => currencySpent;
        public int RemainingCurrency => remainingCurrency;
        public string Error => errorMessage;

        public static GachaResult Succeeded(
            string resultBannerId,
            CharacterDefinition drawnCharacter,
            bool isNew,
            int spent,
            int balance)
        {
            return new GachaResult
            {
                success = true,
                bannerId = resultBannerId ?? string.Empty,
                characterId = drawnCharacter == null ? string.Empty : drawnCharacter.Id,
                character = drawnCharacter,
                rarity = drawnCharacter == null ? Rarity.R : drawnCharacter.Rarity,
                isNewCharacter = isNew,
                currencySpent = Math.Max(0, spent),
                remainingCurrency = Math.Max(0, balance),
                errorMessage = string.Empty
            };
        }

        public static GachaResult Failed(string message, int balance, string resultBannerId = "")
        {
            return new GachaResult
            {
                success = false,
                errorMessage = message ?? "Gacha draw failed.",
                bannerId = resultBannerId ?? string.Empty,
                characterId = string.Empty,
                isNewCharacter = false,
                currencySpent = 0,
                remainingCurrency = Math.Max(0, balance)
            };
        }
    }
}
