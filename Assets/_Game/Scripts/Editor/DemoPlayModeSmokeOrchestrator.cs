using System;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GenericGachaRPG.Editor
{
    [InitializeOnLoad]
    public static class DemoPlayModeSmokeOrchestrator
    {
        private const string MenuPath = "Tools/Generic Gacha RPG/Run Automated Play Smoke";
        private const string DatabasePath = "Assets/_Game/Data/GameDatabase.asset";
        private const string ScenePath = "Assets/_Game/Scenes/GachaRPGDemo.unity";
        private const double EditorTimeoutSeconds = 45d;

        private const string SessionPrefix = "GenericGachaRPG.PlaySmoke.";
        private const string ActiveSessionKey = SessionPrefix + "Active";
        private const string HadSaveSessionKey = SessionPrefix + "HadSave";
        private const string SaveValueSessionKey = SessionPrefix + "SaveValue";
        private const string StartedTicksSessionKey = SessionPrefix + "StartedTicks";

        static DemoPlayModeSmokeOrchestrator()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            AttachRunnerCallback();
        }

        [MenuItem(MenuPath, priority = 13)]
        public static void RunAutomatedPlaySmoke()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                throw new InvalidOperationException(
                    $"{DemoPlayModeSmokeRunner.FailMarker} Unity is still compiling or importing.");
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    $"{DemoPlayModeSmokeRunner.FailMarker} Exit Play Mode before starting the automated smoke test.");
            }

            RecoverInterruptedSessionIfNeeded();
            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (database == null)
            {
                throw new InvalidOperationException(
                    $"{DemoPlayModeSmokeRunner.FailMarker} GameDatabase is missing at '{DatabasePath}'.");
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                throw new InvalidOperationException(
                    $"{DemoPlayModeSmokeRunner.FailMarker} Demo scene is missing at '{ScenePath}'.");
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (!string.Equals(activeScene.path, ScenePath, StringComparison.Ordinal))
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    throw new OperationCanceledException(
                        $"{DemoPlayModeSmokeRunner.FailMarker} Scene change was cancelled.");
                }

                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            try
            {
                BackupAndInstallTemporarySave(database);
                AttachRunnerCallback();
                EditorApplication.isPlaying = true;
            }
            catch
            {
                RestoreOriginalSave();
                throw;
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(ActiveSessionKey, false))
            {
                return;
            }

            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    EnsureRunnerExists();
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                    Debug.LogError(
                        $"{DemoPlayModeSmokeRunner.FailMarker} Play Mode exited before the smoke runner reported completion.");
                    RestoreOriginalSave();
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    RestoreOriginalSave();
                    break;
            }
        }

        private static void OnRunnerCompleted(bool success, string message)
        {
            DemoPlayModeSmokeRunner.Completed -= OnRunnerCompleted;
            RestoreOriginalSave();

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = false;
            }

            if (Application.isBatchMode)
            {
                EditorApplication.delayCall += () => EditorApplication.Exit(success ? 0 : 1);
            }
        }

        private static void OnEditorUpdate()
        {
            if (!SessionState.GetBool(ActiveSessionKey, false))
            {
                return;
            }

            if (EditorApplication.isPlaying)
            {
                EnsureRunnerExists();
            }

            string rawTicks = SessionState.GetString(StartedTicksSessionKey, string.Empty);
            if (!long.TryParse(rawTicks, NumberStyles.Integer, CultureInfo.InvariantCulture, out long startedTicks))
            {
                return;
            }

            double elapsedSeconds = (DateTime.UtcNow.Ticks - startedTicks) / (double)TimeSpan.TicksPerSecond;
            if (elapsedSeconds <= EditorTimeoutSeconds)
            {
                return;
            }

            Debug.LogError(
                $"{DemoPlayModeSmokeRunner.FailMarker} Editor timeout after {EditorTimeoutSeconds:0}s.");
            RestoreOriginalSave();
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = false;
            }

            if (Application.isBatchMode)
            {
                EditorApplication.delayCall += () => EditorApplication.Exit(2);
            }
        }

        private static void EnsureRunnerExists()
        {
            AttachRunnerCallback();
            if (UnityEngine.Object.FindAnyObjectByType<DemoPlayModeSmokeRunner>() != null)
            {
                return;
            }

            var runnerObject = new GameObject("P0_PlayModeSmokeRunner");
            runnerObject.AddComponent<DemoPlayModeSmokeRunner>();
        }

        private static void AttachRunnerCallback()
        {
            DemoPlayModeSmokeRunner.Completed -= OnRunnerCompleted;
            DemoPlayModeSmokeRunner.Completed += OnRunnerCompleted;
        }

        private static void BackupAndInstallTemporarySave(GameDatabase database)
        {
            string saveKey = PlayerPrefsJsonSaveService.DefaultSaveKey;
            bool hadSave = PlayerPrefs.HasKey(saveKey);
            string originalValue = hadSave ? PlayerPrefs.GetString(saveKey, string.Empty) : string.Empty;

            SessionState.SetBool(HadSaveSessionKey, hadSave);
            SessionState.SetString(SaveValueSessionKey, originalValue);
            SessionState.SetString(
                StartedTicksSessionKey,
                DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture));
            SessionState.SetBool(ActiveSessionKey, true);

            PlayerState temporaryState = database.CreateDefaultPlayerState();
            if (temporaryState == null)
            {
                throw new InvalidOperationException(
                    $"{DemoPlayModeSmokeRunner.FailMarker} GameDatabase returned a null default state.");
            }

            string temporaryJson = JsonUtility.ToJson(temporaryState);
            if (string.IsNullOrEmpty(temporaryJson))
            {
                throw new InvalidOperationException(
                    $"{DemoPlayModeSmokeRunner.FailMarker} Temporary player save JSON is empty.");
            }

            PlayerPrefs.SetString(saveKey, temporaryJson);
            PlayerPrefs.SetInt(DemoPlayModeSmokeRunner.RuntimeActivationKey, 1);
            PlayerPrefs.Save();
        }

        private static void RecoverInterruptedSessionIfNeeded()
        {
            if (SessionState.GetBool(ActiveSessionKey, false))
            {
                RestoreOriginalSave();
            }
        }

        private static void RestoreOriginalSave()
        {
            if (!SessionState.GetBool(ActiveSessionKey, false))
            {
                return;
            }

            string saveKey = PlayerPrefsJsonSaveService.DefaultSaveKey;
            bool hadSave = SessionState.GetBool(HadSaveSessionKey, false);
            if (hadSave)
            {
                PlayerPrefs.SetString(
                    saveKey,
                    SessionState.GetString(SaveValueSessionKey, string.Empty));
            }
            else
            {
                PlayerPrefs.DeleteKey(saveKey);
            }

            PlayerPrefs.DeleteKey(DemoPlayModeSmokeRunner.RuntimeActivationKey);
            PlayerPrefs.Save();
            SessionState.SetBool(ActiveSessionKey, false);
            SessionState.EraseBool(HadSaveSessionKey);
            SessionState.EraseString(SaveValueSessionKey);
            SessionState.EraseString(StartedTicksSessionKey);
        }
    }
}
