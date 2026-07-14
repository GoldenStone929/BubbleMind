using System;
using UnityEngine;

namespace GenericGachaRPG
{
    public sealed class PlayerPrefsJsonSaveService : ISaveService
    {
        public const string DefaultSaveKey = "GenericGachaRPG.PlayerState.v1";

        private readonly string saveKey;
        private readonly Func<PlayerState> defaultStateFactory;

        public bool HasSave => PlayerPrefs.HasKey(saveKey);

        public PlayerPrefsJsonSaveService()
            : this(() => PlayerState.CreateDefault(), DefaultSaveKey)
        {
        }

        public PlayerPrefsJsonSaveService(string customSaveKey)
            : this(() => PlayerState.CreateDefault(), customSaveKey)
        {
        }

        public PlayerPrefsJsonSaveService(Func<PlayerState> factory, string customSaveKey = DefaultSaveKey)
        {
            defaultStateFactory = factory ?? throw new ArgumentNullException(nameof(factory));
            saveKey = string.IsNullOrWhiteSpace(customSaveKey) ? DefaultSaveKey : customSaveKey.Trim();
        }

        public PlayerState Load()
        {
            if (!HasSave)
            {
                return CreateFreshAndSave();
            }

            string json = PlayerPrefs.GetString(saveKey, string.Empty);
            try
            {
                if (string.IsNullOrWhiteSpace(json))
                {
                    throw new InvalidOperationException("Save data is empty.");
                }

                PlayerState state = JsonUtility.FromJson<PlayerState>(json);
                if (state == null || !state.Normalize())
                {
                    throw new InvalidOperationException("Save data has an unsupported schema.");
                }

                return state;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Player save was invalid and has been reset. {exception.Message}");
                PlayerPrefs.DeleteKey(saveKey);
                PlayerPrefs.Save();
                return CreateFreshAndSave();
            }
        }

        public void Save(PlayerState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (!state.Normalize())
            {
                throw new InvalidOperationException("Cannot save an unsupported player-state schema.");
            }

            state.MarkSaved();
            string json = JsonUtility.ToJson(state);
            PlayerPrefs.SetString(saveKey, json);
            PlayerPrefs.Save();
        }

        public PlayerState Reset()
        {
            PlayerPrefs.DeleteKey(saveKey);
            PlayerPrefs.Save();
            return CreateFreshAndSave();
        }

        private PlayerState CreateFreshAndSave()
        {
            PlayerState state = defaultStateFactory() ?? PlayerState.CreateDefault();
            if (!state.Normalize())
            {
                state = PlayerState.CreateDefault();
            }

            Save(state);
            return state;
        }
    }
}
